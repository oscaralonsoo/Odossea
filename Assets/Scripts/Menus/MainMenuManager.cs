using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    public UIPanel mainPanel;
    public UIPanel optionsPanel;

    UIPanel current;

    void Start()
    {
        mainPanel.Show();
        current = mainPanel;
    }

    public void ShowOptions()
    {
        mainPanel.Hide();
        optionsPanel.Show();
        current = optionsPanel;
    }

    public void BackToMain()
    {
        optionsPanel.Hide();
        mainPanel.Show();
        current = mainPanel;
    }

    public void Play(string scene)
    {
        SceneTransitionManager.Instance.LoadScene(scene);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
