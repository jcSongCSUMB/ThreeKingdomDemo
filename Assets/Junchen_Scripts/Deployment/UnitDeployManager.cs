using System.Collections.Generic;
using UnityEngine;
using System.Linq;    // for RemoveAll / filtering

public class UnitDeployManager : MonoBehaviour
{
    public static UnitDeployManager Instance;

    [Header("Unit prefab selected for deployment")]
    public GameObject selectedUnitPrefab;

    [Header("Last clicked UI button")]
    public GameObject lastSelectedButton;

    // Tracks all deployed units for cleanup
    private List<GameObject> deployedUnits = new List<GameObject>();

    private void Awake()
    {
        // Singleton pattern: ensure only one instance exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    // Called by UI button to assign a unit prefab for deployment
    public void SelectUnit(GameObject unitPrefab)
    {
        selectedUnitPrefab = unitPrefab;

        // Record last clicked button
        lastSelectedButton = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;

        Debug.Log($"Selected unit: {unitPrefab.name}");

        // NEW: Show red/green deploy zones when a unit is selected
        MapManager.Instance.ShowDeployZones();
    }

    // Clears the current selection (used when deployment is canceled or completed)
    public void ClearSelection()
    {
        selectedUnitPrefab = null;

        // NEW: Hide deploy zones when selection is cleared
        MapManager.Instance.HideDeployZones();
    }

    // Registers a unit after it's deployed so we can clear it later
    public void RegisterDeployedUnit(GameObject unit)
    {
        deployedUnits.Add(unit);
    }

    // Destroys all deployed units and clears the list
    public void ClearAllDeployedUnits()
    {
        foreach (GameObject unit in deployedUnits)
        {
            if (unit != null)
            {
                Destroy(unit);
            }
        }

        deployedUnits.Clear();
        Debug.Log("[DeployManager] All deployed units cleared.");
    }
    
    // === Prune helpers ===================================================

    // Returns true if the GameObject is null or has been destroyed.
    private bool IsDestroyedOrNull(GameObject go)
    {
        // Unity overrides == for destroyed objects so a simple null check works.
        return go == null;
    }

    // Remove destroyed objects that belong to the Player team.
    public void PruneDestroyedPlayerUnits()
    {
        int before = deployedUnits.Count;

        deployedUnits.RemoveAll(u =>
        {
            if (IsDestroyedOrNull(u)) return true;
            BaseUnit bu = u.GetComponent<BaseUnit>();
            // Remove if missing BaseUnit OR belongs to Player and is effectively gone (standOnTile may be null after Destroy)
            return bu == null || bu.teamType == UnitTeam.Player && IsDestroyedOrNull(bu.gameObject);
        });

        int after = deployedUnits.Count;
        if (before != after)
            Debug.Log($"[DeployManager] Pruned player units. {before - after} removed. New count={after}");
    }

    // Optional: prune both sides (can be used at full-turn boundaries)
    public void PruneDestroyedUnits(bool includeEnemies = true)
    {
        int before = deployedUnits.Count;

        deployedUnits.RemoveAll(u =>
        {
            if (IsDestroyedOrNull(u)) return true;
            BaseUnit bu = u.GetComponent<BaseUnit>();
            if (bu == null) return true;
            if (!includeEnemies && bu.teamType == UnitTeam.Enemy) return false;
            return IsDestroyedOrNull(bu.gameObject);
        });

        int after = deployedUnits.Count;
        if (before != after)
            Debug.Log($"[DeployManager] Pruned {(includeEnemies ? "all" : "player")} destroyed units. Removed={before - after}, Remaining={after}");
    }
    
    // Return all currently deployed player units (filtered / pruned)
    public List<BaseUnit> GetAllDeployedPlayerUnits()
    {
        // ensure dead/destroyed refs are gone before returning
        PruneDestroyedPlayerUnits();

        List<BaseUnit> playerUnits = new List<BaseUnit>();
        foreach (var unit in deployedUnits)
        {
            BaseUnit baseUnit = unit.GetComponent<BaseUnit>();
            if (baseUnit != null && baseUnit.teamType == UnitTeam.Player)
            {
                playerUnits.Add(baseUnit);
            }
        }
        return playerUnits;
    }
    
    // Return all currently deployed enemy units (filtered / pruned if desired)
    public List<BaseUnit> GetAllDeployedEnemyUnits(bool autoPrune = true)
    {
        if (autoPrune)
        {
            // prune destroyed objects but keep enemies if alive
            PruneDestroyedUnits(includeEnemies: true);
        }

        List<BaseUnit> enemyUnits = new List<BaseUnit>();
        foreach (var unit in deployedUnits)
        {
            BaseUnit baseUnit = unit.GetComponent<BaseUnit>();
            if (baseUnit != null && baseUnit.teamType == UnitTeam.Enemy)
            {
                enemyUnits.Add(baseUnit);
            }
        }
        return enemyUnits;
    }
    
    // Refreshes the deployed player units list with latest references (positions, states)
    // Called after PlayerExecution completes to sync TurnSystem + DeployManager.
    public void UpdateRegisteredPlayerUnits(List<BaseUnit> playerUnits)
    {
        // first prune out any destroyed player unit refs
        PruneDestroyedPlayerUnits();

        // Remove all existing player units (live ones will be re-added below)
        deployedUnits.RemoveAll(u =>
        {
            if (IsDestroyedOrNull(u)) return true;
            BaseUnit bu = u.GetComponent<BaseUnit>();
            return bu != null && bu.teamType == UnitTeam.Player;
        });

        // Add updated player units
        foreach (var unit in playerUnits)
        {
            if (unit != null)
            {
                deployedUnits.Add(unit.gameObject);
            }
        }

        Debug.Log($"[DeployManager] Updated registered player units. Count now: {GetAllDeployedPlayerUnits().Count}");
    }
}