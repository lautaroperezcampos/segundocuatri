using UnityEngine;

public class Proyectil : MonoBehaviour
{
    [Header("Configuracion")]
    public float velocidad = 10f;
    public int daño = 15;
    public float vidaUtil = 3f; // se autodestruye despues de este tiempo si no choca nada

    [Header("Paredes y plataformas")]
    public bool frenarEnPlataformas = true; // el disparo se destruye al chocar con piso, plataformas y paredes
    public bool atravesarParedes = false; // true = ignora paredes y obstaculos (lo usa el jefe final). Pisa a "Frenar En Plataformas"
    public LayerMask capaSuelo; // opcional: la capa "Suelo". Si la dejas vacia, frena con cualquier collider solido
    public float tiempoGracia = 0.05f; // al nacer ignora choques por este tiempo (por si sale pegado al piso)
    private float tiempoNacimiento;

    private Vector2 direccion = Vector2.right;
    private bool dañaAlJugador = false; // true = hiere al Jugador, false = hiere a Puerta/Enemigo/Subjefe/Diana
    private GameObject disparadoPor; // quien lo disparo, para no autoimpactarse

    // llamado justo despues de instanciarlo, para configurar hacia donde va y a quien le pega.
    // "disparador" es opcional: pasale el GameObject que lo dispara para que nunca se pegue a si mismo
    public void Configurar(Vector2 direccionDisparo, bool objetivoEsJugador, GameObject disparador = null)
    {
        direccion = direccionDisparo.normalized;
        dañaAlJugador = objetivoEsJugador;
        disparadoPor = disparador;

        // rotamos el sprite para que apunte hacia donde viaja
        float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angulo);

        Destroy(gameObject, vidaUtil);
    }

    void Awake()
    {
        tiempoNacimiento = Time.time;
    }

    void Update()
    {
        Vector3 desplazamiento = (Vector3)(direccion * velocidad * Time.deltaTime);

        // revisamos TODO el camino de este frame (no solo el punto final): asi un disparo rapido
        // no se "salta" una plataforma finita, y funciona igual en cualquier escena
        if (frenarEnPlataformas && !atravesarParedes && Time.time >= tiempoNacimiento + tiempoGracia)
        {
            RaycastHit2D[] impactos = Physics2D.RaycastAll(transform.position, direccion, desplazamiento.magnitude);

            foreach (RaycastHit2D impacto in impactos)
            {
                if (impacto.collider == null) continue;
                if (disparadoPor != null && impacto.collider.gameObject == disparadoPor) continue;

                if (EsSolido(impacto.collider))
                {
                    Destroy(gameObject);
                    return;
                }
            }
        }

        transform.position += desplazamiento;
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (disparadoPor != null && otro.gameObject == disparadoPor) return; // nunca nos pegamos a nosotros mismos

        // si choca con el piso / una plataforma / una pared, el disparo desaparece
        if (frenarEnPlataformas && !atravesarParedes && Time.time >= tiempoNacimiento + tiempoGracia && EsSolido(otro))
        {
            Destroy(gameObject);
            return;
        }

        if (dañaAlJugador)
        {
            Jugador jugador = otro.GetComponent<Jugador>();
            if (jugador != null)
            {
                jugador.RecibirDaño(daño);
                Destroy(gameObject);
            }
        }
        else
        {
            Puerta puerta = otro.GetComponent<Puerta>();
            if (puerta != null)
            {
                puerta.RecibirDaño(daño);
                Destroy(gameObject);
                return;
            }

            Enemigo enemigo = otro.GetComponent<Enemigo>();
            if (enemigo != null)
            {
                enemigo.MorirInstantaneo();
                Destroy(gameObject);
                return;
            }

            Diana diana = otro.GetComponent<Diana>();
            if (diana != null)
            {
                diana.RecibirImpacto();
                Destroy(gameObject);
                return;
            }

            Subjefe subjefe = otro.GetComponent<Subjefe>();
            if (subjefe != null)
            {
                subjefe.RecibirDaño(daño);
                Destroy(gameObject);
            }
        }
    }

    // true si el collider es piso / plataforma / pared (y no un personaje, puerta, diana u otro disparo)
    bool EsSolido(Collider2D otro)
    {
        // las plataformas que se pueden atravesar (con Platform Effector 2D) SIEMPRE frenan el disparo,
        // sin importar su capa ni si el collider esta en "Is Trigger" en esa escena
        if (otro.GetComponentInParent<PlatformEffector2D>() != null) return true;

        if (otro.isTrigger) return false;

        if (capaSuelo.value != 0)
        {
            return (capaSuelo.value & (1 << otro.gameObject.layer)) != 0;
        }

        // sin capa asignada: cualquier collider solido que no sea de un personaje u objeto "golpeable"
        return otro.GetComponentInParent<Jugador>() == null
            && otro.GetComponentInParent<Enemigo>() == null
            && otro.GetComponentInParent<Subjefe>() == null
            && otro.GetComponentInParent<Puerta>() == null
            && otro.GetComponentInParent<Diana>() == null
            && otro.GetComponentInParent<Proyectil>() == null;
    }
}