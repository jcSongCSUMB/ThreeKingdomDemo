using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class tileScript : MonoBehaviour {
    public unitScript unit;
    public bool hasUnit;
    public bool playerSpawn = true;
    public string spawnType = "None";
    
    // Start is called before the first frame update
    void Start() {
        
    }

    // Update is called once per frame
    void Update() {
        
    }

    public void UnitHealthCheck() {
        if (unit == null) {
            Debug.LogWarning("Unit is null.");
            return;
        }
        
        if (unit.unitHealth <= 0) {
            GameObject GO = unit.gameObject;
            hasUnit = false;
            unit = null;
            Destroy(GO);
        }
    }

    public void AssignUnit(unitScript newUnit) {
        unit = newUnit;
        hasUnit = true;
    }

    public void ClearUnit() {
        unit = null;
        hasUnit = false;
    }
}
