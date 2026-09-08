using UnityEngine;
using System.Collections;

public class CamaraSeguimiento : MonoBehaviour
{
    [Header("A quien sigue")]
    public Transform objetivo; // arrastra el Jugador aca desde el Inspector

    [Header("Suavizado")]
    public float suavizado = 5f; // mas alto = sigue mas rapido/brusco

    [Header("Zoom inicial (efecto de intro)")]
    public bool hacerZoomInicial = true;
    public float sizeInicial = 1f; // con cuanto zoom arranca (mas chico = mas cerca)
    public float duracionZoomInicial = 2f; // cuanto tarda en llegar al zoom normal
    public Transform puntoInicial; // arrastra aca un GameObject vacio: es donde se centra el zoom. Si lo dejas vacio, usa la posicion actual de la camara

    private float offsetZ; // guardamos el Z original de la camara para no romper la perspectiva 2D
    private bool camaraFija = false;
    private Vector3 posicionFija;
    private Camera camara;
    private float sizeOriginal; // el zoom normal (el que configuraste en el Inspector), para volver a el
    private float sizeDeseado;
    private bool haciendoZoomInicial = false;

    void Start()
    {
        offsetZ = transform.position.z;
        camara = GetComponent<Camera>();

        if (camara != null)
        {
            sizeOriginal = camara.orthographicSize; // ej: 12, el zoom normal de juego
            sizeDeseado = sizeOriginal;

            if (hacerZoomInicial)
            {
                // si asignaste un punto, la camara arranca centrada ahi (fija, sin seguirte todavia)
                if (puntoInicial != null)
                {
                    transform.position = new Vector3(puntoInicial.position.x, puntoInicial.position.y, offsetZ);
                }

                camara.orthographicSize = sizeInicial;
                haciendoZoomInicial = true;
                StartCoroutine(ZoomInicial());
            }
        }
    }

    IEnumerator ZoomInicial()
    {
        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracionZoomInicial)
        {
            tiempoTranscurrido += Time.deltaTime;
            float t = tiempoTranscurrido / duracionZoomInicial;
            camara.orthographicSize = Mathf.Lerp(sizeInicial, sizeOriginal, t);
            yield return null;
        }

        camara.orthographicSize = sizeOriginal;

        // al terminar el zoom, la camara se pone en X:0 Y:0 (con su Z original)
        transform.position = new Vector3(0f, 0f, offsetZ);

        haciendoZoomInicial = false;
    }

    void LateUpdate()
    {
        // mientras dura el zoom inicial, la posicion se queda fija en el punto elegido
        // (no sigue al jugador todavia) - recien termina cuando termina la corrutina
        if (haciendoZoomInicial) return;

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
        camara.orthographicSize = Mathf.Lerp(camara.orthographicSize, sizeDeseado, suavizado * Time.deltaTime);
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