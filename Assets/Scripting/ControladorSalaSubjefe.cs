using UnityEngine;

// Maneja toda la secuencia: activa cada Diana en su momento, spawnea enemigos y
// los 4 Subjefes Disparadores hostiles, y se asegura de que SIEMPRE haya un
// SubjefeDisparador disponible para poseer (si no, el jugador queda trabado
// porque solo el SubjefeDisparador le puede pegar a las Dianas).
public class ControladorSalaSubjefe : MonoBehaviour
{
    private enum Etapa
    {
        EsperandoPosesion,
        EsperandoDisparoDiana1,
        EsperandoEnemigos,
        EsperandoDisparoDiana2,
        EsperandoSubjefesHostiles,
        EsperandoDisparoDiana3,
        Completo
    }

    [Header("Jugador")]
    public Jugador jugador;

    [Header("Respawn del SubjefeDisparador (para no trabar al jugador)")]
    public GameObject prefabSubjefeDisparador;
    public Transform puntoRespawnSubjefe;

    [Header("Etapa 1: primera Diana")]
    public Diana diana1;

    [Header("Etapa 2: enemigos normales")]
    public SpawnerEnemigos spawnerEnemigos;
    public Diana diana2;

    [Header("Etapa 3: 4 Subjefes Disparadores hostiles")]
    public GameObject prefabSubjefeDisparadorHostil; // mismo prefab del SubjefeDisparador
    public Transform[] puntosSpawnHostiles; // asigna las 4 posiciones
    public Diana diana3;

    [Header("Puerta final")]
    public PuertaInteractiva puertaFinal;

    private Etapa etapaActual = Etapa.EsperandoPosesion;
    private GameObject[] subjefesHostilesActivos;

    void Start()
    {
        // todo arranca oculto/bloqueado hasta que le toque su momento
        if (diana1 != null) diana1.gameObject.SetActive(false);
        if (diana2 != null) diana2.gameObject.SetActive(false);
        if (diana3 != null) diana3.gameObject.SetActive(false);
        if (puertaFinal != null) puertaFinal.Bloquear();
    }

    void Update()
    {
        switch (etapaActual)
        {
            case Etapa.EsperandoPosesion:
                AsegurarSubjefeDisponible();

                if (jugador != null && jugador.estaPoseyendo)
                {
                    ActivarDiana1();
                }
                break;

            case Etapa.EsperandoDisparoDiana1:
            case Etapa.EsperandoDisparoDiana2:
            case Etapa.EsperandoDisparoDiana3:
                AsegurarSubjefeDisponible();
                break;

            case Etapa.EsperandoEnemigos:
                AsegurarSubjefeDisponible();
                RevisarEnemigosMuertos();
                break;

            case Etapa.EsperandoSubjefesHostiles:
                AsegurarSubjefeDisponible();
                RevisarSubjefesHostilesEliminados();
                break;
        }
    }

    void ActivarDiana1()
    {
        if (diana1 != null) diana1.gameObject.SetActive(true);
        etapaActual = Etapa.EsperandoDisparoDiana1;
    }

    void RevisarEnemigosMuertos()
    {
        Enemigo[] enemigosVivos = FindObjectsByType<Enemigo>(FindObjectsSortMode.None);
        if (enemigosVivos.Length == 0)
        {
            if (diana2 != null) diana2.gameObject.SetActive(true);
            etapaActual = Etapa.EsperandoDisparoDiana2;
        }
    }

    void SpawnearSubjefesHostiles()
    {
        subjefesHostilesActivos = new GameObject[puntosSpawnHostiles.Length];

        for (int i = 0; i < puntosSpawnHostiles.Length; i++)
        {
            subjefesHostilesActivos[i] = Instantiate(
                prefabSubjefeDisparadorHostil,
                puntosSpawnHostiles[i].position,
                Quaternion.identity);
        }
    }

    void RevisarSubjefesHostilesEliminados()
    {
        foreach (GameObject subjefeObj in subjefesHostilesActivos)
        {
            if (subjefeObj == null) continue; // murio: ya no cuenta

            Subjefe subjefe = subjefeObj.GetComponent<Subjefe>();
            if (subjefe != null && !subjefe.estaPoseido)
            {
                return; // sigue habiendo al menos uno hostil sin resolver
            }
        }

        // todos muertos o poseidos: se termino esta etapa
        if (diana3 != null) diana3.gameObject.SetActive(true);
        etapaActual = Etapa.EsperandoDisparoDiana3;
    }

    // llamado por las Dianas (numeroDiana = 1, 2 o 3) cuando las golpean
    public void NotificarDianaGolpeada(int numeroDiana)
    {
        Debug.Log("NotificarDianaGolpeada(" + numeroDiana + ") llamado. etapaActual=" + etapaActual);

        if (numeroDiana == 1 && etapaActual == Etapa.EsperandoDisparoDiana1)
        {
            Debug.Log("Condicion cumplida: activando spawner de enemigos");
            if (spawnerEnemigos != null) spawnerEnemigos.ActivarDesdeAfuera();
            etapaActual = Etapa.EsperandoEnemigos;
        }
        else if (numeroDiana == 2 && etapaActual == Etapa.EsperandoDisparoDiana2)
        {
            SpawnearSubjefesHostiles();
            etapaActual = Etapa.EsperandoSubjefesHostiles;
        }
        else if (numeroDiana == 3 && etapaActual == Etapa.EsperandoDisparoDiana3)
        {
            if (puertaFinal != null) puertaFinal.Desbloquear();
            etapaActual = Etapa.Completo;
        }
        else
        {
            Debug.LogWarning("Ninguna condicion coincidio para diana " + numeroDiana + " en etapa " + etapaActual);
        }
    }

    // corre en TODAS las etapas donde haga falta un SubjefeDisparador: si el
    // jugador no esta poseyendo ninguno Y no hay ninguno libre en la escena
    // para poseer, respawnea uno nuevo (asi nunca te quedas sin forma de continuar)
    void AsegurarSubjefeDisponible()
    {
        if (jugador != null && jugador.estaPoseyendo) return;

        SubjefeDisparador[] disponibles = FindObjectsByType<SubjefeDisparador>(FindObjectsSortMode.None);
        if (disponibles.Length > 0) return;

        if (prefabSubjefeDisparador != null && puntoRespawnSubjefe != null)
        {
            Instantiate(prefabSubjefeDisparador, puntoRespawnSubjefe.position, Quaternion.identity);
        }
    }
}