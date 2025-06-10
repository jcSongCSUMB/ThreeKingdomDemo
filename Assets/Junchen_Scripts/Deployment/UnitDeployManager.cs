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
}