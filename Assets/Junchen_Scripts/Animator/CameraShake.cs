using System.Collections;
using UnityEngine;

// CameraShake adds temporary screen shake for impact feedback.
// Designed as a singleton for easy global access.
public class CameraShake : MonoBehaviour
{
    // Singleton instance
    public static CameraShake Instance { get; private set; }

    [Header("Default Shake Settings")]
    [Tooltip("Default duration of the shake in seconds.")]
    public float defaultDuration = 0.1f;

    [Tooltip("Default magnitude of the shake offset.")]
    public float defaultMagnitude = 0.1f;

    // Store original position to restore after shaking
    private Vector3 originalPosition;

    private void Awake()
    {
        // Enforce singleton pattern
        if (Instance == null)
        {
            Instance = this;
            originalPosition = transform.localPosition;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Trigger the camera shake effect with default parameters
    public void Shake()
    {
        StartCoroutine(PerformShake(defaultDuration, defaultMagnitude));
    }

    // Trigger the camera shake effect with custom parameters
    public void Shake(float duration, float magnitude)
    {
        StartCoroutine(PerformShake(duration, magnitude));
    }

    // Coroutine that performs the shaking by applying random offsets
    private IEnumerator PerformShake(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float offsetX = Random.Range(-1f, 1f) * magnitude;
            float offsetY = Random.Range(-1f, 1f) * magnitude;

            transform.localPosition = originalPosition + new Vector3(offsetX, offsetY, 0);

            yield return null;
        }

        // Restore original position
        transform.localPosition = originalPosition;
    }
}
