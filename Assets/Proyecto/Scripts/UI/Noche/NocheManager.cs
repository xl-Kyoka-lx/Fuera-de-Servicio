using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NocheManager : MonoBehaviour
{
    public DayNightController dayNightController;
    public GameObject panelPago;        
    public GameObject panelNoche;       
    public Button botonTienda;
    public Button botonLimpiar;
    public Button botonContinuar;

    public int costoPago = 100;

    public string nombreEscenaTienda = "Tienda";

    public void IrATienda()
    {
        SceneManager.LoadScene(nombreEscenaTienda);
    }

    private void Start()
    {
        if (panelPago != null)
            panelPago.SetActive(false);

        if (botonTienda != null)
            botonTienda.onClick.AddListener(OnTienda);

        if (botonLimpiar != null)
            botonLimpiar.onClick.AddListener(OnLimpiar);

        if (botonContinuar != null)
            botonContinuar.onClick.AddListener(OnContinuar);
    }

    public void OnTienda()
    {
        Debug.Log("Abriendo tienda...");
    }

    public void OnLimpiar()
    {
        Debug.Log(" Iniciando limpieza...");
    }

    public void OnContinuar()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.AplicarPopularidadDelDia();

        if (panelPago != null)
            panelPago.SetActive(true);

        if (panelNoche != null)
            panelNoche.SetActive(false);
    }

    public void OnPagoExitoso()
    {
        if (dayNightController != null)
            dayNightController.IrAEscenaDia();
    }

    public void OnPagoFallido()
    {
        Debug.Log("No pudo pagar. Game Over.");
    }
}