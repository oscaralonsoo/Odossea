using Unity.VisualScripting;
using UnityEngine;

public class DropableCubeTrigger : MonoBehaviour
{
    private DropableCube dropableCube;
    private Rigidbody2D rigidbody;

    private void Awake()
    {
        dropableCube = transform.parent.GetComponentInChildren<DropableCube>();
        rigidbody = dropableCube.GetComponent<Rigidbody2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!dropableCube || !rigidbody) return;

        if (collision.gameObject.tag == "CubeTrigger")
        {
            DropableCube otherDropableCube = null;
            Rigidbody2D otherRigidbody = null;

            otherDropableCube = collision.transform.parent.GetComponentInChildren<DropableCube>();

            if (otherDropableCube == null) return;

            otherRigidbody = otherDropableCube.GetComponent<Rigidbody2D>();

            if (otherRigidbody == null) return;

            if (!otherDropableCube.collided)
            {
                if (dropableCube.growNum > otherDropableCube.growNum)
                {
                    dropableCube.Grow(otherDropableCube.growNum);
                    otherDropableCube.Collided();
                }
                else if (dropableCube.growNum == otherDropableCube.growNum && rigidbody.linearVelocity.magnitude < otherRigidbody.linearVelocity.magnitude)
                {
                    dropableCube.Grow(otherDropableCube.growNum);
                    otherDropableCube.Collided();
                }
            }

        }
    }
}
