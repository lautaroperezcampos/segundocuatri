using UnityEngine;

public class Jugador : MonoBehaviour
{
    [Header("Vida")]
    public int vidaMaxima = 100;
    public int vidaActual;

    [Header("Regeneracion")]
    public float intervaloRegeneracion = 5f; // segundos entre cada punto de vida recuperado
    private float tiempoUltimaRegeneracion;

    [Header("Movimiento")]
    public float velocidad = 5f;
    private Rigidbody2D rb;
    private Collider2D colisionador;
    private int direccion = 1; // 1 = mirando a la derecha, -1 = mirando a la izquierda

    [Header("Salto")]
    public float fuerzaSalto = 8f;
    public float distanciaChequeoSuelo = 0.6f; // ajustar segun el alto del sprite
    public LayerMask capaSuelo; // elegi que layer cuenta como "suelo" en el Inspector
    private bool enSuelo = false;

    [Header("Escalera diagonal")]
    private bool tocandoEscalera = false;
    private EscaleraDiagonal escaleraActual;

    [Header("Ataque")]
    public int daño = 10;
    public float rangoAtaque = 1.5f;

    [Header("Golpe visual")]
    public Sprite spriteGolpe; // arrastra el mismo sprite "Square" desde el Inspector
    public Color colorGolpe = Color.yellow;
    public float tamañoGolpe = 0.4f;
    public float distanciaGolpe = 1f; // que tan lejos del jugador aparece
    public float duracionGolpe = 0.12f; // cuanto tiempo se ve antes de desaparecer

    [Header("Posesion")]
    public bool estaPoseyendo = false;
    private Subjefe subjefeCercano; // referencia al subjefe que esta en rango
    private Subjefe subjefePoseido; // el que estamos controlando ahora mismo

    [Header("UI")]
    public GameObject botonPosesion; // arrastra aca el boton/icono desde el Inspector

    [Header("Referencias")]
    public GameObject modeloJugador; // el sprite/visual del jugador, para esconderlo al poseer
    public CamaraSeguimiento camara; // arrastra el Main Camera desde el Inspector
    public GameObject panelGameOver; // arrastra el panel de UI con el texto y el boton Reintentar
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        colisionador = GetComponent<Collider2D>();
        vidaActual = vidaMaxima;

        if (modeloJugador != null)
        {
            spriteRenderer = modeloJugador.GetComponent<SpriteRenderer>();
            animator = modeloJugador.GetComponent<Animator>();
        }
    }

    void Update()
    {
        if (estaMuerto) return; // muerto: ignoramos todos los controles

        RegenerarVida();
        ChequearSuelo();
        BuscarSubjefePoseible();
        ManejarInputPosesion();

        if (!estaPoseyendo)
        {
            ManejarInputAtaque();
            ManejarInputSalto();
        }
        else
        {
            ManejarInputAtaqueSubjefe();
            ManejarInputSaltoSubjefe();
        }
    }

    void RegenerarVida()
    {
        if (vidaActual >= vidaMaxima) return;

        if (Time.time >= tiempoUltimaRegeneracion + intervaloRegeneracion)
        {
            tiempoUltimaRegeneracion = Time.time;
            vidaActual++;
            vidaActual = Mathf.Min(vidaActual, vidaMaxima);
        }
    }

    void ChequearSuelo()
    {
        enSuelo = Physics2D.Raycast(transform.position, Vector2.down, distanciaChequeoSuelo, capaSuelo);

        if (animator != null)
        {
            animator.SetBool("EnSuelo", enSuelo);
        }
    }

    void FixedUpdate()
    {
        if (estaMuerto)
        {
            // dejamos que la gravedad lo siga afectando, pero sin movimiento horizontal
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        bool subiendoEscalera = false;
        bool sostenidoEnEscalera = false;

        if (tocandoEscalera)
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            if (horizontal != 0 && vertical != 0)
            {
                subiendoEscalera = true; // escala en diagonal
            }
            else if (horizontal == 0 && vertical == 0)
            {
                sostenidoEnEscalera = true; // parado quieto: no se cae
            }
            // si aprieta solo horizontal (sin vertical), sigue caminando derecho normal
        }

        ActualizarColisionConPlataforma(subiendoEscalera || sostenidoEnEscalera);

        // el movimiento con fisica va en FixedUpdate, no en Update
        if (estaPoseyendo)
        {
            if (subiendoEscalera)
            {
                MoverSubjefeEnEscalera();
            }
            else if (sostenidoEnEscalera)
            {
                SostenerSubjefeEnEscalera();
            }
            else
            {
                if (subjefePoseido != null)
                {
                    subjefePoseido.CambiarTipoDeCuerpo(RigidbodyType2D.Dynamic);
                }
                MoverSubjefe();
            }
        }
        else if (subiendoEscalera)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            MoverEnEscaleraDiagonal();
        }
        else if (sostenidoEnEscalera)
        {
            // se queda flotando en el lugar, sin gravedad, hasta que se mueva o suelte
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }
        else
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            MoverJugador();
        }
    }

    // la plataforma que la escalera atraviesa es solida siempre,
    // EXCEPTO mientras estamos escalando o sostenidos sobre la escalera.
    // Ignora colision con el collider que este activo en ese momento (jugador o subjefe poseido)
    void ActualizarColisionConPlataforma(bool debeIgnorar)
    {
        if (escaleraActual == null || escaleraActual.plataformaQueAtraviesa == null) return;

        Collider2D colliderActivo = colisionador;

        if (estaPoseyendo && subjefePoseido != null)
        {
            colliderActivo = subjefePoseido.ObtenerCollider();
        }

        if (colliderActivo != null)
        {
            Physics2D.IgnoreCollision(colliderActivo, escaleraActual.plataformaQueAtraviesa, debeIgnorar);
        }
    }

    void MoverEnEscaleraDiagonal()
    {
        Vector2 direccionRampa = escaleraActual.transform.right;
        float vertical = Input.GetAxisRaw("Vertical");

        // si el vertical es negativo (S), invertimos para bajar la rampa
        float sentido = vertical > 0 ? 1f : -1f;

        rb.linearVelocity = direccionRampa.normalized * velocidad * sentido;
    }

    // version para cuando se esta controlando al subjefe poseido
    void MoverSubjefeEnEscalera()
    {
        if (subjefePoseido == null || escaleraActual == null) return;

        subjefePoseido.CambiarTipoDeCuerpo(RigidbodyType2D.Dynamic);

        Vector2 direccionRampa = escaleraActual.transform.right;
        float vertical = Input.GetAxisRaw("Vertical");
        float sentido = vertical > 0 ? 1f : -1f;

        subjefePoseido.MoverEnDireccion(direccionRampa.normalized * subjefePoseido.velocidad * sentido);

        // mantenemos al jugador invisible sincronizado con el subjefe
        transform.position = subjefePoseido.transform.position;
    }

    void SostenerSubjefeEnEscalera()
    {
        if (subjefePoseido == null) return;

        subjefePoseido.CambiarTipoDeCuerpo(RigidbodyType2D.Kinematic);
        subjefePoseido.MoverEnDireccion(Vector2.zero);

        transform.position = subjefePoseido.transform.position;
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        EscaleraDiagonal escalera = otro.GetComponent<EscaleraDiagonal>();
        if (escalera != null)
        {
            tocandoEscalera = true;
            escaleraActual = escalera;
        }
    }

    void OnTriggerExit2D(Collider2D otro)
    {
        EscaleraDiagonal escalera = otro.GetComponent<EscaleraDiagonal>();
        if (escalera != null && escalera == escaleraActual)
        {
            tocandoEscalera = false;

            // al salir, nos aseguramos de dejar la colision restaurada (solida)
            // tanto para el jugador como para el subjefe, por si estaba poseido
            if (escalera.plataformaQueAtraviesa != null)
            {
                Physics2D.IgnoreCollision(colisionador, escalera.plataformaQueAtraviesa, false);

                if (subjefePoseido != null)
                {
                    Collider2D colliderSubjefe = subjefePoseido.ObtenerCollider();
                    if (colliderSubjefe != null)
                    {
                        Physics2D.IgnoreCollision(colliderSubjefe, escalera.plataformaQueAtraviesa, false);
                    }
                }
            }

            escaleraActual = null;
        }
    }

    void MoverJugador()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        rb.linearVelocity = new Vector2(horizontal * velocidad, rb.linearVelocity.y);

        if (horizontal != 0)
        {
            direccion = horizontal > 0 ? 1 : -1;
        }

        if (animator != null)
        {
            animator.SetBool("Caminando", horizontal != 0);
        }

        // volteamos el sprite segun hacia donde estamos mirando
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = direccion < 0;
        }
    }

    void MoverSubjefe()
    {
        if (subjefePoseido == null) return;

        float horizontal = Input.GetAxisRaw("Horizontal");
        subjefePoseido.Mover(horizontal);

        // mantenemos al jugador invisible en la misma posicion del subjefe,
        // asi los enemigos y otros sistemas que lo buscan por posicion funcionan bien
        transform.position = subjefePoseido.transform.position;
    }

    void ManejarInputSalto()
    {
        if (Input.GetKeyDown(KeyCode.Space) && enSuelo)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSalto);
        }
    }

    void ManejarInputSaltoSubjefe()
    {
        if (subjefePoseido == null) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            subjefePoseido.Saltar();
        }
    }

    void ManejarInputAtaque()
    {
        // atacamos con click izquierdo
        if (Input.GetMouseButtonDown(0))
        {
            Atacar();
        }
    }

    void ManejarInputAtaqueSubjefe()
    {
        if (subjefePoseido == null) return;

        // mismo click, pero ataca el subjefe (por ejemplo para romper puertas)
        if (Input.GetMouseButtonDown(0))
        {
            subjefePoseido.Atacar();
        }
    }

    void Atacar()
    {
        MostrarGolpeVisual();

        if (animator != null)
        {
            animator.SetTrigger("Atacando");
        }

        // buscamos en un radio generoso primero, y despues filtramos por la
        // distancia REAL entre bordes de collider (no entre centros) - asi un
        // objetivo grande es mas facil de alcanzar sin inflar el rango contra
        // objetivos chicos
        float radioBusqueda = rangoAtaque + 5f;
        Collider2D[] candidatos = Physics2D.OverlapCircleAll(transform.position, radioBusqueda);

        foreach (Collider2D candidato in candidatos)
        {
            if (candidato == colisionador) continue; // nuestro propio collider, ignorar

            ColliderDistance2D distancia = candidato.Distance(colisionador);
            if (distancia.distance > rangoAtaque) continue; // el borde esta muy lejos todavia

            Subjefe subjefe = candidato.GetComponent<Subjefe>();
            if (subjefe != null)
            {
                subjefe.RecibirDaño(daño);
                Debug.Log("Le pegaste al subjefe: " + subjefe.name);
            }

            Enemigo enemigo = candidato.GetComponent<Enemigo>();
            if (enemigo != null)
            {
                enemigo.RecibirDaño(daño);
                Debug.Log("Le pegaste al enemigo: " + enemigo.name);
            }
        }
    }

    void MostrarGolpeVisual()
    {
        GameObject golpe = new GameObject("GolpeVisual");
        golpe.transform.SetParent(transform); // hijo del jugador: se mueve junto con el
        golpe.transform.localPosition = new Vector3(direccion * distanciaGolpe, 0, 0);
        golpe.transform.localScale = Vector3.one * tamañoGolpe;

        SpriteRenderer sr = golpe.AddComponent<SpriteRenderer>();
        sr.sprite = spriteGolpe;
        sr.color = colorGolpe;
        sr.sortingOrder = 10; // para que se vea por encima de todo

        Destroy(golpe, duracionGolpe);
    }

    void BuscarSubjefePoseible()
    {
        // mientras estamos poseyendo, no hace falta buscar otro
        if (estaPoseyendo) return;

        // buscamos todos los subjefes en la escena y vemos si alguno esta poseible
        Subjefe[] subjefes = FindObjectsByType<Subjefe>(FindObjectsSortMode.None);
        subjefeCercano = null;

        foreach (Subjefe subjefe in subjefes)
        {
            if (subjefe.esPoseible)
            {
                subjefeCercano = subjefe;
                break;
            }
        }

        // mostramos u ocultamos el boton segun corresponda
        if (botonPosesion != null)
        {
            botonPosesion.SetActive(subjefeCercano != null);
        }
    }

    void ManejarInputPosesion()
    {
        // salir de la posesion con E de nuevo
        if (estaPoseyendo)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                DejarDePoseer();
            }
            return;
        }

        if (subjefeCercano == null) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            PoseerSubjefe(subjefeCercano);
        }
    }

    void PoseerSubjefe(Subjefe subjefe)
    {
        estaPoseyendo = true;
        subjefePoseido = subjefe;
        subjefe.estaPoseido = true; // asi el subjefe sabe que no debe atacar solo

        // revivimos al subjefe con vida completa al poseerlo
        subjefe.vidaActual = subjefe.vidaMaxima;

        Debug.Log("Poseyendo al subjefe: " + subjefe.name);

        // frenamos al jugador y apagamos su fisica para que no pelee
        // con la sincronizacion de posicion mientras esta escondido
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        // el collider pasa a ser Trigger: no empuja fisicamente al subjefe,
        // pero sigue siendo detectable por enemigos, spawners, etc.
        if (colisionador != null)
        {
            colisionador.isTrigger = true;
        }

        // escondemos al jugador mientras posee (sigue existiendo, solo no se ve ni se mueve solo)
        if (modeloJugador != null)
        {
            modeloJugador.SetActive(false);
        }

        if (botonPosesion != null)
        {
            botonPosesion.SetActive(false);
        }

        if (camara != null)
        {
            camara.CambiarObjetivo(subjefe.transform);
        }
    }

    public void DejarDePoseer()
    {
        estaPoseyendo = false;

        Debug.Log("Dejaste de poseer al subjefe, desaparece");

        // movemos al jugador a donde estaba el subjefe antes de que desaparezca,
        // y le devolvemos la fisica normal con velocidad limpia
        if (subjefePoseido != null)
        {
            transform.position = subjefePoseido.transform.position;
            Destroy(subjefePoseido.gameObject);
        }

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = Vector2.zero;

        if (colisionador != null)
        {
            colisionador.isTrigger = false;
        }

        if (modeloJugador != null)
        {
            modeloJugador.SetActive(true);
        }

        if (camara != null)
        {
            camara.CambiarObjetivo(transform);
        }

        subjefePoseido = null;
    }

    private bool estaMuerto = false;

    public void RecibirDaño(int cantidad)
    {
        // mientras poseemos al subjefe, el daño le pega a el, no al jugador
        if (estaPoseyendo && subjefePoseido != null)
        {
            subjefePoseido.RecibirDaño(cantidad);

            if (subjefePoseido.vidaActual <= 0)
            {
                Debug.Log("El subjefe poseido se quedo sin vida, volviendo al jugador");
                DejarDePoseer();
            }

            return;
        }

        if (estaMuerto) return; // ya esta muerto, ignoramos mas daño

        vidaActual -= cantidad;
        vidaActual = Mathf.Max(vidaActual, 0);

        Debug.Log("Recibiste daño, vida restante: " + vidaActual);

        if (vidaActual <= 0)
        {
            Morir();
        }
    }

    void Morir()
    {
        estaMuerto = true;
        Debug.Log("GAME OVER - El jugador murio");

        // por si murio en medio de la escalera (donde queda en modo Kinematic sin gravedad),
        // forzamos que vuelva a Dynamic para que caiga normal al piso
        rb.bodyType = RigidbodyType2D.Dynamic;

        if (animator != null)
        {
            animator.SetTrigger("Muerto");
        }

        if (panelGameOver != null)
        {
            panelGameOver.SetActive(true);
        }

        // ya NO congelamos el tiempo, asi se alcanza a ver la animacion de muerte
    }

    // dibuja el rango de ataque y el raycast de suelo en el editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangoAtaque);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * distanciaChequeoSuelo);
    }
}