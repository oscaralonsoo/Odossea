using UnityEngine;

public class CubeTrigger : MonoBehaviour
{
    private MarcScript playerCube;
    private Rigidbody2D rigidbody;

    private void Awake()
    {
        playerCube = transform.parent.GetComponent<MarcScript>();
        rigidbody = playerCube.GetComponent<Rigidbody2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!playerCube || !rigidbody) return;

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
