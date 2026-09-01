using UnityEngine;
using System.Collections;

public class PlataformaRompible : MonoBehaviour
{
    [Header("Rotura")]
    public float tiempoAntesDeRomper = 1f; // cuanto aguanta pisada antes de romperse

    [Header("Respawn (opcional)")]
    public bool respawnea = true;
    public float tiempoRespawn = 3f; // cuanto tarda en volver a aparecer

    private SpriteRenderer spriteRenderer;
    private Collider2D colisionador;
    private bool rompiendose = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        colisionador = GetComponent<Collider2D>();
    }

    void OnCollisionEnter2D(Collision2D colision)
    {
        if (rompiendose) return;

        // solo cuenta si te pararon ENCIMA (mismo chequeo que usamos en el Trampolin)
        foreach (ContactPoint2D contacto in colision.contacts)
        {
            if (contacto.normal.y < -0.5f)
            {
                StartCoroutine(SecuenciaDeRotura());
                break;
            }
        }
    }

    IEnumerator SecuenciaDeRotura()
    {
        rompiendose = true;

        // primera mitad del tiempo: quieta, sin avisar todavia
        yield return new WaitForSeconds(tiempoAntesDeRomper * 0.5f);

        // segunda mitad: parpadea como aviso de que esta por romperse
        float tiempoRestante = tiempoAntesDeRomper * 0.5f;
        float intervaloParpadeo = 0.1f;

        while (tiempoRestante > 0f)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = !spriteRenderer.enabled;
            }

            yield return new WaitForSeconds(intervaloParpadeo);
            tiempoRestante -= intervaloParpadeo;
        }

        // se rompe: desaparece y deja de ser solida
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        if (colisionador != null)
        {
            colisionador.enabled = false;
        }

        if (respawnea)
        {
            yield return new WaitForSeconds(tiempoRespawn);

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
            }

            if (colisionador != null)
            {
                colisionador.enabled = true;
            }

            rompiendose = false;
        }
        // si no respawnea, se queda rota para siempre (no reseteamos "rompiendose")
    }
}