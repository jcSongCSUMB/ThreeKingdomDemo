using System.Collections;
using UnityEngine;

/// <summary>
/// Controls attack animation by performing a vertical jump with easing.
/// Designed for single-use trigger per attack.
/// Only modifies localPosition of the Visual child.
/// </summary>
public class AttackAnimator : MonoBehaviour
{
    [Header("Attack Animation Settings")]
    [Tooltip("Peak height of the jump in world units.")]
    public float jumpHeight = 1.0f;

    [Tooltip("Duration of the upward movement (seconds).")]
    public float upDuration = 1.0f;

    [Tooltip("Duration of the downward movement (seconds).")]
    public float downDuration = 0.5f;

    // Reference to optional IdleAnimator on the same object
    private IdleAnimator idleAnimator;

    // Cached original local position
    private Vector3 initialLocalPosition;

    private void Start()
    {
        // Cache starting local position relative to parent (i.e., UnitRoot)
        initialLocalPosition = transform.localPosition;

        // Try to find the IdleAnimator on the same GameObject
        idleAnimator = GetComponent<IdleAnimator>();
    }

    /// <summary>
    /// Plays the attack animation coroutine.
    /// Pauses idle animation during the attack.
    /// Uses ease-out on up, ease-in on down for better impact.
    /// Leaves half-second pause after landing for potential VFX.
    /// </summary>
    public IEnumerator PlayAttackAnimation()
    {
        if (idleAnimator != null)
        {
            idleAnimator.PauseIdle();
        }

        // Upward phase with ease-out
        float elapsed = 0f;
        while (elapsed < upDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / upDuration);
            float ease = 1f - Mathf.Pow(1f - t, 2);
            float offsetY = ease * jumpHeight;

            transform.localPosition = initialLocalPosition + new Vector3(0, offsetY, 0);
            yield return null;
        }

        // Downward phase with ease-in
        elapsed = 0f;
        while (elapsed < downDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / downDuration);
            float ease = t * t;
            float offsetY = jumpHeight * (1f - ease);

            transform.localPosition = initialLocalPosition + new Vector3(0, offsetY, 0);
            yield return null;
        }

        // Snap back to original position
        transform.localPosition = initialLocalPosition;

        // Wait to allow for impact VFX or feedback
        yield return new WaitForSeconds(0.5f);

        if (idleAnimator != null)
        {
            idleAnimator.ResumeIdle();
        }
    }
}