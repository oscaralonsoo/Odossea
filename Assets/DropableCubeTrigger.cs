using Unity.VisualScripting;
using UnityEngine;

public class DropableCubeTrigger : MonoBehaviour
{
    private DropableCube dropableCube;
    private Rigidbody2D rigidbody;

    private void Awake()
    {
        dropableCube = transform.parent.GetComponent<DropableCube>();
        rigidbody = dropableCube.GetComponent<Rigidbody2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!dropableCube || !rigidbody) return;

        if (collision.gameObject.tag == "CubeTrigger")
        {
            DropableCube otherDropableCube = collision.transform.parent.GetComponent<DropableCube>();
            Rigidbody2D otherRigidbody = otherDropableCube.GetComponent<Rigidbody2D>();

            if (!dropableCube || !rigidbody || !otherDropableCube || !otherRigidbody) return;

            if (!otherDropableCube.collided)
            {
                if (dropableCube.growNum > otherDropableCube.growNum)
                {
                    dropableCube.Grow(otherDropableCube.growNum);
                    otherDropableCube.collided = true;
                    Destroy(otherDropableCube.gameObject);
                }
                else if (dropableCube.growNum == otherDropableCube.growNum && rigidbody.linearVelocity.magnitude < otherRigidbody.linearVelocity.magnitude)
                {
                    dropableCube.Grow(otherDropableCube.growNum);
                    otherDropableCube.collided = true;
                    Destroy(otherDropableCube.gameObject);
                }
            }
        }
    }
}
