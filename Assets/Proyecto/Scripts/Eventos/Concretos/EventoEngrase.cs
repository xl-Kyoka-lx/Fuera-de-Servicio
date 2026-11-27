using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class EventoEngrase : EventoBase
{
    [Header("Mensajes (salen en la TV)")]
    public string mensajeInicial = "¡Sobrecalentamiento! Falta grasa en la polea";
    public string mensajeInstruccion = "Mantén Espacio para subir el cuadrado y sigue la línea";
    public Color colorMensaje = Color.yellow;

    [Header("UI de la barra (hijos del prefab)")]
    public RectTransform barra;
    public RectTransform cuadrado;
    public RectTransform linea;
    public Image imagenCuadrado;
    public Color colorSinContacto = Color.white;
    public Color colorConContacto = Color.green;

    [Header("Reglas")]
    [Tooltip("Segundos acumulados de contacto necesarios")]
    public float tiempoObjetivo = 10f;
    public float tiempoLimite = 30f;

    [Header("Movimiento del cuadrado")]
    public float aceleracionSubida = 900f;
    public float gravedad = 700f;
    public float velocidadMaxima = 350f;

    [Header("Movimiento de la línea")]
    public float velocidadLinea = 120f;
    [Tooltip("Cada cuántos segundos cambia de destino como máximo")]
    public float cambioDestinoMax = 2f;

    [Header("Tiempo")]
    [Tooltip("Congela el juego (ascensor, reloj) mientras dura el evento")]
    public bool detenerTiempo = true;

    [Header("Resultado sobre la polea")]
    public float bonoSiResuelve = 20f;
    public float dañoSiFalla = 5f;

    [Header("---input---")]
    public InputActionAsset inputActions;
    public string actionMapName = "Gameplay";
    public string actionName = "MoverAscensor";
    private InputAction inputSubir;

    private float posCuadrado;
    private float velCuadrado;
    private float posLinea;
    private float destinoLinea;
    private float timerDestino;
    private float minY;
    private float maxY;

    private float tiempoContacto = 0f;
    private float tiempoRestante = 0f;
    private bool minijuegoActivo = false;
    private bool yaResuelto = false;
    private bool tiempoDetenido = false;

    private void Awake()
    {
        if (inputActions != null)
        {
            inputSubir = inputActions.FindActionMap(actionMapName)?.FindAction(actionName);

            if (inputSubir == null)
                Debug.LogError($"No se encontró '{actionName}' en '{actionMapName}'");
        }
    }

    private void OnEnable()
    {
        if (inputSubir != null)
            inputSubir.Enable();
    }

    protected override void Start()
    {
        base.Start();

        Debug.Log("EventoEngrase iniciado");

        if (PanelMensajesMonitor.Instance != null)
            PanelMensajesMonitor.Instance.MostrarMensajeInmediato(mensajeInicial, colorMensaje);

        if (barra == null || cuadrado == null || linea == null)
        {
            Debug.LogError("EventoEngrase: faltan referencias de la UI (barra, cuadrado o línea)");
            Terminar();
            return;
        }

        float altura = barra.rect.height;
        minY = cuadrado.rect.height * 0.5f;
        maxY = Mathf.Max(minY, altura - minY);

        posCuadrado = minY;
        velCuadrado = 0f;
        posLinea = Mathf.Lerp(minY, maxY, 0.5f);
        ElegirDestinoLinea();

        tiempoContacto = 0f;
        tiempoRestante = tiempoLimite;
        minijuegoActivo = true;

        if (detenerTiempo)
        {
            PausaJuego.Pedir(this);
            tiempoDetenido = true;
        }

        AplicarPosiciones();
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
        if (!minijuegoActivo) return;

        float dt = Time.unscaledDeltaTime;

        tiempoRestante -= dt;

        MoverCuadrado(dt);
        MoverLinea(dt);
        AplicarPosiciones();

        bool contacto = HayContacto();
        if (contacto)
            tiempoContacto += dt;

        if (imagenCuadrado != null)
            imagenCuadrado.color = contacto ? colorConContacto : colorSinContacto;

        ActualizarTextoProgreso();

        if (tiempoContacto >= tiempoObjetivo)
            ResolverMinijuego();
        else if (tiempoRestante <= 0f)
            FallarMinijuego();
    }

    private void MoverCuadrado(float dt)
    {
        bool presionando = inputSubir != null && inputSubir.IsPressed();
        SonidoEngrase(true);

        velCuadrado += (presionando ? aceleracionSubida : -gravedad) * dt;
        velCuadrado = Mathf.Clamp(velCuadrado, -velocidadMaxima, velocidadMaxima);

        posCuadrado += velCuadrado * dt;

        if (posCuadrado <= minY)
        {
            posCuadrado = minY;
            velCuadrado = 0f;
        }
        else if (posCuadrado >= maxY)
        {
            posCuadrado = maxY;
            velCuadrado = 0f;
        }
    }

    private void MoverLinea(float dt)
    {
        posLinea = Mathf.MoveTowards(posLinea, destinoLinea, velocidadLinea * dt);
        timerDestino -= dt;

        if (Mathf.Approximately(posLinea, destinoLinea) || timerDestino <= 0f)
            ElegirDestinoLinea();
    }

    private void ElegirDestinoLinea()
    {
        destinoLinea = Random.Range(minY, maxY);
        timerDestino = Random.Range(cambioDestinoMax * 0.5f, cambioDestinoMax);
    }

    private bool HayContacto()
    {
        return Mathf.Abs(posCuadrado - posLinea) <= cuadrado.rect.height * 0.5f;
    }

    private void AplicarPosiciones()
    {
        cuadrado.anchoredPosition = new Vector2(cuadrado.anchoredPosition.x, posCuadrado);
        linea.anchoredPosition = new Vector2(linea.anchoredPosition.x, posLinea);
    }

    private void ActualizarTextoProgreso()
    {
        if (PanelMensajesMonitor.Instance == null) return;

        string texto = $"{mensajeInstruccion}\n";
        texto += $"Engrase: {tiempoContacto:F1}/{tiempoObjetivo:F0}s\n";
        texto += $"Tiempo: {Mathf.Max(0, tiempoRestante):F1}s";

        PanelMensajesMonitor.Instance.MostrarMensajeInmediato(texto, colorMensaje);
    }

    private AudioSource fuenteEngrase;

    private void SonidoEngrase(bool sonar)
    {
        if (fuenteEngrase == null)
        {
            var b = Sonidos.Biblioteca;
            if (b == null || b.engrasadoPolea == null || !sonar) return;
            fuenteEngrase = gameObject.AddComponent<AudioSource>();
            fuenteEngrase.clip = b.engrasadoPolea;
            fuenteEngrase.loop = true;
            fuenteEngrase.volume = b.volumenQte;
            fuenteEngrase.playOnAwake = false;
        }

        if (sonar && !fuenteEngrase.isPlaying)
        {
            if (fuenteEngrase.time > 0f) fuenteEngrase.UnPause();
            else fuenteEngrase.Play();
        }
        else if (!sonar && fuenteEngrase.isPlaying)
        {
            fuenteEngrase.Pause();
        }
    }

    private void ResolverMinijuego()
    {
        if (yaResuelto) return;
        yaResuelto = true;
        SonidoEngrase(false);
        minijuegoActivo = false;
        RestaurarTiempo();

        if (GameManager.Instance != null)
            GameManager.Instance.ModificarPolea(bonoSiResuelve);

        if (PanelMensajesMonitor.Instance != null)
            PanelMensajesMonitor.Instance.MostrarMensaje("¡Polea engrasada!", Color.green, 2f);

        Terminar();
    }

    private void FallarMinijuego()
    {
        if (yaResuelto) return;
        yaResuelto = true;
        SonidoEngrase(false);
        minijuegoActivo = false;
        RestaurarTiempo();

        if (GameManager.Instance != null)
            GameManager.Instance.ModificarPolea(-dañoSiFalla);

        if (PanelMensajesMonitor.Instance != null)
            PanelMensajesMonitor.Instance.MostrarMensaje("¡No alcanzaste a engrasar la polea!", Color.red, 3f);

        Terminar();
    }
}