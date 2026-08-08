using UnityEngine;

public class Subjefe : MonoBehaviour
{
    [Header("Vida")]
    public int vidaMaxima = 100;
    public int vidaActual;

    [Header("Movimiento")]
    public float velocidad = 3f; // usado cuando el jugador lo posee

    [Header("Posesion")]
    public bool esPoseible = false; // se activa cuando el jugador puede poseerlo
    public float rangoDeteccion = 3f; // que tan cerca tiene que estar el jugador
    [Range(0f, 1f)]
    public float porcentajeVidaParaPoseer = 0.3f; // se puede poseer cuando la vida baja de este %

    private Transform jugador;
    private Rigidbody2D rb;

    void Start()
    {
        vidaActual = vidaMaxima;
        rb = GetComponent<Rigidbody2D>();

        // buscamos al jugador por su tag (asegurate de que el jugador tenga el tag "Player")
        GameObject jugadorObj = GameObject.FindGameObjectWithTag("Player");
        if (jugadorObj != null)
        {
            jugador = jugadorObj.transform;
        }
    }

    public void Mover(float horizontal)
    {
        rb.linearVelocity = new Vector2(horizontal * velocidad, rb.linearVelocity.y);
    }

    void Update()
    {
        if (jugador == null) return;

        float distancia = Vector2.Distance(transform.position, jugador.position);
        bool enRango = distancia <= rangoDeteccion;
        bool vidaBaja = vidaActual <= vidaMaxima * porcentajeVidaParaPoseer;

        esPoseible = enRango && vidaBaja;
    }

    public void RecibirDaño(int cantidad)
    {
        vidaActual -= cantidad;
        vidaActual = Mathf.Max(vidaActual, 0);

        if (vidaActual <= 0)
        {
            Morir();
        }
    }

    void Morir()
    {
        Debug.Log("El subjefe murio");
        // aca despues metemos animacion, drop de recompensa, etc.
    }

    // dibuja el rango de deteccion en el editor, para que lo veas mientras trabajas
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);
    }
}