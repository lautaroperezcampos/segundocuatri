using UnityEngine;
using System.Collections.Generic;

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
    public bool exigirMatarGolpeadores = true; // tildado: ademas de las 4 palancas, hay que matar a los subjefes golpeadores de la palanca 3
    public bool exigirMatarDisparadores = false; // tildalo si tambien hay que matar a los disparadores de la palanca 4

    private List<GameObject> golpeadoresSpawneados = new List<GameObject>();
    private List<GameObject> disparadoresSpawneados = new List<GameObject>();
    private bool puertaYaAbierta = false;

    private bool[] palancaActivada = new bool[5]; // indices 1 a 4; el 0 no se usa

    void Start()
    {
        if (puertaFinal != null)
        {
            puertaFinal.Bloquear();
        }
    }

    void Update()
    {
        // la puerta puede abrirse despues de la ultima palanca, cuando muere el ultimo subjefe
        if (!puertaYaAbierta && ContarActivadas() >= 4)
        {
            RevisarPuerta();
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
                SpawnearEnPuntos(prefabSubjefeGolpeador, puntosSpawnGolpeadores, golpeadoresSpawneados);
                break;

            case 4:
                SpawnearEnPuntos(prefabSubjefeDisparador, puntosSpawnDisparadores, disparadoresSpawneados);
                break;
        }

        RevisarPuerta();
    }

    // la puerta final se abre cuando estan las 4 palancas Y (si lo pediste) ya no quedan subjefes por matar
    void RevisarPuerta()
    {
        if (puertaYaAbierta || puertaFinal == null) return;
        if (ContarActivadas() < 4) return;

        if (exigirMatarGolpeadores && !TodosResueltos(golpeadoresSpawneados)) return;
        if (exigirMatarDisparadores && !TodosResueltos(disparadoresSpawneados)) return;

        puertaYaAbierta = true;
        puertaFinal.Desbloquear();
    }

    // un subjefe cuenta como "resuelto" si murio/desaparecio, o si lo poseiste (asi no te trabas)
    bool TodosResueltos(List<GameObject> lista)
    {
        foreach (GameObject obj in lista)
        {
            if (obj == null) continue;

            Subjefe sj = obj.GetComponent<Subjefe>();
            if (sj != null && !sj.estaMuerto && !sj.estaPoseido) return false;
        }
        return true;
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

    void SpawnearEnPuntos(GameObject prefab, Transform[] puntos, List<GameObject> registro)
    {
        if (prefab == null || puntos == null) return;

        foreach (Transform punto in puntos)
        {
            if (punto != null)
            {
                GameObject nuevo = Instantiate(prefab, punto.position, Quaternion.identity);
                registro.Add(nuevo);
            }
        }
    }
}