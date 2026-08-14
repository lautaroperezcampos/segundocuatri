using UnityEngine;

public class ControladorDianas : MonoBehaviour
{
    [Header("Configuracion")]
    public int totalDianas = 3; // cuantas dianas hay que golpear en total
    public Puerta puertaARomper; // arrastra la Puerta que se abre al completar

    private int dianasGolpeadas = 0;

    public void NotificarDianaGolpeada()
    {
        dianasGolpeadas++;
        Debug.Log("Dianas golpeadas: " + dianasGolpeadas + "/" + totalDianas);

        if (dianasGolpeadas >= totalDianas)
        {
            AbrirPuerta();
        }
    }

    void AbrirPuerta()
    {
        Debug.Log("Todas las dianas golpeadas, se abre la puerta");

        if (puertaARomper != null)
        {
            // reusamos el mismo sistema de la puerta: le hacemos daño igual a toda su vida
            puertaARomper.RecibirDaño(puertaARomper.vidaMaxima);
        }
    }
}