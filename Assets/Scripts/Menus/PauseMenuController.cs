using UnityEngine;

public class PauseMenuController : MonoBehaviour
{
    private const int IDLE = 0;
    private const int POPUP = 1;
    private const int POPDOWN = 2;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private string mainMenuScene;

    private bool isPaused = false;
    private bool isTransitioning = false;

    void Awake()
    {
        // El Animator ignora Time.timeScale
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
    }

    void Update()
    {
        if (isTransitioning)
            return;

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown("Cancel"))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (isPaused)
            ClosePause();
        else
            OpenPause();
    }

    public void OpenPause()
    {
        // ⛔ Pausa inmediata
        Time.timeScale = 0f;

        animator.SetInteger("State", POPUP);

        isPaused = true;
        isTransitioning = true;
    }

    public void ClosePause()
    {
        animator.SetInteger("State", POPDOWN);
        isTransitioning = true;
    }

    // 🎬 Final de POPUP
    public void OnPopupFinished()
    {
        isTransitioning = false;
    }

    // 🎬 Final de POPDOWN
    public void OnPopdownFinished()
    {
        animator.SetInteger("State", IDLE);

        Time.timeScale = 1f;
        isPaused = false;
        isTransitioning = false;
    }

    public void Back()
    {
        if (!isTransitioning)
            ClosePause();
    }

    public void Exit()
    {
        Time.timeScale = 1f;
        SceneTransitionManager.Instance.LoadScene(mainMenuScene);
    }
}
