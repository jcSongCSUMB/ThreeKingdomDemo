using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwayImage : MonoBehaviour
{
    public float swaySpeed = 1f;
    public float swayAmount = 10f;

    private Vector3 startPosition;

    void Start() {
        startPosition = transform.localPosition;
    }

    void Update() {
        float newY = startPosition.y + Mathf.Sin(Time.time * swaySpeed) * swayAmount;
        transform.localPosition = new Vector3(startPosition.x, newY, startPosition.z);
    }
}

