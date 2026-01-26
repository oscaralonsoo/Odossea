using UnityEngine;

public class CubeTrigger : MonoBehaviour
{
    public PlayerController playerCube;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!playerCube) return;

        if (collision.gameObject.tag == "CubeTrigger")
        {
            DropableCube dropableCube;
            if (collision.transform.parent.TryGetComponent<DropableCube>(out dropableCube))
            {
                if (!dropableCube.collided)
                {
                    playerCube.Grow(dropableCube.growNum);
                    dropableCube.collided = true;
                    Destroy(dropableCube.gameObject);
                }
            }
        }
    }
}
