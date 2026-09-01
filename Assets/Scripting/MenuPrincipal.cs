using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipal : MonoBehaviour
{
    [Header("Escena a cargar al jugar")]
    public string nombreEscenaJuego = "Tutorial"; // el nombre EXACTO de la escena, tal cual aparece en Build Settings

    // enganchá esto al OnClick() del boton "Jugar"
    public void Jugar()
    {
        SceneManager.LoadScene(nombreEscenaJuego);
    }

    // enganchá esto al OnClick() del boton "Salir"
    public void Salir()
    {
        Debug.Log("Saliendo del juego...");

#if UNITY_EDITOR
        // en el Editor, Application.Quit() no hace nada - esto para el Play en su lugar
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}