using UnityEngine;

public class CamaraSeguimiento : MonoBehaviour
{
    [Header("A quien sigue")]
    public Transform objetivo; // arrastra el Jugador aca desde el Inspector

    [Header("Suavizado")]
    public float suavizado = 5f; // mas alto = sigue mas rapido/brusco

    private float offsetZ; // guardamos el Z original de la camara para no romper la perspectiva 2D

    private bool camaraFija = false;
    private Vector3 posicionFija;

    void Start()
    {
        offsetZ = transform.position.z;
    }

    void LateUpdate()
    {
        Vector3 posicionDeseada;

        if (camaraFija)
        {
            posicionDeseada = posicionFija;
        }
        else
        {
            if (objetivo == null) return;
            posicionDeseada = new Vector3(objetivo.position.x, objetivo.position.y, offsetZ);
        }

        transform.position = Vector3.Lerp(transform.position, posicionDeseada, suavizado * Time.deltaTime);
    }

    public void CambiarObjetivo(Transform nuevoObjetivo)
    {
        objetivo = nuevoObjetivo;
    }

    // llamado por una ZonaCamaraFija cuando el jugador entra a un area de pantalla fija
    public void FijarCamara(Vector3 posicion)
    {
        camaraFija = true;
        posicionFija = new Vector3(posicion.x, posicion.y, offsetZ);
    }

    // llamado al salir del area, para que vuelva a seguir al objetivo normal
    public void VolverASeguir()
    {
        camaraFija = false;
    }
}