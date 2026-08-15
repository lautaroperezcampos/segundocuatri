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

    // guardamos hacia donde y a quien va el proximo disparo, hasta que la
    // animacion llegue al frame correcto y lo dispare de verdad
    private Vector2 direccionPendiente;
    private bool objetivoEsJugadorPendiente;
    private bool hayDisparoPendiente = false;

    protected override void Update()
    {
        base.Update(); // mantiene toda la logica original: suelo, deteccion de posesion, etc

        // solo dispara si no esta poseido Y sigue vivo
        if (!estaPoseido && !estaMuerto)
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
        PrepararDisparo(direccionDisparo, true); // true = este disparo hiere al Jugador
    }

    // el jugador llama a esto (via Atacar()) cuando lo posee y hace click
    public override void Atacar()
    {
        if (Time.time < tiempoUltimoDisparo + cooldownDisparo) return;

        tiempoUltimoDisparo = Time.time;

        Vector2 direccionDisparo = new Vector2(direccion, 0);
        PrepararDisparo(direccionDisparo, false); // false = este disparo hiere a Puerta/Enemigo
    }

    // guarda la info del disparo y arranca la animacion; el proyectil todavia NO aparece
    void PrepararDisparo(Vector2 direccionDisparo, bool objetivoEsJugador)
    {
        direccionPendiente = direccionDisparo;
        objetivoEsJugadorPendiente = objetivoEsJugador;
        hayDisparoPendiente = true;

        if (animator != null)
        {
            animator.SetTrigger("Disparando");
        }
        else
        {
            // si no hay animator configurado, disparamos al toque como antes
            SpawnearProyectilAhora();
        }
    }

    // IMPORTANTE: este metodo lo tiene que llamar un Animation Event
    // puesto en el frame exacto de la animacion de disparo donde queres
    // que aparezca el proyectil (click derecho en ese frame en la ventana
    // de Animation > Add Animation Event > elegi esta funcion)
    public void SpawnearProyectilAhora()
    {
        if (!hayDisparoPendiente) return;
        hayDisparoPendiente = false;

        Vector3 posicionSpawn = transform.position + (Vector3)(direccionPendiente * distanciaSpawnProyectil);
        GameObject nuevoProyectil = Instantiate(prefabProyectil, posicionSpawn, Quaternion.identity);

        Proyectil scriptProyectil = nuevoProyectil.GetComponent<Proyectil>();
        if (scriptProyectil != null)
        {
            scriptProyectil.daño = dañoProyectil;
            scriptProyectil.velocidad = velocidadProyectil;
            scriptProyectil.Configurar(direccionPendiente, objetivoEsJugadorPendiente);
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