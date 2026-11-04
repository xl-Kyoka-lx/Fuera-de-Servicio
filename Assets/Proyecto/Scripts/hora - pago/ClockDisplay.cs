using UnityEngine;
using TMPro;

public class ClockDisplay : MonoBehaviour
{
    [SerializeField] private ClockManager clock;
    [SerializeField] private TMP_Text horaTexto;

    private void OnEnable()
    {
        if (clock != null)
        {
            clock.OnHoraCambio += ActualizarHora;
            clock.OnDiaTerminado += MostrarFinDia;
        }
        ActualizarHora(clock != null ? clock.GetHoraActual() : 0);
    }

    private void OnDisable()
    {
        if (clock != null)
        {
            clock.OnHoraCambio -= ActualizarHora;
            clock.OnDiaTerminado -= MostrarFinDia;
        }
    }

    private void ActualizarHora(int hora)
    {
        if (horaTexto != null)
            horaTexto.text = $"{hora:00}:00";
    }

    private void MostrarFinDia()
    {
        if (horaTexto != null)
            horaTexto.text = "--:--";
    }
}