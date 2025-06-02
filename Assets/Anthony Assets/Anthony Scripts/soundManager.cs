using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class soundManager : MonoBehaviour {
    public AudioClip 
        warriorSlash, 
        bowmanShoot, 
        spearmanSlash,
        unitMove,
        unitDown,
        battleMusic,
        menuClick;

    public AudioSource SFX, BGM;
    
    // Start is called before the first frame update
    void Start() {
        BGM.clip = battleMusic;
        BGM.volume = 0.20f;
        BGM.loop = true;
        BGM.Play();
    }

    // Update is called once per frame
    void Update() {
        
    }

    public void playSFX(string _SFX) {
        if (_SFX == "warrior") {
            SFX.clip = warriorSlash;
            SFX.volume = 0.5f;
        } else if (_SFX == "archer") {
            SFX.clip = bowmanShoot;
            SFX.volume = 0.5f;
        } else if (_SFX == "spearman") {
            SFX.clip = spearmanSlash;
            SFX.volume = 0.5f;
        } else if (_SFX == "unitDown") {
            SFX.clip = unitDown;
            SFX.volume = 0.5f;
        } else if (_SFX == "move") {
            SFX.clip = unitMove;
            SFX.volume = 0.5f;
        } else if (_SFX == "click") {
            SFX.clip = menuClick;
            SFX.volume = 0.5f;
        }
        else {
            SFX.clip = menuClick;
            SFX.volume = 0.5f;
        }
        
        SFX.Play();
    }
}
