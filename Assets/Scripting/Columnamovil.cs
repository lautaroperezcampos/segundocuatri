using UnityEngine;

// Columna (o par de columnas) que sube y baja sola, como los tubos del Flappy Bird.
// Tambien sirve para columnas QUIETAS: destildas "Se Mueve" y solo hace de columna mortal.
// Ponelo en el objeto PADRE de las columnas (ej: "ParColumnas" con la columna de arriba y la de abajo
// como hijos, cada una con su Collider2D). Se mueve sola con un Rigidbody2D Kinematic.
// Si el jugador (o el Subjefe que esta poseyendo) la toca, recibe dano o muere.
[RequireComponent(typeof(Rigidbody2D))]
public class ColumnaMovil : MonoBehaviour
{
    [Header("Movimiento vertical")]
    public bool seMueve = true; // destildalo para una columna normal (quieta) que igual te mata
    public float distancia = 3f; // cuanto sube Y cuanto baja desde su posicion inicial
    public float velocidad = 2f; // velocidad maxima de subida/bajada
    public bool movimientoSuave = true; // true: frena en los extremos; false: velocidad constante (mas "Flappy")
    [Range(0f, 1f)]
    public float desfase = 0f; // 0 a 1: para que no suban y bajen todas juntas (0.5 = justo al reves)
    public bool desfaseAleatorio = false; // si lo tildas, ignora "desfase" y elige uno al azar al empezar

    [Header("Dano al tocarla")]
    public bool matarAlTocar = true; // true: te mata al instante (como el Flappy). Destildalo para que solo te saque vida
    public int dañoAlTocar = 20; // solo se usa si "Matar Al Tocar" esta destildado
    public float cooldownDaño = 1f; // segundos entre un golpe y el siguiente
    private float tiempoUltimoDaño = -999f;

    private Rigidbody2D rb;
    private Vector2 posicionInicial;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic; // se mueve por codigo, no la empuja nada
    }

    void Start()
    {
        posicionInicial = rb.position;

        if (desfaseAleatorio)
        {
            desfase = Random.value;
        }
    }

    void FixedUpdate()
    {
        if (!seMueve || distancia <= 0f) return;

        float desplazamiento;

        if (movimientoSuave)
        {
            // seno: la velocidad maxima en el medio es "velocidad"
            float t = Time.time * (velocidad / distancia) + desfase * Mathf.PI * 2f;
            desplazamiento = Mathf.Sin(t) * distancia;
        }
        else
        {
            // ida y vuelta a velocidad constante
            float t = Time.time * velocidad + desfase * distancia * 4f;
            desplazamiento = Mathf.PingPong(t, distancia * 2f) - distancia;
        }

        rb.MovePosition(new Vector2(posicionInicial.x, posicionInicial.y + desplazamiento));
    }

    // ---- dano: funciona si las columnas tienen collider normal o en "Is Trigger" ----

    void OnCollisionEnter2D(Collision2D colision) { Tocar(colision.gameObject); }
    void OnCollisionStay2D(Collision2D colision) { Tocar(colision.gameObject); }
    void OnTriggerEnter2D(Collider2D otro) { Tocar(otro.gameObject); }
    void OnTriggerStay2D(Collider2D otro) { Tocar(otro.gameObject); }

    void Tocar(GameObject otro)
    {
        if (Time.time < tiempoUltimoDaño + cooldownDaño) return;

        Jugador jugador = otro.GetComponentInParent<Jugador>();

        // si esta poseyendo un Subjefe, el que choca es el Subjefe: el dano va igual al Jugador,
        // que se lo pasa al Subjefe poseido
        if (jugador == null)
        {
            Subjefe subjefe = otro.GetComponentInParent<Subjefe>();
            if (subjefe != null && subjefe.estaPoseido)
            {
                jugador = FindFirstObjectByType<Jugador>();
            }
        }

        if (jugador == null) return;

        tiempoUltimoDaño = Time.time;

        if (matarAlTocar)
        {
            jugador.MatarInstantaneo();
        }
        else
        {
            jugador.RecibirDaño(dañoAlTocar);
        }
    }
}