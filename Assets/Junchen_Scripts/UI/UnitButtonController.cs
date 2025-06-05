using UnityEngine;
using UnityEngine.UI;

public class UnitButtonController : MonoBehaviour
{
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void DisableButton()
    {
        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.5f;
        }

        Debug.Log($"[CanvasGroup] Button {gameObject.name} disabled.");
    }

    // Restores button to initial state
    public void ResetButton()
    {
        if (canvasGroup != null)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }

        Debug.Log($"[CanvasGroup] Button {gameObject.name} reset.");
    }
}