using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI textoLimpieza;
    public TextMeshProUGUI textoPolea;
    public TextMeshProUGUI textoPasajeros;
    public TextMeshProUGUI textoEstadoAscensor;
    public TextMeshProUGUI textoDinero;

    [Header("Pasajeros")]
    [Tooltip("Si se deja vacío, se busca solo en la escena")]
    public AscensorManager ascensorManager;
    [Tooltip("Cabina cuyos pasajeros se muestran (posición en la lista del AscensorManager, 0 = la primera)")]
    public int indiceCabina = 0;

    private GameData gameData;

    [System.Obsolete]
    private void Start()
    {
        gameData = GameManager.Instance.gameData;
        gameData.OnDataChanged += ActualizarUI;

        if (ascensorManager == null)
            ascensorManager = FindFirstObjectByType<AscensorManager>();
        if (ascensorManager != null)
            ascensorManager.OnPasajerosCambiados += ActualizarUI;

        ActualizarUI();
    }

    private void OnDestroy()
    {
        if (gameData != null)
            gameData.OnDataChanged -= ActualizarUI;
        if (ascensorManager != null)
            ascensorManager.OnPasajerosCambiados -= ActualizarUI;
    }

    private void ActualizarUI()
    {
        if (textoLimpieza != null)
            textoLimpieza.text = $" Limpieza: {gameData.contadorLimpieza:F0}%";

        if (textoPolea != null)
            textoPolea.text = $"Mantenimiento: {gameData.contadorGrasa:F0}%";

        if (textoPasajeros != null)
            textoPasajeros.text = TextoPasajeros();

        if (textoDinero != null)
            textoDinero.text = $" Dinero: {gameData.dineroTotal:F0}";

        if (textoEstadoAscensor != null)
        {
            string estado = gameData.ascensorEnMovimiento ? " EN MOVIMIENTO" : " DETENIDO";
            textoEstadoAscensor.text = estado;
        }
    }

    // "Pasajeros: 3/5" = cuántos van dentro de la cabina / cuántos caben
    private string TextoPasajeros()
    {
        if (ascensorManager == null || indiceCabina < 0 || indiceCabina >= ascensorManager.ascensores.Count)
            return " Pasajeros: -";

        var cabina = ascensorManager.ascensores[indiceCabina];
        return $" Pasajeros: {cabina.PasajerosTotales}/{cabina.CapacidadMaxima}";
    }
}
