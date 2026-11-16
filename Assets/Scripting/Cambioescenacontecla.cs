using UnityEngine;
using UnityEngine.SceneManagement;

// Sin botones ni HUD: apretas J y pasa a la escena que sigue (segun el orden del Build Settings).
// Con H vuelve a la anterior.
//
// COMO ARMARLO: un objeto vacio en la PRIMERA escena (el menu) con este script. Sobrevive entre escenas.
public class CambioEscenaConTecla : MonoBehaviour
{
    public static CambioEscenaConTecla instancia;

    public KeyCode teclaSiguiente = KeyCode.J;
    public KeyCode teclaAnterior = KeyCode.H;
    public bool usarFundido = true; // usa TransicionEscena (fundido a negro) si existe

    private bool cambiando = false;

    void Awake()
    {
        // si ya existe uno (volvimos al menu), este nuevo sobra
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
    }

    void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        cambiando = false;
    }

    void Update()
    {
        if (cambiando) return;

        if (Input.GetKeyDown(teclaSiguiente)) IrAIndice(SceneManager.GetActiveScene().buildIndex + 1);
        if (Input.GetKeyDown(teclaAnterior)) IrAIndice(SceneManager.GetActiveScene().buildIndex - 1);
    }

    void IrAIndice(int indice)
    {
        int total = SceneManager.sceneCountInBuildSettings;
        if (total == 0) return;

        // da la vuelta: despues de la ultima va a la primera, y antes de la primera va a la ultima
        if (indice >= total) indice = 0;
        if (indice < 0) indice = total - 1;

        cambiando = true;

        // por si venias de un Game Over (que congela el tiempo)
        Time.timeScale = 1f;

        string nombre = System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(indice));

        if (usarFundido && TransicionEscena.instancia != null)
        {
            TransicionEscena.instancia.IrAEscena(nombre);
        }
        else
        {
            SceneManager.LoadScene(indice);
        }
    }
}