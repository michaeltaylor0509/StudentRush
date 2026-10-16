using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// El "cerebro" del juego: sabe si estamos jugando, en pausa o hemos perdido.
public class GameManager : MonoBehaviour
{
    // Esto permite que otros scripts escriban GameManager.Instance para hablar con él
    public static GameManager Instance;

    public float speed = 10f;        // Velocidad del mundo (metros por segundo)
    public PlayerController player;  // El jugador, para hacerle caer al perder

    public bool isPaused = false;    // ¿Está en pausa? booleano para verlo empieza en no (false)
    public bool isGameOver = false;  // ¿Hemos perdido? aqui lo mismo
    public float distance = 0f;      // Metros recorridos esto servirá para la puntuaciónm

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f; // El tiempo corre normal a velocidad x1
    }

    // ¿Se está jugando? booleano de si está jugando, si no estamos en pausa ni hemos perdido
    public bool IsPlaying()
    {
        return !isPaused && !isGameOver;
    }

    // un get de obtener la velocidad y si no se esta jugando no tiene velocidad
    public float GetSpeed()
    {
        if (IsPlaying())
            return speed;
        return 0f;
    }

    private void Update()
    {
        // Sumamos los metros recorridos
        distance += GetSpeed() * Time.deltaTime;

        Keyboard kb = Keyboard.current;

        // Esc para pausar o continuar && solo si no hemos perdido)
        if (kb.escapeKey.wasPressedThisFrame && !isGameOver)
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }

        // R para reiniciar && en pausa o al perder
        if (kb.rKey.wasPressedThisFrame && !IsPlaying())
        {
            Restart();
        }
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f; // Congela el tiempo
        // aqui irá en el futuro lo de los menús
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f; // Descongela el tiempo
        // aqui irá en un futuro lo de los menús
    }

    public void GameOver()
    {
        if (isGameOver) return; // Si ya hemos perdido no hacemos nada

        isGameOver = true;
        player.Die();
    }

    // Vuelve a cargar la escena desde cero
    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // reiniciar
    }
}