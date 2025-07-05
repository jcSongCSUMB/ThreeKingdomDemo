using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Responsible for executing all planned player unit actions in the PlayerExecuting phase.
/// </summary>
public static class PlayerExecutor
{
    // Define a constant defense boost value for Defend action
    private const int DEFENSE_BOOST_AMOUNT = 5;
    private const float MOVE_SPEED = 2f;
    private const float TILE_THRESHOLD = 0.01f;

    // Main entry point to execute all player units
    public static IEnumerator Execute(List<BaseUnit> allUnits)
    {
        Debug.Log("[TurnSystem] === Executing all Player units ===");

        foreach (BaseUnit unit in allUnits)
        {
            if (unit.teamType != UnitTeam.Player)
                continue;

            Debug.Log($"[TurnSystem] Executing unit: {unit.name} (plannedAction={unit.plannedAction})");

            // === Execute movement along plannedPath with stepping animation ===
            if (unit.plannedPath != null && unit.plannedPath.Count > 0)
            {
                yield return TurnSystem.Instance.StartCoroutine(MoveUnitAlongPath(unit));
            }
            else
            {
                Debug.Log($"[TurnSystem] {unit.name} has no movement planned.");
            }

            // === Execute plannedAction ===
            switch (unit.plannedAction)
            {
                case PlannedAction.None:
                    Debug.Log($"[TurnSystem] {unit.name} has no action planned.");
                    break;

                case PlannedAction.Defend:
                    unit.defensePower += DEFENSE_BOOST_AMOUNT;
                    unit.tempDefenseBonus = DEFENSE_BOOST_AMOUNT;
                    Debug.Log($"[TurnSystem] {unit.name} is defending. Defense boosted by +{DEFENSE_BOOST_AMOUNT}. New defensePower: {unit.defensePower}");
                    break;

                case PlannedAction.Attack:
                    if (unit.targetUnit != null)
                    {
                        // Play ATTACKER's attack animation
                        if (unit.visual != null)
                        {
                            var animator = unit.visual.GetComponent<AttackAnimator>();
                            if (animator != null)
                            {
                                Debug.Log($"[PlayerExecutor] Playing AttackAnimator on attacker: {unit.name}");
                                yield return TurnSystem.Instance.StartCoroutine(animator.PlayAttackAnimation());
                            }
                        }

                        // Apply damage calculation
                        int damage = Mathf.Max(unit.attackPower - unit.targetUnit.defensePower, 1);
                        unit.targetUnit.health -= damage;
                        Debug.Log($"[TurnSystem] {unit.name} attacks {unit.targetUnit.name} for {damage} damage. {unit.targetUnit.name} HP now {unit.targetUnit.health}.");

                        // Update the target's health bar
                        unit.targetUnit.UpdateHealthBar();

                        // Trigger camera shake at the same moment as damage feedback
                        CameraShake.Instance.Shake();

                        // NEW: Check if target died
                        if (unit.targetUnit.health <= 0)
                        {
                            Debug.Log($"[PlayerExecutor] Target {unit.targetUnit.name} has died. Starting death sequence.");
                            yield return TurnSystem.Instance.StartCoroutine(unit.targetUnit.DieAndRemove());
                        }
                    }
                    else
                    {
                        Debug.Log($"[TurnSystem] {unit.name} planned an attack but has no target.");
                    }
                    break;
            }

            // === Mark unit as finished ===
            unit.hasFinishedAction = true;

            // === Clear planning fields for next turn ===
            unit.plannedPath.Clear();
            unit.plannedAction = PlannedAction.None;
            unit.targetUnit = null;
        }

        Debug.Log("[TurnSystem] === PlayerExecuting phase complete ===");
    }

    // Move unit along its planned path
    private static IEnumerator MoveUnitAlongPath(BaseUnit unit)
    {
        foreach (OverlayTile tile in unit.plannedPath)
        {
            Vector3 targetPos = tile.transform.position;

            while (Vector2.Distance(unit.transform.position, targetPos) > TILE_THRESHOLD)
            {
                unit.transform.position = Vector3.MoveTowards(unit.transform.position, targetPos, MOVE_SPEED * Time.deltaTime);
                yield return null;
            }

            unit.transform.position = targetPos;
            unit.standOnTile = tile;

            Debug.Log($"[TurnSystem] {unit.name} moved to tile {tile.grid2DLocation}");
        }
    }
}