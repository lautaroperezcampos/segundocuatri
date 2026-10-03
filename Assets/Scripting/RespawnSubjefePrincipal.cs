using UnityEngine;

// Vigila que siempre haya un Subjefe (el que rompe puertas a golpes) disponible.
// Si el jugador lo mata y no queda ninguno libre para poseer, respawnea uno nuevo -
// asi nunca te quedas sin forma de romper la puerta y seguir avanzando.
public class RespawnSubjefeInicial : MonoBehaviour
{
    [Header("Jugador")]
    public Jugador jugador;

    [Header("Subjefe a vigilar/respawnear")]
    public GameObject prefabSubjefe; // el prefab del Subjefe melee (el que rompe puertas)
    public Transform puntoRespawn;

    void Update()
    {
        AsegurarSubjefeDisponible();
    }

    void AsegurarSubjefeDisponible()
    {
        // si ya estamos poseyendo algo, no hace falta chequear mas
        if (jugador != null && jugador.estaPoseyendo) return;

        // buscamos especificamente Subjefes "melee" (no contamos los SubjefeDisparador,
        // que son otro tipo de titan aunque hereden de la misma clase base)
        Subjefe[] todos = FindObjectsByType<Subjefe>(FindObjectsSortMode.None);

        foreach (Subjefe s in todos)
        {
            if (s.GetComponent<SubjefeDisparador>() == null)
            {
                return; // ya hay al menos uno melee libre, no hace falta respawnear
            }
        }

        if (prefabSubjefe != null && puntoRespawn != null)
        {
            Instantiate(prefabSubjefe, puntoRespawn.position, Quaternion.identity);
        }
    }
}