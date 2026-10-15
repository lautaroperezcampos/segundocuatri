using UnityEngine;
using UnityEngine.SceneManagement;

// Poné este script en un GameObject con Collider2D marcado "Is Trigger",
// en el lugar de la puerta que lleva a la otra escena.
public class PuertaEscena : MonoBehaviour
{
    [Header("Escena a cargar")]
    public string nombreEscenaDestino; // el nombre EXACTO tal cual aparece en Build Settings

    private bool yaSeActivo = false; // evita disparar la transicion mas de una vez

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (yaSeActivo) return;

        // pasa el Jugador, O un Subjefe que el Jugador este poseyendo ahora mismo.
        // Un Subjefe hostil o suelto NO la activa.
        bool esJugador = otro.GetComponent<Jugador>() != null;
        Subjefe subjefe = otro.GetComponent<Subjefe>();
        bool esSubjefePoseido = subjefe != null && subjefe.estaPoseido;

        if (!esJugador && !esSubjefePoseido) return;

        yaSeActivo = true;

        if (TransicionEscena.instancia != null)
        {
            TransicionEscena.instancia.IrAEscena(nombreEscenaDestino);
        }
        else
        {
            // respaldo por si todavia no armaste el sistema de fundido: cambia directo, sin negro
            Debug.LogWarning("No hay TransicionEscena en la escena, cambiando sin fundido");
            SceneManager.LoadScene(nombreEscenaDestino);
        }
    }
}