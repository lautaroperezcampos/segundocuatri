using UnityEngine;

public class CamaraSeguimiento : MonoBehaviour
{
    [Header("A quien sigue")]
    public Transform objetivo; // arrastra el Jugador aca desde el Inspector

    [Header("Suavizado")]
    public float suavizado = 5f; // mas alto = sigue mas rapido/brusco

    private float offsetZ; // guardamos el Z original de la camara para no romper la perspectiva 2D

    void Start()
    {
        offsetZ = transform.position.z;
    }

    void LateUpdate()
    {
        if (objetivo == null) return;

        Vector3 posicionDeseada = new Vector3(objetivo.position.x, objetivo.position.y, offsetZ);
        transform.position = Vector3.Lerp(transform.position, posicionDeseada, suavizado * Time.deltaTime);
    }

    public void CambiarObjetivo(Transform nuevoObjetivo)
    {
        objetivo = nuevoObjetivo;
    }
}