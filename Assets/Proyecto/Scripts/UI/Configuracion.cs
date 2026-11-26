using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Configuracion
{
    public const string NombreSliderVolumen = "Volumen";
    public const string NombreSliderVelocidad = "Velocidad_Texto";

    private const string ClaveVolumen = "Config_Volumen";
    private const string ClaveVelocidad = "Config_VelocidadTexto";

    public const float LetraMasLenta = 0.08f;
    public const float LetraMasRapida = 0.005f;

    public static float Volumen { get; private set; } = 1f;
    public static float VelocidadTexto { get; private set; } = 0.65f;

    public static float SegundosPorLetra => Mathf.Lerp(LetraMasLenta, LetraMasRapida, VelocidadTexto);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Iniciar()
    {
        Volumen = PlayerPrefs.GetFloat(ClaveVolumen, 1f);
        VelocidadTexto = PlayerPrefs.GetFloat(ClaveVelocidad, 0.65f);
        AudioListener.volume = Volumen;

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
        }
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

    public static void Guardar()
    {
        PlayerPrefs.SetFloat(ClaveVolumen, Volumen);
        PlayerPrefs.SetFloat(ClaveVelocidad, VelocidadTexto);
        PlayerPrefs.Save();
    }
}
