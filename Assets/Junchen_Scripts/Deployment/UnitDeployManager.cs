using System.Collections.Generic;
using UnityEngine;

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

    // Return all currently deployed player units
    public List<BaseUnit> GetAllDeployedPlayerUnits()
    {
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

    // Return all currently deployed enemy units
    public List<BaseUnit> GetAllDeployedEnemyUnits()
    {
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

    // === NEW ===
    // Refreshes the deployed player units list with latest references (positions, states)
    public void UpdateRegisteredPlayerUnits(List<BaseUnit> playerUnits)
    {
        // Remove old player units
        deployedUnits.RemoveAll(u =>
        {
            BaseUnit bu = u.GetComponent<BaseUnit>();
            return bu != null && bu.teamType == UnitTeam.Player;
        });

        // Add updated player units
        foreach (var unit in playerUnits)
        {
            deployedUnits.Add(unit.gameObject);
        }

        Debug.Log($"[DeployManager] Updated registered player units. Count now: {GetAllDeployedPlayerUnits().Count}");
    }
}