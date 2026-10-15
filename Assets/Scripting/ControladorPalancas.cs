using UnityEngine;

public class ControladorPalancas : MonoBehaviour
{
    [Header("Palanca 2: aparecen minions")]
    public SpawnerEnemigos spawnerEnemigos; // el spawner de enemigos normales

    [Header("Palanca 3: aparecen Subjefes golpeadores (melee)")]
    public GameObject prefabSubjefeGolpeador;
    public Transform[] puntosSpawnGolpeadores;

    [Header("Palanca 4: aparecen Subjefes disparadores")]
    public GameObject prefabSubjefeDisparador;
    public Transform[] puntosSpawnDisparadores;

    [Header("Puerta final: se abre solo cuando se activaron las 4 palancas (en cualquier orden)")]
    public PuertaInteractiva puertaFinal;

    private bool[] palancaActivada = new bool[5]; // indices 1 a 4; el 0 no se usa

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
        if (numeroPalanca < 1 || numeroPalanca > 4) return;
        if (palancaActivada[numeroPalanca]) return; // esa palanca ya se uso

        palancaActivada[numeroPalanca] = true;
        Debug.Log("Palanca " + numeroPalanca + " activada (" + ContarActivadas() + "/4)");

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
                break;
        }

        // la puerta final se abre recien cuando estan las 4, sin importar el orden
        if (ContarActivadas() >= 4 && puertaFinal != null)
        {
            puertaFinal.Desbloquear();
        }
    }

    int ContarActivadas()
    {
        int total = 0;
        for (int i = 1; i <= 4; i++)
        {
            if (palancaActivada[i]) total++;
        }
        return total;
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