using UnityEngine;

public class CubeTrigger : MonoBehaviour
{
    public PlayerController playerCube;

    private void Awake()
    {
        playerCube = null;
        playerCube = transform.parent.GetComponent<PlayerController>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!playerCube) return;

        if (playerCube.IsFull()) return;

        if (collision.gameObject.tag == "CubeTrigger")
        {
            DropableCube dropableCube = null;

            dropableCube = collision.transform.parent.GetComponentInChildren<DropableCube>();

            if (dropableCube != null && !dropableCube.collided)
            {
                playerCube.Grow(dropableCube.growNum);
                dropableCube.Collided();
            }
        }
    }
}
