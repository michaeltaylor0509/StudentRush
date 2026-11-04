using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// El "cerebro" del juego: estado y velocidad del mundo.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;     // para llamarlo desde cualquier script

    public float speed = 10f;               // velocidad del mundo (m/s)
    public PlayerController player;         // para hacerle caer al perder

    public bool isPaused = false;
    public bool isGameOver = false;
    public float distance = 0f;             // metros recorridos

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;                 // por si venimos de una pausa
    }

    // se juega si no hay pausa ni game over
    public bool IsPlaying()
    {
        return !isPaused && !isGameOver;
    }

    // velocidad del mundo: la normal si jugamos, 0 si no
    public float GetSpeed()
    {
        if (IsPlaying())
            return speed;
        return 0f;
    }

    void Update()
    {
        if (IsPlaying())
            distance += GetSpeed() * Time.deltaTime;

        Keyboard kb = Keyboard.current;

        // Esc: pausar o continuar (solo si no has perdido)
        if (kb.escapeKey.wasPressedThisFrame && !isGameOver)
        {
            if (isPaused) Resume();
            else Pause();
        }

        // R: reiniciar (en pausa o al perder)
        if (kb.rKey.wasPressedThisFrame && !IsPlaying())
            Restart();
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

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}