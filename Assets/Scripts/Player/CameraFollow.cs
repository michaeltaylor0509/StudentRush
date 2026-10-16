using UnityEngine;

// Cámara en tercera persona estilo Subway Surfers:
// sigue al jugador casi al instante en horizontal y un poco en vertical.
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private PlayerController player;                   // El Player
    [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -8f); // Posición base respecto al jugador

    [Header("Seguimiento")]
    [Range(0f, 1f)]
    [SerializeField] private float horizontalFollow = 0.8f; // 0 = fija, 1 = pegada al jugador
    [Range(0f, 1f)]
    [SerializeField] private float verticalFollow = 0.3f;   // Cuánto sube al saltar
    [SerializeField] private float slideDrop = 1f;          // Cuántos metros baja al deslizar

    [Header("Suavidad")]
    [SerializeField] private float horizontalSmooth = 15f;  // Más alto = más instantánea
    [SerializeField] private float verticalSmooth = 8f;

    private void LateUpdate()
    {
        if (player == null) return;

        Vector3 playerPos = player.transform.position;

        // Horizontal: casi igual que el jugador
        float targetX = offset.x + playerPos.x * horizontalFollow;

        // Vertical: altura base + una parte del salto o de la rampa
        float heightAboveGround = playerPos.y - player.GroundY;
        float targetY = offset.y + player.GroundY + heightAboveGround * verticalFollow;
        if (player.IsSliding) targetY -= slideDrop;

        // Profundidad fija
        float targetZ = playerPos.z + offset.z;

        Vector3 pos = transform.position;
        pos.x = Mathf.Lerp(pos.x, targetX, horizontalSmooth * Time.deltaTime);
        pos.y = Mathf.Lerp(pos.y, targetY, verticalSmooth * Time.deltaTime);
        pos.z = targetZ;
        transform.position = pos;
    }
}