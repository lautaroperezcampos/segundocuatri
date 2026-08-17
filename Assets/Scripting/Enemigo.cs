using UnityEngine;

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

    private Transform jugador;
    private Jugador scriptJugador;
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    [Header("Referencias")]
    public GameObject modeloVisual; // el sprite/hijo separado (Animator y SpriteRenderer viven ahi)

    [HideInInspector]
    public SpawnerEnemigos spawner; // asignado automaticamente por el spawner al crearlo

    void Start()
    {
        vidaActual = vidaMaxima;
        rb = GetComponent<Rigidbody2D>();

        GameObject fuenteVisual = modeloVisual != null ? modeloVisual : gameObject;
        animator = fuenteVisual.GetComponent<Animator>();
        spriteRenderer = fuenteVisual.GetComponent<SpriteRenderer>();

        GameObject jugadorObj = GameObject.FindGameObjectWithTag("Player");
        if (jugadorObj != null)
        {
            jugador = jugadorObj.transform;
            scriptJugador = jugadorObj.GetComponent<Jugador>();
        }
    }

    void Update()
    {
        if (jugador == null) return;

        float distancia = Vector2.Distance(transform.position, jugador.position);

        // IA simple: si el jugador esta cerca, lo persigue caminando hacia el
        if (distancia <= rangoDeteccion)
        {
            Perseguir();
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

            if (animator != null)
            {
                animator.SetBool("Caminando", false);
            }
        }

        // si esta lo bastante cerca, intenta atacar (con cooldown para no pegar cada frame)
        if (distancia <= rangoAtaque)
        {
            IntentarAtacar();
        }
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