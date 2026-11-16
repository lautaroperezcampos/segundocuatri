using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Jugador : MonoBehaviour
{
    [Header("Vida")]
    public int vidaMaxima = 100;
    public int vidaActual;

    [Header("Caida al vacio")]
    public float limiteCaidaY = -20f; // si tu Position Y baja de esto, se considera "te caiste"

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

    [Header("Resbalar si cae arriba de un enemigo")]
    public float velocidadResbalar = 5f; // que tan fuerte se desliza hacia el costado
    public float duracionResbalar = 0.3f; // cuanto tiempo pierde el control mientras resbala
    private float tiempoFinResbalar = 0f;
    private float direccionResbalar = 0f;

    [Header("Bajar de plataformas (abajo + salto)")]
    public float tiempoIgnorarPlataforma = 0.4f; // cuanto tiempo se atraviesa antes de volver a ser solida
    private Collider2D plataformaActual; // la que esta pisando ahora mismo, si la hay

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
    public GameObject prefabParticulasGolpe; // opcional: un Particle System que sale cuando pegas
    public float duracionParticulasGolpe = 0.5f;

    [Header("Posesion")]
    public bool estaPoseyendo = false;
    private Subjefe subjefeCercano; // referencia al subjefe que esta en rango
    private Subjefe subjefePoseido; // el que estamos controlando ahora mismo

    [Header("Efectos")]
    public GameObject prefabParticulasPosesion; // arrastra el prefab del Particle System
    public float duracionParticulasPosesion = 2f; // por si el efecto no se autodestruye solo

    [Header("UI")]
    public GameObject prefabIconoPosesion; // arrastra aca el sprite/icono del boton (ej: "Y")
    public float alturaIconoPosesion = 1.5f; // que tan arriba del titan aparece
    private GameObject iconoInstanciado;
    public Image barraVidaHUD; // arrastra la Image (Fill) de la barra fija en pantalla
    public Color colorBarraJugador = Color.green;
    public Color colorBarraSubjefe = new Color(1f, 0.55f, 0f); // naranja, para diferenciar
    public bool pintarBarra = false; // false = respeta los colores del sprite (rojo del diseño). true = usa los colores de arriba
    public bool pintarAlPoseer = true; // la barra se pinta SOLO mientras poseyes un subjefe (despues vuelve al color normal)
    public Material materialTono; // OPCIONAL: material con el shader "UI/CambioDeTono". Si lo pones, en vez de tintar gira el tono (rojo -> naranja)
    public float tonoSubjefe = 0.08f; // cuanto gira el tono al poseer (0.08 = rojo a naranja)
    private Material materialTonoInstancia;
    public int segmentosBarra = 10; // cantidad de cuadraditos de la barra (0 = barra continua)
    public Sprite[] spritesBarraSubjefe; // OPCIONAL: las mismas 10 imagenes pero en naranja, para cuando poseyes un subjefe. Si esta vacio usa spritesBarra
    public Sprite[] spritesBarra; // OPCIONAL: 10 imagenes de la barra completa, en orden (1/10, 2/10 ... 10/10). Si las cargas, cambia de imagen segun la vida

    [Header("Referencias")]
    public GameObject modeloJugador; // el sprite/visual del jugador, para esconderlo al poseer
    public CamaraSeguimiento camara; // arrastra el Main Camera desde el Inspector
    public GameObject panelGameOver; // arrastra el panel de UI con el texto y el boton Reintentar
    public bool congelarAlCaerAlVacio = true; // si moris cayendo al vacio, se congela la pantalla de atras
    public bool congelarSiempreAlMorir = false; // tildalo para congelar en cualquier muerte (corta la animacion de muerte)
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private EfectoFlashDaño efectoFlash;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        colisionador = GetComponent<Collider2D>();
        vidaActual = vidaMaxima;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep; // si se duerme, OnCollisionStay2D deja de llamarse y no resbala

        if (modeloJugador != null)
        {
            spriteRenderer = modeloJugador.GetComponent<SpriteRenderer>();
            animator = modeloJugador.GetComponent<Animator>();
            efectoFlash = modeloJugador.GetComponent<EfectoFlashDaño>();
        }
    }

    void Update()
    {
        ActualizarBarraVidaHUD();

        if (estaMuerto) return; // muerto: ignoramos todos los controles

        // juego en pausa (timeScale = 0): ignoramos salto, ataque y posesion, para que
        // apretar A en el menu de pausa no haga saltar al personaje
        if (Time.timeScale == 0f) return;

        ChequearCaidaAlVacio();
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

    void ChequearCaidaAlVacio()
    {
        float posicionY = estaPoseyendo && subjefePoseido != null
            ? subjefePoseido.transform.position.y
            : transform.position.y;

        if (posicionY < limiteCaidaY)
        {
            Morir(true);
        }
    }

    void ActualizarBarraVidaHUD()
    {
        if (barraVidaHUD == null) return;

        float porcentaje;
        Color colorBarra;

        if (estaPoseyendo && subjefePoseido != null)
        {
            porcentaje = (float)subjefePoseido.vidaActual / subjefePoseido.vidaMaxima;
            colorBarra = colorBarraSubjefe;
        }
        else
        {
            porcentaje = (float)vidaActual / vidaMaxima;
            colorBarra = colorBarraJugador;
        }

        porcentaje = Mathf.Clamp01(porcentaje);

        // barra por cuadraditos: redondea para arriba, asi con 1 de vida ya se ve 1 cuadradito
        if (segmentosBarra > 0)
        {
            porcentaje = Mathf.Ceil(porcentaje * segmentosBarra) / segmentosBarra;
        }

        // modo imagenes: cambia el sprite completo segun el porcentaje de vida
        bool poseyendo = estaPoseyendo && subjefePoseido != null;
        Sprite[] juego = (poseyendo && spritesBarraSubjefe != null && spritesBarraSubjefe.Length > 0)
            ? spritesBarraSubjefe
            : spritesBarra;

        if (juego != null && juego.Length > 0)
        {
            int indice = Mathf.CeilToInt(Mathf.Clamp01(porcentaje) * juego.Length) - 1;
            indice = Mathf.Clamp(indice, 0, juego.Length - 1);
            barraVidaHUD.type = Image.Type.Simple;
            barraVidaHUD.sprite = juego[indice];
            barraVidaHUD.color = ColorFinalBarra(colorBarra);
            return;
        }

        barraVidaHUD.fillAmount = porcentaje;
        barraVidaHUD.color = ColorFinalBarra(colorBarra);
    }

    // aplica el material de cambio de tono (si hay) y devuelve el color con el que se tinta la Image
    Color ColorFinalBarra(Color colorBarra)
    {
        bool poseyendo = estaPoseyendo && subjefePoseido != null;

        if (materialTono != null)
        {
            if (materialTonoInstancia == null)
            {
                materialTonoInstancia = new Material(materialTono);
            }
            materialTonoInstancia.SetFloat("_Tono", tonoSubjefe);
            barraVidaHUD.material = (poseyendo && pintarAlPoseer) ? materialTonoInstancia : null;
            return pintarBarra ? colorBarra : Color.white;
        }

        if (poseyendo && pintarAlPoseer) return colorBarraSubjefe;
        return pintarBarra ? colorBarra : Color.white;
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
        RaycastHit2D golpe = Physics2D.Raycast(transform.position, Vector2.down, distanciaChequeoSuelo, capaSuelo);
        enSuelo = golpe.collider != null;
        plataformaActual = golpe.collider;

        // parado arriba de un enemigo/subjefe tambien cuenta como suelo (para poder caminar y saltar)
        if (!enSuelo && ResbalarEncima.BuscarDebajo(colisionador, true) != null)
        {
            enSuelo = true;
        }

        // apoyado en CUALQUIER collider solido que no este en la capa Suelo (ej: un collider suelto
        // arriba de un sprite): tambien cuenta como suelo, si no estas subiendo (asi no resetea el salto)
        if (!enSuelo && colisionador != null && rb.linearVelocity.y <= 0.1f && HayColliderSolidoDebajo())
        {
            enSuelo = true;
        }

        if (animator != null)
        {
            animator.SetBool("EnSuelo", enSuelo);
        }
    }

    bool HayColliderSolidoDebajo()
    {
        Bounds b = colisionador.bounds;
        Vector2 centro = new Vector2(b.center.x, b.min.y - 0.05f);
        Vector2 tam = new Vector2(b.size.x * 0.8f, 0.12f);

        foreach (Collider2D otro in Physics2D.OverlapBoxAll(centro, tam, 0f))
        {
            if (otro == colisionador || otro.isTrigger) continue;
            if (otro.attachedRigidbody == rb) continue;
            if (otro.GetComponentInParent<Proyectil>() != null) continue;
            return true;
        }
        return false;
    }

    void FixedUpdate()
    {
        if (estaMuerto)
        {
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
                subiendoEscalera = true;
            }
            else if (horizontal == 0 && vertical == 0)
            {
                sostenidoEnEscalera = true;
            }
        }

        ActualizarColisionConPlataforma(subiendoEscalera || sostenidoEnEscalera);

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
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }
        else
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            MoverJugador();
        }
    }

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
        float sentido = vertical > 0 ? 1f : -1f;

        rb.linearVelocity = direccionRampa.normalized * velocidad * sentido;
    }

    void MoverSubjefeEnEscalera()
    {
        if (subjefePoseido == null || escaleraActual == null) return;

        subjefePoseido.CambiarTipoDeCuerpo(RigidbodyType2D.Dynamic);

        Vector2 direccionRampa = escaleraActual.transform.right;
        float vertical = Input.GetAxisRaw("Vertical");
        float sentido = vertical > 0 ? 1f : -1f;

        subjefePoseido.MoverEnDireccion(direccionRampa.normalized * subjefePoseido.velocidad * sentido);

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
        // resbalando de arriba de un enemigo: sin control un instante, empujado hacia el costado
        if (Time.time < tiempoFinResbalar)
        {
            rb.linearVelocity = new Vector2(direccionResbalar * velocidadResbalar, rb.linearVelocity.y);
            return;
        }

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

        transform.position = subjefePoseido.transform.position;
    }

    void ManejarInputSalto()
    {
        if (!Input.GetButtonDown("Jump") || !enSuelo) return;

        float vertical = Input.GetAxisRaw("Vertical");

        // solo se atraviesa si la plataforma tiene Platform Effector 2D
        bool sePuedeAtravesar = plataformaActual != null && plataformaActual.GetComponentInParent<PlatformEffector2D>() != null;

        if (vertical < -0.5f && plataformaActual != null && sePuedeAtravesar)
        {
            StartCoroutine(IgnorarPlataformaTemporalmente(colisionador, plataformaActual));
        }
        else
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSalto);
        }
    }

    IEnumerator IgnorarPlataformaTemporalmente(Collider2D colliderPropio, Collider2D plataforma)
    {
        Physics2D.IgnoreCollision(colliderPropio, plataforma, true);
        yield return new WaitForSeconds(tiempoIgnorarPlataforma);

        if (plataforma != null && colliderPropio != null)
        {
            Physics2D.IgnoreCollision(colliderPropio, plataforma, false);
        }
    }

    void ManejarInputSaltoSubjefe()
    {
        if (subjefePoseido == null) return;

        if (!Input.GetButtonDown("Jump")) return;

        float vertical = Input.GetAxisRaw("Vertical");
        Collider2D plataformaSubjefe = subjefePoseido.ObtenerPlataformaActual();

        // solo se atraviesa si la plataforma tiene Platform Effector 2D
        bool sePuedeAtravesar = plataformaSubjefe != null && plataformaSubjefe.GetComponentInParent<PlatformEffector2D>() != null;

        if (vertical < -0.5f && plataformaSubjefe != null && sePuedeAtravesar)
        {
            StartCoroutine(IgnorarPlataformaTemporalmente(subjefePoseido.ObtenerCollider(), plataformaSubjefe));
        }
        else
        {
            subjefePoseido.Saltar();
        }
    }

    void ManejarInputAtaque()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            Atacar();
        }
    }

    void ManejarInputAtaqueSubjefe()
    {
        if (subjefePoseido == null) return;

        // toque rapido: dispara al instante hacia donde estas mirando/apuntando
        // con el stick en ese momento, sin necesitar soltar el boton
        if (Input.GetButtonDown("Fire1"))
        {
            subjefePoseido.Atacar();
        }

        // manteniendo apretado, engancha la mira a un objetivo si pasa cerca
        if (Input.GetButton("Fire1"))
        {
            subjefePoseido.MantenerApuntado();
        }

        // al soltar, si habia un objetivo enganchado, dispara ahi (respetando cooldown)
        if (Input.GetButtonUp("Fire1"))
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

        // el golpe pega SOLO cerca de donde aparece el sprite amarillo (adelante
        // tuyo, segun hacia donde mires) - no en un circulo grande alrededor tuyo
        Vector2 puntoGolpe = (Vector2)transform.position + new Vector2(direccion * distanciaGolpe, 0);
        Collider2D[] candidatos = Physics2D.OverlapCircleAll(puntoGolpe, rangoAtaque);

        bool pegoAAlgo = false;

        foreach (Collider2D candidato in candidatos)
        {
            if (candidato == colisionador) continue;

            Subjefe subjefe = candidato.GetComponent<Subjefe>();
            if (subjefe != null)
            {
                subjefe.RecibirDaño(daño);
                Debug.Log("Le pegaste al subjefe: " + subjefe.name);
                pegoAAlgo = true;
            }

            Enemigo enemigo = candidato.GetComponent<Enemigo>();
            if (enemigo != null)
            {
                enemigo.RecibirDaño(daño);
                Debug.Log("Le pegaste al enemigo: " + enemigo.name);
                pegoAAlgo = true;
            }
        }

        // las particulas SOLO aparecen si de verdad le pegaste a algo
        if (pegoAAlgo)
        {
            MostrarParticulasGolpe(puntoGolpe);
        }
    }

    void MostrarGolpeVisual()
    {
        GameObject golpe = new GameObject("GolpeVisual");
        golpe.transform.SetParent(transform);
        golpe.transform.localPosition = new Vector3(direccion * distanciaGolpe, 0, 0);
        golpe.transform.localScale = Vector3.one * tamañoGolpe;

        SpriteRenderer sr = golpe.AddComponent<SpriteRenderer>();
        sr.sprite = spriteGolpe;
        sr.color = colorGolpe;
        sr.sortingOrder = 10;

        Destroy(golpe, duracionGolpe);
    }

    void MostrarParticulasGolpe(Vector2 posicion)
    {
        if (prefabParticulasGolpe == null) return;

        GameObject particulas = Instantiate(prefabParticulasGolpe, posicion, Quaternion.identity);

        // giramos las particulas para que salgan hacia el lado que estas mirando
        if (direccion < 0)
        {
            particulas.transform.localScale = new Vector3(-1, 1, 1);
        }

        Destroy(particulas, duracionParticulasGolpe);
    }

    void BuscarSubjefePoseible()
    {
        if (estaPoseyendo) return;

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

        ActualizarIconoPosesion();
    }

    void ActualizarIconoPosesion()
    {
        if (subjefeCercano != null)
        {
            if (iconoInstanciado == null && prefabIconoPosesion != null)
            {
                iconoInstanciado = Instantiate(prefabIconoPosesion);
            }

            if (iconoInstanciado != null)
            {
                iconoInstanciado.transform.position = subjefeCercano.transform.position + Vector3.up * alturaIconoPosesion;
                iconoInstanciado.SetActive(true);
            }
        }
        else if (iconoInstanciado != null)
        {
            iconoInstanciado.SetActive(false);
        }
    }

    void ManejarInputPosesion()
    {
        if (estaPoseyendo)
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetButtonDown("Fire2"))
            {
                DejarDePoseer();
            }
            return;
        }

        if (subjefeCercano == null) return;

        if (Input.GetKeyDown(KeyCode.E) || Input.GetButtonDown("Fire2"))
        {
            PoseerSubjefe(subjefeCercano);
        }
    }

    void PoseerSubjefe(Subjefe subjefe)
    {
        estaPoseyendo = true;
        subjefePoseido = subjefe;
        subjefe.estaPoseido = true;

        subjefe.vidaActual = subjefe.vidaMaxima;

        Debug.Log("Poseyendo al subjefe: " + subjefe.name);

        // efecto de particulas justo en el momento/lugar de la posesion
        if (prefabParticulasPosesion != null)
        {
            GameObject particulas = Instantiate(prefabParticulasPosesion, subjefe.transform.position, Quaternion.identity);
            Destroy(particulas, duracionParticulasPosesion);
        }

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        if (colisionador != null)
        {
            colisionador.isTrigger = true;
        }

        if (modeloJugador != null)
        {
            modeloJugador.SetActive(false);
        }

        if (iconoInstanciado != null)
        {
            iconoInstanciado.SetActive(false);
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

        if (estaMuerto) return;

        vidaActual -= cantidad;
        vidaActual = Mathf.Max(vidaActual, 0);

        Debug.Log("Recibiste daño, vida restante: " + vidaActual);

        if (efectoFlash != null)
        {
            efectoFlash.Flashear();
        }

        if (vidaActual <= 0)
        {
            Morir();
        }
    }

    // muerte instantanea (columnas, trampas, etc): si estaba poseyendo, suelta al subjefe y muere el personaje
    public void MatarInstantaneo()
    {
        if (estaMuerto) return;

        if (estaPoseyendo)
        {
            DejarDePoseer();
        }

        vidaActual = 0;
        Morir();
    }

    void Morir(bool porCaidaAlVacio = false)
    {
        if (estaMuerto) return;
        estaMuerto = true;
        Debug.Log("GAME OVER - El jugador murio");

        rb.bodyType = RigidbodyType2D.Dynamic;

        if (animator != null)
        {
            animator.SetTrigger("Muerto");
        }

        if (panelGameOver != null)
        {
            panelGameOver.SetActive(true);
        }

        // congela todo para que no se vea al jugador cayendo mientras sale el menu de perdiste
        if ((porCaidaAlVacio && congelarAlCaerAlVacio) || congelarSiempreAlMorir)
        {
            Time.timeScale = 0f;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector2 puntoGolpe = (Vector2)transform.position + new Vector2(direccion * distanciaGolpe, 0);
        Gizmos.DrawWireSphere(puntoGolpe, rangoAtaque);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * distanciaChequeoSuelo);
    }
}