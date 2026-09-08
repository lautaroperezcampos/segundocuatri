using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    // enganchá este metodo al OnClick() del boton "Reiniciar"
    public void Reiniciar()
    {
        Time.timeScale = 1f; // por si algo habia quedado pausado
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}