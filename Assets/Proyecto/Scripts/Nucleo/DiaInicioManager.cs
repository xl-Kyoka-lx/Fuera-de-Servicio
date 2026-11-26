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

        // Al empezar el día no hay evento ni viaje en curso (evita valores viejos del asset)
        data.eventoOcurriendo = false;
        data.ascensorEnMovimiento = false;

        GameManager.Instance.ResumenDia.IniciarDia(
            data.dineroTotal,
            data.contadorLimpieza,
            data.contadorGrasa
        );

        Debug.Log("📊 Día iniciado, resumen reseteado");
    }
}