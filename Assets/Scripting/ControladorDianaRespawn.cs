using UnityEngine;

public class ControladorDianaConRespawn : MonoBehaviour
{
    [Header("Jugador")]
    public Jugador jugador;

    [Header("Subjefe necesario (se respawnea si muere o no hay ninguno libre)")]
    public GameObject prefabSubjefe; // el prefab del SubjefeDisparador (o el que uses)
    public Transform puntoRespawn;

    [Header("Puerta a abrir cuando le pegan a la Diana")]
    public PuertaInteractiva puerta;

    void Update()
    {
        AsegurarSubjefeDisponible();
    }

    // si el jugador no esta poseyendo ninguno Y no hay ninguno libre en la escena
    // para poseer, respawnea uno nuevo (asi nunca te quedas sin forma de pegarle a la diana)
    void AsegurarSubjefeDisponible()
    {
        if (jugador != null && jugador.estaPoseyendo) return;

        SubjefeDisparador[] disponibles = FindObjectsByType<SubjefeDisparador>(FindObjectsSortMode.None);
        if (disponibles.Length > 0) return;

        if (prefabSubjefe != null && puntoRespawn != null)
        {
            Instantiate(prefabSubjefe, puntoRespawn.position, Quaternion.identity);
        }
    }

    // conectá esto desde la Diana (campo "Controlador Simple" que le agregamos)
    public void AbrirPuerta()
    {
        if (puerta != null)
        {
            puerta.Desbloquear();
        }
    }
}