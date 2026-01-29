using UnityEngine;

public class UIPanel : MonoBehaviour
{
    public Animator animator;

    const int IDLE = 0;
    const int POPUP = 1;
    const int POPDOWN = 2;

    void Awake()
    {
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        animator.SetInteger("State", IDLE);
    }
    
    public void Show()
    {
        animator.SetInteger("State", POPUP);
    }

    public void Hide()
    {
        animator.SetInteger("State", POPDOWN);

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
