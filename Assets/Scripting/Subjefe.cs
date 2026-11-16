using UnityEngine;

public class Subjefe : MonoBehaviour
{
    [Header("Vida")]
    public int vidaMaxima = 100;
    public int vidaActual;

    [Header("Movimiento")]
    public float velocidad = 3f; // velocidad cuando el jugador lo POSEE
    public float velocidadPersecucion = 2f; // velocidad cuando NO esta poseido y persigue al jugador (mas baja = no se chocan entre ellos)
    protected int direccion = 1; // 1 = mirando a la derecha, -1 = mirando a la izquierda

    [Header("Persecucion (cuando NO esta poseido)")]
    public bool persigueAlJugador = true;
    public float rangoPersecucion = 6f; // a que distancia empieza a caminar hacia el jugador

    [Header("Ataque cuerpo a cuerpo contra el jugador (cuando NO esta poseido)")]
    public int dañoContraJugador = 10;
    public float rangoAtaqueJugador = 1.2f;
    public float cooldownAtaqueJugador = 1.5f;
    private float tiempoUltimoAtaqueJugador = -999f;

    [Header("Saltar a plataformas persiguiendo (cuando NO esta poseido y el jugador esta arriba)")]
    public bool saltarAlPerseguir = true;
    public float alturaMinimaParaSaltar = 1.2f; // el jugador tiene que estar al menos esto mas arriba
    public float alturaMaximaParaSaltar = 3f; // si esta mas arriba que esto, no intenta (no llegaria)
    public float margenHorizontalParaSaltar = 3f; // que tan cerca en X del jugador para decidir saltar
    public float cooldownSaltoPersecucion = 1.5f;
    private float tiempoUltimoSaltoPersecucion = -999f;

    [Header("Resbalar si cae arriba del jugador (cuando NO esta poseido)")]
    public float velocidadResbalar = 4f; // que tan fuerte se desliza hacia el costado
    public float duracionResbalar = 0.4f; // cuanto tiempo deja de perseguir mientras resbala
    private float tiempoFinResbalar = 0f;
    private float direccionResbalar = 0f;

    [Header("Salto")]
    public float fuerzaSalto = 8f;
    public float distanciaChequeoSuelo = 0.6f; // ajustar segun el alto del sprite
    public LayerMask capaSuelo; // elegi que layer cuenta como "suelo" en el Inspector
    protected bool enSuelo = false;
    private Collider2D plataformaActual; // la que esta pisando ahora mismo, si la hay

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

    [Header("Tiempo de gracia al aparecer")]
    public float tiempoInactivoAlAparecer = 1.5f; // no ataca ni persigue durante estos segundos al spawnear
    private float tiempoSpawn;

    [Header("Orientacion del sprite")]
    public bool spriteMiraALaIzquierda = false; // tildalo SOLO si el sprite original mira hacia la izquierda (si no, hace moonwalk)

    [Header("Referencias")]
    public GameObject modeloVisual; // el sprite/hijo separado, para escalarlo sin tocar el collider

    protected Transform jugador;
    protected Rigidbody2D rb;
    protected Animator animator;
    private SpriteRenderer spriteRenderer;
    private EfectoFlashDaño efectoFlash;
    private Collider2D colisionadorPropio;
    private Collider2D colisionadorJugador;

    protected virtual void Start()
    {
        tiempoSpawn = Time.time;
        vidaActual = vidaMaxima;
        rb = GetComponent<Rigidbody2D>();
        colisionadorPropio = GetComponent<Collider2D>();

        // evita que el subjefe se de vuelta (rote) al chocar o caer arriba de algo
        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.rotation = 0f;
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        }

        // el Animator/SpriteRenderer pueden estar en el mismo objeto, o en el hijo "modeloVisual"
        GameObject fuenteVisual = modeloVisual != null ? modeloVisual : gameObject;
        animator = fuenteVisual.GetComponent<Animator>();
        spriteRenderer = fuenteVisual.GetComponent<SpriteRenderer>();
        efectoFlash = fuenteVisual.GetComponent<EfectoFlashDaño>();

        // sincroniza el espejado del sprite con la direccion inicial (antes quedaba como lo dibujaste
        // hasta el primer movimiento, y el golpe/ataque ya contaba la direccion como "derecha")
        ActualizarDireccion(direccion);

        // buscamos al jugador por su tag (asegurate de que el jugador tenga el tag "Player")
        GameObject jugadorObj = GameObject.FindGameObjectWithTag("Player");
        if (jugadorObj != null)
        {
            jugador = jugadorObj.transform;
            colisionadorJugador = jugadorObj.GetComponent<Collider2D>();
        }
    }

    // si hay otro personaje debajo de los pies, resbala hacia el costado
    // (poseido: ignora al Jugador, que esta escondido, y Mover() aplica el empujon)
    void ChequearResbalarEncima()
    {
        // poseido: no resbala a la fuerza, asi podes caminar y saltar arriba de enemigos
        // si es Kinematic (ej: el jefe subiendo/bajando) NO resbala: la velocidad lo dejaria yendose para siempre
        if (estaPoseido || rb == null || rb.bodyType != RigidbodyType2D.Dynamic || Time.time < tiempoFinResbalar) return;

        Collider2D debajo = ResbalarEncima.BuscarDebajo(colisionadorPropio, estaPoseido);
        if (debajo == null) return;

        direccionResbalar = ResbalarEncima.ElegirLado(transform, debajo.transform);
        tiempoFinResbalar = Time.time + duracionResbalar;
        if (!estaPoseido)
        {
            rb.linearVelocity = new Vector2(direccionResbalar * velocidadResbalar, rb.linearVelocity.y);
        }
    }

    // si esta poseido y cayo arriba de un enemigo/subjefe, lo empuja al costado (sin control un instante)
    protected bool AplicarResbalarPoseido()
    {
        if (!estaPoseido || Time.time >= tiempoFinResbalar) return false;
        rb.linearVelocity = new Vector2(direccionResbalar * velocidadResbalar, rb.linearVelocity.y);
        return true;
    }

    public virtual void Mover(float horizontal)
    {
        if (AplicarResbalarPoseido()) return;

        float velocidadActual = estaPoseido ? velocidad : velocidadPersecucion;
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

    protected void ActualizarDireccion(int nuevaDireccion)
    {
        direccion = nuevaDireccion;

        if (spriteRenderer != null)
        {
            // si el sprite original mira a la izquierda, hay que voltearlo al ir a la derecha
            // (y viceversa) - asi cada prefab se configura segun como dibujaste su sprite
            bool voltear = spriteMiraALaIzquierda ? direccion > 0 : direccion < 0;

            // si el prefab (o un padre) tiene escala X negativa, el espejado ya viene invertido: lo compensamos
            if (spriteRenderer.transform.lossyScale.x < 0f) voltear = !voltear;

            spriteRenderer.flipX = voltear;
        }
    }

    public virtual void Saltar()
    {
        if (enSuelo)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSalto);
        }
    }

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

    public Collider2D ObtenerPlataformaActual()
    {
        return plataformaActual;
    }

    public virtual void Atacar()
    {
        MostrarGolpeVisual();

        if (animator != null)
        {
            animator.SetTrigger("Atacando");
        }

        Collider2D[] impactos = Physics2D.OverlapCircleAll(transform.position, rangoAtaque);

        foreach (Collider2D impacto in impactos)
        {
            if (impacto.gameObject == gameObject) continue;

            Puerta puerta = impacto.GetComponent<Puerta>();
            if (puerta != null)
            {
                puerta.RecibirDaño(dañoAtaque);
            }

            Enemigo enemigo = impacto.GetComponent<Enemigo>();
            if (enemigo != null)
            {
                enemigo.MorirInstantaneo();
            }

            Subjefe otroSubjefe = impacto.GetComponent<Subjefe>();
            if (otroSubjefe != null)
            {
                otroSubjefe.RecibirDaño(dañoAtaque);
            }
        }
    }

    public virtual void MantenerApuntado()
    {
    }

    protected void MostrarGolpeVisual()
    {
        GameObject golpe = new GameObject("GolpeVisualSubjefe");
        golpe.transform.SetParent(transform);
        // posicion en el MUNDO (no local): asi sale siempre hacia donde camina, aunque el prefab tenga escala negativa
        golpe.transform.position = transform.position + new Vector3(direccion * distanciaGolpe, 0, 0);
        golpe.transform.localScale = Vector3.one * tamañoGolpe;

        SpriteRenderer sr = golpe.AddComponent<SpriteRenderer>();
        sr.sprite = spriteGolpe;
        sr.color = colorGolpe;
        sr.sortingOrder = 10;

        Destroy(golpe, duracionGolpe);
    }

    // seguro extra: aunque una animacion, la fisica o el prefab intenten girar al subjefe,
    // lo dejamos siempre derecho (rotacion 0) en cada frame
    protected virtual void LateUpdate()
    {
        if (estaMuerto) return;

        transform.rotation = Quaternion.identity;

        if (modeloVisual != null)
        {
            modeloVisual.transform.localRotation = Quaternion.identity;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.flipY = false;
        }
    }

    protected virtual void Update()
    {
        if (estaMuerto)
        {
            esPoseible = false;
            return;
        }

        ChequearResbalarEncima();

        Vector2 origenRaycast = colisionadorPropio != null
            ? new Vector2(colisionadorPropio.bounds.center.x, colisionadorPropio.bounds.min.y)
            : (Vector2)transform.position;

        RaycastHit2D golpeSuelo = Physics2D.Raycast(origenRaycast, Vector2.down, distanciaChequeoSuelo, capaSuelo);
        enSuelo = golpeSuelo.collider != null;
        plataformaActual = golpeSuelo.collider;

        // poseido y parado arriba de un enemigo/subjefe: cuenta como suelo
        if (!enSuelo && estaPoseido && ResbalarEncima.BuscarDebajo(colisionadorPropio, true) != null)
        {
            enSuelo = true;
        }

        if (jugador == null) return;

        if (!estaPoseido && persigueAlJugador && EstaListoParaActuar())
        {
            ManejarPersecucion();
            IntentarAtacarAlJugador();
        }

        bool enRango;

        if (colisionadorPropio != null && colisionadorJugador != null)
        {
            float distanciaBordes = colisionadorPropio.Distance(colisionadorJugador).distance;
            enRango = distanciaBordes <= rangoDeteccion;
        }
        else
        {
            float distancia = Vector2.Distance(transform.position, jugador.position);
            enRango = distancia <= rangoDeteccion;
        }

        bool vidaBaja = vidaActual <= vidaMaxima * porcentajeVidaParaPoseer;

        esPoseible = enRango && vidaBaja;

        if (vidaActual < vidaMaxima)
        {
            Debug.Log(gameObject.name + " | vidaActual=" + vidaActual + " / vidaMaxima=" + vidaMaxima
                + " | umbral=" + (vidaMaxima * porcentajeVidaParaPoseer)
                + " | vidaBaja=" + vidaBaja + " | enRango=" + enRango + " | esPoseible=" + esPoseible);
        }
    }

    // true cuando ya paso el tiempo de gracia desde que aparecio - lo usan tanto
    // la persecucion/ataque normal como el disparo automatico del SubjefeDisparador
    protected bool EstaListoParaActuar()
    {
        return Time.time >= tiempoSpawn + tiempoInactivoAlAparecer;
    }

    // true si el centro de este subjefe esta mas arriba que el del otro collider (no depende del signo de la normal)
    bool EstaArribaDe(Collider2D otro)
    {
        float miY = colisionadorPropio != null ? colisionadorPropio.bounds.center.y : transform.position.y;
        return otro != null && miY > otro.bounds.center.y;
    }

    // si el subjefe esta parado ENCIMA del jugador (cayo arriba de su cabeza), lo empujamos
    // hacia el costado para que resbale y no se quede pegado ahi
    void OnCollisionStay2D(Collision2D colision)
    {
        if (estaMuerto || rb == null) return;
        // vale para el jugador, otros subjefes y minions (si caes arriba de un golpeador, resbalas)
        GameObject otro = colision.gameObject;
        if (otro.GetComponent<Jugador>() == null
            && otro.GetComponentInParent<Subjefe>() == null
            && otro.GetComponentInParent<Enemigo>() == null) return;

        bool esJugadorOtro = otro.GetComponent<Jugador>() != null;

        // poseido: el Jugador esta escondido, solo nos importa resbalar de arriba de un enemigo/subjefe
        if (estaPoseido && esJugadorOtro) return;

        foreach (ContactPoint2D contacto in colision.contacts)
        {
            // lado a lado con otro enemigo/subjefe: los separamos para que no se superpongan
            if (!estaPoseido && !esJugadorOtro && Mathf.Abs(contacto.normal.x) > 0.5f)
            {
                float ladoSep = transform.position.x >= colision.transform.position.x ? 1f : -1f;
                rb.position += new Vector2(ladoSep * 2f * Time.fixedDeltaTime, 0f);
                return;
            }

        }
    }

    // si el jugador esta arriba (en otra plataforma) y bastante cerca en X, salta para buscarlo.
    // usa Saltar(), asi el SubjefeVolador aletea en vez de saltar
    void ConsiderarSaltarHaciaJugador()
    {
        if (!saltarAlPerseguir) return;
        if (Time.time < tiempoUltimoSaltoPersecucion + cooldownSaltoPersecucion) return;

        float alturaRelativa = jugador.position.y - transform.position.y;
        bool jugadorArriba = alturaRelativa >= alturaMinimaParaSaltar && alturaRelativa <= alturaMaximaParaSaltar;
        bool cercaEnX = Mathf.Abs(jugador.position.x - transform.position.x) <= margenHorizontalParaSaltar;

        if (jugadorArriba && cercaEnX)
        {
            tiempoUltimoSaltoPersecucion = Time.time;
            Saltar();
        }
    }

    void ManejarPersecucion()
    {
        // mientras resbala, no lo dejamos perseguir ni frenar
        if (Time.time < tiempoFinResbalar) return;

        ConsiderarSaltarHaciaJugador();

        float distancia = ObtenerDistanciaAlJugador();

        if (distancia <= rangoPersecucion && distancia > rangoAtaqueJugador)
        {
            float horizontal = jugador.position.x > transform.position.x ? 1f : -1f;
            Mover(horizontal);
        }
        else
        {
            Mover(0f);
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

    protected float ObtenerDistanciaAlJugador()
    {
        if (colisionadorPropio != null && colisionadorJugador != null)
        {
            return colisionadorPropio.Distance(colisionadorJugador).distance;
        }

        return Vector2.Distance(transform.position, jugador.position);
    }

    public virtual void RecibirDaño(int cantidad)
    {
        if (estaMuerto) return;

        vidaActual -= cantidad;
        vidaActual = Mathf.Max(vidaActual, 0);

        Debug.Log(gameObject.name + " RecibirDaño(" + cantidad + ") llamado. vidaActual ahora=" + vidaActual);

        if (efectoFlash != null)
        {
            efectoFlash.Flashear();
        }

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
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        if (animator != null)
        {
            animator.SetTrigger("Muerto");
        }

        if (colisionadorPropio != null)
        {
            colisionadorPropio.isTrigger = true;
        }

        Destroy(gameObject, tiempoAntesDeDesaparecer);
    }

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