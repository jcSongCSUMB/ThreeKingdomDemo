using System.Collections.Generic;
using UnityEngine;

public enum PlannedAction
{
    None,
    Attack,
    Defend
}

public enum UnitTeam
{
    Player,
    Enemy
}

public class BaseUnit : MonoBehaviour
{
    // === Movement and Action Planning ===

    public OverlayTile standOnTile;
    public List<OverlayTile> plannedPath = new List<OverlayTile>();
    public PlannedAction plannedAction = PlannedAction.None;
    public bool hasFinishedAction = false;
    public UnitTeam teamType;

    // === Combat Stats ===

    public int health = 100;
    public int maxHealth = 100;
    public int attackPower = 30;
    public int defensePower = 10;
    public int tempDefenseBonus = 0;
    public int actionPoints = 3;
    public string unitType = "Infantry";
    public BaseUnit targetUnit;

    // === Health Bar UI ===

    [Header("Health Bar Settings")]
    public GameObject healthBarPrefab;
    private GameObject healthBarInstance;
    private Transform foregroundBar;

    private Vector3 healthBarOffset = new Vector3(0, 0.5f, 0);

    // === Health Bar Size Cache ===
    private float healthBarMaxWidth = 1f;  // Default value

    private void Start()
    {
        // Instantiate and attach health bar
        if (healthBarPrefab != null)
        {
            healthBarInstance = Instantiate(healthBarPrefab, transform);
            healthBarInstance.transform.localPosition = healthBarOffset;

            // Cache the foreground bar transform
            foregroundBar = healthBarInstance.transform.Find("ForegroundBar");

            // Cache the initial full-width scale
            if (foregroundBar != null)
            {
                healthBarMaxWidth = foregroundBar.localScale.x;
            }
        }

        // Register with TurnSystem
        TurnSystem.Instance.RegisterUnit(this);

        // Initialize health bar display
        UpdateHealthBar();
    }

    /// <summary>
    /// Updates the health bar's foreground based on current health.
    /// Scales X based on cached full-width scale.
    /// </summary>
    public void UpdateHealthBar()
    {
        if (foregroundBar == null) return;

        float healthPercent = Mathf.Clamp01((float)health / maxHealth);
        Vector3 scale = foregroundBar.localScale;
        scale.x = healthBarMaxWidth * healthPercent;
        foregroundBar.localScale = scale;
    }
}