using UnityEngine;

public class UnitDeployManager : MonoBehaviour
{
    public static UnitDeployManager Instance;

    [Header("Unit prefab selected for deployment")]
    public GameObject selectedUnitPrefab;

    [Header("Last clicked UI button")]
    public GameObject lastSelectedButton;

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
    }

    // Clears the current selection (used when deployment is canceled or completed)
    public void ClearSelection()
    {
        selectedUnitPrefab = null;
    }
}