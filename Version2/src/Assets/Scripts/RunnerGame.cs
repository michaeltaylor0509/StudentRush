using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using System.Collections.Generic;

public class RunnerGame : MonoBehaviour
{
    // Prefabs y UI
    public GameObject obstaclePrefab;
    public Text statusText;
    public Text controlsText;

    // Runner y carriles
    public float speed = 20f;
    public float runnerLength = 300f;
    public float laneWidth = 2.5f;
    public float laneSwitchSpeed = 12f;

    // Salto
    public float jumpForce = 8f;
    public float highJumpForce = 24f;
    public float holdTime = 0.22f;

    private bool jumpHeld;
    private bool jumpRequest;
    private bool isJumping;

    // Obstáculos (min, max)
    public float[] obstacleDistance = { 16f, 22f };
    public float[] obstacleHeight = { 1f, 2.2f };

    private List<Transform> obstacles = new List<Transform>();
    private Rigidbody rb;

    // Estado del juego
    private float runTime;
    private float cameraOffsetZ;
    private float nextObstacleZ = 25f;

    private int currentLane = 1; // 0 = izquierda, 1 = centro, 2 = derecha
    private float jumpTimer;
    private float jumpBufferTimer;

    private float laneTimer = 0f;
    public float laneDebounce = 0.2f;

    private bool gameEnded = false;
    private bool isOnFloor = true;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        transform.position = new Vector3(0f, 0.5f, 0f);
        cameraOffsetZ = Camera.main.transform.position.z - transform.position.z;

        controlsText.text =
            "Cambiar de carril = [A/D, flechas, joystick]\n"
            + "Salto = [Espacio, W, flecha arriba] Mantener pulsado = salto largo\n"
            + "Reiniciar = R";
    }

    void Update()
    {
        if (gameEnded)
        {
            if (Input.GetKeyDown(KeyCode.R) || Input.GetButtonDown("Jump"))
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);

            return;
        }

        MovePlayer();
        JumpHandler();

        SpawnObstacles();
        CleanObstacles();

        FollowCamera();
        runTime += Time.deltaTime;

        if (transform.position.z >= runnerLength)
        {
            EndGame("Pulsa R o salta para repetir.");
            return;
        }

        statusText.text = "Tiempo: " + runTime.ToString("F1") + " s";
    }

    void FixedUpdate()
    {
        if (gameEnded)
            return;

        if (jumpRequest)
        {
            jumpBufferTimer = 0.2f;
            jumpRequest = false;
        }
        else if (jumpBufferTimer > 0f)
            jumpBufferTimer -= Time.fixedDeltaTime;

        if (isOnFloor && jumpBufferTimer > 0f)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);

            isOnFloor = false;
            isJumping = true;

            jumpTimer = 0f;
            jumpBufferTimer = 0f;
        }

       
        if (isJumping)
        {
            if (jumpHeld && rb.linearVelocity.y > 0f && jumpTimer < holdTime)
            {
                rb.AddForce(Vector3.up * highJumpForce, ForceMode.Acceleration);
                jumpTimer += Time.fixedDeltaTime;
            }
            else
                isJumping = false;
        }
    }

    void MovePlayer()
    {
        float h = Input.GetAxisRaw("Horizontal");

        laneTimer -= Time.deltaTime;

        if (Mathf.Abs(h) < 0.5f)
            laneTimer = 0f;

        if (laneTimer <= 0f)
        {
            if (h < -0.5f)
            {
                currentLane = Mathf.Max(0, currentLane - 1);
                laneTimer = laneDebounce;
            }
            else if (h > 0.5f)
            {
                currentLane = Mathf.Min(2, currentLane + 1);
                laneTimer = laneDebounce;
            }
        }

        float targetX = (currentLane - 1) * laneWidth;

        Vector3 pos = transform.position;
        pos.x = Mathf.MoveTowards(pos.x, targetX, laneSwitchSpeed * Time.deltaTime);
        pos.z += speed * Time.deltaTime;
        transform.position = pos;
    }

    void JumpHandler()
    {
        bool jumpPressed =
            Input.GetButtonDown("Jump") ||
            Input.GetKeyDown(KeyCode.W) ||
            Input.GetKeyDown(KeyCode.UpArrow);

        jumpHeld =
            Input.GetButton("Jump") ||
            Input.GetKey(KeyCode.W) ||
            Input.GetKey(KeyCode.UpArrow);

        if (jumpPressed)
            jumpRequest = true;
    }

    void OnCollisionStay(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f && rb.linearVelocity.y <= 0.1f)
            {
                isOnFloor = true;
                isJumping = false;
                return;
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        isOnFloor = false;
    }

    void SpawnObstacles()
    {
        while (nextObstacleZ < transform.position.z + 80f && nextObstacleZ < runnerLength - 10f)
            SpawnPattern();
    }

    void SpawnPattern()
    {
        int type = Random.Range(0, 3);

        if (type <= 1) // Coches y furgonetas
        {
            int freeLane = Random.Range(0, 3);

            for (int lane = 0; lane < 3; lane++)
            {
                if (lane == freeLane)
                    continue;

                float x = (lane - 1) * laneWidth;
                float z = nextObstacleZ; // + Random.Range(-3.5f, 3.5f);

                if (Random.Range(0, 2) == 0)
                    CreateObstacle(new Vector3(x, 0f, z), new Vector3(1.8f, obstacleHeight[0], 6f), Color.green);
                else
                    CreateObstacle(new Vector3(x, 0f, z), new Vector3(2f, obstacleHeight[1], 8f), Color.red);
            }
        }
        else // Muro ancho
        {
            if (Random.Range(0, 2) == 0)
                CreateObstacle(new Vector3(0f, 0f, nextObstacleZ), new Vector3(7.5f, obstacleHeight[0], 0.8f), Color.green);
            else
                CreateObstacle(new Vector3(0f, 0f, nextObstacleZ), new Vector3(7.5f, obstacleHeight[1], 0.8f), Color.red);
        }

        nextObstacleZ += Random.Range(obstacleDistance[0], obstacleDistance[1]);
    }

    void CreateObstacle(Vector3 pos, Vector3 scale, Color color)
    {
        GameObject obj = Instantiate(obstaclePrefab);

        obj.transform.localScale = scale;
        obj.transform.position = new Vector3(pos.x, pos.y + scale.y / 2f, pos.z);
        obj.GetComponent<Renderer>().material.color = color;

        obstacles.Add(obj.transform);
    }

    void CleanObstacles()
    {
        for (int i = obstacles.Count - 1; i >= 0; i--)
        {
            if (obstacles[i].position.z < transform.position.z - 10f)
            {
                Destroy(obstacles[i].gameObject);
                obstacles.RemoveAt(i);
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (gameEnded)
            return;

        if (obstacles.Contains(collision.transform))
        {
            if (GetComponent<Collider>().bounds.min.y >= collision.collider.bounds.max.y - 0.2f)// - 0.05f)
            {
                isOnFloor = true;
                isJumping = false;
                return;
            }

            EndGame("Pulsa R o salta para reiniciar.");
        }
    }

    void FollowCamera()
    {
        Vector3 camPos = Camera.main.transform.position;
        camPos.z = transform.position.z + cameraOffsetZ;
        Camera.main.transform.position = camPos;
    }

    void EndGame(string msg)
    {
        gameEnded = true;
        statusText.text = msg;
    }
}