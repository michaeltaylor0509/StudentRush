using UnityEngine;

// Moneda: gira, y al tocarla el jugador suma y desaparece.
// No se desactiva el objeto entero porque las calles se reutilizan:
// solo la escondo, y OnEnable la devuelve cuando la calle vuelve a salir.
public class Coin : MonoBehaviour
{
    public float spinSpeed = 180f;   // grados por segundo que gira

    Renderer rend;                   // lo que se ve
    Collider col;                    // lo que detecta al jugador

    void Awake()
    {
        rend = GetComponent<Renderer>();
        col = GetComponent<Collider>();
    }

    // se ejecuta cada vez que la calle se enciende: la moneda vuelve a estar disponible
    void OnEnable()
    {
        rend.enabled = true;
        col.enabled = true;
    }

    void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World); // gira sobre Y
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        GameManager.Instance.AddCoin();
        rend.enabled = false;   // la escondo
        col.enabled = false;    // y que no cuente otra vez
    }
}