using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class ProcesoPartida : MonoBehaviour
{
    public static ProcesoPartida Instance { get; private set; }

    public enum Fase { Cargando, EsperandoCabinaAbajo, Lista }

    [Header("Referencias (si se deja vacío se busca solo)")]
    public AscensorManager ascensorManager;
    public DirectorEventos director;

    [Header("W")]
    [Tooltip("Pasajeros mínimos dentro de la cabina de arriba para poder pulsar W. 0 = el jugador decide libremente")]
    public int pasajerosMinimosParaAvisar = 0;

    [Header("Espera de la cabina de abajo (segundos, al azar entre los dos valores)")]
    public float esperaMin = 2f;
    public float esperaMax = 4f;

    [Header("Popularidad")]
    [Tooltip("Puntos de popularidad que se pierden al arrancar mal")]
    public float penalizacionPorArranqueIncorrecto = 8f;
    [Tooltip("Puntos que se recuperan al terminar un viaje sin errores")]
    public float recuperacionPorViajeLimpio = 2f;

    [Header("Mensajes de la TV")]
    public string mensajeAvisando = "Avisando a la cabina de abajo...";
    public string mensajeLista = "Cabina de abajo lista";
    public string mensajeSinW = "¡Frenos de emergencia! Arrancaste sin avisar a la cabina de abajo (W)";
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
        controls.Gameplay.AvisarCabina.performed += OnAvisarCabina;
    }

    private void OnEnable()
    {
        controls?.Enable();
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

    public bool IntentarPartir()
    {
        if (fase == Fase.Lista)
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

        if (ascensorIndex == IndiceCabinaArriba()) return true;
        return fase == Fase.Lista;
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