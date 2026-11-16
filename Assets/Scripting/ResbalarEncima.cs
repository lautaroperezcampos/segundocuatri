using UnityEngine;

// Ayuda compartida por Jugador, Enemigo y Subjefe: detecta si hay otro personaje DEBAJO
// de los pies (sin depender de OnCollisionStay2D, que falla si el cuerpo se duerme
// o si el contacto no reporta bien la normal).
public static class ResbalarEncima
{
    // devuelve el Collider2D del personaje (Jugador/Enemigo/Subjefe) que esta justo debajo, o null
    public static Collider2D BuscarDebajo(Collider2D propio, bool ignorarJugador = false)
    {
        if (propio == null) return null;

        Bounds b = propio.bounds;
        Vector2 centro = new Vector2(b.center.x, b.min.y - 0.08f);
        Vector2 tam = new Vector2(b.size.x * 0.8f, 0.2f);

        Collider2D[] cercanos = Physics2D.OverlapBoxAll(centro, tam, 0f);
        foreach (Collider2D otro in cercanos)
        {
            if (otro == propio || otro.isTrigger) continue;
            if (otro.attachedRigidbody != null && otro.attachedRigidbody == propio.attachedRigidbody) continue;

            // el otro tiene que estar realmente DEBAJO: su parte de arriba queda bajo mi centro
            if (otro.bounds.max.y > b.center.y) continue;

            bool esJugador = otro.GetComponentInParent<Jugador>() != null;
            bool esEnemigo = otro.GetComponentInParent<Enemigo>() != null;
            bool esSubjefe = otro.GetComponentInParent<Subjefe>() != null;

            if (esJugador && ignorarJugador) continue;
            if (esJugador || esEnemigo || esSubjefe) return otro;
        }

        return null;
    }

    // hacia que lado resbalar: se aleja del otro; si estan clavados en el mismo X, sortea
    // (si fuera siempre el mismo lado, una pila de tres se movia toda junta)
    public static float ElegirLado(Transform yo, Transform otro)
    {
        float dx = yo.position.x - otro.position.x;
        if (Mathf.Abs(dx) > 0.1f) return Mathf.Sign(dx);
        return Random.value < 0.5f ? -1f : 1f;
    }
}