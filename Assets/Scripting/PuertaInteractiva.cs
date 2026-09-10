using UnityEngine;
using UnityEngine.SceneManagement;

// Puerta bloqueable: empieza cerrada (Bloquear()), y otro sistema (como
// ControladorSalaSubjefe) la desbloquea cuando corresponda. Una vez
// desbloqueada, el jugador entra parandose encima y apretando el boton A.
public class PuertaInteractiva : MonoBehaviour
{
    [Header("Escena a cargar")]
    public string nombreEscenaDestino;

    [Header("Icono de interactuar (opcional)")]
    public GameObject iconoInteractuar; // ej: un sprite del boton A flotando arriba de la puerta

    [Header("Referencias")]
    public SpriteRenderer spriteRenderer; // el sprite de la puerta, se muestra/oculta entera

    private bool jugadorCerca = false;
    private bool bloqueada = true;

    void Start()
    {
        Bloquear(); // arranca invisible y bloqueada hasta que se desbloquee
    }

    public void Bloquear()
    {
        bloqueada = true;

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        if (iconoInteractuar != null)
        {
            iconoInteractuar.SetActive(false);
        }
    }

    public void Desbloquear()
    {
        bloqueada = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        // si el jugador ya esta parado encima cuando se desbloquea, mostramos el icono de una
        if (jugadorCerca && iconoInteractuar != null)
        {
            iconoInteractuar.SetActive(true);
        }
    }

    void Update()
    {
        if (!jugadorCerca || bloqueada) return;

        // GetButtonDown("Interactuar") responde al boton Y del joystick Y a la tecla que le asignes
        if (Input.GetButtonDown("Interactuar"))
        {
            Entrar();
        }
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (otro.GetComponent<Jugador>() == null) return;

        jugadorCerca = true;

        if (!bloqueada && iconoInteractuar != null)
        {
            iconoInteractuar.SetActive(true);
        }
    }

    void OnTriggerExit2D(Collider2D otro)
    {
        if (otro.GetComponent<Jugador>() == null) return;

        jugadorCerca = false;

        if (iconoInteractuar != null)
        {
            iconoInteractuar.SetActive(false);
        }
    }

    void Entrar()
    {
        Debug.Log("PuertaInteractiva.Entrar() en '" + gameObject.name + "' - nombreEscenaDestino='" + nombreEscenaDestino + "' | escena actual='" + SceneManager.GetActiveScene().name + "' | hay TransicionEscena? " + (TransicionEscena.instancia != null));

        if (TransicionEscena.instancia != null)
        {
            TransicionEscena.instancia.IrAEscena(nombreEscenaDestino);
        }
        else
        {
            SceneManager.LoadScene(nombreEscenaDestino);
        }
    }
}