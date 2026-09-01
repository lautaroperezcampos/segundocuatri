using UnityEngine;
using System.Collections;

// Poné este script en el mismo objeto que tiene el Sprite Renderer
// (el hijo "Sprite"/"Modelo Visual" de cada personaje).
public class EfectoFlashDaño : MonoBehaviour
{
    [Header("Flash al recibir daño")]
    // OJO: si el sprite ya esta en blanco puro por defecto (lo normal),
    // poner blanco como flash no se nota - es matematicamente lo mismo.
    // Usa un color CLARAMENTE distinto (amarillo, rojo claro, etc.)
    public Color colorFlash = new Color(1f, 0.4f, 0.4f); // rojo clarito, bien visible
    public float duracionFlash = 0.15f;

    private SpriteRenderer spriteRenderer;
    private Color colorOriginal;
    private Coroutine flashEnCurso;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            colorOriginal = spriteRenderer.color;
        }
    }

    // llamalo desde el RecibirDaño() de cada personaje
    public void Flashear()
    {
        if (spriteRenderer == null) return;

        if (flashEnCurso != null)
        {
            StopCoroutine(flashEnCurso);
        }

        flashEnCurso = StartCoroutine(SecuenciaFlash());
    }

    IEnumerator SecuenciaFlash()
    {
        spriteRenderer.color = colorFlash;
        yield return new WaitForSeconds(duracionFlash);
        spriteRenderer.color = colorOriginal;
        flashEnCurso = null;
    }
}