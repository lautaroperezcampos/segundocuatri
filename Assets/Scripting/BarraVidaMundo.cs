using UnityEngine;

// Poné este script en el prefab de la barra (fondo + relleno como hijos).
// El "Relleno" tiene que tener su pivot en el borde IZQUIERDO (no en el centro),
// para que al achicarse en X se vea como que se vacia de derecha a izquierda.
public class BarraVidaMundo : MonoBehaviour
{
    [Header("Referencias")]
    public Transform relleno; // el sprite que se achica segun el porcentaje de vida

    public void ActualizarPorcentaje(float porcentaje)
    {
        porcentaje = Mathf.Clamp01(porcentaje);

        if (relleno != null)
        {
            Vector3 escala = relleno.localScale;
            escala.x = porcentaje;
            relleno.localScale = escala;
        }
    }
}