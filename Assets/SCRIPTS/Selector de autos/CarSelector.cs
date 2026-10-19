using UnityEngine;

public class CarSelector : MonoBehaviour
{
    [Header("Autos (mismo orden que los botones del menú)")]
    public GameObject[] autos;

    [Header("Opcional: punto de salida")]
    public Transform puntoDeSalida;

    void Awake()
    {
        if (autos == null || autos.Length == 0)
        {
            Debug.LogWarning("CarSelector: no hay autos asignados");
            return;
        }

        int elegido = Mathf.Clamp(PlayerPrefs.GetInt("AutoSeleccionado", 0), 0, autos.Length - 1);

        for (int i = 0; i < autos.Length; i++)
        {
            if (autos[i] == null) continue;

            bool esElegido = (i == elegido);
            autos[i].SetActive(esElegido);
            autos[i].tag = esElegido ? "Player" : "Untagged";
        }

        if (puntoDeSalida != null && autos[elegido] != null)
            autos[elegido].transform.SetPositionAndRotation(puntoDeSalida.position, puntoDeSalida.rotation);

        Debug.Log("Auto activado: " + autos[elegido].name + " (índice " + elegido + ")");
    }
}