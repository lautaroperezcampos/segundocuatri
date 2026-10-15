using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Navega entre botones a mano, usando los mismos Input.GetAxisRaw/GetButtonDown
// que ya funcionan en todo el resto del juego - no depende del InputSystem nuevo
// ni de tocar Project Settings. Pone un componente por menu (principal, pausa,
// game over, fin del juego) con SUS botones en orden.
public class NavegacionUIManual : MonoBehaviour
{
    [Header("Botones en el orden que quieras navegarlos")]
    public Button[] botones;

    [Header("Tiempo entre movimientos (evita que salte varios de una)")]
    public float tiempoEntreMovimientos = 0.2f;

    private int indiceActual = 0;
    private float tiempoUltimoMovimiento = -999f;

    // OnEnable: si el panel se activa/desactiva (pausa, game over), cada vez que
    // aparece vuelve a empezar desde el primer boton
    void OnEnable()
    {
        indiceActual = 0;
        tiempoUltimoMovimiento = -999f;
        ResaltarBoton(indiceActual);
    }

    void Update()
    {
        if (botones == null || botones.Length == 0) return;

        float vertical = Input.GetAxisRaw("Vertical");

        // unscaledTime: sigue funcionando aunque el juego este en pausa (timeScale = 0)
        if (Time.unscaledTime >= tiempoUltimoMovimiento + tiempoEntreMovimientos)
        {
            if (vertical > 0.5f)
            {
                MoverSeleccion(-1);
            }
            else if (vertical < -0.5f)
            {
                MoverSeleccion(1);
            }
        }

        // GetButtonDown("Jump") responde al boton A del joystick Y a la tecla Espacio
        if (Input.GetButtonDown("Jump"))
        {
            Button actual = botones[indiceActual];
            if (actual != null && actual.gameObject.activeInHierarchy && actual.interactable)
            {
                actual.onClick.Invoke();
            }
        }
    }

    void MoverSeleccion(int direccion)
    {
        tiempoUltimoMovimiento = Time.unscaledTime;

        // salta botones nulos, apagados o no interactuables
        for (int i = 0; i < botones.Length; i++)
        {
            indiceActual += direccion;
            if (indiceActual < 0) indiceActual = botones.Length - 1;
            if (indiceActual >= botones.Length) indiceActual = 0;

            Button b = botones[indiceActual];
            if (b != null && b.gameObject.activeInHierarchy && b.interactable) break;
        }

        ResaltarBoton(indiceActual);
    }

    void ResaltarBoton(int indice)
    {
        if (botones == null || indice >= botones.Length) return;

        if (EventSystem.current != null && botones[indice] != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(botones[indice].gameObject);
        }
    }
}