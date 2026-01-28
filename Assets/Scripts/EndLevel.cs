using UnityEngine;
using UnityEngine.SceneManagement;

public class EndLevel : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.gameObject.CompareTag("Bone")) return;

        string currentSceneName = SceneManager.GetActiveScene().name;

        string numberPart = currentSceneName.Replace("Level", "");

        if (int.TryParse(numberPart, out int levelNumber))
        {
            int nextLevelNumber = levelNumber + 1;
            string nextSceneName = "Level" + nextLevelNumber;

            FindFirstObjectByType<SceneTransitionManager>().LoadScene(nextSceneName);
        }
    }
}
