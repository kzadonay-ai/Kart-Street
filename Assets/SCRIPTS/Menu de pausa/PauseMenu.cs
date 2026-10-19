using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    public GameObject pausePanel;

    [Header("Botones")]
    public Button botonContinuar;
    public Button botonSalir;

    [Header("Escena del menú")]
    public string nombreEscenaMenu = "MainMenu";

    [Header("Envío de datos")]
    public float esperaMaximaEnvio = 10f;

    private bool isPaused = false;
    private bool saliendo = false;

    void Start()
    {
        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);
        else
            Debug.LogWarning("PauseMenu: falta arrastrar PausePanel");
    }

    void Update()
    {
        if (saliendo) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused) Resume();
            else Pause();
            return;
        }

        // Detección directa de clics (no depende del EventSystem)
        if (isPaused && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 pos = Mouse.current.position.ReadValue();

            if (ClicEnBoton(botonSalir, pos)) OnSalirAlMenu();
            else if (ClicEnBoton(botonContinuar, pos)) Resume();
        }
    }

    bool ClicEnBoton(Button boton, Vector2 posPantalla)
    {
        if (boton == null || !boton.gameObject.activeInHierarchy) return false;

        Canvas canvas = boton.GetComponentInParent<Canvas>();
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? canvas.worldCamera : null;

        return RectTransformUtility.RectangleContainsScreenPoint(
            (RectTransform)boton.transform, posPantalla, cam);
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (pausePanel != null) pausePanel.SetActive(true);
        if (LapTimer.Instance != null) LapTimer.Instance.PausarCronometro();
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null) pausePanel.SetActive(false);
        if (LapTimer.Instance != null) LapTimer.Instance.ReanudarCronometro();
    }

    public void OnSalirAlMenu()
    {
        if (saliendo) return;
        saliendo = true;
        StartCoroutine(SalirAlMenuRoutine());
    }

    IEnumerator SalirAlMenuRoutine()
    {
        // El juego sigue pausado (timeScale 0) mientras se envía; por eso usamos unscaledDeltaTime
        if (LapTimer.Instance != null)
        {
            RaceResult r = LapTimer.Instance.GetRaceResult();
            Debug.Log("=== DATOS LISTOS PARA SUPABASE ===");
            Debug.Log($"Jugador: {r.playerName} | Pista: {r.trackName} | Mejor: {r.bestLapTime} | Vueltas: {r.lapsCompleted} | Fecha: {r.dateTime}");

            if (LapTimer.Instance.TieneVueltaValida)
            {
                bool terminado = false;
                SupabaseUploader.Instance.Enviar(r, ok => terminado = true);

                float esperado = 0f;
                while (!terminado && esperado < esperaMaximaEnvio)
                {
                    esperado += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            else
            {
                Debug.Log("Sin vueltas completas: no se envía nada a Supabase.");
            }
        }
        else
        {
            Debug.LogWarning("LapTimer.Instance es null, no hay datos que guardar");
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(nombreEscenaMenu);
    }
}