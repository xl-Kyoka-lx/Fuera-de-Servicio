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

        if (botonLimpiar == null && botonTienda != null)
            botonLimpiar = CrearBotonLimpiar();

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
        int kits = EfectosManager.Stock("kit_limpieza");

        if (kits <= 0)
        {
            ConfirmacionUI.Avisar("No tienes kits de limpieza.\nCómpralos en la tienda.");
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.GameData.contadorLimpieza >= 100f)
        {
            ConfirmacionUI.Avisar("El ascensor ya está completamente limpio.");
            return;
        }

        string textoKits = kits == 1 ? "el único kit de limpieza que tienes" : $"1 de los {kits} kits de limpieza que tienes";
        ConfirmacionUI.Preguntar($"Consumirás {textoKits}.\n¿Seguro?", () =>
        {
            if (EfectosManager.UsarKitLimpieza())
            {
                var data = GameManager.Instance.GameData;
                ConfirmacionUI.Avisar($"Limpiaste el ascensor (+{EfectosManager.limpiezaKit:0}%).\nLimpieza: {data.contadorLimpieza:F0}%\nKits restantes: {EfectosManager.Stock("kit_limpieza")}");
            }
        });
    }

    private Button CrearBotonLimpiar()
    {
        Button copia = Instantiate(botonTienda, botonTienda.transform.parent);
        copia.name = "boton_limpiar";

        for (int i = 0; i < copia.onClick.GetPersistentEventCount(); i++)
            copia.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
        copia.onClick.RemoveAllListeners();

        RectTransform rt = copia.GetComponent<RectTransform>();
        RectTransform original = botonTienda.GetComponent<RectTransform>();
        rt.anchoredPosition = original.anchoredPosition - new Vector2(0f, original.rect.height * 1.3f);

        var texto = copia.GetComponentInChildren<TMPro.TMP_Text>(true);
        if (texto != null) texto.text = "Limpiar";

        return copia;
    }

    public void OnContinuar()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.AplicarPopularidadDelDia();

        var pago = FindAnyObjectByType<PaymentManager>();
        if (pago != null)
            pago.IniciarCobro();
        else if (panelPago != null)
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