using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class EventoPestillo : EventoBase
{
    [Header("Mensajes (salen en la TV)")]
    public string mensajeInicial = "¡El pestillo de la puerta se trabó!";
    public string mensajeInstruccion = "Presiona {teclaAcelerar} cuando el anillo llegue al círculo";
    public Color colorMensaje = Color.yellow;

    [Header("Círculo")]
    [Tooltip("Radio del círculo en píxeles (en una pantalla de 1080 de alto)")]
    public float radioCirculo = 110f;
    [Tooltip("Qué tan grande empieza el anillo respecto al círculo (3 = el triple)")]
    public float escalaInicialAnillo = 3f;

    [Header("Ritmo")]
    [Tooltip("Segundos antes de que aparezca el círculo")]
    public float tiempoInicial = 0.6f;
    [Tooltip("Segundos que tarda el anillo en llegar al círculo")]
    public float tiempoAproximacion = 2f;

    [Header("Margen de acierto (segundos)")]
    [Tooltip("Antes o después del momento exacto, Espacio cuenta como acierto")]
    public float ventanaAcierto = 0.4f;
    [Tooltip("Un Espacio más temprano que esto se ignora; entre este valor y la ventana de acierto cuenta como fallo")]
    public float ventanaFallo = 0.9f;

    [Header("Intentos")]
    [Tooltip("Marcado: tras fallar el círculo vuelve a aparecer hasta que el jugador acierte")]
    public bool repetirHastaAcertar = true;
    [Tooltip("Solo si no se repite hasta acertar: 1 = un intento. Con 2 o más, el círculo vuelve a aparecer tras fallar")]
    public int intentosMaximos = 1;

    [Header("Arte (opcional)")]
    [Tooltip("Sprite del círculo relleno. Si se deja vacío se dibuja uno por código. Si el arte ya trae color, pon Color Disco en blanco")]
    public Sprite spriteDiscoArte;
    [Tooltip("Sprite del anillo que se cierra. Si se deja vacío se dibuja uno por código. Si el arte ya trae color, pon Color Anillo en blanco")]
    public Sprite spriteAnilloArte;

    [Header("Colores")]
    public Color colorDisco = new Color(0.45f, 0.35f, 0.25f, 0.9f);
    public Color colorBorde = Color.white;
    public Color colorAnillo = new Color(1f, 0.9f, 0.3f, 1f);
    public Color colorAcierto = new Color(0.3f, 0.9f, 0.3f, 0.9f);
    public Color colorFallo = new Color(0.9f, 0.25f, 0.25f, 0.9f);
    [Range(0f, 1f)] public float oscurecerFondo = 0.3f;

    [Header("Tiempo")]
    [Tooltip("Congela el juego (ascensor, reloj) mientras dura el evento")]
    public bool detenerTiempo = true;

    [Header("Resultado sobre el mantenimiento")]
    public float bonoSiResuelve = 5f;
    [Tooltip("Mantenimiento que se pierde en cada intento fallido")]
    public float dañoSiFalla = 10f;

    [Header("UI")]
    public int ordenCanvas = 100;

    [Header("---input---")]
    public InputActionAsset inputActions;
    public string actionMapName = "Gameplay";
    public string actionName = "MoverAscensor";
    private InputAction inputEspacio;

    private enum Estado { Esperando, Activo, Feedback, Fin }
    private Estado estado = Estado.Esperando;

    private RectTransform raiz;
    private CanvasGroup grupo;
    private Image disco;
    private RectTransform anillo;
    private Sprite spriteDisco;
    private Sprite spriteAnillo;
    private Texture2D texDisco;
    private Texture2D texAnillo;

    private float t = 0f;
    private float tProximoCiclo = 0f;
    private float spawn = 0f;
    private float llegada = 0f;
    private float tFeedbackFin = 0f;
    private int intentoActual = 1;
    private bool ultimoAcierto = false;
    private float radioPx = 110f;

    private bool minijuegoActivo = false;
    private bool tiempoDetenido = false;

    private const float DURACION_FADE_IN = 0.2f;
    private const float DURACION_FEEDBACK = 0.6f;
    private const float PAUSA_REINTENTO = 0.4f;

    private void Awake()
    {
        if (inputActions != null)
        {
            Controles.Aplicar(inputActions);
            inputEspacio = inputActions.FindActionMap(actionMapName)?.FindAction(actionName);

            if (inputEspacio == null)
                Debug.LogError($"No se encontró '{actionName}' en '{actionMapName}'");
        }
    }

    private void OnEnable()
    {
        if (inputEspacio != null)
            inputEspacio.Enable();
    }

    protected override void Start()
    {
        base.Start();

        Debug.Log("EventoPestillo iniciado");

        if (PanelMensajesMonitor.Instance != null)
            PanelMensajesMonitor.Instance.MostrarMensajeInmediato(mensajeInicial, colorMensaje);

        radioPx = radioCirculo * (Screen.height / 1080f);

        spriteDisco = spriteDiscoArte != null ? spriteDiscoArte : CrearSprite(256, 0f, out texDisco);
        spriteAnillo = spriteAnilloArte != null ? spriteAnilloArte : CrearSprite(256, 0.95f, out texAnillo);
        CrearUI();

        t = 0f;
        tProximoCiclo = tiempoInicial;
        intentoActual = 1;
        estado = Estado.Esperando;
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

        if (texDisco != null) { Destroy(spriteDisco); Destroy(texDisco); }
        if (texAnillo != null) { Destroy(spriteAnillo); Destroy(texAnillo); }
    }

    private Sprite CrearSprite(int tam, float radioInterno01, out Texture2D tex)
    {
        tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        float centro = (tam - 1) * 0.5f;
        float rExterior = tam * 0.5f - 1f;
        float rInterior = rExterior * radioInterno01;

        Color32[] pixeles = new Color32[tam * tam];
        for (int y = 0; y < tam; y++)
        {
            for (int x = 0; x < tam; x++)
            {
                float d = Mathf.Sqrt((x - centro) * (x - centro) + (y - centro) * (y - centro));
                float aExt = Mathf.Clamp01(rExterior - d + 0.5f);
                float aInt = radioInterno01 > 0f ? Mathf.Clamp01(d - rInterior + 0.5f) : 1f;
                byte alfa = (byte)(255f * aExt * aInt);
                pixeles[y * tam + x] = new Color32(255, 255, 255, alfa);
            }
        }

        tex.SetPixels32(pixeles);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, tam, tam), new Vector2(0.5f, 0.5f), 100f);
    }

    private void CrearUI()
    {
        GameObject canvasGO = new GameObject("CanvasPestillo", typeof(Canvas), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = ordenCanvas;

        GameObject fondoGO = new GameObject("Fondo", typeof(RectTransform), typeof(Image));
        fondoGO.transform.SetParent(canvasGO.transform, false);
        RectTransform fondoRT = fondoGO.GetComponent<RectTransform>();
        fondoRT.anchorMin = Vector2.zero;
        fondoRT.anchorMax = Vector2.one;
        fondoRT.offsetMin = Vector2.zero;
        fondoRT.offsetMax = Vector2.zero;

        Image fondo = fondoGO.GetComponent<Image>();
        fondo.color = new Color(0f, 0f, 0f, oscurecerFondo);
        fondo.raycastTarget = true;

        float diametro = radioPx * 2f;

        GameObject raizGO = new GameObject("Pestillo", typeof(RectTransform), typeof(CanvasGroup));
        raizGO.transform.SetParent(canvasGO.transform, false);
        raiz = raizGO.GetComponent<RectTransform>();
        raiz.anchorMin = raiz.anchorMax = new Vector2(0.5f, 0.5f);
        raiz.pivot = new Vector2(0.5f, 0.5f);
        raiz.anchoredPosition = Vector2.zero;
        raiz.sizeDelta = new Vector2(diametro, diametro);

        grupo = raizGO.GetComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.blocksRaycasts = false;
        grupo.interactable = false;

        disco = CrearImagen("Disco", raiz, spriteDisco, colorDisco, diametro);
        CrearImagen("Borde", raiz, spriteAnillo, colorBorde, diametro);
        anillo = CrearImagen("Anillo", raiz, spriteAnillo, colorAnillo, diametro * escalaInicialAnillo).rectTransform;
    }

    private Image CrearImagen(string nombre, RectTransform padre, Sprite sprite, Color color, float tam)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(padre, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(tam, tam);

        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;

        return img;
    }

    protected override void Update()
    {
        if (!minijuegoActivo || PausaManager.juegoPausado) return;

        t += Time.unscaledDeltaTime;

        switch (estado)
        {
            case Estado.Esperando:
                if (t >= tProximoCiclo)
                    IniciarCiclo();
                break;

            case Estado.Activo:
                ActualizarCicloActivo();
                break;

            case Estado.Feedback:
                ActualizarFeedback();
                break;
        }
    }

    private void IniciarCiclo()
    {
        spawn = t;
        llegada = t + tiempoAproximacion;

        disco.color = colorDisco;
        anillo.gameObject.SetActive(true);
        grupo.alpha = 0f;

        estado = Estado.Activo;
        ActualizarTextoProgreso();
    }

    private void ActualizarCicloActivo()
    {
        float diametro = radioPx * 2f;
        float edad = t - spawn;
        float progreso = Mathf.Clamp01(edad / tiempoAproximacion);
        float escala = Mathf.Lerp(escalaInicialAnillo, 1f, progreso);

        anillo.sizeDelta = new Vector2(diametro * escala, diametro * escala);
        grupo.alpha = Mathf.Clamp01(edad / DURACION_FADE_IN);

        if (inputEspacio != null && inputEspacio.WasPressedThisFrame())
        {
            float diferencia = t - llegada;

            if (Mathf.Abs(diferencia) <= ventanaAcierto)
            {
                Sonidos.ReproducirQte(b => b.pestilloAcierto);
                ResolverCiclo(true);
                return;
            }
            else if (diferencia < -ventanaAcierto && diferencia >= -ventanaFallo)
            {
                Sonidos.ReproducirQte(b => b.pestilloFallo);
                ResolverCiclo(false);
                return;
            }
        }

        if (t > llegada + ventanaAcierto)
            ResolverCiclo(false);
    }

    private void ResolverCiclo(bool acierto)
    {
        ultimoAcierto = acierto;

        if (!acierto && GameManager.Instance != null)
            GameManager.Instance.ModificarPolea(-dañoSiFalla);

        disco.color = acierto ? colorAcierto : colorFallo;
        anillo.gameObject.SetActive(false);
        grupo.alpha = 1f;

        tFeedbackFin = t + DURACION_FEEDBACK;
        estado = Estado.Feedback;
    }

    private void ActualizarFeedback()
    {
        float restante = tFeedbackFin - t;
        grupo.alpha = Mathf.Clamp01(restante / DURACION_FEEDBACK * 2f);

        if (restante > 0f) return;

        bool averiado = GameManager.Instance != null && GameManager.Instance.GameData.ascensorAveriado;
        bool otroIntento = repetirHastaAcertar || intentoActual < intentosMaximos;

        if (ultimoAcierto || averiado || !otroIntento)
        {
            FinalizarEvento();
        }
        else
        {
            intentoActual++;
            tProximoCiclo = t + PAUSA_REINTENTO;
            grupo.alpha = 0f;
            estado = Estado.Esperando;
            ActualizarTextoProgreso();
        }
    }

    private void ActualizarTextoProgreso()
    {
        if (PanelMensajesMonitor.Instance == null) return;

        string texto = $"{mensajeInstruccion}\n";
        if (repetirHastaAcertar)
        {
            if (intentoActual > 1) texto += $"Intento: {intentoActual}";
        }
        else if (intentosMaximos > 1)
            texto += $"Intento: {intentoActual}/{intentosMaximos}";

        PanelMensajesMonitor.Instance.MostrarMensajeInmediato(texto, colorMensaje);
    }

    private void FinalizarEvento()
    {
        estado = Estado.Fin;
        minijuegoActivo = false;
        RestaurarTiempo();

        if (ultimoAcierto && GameManager.Instance != null)
            GameManager.Instance.ModificarPolea(bonoSiResuelve);

        if (PanelMensajesMonitor.Instance != null)
        {
            if (ultimoAcierto)
                PanelMensajesMonitor.Instance.MostrarMensaje("¡Pestillo liberado!", Color.green, 2f);
            else
                PanelMensajesMonitor.Instance.MostrarMensaje("¡No lograste liberar el pestillo!", Color.red, 3f);
        }

        Terminar();
    }
}