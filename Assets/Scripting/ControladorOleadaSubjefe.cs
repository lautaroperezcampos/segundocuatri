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

    private bool activado = false;
    private bool subjefesYaSpawnados = false;

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (activado) return;
        if (otro.GetComponent<Jugador>() == null) return;

        activado = true;

        if (spawnerEnemigos != null)
        {
            spawnerEnemigos.ActivarDesdeAfuera();
        }
    }

    void Update()
    {
        if (!activado || subjefesYaSpawnados) return;

        Enemigo[] enemigosVivos = FindObjectsByType<Enemigo>(FindObjectsSortMode.None);

        bool oleadaTermino = spawnerEnemigos == null || spawnerEnemigos.OleadaTerminada();

        if (enemigosVivos.Length == 0 && oleadaTermino)
        {
            SpawnearSubjefes();
        }
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