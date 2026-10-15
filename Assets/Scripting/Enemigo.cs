using UnityEngine;
using System.Collections;

public class Enemigo : MonoBehaviour
{
    [Header("Vida")]
    public int vidaMaxima = 30; // con 10 de daño del jugador, muere en 3 golpes
    public int vidaActual;

    [Header("Movimiento (IA)")]
    public float velocidad = 2f;
    public float rangoDeteccion = 10f; // a que distancia empieza a perseguir al jugador
    private int direccion = 1; // 1 = mirando a la derecha, -1 = mirando a la izquierda

    [Header("Ataque")]
    public int dañoAtaque = 10;
    public float rangoAtaque = 2f;
    public float cooldownAtaque = 1f; // segundos entre golpe y golpe
    private float tiempoUltimoAtaque = -999f;

    [Header("Golpe visual")]
    public Sprite spriteGolpe; // arrastra el mismo sprite "Square" desde el Inspector
    public Color colorGolpe = Color.red;
    public float tamañoGolpe = 0.4f;
    public float distanciaGolpe = 1f;
    public float duracionGolpe = 0.12f;

    [Header("Bajar de plataformas persiguiendo (IA mas inteligente)")]
    public LayerMask capaSuelo; // la misma capa "Suelo" que usan Jugador y Subjefe
    public float distanciaChequeoSuelo = 0.6f;
    public float margenHorizontalParaBajar = 1.5f; // que tan alineado en X con el jugador para decidir bajar
    public float diferenciaAlturaParaBajar = 1.5f; // el jugador tiene que estar al menos esto mas abajo
    public float tiempoIgnorarPlataforma = 0.4f;
    private Collider2D colisionadorPropio;
    private Collider2D plataformaActual;
    private bool bajandoPlataforma = false;

    [Header("Saltar a plataformas persiguiendo (cuando el jugador esta arriba)")]
    public bool saltarAlPerseguir = true;
    public float fuerzaSaltoPersecucion = 9f;
    public float alturaMinimaParaSaltar = 1.2f; // el jugador tiene que estar al menos esto mas arriba
    public float alturaMaximaParaSaltar = 4f; // si esta mas arriba que esto, no intenta (no llegaria)
    public float margenHorizontalParaSaltar = 3f; // que tan cerca en X del jugador para decidir saltar
    public float cooldownSaltoPersecucion = 1.5f;
    private float tiempoUltimoSalto = -999f;

    [Header("Resbalar si cae arriba del jugador")]
    public float velocidadResbalar = 4f; // que tan fuerte se desliza hacia el costado
    public float duracionResbalar = 0.4f; // cuanto tiempo deja de perseguir mientras resbala
    private float tiempoFinResbalar = 0f;

    private Transform jugador;
    private Jugador scriptJugador;
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private EfectoFlashDaño efectoFlash;

    [Header("Referencias")]
    public GameObject modeloVisual; // el sprite/hijo separado (Animator y SpriteRenderer viven ahi)

    [HideInInspector]
    public SpawnerEnemigos spawner; // asignado automaticamente por el spawner al crearlo

    void Start()
    {
        vidaActual = vidaMaxima;
        rb = GetComponent<Rigidbody2D>();
        colisionadorPropio = GetComponent<Collider2D>();

        // evita que el enemigo se de vuelta (rote) al chocar o caer arriba de algo
        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.rotation = 0f;
        }

        GameObject fuenteVisual = modeloVisual != null ? modeloVisual : gameObject;
        animator = fuenteVisual.GetComponent<Animator>();
        spriteRenderer = fuenteVisual.GetComponent<SpriteRenderer>();
        efectoFlash = fuenteVisual.GetComponent<EfectoFlashDaño>();

        GameObject jugadorObj = GameObject.FindGameObjectWithTag("Player");
        if (jugadorObj != null)
        {
            jugador = jugadorObj.transform;
            scriptJugador = jugadorObj.GetComponent<Jugador>();
        }
    }

    void Update()
    {
        ChequearSuelo();

        if (jugador == null) return;

        float distancia = Vector2.Distance(transform.position, jugador.position);

        // mientras esta resbalando (cayo arriba tuyo), no lo dejamos perseguir ni frenar,
        // asi el empujon hacia el costado no se pisa al instante
        bool resbalando = Time.time < tiempoFinResbalar;

        // IA simple: si el jugador esta cerca, lo persigue caminando hacia el
        if (!resbalando)
        {
            if (distancia <= rangoDeteccion)
            {
                Perseguir();
                ConsiderarBajarPlataforma();
                ConsiderarSaltarPlataforma();
            }
            else
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

                if (animator != null)
                {
                    animator.SetBool("Caminando", false);
                }
            }
        }

        // si esta lo bastante cerca, intenta atacar (con cooldown para no pegar cada frame)
        if (distancia <= rangoAtaque)
        {
            IntentarAtacar();
        }
    }

    // true si el centro de este enemigo esta mas arriba que el del otro collider (no depende del signo de la normal)
    bool EstaArribaDe(Collider2D otro)
    {
        float miY = colisionadorPropio != null ? colisionadorPropio.bounds.center.y : transform.position.y;
        return otro != null && miY > otro.bounds.center.y;
    }

    // si el enemigo esta parado ENCIMA del jugador (cayo arriba de su cabeza y no se mueve),
    // lo empujamos hacia el costado para que resbale y no se quede ahi pegado
    void OnCollisionStay2D(Collision2D colision)
    {
        GameObject otro = colision.gameObject;
        bool esJugador = otro.GetComponent<Jugador>() != null;
        bool esEnemigo = otro.GetComponentInParent<Enemigo>() != null || otro.GetComponentInParent<Subjefe>() != null;
        if (!esJugador && !esEnemigo) return;

        foreach (ContactPoint2D contacto in colision.contacts)
        {
            // lado a lado con otro enemigo/subjefe: los separamos para que no se superpongan
            if (esEnemigo && Mathf.Abs(contacto.normal.x) > 0.5f)
            {
                float ladoSep = transform.position.x >= colision.transform.position.x ? 1f : -1f;
                rb.position += new Vector2(ladoSep * 2f * Time.fixedDeltaTime, 0f);
                return;
            }

            // normal.y < -0.5: el enemigo esta apoyado desde ARRIBA sobre el jugador
            // (mismo criterio que usamos en el Trampolin y la PlataformaRompible)
            if (Mathf.Abs(contacto.normal.y) > 0.5f && EstaArribaDe(colision.collider))
            {
                float lado = transform.position.x >= colision.transform.position.x ? 1f : -1f;
                rb.linearVelocity = new Vector2(lado * velocidadResbalar, rb.linearVelocity.y);
                tiempoFinResbalar = Time.time + duracionResbalar;
                return;
            }
        }
    }

    // si el jugador esta arriba (en otra plataforma) y bastante cerca en X, salta para buscarlo
    void ConsiderarSaltarPlataforma()
    {
        if (!saltarAlPerseguir) return;
        if (bajandoPlataforma) return;
        if (plataformaActual == null) return; // solo salta si esta apoyado en el suelo
        if (Time.time < tiempoUltimoSalto + cooldownSaltoPersecucion) return;

        float alturaRelativa = jugador.position.y - transform.position.y;
        bool jugadorArriba = alturaRelativa >= alturaMinimaParaSaltar && alturaRelativa <= alturaMaximaParaSaltar;
        bool cercaEnX = Mathf.Abs(jugador.position.x - transform.position.x) <= margenHorizontalParaSaltar;

        if (jugadorArriba && cercaEnX)
        {
            tiempoUltimoSalto = Time.time;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSaltoPersecucion);
        }
    }

    void ChequearSuelo()
    {
        if (colisionadorPropio == null) return;

        Vector2 origenRaycast = new Vector2(colisionadorPropio.bounds.center.x, colisionadorPropio.bounds.min.y);
        RaycastHit2D golpe = Physics2D.Raycast(origenRaycast, Vector2.down, distanciaChequeoSuelo, capaSuelo);
        plataformaActual = golpe.collider;
    }

    // si el jugador esta bastante mas abajo y mas o menos alineado en X, se tira
    // de la plataforma actual (atravesandola un rato) en vez de quedarse pegado
    // caminando contra el borde como un boludo
    void ConsiderarBajarPlataforma()
    {
        if (bajandoPlataforma) return;
        if (plataformaActual == null) return;
        if (plataformaActual.GetComponentInParent<PlatformEffector2D>() == null) return;

        bool jugadorMuchoMasAbajo = (transform.position.y - jugador.position.y) >= diferenciaAlturaParaBajar;
        bool alineadoHorizontal = Mathf.Abs(jugador.position.x - transform.position.x) <= margenHorizontalParaBajar;

        if (jugadorMuchoMasAbajo && alineadoHorizontal)
        {
            StartCoroutine(BajarPlataforma(plataformaActual));
        }
    }

    IEnumerator BajarPlataforma(Collider2D plataforma)
    {
        bajandoPlataforma = true;

        Physics2D.IgnoreCollision(colisionadorPropio, plataforma, true);
        yield return new WaitForSeconds(tiempoIgnorarPlataforma);

        if (plataforma != null && colisionadorPropio != null)
        {
            Physics2D.IgnoreCollision(colisionadorPropio, plataforma, false);
        }

        bajandoPlataforma = false;
    }

    void Perseguir()
    {
        direccion = jugador.position.x > transform.position.x ? 1 : -1;
        rb.linearVelocity = new Vector2(direccion * velocidad, rb.linearVelocity.y);

        if (animator != null)
        {
            animator.SetBool("Caminando", true);
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = direccion < 0;
        }
    }

    void IntentarAtacar()
    {
        if (Time.time >= tiempoUltimoAtaque + cooldownAtaque)
        {
            tiempoUltimoAtaque = Time.time;

            MostrarGolpeVisual();

            if (animator != null)
            {
                animator.SetTrigger("Atacando");
            }

            Collider2D[] impactos = Physics2D.OverlapCircleAll(transform.position, rangoAtaque);

            foreach (Collider2D impacto in impactos)
            {
                Jugador jugadorImpactado = impacto.GetComponent<Jugador>();
                if (jugadorImpactado != null)
                {
                    jugadorImpactado.RecibirDaño(dañoAtaque);
                }
            }
        }
    }

    void MostrarGolpeVisual()
    {
        GameObject golpe = new GameObject("GolpeVisualEnemigo");
        golpe.transform.SetParent(transform); // hijo del enemigo: se mueve junto con el
        golpe.transform.localPosition = new Vector3(direccion * distanciaGolpe, 0, 0);
        golpe.transform.localScale = Vector3.one * tamañoGolpe;

        SpriteRenderer sr = golpe.AddComponent<SpriteRenderer>();
        sr.sprite = spriteGolpe;
        sr.color = colorGolpe;
        sr.sortingOrder = 10;

        Destroy(golpe, duracionGolpe);
    }

    public void RecibirDaño(int cantidad)
    {
        vidaActual -= cantidad;
        Debug.Log("Enemigo recibio daño, vida restante: " + vidaActual);

        if (efectoFlash != null)
        {
            efectoFlash.Flashear();
        }

        if (vidaActual <= 0)
        {
            Morir();
        }
    }

    public void MorirInstantaneo()
    {
        Debug.Log("Enemigo eliminado de un golpe por el subjefe");
        Morir();
    }

    void Morir()
    {
        Debug.Log("Enemigo murio");

        if (animator != null)
        {
            animator.SetTrigger("Muerto");
        }

        if (spawner != null)
        {
            spawner.NotificarMuerte();
        }

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangoAtaque);
    }
}