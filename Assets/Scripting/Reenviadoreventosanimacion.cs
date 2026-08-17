using UnityEngine;

// Este script va en el mismo objeto que el Animator (el hijo "Sprite").
// Su unico trabajo es recibir los Animation Events y reenviarlos al padre,
// que es donde vive la logica real (SubjefeDisparador).
public class ReenviadorEventosAnimacion : MonoBehaviour
{
    private SubjefeDisparador subjefeDisparador;

    void Start()
    {
        subjefeDisparador = GetComponentInParent<SubjefeDisparador>();
    }

    // llamado por el Animation Event en el clip Attack
    public void SpawnearProyectilAhora()
    {
        if (subjefeDisparador != null)
        {
            subjefeDisparador.SpawnearProyectilAhora();
        }
    }
}