using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    // PANELS
    public UIPanel mainPanel;
    public UIPanel optionsPanel;
    public UIPanel soundPanel;
    public UIPanel graphicsPanel;

    UIPanel current;

    void Start()
    {
        mainPanel.Show();
        current = mainPanel;
    }

    //  MAIN MENU 
    public void Play(string scene)
    {
        SceneTransitionManager.Instance.LoadScene(scene);
    }

    public void ShowOptions()
    {
        if (current != null)
            current.Hide();

        optionsPanel.Show();
        current = optionsPanel;
    }

    public void BackToMain()
    {
        if (current != null)
            current.Hide();

        mainPanel.Show();
        current = mainPanel;
    }

    public void Quit()
    {
        Application.Quit();
    }

    // SOUND / GRAPHICS 
    public void ShowSound()
    {
        if (current != null)
            current.Hide();

        soundPanel.Show();
        current = soundPanel;
    }

    public void ShowGraphics()
    {
        if (current != null)
            current.Hide();

        graphicsPanel.Show();
        current = graphicsPanel;
    }

    public void BackToOptions()
    {
        if (current != null)
            current.Hide();

        optionsPanel.Show();
        current = optionsPanel;
    }
}
