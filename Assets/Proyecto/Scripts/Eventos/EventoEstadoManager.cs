using UnityEngine;
using System.Collections.Generic;
using System;

public class EventoEstadoManager : MonoBehaviour
{
    public enum CondicionAscensor
    {
        Cualquiera,
        SoloArribaQuieto
    }

    [System.Serializable]
    public class EventoEstadoConfig : EventoConfig
    {
        public TipoSistema sistema;

        [Tooltip("Cuándo puede salir este evento según el ascensor")]
        public CondicionAscensor condicionAscensor = CondicionAscensor.SoloArribaQuieto;
    }

    public List<EventoEstadoConfig> eventos = new List<EventoEstadoConfig>();

    [Header("Ascensor")]
    [Tooltip("Si se deja vacío, se busca solo en la escena")]
    public AscensorManager ascensorManager;

    public bool debugLogs = true;

    public Action<GameObject> OnEventoTerminado;

    private bool eventoEnCurso = false;
    private GameObject eventoInstanciado;

    [Obsolete]
    private void Awake()
    {
        if (ascensorManager == null)
            ascensorManager = FindFirstObjectByType<AscensorManager>();
    }

    public bool HayEventoEnCurso => eventoEnCurso;

    public List<EventoEstadoConfig> ObtenerConfigs(bool basura)
    {
        List<EventoEstadoConfig> lista = new List<EventoEstadoConfig>();

        foreach (var e in eventos)
        {
            bool esBasura = e.sistema == TipoSistema.Limpieza;
            if (esBasura == basura && e.prefab != null)
                lista.Add(e);
        }

        return lista;
    }

    public bool PuedeLanzarAhora(EventoEstadoConfig e)
    {
        if (e == null || eventoEnCurso) return false;
        if (BloqueoEventos.EventosBloqueados) return false;
        return CumpleCondicionAscensor(e);
    }

    public bool LanzarConfig(EventoEstadoConfig e)
    {
        if (!PuedeLanzarAhora(e)) return false;

        LanzarEvento(e);
        return true;
    }

    public bool ForzarEvento<T>() where T : EventoBase
    {
        if (eventoEnCurso) return false;

        var config = eventos.Find(e => e.prefab != null && e.prefab.GetComponent<T>() != null);
        if (config == null) return false;

        LanzarEvento(config);
        return true;
    }

    private bool CumpleCondicionAscensor(EventoEstadoConfig e)
    {
        if (e.condicionAscensor == CondicionAscensor.Cualquiera)
            return true;

        return ascensorManager == null || !ascensorManager.enMovimiento;
    }

    private void LanzarEvento(EventoEstadoConfig e)
    {
        eventoEnCurso = true;

        if (debugLogs)
            Debug.Log($" Lanzando evento de estado: {e.nombre}");

        if (GameManager.Instance != null)
            GameManager.Instance.GameData.eventoOcurriendo = true;

        if (e.prefab == null)
        {
            Debug.LogError($"El evento '{e.nombre}' no tiene prefab asignado");
            TerminarEvento();
            return;
        }

        eventoInstanciado = Instantiate(e.prefab);

        var eventoBase = eventoInstanciado.GetComponent<EventoBase>();
        if (eventoBase != null)
        {
            eventoBase.OnTerminado += TerminarEventoDesdeBase;
        }
        else
        {
            Destroy(eventoInstanciado, 5f);
            Invoke(nameof(TerminarEvento), 5f);
        }
    }

    private void TerminarEventoDesdeBase()
    {
        if (debugLogs)
            Debug.Log("EventoBase avisó que terminó");

        TerminarEvento();
    }

    private void TerminarEvento()
    {
        BloqueoEventos.BloquearArranque(1f);

        eventoEnCurso = false;
        eventoInstanciado = null;

        if (GameManager.Instance != null)
            GameManager.Instance.GameData.eventoOcurriendo = false;

        OnEventoTerminado?.Invoke(eventoInstanciado);

        if (debugLogs)
            Debug.Log(" Evento de estado finalizado. Listo para el siguiente.");
    }
}
