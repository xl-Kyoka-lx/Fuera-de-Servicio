using System.Collections.Generic;
using UnityEngine;

public class DirectorEventos : MonoBehaviour
{
    public enum Momento
    {
        CualquierMomento,
        AlPulsarW,
        AlArrancar,
        AlLlegar
    }

    [Header("Referencias")]
    public EventoEstadoManager estadoManager;
    public EventManager eventManager;
    public ClockManager clockManager;
    [Tooltip("Si se deja vacío, se busca solo en la escena")]
    public ProcesoPartida procesoPartida;

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
    [Tooltip("Segundos sin eventos al empezar el día (el día completo dura lo que diga el ClockManager)")]
    public float margenInicio = 15f;
    [Tooltip("Segundos sin eventos antes del final estimado")]
    public float margenFinal = 20f;
    [Tooltip("Segundos de calma después de que termina un evento (solo para los eventos de cualquier momento)")]
    public float pausaEntreEventos = 4f;
    [Tooltip("Segundos entre un evento de la llegada y el siguiente (puerta y luego basura)")]
    public float pausaEntreEventosEnLlegada = 0.75f;

    public bool debugLogs = true;

    private class EventoProgramado
    {
        public EventoEstadoManager.EventoEstadoConfig config;
        public Momento momento;
        public float tiempo;
        public bool lanzado;
    }

    private readonly List<EventoProgramado> programados = new List<EventoProgramado>();
    private bool yaProgramado = false;
    private float tiempoDia = 0f;
    private float cooldown = 0f;
    private bool huboEvento = false;

    private bool ventanaLlegada = false;
    private float llegadaListaDesde = 0f;

    [System.Obsolete]
    private void Start()
    {
        if (estadoManager == null) estadoManager = FindAnyObjectByType<EventoEstadoManager>();
        if (eventManager == null) eventManager = FindAnyObjectByType<EventManager>();
        if (clockManager == null) clockManager = FindAnyObjectByType<ClockManager>();
        if (procesoPartida == null) procesoPartida = ProcesoPartida.Instance;
        if (procesoPartida == null) procesoPartida = FindFirstObjectByType<ProcesoPartida>();

        if (procesoPartida != null)
        {
            procesoPartida.OnWPulsada += AlPulsarW;
            procesoPartida.OnLlegada += AlLlegar;
        }
        else
        {
            Debug.LogWarning("[Director] No hay ProcesoPartida en la escena: solo saldrán los eventos de cualquier momento");
        }
    }

    private void OnDestroy()
    {
        if (procesoPartida != null)
        {
            procesoPartida.OnWPulsada -= AlPulsarW;
            procesoPartida.OnLlegada -= AlLlegar;
        }
    }

    private void Update()
    {
        if (!PuedeActuar()) return;

        if (clockManager != null) tiempoDia = clockManager.SegundosTranscurridos;

        if (!yaProgramado)
        {
            ProgramarDia();
            yaProgramado = true;
        }

        if (clockManager != null && !clockManager.relojActivo) return;
        if (Time.timeScale == 0f) return;
        if (DialogoManager.Instance != null && DialogoManager.Instance.HayDialogoActivo()) return;

        if (estadoManager.HayEventoEnCurso || GameManager.Instance.GameData.eventoOcurriendo)
        {
            huboEvento = true;
            return;
        }

        if (huboEvento)
        {
            huboEvento = false;
            cooldown = pausaEntreEventos;
            llegadaListaDesde = Time.unscaledTime + pausaEntreEventosEnLlegada;
        }

        ProcesarLlegada();

        if (cooldown > 0f)
        {
            cooldown -= Time.deltaTime;
            return;
        }

        if (BloqueoEventos.EventosBloqueados) return;

        foreach (var p in programados)
        {
            if (p.lanzado || p.momento != Momento.CualquierMomento || p.tiempo > tiempoDia) continue;

            if (estadoManager.LanzarConfig(p.config))
            {
                p.lanzado = true;

                if (debugLogs)
                    Debug.Log($"[Director] Lanzado '{p.config.nombre}' (programado a los {p.tiempo:F0}s, ahora {tiempoDia:F0}s)");

                break;
            }
        }
    }

    private void AlPulsarW()
    {
        ventanaLlegada = false;
        IntentarLanzarEnMomento(Momento.AlPulsarW);
    }

    private void AlLlegar()
    {
        ventanaLlegada = true;
        llegadaListaDesde = Time.unscaledTime + pausaEntreEventosEnLlegada;
    }

    public bool IntentarEventoAlArrancar()
    {
        return IntentarLanzarEnMomento(Momento.AlArrancar);
    }

    private void ProcesarLlegada()
    {
        if (!ventanaLlegada) return;

        if (procesoPartida != null && procesoPartida.fase != ProcesoPartida.Fase.Cargando)
        {
            ventanaLlegada = false;
            return;
        }

        if (!HayVencidoEn(Momento.AlLlegar))
        {
            ventanaLlegada = false;
            return;
        }

        if (Time.unscaledTime < llegadaListaDesde) return;

        IntentarLanzarEnMomento(Momento.AlLlegar);
    }

    private bool HayVencidoEn(Momento momento)
    {
        foreach (var p in programados)
        {
            if (!p.lanzado && p.momento == momento && p.tiempo <= tiempoDia)
                return true;
        }
        return false;
    }

    private bool IntentarLanzarEnMomento(Momento momento)
    {
        if (!PuedeLanzarEnMomento()) return false;

        EventoProgramado elegido = null;

        foreach (var p in programados)
        {
            if (p.lanzado || p.momento != momento || p.tiempo > tiempoDia) continue;

            if (elegido == null || Prioridad(p) < Prioridad(elegido))
                elegido = p;
        }

        if (elegido == null) return false;
        if (!estadoManager.LanzarConfig(elegido.config)) return false;

        elegido.lanzado = true;

        if (debugLogs)
            Debug.Log($"[Director] Lanzado '{elegido.config.nombre}' ({momento}, programado a los {elegido.tiempo:F0}s, ahora {tiempoDia:F0}s)");

        return true;
    }

    private int Prioridad(EventoProgramado p)
    {
        return p.config.sistema == TipoSistema.Limpieza ? 1 : 0;
    }

    private bool PuedeActuar()
    {
        if (estadoManager == null || GameManager.Instance == null) return false;

        if (!estadoManager.enabled) return false;
        if (eventManager != null && !eventManager.eventosActivos) return false;

        return true;
    }

    private bool PuedeLanzarEnMomento()
    {
        if (!PuedeActuar() || !yaProgramado) return false;
        if (clockManager != null && !clockManager.relojActivo) return false;
        if (Time.timeScale == 0f) return false;
        if (DialogoManager.Instance != null && DialogoManager.Instance.HayDialogoActivo()) return false;
        if (BloqueoEventos.EventosBloqueados) return false;
        if (estadoManager.HayEventoEnCurso || GameManager.Instance.GameData.eventoOcurriendo) return false;

        return true;
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
                horario += $"\n   {p.tiempo:F0}s -> {p.config.nombre} ({p.momento})";

            Debug.Log($"[Director] Día programado. Mantenimiento {data.mantenimientoDelDia:F0}% -> {cantMantenimiento} eventos. " +
                      $"Limpieza {data.limpiezaDelDia:F0}% -> {cantBasura} eventos.{horario}");
        }
    }

    private void AgregarEventos(List<EventoEstadoManager.EventoEstadoConfig> configs, int cantidad)
    {
        if (configs.Count == 0 || cantidad <= 0) return;

        float duracionDia = clockManager != null ? clockManager.duracionDiaSegundos : 600f;
        float inicio = tiempoDia + margenInicio;
        float fin = Mathf.Max(inicio + 1f, duracionDia - margenFinal);
        float tramo = (fin - inicio) / cantidad;

        for (int i = 0; i < cantidad; i++)
        {
            EventoProgramado p = new EventoProgramado();
            p.config = ElegirConfig(configs);

            if (p.config.prefab != null && p.config.prefab.GetComponent<EventoPoleaTrabada>() != null
                && Random.value > EfectosManager.ProbabilidadBasuraPoleas)
            {
                if (debugLogs) Debug.Log("[Director] La malla protectora evitó una polea trabada");
                continue;
            }
            p.momento = MomentoDe(p.config);
            p.tiempo = inicio + tramo * i + Random.Range(0.15f, 0.85f) * tramo;
            programados.Add(p);
        }
    }

    private static Momento MomentoDe(EventoEstadoManager.EventoEstadoConfig config)
    {
        if (config.prefab != null)
        {
            if (config.prefab.GetComponent<EventoPestillo>() != null) return Momento.AlPulsarW;
            if (config.prefab.GetComponent<EventoEngrase>() != null) return Momento.AlArrancar;
            if (config.prefab.GetComponent<EventoPuertaAtascada>() != null) return Momento.AlLlegar;
        }

        if (config.sistema == TipoSistema.Limpieza) return Momento.AlLlegar;

        return Momento.CualquierMomento;
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