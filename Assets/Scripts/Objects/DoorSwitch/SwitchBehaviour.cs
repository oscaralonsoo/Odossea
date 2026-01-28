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

    Vector3 upPos;
    Vector3 downPos;

    bool isPressed;
    int pressCount = 0;

    [SerializeField] Transform button;

    [SerializeField] float switchSpeed = 3f;
    [SerializeField] float releaseDelay = 0.2f;
    [SerializeField] float downPosY = 0.2f;

    void Awake()
    {
        upPos = button.position;
        downPos = upPos + Vector3.down * downPosY;
    }

    void Update()
    {
        Vector3 target = isPressed ? downPos : upPos;
        button.position = Vector3.MoveTowards(button.position, target, switchSpeed * Time.deltaTime);
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
                if(!door.IsOpen())door.SetDoor(true);
                break;

            case SwitchMode.CloseOnly:
                if (door.IsOpen()) door.SetDoor(false);
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
