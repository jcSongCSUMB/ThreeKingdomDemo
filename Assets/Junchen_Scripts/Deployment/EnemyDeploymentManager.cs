using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyDeploymentManager : MonoBehaviour
{
    public static EnemyDeploymentManager Instance;

    [Header("Enemy Unit Prefab (used for auto-deployment)")]
    public GameObject enemyUnitPrefab;

    [Header("Maximum number of enemy units to deploy")]
    [SerializeField] private int maxEnemyUnits = 5; // Default is 5, can be changed in Inspector

    private void Awake()
    {
        // Ensure singleton instance
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    // Automatically deploys enemy units at random enemy deploy tiles
    public void AutoDeployEnemies()
    {
        Debug.Log("[EnemyDeploy] Starting auto-deploy of enemy units...");

        // Get all available enemy deploy tiles (red tiles that are not yet blocked)
        List<OverlayTile> candidateTiles = MapManager.Instance.map.Values
            .Where(tile => tile.isEnemyDeployZone && !tile.isBlocked)
            .ToList();

        // Determine how many enemies to deploy (cannot exceed available tiles)
        int deployCount = Mathf.Min(maxEnemyUnits, candidateTiles.Count);

        for (int i = 0; i < deployCount; i++)
        {
            // Pick a random tile from the list
            int index = Random.Range(0, candidateTiles.Count);
            OverlayTile tile = candidateTiles[index];
            candidateTiles.RemoveAt(index); // Prevent duplicate placement

            // Instantiate enemy unit and place it at the tile
            GameObject enemy = Instantiate(enemyUnitPrefab);
            enemy.transform.position = tile.transform.position;
            enemy.transform.SetParent(GameObject.Find("Grid").transform);

            // Mark the tile as occupied
            tile.MarkAsBlocked();

            // Set the tile reference in CharacterInfo (if applicable)
            CharacterInfo info = enemy.GetComponent<CharacterInfo>();
            if (info != null)
            {
                info.standOnTile = tile;
            }

            // Set standOnTile in BaseUnit for selection logic
            BaseUnit unit = enemy.GetComponent<BaseUnit>();
            if (unit != null)
            {
                unit.standOnTile = tile;
            }

            Debug.Log($"[EnemyDeploy] Enemy unit deployed at tile: {tile.gridLocation}");
        }
    }
}