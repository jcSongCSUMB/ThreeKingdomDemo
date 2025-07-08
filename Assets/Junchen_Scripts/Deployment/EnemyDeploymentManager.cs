using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyDeploymentManager : MonoBehaviour
{
    public static EnemyDeploymentManager Instance;

    [Header("Enemy Unit Prefab (used for auto-deployment)")]
    public GameObject enemyUnitPrefab;

    [Header("Maximum number of enemy units to deploy")]
    [SerializeField] private int maxEnemyUnits = 5;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void AutoDeployEnemies()
    {
        Debug.Log("[EnemyDeploy] Starting auto-deploy of enemy units...");

        List<OverlayTile> candidateTiles = MapManager.Instance.map.Values
            .Where(tile => tile.isEnemyDeployZone && !tile.isBlocked)
            .ToList();

        int deployCount = Mathf.Min(maxEnemyUnits, candidateTiles.Count);

        for (int i = 0; i < deployCount; i++)
        {
            int index = Random.Range(0, candidateTiles.Count);
            OverlayTile tile = candidateTiles[index];
            candidateTiles.RemoveAt(index);

            GameObject enemy = Instantiate(enemyUnitPrefab);
            enemy.transform.position = tile.transform.position;
            enemy.transform.SetParent(GameObject.Find("Grid").transform);

            // Mark as turn-blocked only (not permanently blocked)
            tile.MarkAsTurnBlocked();

            CharacterInfo info = enemy.GetComponent<CharacterInfo>();
            if (info != null)
            {
                info.standOnTile = tile;
            }

            BaseUnit unit = enemy.GetComponent<BaseUnit>();
            if (unit != null)
            {
                unit.standOnTile = tile;
            }

            // Register deployed enemy in UnitDeployManager
            UnitDeployManager.Instance.RegisterDeployedUnit(enemy);

            Debug.Log($"[EnemyDeploy] Enemy unit deployed at tile: {tile.gridLocation}");
        }
    }
}