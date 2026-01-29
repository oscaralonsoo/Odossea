using UnityEngine;
using UnityEngine.SceneManagement;

public class EndLevel : MonoBehaviour
{

    public string sceneName;
    private void OnTriggerEnter2D(Collider2D collision)
    {

        FindFirstObjectByType<SceneTransitionManager>().LoadScene(sceneName);
    }
}
