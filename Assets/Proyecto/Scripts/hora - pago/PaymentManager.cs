using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System;

public class PaymentManager : MonoBehaviour
{
    public int costoPago = 100;
    public string nombreEscenaDia = "Escena_Dia";
    public GameObject panelPago;
    public GameObject panelDerrota;
    public TextMeshProUGUI textoCosto;
    public TextMeshProUGUI textoDineroActual;
    public bool esperandoPago = false;

    public event Action OnPagoExitoso;
    public event Action OnDerrota;
    private GameData gameData;

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            gameData = GameManager.Instance.gameData;
            
            gameData.OnDataChanged += ActualizarTextosPanel;
        }
        else
        {
            Debug.LogError("PaymentManager: GameManager.Instance es null en Start()");
        }

        if (panelPago != null) panelPago.SetActive(false);
        if (panelDerrota != null) panelDerrota.SetActive(false);
    }

    private void OnDestroy()
    {
        if (gameData != null)
            gameData.OnDataChanged -= ActualizarTextosPanel;
    }

    public int netoDelDia { get; private set; }

    public string TextoCobro => netoDelDia <= 0 ? $"Recibes: ${-netoDelDia}" : $"Debes pagar: ${netoDelDia}";

    public void IniciarCobro()
    {
        if (gameData == null && GameManager.Instance != null)
            gameData = GameManager.Instance.gameData;

        if (gameData != null)
        {
            netoDelDia = Economia.Calcular(gameData).Neto;
            costoPago = Mathf.Max(0, netoDelDia);
        }

        esperandoPago = true;

        if (panelPago != null)
            panelPago.SetActive(true);

        ActualizarTextosPanel();
    }

    private void ActualizarTextosPanel()
    {
        if (gameData == null) return;

        int dinero = gameData.dineroTotal;

        if (textoCosto != null)
            textoCosto.text = TextoCobro;

        if (textoDineroActual != null)
        {
            textoDineroActual.text = $"Tu dinero: ${dinero}";
            textoDineroActual.color = dinero >= costoPago ? Color.green : Color.red;
        }
    }
    public void IntentarPagar()
    {
        if (!esperandoPago)
        {
            Debug.LogWarning("esperandoPago es false");
            return;
        }

        if (gameData == null)
        {
            Debug.LogError("gameData es null");
            return;
        }

        int dinero = gameData.dineroTotal;
        
        if (dinero >= netoDelDia)
        {
            gameData.dineroTotal -= netoDelDia;
            gameData.RegistrarGasto(netoDelDia);
            gameData.CerrarEstadisticasDelDia();
            gameData.diaActual++;

            gameData.OnDataChanged?.Invoke();

            esperandoPago = false;
            if (panelPago != null) panelPago.SetActive(false);

            OnPagoExitoso?.Invoke();

            IrAEscenaDia();
        }
        else
        {
             Derrota();
        }
    }

    private void IrAEscenaDia()
    {
        if (gameData != null)
            gameData.horaActual = 7;

        SceneManager.LoadScene(nombreEscenaDia);
    }

    private void Derrota()
    {
        esperandoPago = false;
        if (panelPago != null) panelPago.SetActive(false);
        PantallaFueraDeServicio.Mostrar();

        OnDerrota?.Invoke();
    }

    public void ReiniciarPago()
    {
        esperandoPago = false;
        if (panelPago != null) panelPago.SetActive(false);
        if (panelDerrota != null) panelDerrota.SetActive(false);
        PausaJuego.Liberar(this);
    }
}