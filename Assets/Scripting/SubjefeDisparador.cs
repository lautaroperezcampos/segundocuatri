using UnityEngine;

// Titan nuevo, separado del Subjefe original. Hereda vida, movimiento, salto y
// posesion del Subjefe base, pero cambia el ataque por disparos a distancia.
public class SubjefeDisparador : Subjefe
{
    [Header("Disparo")]
    public GameObject prefabProyectil; // arrastra el prefab del Proyectil
    public float cooldownDisparo = 2f;
    public float rangoDisparo = 8f;
    public int dañoProyectil = 15;
    public float velocidadProyectil = 10f;
    public float distanciaSpawnProyectil = 1f; // que tan lejos del centro aparece el disparo

    private float tiempoUltimoDisparo = -999f;

    protected override void Update()
    {
        base.Update(); // mantiene toda la logica original: suelo, deteccion de posesion, etc

        // solo dispara solo cuando NO esta siendo controlado por el jugador
        if (!estaPoseido)
        {
            IntentarDispararAlJugador();
        }
    }

    void IntentarDispararAlJugador()
    {
        if (jugador == null || prefabProyectil == null) return;
        if (Time.time < tiempoUltimoDisparo + cooldownDisparo) return;

        float distancia = Vector2.Distance(transform.position, jugador.position);
        if (distancia > rangoDisparo) return;

        tiempoUltimoDisparo = Time.time;

        Vector2 direccionDisparo = (jugador.position - transform.position).normalized;
        Disparar(direccionDisparo, true); // true = este disparo hiere al Jugador
    }

    // el jugador llama a esto (via Atacar()) cuando lo posee y hace click
    public override void Atacar()
    {
        if (Time.time < tiempoUltimoDisparo + cooldownDisparo) return;

        tiempoUltimoDisparo = Time.time;

        Vector2 direccionDisparo = new Vector2(direccion, 0);
        Disparar(direccionDisparo, false); // false = este disparo hiere a Puerta/Enemigo
    }

    void Disparar(Vector2 direccionDisparo, bool objetivoEsJugador)
    {
        Vector3 posicionSpawn = transform.position + (Vector3)(direccionDisparo * distanciaSpawnProyectil);
        GameObject nuevoProyectil = Instantiate(prefabProyectil, posicionSpawn, Quaternion.identity);

        Proyectil scriptProyectil = nuevoProyectil.GetComponent<Proyectil>();
        if (scriptProyectil != null)
        {
            scriptProyectil.daño = dañoProyectil;
            scriptProyectil.velocidad = velocidadProyectil;
            scriptProyectil.Configurar(direccionDisparo, objetivoEsJugador);
        }
    }

    // dibuja el rango de disparo en el editor, ademas de lo que ya dibuja el Subjefe base
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, rangoDisparo);
    }
}