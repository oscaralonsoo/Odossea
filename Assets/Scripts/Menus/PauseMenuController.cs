using UnityEngine;

public class PauseMenuController : MonoBehaviour
{
    private const int IDLE = 0;
    private const int POPUP = 1;
    private const int POPDOWN = 2;

    [Header("Animators")]
    [SerializeField] private Animator pauseAnimator;
    [SerializeField] private Animator optionsAnimator;

    [Header("Containers")]
    [SerializeField] private GameObject pauseContainer;
    [SerializeField] private GameObject optionsContainer;

    [Header("Main Menu")]
    [SerializeField] private string mainMenuScene; // Nombre de la escena del Main Menu

    private bool isPaused = false;
    private bool isTransitioning = false;

    private enum MenuState { Pause, Options }
    private MenuState currentMenu = MenuState.Pause;

    void Awake()
    {
        pauseAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        optionsAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;

        // Forzar estado inicial a IDLE para ambos animators
        pauseAnimator.SetInteger("State", IDLE);
        optionsAnimator.SetInteger("State", IDLE);
    }

    void Update()
    {
        if (isTransitioning)
            return;

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown("Cancel"))
        {
            if (currentMenu == MenuState.Options)
                BackFromOptions();
            else
                TogglePause();
        }
    }

    #region Pause

    public void TogglePause()
    {
        if (isPaused)
            ClosePause();
        else
            OpenPause();
    }

    public void OpenPause()
    {
        Time.timeScale = 0f;

        pauseAnimator.SetInteger("State", POPUP);

        isPaused = true;
        isTransitioning = true;
        currentMenu = MenuState.Pause;
    }

    public void ClosePause()
    {
        pauseAnimator.SetInteger("State", POPDOWN);
        isTransitioning = true;
    }

    // Callback de animación: PopUp de Pause terminado
    public void OnPausePopupFinished()
    {
        isTransitioning = false;
    }

    // Callback de animación: PopDown de Pause terminado
    public void OnPausePopdownFinished()
    {
        pauseAnimator.SetInteger("State", IDLE);

        if (currentMenu == MenuState.Options)
        {
            // Si abrimos Options, ahora POPUP de Options
            optionsAnimator.SetInteger("State", POPUP);
        }
        else
        {
            // Cerrando Pause normal
            Time.timeScale = 1f;
            isPaused = false;
            isTransitioning = false;
        }
    }

    // Botón "Back to Main Menu"
    public void ExitToMainMenu()
    {
        Time.timeScale = 1f;
        SceneTransitionManager.Instance.LoadScene(mainMenuScene);
    }

    #endregion

    #region Options

    public void OpenOptions()
    {
        if (isTransitioning) return;

        // Cambiar el estado antes de abrir Options
        currentMenu = MenuState.Options;

        // Primero hacer PopDown de Pause
        pauseAnimator.SetInteger("State", POPDOWN);
        isTransitioning = true;
    }

    public void CloseOptions()
    {
        if (isTransitioning) return;

        optionsAnimator.SetInteger("State", POPDOWN);
        isTransitioning = true;
    }

    public void BackFromOptions()
    {
        CloseOptions();
    }

    // Callback de animación: PopUp de Options terminado
    public void OnOptionsPopupFinished()
    {
        isTransitioning = false;
    }

    // Callback de animación: PopDown de Options terminado
    public void OnOptionsPopdownFinished()
    {
        optionsAnimator.SetInteger("State", IDLE);

        // Volver automáticamente a Pause
        currentMenu = MenuState.Pause;
        pauseAnimator.SetInteger("State", POPUP);
    }

    #endregion
}
