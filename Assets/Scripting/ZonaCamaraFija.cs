using UnityEngine;

// Poné este script en un GameObject con un Collider2D marcado como "Is Trigger",
// que cubra el area de la pelea (por ejemplo la arena de un Titan).
public class ZonaCamaraFija : MonoBehaviour
{
    [Header("Punto donde se centra la camara")]
    public Transform puntoCamaraFija; // arrastra un GameObject vacio ubicado en el centro de la arena

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
            camara.FijarCamara(puntoCamaraFija.position);
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