using UnityEngine;

// Cierra una zona con paredes invisibles cuando arranca la oleada de CUALQUIERA
// de los Spawners, y las abre (las desactiva) cuando ya salieron todos los minios
// de TODOS los Spawners y mataste a todos los minios de ESOS spawners.
// Las paredes son GameObjects con Collider2D (sin Sprite Renderer, asi no se ven).
public class ParedesInvisibles : MonoBehaviour
{
    [Header("Referencias")]
    public SpawnerEnemigos[] spawners; // todos los spawners de la zona
    public GameObject[] paredes; // las paredes invisibles (con Collider2D)

    [Header("Subjefe de repuesto (opcional)")]
    public bool aparecerSubjefeRepuesto = false; // tildalo para que aparezca uno cuando se cierran las paredes
    public GameObject prefabSubjefeRepuesto; // si esta vacio, no pasa nada aunque este tildado
    public Transform puntoRepuesto; // donde aparece (si esta vacio, no pasa nada)
    public bool soloSiNoHaySubjefeLibre = false; // tildado: solo aparece si no queda ningun subjefe libre en la escena
    private bool repuestoYaAparecido = false;

    [Header("Chequeo")]
    public float intervaloChequeo = 0.25f; // cada cuanto revisa si quedan minios vivos

    private bool paredesCerradas = false;
    private bool terminado = false;
    private float proximoChequeo = 0f;

    void Start()
    {
        // arrancan abiertas (desactivadas) hasta que empiece la oleada
        CambiarParedes(false);
    }

    void Update()
    {
        if (terminado || spawners == null || spawners.Length == 0) return;

        // la oleada arranco (alguno de los spawners se activo): cerramos las paredes
        if (!paredesCerradas)
        {
            if (AlgunSpawnerActivo())
            {
                paredesCerradas = true;
                CambiarParedes(true);
                AparecerSubjefeRepuesto();
            }
            return;
        }

        if (Time.time < proximoChequeo) return;
        proximoChequeo = Time.time + intervaloChequeo;

        // se abren cuando TODOS los spawners terminaron Y no queda ningun enemigo vivo
        // (solo cuentan los enemigos de ESTOS spawners: uno suelto en otra parte de la escena ya no las deja cerradas para siempre)
        if (TodosTerminaron() && !HayEnemigosVivosDeMisSpawners())
        {
            terminado = true;
            CambiarParedes(false);
        }
    }

    void AparecerSubjefeRepuesto()
    {
        if (!aparecerSubjefeRepuesto || repuestoYaAparecido) return;
        if (prefabSubjefeRepuesto == null || puntoRepuesto == null) return;

        if (soloSiNoHaySubjefeLibre && HaySubjefeLibre()) return;

        repuestoYaAparecido = true;
        Instantiate(prefabSubjefeRepuesto, puntoRepuesto.position, Quaternion.identity);
    }

    bool HaySubjefeLibre()
    {
        foreach (Subjefe sub in FindObjectsByType<Subjefe>(FindObjectsSortMode.None))
        {
            if (!sub.estaMuerto && !sub.estaPoseido) return true;
        }
        return false;
    }

    bool HayEnemigosVivosDeMisSpawners()
    {
        foreach (Enemigo e in FindObjectsByType<Enemigo>(FindObjectsSortMode.None))
        {
            if (e == null) continue;

            foreach (SpawnerEnemigos s in spawners)
            {
                if (s != null && e.spawner == s) return true;
            }
        }

        return false;
    }

    bool AlgunSpawnerActivo()
    {
        foreach (SpawnerEnemigos s in spawners)
        {
            if (s != null && s.yaSeActivo) return true;
        }
        return false;
    }

    bool TodosTerminaron()
    {
        foreach (SpawnerEnemigos s in spawners)
        {
            // un spawner que nunca se activo todavia tiene minios por salir
            if (s != null && (!s.yaSeActivo || !s.OleadaTerminada())) return false;
        }
        return true;
    }

    void CambiarParedes(bool activas)
    {
        foreach (GameObject pared in paredes)
        {
            if (pared != null)
            {
                pared.SetActive(activas);
            }
        }
    }
}