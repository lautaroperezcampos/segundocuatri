using UnityEngine;
using System.Collections.Generic;

// Plataforma que se mueve TODO EL TIEMPO yendo y viniendo (arriba/abajo, izquierda/derecha o en diagonal).
// Poné este script en la plataforma. Necesita Collider2D y Rigidbody2D en "Kinematic" (se pone solo).
// A diferencia de PlataformaMovil (que se mueve una sola vez), esta no para nunca.
[RequireComponent(typeof(Rigidbody2D))]
public class PlataformaVaiven : MonoBehaviour
{
    [Header("Recorrido (desde donde la pones en la escena)")]
    public Vector2 desplazamiento = new Vector2(0f, 4f); // hasta donde llega: (0, 4) = sube 4 unidades y vuelve. (4, 0) = va 4 a la derecha
    public float velocidad = 2f; // unidades por segundo
    public bool movimientoSuave = true; // frena y acelera en los extremos

    [Header("Pausa y desfase")]
    public float pausaEnExtremos = 0f; // segundos que se queda quieta arriba/abajo (0 = no frena)
    [Range(0f, 1f)]
    public float desfase = 0f; // 0 a 1: en que punto del recorrido arranca (poné 0.5 en la otra plataforma para que vayan al reves)

    [Header("Pasajeros")]
    public bool llevarPasajeros = true; // el jugador/subjefe parado arriba se mueve con ella

    private Rigidbody2D rb;
    private Vector2 posicionInicial;
    private float longitud;
    private float tiempo;
    private Vector2 ultimaPosicion;
    private List<Rigidbody2D> pasajeros = new List<Rigidbody2D>();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        posicionInicial = rb.position;
        ultimaPosicion = posicionInicial;
        longitud = Mathf.Max(0.01f, desplazamiento.magnitude);
    }

    void FixedUpdate()
    {
        tiempo += Time.fixedDeltaTime;

        // recorrido de ida y vuelta: x va de 0 a (1 + pausa), y tiempo muerto en cada punta
        float duracionTrayecto = longitud / Mathf.Max(0.01f, velocidad);
        float pausaNormalizada = pausaEnExtremos / duracionTrayecto;
        float x = Mathf.PingPong(tiempo / duracionTrayecto + desfase * (1f + pausaNormalizada), 1f + pausaNormalizada);
        float t = Mathf.Clamp01(x - pausaNormalizada * 0.5f);

        if (movimientoSuave) t = Mathf.SmoothStep(0f, 1f, t);

        Vector2 nueva = posicionInicial + desplazamiento * t;
        Vector2 delta = nueva - ultimaPosicion;
        ultimaPosicion = nueva;

        rb.MovePosition(nueva);

        // los que estan parados arriba se mueven igual que la plataforma
        if (llevarPasajeros)
        {
            foreach (Rigidbody2D p in pasajeros)
            {
                if (p != null) p.position += delta;
            }
        }
        pasajeros.Clear(); // se vuelven a llenar en cada paso fisico con OnCollisionStay2D
    }

    void OnCollisionStay2D(Collision2D colision)
    {
        if (!llevarPasajeros) return;

        Rigidbody2D otro = colision.rigidbody;
        if (otro == null || otro.bodyType != RigidbodyType2D.Dynamic) return;
        if (pasajeros.Contains(otro)) return;

        // solo si esta ARRIBA de la plataforma
        if (colision.collider.bounds.center.y > GetComponent<Collider2D>().bounds.center.y)
        {
            pasajeros.Add(otro);
        }
    }

    void OnDrawGizmosSelected()
    {
        Vector2 origen = Application.isPlaying ? posicionInicial : (Vector2)transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(origen, origen + desplazamiento);
        Gizmos.DrawWireSphere(origen, 0.2f);
        Gizmos.DrawWireSphere(origen + desplazamiento, 0.2f);
    }
}