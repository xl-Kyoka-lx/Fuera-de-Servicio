using UnityEngine;
using System;

// Reloj del día. Corre a un ritmo fijo: no se detiene por eventos ni por viajes,
// así todos los días duran lo mismo. Solo espera cuando el juego está en pausa o en un diálogo.
public class ClockManager : MonoBehaviour
{
    [Tooltip("Duración real de un día completo, en segundos (600 = 10 minutos)")]
    public float duracionDiaSegundos = 600f;
    public int horaInicio = 8;
    public int horaFin = 17;

    public int horaActual = 8;
    public bool relojActivo = true;

    public event Action<int> OnHoraCambio;
    public event Action OnDiaTerminado;

    private float timer = 0f;

    // Llegó la hora de cierre: el día termina cuando acaba el evento o el viaje en curso
    private bool terminandoDia = false;

    // Segundos reales que dura cada hora del juego
    public float SegundosPorHora => duracionDiaSegundos / Mathf.Max(1, horaFin - horaInicio);

    // Segundos del día que ya pasaron (lo usa el Director para repartir los eventos)
    public float SegundosTranscurridos => (horaActual - horaInicio) * SegundosPorHora + timer;

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            horaActual = GameManager.Instance.GameData.horaActual;
        }
        else
        {
            horaActual = horaInicio;
        }

        Debug.Log($"Reloj iniciado a las {horaActual}:00 (día de {duracionDiaSegundos:F0} s, {SegundosPorHora:F1} s por hora)");
    }

    private void Update()
    {
        if (!relojActivo) return;

        var data = GameManager.Instance != null ? GameManager.Instance.GameData : null;

        // Pausa, diálogos y averías: el reloj espera
        if (PausaManager.juegoPausado) return;
        if (EstadoJuegoManager.Instance != null && EstadoJuegoManager.Instance.EstaPausado()) return;
        if (data != null && data.ascensorAveriado) return;

        // Los eventos congelan el tiempo del juego (timeScale 0), pero el reloj sigue corriendo
        float dt;
        if (Time.timeScale > 0f) dt = Time.deltaTime;
        else if (data != null && data.eventoOcurriendo) dt = Time.unscaledDeltaTime;
        else return;

        if (terminandoDia)
        {
            if (data != null && (data.eventoOcurriendo || data.ascensorEnMovimiento)) return;

            terminandoDia = false;
            TerminarDia();
            return;
        }

        timer += dt;

        if (timer >= SegundosPorHora)
        {
            timer -= SegundosPorHora;
            AvanzarHora();
        }
    }

    private void TerminarDia()
    {
        relojActivo = false;

        if (GameManager.Instance != null)
        {
            var data = GameManager.Instance.GameData;

            // Cerrar el resumen
            if (GameManager.Instance.ResumenDia != null)
            {
                GameManager.Instance.ResumenDia.TerminarDia(
                    data.dineroTotal,
                    data.pasajerosTotales
                );
            }

            // Resetear el flag de resumen mostrado
            data.resumenNocheMostrado = false;

            // Desgaste fijo del ascensor al terminar cada día
            if (!data.ascensorAveriado)
                GameManager.Instance.AplicarDesgasteDiario();
        }

        Debug.Log("Día terminado");
        OnDiaTerminado?.Invoke();
    }

    private void AvanzarHora()
    {
        horaActual++;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameData.horaActual = horaActual;
        }

        OnHoraCambio?.Invoke(horaActual);
        Debug.Log($"Hora actual: {horaActual}:00");

        if (horaActual >= horaFin)
        {
            // No se corta en medio de un evento o de un viaje: se espera a que terminen
            terminandoDia = true;
        }
    }

    public int GetHoraActual() => horaActual;

    public void ReiniciarDia()
    {
        horaActual = horaInicio;
        timer = 0f;
        relojActivo = true;
        terminandoDia = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameData.horaActual = horaActual;
        }

        Debug.Log($"Nuevo día comenzado a las {horaActual}:00");
    }
}