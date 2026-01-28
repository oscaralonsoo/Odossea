using Unity.VisualScripting;
using UnityEngine;

public class DoorBehaviour : MonoBehaviour
{
    Vector2 closedPos;
    Vector2 openPos;

    [SerializeField] float speed = 10f;
    [SerializeField] Vector2 displacement;

    bool isOpen;
    int users = 0;

    void Awake()
    {
        closedPos = transform.position;
        openPos = closedPos + displacement;
    }

    void Update()
    {
        Vector3 target = isOpen ? openPos : closedPos;
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
    }

    public void SetDoor(bool open)
    {
        if (open) users++;
        else users--;
        isOpen = users > 0;
    }

    public void ToggleDoor()
    {
        isOpen = !isOpen;
    }

    public bool IsOpen()
    {
        return isOpen;
    }
}
