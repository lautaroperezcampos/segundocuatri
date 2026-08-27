using UnityEngine;
using System.Collections;

public class PlataformaMovil : MonoBehaviour
{
    [Header("Movimiento")]
    public Transform puntoFinal; // arrastra un GameObject vacio en la posicion de destino (mas abajo)
    public float velocidadMovimiento = 2f;

    private Rigidbody2D rb;
    private bool activada = false; // por ahora se mueve una sola vez; podes sacar este chequeo si la queres reusable

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // llamalo desde donde quieras disparar el movimiento (por ejemplo, desde una Diana)
    public void Bajar()
    {
        if (activada) return;
        activada = true;

        StopAllCoroutines();
        StartCoroutine(MoverHacia(puntoFinal.position));
    }

    IEnumerator MoverHacia(Vector3 destino)
    {
        while (Vector3.Distance(transform.position, destino) > 0.05f)
        {
            Vector3 nuevaPos = Vector3.MoveTowards(transform.position, destino, velocidadMovimiento * Time.deltaTime);

            if (rb != null)
            {
                rb.MovePosition(nuevaPos);
            }
            else
            {
                transform.position = nuevaPos;
            }

            yield return new WaitForFixedUpdate();
        }
    }
}