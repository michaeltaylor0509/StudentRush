using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Los estados posibles de la partida
public enum GameState { Ready, Playing, Paused, GameOver }

// El "cerebro" del juego: sabe en qué estado está la partida
// y a qué velocidad se mueve el mundo.
public class GameManager : MonoBehaviour
{
    // Singleton: cualquier script puede usar GameManager.Instance para hablar con él
    public static GameManager Instance { get; private set; }

    [SerializeField] private float speed = 10f;        // Velocidad del mundo en metros por segundo
    [SerializeField] private bool autoStart = true;    // Si está marcada, la partida empieza sola al dar Play (para probar)
    [SerializeField] private PlayerController player;  // El jugador, para hacerle caer al perder

    // Los demás scripts pueden leer estos valores, pero solo el GameManager los cambia
    public GameState State { get; private set; } = GameState.Ready;
    public float Distance { get; private set; }        // Metros recorridos

    // Velocidad del mundo: solo se mueve mientras se juega
    public float Speed => State == GameState.Playing ? speed : 0f;

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f; // Por si venimos de reiniciar estando en pausa
    }

    private void Start()
    {
        // Si Auto Start está marcada, empezamos directamente
        if (autoStart) StartRun();
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        switch (State)
        {
            case GameState.Ready:
                if (kb.enterKey.wasPressedThisFrame) StartRun();       // Enter: empezar
                break;

            case GameState.Playing:
                if (kb.escapeKey.wasPressedThisFrame)
                {
                    Pause();                                           // Esc: pausar
                    break;
                }
                Distance += Speed * Time.deltaTime;                    // Sumamos lo recorrido
                break;

            case GameState.Paused:
                if (kb.escapeKey.wasPressedThisFrame) Resume();        // Esc: continuar
                else if (kb.rKey.wasPressedThisFrame) Restart();       // R: reiniciar
                break;

            case GameState.GameOver:
                if (kb.rKey.wasPressedThisFrame) Restart();            // R: reiniciar
                break;
        }
    }

    private void StartRun()
    {
        State = GameState.Playing;
        Debug.Log("Partida empezada");
    }

    public void Pause()
    {
        State = GameState.Paused;
        Time.timeScale = 0f; // Congela el tiempo del juego
        Debug.Log("Pausa");
    }

    public void Resume()
    {
        State = GameState.Playing;
        Time.timeScale = 1f; // El tiempo vuelve a la normalidad
        Debug.Log("Continuar");
    }

    public void GameOver()
    {
        if (State != GameState.Playing) return; // Solo se puede perder jugando

        State = GameState.GameOver;
        player.Die();
        Debug.Log("Game Over. Pulsa R para reiniciar");
    }

    // Vuelve a cargar la escena desde el principio
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}