using UnityEngine;
using UnityEngine.Events;

// Poné este script en un GameObject con Collider2D marcado "Is Trigger".
public class Palanca : MonoBehaviour
{
    public ControladorPalancas controlador; // arrastra aca el controlador de las 4 palancas
    public int numeroPalanca; // 1, 2, 3 o 4 - le dice al controlador cual es esta (si no usas controlador, dejalo)

    [Header("Plataforma (opcional)")]
    public PlataformaMovil plataforma; // al bajar la palanca, esta plataforma se mueve
    public UnityEvent alActivar; // otros efectos opcionales (ej: ControladorOleadas -> Iniciar)

    [Header("Sprites")]
    public Sprite spriteNormal;  // palanca arriba (sin activar)
    public Sprite spriteBajada;  // palanca abajo (activada)
    public SpriteRenderer spriteRenderer; // opcional: si el sprite esta en un hijo, arrastralo aca

    private bool activada = false;

    void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        // arranca con el sprite normal
        if (spriteRenderer != null && spriteNormal != null)
        {
            spriteRenderer.sprite = spriteNormal;
        }
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (activada) return;

        // se activa tanto si la toca el Jugador normal como un Subjefe poseido
        bool esValido = otro.GetComponent<Jugador>() != null || otro.GetComponent<Subjefe>() != null;
        if (!esValido) return;

        activada = true;

        if (spriteRenderer != null)
        {
            if (spriteBajada != null)
            {
                spriteRenderer.sprite = spriteBajada; // palanca bajada
            }
            else
            {
                spriteRenderer.color = Color.gray; // si no cargaste sprite, se pone gris como antes
            }
        }

        if (plataforma != null)
        {
            plataforma.Bajar();
        }

        alActivar.Invoke();

        if (controlador != null)
        {
            controlador.NotificarPalancaActivada(numeroPalanca);
        }
    }
}