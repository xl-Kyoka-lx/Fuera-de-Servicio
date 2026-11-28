using UnityEngine;
using UnityEngine.InputSystem;

public class EventoPoleaTrabada : EventoBase
{
    [Header("Mensajes (salen en la TV)")]
    public string mensajeInicial = "¡La polea se trabó!";
    public string mensajeInstruccion = "Presiona {teclaAcelerar} al ritmo del brillo";
    public Color colorMensaje = Color.yellow;

    [Header("Ritmo")]
    [Tooltip("Segundos entre un brillo y el siguiente")]
    public float periodo = 1f;
    [Tooltip("Margen (en segundos) antes o después del brillo para que cuente como acierto")]
    public float tolerancia = 0.2f;
    [Tooltip("Cuánto dura el brillo alrededor de cada pulso, en segundos")]
    public float anchoBrillo = 0.35f;

    [Header("Reglas")]
    public int aciertosRequeridos = 5;
    public int erroresMaximos = 3;
    public float tiempoLimite = 15f;

    [Header("Pantalla oscura")]
    [Tooltip("CanvasGroup de una Image negra a pantalla completa, hija de este prefab")]
    public CanvasGroup overlay;
    [Range(0f, 1f)] public float alphaOscuro = 0.85f;

    [Header("Tiempo")]
    [Tooltip("Congela el juego (ascensor, reloj) mientras dura el evento")]
    public bool detenerTiempo = true;

    [Header("Resultado sobre la polea")]
    public float bonoSiResuelve = 5f;
    public float dañoSiFalla = 10f;

    [Header("---input---")]
    public InputActionAsset inputActions;
    public string actionMapName = "Gameplay";
    public string actionName = "MoverAscensor";
    private InputAction inputResolver;

    private float tiempo = 0f;
    private float tiempoRestante = 0f;
    private int aciertos = 0;
    private int errores = 0;
    private int ultimoPulsoAcertado = -1;
    private bool minijuegoActivo = false;
    private bool yaResuelto = false;
    private bool tiempoDetenido = false;

    private void Awake()
    {
        if (inputActions != null)
        {
            Controles.Aplicar(inputActions);
            inputResolver = inputActions.FindActionMap(actionMapName)?.FindAction(actionName);

            if (inputResolver == null)
                Debug.LogError($"No se encontró '{actionName}' en '{actionMapName}'");
        }
    }

    private void OnEnable()
    {
        if (inputResolver != null)
        {
            inputResolver.Enable();
            inputResolver.performed += OnResolverPerformed;
        }
    }

    private void OnDisable()
    {
        if (inputResolver != null)
        {
            inputResolver.performed -= OnResolverPerformed;
        }
    }

    protected override void Start()
    {
        base.Start();

        Debug.Log("EventoPoleaTrabada iniciado");

        if (GameManager.Instance != null && GameManager.Instance.GameData.ascensorEnMovimiento)
            Sonidos.ReproducirQte(b => b.ascensorDetenido);

        if (PanelMensajesMonitor.Instance != null)
            PanelMensajesMonitor.Instance.MostrarMensajeInmediato(mensajeInicial, colorMensaje);

        tiempo = 0f;
        tiempoRestante = tiempoLimite;
        aciertos = 0;
        errores = 0;
        minijuegoActivo = true;

        if (detenerTiempo)
        {
            PausaJuego.Pedir(this);
            tiempoDetenido = true;
        }

        ActualizarTextoProgreso();
    }

    private void RestaurarTiempo()
    {
        if (!tiempoDetenido) return;
        tiempoDetenido = false;
        PausaJuego.Liberar(this);
    }

    private void OnDestroy()
    {
        RestaurarTiempo();
    }

    protected override void Update()
    {
        if (!minijuegoActivo || PausaManager.juegoPausado) return;

        tiempo += Time.unscaledDeltaTime;
        tiempoRestante -= Time.unscaledDeltaTime;

        ActualizarOverlay();
        ActualizarTextoProgreso();

        if (tiempoRestante <= 0f)
            FallarMinijuego();
    }

    private int PulsoMasCercano()
    {
        return Mathf.Max(1, Mathf.RoundToInt(tiempo / periodo));
    }

    private void ActualizarOverlay()
    {
        if (overlay == null) return;

        int pulso = PulsoMasCercano();
        float distancia = Mathf.Abs(tiempo - pulso * periodo);
        float brillo = 1f - Mathf.Clamp01(distancia / anchoBrillo);

        overlay.alpha = Mathf.Lerp(alphaOscuro, 0f, brillo);
    }

    private void OnResolverPerformed(InputAction.CallbackContext context)
    {
        if (!minijuegoActivo || yaResuelto) return;

        int pulso = PulsoMasCercano();
        float distancia = Mathf.Abs(tiempo - pulso * periodo);

        if (distancia <= tolerancia)
        {
            if (pulso == ultimoPulsoAcertado) return;

            ultimoPulsoAcertado = pulso;
            aciertos++;
            ActualizarTextoProgreso();

            if (aciertos >= aciertosRequeridos)
                ResolverMinijuego();
        }
        else
        {
            errores++;
            ActualizarTextoProgreso();

            if (errores >= erroresMaximos)
                FallarMinijuego();
        }
    }

    private void ActualizarTextoProgreso()
    {
        if (PanelMensajesMonitor.Instance == null) return;

        string texto = $"{mensajeInstruccion}\n";
        texto += $"Aciertos: {aciertos}/{aciertosRequeridos}\n";
        texto += $"Errores: {errores}/{erroresMaximos}\n";
        texto += $"Tiempo: {Mathf.Max(0, tiempoRestante):F1}s";

        PanelMensajesMonitor.Instance.MostrarMensajeInmediato(texto, colorMensaje);
    }

    private void ResolverMinijuego()
    {
        if (yaResuelto) return;
        yaResuelto = true;
        minijuegoActivo = false;
        RestaurarTiempo();

        if (GameManager.Instance != null)
            GameManager.Instance.ModificarPolea(bonoSiResuelve);

        Sonidos.ReproducirQte(b => b.poleaDestrabada);

        if (PanelMensajesMonitor.Instance != null)
            PanelMensajesMonitor.Instance.MostrarMensaje("¡Polea liberada!", Color.green, 2f);

        Terminar();
    }

    private void FallarMinijuego()
    {
        if (yaResuelto) return;
        yaResuelto = true;
        minijuegoActivo = false;
        RestaurarTiempo();

        if (GameManager.Instance != null)
            GameManager.Instance.ModificarPolea(-dañoSiFalla);

        if (PanelMensajesMonitor.Instance != null)
            PanelMensajesMonitor.Instance.MostrarMensaje("¡No lograste liberar la polea!", Color.red, 3f);

        Terminar();
    }
}