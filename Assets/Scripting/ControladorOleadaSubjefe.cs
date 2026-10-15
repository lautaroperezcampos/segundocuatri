using UnityEngine;
using System.Collections.Generic;

// Poné este script en un GameObject con Collider2D marcado "Is Trigger",
// en la plataforma/zona donde quieras esta secuencia.
public class ControladorOleadaSubjefes : MonoBehaviour
{
    [Header("Minions primero (Spawner de Enemigos normales)")]
    public SpawnerEnemigos spawnerEnemigos;

    [Header("Subjefes que aparecen despues de matarlos a todos")]
    public GameObject[] prefabsSubjefes; // los 2 prefabs (pueden repetirse el mismo)
    public Transform[] puntosSpawnSubjefes; // 2 posiciones, una por cada Subjefe

    [Header("Reposicion (opcional): si mueren todos los subjefes de ESTE controlador, aparece otro")]
    public bool reponerDisparador = false; // dejalo destildado si no lo necesitas (todo funciona como antes)
    public Jugador jugador; // para saber si lo estas poseyendo
    public GameObject prefabRepuesto; // el prefab del SubjefeDisparador que se repone
    public Transform puntoRepuesto; // donde reaparece
    public float retardoRespawn = 1f; // espera antes de reponerlo, para que no aparezca de golpe

    private bool activado = false;
    private bool subjefesYaSpawnados = false;
    private float tiempoSinDisparador = -1f;

    // DIAGNOSTICO TEMPORAL: si esto no aparece apenas das Play, el componente no esta en la escena o esta apagado
    void Start()
    {
        Debug.Log("[OleadaSubjefes '" + name + "'] ARRANCO. Collider2D=" + (GetComponent<Collider2D>() != null)
            + " isTrigger=" + (GetComponent<Collider2D>() != null && GetComponent<Collider2D>().isTrigger));
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        Debug.Log("[OleadaSubjefes '" + name + "'] entro algo al trigger: " + otro.name);

        if (activado) return;

        // se activa si entra el Jugador, O un Subjefe que el jugador este poseyendo
        // (mientras poseés, el que se mueve de verdad es el Subjefe)
        bool esJugador = otro.GetComponent<Jugador>() != null;
        Subjefe subjefe = otro.GetComponent<Subjefe>();
        bool esSubjefePoseido = subjefe != null && subjefe.estaPoseido;

        if (!esJugador && !esSubjefePoseido) return;

        activado = true;

        if (spawnerEnemigos != null)
        {
            spawnerEnemigos.ActivarDesdeAfuera();
        }
    }

    private float proximoLogDiagnostico = 0f;

    // SOLO los subjefes que creo ESTE controlador (los 2 iniciales y sus repuestos).
    // Asi varios controladores en el mapa no se pisan entre si.
    private List<Subjefe> misSubjefes = new List<Subjefe>();

    void Update()
    {
        // DIAGNOSTICO TEMPORAL: una vez por segundo dice en que estado esta este controlador
        if (Time.time >= proximoLogDiagnostico)
        {
            proximoLogDiagnostico = Time.time + 1f;
            Debug.Log("[OleadaSubjefes '" + name + "'] activado=" + activado
                + " subjefesYaSpawnados=" + subjefesYaSpawnados
                + " reponerDisparador=" + reponerDisparador
                + " jugadorAsignado=" + (jugador != null)
                + " poseyendo=" + (jugador != null && jugador.estaPoseyendo)
                + " prefabRepuesto=" + (prefabRepuesto != null)
                + " puntoRepuesto=" + (puntoRepuesto != null));
        }

        if (!activado) return;

        // ya aparecieron los subjefes: lo unico que queda por hacer es reponer si hace falta
        if (subjefesYaSpawnados)
        {
            if (reponerDisparador)
            {
                AsegurarDisparador();
            }
            return;
        }

        // sin spawner asignado = esta zona no tiene minions: los subjefes aparecen apenas
        // se activa, sin esperar a que mueran enemigos de otros lados de la escena
        if (spawnerEnemigos == null)
        {
            SpawnearSubjefes();
            return;
        }

        Enemigo[] enemigosVivos = FindObjectsByType<Enemigo>(FindObjectsSortMode.None);

        bool oleadaTermino = spawnerEnemigos.OleadaTerminada();

        if (enemigosVivos.Length == 0 && oleadaTermino)
        {
            SpawnearSubjefes();
        }
    }

    // si NINGUNO de los subjefes de ESTE controlador sigue vivo y no estas poseyendo uno
    // (los mataron, o lo poseiste y lo soltaste), espera un ratito y repone otro.
    // Los subjefes de otros controladores o puestos a mano no cuentan.
    void AsegurarDisparador()
    {
        if (jugador != null && jugador.estaPoseyendo)
        {
            tiempoSinDisparador = -1f;
            return;
        }

        // sacamos de la lista los que ya fueron destruidos
        misSubjefes.RemoveAll(x => x == null);

        // contamos solo los vivos: uno muerto queda unos segundos en escena por la animacion
        foreach (Subjefe s in misSubjefes)
        {
            if (!s.estaMuerto)
            {
                tiempoSinDisparador = -1f;
                return;
            }
        }

        if (tiempoSinDisparador < 0f)
        {
            tiempoSinDisparador = Time.time;
        }

        if (Time.time >= tiempoSinDisparador + retardoRespawn)
        {
            if (prefabRepuesto != null && puntoRepuesto != null)
            {
                GameObject repuesto = Instantiate(prefabRepuesto, puntoRepuesto.position, Quaternion.identity);
                Subjefe scriptRepuesto = repuesto.GetComponent<Subjefe>();
                if (scriptRepuesto != null) misSubjefes.Add(scriptRepuesto);

                Debug.Log("[OleadaSubjefes '" + name + "'] REPONIENDO '" + repuesto.name + "' en " + puntoRepuesto.position);
            }
            else
            {
                Debug.LogWarning("ControladorOleadaSubjefes: falta asignar Prefab Repuesto o Punto Repuesto en '" + name + "', no puede reponer el Subjefe");
            }

            tiempoSinDisparador = -1f;
        }
    }

    // llamalo desde otro script cuando ya no haga falta reponer mas
    public void DetenerReposicion()
    {
        reponerDisparador = false;
    }

    void SpawnearSubjefes()
    {
        subjefesYaSpawnados = true;

        int cantidad = Mathf.Min(prefabsSubjefes.Length, puntosSpawnSubjefes.Length);

        for (int i = 0; i < cantidad; i++)
        {
            GameObject nuevo = Instantiate(prefabsSubjefes[i], puntosSpawnSubjefes[i].position, Quaternion.identity);
            Subjefe scriptSubjefe = nuevo.GetComponent<Subjefe>();
            if (scriptSubjefe != null) misSubjefes.Add(scriptSubjefe);
        }
    }
}