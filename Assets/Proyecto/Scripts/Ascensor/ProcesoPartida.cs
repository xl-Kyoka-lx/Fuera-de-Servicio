using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class ProcesoPartida : MonoBehaviour
{
    public static ProcesoPartida Instance { get; private set; }

    public enum Fase
    {
        Cargando,
        EsperandoCabinaAbajo,
        Lista,
        // La cabina de abajo cobró y está cerrando sus puertas (todavía no toca el timbre)
        AbajoCerrando,
        // La cabina de abajo tocó el timbre por su cuenta: el jugador decide cuándo partir
        AbajoLlamo
    }

    [Header("Referencias (si se deja vacío se busca solo)")]
    public AscensorManager ascensorManager;
    public DirectorEventos director;

    [Header("W")]
    [Tooltip("Pasajeros mínimos dentro de la cabina de arriba para poder pulsar W. 0 = el jugador decide libremente")]
    public int pasajerosMinimosParaAvisar = 0;

    [Header("Espera de la cabina de abajo (segundos, al azar entre los dos valores)")]
    public float esperaMin = 2f;
    public float esperaMax = 4f;

    [Header("Timbre de la cabina de abajo (avisa sin que el jugador pulse W)")]
    public bool senalDeAbajoActiva = true;
    [Range(0f, 1f)]
    [Tooltip("Probabilidad, en cada parada, de que la cabina de abajo toque el timbre por su cuenta")]
    public float probabilidadSenalDeAbajo = 0.3f;
    [Tooltip("Segundos después de llegar en que suena el timbre (al azar entre los dos valores)")]
    public float senalDespuesMin = 6f;
    public float senalDespuesMax = 12f;
    [Tooltip("Segundos entre que abajo cobra los pasajes y suena el timbre")]
    public float segundosEntrePagoYTimbre = 2f;
    [Tooltip("Pasajeros mínimos dentro de la cabina de abajo para que toque el timbre")]
    public int pasajerosMinimosParaSenal = 1;
    public string mensajeCobroAbajo = "Cabina de abajo: +${dinero} ({pasajeros} pasajeros)";
    public string mensajeAbajoLlena = "Cabina de abajo llena";
    public string mensajeAbajoCerrada = "Cabina de abajo cerrada";
    [Tooltip("Texto del indicador de sonido (accesibilidad)")]
    public string indicadorTimbre = "Timbre de la cabina de abajo";

    [Header("Popularidad")]
    [Tooltip("Puntos de popularidad que se pierden al arrancar mal")]
    public float penalizacionPorArranqueIncorrecto = 8f;
    [Tooltip("Puntos que se recuperan al terminar un viaje sin errores")]
    public float recuperacionPorViajeLimpio = 2f;

    [Header("Mensajes de la TV")]
    public string mensajeAvisando = "Avisando a la cabina de abajo...";
    public string mensajeLista = "Cabina de abajo lista";
    public string mensajeSinW = "¡Frenos de emergencia! Arrancaste sin avisar a la cabina de abajo ({teclaTimbre})";
    public string mensajeNoLista = "¡Frenos de emergencia! La cabina de abajo aún no está lista";
    public string mensajeFaltanPasajeros = "Todavía no hay pasajeros suficientes para avisar";
    public float duracionMensajeError = 2.5f;
    public Color colorAviso = new Color(1f, 0.85f, 0.3f);
    public Color colorLista = new Color(0.4f, 1f, 0.5f);
    public Color colorError = new Color(1f, 0.35f, 0.35f);

    [Header("Estado (solo lectura)")]
    public Fase fase = Fase.Cargando;

    public event Action OnWPulsada;
    public event Action OnCabinaLista;
    public event Action OnLlegada;

    public static event Action<string> OnErrorDeProcedimiento;

    private AscensorControls controls;
    private bool estabaEnMovimiento;
    private bool habiaEventoEnCurso;
    private float esperaRestante;
    private float mensajeTemporalHasta;
    private bool mensajePropioEnPantalla;
    private bool huboErrorEnEsteCiclo;
    private bool senalProgramada;
    private float senalRestante;
    private bool senalForzada;

    [Obsolete]
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        if (ascensorManager == null) ascensorManager = GetComponent<AscensorManager>();
        if (ascensorManager == null) ascensorManager = FindFirstObjectByType<AscensorManager>();
        if (director == null) director = FindFirstObjectByType<DirectorEventos>();

        controls = new AscensorControls();
        Controles.Aplicar(controls.asset);
        controls.Gameplay.AvisarCabina.performed += OnAvisarCabina;
    }

    private void OnEnable()
    {
        controls?.Enable();
    }

    private void Start()
    {
        ProgramarSenalDeAbajo();
    }

    private void OnDisable()
    {
        controls?.Disable();
    }

    private void OnDestroy()
    {
        if (controls != null)
        {
            controls.Gameplay.AvisarCabina.performed -= OnAvisarCabina;
            controls.Dispose();
            controls = null;
        }

        if (Instance == this) Instance = null;
    }

    private void OnAvisarCabina(InputAction.CallbackContext ctx)
    {
        PulsarW();
    }

    private void Update()
    {
        if (ascensorManager == null) return;

        bool enMovimiento = ascensorManager.enMovimiento;

        if (enMovimiento && !estabaEnMovimiento)
        {
            LimpiarMensajePropio();
        }

        if (!enMovimiento && estabaEnMovimiento)
        {
            RecuperarPopularidadSiViajeLimpio();
            huboErrorEnEsteCiclo = false;

            fase = Fase.Cargando;
            esperaRestante = 0f;
            mensajeTemporalHasta = 0f;
            ProgramarSenalDeAbajo();
            OnLlegada?.Invoke();
        }

        estabaEnMovimiento = enMovimiento;

        if (enMovimiento) return;

        bool hayEvento = GameManager.Instance != null && GameManager.Instance.GameData.eventoOcurriendo;

        if (fase == Fase.EsperandoCabinaAbajo && !hayEvento && Time.timeScale > 0f)
        {
            esperaRestante -= Time.deltaTime;
            if (esperaRestante <= 0f)
            {
                fase = Fase.Lista;
                MostrarMensajeDeFase(false);
                OnCabinaLista?.Invoke();
            }
        }

        ProcesarSenalDeAbajo(hayEvento);

        if (habiaEventoEnCurso && !hayEvento)
        {
            MostrarMensajeDeFase(false);
        }
        habiaEventoEnCurso = hayEvento;

        if (mensajeTemporalHasta > 0f && Time.unscaledTime >= mensajeTemporalHasta)
        {
            mensajeTemporalHasta = 0f;
            MostrarMensajeDeFase(true);
        }
    }

    public void PulsarW()
    {
        if (ascensorManager == null) return;
        if (fase != Fase.Cargando) return;
        if (ascensorManager.enMovimiento) return;
        if (Time.timeScale == 0f) return;
        if (TutorialManager.ViajeBloqueado) return;

        if (DialogoManager.Instance != null && DialogoManager.Instance.HayDialogoActivo()) return;

        if (GameManager.Instance != null)
        {
            var data = GameManager.Instance.GameData;
            if (data.eventoOcurriendo || data.ascensorAveriado) return;
        }

        int arriba = IndiceCabinaArriba();
        if (arriba < 0) return;

        if (ascensorManager.ascensores[arriba].pasajerosDentro.Count < pasajerosMinimosParaAvisar)
        {
            MostrarTemporal(mensajeFaltanPasajeros, colorAviso);
            return;
        }

        fase = Fase.EsperandoCabinaAbajo;
        esperaRestante = UnityEngine.Random.Range(esperaMin, esperaMax);
        mensajeTemporalHasta = 0f;
        MostrarMensajeDeFase(false);

        OnWPulsada?.Invoke();
    }

    private void ProgramarSenalDeAbajo()
    {
        senalForzada = false;
        senalProgramada = senalDeAbajoActiva && UnityEngine.Random.value < probabilidadSenalDeAbajo;
        senalRestante = UnityEngine.Random.Range(senalDespuesMin, senalDespuesMax);
    }

    /// <summary>
    /// Para pruebas: hace que la cabina de abajo toque el timbre ahora mismo,
    /// sin probabilidad y aunque no haya pasajeros abajo. Devuelve el motivo si no se pudo.
    /// </summary>
    public string ForzarSenalDeAbajo()
    {
        if (ascensorManager == null) return "no hay AscensorManager";
        if (ascensorManager.enMovimiento) return "el ascensor está en movimiento";
        if (fase != Fase.Cargando) return $"la fase actual es {fase} (tiene que ser Cargando: espera a que llegue y no pulses W)";
        if (IndiceCabinaAbajo() < 0) return "no hay ninguna cabina abajo";

        senalProgramada = true;
        senalRestante = 0f;
        senalForzada = true;
        return null;
    }

    private void ProcesarSenalDeAbajo(bool hayEvento)
    {
        if (fase == Fase.AbajoCerrando)
        {
            if (hayEvento || Time.timeScale <= 0f) return;

            esperaRestante -= Time.deltaTime;
            if (esperaRestante > 0f) return;

            fase = Fase.AbajoLlamo;
            TocarTimbre();
            return;
        }

        if (!senalProgramada || fase != Fase.Cargando) return;
        if (hayEvento || Time.timeScale <= 0f) return;
        if (TutorialManager.EnCurso && !senalForzada) return;
        if (DialogoManager.Instance != null && DialogoManager.Instance.HayDialogoActivo()) return;
        if (DecisionUI.Abierta) return;
        if (GameManager.Instance != null && GameManager.Instance.GameData.ascensorAveriado) return;

        senalRestante -= Time.deltaTime;
        if (senalRestante > 0f) return;

        int abajo = IndiceCabinaAbajo();
        if (abajo < 0) return;

        var cabina = ascensorManager.ascensores[abajo];
        if (!senalForzada && cabina.pasajerosDentro.Count < Mathf.Max(0, pasajerosMinimosParaSenal))
        {
            // Todavía no sube nadie abajo: se vuelve a revisar en un rato
            senalRestante = 2f;
            return;
        }

        senalProgramada = false;
        senalForzada = false;
        CobrarCabinaDeAbajo(cabina);

        fase = Fase.AbajoCerrando;
        esperaRestante = Mathf.Max(0f, segundosEntrePagoYTimbre);
        mensajeTemporalHasta = 0f;
    }

    private void CobrarCabinaDeAbajo(AscensorManager.AscensorData cabina)
    {
        int total = 0;
        int pasajeros = 0;

        foreach (var p in cabina.pasajerosDentro)
        {
            if (p == null || p.PagoPorAdelantado) continue;
            total += p.CobrarPorAdelantado();
            pasajeros++;
        }

        if (total > 0 && GameManager.Instance != null)
            GameManager.Instance.SumarDinero(total);

        if (pasajeros > 0)
        {
            string texto = mensajeCobroAbajo
                .Replace("{dinero}", total.ToString())
                .Replace("{pasajeros}", pasajeros.ToString());
            MostrarPropio(texto, colorLista);
        }
    }

    private void TocarTimbre()
    {
        var b = Sonidos.Biblioteca;
        if (b != null) Sonidos.Reproducir(b.timbreCabinaAbajo, b.volumenTimbre * Configuracion.VolumenAscensor);

        IndicadoresSonido.Mostrar(indicadorTimbre, IndicadoresSonido.Lado.Frente, true);

        MostrarMensajeDeFase(false);
        OnCabinaLista?.Invoke();
    }

    public bool IntentarPartir()
    {
        if (fase == Fase.Lista || fase == Fase.AbajoLlamo)
        {
            if (TutorialManager.Instance != null && TutorialManager.Instance.IntentarEventoAlArrancar()) return false;

            if (director != null && director.IntentarEventoAlArrancar()) return false;

            return true;
        }

        string motivo = fase == Fase.Cargando ? mensajeSinW : mensajeNoLista;
        MostrarTemporal(motivo, colorError);

        BloqueoEventos.BloquearArranque(duracionMensajeError);

        huboErrorEnEsteCiclo = true;

        if (!TutorialManager.EnCurso)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.ModificarPopularidad(-penalizacionPorArranqueIncorrecto);

            OnErrorDeProcedimiento?.Invoke("arranque");
        }

        return false;
    }

    private void RecuperarPopularidadSiViajeLimpio()
    {
        if (huboErrorEnEsteCiclo || TutorialManager.EnCurso) return;
        if (GameManager.Instance == null) return;

        GameManager.Instance.ModificarPopularidad(recuperacionPorViajeLimpio);
    }

    public bool PuertaCerrada(int ascensorIndex)
    {
        if (ascensorManager == null) return false;
        if (fase == Fase.Cargando) return false;

        // Cuando avisa la cabina de abajo, solo se cierra la de abajo: arriba se puede seguir cargando
        if (fase == Fase.AbajoCerrando || fase == Fase.AbajoLlamo)
            return ascensorIndex != IndiceCabinaArriba();

        if (ascensorIndex == IndiceCabinaArriba()) return true;
        return fase == Fase.Lista;
    }

    public int IndiceCabinaAbajo()
    {
        if (ascensorManager == null) return -1;

        for (int i = 0; i < ascensorManager.ascensores.Count; i++)
        {
            if (ascensorManager.ascensores[i].estaAbajo) return i;
        }
        return -1;
    }

    public int IndiceCabinaArriba()
    {
        if (ascensorManager == null) return -1;

        for (int i = 0; i < ascensorManager.ascensores.Count; i++)
        {
            if (!ascensorManager.ascensores[i].estaAbajo) return i;
        }
        return -1;
    }

    private void MostrarMensajeDeFase(bool limpiarSiCargando)
    {
        switch (fase)
        {
            case Fase.EsperandoCabinaAbajo:
                MostrarPropio(mensajeAvisando, colorAviso);
                break;
            case Fase.Lista:
                MostrarPropio(mensajeLista, colorLista);
                break;
            case Fase.AbajoLlamo:
                int abajo = IndiceCabinaAbajo();
                bool llena = abajo >= 0 && !ascensorManager.ascensores[abajo].HayEspacio;
                MostrarPropio(llena ? mensajeAbajoLlena : mensajeAbajoCerrada, colorLista);
                break;
            default:
                if (limpiarSiCargando) LimpiarMensajePropio();
                break;
        }
    }

    private void MostrarTemporal(string texto, Color color)
    {
        MostrarPropio(texto, color);
        mensajeTemporalHasta = Time.unscaledTime + duracionMensajeError;
    }

    private void MostrarPropio(string texto, Color color)
    {
        if (PanelMensajesMonitor.Instance == null) return;

        PanelMensajesMonitor.Instance.MostrarMensajeInmediato(texto, color);
        mensajePropioEnPantalla = true;
    }

    private void LimpiarMensajePropio()
    {
        if (!mensajePropioEnPantalla) return;
        mensajePropioEnPantalla = false;
        mensajeTemporalHasta = 0f;

        if (PanelMensajesMonitor.Instance != null)
            PanelMensajesMonitor.Instance.MostrarMensajeInmediato("", Color.white);
    }
}