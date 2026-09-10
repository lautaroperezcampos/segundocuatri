using UnityEngine;
using UnityEngine.SceneManagement;

public class PausaManager : MonoBehaviour
{
    [Header("Panel de pausa")]
    public GameObject panelPausa;

    [Header("Escena del menu principal")]
    public string nombreEscenaMenu = "MenuPrincipal"; // el nombre EXACTO tal cual esta en Build Settings

    [Header("Tecla/boton para pausar")]
    public bool permitirPausaConEscape = true;

    private bool pausado = false;

    void Start()
    {
        if (panelPausa != null)
        {
            panelPausa.SetActive(false);
        }
    }

    void Update()
    {
        if (!permitirPausaConEscape) return;

        // Escape en teclado, o Start/boton 7 en la mayoria de los joysticks
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown("joystick button 7"))
        {
            if (pausado)
            {
                Reanudar();
            }
            else
            {
                Pausar();
            }
        }
    }

    public void Pausar()
    {
        pausado = true;
        Time.timeScale = 0f;

        if (panelPausa != null)
        {
            panelPausa.SetActive(true);
        }
    }

    // enganchá esto al OnClick() del boton "Reanudar"
    public void Reanudar()
    {
        pausado = false;
        Time.timeScale = 1f;

        if (panelPausa != null)
        {
            panelPausa.SetActive(false);
        }
    }

    // enganchá esto al OnClick() del boton "Reiniciar"
    public void ReiniciarNivel()
    {
        Time.timeScale = 1f; // importante: sino la escena nueva arranca pausada
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // enganchá esto al OnClick() del boton "Volver al Menu"
    public void VolverAlMenu()
    {
        Time.timeScale = 1f; // importante: sino el menu arranca pausado

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