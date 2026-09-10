using UnityEngine;
using TMPro;

public enum Hablante { Protagonista, OtroPersonaje }

[System.Serializable]
public class LineaDialogo
{
    public Hablante hablante; // quien dice esta linea
    [TextArea(2, 4)]
    public string texto;
}

public class DialogoManager : MonoBehaviour
{
    [Header("Lineas de dialogo, en orden")]
    public LineaDialogo[] lineas;

    [Header("Caja del Protagonista (su cara ya esta puesta ahi fija)")]
    public GameObject cajaProtagonista; // el conjunto entero: cara + fondo del texto
    public TextMeshProUGUI textoProtagonista;

    [Header("Caja del Otro Personaje (su cara ya esta puesta ahi fija)")]
    public GameObject cajaOtroPersonaje;
    public TextMeshProUGUI textoOtroPersonaje;

    [Header("Al terminar (opcional)")]
    public string nombreEscenaSiguiente; // si lo dejas vacio, simplemente cierra las cajas

    private int indiceActual = 0;

    void Start()
    {
        MostrarLinea();
    }

    void Update()
    {
        // GetButtonDown("Jump") responde al boton A del joystick Y a la tecla Espacio a la vez
        if (Input.GetButtonDown("Jump"))
        {
            AvanzarDialogo();
        }
    }

    void MostrarLinea()
    {
        if (indiceActual >= lineas.Length)
        {
            TerminarDialogo();
            return;
        }

        LineaDialogo linea = lineas[indiceActual];
        bool hablaProtagonista = linea.hablante == Hablante.Protagonista;

        // se prende SOLO la caja de quien esta hablando en esta linea, la otra se apaga
        if (cajaProtagonista != null)
        {
            cajaProtagonista.SetActive(hablaProtagonista);
        }

        if (cajaOtroPersonaje != null)
        {
            cajaOtroPersonaje.SetActive(!hablaProtagonista);
        }

        if (hablaProtagonista && textoProtagonista != null)
        {
            textoProtagonista.text = linea.texto;
        }
        else if (!hablaProtagonista && textoOtroPersonaje != null)
        {
            textoOtroPersonaje.text = linea.texto;
        }
    }

    void AvanzarDialogo()
    {
        indiceActual++;
        MostrarLinea();
    }

    void TerminarDialogo()
    {
        if (cajaProtagonista != null)
        {
            cajaProtagonista.SetActive(false);
        }

        if (cajaOtroPersonaje != null)
        {
            cajaOtroPersonaje.SetActive(false);
        }

        if (string.IsNullOrEmpty(nombreEscenaSiguiente)) return;

        if (TransicionEscena.instancia != null)
        {
            TransicionEscena.instancia.IrAEscena(nombreEscenaSiguiente);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(nombreEscenaSiguiente);
        }
    }
}