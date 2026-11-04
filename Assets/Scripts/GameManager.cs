using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// El "cerebro" del juego: estado, velocidad, puntos y monedas.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;     // para llamarlo desde cualquier script

    public float speed = 10f;               // velocidad del mundo (m/s)
    public PlayerController player;         // para hacerle caer al perder

    public bool isStarted = false;          // false = estamos en el menú de inicio
    public bool isPaused = false;
    public bool isGameOver = false;
    public float distance = 0f;             // metros recorridos
    public float playTime = 0f;             // segundos jugados
    public int coins = 0;                   // monedas recogidas

    public double score = 0;                // puntos acumulados (double: aguanta números enormes)
    public float pointsPerMeter = 10f;      // puntos base por metro
    public float metersPerMultiplier = 1000f; // cada tantos metros el multiplicador sube +1
    public int coinValue = 100;             // puntos base de una moneda

    // static = pertenece a la clase y no se borra al recargar la escena.
    // Así, al reintentar, saltamos el menú y vamos directos a jugar.
    static bool skipMenu = false;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;                 // por si venimos de una pausa
        isStarted = skipMenu;                // tras reintentar: empieza ya; la primera vez: menú
        skipMenu = false;                    // lo dejo listo para la próxima vez
    }

    // se juega si hemos empezado y no hay pausa ni game over
    public bool IsPlaying()
    {
        return isStarted && !isPaused && !isGameOver;
    }

    // velocidad del mundo: la normal si jugamos, 0 si no
    public float GetSpeed()
    {
        if (IsPlaying())
            return speed;
        return 0f;
    }

    // multiplicador: x1 al empezar, +1 cada metersPerMultiplier metros
    public float GetMultiplier()
    {
        return 1f + distance / metersPerMultiplier;
    }

    // puntos para mostrar (long = entero muy grande)
    public long GetScore()
    {
        return (long)score;
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        Gamepad pad = Gamepad.current; // null si no hay mando

        // menú de inicio: Enter o Start empiezan la partida
        if (!isStarted)
        {
            if (kb != null && kb.enterKey.wasPressedThisFrame) StartGame();
            if (pad != null && pad.startButton.wasPressedThisFrame) StartGame();
            return; // en el menú no se hace nada más
        }

        if (IsPlaying())
        {
            float move = GetSpeed() * Time.deltaTime; // metros de este frame
            distance += move;
            playTime += Time.deltaTime;

            // puntos de este frame = metros x puntos por metro x multiplicador
            score += move * pointsPerMeter * GetMultiplier();
        }

        // Esc o Start: pausar o continuar (solo si no has perdido)
        bool pausePressed = false;
        if (kb != null && kb.escapeKey.wasPressedThisFrame) pausePressed = true;
        if (pad != null && pad.startButton.wasPressedThisFrame) pausePressed = true;

        if (pausePressed && !isGameOver)
        {
            if (isPaused) Resume();
            else Pause();
        }

        // R o botón oeste (X / Cuadrado): reiniciar (en pausa o al perder)
        bool restartPressed = false;
        if (kb != null && kb.rKey.wasPressedThisFrame) restartPressed = true;
        if (pad != null && pad.buttonWest.wasPressedThisFrame) restartPressed = true;

        if (restartPressed && !IsPlaying())
            Restart();
    }

    // la llaman el botón Jugar y las teclas del menú
    public void StartGame()
    {
        isStarted = true;
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f; // congela el tiempo
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
    }

    public void GameOver()
    {
        if (isGameOver) return; // para que no se ejecute dos veces

        isGameOver = true;
        player.Die();
    }

    // la llama cada moneda al recogerla
    public void AddCoin()
    {
        coins++;
        score += coinValue * GetMultiplier(); // la moneda también vale más con el multiplicador
    }

    public void Restart()
    {
        skipMenu = true; // que la escena nueva empiece directamente
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}