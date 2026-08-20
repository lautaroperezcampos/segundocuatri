using UnityEngine;

public class Trampolin : MonoBehaviour
{
    [Header("Impulso")]
    public float fuerzaImpulso = 25f;

    void OnCollisionEnter2D(Collision2D colision)
    {
        // solo impulsa si te aterrizo ENCIMA (no si choco de costado o por abajo)
        foreach (ContactPoint2D contacto in colision.contacts)
        {
            if (contacto.normal.y < -0.5f)
            {
                Rigidbody2D rb = colision.rigidbody;
                if (rb != null)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaImpulso);
                }
                break;
            }
        }
    }
}