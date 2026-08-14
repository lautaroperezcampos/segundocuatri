using UnityEngine;

// Poné este script en un GameObject con un Collider2D marcado como "Is Trigger"
// (NO solido), que cubra el area del dibujo de la escalera diagonal.
// La direccion de "subir" es el "derecha" local de este objeto (el dibujo esta
// estirado en el eje X) - rotalo en el Inspector para que coincida con el dibujo.
public class EscaleraDiagonal : MonoBehaviour
{
    [Header("Plataforma que atraviesa")]
    // arrastra aca el Collider2D de la plataforma solida que esta escalera atraviesa
    // (la que tiene el hueco). Se ignora la colision con ella SOLO mientras se escala.
    public Collider2D plataformaQueAtraviesa;
}