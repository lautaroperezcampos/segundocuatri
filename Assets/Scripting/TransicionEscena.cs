using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

// Poné este script en un GameObject con un Canvas propio (separado del resto de tu UI)
// que tenga una Image negra ocupando toda la pantalla. Este objeto sobrevive entre
// escenas (DontDestroyOnLoad), asi no hace falta armarlo de nuevo en cada una.
public class TransicionEscena : MonoBehaviour
{
    public static TransicionEscena instancia;

    [Header("Referencias")]
    public Image imagenNegra; // la Image negra que cubre toda la pantalla

    [Header("Configuracion")]
    public float duracionFade = 1f;

    void Awake()
    {
        // si ya existe una instancia (venimos de otra escena), esta copia nueva sobra
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;
        DontDestroyOnLoad(gameObject);

        if (imagenNegra != null)
        {
            Color c = imagenNegra.color;
            c.a = 0f; // arranca transparente
            imagenNegra.color = c;
        }
    }

    // llamalo desde la Puerta (o desde donde quieras) para cambiar de escena con fundido
    public void IrAEscena(string nombreEscena)
    {
        StartCoroutine(SecuenciaTransicion(nombreEscena));
    }

    IEnumerator SecuenciaTransicion(string nombreEscena)
    {
        yield return StartCoroutine(Fade(0f, 1f)); // fundido a negro

        SceneManager.LoadScene(nombreEscena);

        yield return null; // un frame de margen para que la escena nueva termine de asentarse

        yield return StartCoroutine(Fade(1f, 0f)); // fundido de vuelta a claro
    }

    IEnumerator Fade(float desde, float hasta)
    {
        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracionFade)
        {
            tiempoTranscurrido += Time.deltaTime;
            float t = tiempoTranscurrido / duracionFade;

            Color c = imagenNegra.color;
            c.a = Mathf.Lerp(desde, hasta, t);
            imagenNegra.color = c;

            yield return null;
        }

        Color final = imagenNegra.color;
        final.a = hasta;
        imagenNegra.color = final;
    }
}