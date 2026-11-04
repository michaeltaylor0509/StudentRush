using TMPro;           // para los textos TextMeshPro
using UnityEngine;

// Pinta en pantalla los datos del GameManager y enseña los paneles.
public class UIManager : MonoBehaviour
{
    public GameObject[] hudElements;  // los textos del HUD (se ocultan en el menú)

    public TMP_Text timeText;        // tiempo jugado
    public TMP_Text scoreText;       // puntos
    public TMP_Text distanceText;    // metros recorridos
    public TMP_Text coinsText;       // monedas

    public GameObject startPanel;     // menú de inicio con los controles
    public GameObject pausePanel;     // menú de pausa
    public GameObject gameOverPanel;  // pantalla de derrota
    public TMP_Text finalScoreText;   // puntos finales dentro del game over

    GameManager gm; // atajo para no escribir GameManager.Instance siempre

    void Start()
    {
        gm = GameManager.Instance;
    }

    void Update()
    {
        // el HUD solo se ve cuando ya hemos empezado; el panel de inicio, al revés
        foreach (GameObject element in hudElements)
            element.SetActive(gm.isStarted);
        startPanel.SetActive(!gm.isStarted);

        // tiempo en formato minutos:segundos
        int minutes = (int)(gm.playTime / 60f);
        int seconds = (int)(gm.playTime % 60f); // % = resto de la división
        timeText.text = minutes + ":" + seconds.ToString("00"); // "00" = siempre dos cifras

        scoreText.text = "Puntos: " + gm.GetScore().ToString("N0"); // "N0" = con miles y sin decimales
        distanceText.text = (int)gm.distance + " m";
        coinsText.text = "Monedas: " + gm.coins;

        // paneles: solo se ve el que toca
        pausePanel.SetActive(gm.isPaused);
        gameOverPanel.SetActive(gm.isGameOver);

        if (gm.isGameOver) finalScoreText.text = "Puntos: " + gm.GetScore().ToString("N0");
    }

    // las llaman los botones desde el Inspector (evento On Click)
    public void OnPlay()    { gm.StartGame(); }
    public void OnResume()  { gm.Resume(); }
    public void OnRestart() { gm.Restart(); }
}