using UnityEngine;
using UnityEngine.InputSystem; // Sistema de input de Unity 6

// Controla al jugador: carriles (X), salto y deslizamiento (Y).
// El jugador NUNCA avanza en Z: es el mundo el que viene hacia él.
// Sigue la altura del suelo (rampas y techos de tren) y muere si choca de lado contra un tren.
public class PlayerController : MonoBehaviour
{
    private const int LaneCount = 4; // Número de carriles

    [Header("Carriles")]
    [SerializeField] private float laneDistance = 2f;     // Metros entre carriles
    [SerializeField] private float laneChangeSpeed = 12f; // Rapidez al cambiar de carril
    [SerializeField] private int startLane = 1;           // Carril inicial (0 a 3, de izquierda a derecha)
    [SerializeField] private bool canChangeLaneInAir = false;        // ¿Se puede cambiar de carril saltando?
    [SerializeField] private bool canChangeLaneWhileSliding = false; // ¿Se puede cambiar de carril deslizando?
    private int currentLane;                              // Carril actual

    [Header("Salto")]
    [SerializeField] private float jumpForce = 9f;        // Impulso hacia arriba al saltar
    [SerializeField] private float gravity = -25f;        // Gravedad al subir
    [SerializeField] private float fallMultiplier = 1.8f; // Gravedad extra al caer
    private float verticalVelocity;
    private bool isGrounded = true;
    private float groundY;                                // Altura del suelo de la calle

    [Header("Deslizar")]
    [SerializeField] private float slideDuration = 0.8f;
    [SerializeField] private float slideBufferTime = 0.15f;
    private bool isSliding;
    private float slideTimer;
    private float slideBufferTimer;

    [Header("Rampas y trenes")]
    [SerializeField] private LayerMask walkableMask;          // Capa "Walkable": rampas y techos
    [SerializeField] private float stepHeight = 0.6f;         // Desnivel máximo que AJ sube o baja pegado al suelo
    [SerializeField] private bool sideCollisionKills = true;  // Marcada: chocar de lado contra un tren = game over

    [Header("Referencias")]
    [SerializeField] private Animator animator;
    [SerializeField] private CapsuleCollider capsule;
    private float originalHeight;
    private Vector3 originalCenter;

    private bool isDead;

    public bool IsSliding => isSliding;
    public bool IsGrounded => isGrounded;
    public float GroundY => groundY;

    // X de un carril. Con 4 carriles quedan en -1.5, -0.5, +0.5, +1.5 veces laneDistance
    private float LaneX(int lane)
    {
        return (lane - (LaneCount - 1) / 2f) * laneDistance;
    }

    private void Start()
    {
        groundY = transform.position.y;
        originalHeight = capsule.height;
        originalCenter = capsule.center;
        animator.SetBool("IsGrounded", true);

        // Empezamos ya colocados en el carril inicial
        currentLane = Mathf.Clamp(startLane, 0, LaneCount - 1);
        Vector3 pos = transform.position;
        pos.x = LaneX(currentLane);
        transform.position = pos;
    }

    private void Update()
    {
        if (isDead) return;

        if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing) return;

        // Las calles se mueven por transform: avisamos a la física para que sus colliders estén al día
        Physics.SyncTransforms();

        ReadInput();
        UpdateSlide();
        Move();
    }

    private void ReadInput()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame)
            ChangeLane(-1);

        if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame)
            ChangeLane(1);

        if (kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)
            Jump();

        if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)
            Slide();

        // SOLO PARA PROBAR: K provoca un game over
        if (kb.kKey.wasPressedThisFrame && GameManager.Instance != null)
            GameManager.Instance.GameOver();
    }

    // ¿A qué altura está el suelo en esa X? Rayo desde arriba que busca algo "Walkable".
    private float GetFloorY(float x)
    {
        Vector3 origin = new Vector3(x, groundY + 20f, transform.position.z);

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 40f, walkableMask, QueryTriggerInteraction.Ignore))
            return Mathf.Max(hit.point.y, groundY);

        return groundY;
    }

    // Cambio de carril. Si el carril de destino tiene un muro (lateral de un tren), mueres.
    private void ChangeLane(int direction)
    {
        if (!isGrounded && !canChangeLaneInAir) return;
        if (isSliding && !canChangeLaneWhileSliding) return;

        int newLane = Mathf.Clamp(currentLane + direction, 0, LaneCount - 1);
        if (newLane == currentLane) return; // Ya estás en el borde

        // ¿Hay algo mucho más alto en el carril de destino?
        if (GetFloorY(LaneX(newLane)) - transform.position.y > stepHeight)
        {
            if (sideCollisionKills && GameManager.Instance != null)
            {
                Debug.Log("Choque lateral: game over");
                GameManager.Instance.GameOver();
            }
            return; // Si no mata, simplemente no entra
        }

        currentLane = newLane;
    }

    private void Jump()
    {
        if (!isGrounded) return;

        slideBufferTimer = 0f;
        if (isSliding) StopSlide();

        verticalVelocity = jumpForce;
        isGrounded = false;

        animator.SetBool("IsGrounded", false);
        animator.SetTrigger("Jump");
    }

    private void Slide()
    {
        if (!isGrounded)
        {
            slideBufferTimer = slideBufferTime;
            return;
        }

        if (isSliding) return;

        isSliding = true;
        slideTimer = slideDuration;
        animator.SetBool("IsSliding", true);

        capsule.height = originalHeight / 2f;
        capsule.center = new Vector3(originalCenter.x, originalCenter.y / 2f, originalCenter.z);
    }

    private void UpdateSlide()
    {
        if (slideBufferTimer > 0f)
            slideBufferTimer -= Time.deltaTime;

        if (!isSliding) return;

        slideTimer -= Time.deltaTime;
        if (slideTimer <= 0f) StopSlide();
    }

    private void StopSlide()
    {
        isSliding = false;
        animator.SetBool("IsSliding", false);

        capsule.height = originalHeight;
        capsule.center = originalCenter;
    }

    // Movimiento en X (carriles) e Y (salto / altura del suelo). Z no se toca nunca.
    private void Move()
    {
        Vector3 pos = transform.position;

        // --- Eje X ---
        float targetX = LaneX(currentLane);
        pos.x = Mathf.Lerp(pos.x, targetX, laneChangeSpeed * Time.deltaTime);
        if (Mathf.Abs(pos.x - targetX) < 0.001f) pos.x = targetX;

        // --- Eje Y ---
        float floorY = GetFloorY(pos.x);

        if (isGrounded)
        {
            float diff = floorY - pos.y;

            if (Mathf.Abs(diff) <= stepHeight)
            {
                pos.y = floorY; // Nos pegamos al suelo (así subimos y bajamos por rampas)
            }
            else if (diff < 0f)
            {
                // El suelo ha desaparecido (se acabó el techo del tren): empezamos a caer
                isGrounded = false;
                verticalVelocity = 0f;
                animator.SetBool("IsGrounded", false);
            }
        }

        if (!isGrounded)
        {
            float currentGravity = verticalVelocity < 0f ? gravity * fallMultiplier : gravity;
            verticalVelocity += currentGravity * Time.deltaTime;
            pos.y += verticalVelocity * Time.deltaTime;

            if (verticalVelocity <= 0f && pos.y <= floorY)
            {
                pos.y = floorY;
                verticalVelocity = 0f;
                isGrounded = true;
                animator.SetBool("IsGrounded", true);
                animator.ResetTrigger("Jump");

                if (slideBufferTimer > 0f)
                {
                    slideBufferTimer = 0f;
                    Slide();
                }
            }
        }

        transform.position = pos;
    }

    public void Die()
    {
        if (isDead) return;

        isDead = true;
        animator.SetTrigger("Die");
    }
}