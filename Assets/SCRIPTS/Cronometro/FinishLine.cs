using UnityEngine;

public class FinishLine : MonoBehaviour
{
    public LapTimer lapTimer;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[Meta] Entró: {other.name} | tag: {other.tag}");

        if (!EsJugador(other.transform))
        {
            Debug.Log("[Meta] Ignorado: ni el objeto ni sus padres tienen el tag Player");
            return;
        }

        LapTimer timer = lapTimer != null ? lapTimer : LapTimer.Instance;
        if (timer == null)
        {
            Debug.LogWarning("[Meta] No hay LapTimer en la escena");
            return;
        }

        timer.CruzoLineaDeMeta();
    }

    // Busca el tag Player en el objeto y en todos sus padres
    bool EsJugador(Transform t)
    {
        while (t != null)
        {
            if (t.CompareTag("Player")) return true;
            t = t.parent;
        }
        return false;
    }
}