using UnityEngine;

public class UIPanel : MonoBehaviour
{
    public Animator animator;
    CanvasGroup canvasGroup;

    const int IDLE = 0;
    const int POPUP = 1;
    const int POPDOWN = 2;

    void Awake()
    {
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        animator.SetInteger("State", IDLE);

        canvasGroup = GetComponent<CanvasGroup>();
        SetInteractable(false);
    }

    public void Show()
    {
        SetInteractable(true);         
        animator.SetInteger("State", POPUP);
    }

    public void Hide()
    {
        SetInteractable(false);       
        animator.SetInteger("State", POPDOWN);
    }

    void SetInteractable(bool value)
    {
        canvasGroup.interactable = value;
        canvasGroup.blocksRaycasts = value;
    }

    // Animation Events
    public void OnPopupFinished()
    {
        animator.SetInteger("State", IDLE);
    }

    public void OnPopdownFinished()
    {
        animator.SetInteger("State", IDLE);
    }
}
