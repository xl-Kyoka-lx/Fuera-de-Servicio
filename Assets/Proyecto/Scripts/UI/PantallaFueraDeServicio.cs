using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PantallaFueraDeServicio : MonoBehaviour
{
    public static bool Mostrando { get; private set; }

    public float duracionOscurecer = 2.5f;
    public float duracionAparecerTitulo = 1.5f;
    public float tiempoTituloVisible = 2f;
    public float duracionDesaparecerTitulo = 1.5f;
    public float segundosPorLetra = 0.035f;
    public float pausaEntreLineas = 0.35f;

    private string escenaMenu = "Menu inicio";
    private Image fondo;
    private TextMeshProUGUI titulo;
    private TextMeshProUGUI estadisticas;
    private GameObject boton;

    public static void Mostrar(string escenaMenu = "Menu inicio")
    {
        if (Mostrando) return;
        Mostrando = true;

        // Al perder se borra la partida guardada (aunque el jugador cierre el juego en esta pantalla).
        MenuPrincipal.BorrarPartidaGuardada();

        var go = new GameObject("PantallaFueraDeServicio");
        var pantalla = go.AddComponent<PantallaFueraDeServicio>();
        pantalla.escenaMenu = escenaMenu;
    }

    private void Start()
    {
        PausaJuego.Pedir(this);
        CrearUI();
        MusicaFinal.Iniciar(escenaMenu);
        StartCoroutine(ApagarAmbiente());
        StartCoroutine(Secuencia());
    }

    private void OnDestroy()
    {
        Mostrando = false;
        PausaJuego.Liberar(this);
    }

    // Baja y apaga los sonidos de ambiente (gente, aves, etc.) mientras se oscurece la pantalla.
    // Se salvan la música final y la fuente de Sonidos (clic de los botones).
    // Como el juego queda pausado, el ambiente no vuelve a empezar hasta salir de la escena.
    private IEnumerator ApagarAmbiente()
    {
        var fuentes = new System.Collections.Generic.List<AudioSource>();
        var volumenes = new System.Collections.Generic.List<float>();

        foreach (var f in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
        {
            if (f.GetComponent<MusicaFinal>() != null || f.GetComponent<Sonidos>() != null) continue;
            fuentes.Add(f);
            volumenes.Add(f.volume);
        }

        float t = 0f;
        while (t < duracionOscurecer)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Clamp01(t / duracionOscurecer);
            for (int i = 0; i < fuentes.Count; i++)
                if (fuentes[i] != null) fuentes[i].volume = volumenes[i] * k;
            yield return null;
        }

        foreach (var f in fuentes)
            if (f != null) { f.Stop(); f.volume = 0f; }
    }

    private IEnumerator Secuencia()
    {
        yield return Fundido(a => fondo.color = new Color(0f, 0f, 0f, a), 0f, 1f, duracionOscurecer);

        yield return Fundido(a => titulo.alpha = a, 0f, 1f, duracionAparecerTitulo);
        yield return new WaitForSecondsRealtime(tiempoTituloVisible);
        yield return Fundido(a => titulo.alpha = a, 1f, 0f, duracionDesaparecerTitulo);

        estadisticas.text = "";
        foreach (string linea in LineasEstadisticas())
        {
            if (estadisticas.text.Length > 0) estadisticas.text += "\n";
            foreach (char c in linea)
            {
                estadisticas.text += c;
                yield return new WaitForSecondsRealtime(segundosPorLetra);
            }
            yield return new WaitForSecondsRealtime(pausaEntreLineas);
        }

        boton.SetActive(true);
    }

    private IEnumerator Fundido(System.Action<float> aplicar, float desde, float hasta, float duracion)
    {
        float t = 0f;
        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            aplicar(Mathf.Lerp(desde, hasta, Mathf.Clamp01(t / duracion)));
            yield return null;
        }
        aplicar(hasta);
    }

    private string[] LineasEstadisticas()
    {
        var data = GameManager.Instance != null ? GameManager.Instance.GameData : null;
        if (data == null) return new string[0];

        data.CerrarEstadisticasDelDia();
        data.RegistrarPopularidad();

        return new[]
        {
            $"Días jugados: {data.diaActual}",
            $"Dinero recaudado: ${data.dineroRecaudadoTotal}",
            $"Popularidad al cerrar: {data.contadorPopularidad:F0}%",
            $"Popularidad mínima: {data.popularidadMinima:F0}%",
            $"Máximo recaudado en un día: ${data.maxGanadoEnUnDia}",
            $"Máximo perdido en un día: ${data.maxPerdidoEnUnDia}",
        };
    }

    private void CrearUI()
    {
        float escala = Screen.height / 1080f;

        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        var fondoGO = new GameObject("Fondo", typeof(RectTransform), typeof(Image));
        fondoGO.transform.SetParent(canvasGO.transform, false);
        var fondoRT = fondoGO.GetComponent<RectTransform>();
        fondoRT.anchorMin = Vector2.zero;
        fondoRT.anchorMax = Vector2.one;
        fondoRT.offsetMin = fondoRT.offsetMax = Vector2.zero;
        fondo = fondoGO.GetComponent<Image>();
        fondo.color = new Color(0f, 0f, 0f, 0f);

        titulo = CrearTexto(canvasGO.transform, "ASCENSOR FUERA DE SERVICIO", 84f * escala, Vector2.zero, new Vector2(1700f, 200f) * escala, TextAlignmentOptions.Center);
        titulo.alpha = 0f;

        estadisticas = CrearTexto(canvasGO.transform, "", 40f * escala, new Vector2(0f, 60f) * escala, new Vector2(1000f, 500f) * escala, TextAlignmentOptions.MidlineLeft);
        estadisticas.lineSpacing = 20f;

        boton = new GameObject("BotonMenu", typeof(RectTransform), typeof(Image), typeof(Button));
        boton.transform.SetParent(canvasGO.transform, false);
        var botonRT = boton.GetComponent<RectTransform>();
        botonRT.anchoredPosition = new Vector2(0f, -320f) * escala;
        botonRT.sizeDelta = new Vector2(420f, 90f) * escala;
        boton.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);
        boton.GetComponent<Button>().onClick.AddListener(VolverAlMenu);
        CrearTexto(boton.transform, "Volver al menú", 36f * escala, Vector2.zero, botonRT.sizeDelta, TextAlignmentOptions.Center);
        boton.SetActive(false);
    }

    private TextMeshProUGUI CrearTexto(Transform padre, string texto, float tam, Vector2 pos, Vector2 dim, TextAlignmentOptions alineacion)
    {
        var go = new GameObject("Texto", typeof(RectTransform));
        go.transform.SetParent(padre, false);

        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = texto;
        t.fontSize = tam;
        t.alignment = alineacion;
        t.color = Color.white;
        t.raycastTarget = false;

        var rt = t.rectTransform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = dim;
        return t;
    }

    private void VolverAlMenu()
    {
        PausaJuego.Limpiar();

        if (GameManager.Instance != null)
            GameManager.Instance.ResetearJuego();

        SceneManager.LoadScene(escenaMenu);
    }
}
