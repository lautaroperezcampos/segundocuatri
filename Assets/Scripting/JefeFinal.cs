using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

// JEFE FINAL: hereda de Subjefe (igual que SubjefeVoladorJefe) asi el golpe del Jugador, la vida y
// la animacion de muerte ya funcionan. NO se puede poseer.
//
// Secuencia (se arma con la lista "Ciclos", normalmente 3):
//   EN EL AIRE  -> se teletransporta, DISPARA CONSTANTEMENTE (los disparos atraviesan paredes),
//                  salen los minions, y cuando mueren salen los subjefes del ciclo.
//   Cuando mueren todos -> BAJA a "Punto Suelo" y se le puede pegar hasta el % de vida del ciclo.
//   Despues SUBE, se teletransporta y arranca el ciclo siguiente.
//   En el ultimo ciclo (Porcentaje Vida Fin = 0) se le pega hasta que muere.
public class JefeFinal : Subjefe
{
    [System.Serializable]
    public class CicloJefe
    {
        public string nombre = "Ciclo"; // solo para ordenarte en el Inspector

        [Header("Minions")]
        public SpawnerEnemigos minions; // spawner de este ciclo (SIN Collider2D, asi no arranca solo). Puede quedar vacio

        [Header("Subjefes (salen cuando mueren los minions)")]
        public GameObject[] prefabsSubjefes; // disparadores / voladores / golpeador
        public Transform[] puntosSubjefes; // donde sale cada uno (mismo orden que los prefabs)

        [Header("Cuando baja")]
        [Range(0f, 1f)]
        public float porcentajeVidaFin = 0.65f; // se le pega hasta quedar con este % y vuelve a subir. 0 = hasta morir (ultimo ciclo)
        public float tiempoMaximoAbajo = 0f; // opcional: si pasan estos segundos abajo sin llegar al %, sube igual. 0 = sin limite
    }

    [Header("Jefe final - Arena")]
    public Collider2D zonaArena; // trigger que cubre la arena; cuando el jugador entra arranca. Vacio = arranca apenas empieza la escena
    public float retardoInicio = 1f; // segundos entre que entra el jugador y empieza la pelea
    public Transform[] puntosAire; // posiciones donde flota y se teletransporta (pone 2 o mas)
    public Transform puntoSuelo; // donde baja para que le puedas pegar
    public float velocidadTransicion = 6f; // que tan rapido sube/baja
    public GameObject[] paredes; // se activan al empezar y se apagan al terminar (opcional)

    [Header("Jefe final - Ciclos")]
    public CicloJefe[] ciclos; // ciclo 1, ciclo 2, ciclo 3...
    public float pausaEntreFases = 1.5f; // respiro entre minions -> subjefes -> bajar
    public float limiteCaidaY = -40f; // un subjefe que cae por debajo se elimina, para que la pelea no se trabe

    [Header("Jefe final - Disparo (solo cuando esta en el aire)")]
    public GameObject prefabProyectil; // el proyectil DEL JEFE (puede ser distinto al del subjefe disparador). Tiene que llevar el script Proyectil
    public Transform puntoDisparo; // opcional: de donde sale el disparo (si no, del centro del jefe)
    public float cooldownDisparo = 1.5f;
    public int dañoProyectil = 15;
    public float velocidadProyectil = 8f;
    public float distanciaSpawnProyectil = 1f; // solo se usa si no pusiste Punto Disparo

    [Header("Jefe final - Teletransporte")]
    public float duracionDesaparecer = 0.25f; // cuanto tarda invisible al teletransportarse

    [Header("Jefe final - Al derrotarlo")]
    public GameObject salidaSiguienteEscena; // objeto con PuertaEscena que arranca APAGADO y se enciende al morir (opcional)
    public bool cambiarEscenaAlDerrotar = false; // tildalo para pasar solo a la siguiente escena despues de que muere
    public string nombreEscenaSiguiente; // nombre EXACTO de la escena (la misma que esta en Build Settings)
    public float retardoCambioEscena = 4f; // segundos de espera para que se vea la animacion de muerte antes de cambiar
    public UnityEvent alIniciarPelea;
    public UnityEvent alDerrotar;

    // ---- estado interno ----
    private bool invulnerable = true;
    private bool disparando = false;
    private float tiempoUltimoDisparo = -999f;
    private int cicloActual = 0;
    private int indiceAireActual = 0;
    private SpriteRenderer visual;
    private HashSet<string> parametrosAnimator = new HashSet<string>();

    protected override void Start()
    {
        base.Start();

        // el jefe final NUNCA se puede poseer
        porcentajeVidaParaPoseer = 0f;
        esPoseible = false;

        persigueAlJugador = false;
        invulnerable = true;

        GameObject fuente = modeloVisual != null ? modeloVisual : gameObject;
        visual = fuente.GetComponent<SpriteRenderer>();
        if (visual == null) visual = fuente.GetComponentInChildren<SpriteRenderer>();

        // guardamos los parametros que EXISTEN en el Animator para no spamear errores si falta alguno
        if (animator != null)
        {
            foreach (AnimatorControllerParameter p in animator.parameters) parametrosAnimator.Add(p.name);
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // empieza flotando en el primer punto del aire
        if (puntosAire != null && puntosAire.Length > 0 && puntosAire[0] != null)
        {
            indiceAireActual = 0;
            PonerEn(puntosAire[0].position);
        }

        PonerParedes(false);
        if (salidaSiguienteEscena != null) salidaSiguienteEscena.SetActive(false);

        PonerBool("EnAire", true);
        StartCoroutine(RutinaJefe());
    }

    protected override void Update()
    {
        base.Update();
        if (estaMuerto) return;

        // siempre mira hacia el jugador
        if (jugador != null)
        {
            ActualizarDireccion(jugador.position.x > transform.position.x ? 1 : -1);
        }
    }

    // ---------------- vida ----------------

    // mientras vuela es invulnerable. En los ciclos que no son el ultimo NUNCA muere (queda con 1 de vida como minimo)
    public override void RecibirDaño(int cantidad)
    {
        if (invulnerable || estaMuerto) return;

        bool ultimoCiclo = ciclos == null || cicloActual >= ciclos.Length - 1;

        if (!ultimoCiclo)
        {
            int maximoPermitido = vidaActual - 1;
            if (maximoPermitido <= 0) return;
            cantidad = Mathf.Min(cantidad, maximoPermitido);
        }

        base.RecibirDaño(cantidad);
    }

    // secuencia de derrota
    protected override void Morir()
    {
        Debug.Log("[JefeFinal] derrotado");

        disparando = false;
        invulnerable = true;
        PonerBool("EnAire", false);
        if (visual != null) visual.enabled = true;

        StopAllCoroutines();
        PonerParedes(false);

        if (salidaSiguienteEscena != null) salidaSiguienteEscena.SetActive(true);
        alDerrotar.Invoke();

        // el jefe tiene que seguir existiendo hasta que cambie la escena (si no, la rutina se corta)
        if (cambiarEscenaAlDerrotar) tiempoAntesDeDesaparecer = Mathf.Max(tiempoAntesDeDesaparecer, retardoCambioEscena + 2f);

        base.Morir(); // trigger "Muerto", queda inmovil y se destruye despues de "Tiempo Antes De Desaparecer"

        if (cambiarEscenaAlDerrotar) StartCoroutine(CambiarEscenaTrasMorir());
    }

    IEnumerator CambiarEscenaTrasMorir()
    {
        yield return new WaitForSeconds(retardoCambioEscena);

        if (string.IsNullOrEmpty(nombreEscenaSiguiente))
        {
            Debug.LogWarning("[JefeFinal] falta el Nombre Escena Siguiente");
            yield break;
        }

        if (TransicionEscena.instancia != null)
        {
            TransicionEscena.instancia.IrAEscena(nombreEscenaSiguiente); // con fundido a negro
        }
        else
        {
            SceneManager.LoadScene(nombreEscenaSiguiente);
        }
    }

    // ---------------- rutina principal ----------------

    IEnumerator RutinaJefe()
    {
        // 0) espera al jugador
        while (!JugadorEnArena()) yield return null;

        yield return new WaitForSeconds(retardoInicio);

        Debug.Log("[JefeFinal] empieza la pelea");
        PonerParedes(true);
        alIniciarPelea.Invoke();

        if (ciclos == null || ciclos.Length == 0)
        {
            Debug.LogWarning("[JefeFinal] no cargaste ningun ciclo");
            yield break;
        }

        for (cicloActual = 0; cicloActual < ciclos.Length; cicloActual++)
        {
            CicloJefe ciclo = ciclos[cicloActual];
            Debug.Log("[JefeFinal] ciclo " + (cicloActual + 1) + " (" + ciclo.nombre + ")");

            // ---- EN EL AIRE: dispara sin parar ----
            if (cicloActual > 0) yield return Teletransportar();

            invulnerable = true;
            disparando = true;
            StartCoroutine(RutinaDisparo());

            // minions
            if (ciclo.minions != null)
            {
                ciclo.minions.ActivarDesdeAfuera();

                while (!ciclo.minions.OleadaTerminada() || HayEnemigosVivosDe(ciclo.minions))
                {
                    yield return new WaitForSeconds(0.25f);
                }
            }

            yield return new WaitForSeconds(pausaEntreFases);

            // subjefes del ciclo
            List<Subjefe> subjefes = SpawnearSubjefes(ciclo);

            while (HaySubjefesVivos(subjefes))
            {
                yield return new WaitForSeconds(0.25f);
            }

            // ---- BAJA: se le puede pegar ----
            disparando = false;
            yield return new WaitForSeconds(pausaEntreFases);

            yield return IrAPunto(puntoSuelo != null ? puntoSuelo.position : transform.position);
            PonerBool("EnAire", false);
            invulnerable = false;
            Debug.Log("[JefeFinal] abajo, vulnerable");

            bool esUltimo = cicloActual >= ciclos.Length - 1;
            float inicioAbajo = Time.time;

            // el ultimo ciclo termina en Morir() (que corta esta rutina)
            while (!estaMuerto)
            {
                if (!esUltimo && vidaActual <= vidaMaxima * ciclo.porcentajeVidaFin) break;
                if (!esUltimo && ciclo.tiempoMaximoAbajo > 0f && Time.time >= inicioAbajo + ciclo.tiempoMaximoAbajo) break;
                yield return null;
            }

            if (estaMuerto) yield break;

            // ---- SUBE de nuevo ----
            invulnerable = true;
            PonerBool("EnAire", true);
            yield return IrAPunto(PuntoAire(indiceAireActual));
        }
    }

    // dispara al jugador mientras "disparando" sea true (en el aire). Los disparos atraviesan paredes
    IEnumerator RutinaDisparo()
    {
        while (disparando && !estaMuerto)
        {
            if (jugador != null && prefabProyectil != null && Time.time >= tiempoUltimoDisparo + cooldownDisparo)
            {
                tiempoUltimoDisparo = Time.time;
                Disparar();
            }

            yield return null;
        }
    }

    void Disparar()
    {
        Vector3 origen = puntoDisparo != null ? puntoDisparo.position : transform.position;
        Vector2 direccionDisparo = ((Vector2)jugador.position - (Vector2)origen).normalized;

        if (puntoDisparo == null) origen += (Vector3)(direccionDisparo * distanciaSpawnProyectil);

        PonerTrigger("Disparando");

        GameObject nuevo = Instantiate(prefabProyectil, origen, Quaternion.identity);
        Proyectil proyectil = nuevo.GetComponent<Proyectil>();

        if (proyectil != null)
        {
            proyectil.daño = dañoProyectil;
            proyectil.velocidad = velocidadProyectil;
            proyectil.atravesarParedes = true; // ignora paredes y obstaculos
            proyectil.Configurar(direccionDisparo, true, gameObject); // true = hiere al Jugador
        }
    }

    // ---------------- movimiento ----------------

    IEnumerator Teletransportar()
    {
        if (puntosAire == null || puntosAire.Length == 0) yield break;

        invulnerable = true;
        PonerTrigger("Teletransporte");

        // desaparece, cambia de lugar y reaparece
        if (visual != null) visual.enabled = false;
        yield return new WaitForSeconds(duracionDesaparecer);

        // elige un punto distinto al actual
        if (puntosAire.Length > 1)
        {
            int nuevo = indiceAireActual;
            while (nuevo == indiceAireActual) nuevo = Random.Range(0, puntosAire.Length);
            indiceAireActual = nuevo;
        }

        PonerEn(PuntoAire(indiceAireActual));

        yield return new WaitForSeconds(duracionDesaparecer);
        if (visual != null) visual.enabled = true;
    }

    Vector3 PuntoAire(int indice)
    {
        if (puntosAire != null && indice >= 0 && indice < puntosAire.Length && puntosAire[indice] != null)
        {
            return puntosAire[indice].position;
        }

        return transform.position;
    }

    void PonerEn(Vector3 posicion)
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.position = posicion;
        }

        transform.position = posicion;
    }

    IEnumerator IrAPunto(Vector3 destino)
    {
        while (Vector2.Distance(rb.position, destino) > 0.05f)
        {
            if (estaMuerto) yield break;

            rb.linearVelocity = Vector2.zero; // en Kinematic la velocidad sobrante lo dejaria yendose
            rb.position = Vector2.MoveTowards(rb.position, destino, velocidadTransicion * Time.deltaTime);
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        rb.position = destino;
    }

    // ---------------- ayudas ----------------

    bool JugadorEnArena()
    {
        if (jugador == null) return false;
        if (zonaArena == null) return true;
        return zonaArena.OverlapPoint(jugador.position);
    }

    bool HayEnemigosVivosDe(SpawnerEnemigos spawner)
    {
        Enemigo[] todos = FindObjectsByType<Enemigo>(FindObjectsSortMode.None);

        foreach (Enemigo e in todos)
        {
            if (e != null && e.spawner == spawner) return true;
        }

        return false;
    }

    List<Subjefe> SpawnearSubjefes(CicloJefe ciclo)
    {
        List<Subjefe> lista = new List<Subjefe>();

        if (ciclo.prefabsSubjefes == null) return lista;

        for (int i = 0; i < ciclo.prefabsSubjefes.Length; i++)
        {
            if (ciclo.prefabsSubjefes[i] == null) continue;
            if (ciclo.puntosSubjefes == null || i >= ciclo.puntosSubjefes.Length || ciclo.puntosSubjefes[i] == null) continue;

            GameObject nuevo = Instantiate(ciclo.prefabsSubjefes[i], ciclo.puntosSubjefes[i].position, Quaternion.identity);
            Subjefe subjefe = nuevo.GetComponent<Subjefe>();
            if (subjefe != null) lista.Add(subjefe);
        }

        return lista;
    }

    bool HaySubjefesVivos(List<Subjefe> lista)
    {
        lista.RemoveAll(x => x == null);

        // un subjefe que se cae del mapa se elimina, si no la pelea queda trabada esperandolo
        foreach (Subjefe s in lista.ToArray())
        {
            if (!s.estaPoseido && s.transform.position.y < limiteCaidaY)
            {
                Destroy(s.gameObject);
                lista.Remove(s);
            }
        }

        foreach (Subjefe s in lista)
        {
            if (!s.estaMuerto) return true;
        }

        return false;
    }

    void PonerParedes(bool activas)
    {
        if (paredes == null) return;

        foreach (GameObject pared in paredes)
        {
            if (pared != null) pared.SetActive(activas);
        }
    }

    // ---- Animator seguro: solo toca parametros que existen ----
    void PonerBool(string nombre, bool valor)
    {
        if (animator != null && parametrosAnimator.Contains(nombre)) animator.SetBool(nombre, valor);
    }

    void PonerTrigger(string nombre)
    {
        if (animator != null && parametrosAnimator.Contains(nombre)) animator.SetTrigger(nombre);
    }
}