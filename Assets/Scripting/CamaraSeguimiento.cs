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

    private Camera camara;
    private float sizeOriginal; // el zoom normal, para volver a el al salir de una zona
    private float sizeDeseado;

    void Start()
    {
        offsetZ = transform.position.z;
        camara = GetComponent<Camera>();

        if (camara != null)
        {
            sizeOriginal = camara.orthographicSize;
            sizeDeseado = sizeOriginal;
        }
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

        if (camara != null)
        {
            camara.orthographicSize = Mathf.Lerp(camara.orthographicSize, sizeDeseado, suavizado * Time.deltaTime);
        }
    }

    public void CambiarObjetivo(Transform nuevoObjetivo)
    {
        objetivo = nuevoObjetivo;
    }

    // llamado por una ZonaCamaraFija cuando el jugador entra a un area de pantalla fija.
    // "nuevoSize" es opcional: si le pasas un numero <= 0, mantiene el zoom que tenia
    public void FijarCamara(Vector3 posicion, float nuevoSize = -1f)
    {
        camaraFija = true;
        posicionFija = new Vector3(posicion.x, posicion.y, offsetZ);

        if (nuevoSize > 0f)
        {
            sizeDeseado = nuevoSize;
        }
    }

    // llamado al salir del area, para que vuelva a seguir al objetivo normal
    public void VolverASeguir()
    {
        camaraFija = false;
        sizeDeseado = sizeOriginal; // tambien vuelve al zoom original
    }
}