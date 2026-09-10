using UnityEngine;
public class Diana : MonoBehaviour
{
    [Header("Plataforma que activa (opcional)")]
    public PlataformaMovil plataformaAMover; // arrastra aca la plataforma que tiene que bajar

    [Header("Sala de 3 dianas (opcional)")]
    public ControladorSalaSubjefe controladorSala; // arrastra aca el controlador, si esta diana es parte de esa secuencia
    public int numeroDiana; // 1, 2 o 3 - le dice al controlador cual de las tres es esta

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

        if (plataformaAMover != null)
        {
            plataformaAMover.Bajar();
        }

        if (controladorSala != null)
        {
            Debug.Log("Diana " + numeroDiana + " avisando al ControladorSala");
            controladorSala.NotificarDianaGolpeada(numeroDiana);
        }
        else
        {
            Debug.LogWarning("Diana " + name + " no tiene ControladorSala asignado");
        }
    }
}