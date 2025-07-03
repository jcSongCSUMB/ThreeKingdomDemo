using UnityEngine;

/// <summary>
/// IdleAnimator handles idle breathing animation for the Visual container.
/// It applies a sinusoidal vertical offset to create a gentle breathing effect.
/// Only modifies localPosition of the Visual child to avoid interfering with root movement.
/// Supports pausing and resuming.
/// </summary>
public class IdleAnimator : MonoBehaviour
{
    [Header("Idle Animation Settings")]
    [Tooltip("Amplitude of the breathing motion (units).")]
    public float amplitude = 0.03f;

    [Tooltip("Frequency of the breathing motion (cycles per second).")]
    public float frequency = 1f;

    // Tracks whether idle animation is active
    private bool isAnimating = true;

    // Cached original local position
    private Vector3 initialLocalPosition;

    // Local time accumulator to ensure smooth wave phase
    private float animationTime = 0f;

    private void Start()
    {
        // Store initial localPosition relative to parent (UnitRoot)
        initialLocalPosition = transform.localPosition;
    }

    private void Update()
    {
        if (!isAnimating) return;

        animationTime += Time.deltaTime;

        float offsetY = Mathf.Sin(animationTime * frequency * 2f * Mathf.PI) * amplitude;
        transform.localPosition = initialLocalPosition + new Vector3(0, offsetY, 0);
    }

    /// <summary>
    /// Pauses the idle breathing animation and resets localPosition.
    /// </summary>
    public void PauseIdle()
    {
        isAnimating = false;
        transform.localPosition = initialLocalPosition;
    }

    /// <summary>
    /// Resumes the idle breathing animation from phase zero.
    /// </summary>
    public void ResumeIdle()
    {
        isAnimating = true;
        animationTime = 0f;
    }
}