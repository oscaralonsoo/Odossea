using UnityEngine;
using UnityEngine.Events;

public class DropableCubeTrigger : MonoBehaviour
{
    private DropableCube dropableCube;
    private Rigidbody2D rigidbody;

    private void Awake()
    {
        dropableCube = transform.parent.GetComponent<DropableCube>();
        rigidbody = dropableCube.GetComponent<Rigidbody2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!dropableCube || !rigidbody) return;
        
        if (collision.gameObject.tag == "CubeTrigger")
        {
            DropableCube otherDropableCube = collision.transform.parent.GetComponent<DropableCube>();
            Rigidbody2D otherRigidbody = otherDropableCube.GetComponent<Rigidbody2D>();

            if (!dropableCube || !rigidbody || !otherDropableCube || !otherRigidbody) return;

            if (dropableCube.growNum > otherDropableCube.growNum)
            {
                dropableCube.Grow(otherDropableCube.growNum);
                Destroy(collision.transform.parent.gameObject);
            }
            else if (dropableCube.growNum == otherDropableCube.growNum && rigidbody.linearVelocity.magnitude < otherRigidbody.linearVelocity.magnitude)
            {
                dropableCube.Grow(otherDropableCube.growNum);
                Destroy(collision.transform.parent.gameObject);
            }
        }
    }
}
