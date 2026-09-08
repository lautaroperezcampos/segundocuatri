using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Poné este script en un GameObject padre (por ejemplo "PanelTutorial") que
// tenga como hijos el Texto (TextMeshPro) y, opcionalmente, la Image del icono.
public class TextoTutorial : MonoBehaviour
{
    public static TextoTutorial instancia;

    [Header("Referencias (arrastralas desde los hijos)")]
    public TextMeshProUGUI texto;
    public Image imagen; // opcional: el icono al lado del texto (ej: boton de joystick)

    void Awake()
    {
        instancia = this;
        Debug.Log("TextoTutorial listo. Texto asignado? " + (texto != null) + " | Imagen asignada? " + (imagen != null));
        Ocultar();
    }

    // "icono" es opcional: si no le pasas nada, no muestra ninguna imagen
    public void Mostrar(string mensaje, Sprite icono = null)
    {
        Debug.Log("TextoTutorial.Mostrar() llamado con: " + mensaje);

        if (texto != null)
        {
            texto.text = mensaje;
        }

        if (imagen != null)
        {
            if (icono != null)
            {
                imagen.sprite = icono;
                imagen.enabled = true;
            }
            else
            {
                imagen.enabled = false;
            }
        }
    }

    public void Ocultar()
    {
        if (texto != null)
        {
            texto.text = "";
        }

        if (imagen != null)
        {
            imagen.enabled = false;
        }
    }
}