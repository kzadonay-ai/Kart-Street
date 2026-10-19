using UnityEngine;
using TMPro;
using System;

public class LapTimer : MonoBehaviour
{
    public static LapTimer Instance { get; private set; }

    [Header("UI")]
    public TextMeshProUGUI tiempoActualTexto;
    public TextMeshProUGUI mejorTiempoTexto;
    public TextMeshProUGUI vueltaActualTexto;

    [Header("Configuración")]
    public int totalVueltas = 3;

    // Datos de la carrera actual
    private float tiempoActual = 0f;
    private float mejorTiempoVuelta = float.MaxValue;
    private int vueltasCompletadas = 0;
    private bool cronometroActivo = false;
    private bool primeraVuelta = true;
    private float ultimoCruce = -10f;
    private string nombreJugador;
    private string nombrePista;

    // True si hay al menos una vuelta completa (dato válido para guardar)
    public bool TieneVueltaValida => mejorTiempoVuelta < float.MaxValue;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        nombreJugador = PlayerPrefs.GetString("NombreJugador", "Jugador");
        nombrePista = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        ActualizarUI();
    }

    void Update()
    {
        if (cronometroActivo)
        {
            tiempoActual += Time.deltaTime;
            ActualizarUI();
        }
    }

    public void CruzoLineaDeMeta()
    {
        // Anti-rebote: ignora cruces repetidos en menos de 2 segundos
        if (Time.time - ultimoCruce < 2f) return;
        ultimoCruce = Time.time;

        if (primeraVuelta)
        {
            primeraVuelta = false;
            cronometroActivo = true;
            tiempoActual = 0f;
            vueltasCompletadas = 0;
            Debug.Log("Cronómetro iniciado");
        }
        else
        {
            // Completó una vuelta
            vueltasCompletadas++;

            if (tiempoActual < mejorTiempoVuelta)
            {
                mejorTiempoVuelta = tiempoActual;
            }

            Debug.Log($"Vuelta {vueltasCompletadas} completada en {FormatearTiempo(tiempoActual)}");

            // Reiniciamos el tiempo de la vuelta actual
            tiempoActual = 0f;
        }

        ActualizarUI();
    }

    // ========== ESTE MÉTODO SE LLAMA AL SALIR DE LA CARRERA ==========
    public RaceResult GetRaceResult()
    {
        // Si nunca hizo una vuelta válida, ponemos 0
        float bestTime = TieneVueltaValida ? mejorTiempoVuelta : 0f;

        // Nunca reportamos más vueltas que el total de la carrera
        int vueltas = Mathf.Min(vueltasCompletadas, totalVueltas);

        return new RaceResult(
            nombreJugador,
            nombrePista,
            bestTime,
            vueltas
        );
    }

    void ActualizarUI()
    {
        if (tiempoActualTexto != null)
            tiempoActualTexto.text = FormatearTiempo(tiempoActual);

        if (mejorTiempoTexto != null)
        {
            if (mejorTiempoVuelta < float.MaxValue)
                mejorTiempoTexto.text = "Mejor: " + FormatearTiempo(mejorTiempoVuelta);
            else
                mejorTiempoTexto.text = "Mejor: --:--.---";
        }

        if (vueltaActualTexto != null)
            vueltaActualTexto.text = $"Vuelta {Mathf.Min(vueltasCompletadas + 1, totalVueltas)}/{totalVueltas}";
    }

    string FormatearTiempo(float tiempo)
    {
        TimeSpan t = TimeSpan.FromSeconds(tiempo);
        return string.Format("{0:00}:{1:00}.{2:000}", t.Minutes, t.Seconds, t.Milliseconds);
    }

    public void PausarCronometro() => cronometroActivo = false;

    public void ReanudarCronometro()
    {
        if (!primeraVuelta)
            cronometroActivo = true;
    }
}