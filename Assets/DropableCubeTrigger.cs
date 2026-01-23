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
            if (rigidbody.linearVelocity.magnitude < collision.attachedRigidbody.linearVelocity.magnitude)
            {
                dropableCube.Grow();
                collision.transform.parent.gameObject.SetActive(false);
            }
        }
    }
}
