using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

// Musica de fondo SOLO para las escenas de juego. En las escenas que pongas en la lista
// "Escenas Sin Musica" (menu, escena final, etc) se apaga con un fundido, y vuelve al entrar a un nivel.
//
// COMO ARMARLO:
//  1) Un objeto vacio en el PRIMER nivel (no en el menu) llamado "MusicaJuego", con este script.
//     (se le agrega solo el AudioSource; arrastra tu cancion al campo "Musica")
//  2) En "Escenas Sin Musica" escribi los nombres EXACTOS del menu y de la escena final.
//  3) Sobrevive entre escenas, asi la musica no se corta al pasar de nivel.
[RequireComponent(typeof(AudioSource))]
public class MusicaJuego : MonoBehaviour
{
    public static MusicaJuego instancia;

    [Header("Musica")]
    public AudioClip musica;
    [Range(0f, 1f)]
    public float volumen = 0.5f;
    public float duracionFundido = 1f;

    [Header("Escenas SIN musica (nombre exacto, tal cual en Build Settings)")]
    public string[] escenasSinMusica = { "Menu", "Final" };

    private AudioSource fuente;
    private Coroutine fundidoActual;

    void Awake()
    {
        // si ya existe una (volvimos al primer nivel desde el menu), esta nueva sobra
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;
        DontDestroyOnLoad(gameObject);

        fuente = GetComponent<AudioSource>();
        fuente.clip = musica;
        fuente.loop = true;
        fuente.playOnAwake = false;
        fuente.volume = 0f;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
    }

    void Start()
    {
        // la primera escena (donde esta este objeto) tambien tiene que decidir
        if (instancia == this) Decidir(SceneManager.GetActiveScene().name);
    }

    void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        if (instancia != this) return;
        Decidir(escena.name);
    }

    void Decidir(string nombreEscena)
    {
        bool sinMusica = false;

        foreach (string nombre in escenasSinMusica)
        {
            if (nombre == nombreEscena) { sinMusica = true; break; }
        }

        if (sinMusica) Apagar();
        else Prender();
    }

    void Prender()
    {
        if (fuente == null || musica == null) return;
        if (fuente.clip != musica) fuente.clip = musica;

        if (!fuente.isPlaying) fuente.Play();

        IniciarFundido(volumen, false);
    }

    void Apagar()
    {
        if (fuente == null) return;
        IniciarFundido(0f, true);
    }

    void IniciarFundido(float destino, bool pausarAlFinal)
    {
        if (fundidoActual != null) StopCoroutine(fundidoActual);
        fundidoActual = StartCoroutine(Fundido(destino, pausarAlFinal));
    }

    // usa tiempo "real" asi funciona aunque el juego este en pausa (Time.timeScale = 0)
    IEnumerator Fundido(float destino, bool pausarAlFinal)
    {
        float inicio = fuente.volume;
        float t = 0f;

        while (t < duracionFundido)
        {
            t += Time.unscaledDeltaTime;
            fuente.volume = Mathf.Lerp(inicio, destino, duracionFundido <= 0f ? 1f : t / duracionFundido);
            yield return null;
        }

        fuente.volume = destino;

        if (pausarAlFinal) fuente.Pause();
    }
}