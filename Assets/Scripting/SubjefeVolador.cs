using UnityEngine;

// Por ahora, este Titan se comporta EXACTAMENTE igual que un Subjefe normal
// cuando esta libre (camina, persigue, pega). La unica diferencia es que,
// cuando el jugador lo posee, en vez de un salto normal (que necesita estar
// en el suelo), puede aletear en el aire las veces que quiera con Espacio.
public class SubjefeVolador : Subjefe
{
    [Header("Vuelo (solo cuando lo posee el jugador)")]
    public float fuerzaAleteo = 6f;
    public float velocidadVolando = 4f; // velocidad horizontal cuando esta en el aire (en el suelo usa "velocidad")

    [Header("Vuelo suave")]
    public bool aletearMantenendo = true; // si mantenes Espacio/A apretado, aletea solo cada tanto
    public float intervaloAleteoMantenido = 0.25f; // segundos entre aleteos al mantener apretado
    public float velocidadMaximaCaida = 8f; // tope de velocidad al caer (para que no sea tan pesado)
    private float tiempoUltimoAleteo = -999f;

    protected override void Update()
    {
        base.Update();

        if (estaMuerto || rb == null) return;

        // tope de caida
        if (rb.linearVelocity.y < -velocidadMaximaCaida)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -velocidadMaximaCaida);
        }

        // mantener apretado = aletea solo (salvo que apretes abajo para bajar de plataforma)
        if (aletearMantenendo && estaPoseido && Time.timeScale > 0f
            && Input.GetButton("Jump") && Input.GetAxisRaw("Vertical") > -0.5f
            && Time.time >= tiempoUltimoAleteo + intervaloAleteoMantenido)
        {
            Saltar();
        }
    }

    // en el suelo camina con la velocidad normal; en el aire usa velocidadVolando
    public override void Mover(float horizontal)
    {
        if (AplicarResbalarPoseido()) return;

        // poseido: caminando usa "velocidad" y en el aire "velocidadVolando". Sin poseer: siempre "velocidadPersecucion"
        float velocidadActual = estaPoseido
            ? (enSuelo ? velocidad : velocidadVolando)
            : velocidadPersecucion;
        rb.linearVelocity = new Vector2(horizontal * velocidadActual, rb.linearVelocity.y);

        if (horizontal != 0)
        {
            ActualizarDireccion(horizontal > 0 ? 1 : -1);
        }

        if (animator != null)
        {
            animator.SetBool("Caminando", horizontal != 0);
        }
    }

    public override void Saltar()
    {
        tiempoUltimoAleteo = Time.time;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaAleteo);

        if (animator != null)
        {
            animator.SetTrigger("Saltando");
        }
    }
}