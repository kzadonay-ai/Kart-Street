using UnityEngine;

public class CarMenuPager : MonoBehaviour
{
    [Header("Páginas")]
    public GameObject[] paginas;

    [Header("Flechas")]
    public GameObject botonAnterior;
    public GameObject botonSiguiente;

    private int paginaActual = 0;

    // Se ejecuta cada vez que aparece el panel de autos
    void OnEnable()
    {
        Mostrar(0);
    }

    public void Siguiente() { Mostrar(paginaActual + 1); }
    public void Anterior() { Mostrar(paginaActual - 1); }

    void Mostrar(int indice)
    {
        if (paginas == null || paginas.Length == 0) return;

        paginaActual = Mathf.Clamp(indice, 0, paginas.Length - 1);

        for (int i = 0; i < paginas.Length; i++)
            if (paginas[i] != null)
                paginas[i].SetActive(i == paginaActual);

        if (botonAnterior != null)
            botonAnterior.SetActive(paginaActual > 0);
        if (botonSiguiente != null)
            botonSiguiente.SetActive(paginaActual < paginas.Length - 1);
    }
}