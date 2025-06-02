using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Color = UnityEngine.Color;
using Random = System.Random;

// Notes
// We want to get a raycast click onto the tile we're selecting
// to select the tile, but after a tile is selected, temporarily 
// disable the raycast selection (through if statement) to allow for
// click on menu to then allow the movement to occur. Have two functions
// that allow one to select the tile, then the other to move the unit 
// selected.

// TODO:
// fix issue with start of moving characters or attacking. (fixed after first move?)
// incorporate area detection 

public class tileMap : MonoBehaviour {
    [Header("Tilemap Variables")]
    public Tilemap tilemap;
    public tileScript tile, lastTile;
    public SpriteRenderer spr = null;
    public unitScript selectedUnit, lastUnit;
    public bool isMovingUnit = false;
    public bool isAttacking = false;
    
    
    [Header("Tile Variables")]
    public Tile groundTile;
    public List<tileScript> validMoveTiles = new List<tileScript>();
    public List<Tuple<tileScript, Vector3Int>> tileTuples = new List<Tuple<tileScript, Vector3Int>>();

    
    [Header("Managers")]
    public actionManager acMan;
    public battleManager bm;
    public unitList unitOptions;
    public soundManager soundMan;

    
    // Start is called before the first frame update
    void Start() {
        acMan = FindObjectOfType<actionManager>();
        SetGameObjectsToTiles();
        SpawnEnemies();
        //CreateGrid(tilemap);
    }

    // Update is called once per frame
    void Update() {
        // for selection, we want to check for valid tile for selection.
        // this gives us a selected unit if a battle unit is present on the tile
        //moveCam();
        if (Input.GetMouseButtonDown(0)) {
            if (tile != null) {
                lastTile = tile;
                tile = GetTileHit().collider.GetComponent<tileScript>();
                //spr = GetTileHit().collider.GetComponent<SpriteRenderer>();
                BattleLogic();
                
            }
            else {
                tile = GetTileHit().collider.GetComponent<tileScript>();
                spr = GetTileHit().collider.GetComponent<SpriteRenderer>();
            }
            
            

            if (selectedUnit != null) {
                lastUnit = selectedUnit;
            }
            CheckSprite(tile);
        }
        

        if (tile != null) {
            Color newColor = spr.color;
            newColor.a = 0.5f + Mathf.PingPong(Time.time * 0.5f, 0.5f);
            spr.color = newColor;
        }
    }

    // finds all gameobjects, will locate based on space in world
    // and assign the gameObject tile to the tilemap to make it easier
    // to check surroundings for battle logic and movement.
    void SetGameObjectsToTiles() {
        int count = 0;
        foreach (var _tile in FindObjectsOfType<tileScript>()) {
            Vector3 tileWorldPos = _tile.transform.position;
            Vector3Int tileCellPos = tilemap.WorldToCell(tileWorldPos);
            Tuple<tileScript, Vector3Int> t = new Tuple<tileScript, Vector3Int>(_tile, tileCellPos);
            tileTuples.Add(t);
            count++;
        }

        // checking if it finds all tiles.
        Debug.Log($"Number of Tiles found: {count}");
    }

    void FindEnemySpawns() {
        
    }
    
    public void SpawnEnemies() {
        int activeUnits = 0;
        foreach (var tile in tileTuples) {
            if (tile.Item1.playerSpawn == false && tile.Item1.spawnType != "None") {
                GameObject unit = Instantiate(unitOptions.playerType(tile.Item1.spawnType));
                unitScript unitInfo = unit.GetComponent<unitScript>();
                unitInfo.unitName = tile.Item1.spawnType;
                unitInfo.friendly = false;
                unit.transform.position = tile.Item1.transform.position + new Vector3(0, 0.15f, 0);
                tile.Item1.hasUnit = true;
                tile.Item1.unit = unit.GetComponent<unitScript>();
            }

            if (tile.Item1.playerSpawn == true && tile.Item1.spawnType != "None") {
                activeUnits++;
                GameObject unit = Instantiate(unitOptions.playerType(tile.Item1.spawnType));
                unitScript unitInfo = unit.GetComponent<unitScript>();
                unitInfo.unitName = tile.Item1.spawnType;
                unitInfo.friendly = true;
                unit.transform.position = tile.Item1.transform.position + new Vector3(0, 0.15f, 0);
                tile.Item1.hasUnit = true;
                tile.Item1.unit = unit.GetComponent<unitScript>();
            }
        }

        bm.activeUnits = activeUnits;
    }

    void moveCam() {
        Vector3 mPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mPos.z = Camera.main.transform.position.z;
        Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, mPos, 0.001f);
    }

    void SprColorReset() {
        Color orig = spr.color;
        orig.a = 1;
        spr.color = orig;
    }

    public unitScript getUnitAP() {
        if (selectedUnit == null) {
            Debug.LogWarning("No unit selected.");
            return null;
        }
        
        unitScript us = selectedUnit.GetComponent<unitScript>();
        if (us != null) {
            return us;    
        }
        else {
            Debug.LogError("Unit script not found.");
            return null;
        }
    }
    
    public RaycastHit2D GetTileHit() {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mPos2D = new Vector2(mousePos.x, mousePos.y);
        RaycastHit2D hit = Physics2D.Raycast(mPos2D, Vector2.zero);
        
        if (hit == null) {
            acMan.CloseActionPanel();
            Debug.LogWarning("Clicked non-tile.");
        }
        
        return hit;
    }

    void BattleLogic() {
        // battle logic
        if (isMovingUnit) {
            MoveUnitToTile(selectedUnit, lastTile, tile);
            isMovingUnit = false;
        } else if (isAttacking) {
            AttackUnitOnTile(selectedUnit, tile);
            isAttacking = false;
        } else {
            //Debug.Log("BL else reached.");
        }
    }

    void ResetValidMoveTiles() {
        foreach (var tile in validMoveTiles) {
            SpriteRenderer sprRen = tile.GetComponent<SpriteRenderer>();
            if (sprRen != null) {
                sprRen.color = Color.white;
            }
        }
        validMoveTiles.Clear();
    }

    void CheckSprite(tileScript tile) {
        // selected tile
        if (tile != null) {

            // checking for unit on tile
            if (tile.hasUnit && tile.unit.friendly == true) {
                soundMan.playSFX("click");
                Debug.Log("Unit selected!");
                selectedUnit = tile.unit;
                acMan.OpenActionPanel();

                validMoveTiles = GetSurroundingTiles(tile.gameObject);

                foreach (var valid in validMoveTiles) {
                    SpriteRenderer sprRen = valid.GetComponent<SpriteRenderer>();
                    if (sprRen != null) {
                        sprRen.color = Color.yellow;
                    }
                }

            } else {
                Debug.Log("Unit de-selected.");
                lastUnit = selectedUnit;
                selectedUnit = null;
                acMan.CloseActionPanel();
                ResetValidMoveTiles();
            }

        // nothing selected
        } else {
            Debug.Log("No tile selected!");
            SprColorReset();
        }
    }

    // to avoid null object, use tileScript as target and check for unit or apply to unit
    // on the tile. 
    void AttackUnitOnTile(unitScript unit, tileScript target) {
        target.UnitHealthCheck();
        if (!validMoveTiles.Contains(target) && !target.hasUnit) {
            Debug.LogWarning("Invalid attack!");
            return;
        }
        if (unit.unitAP > 0) {
            target.unit.unitHealth -= unit.unitDMG;
            //target.unit.SetHealth();
            unit.actionUse();
            isAttacking = false;
            selectedUnit = null;
            
            soundMan.playSFX(unit.unitName);
            ResetValidMoveTiles();
        }

        else {
            //Debug.Log("Unit has no AP.");
        }
    }

    void MoveUnitToTile(unitScript unit, tileScript prevTile, tileScript targetTile) {
        //unitScript us = unit.GetComponent<unitScript>();
        //Debug.Log($"Unit: {unit}, prevTile: {prevTile.transform.position}, target: {targetTile.transform.position}");
        if (!validMoveTiles.Contains(targetTile) && unit.friendly) {
            Debug.LogWarning("Invalid move!");
            return;
        }
        
        if (unit.unitAP > 0) {
            unit.transform.position = new Vector3(targetTile.transform.position.x, targetTile.transform.position.y + 0.15f, 0);
            prevTile.ClearUnit();
            targetTile.AssignUnit(unit);
            moraleCheck(targetTile.gameObject); // morale check for target tile to move to.
            unit.actionUse();
            
            ResetValidMoveTiles();
            soundMan.playSFX("move");
            Debug.Log($"Go through if?");
        }

        else {
            Debug.Log("Unit has no AP.");
        }
        
        //Debug.Log("Unit moved to new tile.");
    }

    public void CreateGrid(Tilemap tmap) {
        for (int i = 10; i < 15; i++) {
            for (int j = 2; j > -3; j--) {
                Vector3Int pos = new Vector3Int(i, j, 0);
                tmap.SetTile(pos, groundTile);
            }
        }
    }

    unitScript FindNearestPlayer(unitScript enemy) {
        unitScript nearestPlayer = null;
        float shortestDistance = 100f;

        foreach (var unit in FindObjectsOfType<unitScript>()) {
            if (unit.friendly && unit.isActive && unit != enemy) {
                float distance = Vector3.Distance(enemy.transform.position, unit.transform.position);

                if (distance < shortestDistance) {
                    shortestDistance = distance;
                    nearestPlayer = unit;
                }
            }
        }

        return nearestPlayer;
    }

    
    public void RemoveUnit(unitScript unit) {
        if (unit == null) return;

        bm.enemyUnits.Remove(unit);
        bm.activeUnits--;
    }
    


    tileScript enemyMoveUnit(unitScript enemyUnit, unitScript targetUnit) {
        Vector3Int eTilePos = tilemap.WorldToCell(enemyUnit.transform.position);
        tileScript eTile = tileTuples.Find(tuple => tuple.Item2 == eTilePos).Item1;
        
        List<tileScript> surroundingTiles = GetSurroundingTiles(eTile.gameObject);
        tileScript bestMove = null;
        float shortestDistance = 1000f;
        
        Grid grid = tilemap.GetComponentInParent<Grid>();
        Vector3Int enemyGridPos = grid.WorldToCell(enemyUnit.transform.position);
       
        foreach (var tile in surroundingTiles) {
            if (tile.hasUnit) {
                return tile;
            }
            
            float distance = Vector3.Distance(tile.transform.position, targetUnit.transform.position);
            
            if (distance < shortestDistance) {
                shortestDistance = distance;
                bestMove = tile;
            }
        }
        Debug.Log($"Best Move: {bestMove.transform.position}, Tile: {bestMove}");

        return bestMove;
    }
    
    public void enemyTurn() {
        foreach (var enemy in bm.enemyUnits.ToList()) { // Use a copy of the list to avoid modification issues
            if (enemy == null) continue; // Skip if the enemy unit has been destroyed

            while (enemy.unitAP > 0 && enemy.isActive) {
                Vector3Int eTilePos = tilemap.WorldToCell(enemy.transform.position);
                tileScript eTile = tileTuples.Find(tuple => tuple.Item2 == eTilePos)?.Item1;

                if (eTile == null) break; // Skip if the tile is null

                unitScript target = FindNearestPlayer(enemy);
                if (target != null) {
                    tileScript targetTile = enemyMoveUnit(enemy, target);

                    if (targetTile != null) {
                        if (targetTile.hasUnit) {
                            Debug.Log($"Enemy is attacking!");
                            AttackUnitOnTile(enemy, targetTile);
                            soundMan.playSFX("warrior");
                        } else {
                            Debug.Log($"Enemy is moving. From: {eTile.transform.position} To: {targetTile.transform.position}");
                            MoveUnitToTile(enemy, eTile, targetTile);
                            soundMan.playSFX("move");
                        }
                    }
                } else {
                    Debug.LogWarning("No Target found for enemy.");
                    break;
                }
            }
        }
    }

    
    public List<tileScript> GetSurroundingTiles(GameObject centerTile) {
        // Ensure the Tilemap and Grid are valid
        if (centerTile == null || tilemap == null) {
            Debug.LogWarning("Center tile or tilemap is null.");
            return new List<tileScript>();
        }

        Grid grid = tilemap.GetComponentInParent<Grid>();
        Vector3Int gridPos = grid.WorldToCell(centerTile.transform.position);
        gridPos -= new Vector3Int(-1, -1, 0);
        //Debug.Log("WORLD TO CELL: " + gridPos);
        //Debug.Log($"OBJ: {tilemap.GetInstantiatedObject(gridPos).name}");

        List<tileScript> surroundingTiles = new List<tileScript>();
        Vector3Int[] neighborOffsets = new Vector3Int[] {
            new Vector3Int(0, 1, 0), // Left
            new Vector3Int(0, -1, 0),  // Right
            new Vector3Int(-1, 0, 0), // Down
            new Vector3Int(1, 0, 0),  // Up
            new Vector3Int(-1, 1, 0), // Bottom-left
            new Vector3Int(1, 1, 0),  // Top-left
            new Vector3Int(-1, -1, 0),  // Bottom-right
            new Vector3Int(1, -1, 0)   // Top-right
        };
        
        
        foreach (var offset in neighborOffsets) {
            
            Vector3Int neighborGridPos = gridPos + offset;

            // Find the matching tuple in tileTuples
            var match = tileTuples.Find(t => t.Item2 == neighborGridPos);
            if (match != null) {
                tileScript neighborTile = match.Item1;
                if (neighborTile != null) {
                    surroundingTiles.Add(neighborTile);
                }
            }
        }

        return surroundingTiles;
    }
    
    public int getSurroundingUnits(GameObject centerTile) {
        List<tileScript> surroundingTiles = GetSurroundingTiles(centerTile);
        //Debug.Log("Reaches function");
        int unitCount = 0;

        foreach (var tile in surroundingTiles) {
            Debug.Log($"Tile Position: {tile.transform.position} | HasUnit: {tile.hasUnit} | Tiles: {surroundingTiles.Count} Tile: {tile}");
            if (tile.hasUnit) {
                unitCount++;
            }
        }

        return unitCount;
    }
    
    void moraleCheck(GameObject selectedTile) {
        int surroundingUnits = getSurroundingUnits(selectedTile);
        if (surroundingUnits > 1) {
            bm.decreaseMorale(10);
            Debug.Log($"Morale decreased by 10 due to surrounding units.\nMorale: {bm.morale}");
        }

        else {
            Debug.Log($"Moral: {bm.morale} | Units: {surroundingUnits}");
        }
    }
}
