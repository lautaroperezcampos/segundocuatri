using UnityEngine;

public class Subjefe : MonoBehaviour
{
    [Header("Vida")]
    public int vidaMaxima = 100;
    public int vidaActual;

    [Header("Movimiento")]
    public float velocidad = 3f; // usado cuando el jugador lo posee, y tambien al perseguir
    protected int direccion = 1; // 1 = mirando a la derecha, -1 = mirando a la izquierda

    [Header("Persecucion (cuando NO esta poseido)")]
    public bool persigueAlJugador = true;
    public float rangoPersecucion = 6f; // a que distancia empieza a caminar hacia el jugador

    [Header("Ataque cuerpo a cuerpo contra el jugador (cuando NO esta poseido)")]
    public int dañoContraJugador = 10;
    public float rangoAtaqueJugador = 1.2f;
    public float cooldownAtaqueJugador = 1.5f;
    private float tiempoUltimoAtaqueJugador = -999f;

    [Header("Salto")]
    public float fuerzaSalto = 8f;
    public float distanciaChequeoSuelo = 0.6f; // ajustar segun el alto del sprite
    public LayerMask capaSuelo; // elegi que layer cuenta como "suelo" en el Inspector
    private bool enSuelo = false;

    [Header("Posesion")]
    public bool esPoseible = false; // se activa cuando el jugador puede poseerlo
    public float rangoDeteccion = 3f; // que tan cerca tiene que estar el jugador
    [Range(0f, 1f)]
    public float porcentajeVidaParaPoseer = 0.3f; // se puede poseer cuando la vida baja de este %
    [HideInInspector]
    public bool estaPoseido = false; // el Jugador lo marca true/false al poseer/dejar de poseer
    [HideInInspector]
    public bool estaMuerto = false; // true cuando la vida llega a 0 de verdad

    [Header("Ataque (cuando esta poseido)")]
    public int dañoAtaque = 15;
    public float rangoAtaque = 1.5f;

    [Header("Golpe visual")]
    public Sprite spriteGolpe; // arrastra el mismo sprite "Square" desde el Inspector
    public Color colorGolpe = Color.yellow;
    public float tamañoGolpe = 0.4f;
    public float distanciaGolpe = 1f;
    public float duracionGolpe = 0.12f;

    [Header("Al morir")]
    public float tiempoAntesDeDesaparecer = 2f; // le da tiempo a que se vea la animacion de muerte

    [Header("Referencias")]
    public GameObject modeloVisual; // el sprite/hijo separado, para escalarlo sin tocar el collider

    protected Transform jugador;
    protected Rigidbody2D rb;
    protected Animator animator;
    private SpriteRenderer spriteRenderer;
    private Collider2D colisionadorPropio;
    private Collider2D colisionadorJugador;

    protected virtual void Start()
    {
        vidaActual = vidaMaxima;
        rb = GetComponent<Rigidbody2D>();
        colisionadorPropio = GetComponent<Collider2D>();

        // el Animator/SpriteRenderer pueden estar en el mismo objeto, o en el hijo "modeloVisual"
        GameObject fuenteVisual = modeloVisual != null ? modeloVisual : gameObject;
        animator = fuenteVisual.GetComponent<Animator>();
        spriteRenderer = fuenteVisual.GetComponent<SpriteRenderer>();

        // buscamos al jugador por su tag (asegurate de que el jugador tenga el tag "Player")
        GameObject jugadorObj = GameObject.FindGameObjectWithTag("Player");
        if (jugadorObj != null)
        {
            jugador = jugadorObj.transform;
            colisionadorJugador = jugadorObj.GetComponent<Collider2D>();
        }
    }

    public void Mover(float horizontal)
    {
        rb.linearVelocity = new Vector2(horizontal * velocidad, rb.linearVelocity.y);

        if (horizontal != 0)
        {
            ActualizarDireccion(horizontal > 0 ? 1 : -1);
        }

        if (animator != null)
        {
            animator.SetBool("Caminando", horizontal != 0);
        }
    }

    // actualiza hacia donde "mira" el subjefe y voltea el sprite en consecuencia.
    // la usa Mover() al caminar, y la pueden usar tambien clases hijas (por ejemplo
    // para girar hacia el jugador antes de disparar, aunque no se este moviendo)
    protected void ActualizarDireccion(int nuevaDireccion)
    {
        direccion = nuevaDireccion;

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = direccion < 0;
        }
    }

    public virtual void Saltar()
    {
        if (enSuelo)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSalto);
        }
    }

    // usados por el Jugador para moverlo manualmente por la escalera diagonal
    public void MoverEnDireccion(Vector2 direccionYVelocidad)
    {
        rb.linearVelocity = direccionYVelocidad;
    }

    public void CambiarTipoDeCuerpo(RigidbodyType2D tipo)
    {
        rb.bodyType = tipo;
    }

    public Collider2D ObtenerCollider()
    {
        return colisionadorPropio;
    }

    public virtual void Atacar()
    {
        MostrarGolpeVisual();

        Collider2D[] impactos = Physics2D.OverlapCircleAll(transform.position, rangoAtaque);

        foreach (Collider2D impacto in impactos)
        {
            Puerta puerta = impacto.GetComponent<Puerta>();
            if (puerta != null)
            {
                puerta.RecibirDaño(dañoAtaque);
            }

            Enemigo enemigo = impacto.GetComponent<Enemigo>();
            if (enemigo != null)
            {
                enemigo.MorirInstantaneo(); // el subjefe mata de un solo golpe
            }
        }
    }

    protected void MostrarGolpeVisual()
    {
        GameObject golpe = new GameObject("GolpeVisualSubjefe");
        golpe.transform.SetParent(transform); // hijo del subjefe: se mueve junto con el
        golpe.transform.localPosition = new Vector3(direccion * distanciaGolpe, 0, 0);
        golpe.transform.localScale = Vector3.one * tamañoGolpe;

        SpriteRenderer sr = golpe.AddComponent<SpriteRenderer>();
        sr.sprite = spriteGolpe;
        sr.color = colorGolpe;
        sr.sortingOrder = 10;

        Destroy(golpe, duracionGolpe);
    }

    protected virtual void Update()
    {
        if (estaMuerto)
        {
            esPoseible = false;
            return; // muerto de verdad: no se mueve, no ataca, no se puede poseer
        }

        // el rayo sale desde el borde de ABAJO del collider real (no del pivote del objeto),
        // asi funciona bien sin importar el Offset/Size que le hayas puesto al Box Collider2D
        Vector2 origenRaycast = colisionadorPropio != null
            ? new Vector2(colisionadorPropio.bounds.center.x, colisionadorPropio.bounds.min.y)
            : (Vector2)transform.position;

        enSuelo = Physics2D.Raycast(origenRaycast, Vector2.down, distanciaChequeoSuelo, capaSuelo);

        if (jugador == null) return;

        // persigue y ataca al jugador, SOLO si no esta poseido ahora mismo
        if (!estaPoseido && persigueAlJugador)
        {
            ManejarPersecucion();
            IntentarAtacarAlJugador();
        }

        bool enRango;

        if (colisionadorPropio != null && colisionadorJugador != null)
        {
            // distancia real entre bordes, no entre centros
            float distanciaBordes = colisionadorPropio.Distance(colisionadorJugador).distance;
            enRango = distanciaBordes <= rangoDeteccion;
        }
        else
        {
            // respaldo por si falta algun collider: comparamos centros como antes
            float distancia = Vector2.Distance(transform.position, jugador.position);
            enRango = distancia <= rangoDeteccion;
        }

        bool vidaBaja = vidaActual <= vidaMaxima * porcentajeVidaParaPoseer;

        esPoseible = enRango && vidaBaja;
    }

    void ManejarPersecucion()
    {
        float distancia = ObtenerDistanciaAlJugador();

        // camina hacia el jugador solo si esta en rango de persecucion PERO todavia
        // no lo suficientemente cerca como para atacarlo (si no, se frena y solo pega)
        if (distancia <= rangoPersecucion && distancia > rangoAtaqueJugador)
        {
            float horizontal = jugador.position.x > transform.position.x ? 1f : -1f;
            Mover(horizontal);
        }
        else
        {
            Mover(0f); // ya esta en rango de ataque, o muy lejos: se queda quieto
        }
    }

    protected void IntentarAtacarAlJugador()
    {
        float distancia = ObtenerDistanciaAlJugador();
        if (distancia > rangoAtaqueJugador) return;
        if (Time.time < tiempoUltimoAtaqueJugador + cooldownAtaqueJugador) return;

        tiempoUltimoAtaqueJugador = Time.time;

        MostrarGolpeVisual();

        if (animator != null)
        {
            animator.SetTrigger("Atacando");
        }

        Jugador scriptJugador = jugador.GetComponent<Jugador>();
        if (scriptJugador != null)
        {
            scriptJugador.RecibirDaño(dañoContraJugador);
        }
    }

    // distancia real entre bordes de collider (si hay ambos colliders disponibles),
    // o entre centros como respaldo - asi funciona bien sea cual sea el tamaño del sprite
    protected float ObtenerDistanciaAlJugador()
    {
        if (colisionadorPropio != null && colisionadorJugador != null)
        {
            return colisionadorPropio.Distance(colisionadorJugador).distance;
        }

        return Vector2.Distance(transform.position, jugador.position);
    }

    public void RecibirDaño(int cantidad)
    {
        if (estaMuerto) return; // ya esta muerto, ignoramos mas daño

        vidaActual -= cantidad;
        vidaActual = Mathf.Max(vidaActual, 0);

        if (vidaActual <= 0)
        {
            Morir();
        }
    }

    protected virtual void Morir()
    {
        estaMuerto = true;
        Debug.Log("El subjefe murio");

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic; // se queda congelado en el lugar, sin gravedad
        }

        if (animator != null)
        {
            animator.SetTrigger("Muerto");
        }

        // el collider pasa a Trigger: ya no bloquea el paso, pero el cuerpo no se cae
        if (colisionadorPropio != null)
        {
            colisionadorPropio.isTrigger = true;
        }

        // desaparece del todo despues de un rato, dejando ver la animacion primero
        Destroy(gameObject, tiempoAntesDeDesaparecer);
        // aca despues metemos drop de recompensa, etc.
    }

    // dibuja el rango de deteccion, el rango de persecucion y el raycast de suelo en el editor
    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, rangoPersecucion);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * distanciaChequeoSuelo);
    }
}