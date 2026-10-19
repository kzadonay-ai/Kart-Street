using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// Envía un RaceResult a Supabase (tabla vía REST).
// Se crea solo y sobrevive al cambio de escena (DontDestroyOnLoad).
public class SupabaseUploader : MonoBehaviour
{
    // ========== CONFIGURACIÓN: lo completa quien creó la tabla ==========
    const string SUPABASE_URL = "https://TU-PROYECTO.supabase.co";
    const string SUPABASE_ANON_KEY = "PEGAR_AQUI_LA_ANON_KEY";
    const string TABLA = "race_results";
    const int TIMEOUT_SEGUNDOS = 10;

    // Nombres de columnas esperados en la tabla (si cambian, se editan SOLO aquí)
    [Serializable]
    class Payload
    {
        public string player_name;
        public string track_name;
        public float best_lap_time;   // segundos
        public int laps_completed;
        // created_at: que la tabla lo ponga sola (default now())
    }

    static SupabaseUploader _instance;
    public static SupabaseUploader Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("SupabaseUploader");
                _instance = go.AddComponent<SupabaseUploader>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    bool Configurado =>
        !SUPABASE_URL.Contains("TU-PROYECTO") &&
        !SUPABASE_ANON_KEY.StartsWith("PEGAR_AQUI");

    // onTerminado(true) = guardado OK, false = falló o no está configurado
    public void Enviar(RaceResult r, Action<bool> onTerminado = null)
    {
        if (!Configurado)
        {
            Debug.LogWarning("[Supabase] Falta configurar URL y ANON KEY en SupabaseUploader.cs. No se envió nada.");
            onTerminado?.Invoke(false);
            return;
        }

        StartCoroutine(EnviarRoutine(r, onTerminado));
    }

    IEnumerator EnviarRoutine(RaceResult r, Action<bool> onTerminado)
    {
        Payload p = new Payload
        {
            player_name = r.playerName,
            track_name = r.trackName,
            best_lap_time = r.bestLapTime,
            laps_completed = r.lapsCompleted
        };

        string json = JsonUtility.ToJson(p);
        string url = $"{SUPABASE_URL}/rest/v1/{TABLA}";

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.timeout = TIMEOUT_SEGUNDOS;

            req.SetRequestHeader("apikey", SUPABASE_ANON_KEY);
            req.SetRequestHeader("Authorization", "Bearer " + SUPABASE_ANON_KEY);
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Prefer", "return=minimal");

            yield return req.SendWebRequest();

            bool ok = req.result == UnityWebRequest.Result.Success;

            if (ok)
                Debug.Log("[Supabase] Resultado guardado: " + json);
            else
                Debug.LogError($"[Supabase] Error {req.responseCode}: {req.error}\n{req.downloadHandler.text}");

            onTerminado?.Invoke(ok);
        }
    }
}