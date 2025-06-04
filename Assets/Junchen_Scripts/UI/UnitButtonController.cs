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
}