using UnityEngine;

// Cámara estilo Subway Surfers sigue al jugador casi al instante.
public class CameraFollow : MonoBehaviour
{
    public PlayerController player;                   // refencia al jugador al que sigue
    public Vector3 offset = new Vector3(0f, 5f, -8f); // Dónde está la cámara respecto al jugador

    public float horizontalFollow = 0.8f; // 0 = fija, 1 = pegada al jugador al moverse de lado
    public float verticalFollow = 0.8f;   // Cuánto sube la cámara cuando el jugador salta
    public float slideDrop = 1.5f;          // Cuánto baja la cámara al deslizar
    public float smooth = 12f;            // Rapidez de la cámara (más alto = más instantánea)

    // LateUpdate va después de Update: el jugador ya se ha movido y la cámara no tiembla
    private void LateUpdate()
    {
        Vector3 p = player.transform.position; // Dónde está el jugador

        // A dónde queremos que vaya la cámara
        // offset.x posición base camara (0, centrada) y p.x * horizontalFollow cuanto se ha movido
        float x = offset.x + p.x * horizontalFollow;
        // offset.y es la altura base de la cámara sobre el suelo, player.groundY es la altura del suelo sobre
        // la calle sumando para que la cámara se mida sobre el suelo y luego (p.y - player.groundY) * verticalFollow
        // para ver cuanto ha subido AJ sobre el suelo multiplicando por verticalFollow
        // y lo de p.y - player.groundY hace que funcione en rampas tmb
        float y = offset.y + player.groundY + (p.y - player.groundY) * verticalFollow;
        // como no avanza en z esto nunca cambia
        float z = p.z + offset.z;

        // Si desliza la cámara baja un poco
        if (player.isSliding)
            y -= slideDrop;

        // La cámara se acerca suavemente a ese punto
        Vector3 target = new Vector3(x, y, z);
        transform.position = Vector3.Lerp(transform.position, target, smooth * Time.deltaTime);
    }
}