using UnityEngine;

public class ControladorPalancas : MonoBehaviour
{
    [Header("Palanca 2: aparecen minions")]
    public SpawnerEnemigos spawnerEnemigos; // el spawner de enemigos normales

    [Header("Palanca 3: aparecen Subjefes golpeadores (melee)")]
    public GameObject prefabSubjefeGolpeador;
    public Transform[] puntosSpawnGolpeadores;

    [Header("Palanca 4: aparecen Subjefes disparadores, y se abre la puerta")]
    public GameObject prefabSubjefeDisparador;
    public Transform[] puntosSpawnDisparadores;
    public PuertaInteractiva puertaFinal;

    private int palancasActivadas = 0;

    void Start()
    {
        if (puertaFinal != null)
        {
            puertaFinal.Bloquear();
        }
    }

    // llamado por cada Palanca, pasando su propio numero (1, 2, 3 o 4)
    public void NotificarPalancaActivada(int numeroPalanca)
    {
        palancasActivadas++;
        Debug.Log("Palanca " + numeroPalanca + " activada (" + palancasActivadas + "/4)");

        switch (numeroPalanca)
        {
            case 1:
                // la primera no hace nada todavia, solo abre paso a la siguiente zona
                break;

            case 2:
                if (spawnerEnemigos != null)
                {
                    spawnerEnemigos.ActivarDesdeAfuera();
                }
                break;

            case 3:
                SpawnearEnPuntos(prefabSubjefeGolpeador, puntosSpawnGolpeadores);
                break;

            case 4:
                SpawnearEnPuntos(prefabSubjefeDisparador, puntosSpawnDisparadores);

                if (puertaFinal != null)
                {
                    puertaFinal.Desbloquear();
                }
                break;
        }
    }

    void SpawnearEnPuntos(GameObject prefab, Transform[] puntos)
    {
        if (prefab == null || puntos == null) return;

        foreach (Transform punto in puntos)
        {
            if (punto != null)
            {
                Instantiate(prefab, punto.position, Quaternion.identity);
            }
        }
    }
}