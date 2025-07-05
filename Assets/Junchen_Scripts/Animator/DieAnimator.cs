using System.Collections;
using UnityEngine;

// DieAnimator handles the death animation for the Visual child.
// It smoothly tilts, scales down, and fades out over time.
public class DieAnimator : MonoBehaviour
{
    [Header("Death Animation Settings")]
    [Tooltip("Tilt angle in degrees.")]
    public float tiltAngle = 30f;

    [Tooltip("Duration of the death animation in seconds.")]
    public float duration = 1.5f;

    // Cached original values
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;
    private Vector3 initialLocalScale;
    private SpriteRenderer spriteRenderer;

    private void Start()
    {
        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;
        initialLocalScale = transform.localScale;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // PlayDeathAnimation performs tilt, scale-down, and fade-out
    public IEnumerator PlayDeathAnimation()
    {
        Debug.Log($"[DieAnimator] Playing death animation on {name}");

        float elapsed = 0f;

        // Cache starting alpha
        float startAlpha = 1f;
        if (spriteRenderer != null)
        {
            startAlpha = spriteRenderer.color.a;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Tilt
            float angle = Mathf.Lerp(0, tiltAngle, t);
            transform.localRotation = Quaternion.Euler(0, 0, angle);

            // Scale down
            float scaleFactor = Mathf.Lerp(1f, 0.5f, t);
            transform.localScale = initialLocalScale * scaleFactor;

            // Fade out
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = Mathf.Lerp(startAlpha, 0f, t);
                spriteRenderer.color = color;
            }

            yield return null;
        }

        // Ensure final state
        transform.localRotation = Quaternion.Euler(0, 0, tiltAngle);
        transform.localScale = initialLocalScale * 0.5f;
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 0f;
            spriteRenderer.color = color;
        }

        Debug.Log($"[DieAnimator] Death animation complete on {name}");
    }
}