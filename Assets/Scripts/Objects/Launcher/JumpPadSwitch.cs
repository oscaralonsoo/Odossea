using System.Collections;
using UnityEngine;

public class JumpPadSwitchBehaviour : MonoBehaviour
{
    public enum SwitchMode
    {
        Instant,
        Toggle,
        PressurePlate
    }

    [SerializeField] JumpPadBehaviour jumpPad;
    [SerializeField] SwitchMode mode;

    [Header("Animation")]
    [SerializeField] float switchSpeed = 3f;
    [SerializeField] float releaseDelay = 0.2f;

    float switchSizeY;
    Vector3 upPos;
    Vector3 downPos;

    bool isPressed;
    int pressCount = 0;

    void Awake()
    {
        switchSizeY = transform.localScale.y / 2f;
        upPos = transform.position;
        downPos = upPos + Vector3.down * switchSizeY;
    }

    void Update()
    {
        Vector3 target = isPressed ? downPos : upPos;
        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            switchSpeed * Time.deltaTime
        );
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (!col.CompareTag("Bone")) return;

        pressCount++;
        if (pressCount > 1) return;

        isPressed = true;

        switch (mode)
        {
            case SwitchMode.Instant:
                jumpPad.LaunchAll();
                break;

            case SwitchMode.Toggle:
                jumpPad.Toggle();
                break;

            case SwitchMode.PressurePlate:
                jumpPad.Activate();
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
        else
            isPressed = false;
    }

    IEnumerator Release()
    {
        yield return new WaitForSeconds(releaseDelay);
        isPressed = false;
        jumpPad.Deactivate();
    }
}
