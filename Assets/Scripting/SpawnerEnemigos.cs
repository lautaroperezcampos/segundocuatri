using UnityEngine;
using System.Collections;

public class SpawnerEnemigos : MonoBehaviour
{
    [Header("Configuracion")]
    public GameObject prefabEnemigo; // arrastra aca el prefab del Enemigo
    public int cantidad = 15; // total a spawnear en toda la oleada
    public int maxSimultaneos = 5; // cuantos como maximo puede haber vivos a la vez
    public float radioSpawn = 8f; // que tan lejos del spawner pueden aparecer (en X)
    public float intervaloChequeo = 1.5f; // cada cuanto revisa si puede spawnear otro

    [Header("Deteccion de suelo")]
    public LayerMask capaSuelo; // la misma capa "Suelo" que usan Jugador y Subjefe
    public float alturaBusqueda = 10f; // desde cuanto mas arriba empieza a buscar el piso
    public float profundidadBusqueda = 20f; // que tan lejos hacia abajo busca

    [Header("Efecto de emerger")]
    public float profundidadInicial = 1.5f; // cuanto empieza hundido bajo tierra
    public float duracionEmerger = 0.4f; // cuanto tarda en subir

    [Header("Activacion")]
    public bool yaSeActivo = false; // para que no se dispare mas de una vez

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (yaSeActivo) return;

        // se activa si entra el Jugador normal, o el Subjefe (cuando esta poseido)
        bool esJugador = otro.GetComponent<Jugador>() != null;
        bool esSubjefe = otro.GetComponent<Subjefe>() != null;

        if (esJugador || esSubjefe)
        {
            yaSeActivo = true;
            ActivarSpawn();
        }
    }

    private int totalSpawnados = 0;
    private int vivosActuales = 0;

    void ActivarSpawn()
    {
        StartCoroutine(ManejarOleada());
    }

    IEnumerator ManejarOleada()
    {
        while (totalSpawnados < cantidad)
        {
            if (vivosActuales < maxSimultaneos)
            {
                SpawnearUno();
                totalSpawnados++;
                vivosActuales++;
            }

            yield return new WaitForSeconds(intervaloChequeo);
        }
    }

    // los Enemigos llaman esto cuando mueren, para liberar un lugar en la oleada
    public void NotificarMuerte()
    {
        vivosActuales--;
    }

    void SpawnearUno()
    {
        if (prefabEnemigo == null)
        {
            Debug.LogWarning("Falta asignar el Prefab Enemigo en el Spawner");
            return;
        }

        // posicion X aleatoria cerca del spawner
        float posX = transform.position.x + Random.Range(-radioSpawn, radioSpawn);

        // buscamos el suelo real debajo de esa posicion, tirando un rayo hacia abajo
        Vector2 origenRaycast = new Vector2(posX, transform.position.y + alturaBusqueda);
        RaycastHit2D impacto = Physics2D.Raycast(origenRaycast, Vector2.down, profundidadBusqueda, capaSuelo);

        Vector3 posicionFinal;

        if (impacto.collider != null)
        {
            posicionFinal = new Vector3(posX, impacto.point.y, 0);
        }
        else
        {
            // si no encontro suelo ahi, usamos la altura del spawner como respaldo
            Debug.LogWarning("No se encontro suelo para el spawn en X=" + posX);
            posicionFinal = new Vector3(posX, transform.position.y, 0);
        }

        GameObject nuevoEnemigo = Instantiate(prefabEnemigo, posicionFinal, Quaternion.identity);

        Enemigo scriptEnemigo = nuevoEnemigo.GetComponent<Enemigo>();
        if (scriptEnemigo != null)
        {
            scriptEnemigo.spawner = this;
        }

        StartCoroutine(EmergerDeLaTierra(nuevoEnemigo, posicionFinal));
    }

    IEnumerator EmergerDeLaTierra(GameObject enemigo, Vector3 posicionFinal)
    {
        Rigidbody2D rb = enemigo.GetComponent<Rigidbody2D>();

        // durante la animacion, apagamos la fisica para que no pelee con el movimiento manual
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        Vector3 posicionInicial = posicionFinal + Vector3.down * profundidadInicial;
        enemigo.transform.position = posicionInicial;

        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracionEmerger)
        {
            tiempoTranscurrido += Time.deltaTime;
            float progreso = tiempoTranscurrido / duracionEmerger;
            enemigo.transform.position = Vector3.Lerp(posicionInicial, posicionFinal, progreso);
            yield return null;
        }

        enemigo.transform.position = posicionFinal;

        // terminada la animacion, devolvemos la fisica normal
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
        }
    }
}