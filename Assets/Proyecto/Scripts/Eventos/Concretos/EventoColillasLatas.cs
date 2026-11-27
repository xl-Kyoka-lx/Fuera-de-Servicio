using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class EventoColillasLatas : EventoBase
{
    [Header("Mensajes (salen en la TV)")]
    public string mensajeInicial = "¡Hay basura en la cabina!";
    public string mensajeInstruccion = "Haz clic en cada círculo cuando el anillo lo alcance";
    public Color colorMensaje = Color.yellow;

    [Header("Círculos")]
    public int numeroCirculos = 5;
    [Tooltip("Radio del círculo en píxeles (en una pantalla de 1080 de alto)")]
    public float radioCirculo = 55f;
    [Tooltip("Qué tan grande empieza el anillo respecto al círculo (3 = el triple)")]
    public float escalaInicialAnillo = 3f;
    [Tooltip("Margen respecto a los bordes de la pantalla, en píxeles")]
    public float margenPantalla = 160f;

    [Header("Ritmo")]
    [Tooltip("Segundos antes de que aparezca el primer círculo")]
    public float tiempoInicial = 0.6f;
    [Tooltip("Segundos entre la aparición de un círculo y el siguiente")]
    public float intervaloAparicion = 0.8f;
    [Tooltip("Segundos que tarda el anillo en llegar al círculo")]
    public float tiempoAproximacion = 1.3f;

    [Header("Margen de acierto (segundos)")]
    [Tooltip("Antes o después del momento exacto, el clic cuenta como acierto")]
    public float ventanaAcierto = 0.25f;
    [Tooltip("Un clic más temprano que esto se ignora; entre este valor y la ventana de acierto cuenta como fallo")]
    public float ventanaFallo = 0.5f;

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

    [Header("Resultado sobre la limpieza")]
    public int aciertosRequeridos = 3;
    public float limpiezaPorAcierto = 4f;
    public float limpiezaPorFallo = 4f;

    [Header("UI")]
    public int ordenCanvas = 100;

    private class CirculoOsu
    {
        public RectTransform raiz;
        public CanvasGroup grupo;
        public Image disco;
        public RectTransform anillo;
        public Vector2 pos;
        public float spawn;
        public float llegada;
        public bool resuelto;
        public float tResuelto;
    }

    private readonly List<CirculoOsu> circulos = new List<CirculoOsu>();
    private RectTransform contenedor;
    private Sprite spriteDisco;
    private Sprite spriteAnillo;
    private Texture2D texDisco;
    private Texture2D texAnillo;

    private float t = 0f;
    private int generados = 0;
    private int resueltos = 0;
    private int aciertos = 0;
    private int fallos = 0;
    private float escalaUI = 1f;
    private float radioPx = 55f;

    private bool minijuegoActivo = false;
    private bool terminando = false;
    private float timerFin = 0f;
    private bool tiempoDetenido = false;

    private const float DURACION_DESVANECER = 0.25f;
    private const float DURACION_FADE_IN = 0.15f;
    private const float ESPERA_FINAL = 0.5f;

    protected override void Start()
    {
        base.Start();

        Debug.Log("EventoColillasLatas iniciado");

        if (PanelMensajesMonitor.Instance != null)
            PanelMensajesMonitor.Instance.MostrarMensajeInmediato(mensajeInicial, colorMensaje);

        escalaUI = Screen.height / 1080f;
        radioPx = radioCirculo * escalaUI;

        CrearSprites();
        CrearUI();

        t = 0f;
        generados = 0;
        resueltos = 0;
        aciertos = 0;
        fallos = 0;
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

    private void CrearSprites()
    {
        spriteDisco = spriteDiscoArte != null ? spriteDiscoArte : CrearSprite(256, 0f, out texDisco);
        spriteAnillo = spriteAnilloArte != null ? spriteAnilloArte : CrearSprite(256, 0.93f, out texAnillo);
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
        GameObject canvasGO = new GameObject("CanvasColillas", typeof(Canvas), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = ordenCanvas;

        GameObject fondoGO = new GameObject("Fondo", typeof(RectTransform), typeof(Image));
        fondoGO.transform.SetParent(canvasGO.transform, false);
        EstirarAPantalla(fondoGO.GetComponent<RectTransform>());

        Image fondo = fondoGO.GetComponent<Image>();
        fondo.color = new Color(0f, 0f, 0f, oscurecerFondo);
        fondo.raycastTarget = true;

        GameObject contGO = new GameObject("Circulos", typeof(RectTransform));
        contGO.transform.SetParent(canvasGO.transform, false);
        contenedor = contGO.GetComponent<RectTransform>();
        EstirarAPantalla(contenedor);
    }

    private void EstirarAPantalla(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    private void GenerarCirculo()
    {
        CirculoOsu c = new CirculoOsu();
        c.pos = ElegirPosicion();
        c.spawn = t;
        c.llegada = t + tiempoAproximacion;

        float diametro = radioPx * 2f;

        GameObject raizGO = new GameObject("Circulo", typeof(RectTransform), typeof(CanvasGroup));
        raizGO.transform.SetParent(contenedor, false);
        c.raiz = raizGO.GetComponent<RectTransform>();
        c.raiz.anchorMin = c.raiz.anchorMax = new Vector2(0.5f, 0.5f);
        c.raiz.pivot = new Vector2(0.5f, 0.5f);
        c.raiz.anchoredPosition = c.pos;
        c.raiz.sizeDelta = new Vector2(diametro, diametro);
        c.grupo = raizGO.GetComponent<CanvasGroup>();
        c.grupo.alpha = 0f;
        c.grupo.blocksRaycasts = false;
        c.grupo.interactable = false;

        Image disco = CrearImagen("Disco", c.raiz, spriteDisco, colorDisco, diametro);
        c.disco = disco;

        CrearImagen("Borde", c.raiz, spriteAnillo, colorBorde, diametro);

        Image anillo = CrearImagen("Anillo", c.raiz, spriteAnillo, colorAnillo, diametro * escalaInicialAnillo);
        c.anillo = anillo.rectTransform;

        circulos.Add(c);
        generados++;
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

    private Vector2 ElegirPosicion()
    {
        float mitadAncho = Screen.width * 0.5f - margenPantalla * escalaUI;
        float mitadAlto = Screen.height * 0.5f - margenPantalla * escalaUI;
        mitadAncho = Mathf.Max(mitadAncho, radioPx);
        mitadAlto = Mathf.Max(mitadAlto, radioPx);

        float separacionMin = radioPx * 3f;
        Vector2 candidata = Vector2.zero;

        for (int intento = 0; intento < 30; intento++)
        {
            candidata = new Vector2(
                Random.Range(-mitadAncho, mitadAncho),
                Random.Range(-mitadAlto, mitadAlto));

            bool libre = true;
            foreach (CirculoOsu otro in circulos)
            {
                if (otro.resuelto) continue;
                if (Vector2.Distance(candidata, otro.pos) < separacionMin)
                {
                    libre = false;
                    break;
                }
            }

            if (libre) return candidata;
        }

        return candidata;
    }

    protected override void Update()
    {
        if (!minijuegoActivo) return;

        t += Time.unscaledDeltaTime;

        if (terminando)
        {
            timerFin -= Time.unscaledDeltaTime;
            ActualizarVisuales();

            if (timerFin <= 0f)
                FinalizarEvento();
            return;
        }

        while (generados < numeroCirculos && t >= tiempoInicial + generados * intervaloAparicion)
            GenerarCirculo();

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            ProcesarClic();

        ActualizarVisuales();
    }

    private void ActualizarVisuales()
    {
        float diametro = radioPx * 2f;

        for (int i = circulos.Count - 1; i >= 0; i--)
        {
            CirculoOsu c = circulos[i];

            if (!c.resuelto)
            {
                float edad = t - c.spawn;
                float progreso = Mathf.Clamp01(edad / tiempoAproximacion);
                float escala = Mathf.Lerp(escalaInicialAnillo, 1f, progreso);

                c.anillo.sizeDelta = new Vector2(diametro * escala, diametro * escala);
                c.grupo.alpha = Mathf.Clamp01(edad / DURACION_FADE_IN);

                if (!terminando && t > c.llegada + ventanaAcierto)
                    Resolver(c, false);
            }
            else
            {
                float f = (t - c.tResuelto) / DURACION_DESVANECER;
                c.grupo.alpha = 1f - Mathf.Clamp01(f);

                if (f >= 1f)
                {
                    Destroy(c.raiz.gameObject);
                    circulos.RemoveAt(i);
                }
            }
        }
    }

    private void ProcesarClic()
    {
        Vector2 pantalla = Mouse.current.position.ReadValue();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(contenedor, pantalla, null, out Vector2 local);

        CirculoOsu elegido = null;
        foreach (CirculoOsu c in circulos)
        {
            if (c.resuelto) continue;
            if (Vector2.Distance(local, c.pos) > radioPx * 1.15f) continue;

            if (elegido == null || c.llegada < elegido.llegada)
                elegido = c;
        }

        if (elegido == null) return;

        float diferencia = t - elegido.llegada;

        if (Mathf.Abs(diferencia) <= ventanaAcierto)
            Resolver(elegido, true);
        else if (diferencia < -ventanaAcierto && diferencia >= -ventanaFallo)
            Resolver(elegido, false);
    }

    private void Resolver(CirculoOsu c, bool acierto)
    {
        if (c.resuelto) return;

        c.resuelto = true;
        c.tResuelto = t;
        c.disco.color = acierto ? colorAcierto : colorFallo;
        c.anillo.gameObject.SetActive(false);

        resueltos++;
        if (acierto) aciertos++;
        else fallos++;

        if (acierto)
        {
            if (resueltos >= numeroCirculos) Sonidos.ReproducirQte(b => b.basura);
            else Sonidos.ReproducirQte(b => b.agarrarBasura);
        }

        ActualizarTextoProgreso();

        if (resueltos >= numeroCirculos)
        {
            terminando = true;
            timerFin = ESPERA_FINAL;
        }
    }

    private void ActualizarTextoProgreso()
    {
        if (PanelMensajesMonitor.Instance == null) return;

        string texto = $"{mensajeInstruccion}\n";
        texto += $"Aciertos: {aciertos}/{numeroCirculos}\n";
        texto += $"Fallos: {fallos}";

        PanelMensajesMonitor.Instance.MostrarMensajeInmediato(texto, colorMensaje);
    }

    private void FinalizarEvento()
    {
        minijuegoActivo = false;
        RestaurarTiempo();

        float cambio = aciertos * limpiezaPorAcierto - fallos * limpiezaPorFallo;

        if (GameManager.Instance != null)
            GameManager.Instance.ModificarLimpieza(cambio);

        if (PanelMensajesMonitor.Instance != null)
        {
            if (aciertos >= aciertosRequeridos)
                PanelMensajesMonitor.Instance.MostrarMensaje("¡Cabina limpia!", Color.green, 2f);
            else
                PanelMensajesMonitor.Instance.MostrarMensaje("Quedó basura en la cabina...", Color.red, 3f);
        }

        Terminar();
    }
}
