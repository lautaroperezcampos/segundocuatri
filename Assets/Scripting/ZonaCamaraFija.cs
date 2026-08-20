using UnityEngine;

// Poné este script en un GameObject con un Collider2D marcado como "Is Trigger",
// que cubra el area de la pelea (por ejemplo la arena de un Titan).
public class ZonaCamaraFija : MonoBehaviour
{
    [Header("Punto donde se centra la camara")]
    public Transform puntoCamaraFija; // arrastra un GameObject vacio ubicado en el centro de la arena

    [Header("Zoom (opcional)")]
    public bool cambiarZoom = false;
    public float sizeZoom = 8f; // el "Orthographic Size" que va a tener la camara en esta zona

    [Header("Enemigos (opcional)")]
    public SpawnerEnemigos spawnerEnemigos; // arrastra aca el Spawner de la arena, si esta zona tiene que activarlo

    private CamaraSeguimiento camara;

    void Start()
    {
        camara = Object.FindFirstObjectByType<CamaraSeguimiento>();
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (otro.GetComponent<Jugador>() == null) return;

        if (camara != null && puntoCamaraFija != null)
        {
            float size = cambiarZoom ? sizeZoom : -1f;
            camara.FijarCamara(puntoCamaraFija.position, size);
        }

        if (spawnerEnemigos != null)
        {
            spawnerEnemigos.ActivarDesdeAfuera();
        }
    }

    void OnTriggerExit2D(Collider2D otro)
    {
        if (otro.GetComponent<Jugador>() == null) return;

        if (camara != null)
        {
            camara.VolverASeguir();
        }
    }
}