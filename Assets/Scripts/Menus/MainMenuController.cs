using UnityEngine;

public class MenuController : MonoBehaviour
{
    [Header("Levels To Load")]
    public string newGameLevel;
    public string levelToLoad;

    [SerializeField] private GameObject noSavedGameDialog = null;

    public void NewGameYes()
    {
        Debug.Log("CLICK EN NEW GAME YES");
        SceneTransitionManager.Instance.LoadScene(newGameLevel);
    }

    public void LoadGameYes()
    {
        if (PlayerPrefs.HasKey("SavedLevel"))
        {
            levelToLoad = PlayerPrefs.GetString("SavedLevel");
            SceneTransitionManager.Instance.LoadScene(levelToLoad);
        }
        else
        {
            noSavedGameDialog.SetActive(true);
        }
    }

    public void ExitButton()
    {
        Application.Quit();
    }
}
