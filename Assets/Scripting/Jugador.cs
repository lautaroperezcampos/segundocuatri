using UnityEngine;

public class Jugador : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 5f;
    private Rigidbody2D rb;
    private int direccion = 1; // 1 = mirando a la derecha, -1 = mirando a la izquierda

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

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        BuscarSubjefePoseible();
        ManejarInputPosesion();

        if (!estaPoseyendo)
        {
            ManejarInputAtaque();
        }
    }

    void FixedUpdate()
    {
        // el movimiento con fisica va en FixedUpdate, no en Update
        if (estaPoseyendo)
        {
            MoverSubjefe();
        }
        else
        {
            MoverJugador();
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
    }

    void MoverSubjefe()
    {
        if (subjefePoseido == null) return;

        float horizontal = Input.GetAxisRaw("Horizontal");
        subjefePoseido.Mover(horizontal);
    }

    void ManejarInputAtaque()
    {
        // atacamos con click izquierdo
        if (Input.GetMouseButtonDown(0))
        {
            Atacar();
        }
    }

    void Atacar()
    {
        MostrarGolpeVisual();

        Subjefe[] subjefes = FindObjectsByType<Subjefe>(FindObjectsSortMode.None);

        foreach (Subjefe subjefe in subjefes)
        {
            float distancia = Vector2.Distance(transform.position, subjefe.transform.position);

            if (distancia <= rangoAtaque)
            {
                subjefe.RecibirDaño(daño);
                Debug.Log("Le pegaste al subjefe: " + subjefe.name);
            }
        }
    }

    void MostrarGolpeVisual()
    {
        GameObject golpe = new GameObject("GolpeVisual");
        golpe.transform.position = transform.position + new Vector3(direccion * distanciaGolpe, 0, 0);
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

        Debug.Log("Poseyendo al subjefe: " + subjefe.name);

        // frenamos al jugador para que no quede deslizando mientras esta escondido
        rb.linearVelocity = Vector2.zero;

        // escondemos al jugador mientras posee (sigue existiendo, solo no se ve ni se mueve solo)
        if (modeloJugador != null)
        {
            modeloJugador.SetActive(false);
        }

        if (botonPosesion != null)
        {
            botonPosesion.SetActive(false);
        }
    }

    public void DejarDePoseer()
    {
        estaPoseyendo = false;

        Debug.Log("Dejaste de poseer al subjefe, desaparece");

        // movemos al jugador a donde estaba el subjefe antes de que desaparezca
        if (subjefePoseido != null)
        {
            transform.position = subjefePoseido.transform.position;
            Destroy(subjefePoseido.gameObject);
        }

        if (modeloJugador != null)
        {
            modeloJugador.SetActive(true);
        }

        subjefePoseido = null;
    }

    // dibuja el rango de ataque en el editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangoAtaque);
    }
}