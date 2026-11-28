using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum ModoIndicadores { Desactivados = 0, Importantes = 1, Todos = 2 }

public static class Configuracion
{
    public const string NombreSliderVolumen = "Volumen";
    public const string NombreSliderAscensor = "Volumen_Ascensor";
    public const string NombreSliderAmbiente = "Volumen_Ambiente";
    public const string NombreSliderQte = "Volumen_QTE";
    public const string NombreSliderMusica = "Volumen_Musica";
    public const string NombreSliderSacudida = "Intensidad_Sacudida";
    public const string NombreSliderBrillo = "Brillo";
    // Pantalla: flechas (izquierda = sin número, derecha = "(1)") y el texto que muestra lo elegido
    public const string NombreFlechaResolucion = "Cambio de resolucion";
    public const string NombreTextoResolucion = "Resolucionx";
    public const string NombreFlechaPantalla = "Cambio de pantalla";
    public const string NombreTextoPantalla = "Tipo de pantalla";
    public const string NombreSliderVelocidad = "Velocidad_Texto";
    public const string NombreAudioMono = "Audio_Mono";
    public const string NombreIndicadores = "Indicadores_Sonido";

    private const string ClaveVolumen = "Config_Volumen";
    private const string ClaveAscensor = "Config_VolumenAscensor";
    private const string ClaveAmbiente = "Config_VolumenAmbiente";
    private const string ClaveQte = "Config_VolumenQte";
    private const string ClaveMusica = "Config_VolumenMusica";
    private const string ClaveSacudida = "Config_IntensidadSacudida";
    private const string ClaveBrillo = "Config_Brillo";
    private const string ClaveVelocidad = "Config_VelocidadTexto";
    private const string ClaveMono = "Config_AudioMono";
    private const string ClaveIndicadores = "Config_IndicadoresSonido";
    private const string ClaveResolucion = "Config_Resolucion";
    private const string ClaveModoPantalla = "Config_ModoPantalla";

    // La resolución solo se usa en modo Ventana; los otros dos modos usan la del monitor
    public static readonly Vector2Int[] Resoluciones = { new Vector2Int(854, 480), new Vector2Int(1280, 720), new Vector2Int(1920, 1080) };
    public static readonly string[] NombresModoPantalla = { "Ventana", "Ventana completa", "Pant. Completa" };
    private static readonly FullScreenMode[] ModosPantalla = { FullScreenMode.Windowed, FullScreenMode.FullScreenWindow, FullScreenMode.ExclusiveFullScreen };

    public static readonly string[] NombresIndicadores = { "Desactivados", "Solo audio importante", "Todo el audio" };

    public const float LetraMasLenta = 0.08f;
    public const float LetraMasRapida = 0.005f;

    public static float Volumen { get; private set; } = 1f;
    // Se multiplican por el volumen de cada sonido (el general ya se aplica a todo con AudioListener)
    public static float VolumenAscensor { get; private set; } = 1f;
    public static float VolumenAmbiente { get; private set; } = 1f;
    public static float VolumenQte { get; private set; } = 1f;
    public static float VolumenMusica { get; private set; } = 1f;
    // 0 = sin temblor, 1 = normal
    public static float IntensidadSacudida { get; private set; } = 1f;
    // 0 = más oscuro, 0.5 = normal, 1 = más claro
    public static float Brillo { get; private set; } = 0.5f;
    public static float VelocidadTexto { get; private set; } = 0.65f;
    public static bool AudioMono { get; private set; } = false;
    public static ModoIndicadores Indicadores { get; private set; } = ModoIndicadores.Desactivados;
    // Índices de Resoluciones y NombresModoPantalla
    public static int Resolucion { get; private set; } = 2;
    public static int ModoPantalla { get; private set; } = 2;

    private static AudioSpeakerMode modoOriginal = AudioSpeakerMode.Stereo;

    public static float SegundosPorLetra => Mathf.Lerp(LetraMasLenta, LetraMasRapida, VelocidadTexto);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Iniciar()
    {
        Volumen = PlayerPrefs.GetFloat(ClaveVolumen, 1f);
        VolumenAscensor = PlayerPrefs.GetFloat(ClaveAscensor, 1f);
        VolumenAmbiente = PlayerPrefs.GetFloat(ClaveAmbiente, 1f);
        VolumenQte = PlayerPrefs.GetFloat(ClaveQte, 1f);
        VolumenMusica = PlayerPrefs.GetFloat(ClaveMusica, 1f);
        IntensidadSacudida = PlayerPrefs.GetFloat(ClaveSacudida, 1f);
        Brillo = PlayerPrefs.GetFloat(ClaveBrillo, 0.5f);
        FiltroBrillo.Aplicar(Brillo);
        VelocidadTexto = PlayerPrefs.GetFloat(ClaveVelocidad, 0.65f);
        AudioMono = PlayerPrefs.GetInt(ClaveMono, 0) == 1;
        Indicadores = (ModoIndicadores)Mathf.Clamp(PlayerPrefs.GetInt(ClaveIndicadores, 0), 0, 2);

        IniciarPantalla();

        modoOriginal = AudioSettings.GetConfiguration().speakerMode;
        if (modoOriginal == AudioSpeakerMode.Mono) modoOriginal = AudioSpeakerMode.Stereo;

        AudioListener.volume = Volumen;
        if (AudioMono) AplicarAudioMono();

        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    private static void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        foreach (var slider in Object.FindObjectsByType<Slider>(FindObjectsInactive.Include))
        {
            if (slider.gameObject.scene != escena) continue;

            if (slider.name == NombreSliderVolumen)
                Conectar(slider, Volumen, CambiarVolumen);
            else if (slider.name == NombreSliderAscensor)
                Conectar(slider, VolumenAscensor, v => { VolumenAscensor = Mathf.Clamp01(v); Guardar(); });
            else if (slider.name == NombreSliderAmbiente)
                Conectar(slider, VolumenAmbiente, v => { VolumenAmbiente = Mathf.Clamp01(v); Guardar(); });
            else if (slider.name == NombreSliderQte)
                Conectar(slider, VolumenQte, v => { VolumenQte = Mathf.Clamp01(v); Guardar(); });
            else if (slider.name == NombreSliderMusica)
                Conectar(slider, VolumenMusica, v => { VolumenMusica = Mathf.Clamp01(v); Guardar(); });
            else if (slider.name == NombreSliderSacudida)
                Conectar(slider, IntensidadSacudida, CambiarSacudida);
            else if (slider.name == NombreSliderBrillo)
                Conectar(slider, Brillo, CambiarBrillo);
            else if (slider.name == NombreSliderVelocidad)
                Conectar(slider, VelocidadTexto, CambiarVelocidadTexto);
            else if (slider.name == NombreIndicadores)
            {
                slider.minValue = 0f;
                slider.maxValue = 2f;
                slider.wholeNumbers = true;
                slider.SetValueWithoutNotify((int)Indicadores);
                slider.onValueChanged.AddListener(v => CambiarIndicadores(Mathf.RoundToInt(v)));
            }
        }

        foreach (var toggle in Object.FindObjectsByType<Toggle>(FindObjectsInactive.Include))
        {
            if (toggle.gameObject.scene != escena) continue;

            if (toggle.name == NombreAudioMono)
            {
                toggle.SetIsOnWithoutNotify(AudioMono);
                toggle.onValueChanged.AddListener(CambiarAudioMono);
            }
        }

        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (t.gameObject.scene != escena || t.name != NombreIndicadores) continue;
            ConectarCasillasIndicadores(t);
        }

        ConectarPantalla(escena);

        foreach (var lista in Object.FindObjectsByType<TMP_Dropdown>(FindObjectsInactive.Include))
        {
            if (lista.gameObject.scene != escena || lista.name != NombreIndicadores) continue;
            lista.ClearOptions();
            lista.AddOptions(new List<string>(NombresIndicadores));
            lista.SetValueWithoutNotify((int)Indicadores);
            lista.onValueChanged.AddListener(CambiarIndicadores);
        }
    }

    private static void ConectarCasillasIndicadores(Transform grupo)
    {
        var casillas = new List<Toggle>();
        foreach (Transform hijo in grupo)
        {
            var casilla = hijo.GetComponent<Toggle>();
            if (casilla != null) casillas.Add(casilla);
        }
        if (casillas.Count == 0) return;

        void Refrescar()
        {
            for (int i = 0; i < casillas.Count; i++)
                casillas[i].SetIsOnWithoutNotify(i == (int)Indicadores);
        }

        for (int i = 0; i < casillas.Count; i++)
        {
            int indice = i;
            casillas[i].group = null;
            casillas[i].onValueChanged.AddListener(encendida =>
            {
                if (encendida) CambiarIndicadores(indice);
                Refrescar();
            });
        }

        Refrescar();
    }

    private static void Conectar(Slider slider, float valor, UnityEngine.Events.UnityAction<float> alCambiar)
    {
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(valor);
        slider.onValueChanged.AddListener(alCambiar);
    }

    public static void CambiarVolumen(float valor)
    {
        Volumen = Mathf.Clamp01(valor);
        AudioListener.volume = Volumen;
        Guardar();
    }

    public static void CambiarSacudida(float valor)
    {
        IntensidadSacudida = Mathf.Clamp01(valor);
        Guardar();
    }

    public static void CambiarBrillo(float valor)
    {
        Brillo = Mathf.Clamp01(valor);
        FiltroBrillo.Aplicar(Brillo);
        Guardar();
    }

    // ---------- RESOLUCIÓN Y MODO DE PANTALLA ----------

    private static void IniciarPantalla()
    {
        if (PlayerPrefs.HasKey(ClaveModoPantalla))
        {
            Resolucion = Mathf.Clamp(PlayerPrefs.GetInt(ClaveResolucion, 2), 0, Resoluciones.Length - 1);
            ModoPantalla = Mathf.Clamp(PlayerPrefs.GetInt(ClaveModoPantalla, 2), 0, ModosPantalla.Length - 1);
            AplicarPantalla();
            return;
        }

        // Primera vez: no se toca nada, solo se muestra lo que Unity ya tiene puesto
        switch (Screen.fullScreenMode)
        {
            case FullScreenMode.Windowed: ModoPantalla = 0; break;
            case FullScreenMode.ExclusiveFullScreen: ModoPantalla = 2; break;
            default: ModoPantalla = 1; break;
        }
        Resolucion = ResolucionMasCercana(Screen.width, Screen.height);
    }

    private static int ResolucionMasCercana(int ancho, int alto)
    {
        int mejor = Resoluciones.Length - 1;
        int menorDiferencia = int.MaxValue;
        for (int i = 0; i < Resoluciones.Length; i++)
        {
            int diferencia = Mathf.Abs(Resoluciones[i].y - alto) + Mathf.Abs(Resoluciones[i].x - ancho);
            if (diferencia < menorDiferencia) { menorDiferencia = diferencia; mejor = i; }
        }
        return mejor;
    }

    private static void AplicarPantalla()
    {
        var modo = ModosPantalla[ModoPantalla];

        if (modo == FullScreenMode.Windowed)
        {
            var r = Resoluciones[Resolucion];
            Screen.SetResolution(r.x, r.y, modo);
        }
        else
        {
            // Ventana completa y pantalla completa ocupan todo el monitor con su resolución
            int ancho = Display.main.systemWidth;
            int alto = Display.main.systemHeight;
            if (ancho <= 0 || alto <= 0) { ancho = Screen.currentResolution.width; alto = Screen.currentResolution.height; }
            Screen.SetResolution(ancho, alto, modo);
        }
    }

    public static void CambiarResolucion(int paso)
    {
        Resolucion = (Resolucion + paso + Resoluciones.Length) % Resoluciones.Length;
        GuardarPantalla();
    }

    public static void CambiarModoPantalla(int paso)
    {
        ModoPantalla = (ModoPantalla + paso + ModosPantalla.Length) % ModosPantalla.Length;
        GuardarPantalla();
    }

    private static void GuardarPantalla()
    {
        PlayerPrefs.SetInt(ClaveResolucion, Resolucion);
        PlayerPrefs.SetInt(ClaveModoPantalla, ModoPantalla);
        PlayerPrefs.Save();
        AplicarPantalla();
    }

    private static void ConectarPantalla(Scene escena)
    {
        TextMeshProUGUI textoResolucion = null, textoPantalla = null;
        var flechas = new List<(Button boton, bool esResolucion, int paso)>();

        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (t.gameObject.scene != escena) continue;
            string nombre = t.name.Trim();

            if (nombre == NombreTextoResolucion) textoResolucion = t.GetComponent<TextMeshProUGUI>();
            else if (nombre == NombreTextoPantalla) textoPantalla = t.GetComponent<TextMeshProUGUI>();
            else if (nombre == NombreFlechaResolucion || nombre == NombreFlechaResolucion + " (1)" ||
                     nombre == NombreFlechaPantalla || nombre == NombreFlechaPantalla + " (1)")
            {
                var boton = t.GetComponent<Button>();
                if (boton == null) continue;
                bool esResolucion = nombre.StartsWith(NombreFlechaResolucion);
                int paso = nombre.EndsWith("(1)") ? 1 : -1;
                flechas.Add((boton, esResolucion, paso));
            }
        }

        if (textoResolucion == null && textoPantalla == null && flechas.Count == 0) return;

        Color colorResolucion = textoResolucion != null ? textoResolucion.color : Color.white;

        void Refrescar()
        {
            if (textoResolucion != null)
            {
                var r = Resoluciones[Resolucion];
                textoResolucion.text = $"{r.x}x{r.y}";
                // Fuera de modo Ventana la resolución no se usa: se ve apagada
                var c = colorResolucion;
                if (ModoPantalla != 0) c.a *= 0.4f;
                textoResolucion.color = c;
            }
            if (textoPantalla != null) textoPantalla.text = NombresModoPantalla[ModoPantalla];
        }

        foreach (var (boton, esResolucion, paso) in flechas)
        {
            boton.onClick.AddListener(() =>
            {
                if (esResolucion) CambiarResolucion(paso);
                else CambiarModoPantalla(paso);
                Refrescar();
            });
        }

        Refrescar();
    }

    public static void CambiarVelocidadTexto(float valor)
    {
        VelocidadTexto = Mathf.Clamp01(valor);
        Guardar();
    }

    public static void CambiarIndicadores(int valor)
    {
        Indicadores = (ModoIndicadores)Mathf.Clamp(valor, 0, 2);
        Guardar();
    }

    public static void CambiarAudioMono(bool mono)
    {
        if (AudioMono == mono) return;
        AudioMono = mono;
        AplicarAudioMono();
        Guardar();
    }

    private static void AplicarAudioMono()
    {
        var config = AudioSettings.GetConfiguration();
        var modo = AudioMono ? AudioSpeakerMode.Mono : modoOriginal;
        if (config.speakerMode == modo) return;

        var sonando = new List<(AudioSource fuente, float tiempo)>();
        foreach (var fuente in Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude))
            if (fuente.isPlaying) sonando.Add((fuente, fuente.time));

        config.speakerMode = modo;
        AudioSettings.Reset(config);

        foreach (var (fuente, tiempo) in sonando)
        {
            if (fuente == null || fuente.clip == null) continue;
            fuente.Play();
            fuente.time = Mathf.Min(tiempo, fuente.clip.length - 0.01f);
        }
    }

    public static void Guardar()
    {
        PlayerPrefs.SetFloat(ClaveVolumen, Volumen);
        PlayerPrefs.SetFloat(ClaveAscensor, VolumenAscensor);
        PlayerPrefs.SetFloat(ClaveAmbiente, VolumenAmbiente);
        PlayerPrefs.SetFloat(ClaveQte, VolumenQte);
        PlayerPrefs.SetFloat(ClaveMusica, VolumenMusica);
        PlayerPrefs.SetFloat(ClaveSacudida, IntensidadSacudida);
        PlayerPrefs.SetFloat(ClaveBrillo, Brillo);
        PlayerPrefs.SetFloat(ClaveVelocidad, VelocidadTexto);
        PlayerPrefs.SetInt(ClaveMono, AudioMono ? 1 : 0);
        PlayerPrefs.SetInt(ClaveIndicadores, (int)Indicadores);
        PlayerPrefs.Save();
    }
}
