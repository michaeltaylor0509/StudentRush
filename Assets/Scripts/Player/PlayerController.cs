using UnityEngine;
using UnityEngine.InputSystem; // para leer el teclado

// Controla al jugador: carriles, salto y deslizar.
// El jugador no avanza en Z, el mundo viene hacia él.
public class PlayerController : MonoBehaviour
{
    public float laneDistance = 3.5f;    // metros entre carriles
    public float laneChangeSpeed = 5f;   // lo rápido que cambia de carril
    public int startLane = 1;            // carril inicial (0 a 3)
    int currentLane;                     // carril actual

    public float jumpForce = 9f;         // fuerza del salto
    public float gravity = -25f;         // negativa porque tira hacia abajo
    public float fallMultiplier = 1.8f;  // al caer pesa más, así no flota
    float verticalVelocity;              // positivo = subiendo, negativo = cayendo
    bool isGrounded = true;              // true = en el suelo
    public float groundY;                // altura del suelo (public porque la cámara la lee)

    public float slideDuration = 1f;      // lo que dura el deslizamiento
    public float slideBufferTime = 0.15f; // si pulsas S justo antes de aterrizar, se acuerda
    public bool isSliding;                // public porque la cámara la lee
    float slideTimer;                     // cuenta atrás del deslizamiento
    float slideBufferTimer;               // cuenta atrás de la pulsación recordada

    public LayerMask walkableMask;       // capa Walkable (rampas y techos de tren)
    public float stepHeight = 0.6f;      // desnivel máximo que sube pegado al suelo

    public Animator animator;            // el Animator de AJ
    public CapsuleCollider capsule;      // el collider del Player
    float originalHeight;                // tamaño normal del collider
    Vector3 originalCenter;

    bool isDead;

    void Start()
    {
        groundY = transform.position.y;      // la altura inicial es la del suelo
        originalHeight = capsule.height;     // guardo el collider para devolverlo luego
        originalCenter = capsule.center;
        animator.SetBool("IsGrounded", true);

        currentLane = startLane;
        transform.position = new Vector3(LaneX(currentLane), groundY, transform.position.z); // me coloco en mi carril
    }

    // devuelve la X de un carril. Con laneDistance 3.5: -5.25, -1.75, 1.75, 5.25
    float LaneX(int lane)
    {
        return (lane - 1.5f) * laneDistance;
    }

    void Update()
    {
        if (isDead || !GameManager.Instance.IsPlaying()) return; // muerto o en pausa: nada

        Physics.SyncTransforms(); // las calles se mueven, esto actualiza sus colliders para el rayo

        ReadInput();
        UpdateSlide();
        Move();
    }

    void ReadInput()
    {
        Keyboard kb = Keyboard.current;

        // wasPressedThisFrame = se ha pulsado en este frame (una vez, no mientras la mantienes)
        if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) ChangeLane(-1);
        if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) ChangeLane(1);
        if (kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) Jump();
        if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) Slide();
    }

    // direction: -1 izquierda, 1 derecha
    void ChangeLane(int direction)
    {
        if (!isGrounded || isSliding) return; // ni saltando ni deslizando

        int newLane = Mathf.Clamp(currentLane + direction, 0, 3); // Clamp: que no pase de 0 ni de 3
        if (newLane == currentLane) return;                       // estaba en el borde

        // si el suelo del carril nuevo está mucho más alto, es la pared de un tren: chocas
        if (GetFloorY(LaneX(newLane)) - transform.position.y > stepHeight)
        {
            GameManager.Instance.GameOver();
            return;
        }

        currentLane = newLane; // el movimiento suave lo hace Move()
    }

    void Jump()
    {
        if (!isGrounded) return; // no hay doble salto

        slideBufferTimer = 0f;      // olvido cualquier deslizamiento pendiente
        if (isSliding) StopSlide(); // si deslizaba, me levanto

        verticalVelocity = jumpForce;
        isGrounded = false;
        animator.SetBool("IsGrounded", false);
        animator.SetTrigger("Jump"); // trigger = se dispara una vez
    }

    void Slide()
    {
        if (!isGrounded)
        {
            slideBufferTimer = slideBufferTime; // en el aire no desliza, pero me acuerdo un momento
            return;
        }
        if (isSliding) return; // ya estaba deslizando

        isSliding = true;
        slideTimer = slideDuration;
        animator.SetBool("IsSliding", true);

        // collider a la mitad para pasar bajo las barras (y el centro también para que siga en el suelo)
        capsule.height = originalHeight / 2f;
        capsule.center = new Vector3(originalCenter.x, originalCenter.y / 2f, originalCenter.z);
    }

    void StopSlide()
    {
        isSliding = false;
        animator.SetBool("IsSliding", false);
        capsule.height = originalHeight; // devuelvo el collider a su tamaño
        capsule.center = originalCenter;
    }

    void UpdateSlide()
    {
        // Time.deltaTime = segundos que dura un frame; restarlo cada frame es una cuenta atrás
        slideBufferTimer -= Time.deltaTime;

        if (isSliding)
        {
            slideTimer -= Time.deltaTime;
            if (slideTimer <= 0f) StopSlide(); // acabó el tiempo: me levanto
        }
    }

    // altura del suelo en una X. Lanzo un rayo hacia abajo desde arriba:
    // si toca una rampa o un techo de tren (Walkable), esa es la altura; si no, el suelo normal
    float GetFloorY(float x)
    {
        Vector3 origin = new Vector3(x, groundY + 20f, transform.position.z); // 20 m por encima
        RaycastHit hit; // aquí se guarda lo que toque el rayo

        // out hit = rellena la variable hit con lo que toque
        if (Physics.Raycast(origin, Vector3.down, out hit, 40f, walkableMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.point.y > groundY) return hit.point.y;
        }
        return groundY;
    }

    void Move()
    {
        Vector3 pos = transform.position; // copio la posición, la cambio y al final la devuelvo

        // X: voy hacia el carril. Lerp = avanzar un trozo del camino cada frame (movimiento suave)
        float targetX = LaneX(currentLane);
        pos.x = Mathf.Lerp(pos.x, targetX, laneChangeSpeed * Time.deltaTime);
        if (Mathf.Abs(pos.x - targetX) < 0.001f) pos.x = targetX; // Lerp nunca llega exacto, lo coloco yo

        // Y: suelo o aire
        float floorY = GetFloorY(pos.x);

        if (isGrounded)
        {
            float diff = floorY - pos.y; // diferencia entre el suelo y yo

            if (Mathf.Abs(diff) <= stepHeight) // Abs = sin signo. Si el suelo está cerca, me pego a él
            {
                pos.y = floorY; // así subo y bajo las rampas
            }
            else if (diff < 0f)
            {
                // el suelo está mucho más abajo: se acabó el techo del tren, caigo
                isGrounded = false;
                verticalVelocity = 0f;
                animator.SetBool("IsGrounded", false);
            }
        }

        if (!isGrounded)
        {
            // gravedad: si caigo, pesa más
            if (verticalVelocity < 0f)
                verticalVelocity += gravity * fallMultiplier * Time.deltaTime;
            else
                verticalVelocity += gravity * Time.deltaTime;

            pos.y += verticalVelocity * Time.deltaTime; // espacio = velocidad x tiempo

            // aterrizaje: voy cayendo y llego al suelo
            if (verticalVelocity <= 0f && pos.y <= floorY)
            {
                pos.y = floorY;
                verticalVelocity = 0f;
                isGrounded = true;
                animator.SetBool("IsGrounded", true);
                animator.ResetTrigger("Jump"); // borro saltos pendientes

                // si pulsé S justo antes de aterrizar, deslizo ya
                if (slideBufferTimer > 0f)
                {
                    slideBufferTimer = 0f;
                    Slide();
                }
            }
        }

        transform.position = pos;
    }

    // la llama el GameManager al perder
    public void Die()
    {
        isDead = true;
        animator.SetTrigger("Die");
    }
}