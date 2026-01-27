using UnityEngine;

public class DoorBehaviour : MonoBehaviour
{
    Vector3 closedPos;
    Vector3 openPos;

    [SerializeField] float openHeight = 5f;
    [SerializeField] float speed = 10f;

    bool isOpen;

    void Awake()
    {
        closedPos = transform.position;
        openPos = closedPos + Vector3.up * openHeight;
    }

    void Update()
    {
        Vector3 target = isOpen ? openPos : closedPos;
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
    }

    public void SetDoor(bool open)
    {
        isOpen = open;
    }

    public void ToggleDoor()
    {
        isOpen = !isOpen;
    }
}
