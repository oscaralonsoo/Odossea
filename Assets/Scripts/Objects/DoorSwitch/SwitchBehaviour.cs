using System.Collections;
using UnityEngine;

public class SwitchBehaviour : MonoBehaviour
{
    public enum SwitchMode
    {
        OpenOnly,
        CloseOnly,
        Toggle,
        PressurePlate
    }

    [SerializeField] DoorBehaviour door;
    [SerializeField] SwitchMode mode;

    float switchSizeY;
    Vector3 upPos;
    Vector3 downPos;

    bool isPressed;
    int pressCount = 0;

    [SerializeField] float switchSpeed = 3f;
    [SerializeField] float releaseDelay = 0.2f;

    void Awake()
    {
        switchSizeY = transform.localScale.y / 2;
        upPos = transform.position;
        downPos = upPos + Vector3.down * switchSizeY;
    }

    void Update()
    {
        Vector3 target = isPressed ? downPos : upPos;
        transform.position = Vector3.MoveTowards(transform.position, target, switchSpeed * Time.deltaTime);
    }
    void OnTriggerEnter2D(Collider2D col)
    {
        if (!col.CompareTag("Bone")) return;

        pressCount++;

        if (pressCount > 1) return;

        isPressed = true;

        switch (mode)
        {
            case SwitchMode.OpenOnly:
                door.SetDoor(true);
                break;

            case SwitchMode.CloseOnly:
                door.SetDoor(false);
                break;

            case SwitchMode.Toggle:
                door.ToggleDoor();
                break;

            case SwitchMode.PressurePlate:
                door.SetDoor(true);
                break;
        }
    }
    void OnTriggerExit2D(Collider2D col)
    {
        if (!col.CompareTag("Bone")) return;

        pressCount--;

        if (pressCount > 0) return;

        if (mode == SwitchMode.PressurePlate)
            StartCoroutine(Release());
    }
    IEnumerator Release()
    {
        yield return new WaitForSeconds(releaseDelay);
        isPressed = false;
        door.SetDoor(false);
    }
}
