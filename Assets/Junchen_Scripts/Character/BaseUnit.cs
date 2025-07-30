using System.Collections;
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
    public GameObject visual;

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
    private float healthBarMaxWidth = 1f;

    private void Start()
    {
        // Instantiate and attach health bar
        if (healthBarPrefab != null)
        {
            healthBarInstance = Instantiate(healthBarPrefab, transform);
            healthBarInstance.transform.localPosition = healthBarOffset;

            // Cache the foreground bar transform
            foregroundBar = healthBarInstance.transform.Find("ForegroundBar");

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

    // Updates the health bar based on current health
    public void UpdateHealthBar()
    {
        if (foregroundBar == null) return;

        float healthPercent = Mathf.Clamp01((float)health / maxHealth);
        Vector3 scale = foregroundBar.localScale;
        scale.x = healthBarMaxWidth * healthPercent;
        foregroundBar.localScale = scale;
    }

    // Coroutine to handle unit death
    public IEnumerator DieAndRemove()
    {
        Debug.Log($"[BaseUnit] {name} starting death sequence.");

        // fully release the tile this unit occupies
        if (standOnTile != null)
        {
            standOnTile.UnmarkTurnBlocked();   // clear full‑turn block
            standOnTile.UnmarkTempBlocked();   // clear any temp block + visual

            // UPDATED 2025-07-30: cut the link to prevent re-marking in next phase
            standOnTile = null;
        }

        // Play death animation if assigned
        if (visual != null)
        {
            DieAnimator dieAnimator = visual.GetComponent<DieAnimator>();
            if (dieAnimator != null)
            {
                yield return StartCoroutine(dieAnimator.PlayDeathAnimation());
            }
        }

        // Remove from TurnSystem list
        TurnSystem.Instance.RemoveUnit(this);

        // Finally destroy the GameObject
        Destroy(gameObject);
    }
}