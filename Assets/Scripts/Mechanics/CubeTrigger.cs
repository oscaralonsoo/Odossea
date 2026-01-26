using UnityEngine;

public class CubeTrigger : MonoBehaviour
{
    public PlayerController playerCube;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!playerCube) return;

        if (collision.gameObject.tag == "CubeTrigger")
        {
            DropableCube dropableCube = null;

            collision.transform.TryGetComponent<DropableCube>(out dropableCube);

            if (dropableCube == null && collision.transform.parent != null)
            {
                collision.transform.parent.TryGetComponent<DropableCube>(out dropableCube);
            }

            if (dropableCube != null && !dropableCube.collided)
            {
                playerCube.Grow(dropableCube.growNum);
                dropableCube.collided = true;
                Destroy(dropableCube.gameObject);
            }
        }
    }
}
