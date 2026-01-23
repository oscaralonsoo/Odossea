using UnityEngine;
using UnityEngine.UI; // Necesario para trabajar con Button

public class MenuController : MonoBehaviour
{
    [Header("Levels To Load")]
    public string newGameLevel;
    public string levelToLoad;

    [Header("UI Elements")]
    [SerializeField] private Button loadGameButton = null; // Referencia al botón LoadGame

    private void Start()
    {
        // Desactiva el botón si no hay partida guardada
        if (!PlayerPrefs.HasKey("SavedLevel") && loadGameButton != null)
        {
            loadGameButton.interactable = false;
        }
    }

    public void NewGameYes()
    {
        SceneTransitionManager.Instance.LoadScene(newGameLevel);
    }

    public void LoadGameYes()
    {
        if (PlayerPrefs.HasKey("SavedLevel"))
        {
            levelToLoad = PlayerPrefs.GetString("SavedLevel");
            SceneTransitionManager.Instance.LoadScene(levelToLoad);
        }
        // Ya no necesitamos el diálogo
    }

    public void ExitButton()
    {
        Application.Quit();
    }
}
