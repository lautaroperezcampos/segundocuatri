using UnityEngine;

// Poné este script en un GameObject con Collider2D marcado "Is Trigger".
public class Palanca : MonoBehaviour
{
    public ControladorPalancas controlador; // arrastra aca el controlador de las 4 palancas
    public int numeroPalanca; // 1, 2, 3 o 4 - le dice al controlador cual es esta

    private bool activada = false;

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (activada) return;

        // se activa tanto si la toca el Jugador normal como un Subjefe poseido
        bool esValido = otro.GetComponent<Jugador>() != null || otro.GetComponent<Subjefe>() != null;
        if (!esValido) return;

        activada = true;

        // feedback visual simple: se pone gris, como las Dianas
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = Color.gray;
        }

        if (controlador != null)
        {
            controlador.NotificarPalancaActivada(numeroPalanca);
        }
    }
}