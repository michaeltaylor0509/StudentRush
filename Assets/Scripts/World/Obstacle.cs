using UnityEngine;

// Obstáculo: si toca al jugador, es game over.
// No se mueve por sí solo: va dentro de un prefab de calle y se mueve con ella.
public class Obstacle : MonoBehaviour
{
    // Reset se ejecuta al añadir el script: deja el collider como trigger automáticamente
    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    // Se llama cuando algo entra en el collider
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            GameManager.Instance.GameOver();
    }
}