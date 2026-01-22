using UnityEngine;

public class MarcScript : MonoBehaviour
{
    [SerializeField] Rigidbody2D[] rigibodyes;

    [SerializeField] Rigidbody2D rigibody;
    [SerializeField] float force;

    private void Awake()
    {
        rigibody = GetComponent<Rigidbody2D>();
    }
    private void FixedUpdate()
    {

    }
}
