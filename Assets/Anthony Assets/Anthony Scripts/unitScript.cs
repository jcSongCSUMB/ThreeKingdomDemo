using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class unitScript : MonoBehaviour {
    public int unitHealth = 50;
    private int maxHealth = 0;
    public int unitAP = 2;
    public float unitSpeed = 3f;
    public int unitDMG = 10;
    public bool isActive = true;
    public bool friendly = true;
    public string unitName = "";
    
    private battleManager bm;
    private ProgressBar healthBar;
    //GameObject target;
    //^^^ to be used later for battle
    
    // Start is called before the first frame update
    void Start() {
        maxHealth = unitHealth;
        bm = FindObjectOfType<battleManager>();
        Debug.Log($"Unit has {unitAP} AP.");
    }

    // Update is called once per frame
    void Update() {
        
    }

    public void SetHealth() {
        if (unitHealth <= 0) {
            healthBar.SetEnabled(false);
        }
        else {
            healthBar.value = unitHealth / maxHealth;   
        }
    }

    public void SetupHealthBar(Canvas canvas, Camera camera) {
        //healthBar.
    }

    public void ResetAP() {
        unitAP = 2;
    }

    public void actionUse() {
        
        if (unitAP > 0) {
            Debug.Log("Action Used!");
            unitAP--;
        }

        // only for friendly units will the activeUnits be decremented
        if (unitAP <= 0 && friendly) {
            if (bm != null) {
                bm.decreaseUnits();
                Debug.Log("No more AP available.");
            }
        }
    }
}
