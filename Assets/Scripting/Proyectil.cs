using UnityEngine;

public class Proyectil : MonoBehaviour
{
    [Header("Configuracion")]
    public float velocidad = 10f;
    public int daño = 15;
    public float vidaUtil = 3f; // se autodestruye despues de este tiempo si no choca nada

    private Vector2 direccion = Vector2.right;
    private bool dañaAlJugador = false; // true = hiere al Jugador, false = hiere a Puerta/Enemigo

    // llamado justo despues de instanciarlo, para configurar hacia donde va y a quien le pega
    public void Configurar(Vector2 direccionDisparo, bool objetivoEsJugador)
    {
        direccion = direccionDisparo.normalized;
        dañaAlJugador = objetivoEsJugador;

        // rotamos el sprite para que apunte hacia donde viaja
        float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angulo);

        Destroy(gameObject, vidaUtil);
    }

    void Update()
    {
        transform.position += (Vector3)(direccion * velocidad * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
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
            }
        }
    }
}