using UnityEngine;

// Poné este script en un GameObject con Collider2D marcado "Is Trigger",
// en la plataforma/zona donde quieras esta secuencia.
public class ControladorOleadaSubjefes : MonoBehaviour
{
    [Header("Minions primero (Spawner de Enemigos normales)")]
    public SpawnerEnemigos spawnerEnemigos;

    [Header("Subjefes que aparecen despues de matarlos a todos")]
    public GameObject[] prefabsSubjefes; // los 2 prefabs (pueden repetirse el mismo)
    public Transform[] puntosSpawnSubjefes; // 2 posiciones, una por cada Subjefe

    [Header("Reposicion (opcional): si desaparece el SubjefeDisparador, aparece otro")]
    public bool reponerDisparador = false; // dejalo destildado si no lo necesitas (todo funciona como antes)
    public Jugador jugador; // para saber si lo estas poseyendo
    public GameObject prefabRepuesto; // el prefab del SubjefeDisparador que se repone
    public Transform puntoRepuesto; // donde reaparece
    public float retardoRespawn = 1f; // espera antes de reponerlo, para que no aparezca de golpe

    private bool activado = false;
    private bool subjefesYaSpawnados = false;
    private float tiempoSinDisparador = -1f;

    void OnTriggerEnter2D(Collider2D otro)
    {
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

    void Update()
    {
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

    // si no hay ningun SubjefeDisparador VIVO en la escena y no estas poseyendo uno
    // (lo mataron, o lo poseiste y lo soltaste), espera un ratito y repone otro
    void AsegurarDisparador()
    {
        if (jugador != null && jugador.estaPoseyendo)
        {
            tiempoSinDisparador = -1f;
            return;
        }

        // contamos solo los vivos: uno muerto queda unos segundos en escena por la animacion
        SubjefeDisparador[] todos = FindObjectsByType<SubjefeDisparador>(FindObjectsSortMode.None);

        foreach (SubjefeDisparador s in todos)
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
                Instantiate(prefabRepuesto, puntoRepuesto.position, Quaternion.identity);
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
            Instantiate(prefabsSubjefes[i], puntosSpawnSubjefes[i].position, Quaternion.identity);
        }
    }
}