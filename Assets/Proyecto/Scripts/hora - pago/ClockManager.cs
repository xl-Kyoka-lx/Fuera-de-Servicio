using UnityEngine;
using System;

public class ClockManager : MonoBehaviour
{
    [Tooltip("Duración real de un día completo, en segundos (600 = 10 minutos)")]
    public float duracionDiaSegundos = 600f;
    public int horaInicio = 7;
    public int horaFin = 21;

    public int horaActual = 7;
    public bool relojActivo = true;

    public event Action<int> OnHoraCambio;
    public event Action OnDiaTerminado;

    public event Action OnCierreDelDia;

    private float timer = 0f;

    private bool terminandoDia = false;

    public float SegundosPorHora => duracionDiaSegundos / Mathf.Max(1, horaFin - horaInicio);

    public float SegundosTranscurridos => (horaActual - horaInicio) * SegundosPorHora + timer;

    private void Start()
    {
        horaActual = horaInicio;
        timer = 0f;
        if (GameManager.Instance != null)
            GameManager.Instance.GameData.horaActual = horaInicio;

        Debug.Log($"Reloj iniciado a las {horaActual}:00 (día de {duracionDiaSegundos:F0} s, {SegundosPorHora:F1} s por hora)");
    }

    private void Update()
    {
        if (!relojActivo) return;

        var data = GameManager.Instance != null ? GameManager.Instance.GameData : null;

        if (PausaManager.juegoPausado) return;
        if (EstadoJuegoManager.Instance != null && EstadoJuegoManager.Instance.EstaPausado()) return;
        if (data != null && data.ascensorAveriado) return;

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

            if (GameManager.Instance.ResumenDia != null)
            {
                GameManager.Instance.ResumenDia.TerminarDia(data.dineroTotal);
            }

            data.resumenNocheMostrado = false;

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
            if (!terminandoDia)
            {
                terminandoDia = true;
                OnCierreDelDia?.Invoke();
            }
        }
    }

    public int GetHoraActual() => horaActual;

    public float HoraDecimal => horaActual + Mathf.Clamp01(timer / SegundosPorHora);

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