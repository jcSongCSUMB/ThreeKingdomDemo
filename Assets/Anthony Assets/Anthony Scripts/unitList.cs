using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class unitList : MonoBehaviour {
    public GameObject warrior, archer, spearman;
    public GameObject eWarrior, eArcher, eSpearman;
    // Start is called before the first frame update
    void Start() {
        
    }

    // Update is called once per frame
    void Update() {
        
    }

    public GameObject playerType(string type) {
        if (type == "warrior") {
            return warrior;
        } else if (type == "archer") {
            return archer;
        } else if (type == "spearman") {
            return spearman;
        } else if (type == "eWarrior") {
            return eWarrior;
        } else if (type == "eArcher") {
            return eArcher;
        } else if (type == "eSpearman") {
            return eSpearman;
        }
        
        return warrior;
    }
}
