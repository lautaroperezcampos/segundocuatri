using UnityEngine;
using UnityEngine.SceneManagement;

public class FinDelJuego : MonoBehaviour
{
    [Header("Escena del menu principal")]
    public string nombreEscenaMenu = "MenuPrincipal"; // el nombre EXACTO tal cual esta en Build Settings

    // enganchá esto al OnClick() del boton
    public void VolverAlMenu()
    {
        if (TransicionEscena.instancia != null)
        {
            TransicionEscena.instancia.IrAEscena(nombreEscenaMenu);
        }
        else
        {
            SceneManager.LoadScene(nombreEscenaMenu);
        }
    }
}