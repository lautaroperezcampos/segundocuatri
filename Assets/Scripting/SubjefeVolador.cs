using UnityEngine;

// Por ahora, este Titan se comporta EXACTAMENTE igual que un Subjefe normal
// cuando esta libre (camina, persigue, pega). La unica diferencia es que,
// cuando el jugador lo posee, en vez de un salto normal (que necesita estar
// en el suelo), puede aletear en el aire las veces que quiera con Espacio.
public class SubjefeVolador : Subjefe
{
    [Header("Vuelo (solo cuando lo posee el jugador)")]
    public float fuerzaAleteo = 6f;

    public override void Saltar()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaAleteo);

        if (animator != null)
        {
            animator.SetTrigger("Saltando");
        }
    }
}