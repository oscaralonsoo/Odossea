using Unity.VisualScripting;
using UnityEngine;

public class DoorBehaviour : MonoBehaviour
{
    // Creamos un selector para elegir la dirección
    public enum DoorDirection { Horizontal, Vertical }

    [Header("Configuración de la Puerta")]
    [SerializeField] DoorDirection direction = DoorDirection.Vertical; // Por defecto vertical
    [SerializeField] float openDistance = 3f; // Cuántos metros se mueve
    [SerializeField] float speed = 10f;

    Vector2 closedPos;
    Vector2 openPos;

    bool isOpen;
    int users = 0;

    void Awake()
    {
        closedPos = transform.position;

        // Calculamos el desplazamiento basado en la elección del usuario
        Vector2 displacement = Vector2.zero;

        if (direction == DoorDirection.Horizontal)
        {
            // Se mueve en el eje X
            displacement = new Vector2(openDistance, 0);
        }
        else
        {
            // Se mueve en el eje Y
            displacement = new Vector2(0, openDistance);
        }

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

        // La puerta se mantiene abierta si hay al menos 1 usuario activándola
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