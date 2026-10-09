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

    [Header("Apuntado con el mismo stick de movimiento (cuando esta poseido)")]
    public float umbralStickApuntado = 0.2f; // por debajo de esto, se ignora el stick (evita drift)
    public bool invertirVerticalApuntado = false; // tildalo si arriba/abajo te sale al reves

    [Header("Mira automatica (mantener el boton de atacar apretado)")]
    public GameObject prefabMira; // un sprite simple de mira/crosshair
    public float distanciaMiraLibre = 5f; // que tan lejos del titan flota la mira mientras no engancha nada
    public float radioEnganche = 1f; // que tan cerca tiene que pasar la mira de un objetivo para pegarse solo
    private GameObject miraInstanciada;
    private Transform objetivoBloqueado;

    private float tiempoUltimoDisparo = -999f;

    protected override void Update()
    {
        base.Update(); // mantiene toda la logica original: suelo, deteccion de posesion, etc

        // solo dispara si no esta poseido Y sigue vivo
        if (!estaPoseido && !estaMuerto && EstaListoParaActuar())
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

        // giramos para mirar hacia el jugador antes de disparar, aunque no nos movamos
        ActualizarDireccion(jugador.position.x > transform.position.x ? 1 : -1);

        Vector2 direccionDisparo = (jugador.position - transform.position).normalized;
        DispararAhora(direccionDisparo, true); // true = este disparo hiere al Jugador
    }

    // el jugador llama a esto (via Atacar()) cuando lo posee y aprieta o SUELTA el boton
    public override void Atacar()
    {
        Vector2 direccionDisparo;

        if (objetivoBloqueado != null)
        {
            // habia un objetivo enganchado con la mira: dispara justo ahi
            direccionDisparo = (objetivoBloqueado.position - transform.position).normalized;
            ActualizarDireccion(direccionDisparo.x > 0.1f ? 1 : (direccionDisparo.x < -0.1f ? -1 : direccion));
        }
        else
        {
            // no habia objetivo enganchado: dispara segun el stick, como antes
            direccionDisparo = ObtenerDireccionApuntada();
        }

        // la mira SIEMPRE se esconde al soltar el boton, dispares o no
        OcultarMira();
        objetivoBloqueado = null;

        // si todavia esta recargando, no dispara, pero la mira ya se escondio arriba
        if (Time.time < tiempoUltimoDisparo + cooldownDisparo) return;

        tiempoUltimoDisparo = Time.time;

        DispararAhora(direccionDisparo, false); // false = este disparo hiere a Puerta/Enemigo/Subjefe
    }

    // el jugador llama a esto cada frame MIENTRAS mantiene apretado el boton de atacar.
    // la mira SIEMPRE se ve mientras mantenes, y se mueve segun hacia donde apuntes con
    // el stick. Si al pasar por ahi hay un Enemigo/Diana/Subjefe cerca, se pega solo a el.
    public override void MantenerApuntado()
    {
        Vector2 direccionMira = ObtenerDireccionApuntada();
        Vector3 posicionMiraLibre = transform.position + (Vector3)(direccionMira * distanciaMiraLibre);

        Transform objetivoCercano = BuscarObjetivoCercaDe(posicionMiraLibre);

        if (objetivoCercano != null)
        {
            objetivoBloqueado = objetivoCercano;
            MostrarMira(objetivoBloqueado.position); // la mira se pega al objetivo
        }
        else
        {
            objetivoBloqueado = null;
            MostrarMira(posicionMiraLibre); // la mira sigue al stick libremente
        }
    }

    // busca el Enemigo, Diana o Subjefe (otro, no el mismo) mas cercano a la
    // posicion ACTUAL de la mira (no del titan) - asi se "engancha" solo al pasar cerca
    Transform BuscarObjetivoCercaDe(Vector3 posicionMira)
    {
        Collider2D[] candidatos = Physics2D.OverlapCircleAll(posicionMira, radioEnganche);

        Transform masCercano = null;
        float distanciaMinima = float.MaxValue;

        foreach (Collider2D candidato in candidatos)
        {
            if (candidato.gameObject == gameObject) continue; // no nos apuntamos a nosotros mismos

            bool esValido = candidato.GetComponent<Enemigo>() != null
                || candidato.GetComponent<Diana>() != null
                || candidato.GetComponent<Subjefe>() != null;

            if (!esValido) continue;

            float distancia = Vector2.Distance(posicionMira, candidato.transform.position);
            if (distancia < distanciaMinima)
            {
                distanciaMinima = distancia;
                masCercano = candidato.transform;
            }
        }

        return masCercano;
    }

    void MostrarMira(Vector3 posicion)
    {
        if (miraInstanciada == null && prefabMira != null)
        {
            miraInstanciada = Instantiate(prefabMira);
        }

        if (miraInstanciada != null)
        {
            miraInstanciada.transform.position = posicion;
            miraInstanciada.SetActive(true);
        }
    }

    void OcultarMira()
    {
        if (miraInstanciada != null)
        {
            miraInstanciada.SetActive(false);
        }
    }

    // usa el MISMO stick que ya usas para caminar: si empujas en diagonal
    // (por ejemplo arriba-derecha), camina para ese lado Y dispara en esa diagonal
    // al mismo tiempo. Si no lo estas empujando en vertical, dispara hacia
    // donde estas mirando (izquierda/derecha), como antes.
    Vector2 ObtenerDireccionApuntada()
    {
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");

        if (invertirVerticalApuntado)
        {
            y = -y;
        }

        Vector2 entrada = new Vector2(x, y);

        if (entrada.magnitude > umbralStickApuntado)
        {
            // giramos el sprite para que coincida con hacia donde apunta el stick,
            // sin flippearlo si el stick esta casi vertical (x muy chico)
            if (entrada.x > 0.1f)
            {
                ActualizarDireccion(1);
            }
            else if (entrada.x < -0.1f)
            {
                ActualizarDireccion(-1);
            }

            return entrada.normalized;
        }

        return new Vector2(direccion, 0);
    }

    // dispara AL INSTANTE: la animacion es solo visual (no bloquea el disparo),
    // asi que podes disparar varias veces seguidas sin esperar a que termine
    // ya no hace falta (el disparo sale al instante, no espera al Animation Event),
    // pero la dejamos vacia para que el ReenviadorEventosAnimacion.cs no rompa
    // si todavia tiene el Animation Event conectado en algun clip
    public void SpawnearProyectilAhora()
    {
    }

    void DispararAhora(Vector2 direccionDisparo, bool objetivoEsJugador)
    {
        if (animator != null)
        {
            animator.SetTrigger("Disparando");
        }

        Vector3 posicionSpawn = transform.position + (Vector3)(direccionDisparo * distanciaSpawnProyectil);
        GameObject nuevoProyectil = Instantiate(prefabProyectil, posicionSpawn, Quaternion.identity);

        Proyectil scriptProyectil = nuevoProyectil.GetComponent<Proyectil>();
        if (scriptProyectil != null)
        {
            scriptProyectil.daño = dañoProyectil;
            scriptProyectil.velocidad = velocidadProyectil;
            scriptProyectil.Configurar(direccionDisparo, objetivoEsJugador, gameObject);
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