using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum ModoIndicadores { Desactivados = 0, Importantes = 1, Todos = 2 }

public static class Configuracion
{
    public const string NombreSliderVolumen = "Volumen";
    public const string NombreSliderVelocidad = "Velocidad_Texto";
    public const string NombreAudioMono = "Audio_Mono";
    public const string NombreIndicadores = "Indicadores_Sonido";

    private const string ClaveVolumen = "Config_Volumen";
    private const string ClaveVelocidad = "Config_VelocidadTexto";
    private const string ClaveMono = "Config_AudioMono";
    private const string ClaveIndicadores = "Config_IndicadoresSonido";

    public static readonly string[] NombresIndicadores = { "Desactivados", "Solo audio importante", "Todo el audio" };

    public const float LetraMasLenta = 0.08f;
    public const float LetraMasRapida = 0.005f;

    public static float Volumen { get; private set; } = 1f;
    public static float VelocidadTexto { get; private set; } = 0.65f;
    public static bool AudioMono { get; private set; } = false;
    public static ModoIndicadores Indicadores { get; private set; } = ModoIndicadores.Desactivados;

    private static AudioSpeakerMode modoOriginal = AudioSpeakerMode.Stereo;

    public static float SegundosPorLetra => Mathf.Lerp(LetraMasLenta, LetraMasRapida, VelocidadTexto);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Iniciar()
    {
        Volumen = PlayerPrefs.GetFloat(ClaveVolumen, 1f);
        VelocidadTexto = PlayerPrefs.GetFloat(ClaveVelocidad, 0.65f);
        AudioMono = PlayerPrefs.GetInt(ClaveMono, 0) == 1;
        Indicadores = (ModoIndicadores)Mathf.Clamp(PlayerPrefs.GetInt(ClaveIndicadores, 0), 0, 2);

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
        PlayerPrefs.SetFloat(ClaveVelocidad, VelocidadTexto);
        PlayerPrefs.SetInt(ClaveMono, AudioMono ? 1 : 0);
        PlayerPrefs.SetInt(ClaveIndicadores, (int)Indicadores);
        PlayerPrefs.Save();
    }
}
