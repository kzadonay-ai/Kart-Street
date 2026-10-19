[System.Serializable]
public class RaceResult
{
    public string playerName;
    public string trackName;
    public float bestLapTime;      // en segundos
    public int lapsCompleted;
    public string dateTime;        // útil para la base de datos

    public RaceResult(string name, string track, float bestTime, int laps)
    {
        playerName = name;
        trackName = track;
        bestLapTime = bestTime;
        lapsCompleted = laps;
        dateTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}