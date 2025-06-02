using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class actionManager : MonoBehaviour {
    public GameObject actionPanel;
    public GameObject unitPanel;
    public Button moveButton;
    public Button attackButton;

    public tileMap tmap;
    public AudioClip menuSFX;
    public AudioSource menuSound;

    void Start() {
        menuSound.clip = menuSFX;
        actionPanel.SetActive(false);
        
    }
    
    public void OpenActionPanel() {
        //selectedUnit = unit;
        actionPanel.SetActive(true);
        unitPanel.SetActive(true);
    }
    
    public void CloseActionPanel() {
        //selectedUnit = null;
        actionPanel.SetActive(false);
        unitPanel.SetActive(false);
    }

    public void OnMoveButtonClicked() {
        if (tmap.selectedUnit != null/* && selectedUnit.unitAP > 0*/) {
            //Debug.Log($"Move being pressed! isMoving: {tmap.isMovingUnit}");
            tmap.isMovingUnit = true;
            //selectedUnit.actionUse();
            menuSound.Play();
            CloseActionPanel();
        }
        else {
            Debug.Log("Not enough AP!!!");
            menuSound.Play();
            CloseActionPanel();
        }
    }

    public void OnAttackButtonClicked() {
        if (tmap.selectedUnit != null && tmap.selectedUnit.unitAP > 0) {
            tmap.isAttacking = true;
            //  selectedUnit.actionUse();
            menuSound.Play();
            CloseActionPanel();
        }

        else {
            Debug.Log("Not enough AP!!!");
            menuSound.Play();
            CloseActionPanel();
        }
    }

    public void OnEndTurnButtonClicked() {
        //if (tmap.bm.activeUnits > 0) {
        //    Debug.Log("Still active units on field!");
        //}
        //else {
        menuSound.Play();
        tmap.enemyTurn();
        tmap.bm.ResetEnemies();
            //Debug.Log("EndTurnButton Clicked!");
        //}
    }
}
