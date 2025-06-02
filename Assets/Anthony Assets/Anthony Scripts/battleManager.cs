using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class battleManager : MonoBehaviour {
    public int activeUnits = 0;
    private int friendlyDefault = 0;
    private bool turnComplete = false;
    public int morale = 100;
    public TextMeshProUGUI unitCounter;
    public TextMeshProUGUI healthText, dmgText, apText;
    public tileMap tmap;

    public List<unitScript> enemyUnits = new List<unitScript>();
    public List<unitScript> friendlyUnits = new List<unitScript>();
    
    
    // Start is called before the first frame update
    void Start() {
        unitCounter.text = $"Units: {activeUnits} - {tmap.getUnitAP()}";

        foreach (var tile in FindObjectsOfType<tileScript>()) {
            if (tile.hasUnit && !tile.unit.friendly) {
                enemyUnits.Add(tile.unit);
            }

            if (tile.hasUnit && tile.unit.friendly) {
                friendlyUnits.Add(tile.unit);
                friendlyDefault+=1;
            }
        }
    }

    // Update is called once per frame
    void Update() {
        SetText();
        if (turnComplete == true) {
            // switch to enemy
            // otherwise, can still use up time on their turn even with inactive units
        }
    }

    void SetText() {
        //
        if (tmap.selectedUnit != null) {
            unitScript unit = tmap.getUnitAP();
            if (unit) {
                unitCounter.text = $"{activeUnits}";
                healthText.text = $"{unit.unitHealth}";
                dmgText.text = $"{unit.unitDMG}";
                apText.text = $"{unit.unitAP}";
            }
            else {
                unitCounter.text = $"{activeUnits}";
                healthText.text = $"";
                dmgText.text = $"";
                apText.text = $"";
            }
        }
        else {
            unitCounter.text = $"{activeUnits}";
        }
    }

    public void decreaseUnits() {
        activeUnits--;
    }

    public void decreaseMorale(int amt) {
        morale -= amt;
    }

    public void ResetEnemies() {
        activeUnits = friendlyDefault;
        foreach(var enemy in enemyUnits) {
            enemy.ResetAP();
        }

        foreach (var player in friendlyUnits) {
            player.ResetAP();
        }
    }
}
