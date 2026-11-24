using UnityEngine;
using System.Collections.Generic;
using System;

public class EventoEstadoManager : MonoBehaviour
{
    // Cuándo puede salir un evento según el estado del ascensor.
    public enum CondicionAscensor
    {
        Cualquiera,         // en movimiento o detenido, arriba o abajo
        SoloArribaQuieto    // solo cuando el ascensor llegó arriba y está detenido
    }

    [System.Serializable]
    public class EventoEstadoConfig : EventoConfig
    {

        public TipoSistema sistema;

        [Range(0f, 100f)]
        public float umbral = 50f;

        [Range(0f, 1f)]
        public float probabilidadBase = 0.25f;

        [Range(0f, 1f)]
        public float probabilidadMaxima = 1f;

        [Tooltip("Cuándo puede salir este evento según el ascensor")]
        public CondicionAscensor condicionAscensor = CondicionAscensor.SoloArribaQuieto;
    }

    public List<EventoEstadoConfig> eventos = new List<EventoEstadoConfig>();

    [Header("Ascensor")]
    [Tooltip("Si se deja vacío, se busca solo en la escena")]
    public AscensorManager ascensorManager;
    [Tooltip("Posición, en la lista del AscensorManager, del ascensor donde viaja el jugador (0 = el primero)")]
    public int indiceAscensorJugador = 0;

    [Header("Chequeo")]
    public float intervaloChequeo = 3f;
    public bool debugLogs = true;
    [Tooltip("Muestra en la Console por qué cada evento puede o no salir. Desmárcalo cuando todo funcione")]
    public bool debugDetalle = true;

    public Action<GameObject> OnEventoTerminado;

    private float timer = 0f;
    private bool eventoEnCurso = false;
    private GameObject eventoInstanciado;

    [Obsolete]
    private void Awake()
    {
        if (ascensorManager == null)
            ascensorManager = FindFirstObjectByType<AscensorManager>();
    }

    private void Update()
    {
        if (eventoEnCurso) return;

        timer += Time.deltaTime;
        if (timer < intervaloChequeo) return;

        timer = 0f;
        EvaluarEventos();
    }

    private void EvaluarEventos()
    {
        IntentarLanzarAlguno();
    }

    public bool IntentarLanzarEvento()
    {
        if (eventoEnCurso) return false;
        return IntentarLanzarAlguno();
    }

    // Cada evento válido tiene su oportunidad: si el elegido no pasa su tirada
    // de probabilidad, se prueba con los demás en vez de no hacer nada.
    private bool IntentarLanzarAlguno()
    {
        if (GameManager.Instance == null) return false;

        // No se lanzan eventos mientras se frena (ni 2 s después)
        if (BloqueoEventos.EventosBloqueados)
        {
            if (debugDetalle)
                Debug.Log("[Eventos] Bloqueados por el frenado");
            return false;
        }

        List<EventoEstadoConfig> candidatos = new List<EventoEstadoConfig>();

        foreach (var e in eventos)
        {
            if (PuedeOcurrir(e))
                candidatos.Add(e);
        }

        if (candidatos.Count == 0)
        {
            if (debugLogs)
                Debug.Log("No hay eventos de estado válidos en este momento");
            return false;
        }

        while (candidatos.Count > 0)
        {
            EventoEstadoConfig elegido = ElegirEvento(candidatos);
            float probabilidad = CalcularProbabilidad(elegido);

            if (UnityEngine.Random.value <= probabilidad)
            {
                LanzarEvento(elegido);
                return true;
            }

            if (debugLogs)
                Debug.Log($" {elegido.nombre}: no ocurrió (prob {probabilidad:P0})");

            candidatos.Remove(elegido);
        }

        return false;
    }

    public bool ForzarEvento(TipoSistema sistema)
    {
        if (eventoEnCurso) return false;

        EventoEstadoConfig config = null;

        // El tutorial pide el evento de la puerta: se prefiere la Puerta atascada
        // aunque haya otras entradas del sistema Puerta (por ejemplo el Pestillo).
        if (sistema == TipoSistema.Puerta)
        {
            config = eventos.Find(e => e.sistema == sistema
                                       && e.prefab != null
                                       && e.prefab.GetComponent<EventoPuertaAtascada>() != null);
        }

        if (config == null)
            config = eventos.Find(e => e.sistema == sistema);

        if (config == null) return false;

        LanzarEvento(config);
        return true;
    }

    private bool PuedeOcurrir(EventoEstadoConfig e)
    {
        float valor = ObtenerValorContador(e.sistema);
        bool contadorOk = valor <= e.umbral;
        bool ascensorOk = CumpleCondicionAscensor(e);

        if (debugDetalle)
        {
            Debug.Log($"[Eventos] {e.nombre}: {e.sistema} = {valor:F0} (umbral {e.umbral:F0}) -> {(contadorOk ? "OK" : "NO")}" +
                      $" | ascensor ({e.condicionAscensor}) -> {(ascensorOk ? "OK" : "NO")}" +
                      $" | prefab -> {(e.prefab != null ? "OK" : "FALTA")}");
        }

        return contadorOk && ascensorOk;
    }

    private bool CumpleCondicionAscensor(EventoEstadoConfig e)
    {
        if (e.condicionAscensor == CondicionAscensor.Cualquiera)
            return true;

        return AscensorArribaQuieto();
    }

    private bool AscensorArribaQuieto()
    {
        // Sin referencia al ascensor, no se bloquea nada
        if (ascensorManager == null) return true;

        if (ascensorManager.enMovimiento) return false;

        return !ascensorManager.EstaAbajo(indiceAscensorJugador);
    }

    private float CalcularProbabilidad(EventoEstadoConfig e)
    {
        float valor = ObtenerValorContador(e.sistema);
        float t = Mathf.InverseLerp(e.umbral, 0f, valor);
        return Mathf.Lerp(e.probabilidadBase, e.probabilidadMaxima, t);
    }

    private float ObtenerValorContador(TipoSistema sistema)
    {
        var data = GameManager.Instance.GameData;

        return sistema switch
        {
            TipoSistema.Limpieza => data.contadorLimpieza,
            TipoSistema.Polea => data.contadorGrasa,
            TipoSistema.Puerta => data.contadorPuerta,
            _ => 100f
        };
    }

    private EventoEstadoConfig ElegirEvento(List<EventoEstadoConfig> lista)
    {
        float pesoTotal = 0f;
        foreach (var e in lista) pesoTotal += e.peso;

        float r = UnityEngine.Random.Range(0f, pesoTotal);
        float acumulado = 0f;

        foreach (var e in lista)
        {
            acumulado += e.peso;
            if (r <= acumulado) return e;
        }

        return lista[0];
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
        // Durante 1 segundo tras terminar un evento, Espacio no arranca el ascensor
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