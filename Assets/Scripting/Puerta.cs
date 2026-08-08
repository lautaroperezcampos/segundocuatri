using UnityEngine;

public class Puerta : MonoBehaviour
{
    [Header("Vida")]
    public int vidaMaxima = 30;
    public int vidaActual;

    void Start()
    {
        vidaActual = vidaMaxima;
    }

    public void RecibirDaño(int cantidad)
    {
        vidaActual -= cantidad;
        Debug.Log("Puerta recibio daño, vida restante: " + vidaActual);

        if (vidaActual <= 0)
        {
            Romper();
        }
    }

    void Romper()
    {
        Debug.Log("La puerta se rompio");
        Destroy(gameObject);
        // aca despues podemos sumar un efecto visual o sonido de puerta rota
    }
}