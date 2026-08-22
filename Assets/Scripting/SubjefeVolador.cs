using UnityEngine;

// Titan que vuela dando pequeños saltos/aleteos (estilo Flappy Bird) en vez de caminar.
// Hereda vida, posesion, ataque cuerpo a cuerpo y muerte del Subjefe base - solo
// le cambia por completo la forma de moverse.
public class SubjefeVolador : Subjefe
{
    [Header("Vuelo (estilo Flappy Bird)")]
    public float fuerzaAleteo = 6f; // que tan fuerte sube en cada aleteo
    public float intervaloAleteo = 0.6f; // cada cuanto aletea solo mientras persigue
    private float tiempoUltimoAleteo = -999f;

    protected override void Update()
    {
        base.Update(); // mantiene deteccion de posesion, muerte, etc

        if (!estaPoseido && !estaMuerto && persigueAlJugador && jugador != null)
        {
            VolarHaciaJugador();
            IntentarAtacarAlJugador();
        }
    }

    void VolarHaciaJugador()
    {
        float distancia = ObtenerDistanciaAlJugador();

        // si ya esta en rango de ataque, se frena en el aire (no sigue empujando)
        if (distancia <= rangoAtaqueJugador || distancia > rangoPersecucion)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        float horizontal = jugador.position.x > transform.position.x ? 1f : -1f;
        ActualizarDireccion(horizontal > 0 ? 1 : -1);

        if (animator != null)
        {
            animator.SetBool("Caminando", true); // reusa la misma animacion de movimiento
        }

        // aletea cada "intervaloAleteo" segundos: entre aleteo y aleteo, la gravedad
        // lo va bajando solo, dando el efecto de vuelo tipo Flappy Bird
        if (Time.time >= tiempoUltimoAleteo + intervaloAleteo)
        {
            tiempoUltimoAleteo = Time.time;
            rb.linearVelocity = new Vector2(horizontal * velocidad, fuerzaAleteo);

            if (animator != null)
            {
                animator.SetTrigger("Saltando"); // reusa el clip de salto que ya tenes
            }
        }
        else
        {
            rb.linearVelocity = new Vector2(horizontal * velocidad, rb.linearVelocity.y);
        }
    }

    // el jugador (poseyendolo) tambien aletea con Espacio, sin necesitar estar en el suelo
    public override void Saltar()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaAleteo);

        if (animator != null)
        {
            animator.SetTrigger("Saltando");
        }
    }
}