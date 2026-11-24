using System.Collections.Generic;
using UnityEngine;

// Decide cuántos eventos tiene cada día y cuándo salen.
public class DirectorEventos : MonoBehaviour
{
    [Header("Referencias")]
    public EventoEstadoManager estadoManager;
    public EventManager eventManager;
    public ClockManager clockManager;

    [Header("Eventos de mantenimiento por día")]
    [Tooltip("Cantidad con el mantenimiento en 100%")]
    public float eventosMantenimientoEn100 = 3f;
    [Tooltip("Cantidad con el mantenimiento casi en 0%")]
    public float eventosMantenimientoEn0 = 12f;

    [Header("Eventos de basura por día")]
    [Tooltip("Cantidad con la limpieza en 100%")]
    public float eventosBasuraEn100 = 1f;
    [Tooltip("Cantidad con la limpieza en 0%")]
    public float eventosBasuraEn0 = 6f;

    [Header("Horario del día")]
    [Tooltip("Duración estimada del día jugando, en segundos (incluye los viajes). Los eventos se reparten dentro de este tiempo")]
    public float duracionDiaEstimada = 240f;
    [Tooltip("Segundos sin eventos al empezar el día")]
    public float margenInicio = 15f;
    [Tooltip("Segundos sin eventos antes del final estimado")]
    public float margenFinal = 20f;
    [Tooltip("Segundos de calma después de que termina un evento")]
    public float pausaEntreEventos = 4f;

    public bool debugLogs = true;

    private class EventoProgramado
    {
        public EventoEstadoManager.EventoEstadoConfig config;
        public float tiempo;
        public bool lanzado;
    }

    private readonly List<EventoProgramado> programados = new List<EventoProgramado>();
    private bool yaProgramado = false;
    private float tiempoDia = 0f;
    private float cooldown = 0f;
    private bool huboEvento = false;

    private void Start()
    {
        if (estadoManager == null) estadoManager = FindAnyObjectByType<EventoEstadoManager>();
        if (eventManager == null) eventManager = FindAnyObjectByType<EventManager>();
        if (clockManager == null) clockManager = FindAnyObjectByType<ClockManager>();

        if (estadoManager != null)
            estadoManager.modoDirector = true;
    }

    private void Update()
    {
        if (estadoManager == null || GameManager.Instance == null) return;

        // Tutorial: mientras esté apagado, no se programa ni se lanza nada
        if (!estadoManager.enabled) return;
        if (eventManager != null && !eventManager.eventosActivos) return;

        if (!yaProgramado)
        {
            ProgramarDia();
            yaProgramado = true;
        }

        if (clockManager != null && !clockManager.relojActivo) return;
        if (Time.timeScale == 0f) return;
        if (DialogoManager.Instance != null && DialogoManager.Instance.HayDialogoActivo()) return;

        // El tiempo del día no corre mientras hay un evento en curso
        if (estadoManager.HayEventoEnCurso || GameManager.Instance.GameData.eventoOcurriendo)
        {
            huboEvento = true;
            return;
        }

        if (huboEvento)
        {
            huboEvento = false;
            cooldown = pausaEntreEventos;
        }

        tiempoDia += Time.deltaTime;

        if (cooldown > 0f)
        {
            cooldown -= Time.deltaTime;
            return;
        }

        if (BloqueoEventos.EventosBloqueados) return;

        // Los eventos ya vencidos esperan hasta que las condiciones lo permitan
        // (por ejemplo, ascensor arriba y quieto)
        foreach (var p in programados)
        {
            if (p.lanzado || p.tiempo > tiempoDia) continue;

            if (estadoManager.LanzarConfig(p.config))
            {
                p.lanzado = true;

                if (debugLogs)
                    Debug.Log($"[Director] Lanzado '{p.config.nombre}' (programado a los {p.tiempo:F0}s, ahora {tiempoDia:F0}s)");

                break;
            }
        }
    }

    private void ProgramarDia()
    {
        programados.Clear();

        var data = GameManager.Instance.GameData;
        float mantenimiento = Mathf.Clamp01(data.mantenimientoDelDia / 100f);
        float limpieza = Mathf.Clamp01(data.limpiezaDelDia / 100f);

        int cantMantenimiento = Mathf.RoundToInt(Mathf.Lerp(eventosMantenimientoEn0, eventosMantenimientoEn100, mantenimiento));
        int cantBasura = Mathf.RoundToInt(Mathf.Lerp(eventosBasuraEn0, eventosBasuraEn100, limpieza));

        AgregarEventos(estadoManager.ObtenerConfigs(false), cantMantenimiento);
        AgregarEventos(estadoManager.ObtenerConfigs(true), cantBasura);

        programados.Sort((a, b) => a.tiempo.CompareTo(b.tiempo));

        if (debugLogs)
        {
            string horario = "";
            foreach (var p in programados)
                horario += $"\n   {p.tiempo:F0}s -> {p.config.nombre}";

            Debug.Log($"[Director] Día programado. Mantenimiento {data.mantenimientoDelDia:F0}% -> {cantMantenimiento} eventos. " +
                      $"Limpieza {data.limpiezaDelDia:F0}% -> {cantBasura} eventos.{horario}");
        }
    }

    // Reparte 'cantidad' eventos en el horario del día, uno por tramo, para que no se amontonen.
    private void AgregarEventos(List<EventoEstadoManager.EventoEstadoConfig> configs, int cantidad)
    {
        if (configs.Count == 0 || cantidad <= 0) return;

        float inicio = margenInicio;
        float fin = Mathf.Max(inicio + 1f, duracionDiaEstimada - margenFinal);
        float tramo = (fin - inicio) / cantidad;

        for (int i = 0; i < cantidad; i++)
        {
            EventoProgramado p = new EventoProgramado();
            p.config = ElegirConfig(configs);
            p.tiempo = inicio + tramo * i + Random.Range(0.15f, 0.85f) * tramo;
            programados.Add(p);
        }
    }

    private EventoEstadoManager.EventoEstadoConfig ElegirConfig(List<EventoEstadoManager.EventoEstadoConfig> configs)
    {
        float total = 0f;
        foreach (var c in configs) total += Mathf.Max(0f, c.peso);

        if (total <= 0f) return configs[Random.Range(0, configs.Count)];

        float r = Random.Range(0f, total);
        float acumulado = 0f;

        foreach (var c in configs)
        {
            acumulado += Mathf.Max(0f, c.peso);
            if (r <= acumulado) return c;
        }

        return configs[0];
    }
}