using UnityEngine;

public class PlataformaMovil : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    public Transform puntoA;
    public Transform puntoB;
    public float velocidad = 2f;
    public float tiempoEspera = 0.5f;

    private Vector3 destino;
    private bool esperando = false;

    void Start()
    {
        // Iniciamos la plataforma en la posición del Punto A
        transform.position = puntoA.position;
        destino = puntoB.position;
    }

    void Update()
    {
        if (!esperando)
        {
            // Mover la plataforma hacia el destino
            transform.position = Vector3.MoveTowards(transform.position, destino, velocidad * Time.deltaTime);

            // Comprobar si llegó al destino
            if (Vector3.Distance(transform.position, destino) < 0.02f)
            {
                StartCoroutine(CambiarDestino());
            }
        }
    }

    System.Collections.IEnumerator CambiarDestino()
    {
        esperando = true;
        yield return new WaitForSeconds(tiempoEspera);

        // Intercambiar el destino entre A y B
        destino = (destino == puntoA.position) ? puntoB.position : puntoA.position;
        esperando = false;
    }

    // --- SISTEMA PARA QUE EL JUGADOR NO SE DESLICE ---

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Si el objeto que toca la plataforma tiene el Tag "Player"
        if (collision.gameObject.CompareTag("Player"))
        {
            // El jugador se vuelve "hijo" de la plataforma y se mueve con ella
            collision.transform.SetParent(transform);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // El jugador deja de ser hijo al saltar o bajar
            collision.transform.SetParent(null);
        }
    }
}
