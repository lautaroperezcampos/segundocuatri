using UnityEngine;

// Poné este script en el sprite de la flecha.
public class FlechaIndicadora : MonoBehaviour
{
    [Header("Parpadeo")]
    public float intervaloParpadeo = 0.4f; // cada cuanto se prende/apaga

    private SpriteRenderer spriteRenderer;
    private float tiempoUltimoParpadeo;
    private int cantidadPuertasInicial;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        tiempoUltimoParpadeo = Time.time;

        // contamos cuantas puertas hay al arrancar, para despues notar si rompieron alguna
        cantidadPuertasInicial = FindObjectsByType<Puerta>(FindObjectsSortMode.None).Length;
    }

    void Update()
    {
        // si hay MENOS puertas que al principio, rompieron alguna (no importa cual) y la flecha desaparece
        int cantidadPuertasActual = FindObjectsByType<Puerta>(FindObjectsSortMode.None).Length;
        if (cantidadPuertasActual < cantidadPuertasInicial)
        {
            Destroy(gameObject);
            return;
        }

        if (Time.time >= tiempoUltimoParpadeo + intervaloParpadeo)
        {
            tiempoUltimoParpadeo = Time.time;

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = !spriteRenderer.enabled;
            }
        }
    }
}