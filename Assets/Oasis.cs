using UnityEngine;

public class Oasis : MonoBehaviour
{
    public int growNum;
    public float growSpeed;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerController player = null;
        player = collision.transform.parent.GetComponent<PlayerController>();

        if (player == null) return;

        player.GrowToNum(growNum, growSpeed);
    }
}
