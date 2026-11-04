using UnityEngine;

public class DiaInicioManager : MonoBehaviour
{
    private void Start()
    {
        Invoke(nameof(IniciarResumen), 0.2f);
    }

    private void IniciarResumen()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.ResumenDia == null) return;

        var data = GameManager.Instance.GameData;
        GameManager.Instance.ResumenDia.IniciarDia(
            data.dineroTotal,
            data.pasajerosTotales,
            data.contadorLimpieza,
            data.contadorGrasa,
            data.contadorPuerta
        );

        Debug.Log("📊 Día iniciado, resumen reseteado");
    }
}