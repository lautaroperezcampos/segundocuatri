using UnityEngine;

public class Diana : MonoBehaviour
{
    private bool golpeada = false;
    private ControladorDianas controlador;

    void Start()
    {
        controlador = Object.FindFirstObjectByType<ControladorDianas>();
    }

    public void RecibirImpacto()
    {
        if (golpeada) return;
        golpeada = true;

        Debug.Log("Diana golpeada: " + name);

        // feedback visual simple: la ponemos gris para que se note que ya esta golpeada
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = Color.gray;
        }

        if (controlador != null)
        {
            controlador.NotificarDianaGolpeada();
        }
    }
}