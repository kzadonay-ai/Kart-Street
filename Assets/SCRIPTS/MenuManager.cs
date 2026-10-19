using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject mainMenu;
    public GameObject menuName;
    public GameObject menuMap;
    public GameObject menuCar;

    [Header("Input Nombre")]
    public TMP_InputField inputNombre;

    [Header("Selección de Auto")]
    public Button[] carButtons;
    public Color selectedColor = Color.yellow;
    public Color normalColor = Color.white;

    private string selectedMap;
    private int selectedCarIndex = 0;

    void Start()
    {
        // Por si venimos de la carrera
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        ClearSessionData();
        ShowOnly(mainMenu);

        // Conecta cada botón de auto con su número (0, 1, 2...)
        if (carButtons != null)
        {
            for (int i = 0; i < carButtons.Length; i++)
            {
                if (carButtons[i] == null) continue;
                int indice = i; // copia necesaria para el listener
                carButtons[i].onClick.AddListener(() => OnSelectCar(indice));
            }
        }
    }

    // ========== BOTONES PRINCIPALES ==========

    public void OnJugar()
    {
        ShowOnly(menuName);
    }

    public void OnOpciones()
    {
        Debug.Log("Opciones (próximamente)");
    }

    public void OnSalir()
    {
        Application.Quit();
    }

    // ========== NOMBRE ==========

    public void OnConfirmarNombre()
    {
        if (string.IsNullOrEmpty(inputNombre.text.Trim()))
        {
            Debug.LogWarning("Escribe un nombre");
            return;
        }

        // Guardamos el nombre de forma limpia
        string nombreLimpio = inputNombre.text.Trim();
        PlayerPrefs.SetString("NombreJugador", nombreLimpio);
        PlayerPrefs.Save();

        // Limpiamos el input para el próximo jugador
        inputNombre.text = "";

        ShowOnly(menuMap);
    }

    // ========== MAPAS ==========

    public void OnSelectPistaPix()
    {
        selectedMap = "PistaPix";
        PlayerPrefs.SetString("MapaSeleccionado", selectedMap);
        ShowOnly(menuCar);
    }

    public void OnSelectPistaBoscosa()
    {
        selectedMap = "PistaBoscosa";
        PlayerPrefs.SetString("MapaSeleccionado", selectedMap);
        ShowOnly(menuCar);
    }

    // ========== SELECCIÓN DE AUTO ==========

    public void OnSelectCar(int carIndex)
    {
        selectedCarIndex = carIndex;
        PlayerPrefs.SetInt("AutoSeleccionado", carIndex);
        PlayerPrefs.Save();

        UpdateCarSelectionVisual();
        Debug.Log("Auto seleccionado: " + (carIndex + 1));
    }

    void UpdateCarSelectionVisual()
    {
        if (carButtons == null) return;

        for (int i = 0; i < carButtons.Length; i++)
        {
            if (carButtons[i] == null) continue;

            ColorBlock colors = carButtons[i].colors;
            colors.normalColor = (i == selectedCarIndex) ? selectedColor : normalColor;
            colors.selectedColor = selectedColor;
            carButtons[i].colors = colors;
        }
    }

    // ========== INICIAR CARRERA ==========

    public void OnStartRace()
    {
        string map = PlayerPrefs.GetString("MapaSeleccionado", "PistaPix");
        int car = PlayerPrefs.GetInt("AutoSeleccionado", 0);
        string nombre = PlayerPrefs.GetString("NombreJugador", "Jugador");

        Debug.Log($"Iniciando carrera → Jugador: {nombre} | Mapa: {map} | Auto: {car + 1}");
        SceneManager.LoadScene(map);
    }

    // ========== VOLVER ==========

    public void OnBackToMain()
    {
        ShowOnly(mainMenu);
    }

    public void OnBackToName()
    {
        ShowOnly(menuName);
    }

    public void OnBackToMap()
    {
        ShowOnly(menuMap);
    }

    void ShowOnly(GameObject panel)
    {
        mainMenu.SetActive(false);
        menuName.SetActive(false);
        menuMap.SetActive(false);
        menuCar.SetActive(false);

        if (panel != null)
            panel.SetActive(true);
    }

    // ========== UTILIDADES ==========

    void ClearSessionData()
    {
        // Solo limpiamos lo que no queremos que se arrastre al siguiente jugador
        // (el nombre se limpia después de confirmarlo)
        PlayerPrefs.DeleteKey("MapaSeleccionado");
        PlayerPrefs.DeleteKey("AutoSeleccionado");
    }
}