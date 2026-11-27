using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IndicadoresSonido : MonoBehaviour
{
    public enum Lado { Izquierda, Derecha, Frente }

    private class Cuadro
    {
        public CanvasGroup grupo;
        public TextMeshProUGUI texto;
        public float hasta;
    }

    private const float Fundido = 0.25f;

    private static IndicadoresSonido instancia;

    private Cuadro izquierda;
    private Cuadro derecha;

    public static bool Activos => Configuracion.Indicadores != ModoIndicadores.Desactivados;

    private static bool HayDialogo => DialogoManager.Instance != null && DialogoManager.Instance.HayDialogoActivo();

    public static void Mostrar(string texto, Lado lado, bool importante, float duracion = 3f, bool forzar = false)
    {
        if (!forzar)
        {
            if (Configuracion.Indicadores == ModoIndicadores.Desactivados) return;
            if (Configuracion.Indicadores == ModoIndicadores.Importantes && !importante) return;
            if (HayDialogo) return;
        }

        var i = Instancia();
        if (lado != Lado.Derecha) i.Poner(i.izquierda, texto, duracion);
        if (lado != Lado.Izquierda) i.Poner(i.derecha, texto, duracion);
    }

    public static Lado LadoDesdePan(float pan)
    {
        if (pan < -0.33f) return Lado.Izquierda;
        if (pan > 0.33f) return Lado.Derecha;
        return Lado.Frente;
    }

    private static IndicadoresSonido Instancia()
    {
        if (instancia != null) return instancia;

        var go = new GameObject("IndicadoresSonido");
        DontDestroyOnLoad(go);
        instancia = go.AddComponent<IndicadoresSonido>();
        instancia.CrearUI();
        return instancia;
    }

    private void Poner(Cuadro c, string texto, float duracion)
    {
        c.texto.text = texto;
        c.hasta = Time.unscaledTime + duracion;
    }

    private void Update()
    {
        bool ocultar = HayDialogo;
        Actualizar(izquierda, ocultar);
        Actualizar(derecha, ocultar);
    }

    private void Actualizar(Cuadro c, bool ocultar)
    {
        if (ocultar) c.hasta = 0f;
        float objetivo = Time.unscaledTime < c.hasta ? 1f : 0f;
        c.grupo.alpha = Mathf.MoveTowards(c.grupo.alpha, objetivo, Time.unscaledDeltaTime / Fundido);
    }

    private void CrearUI()
    {
        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;

        var escalador = canvasGO.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920, 1080);
        escalador.matchWidthOrHeight = 1f;

        izquierda = CrearCuadro(canvasGO.transform, true);
        derecha = CrearCuadro(canvasGO.transform, false);
    }

    private Cuadro CrearCuadro(Transform padre, bool izquierdo)
    {
        var go = new GameObject(izquierdo ? "Izquierda" : "Derecha",
            typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        go.transform.SetParent(padre, false);

        var rt = go.GetComponent<RectTransform>();
        Vector2 esquina = izquierdo ? new Vector2(0f, 0f) : new Vector2(1f, 0f);
        rt.anchorMin = rt.anchorMax = rt.pivot = esquina;
        rt.anchoredPosition = new Vector2(izquierdo ? 40f : -40f, 40f);

        go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);

        var layout = go.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 14, 14);
        layout.childControlWidth = layout.childControlHeight = true;

        var ajuste = go.GetComponent<ContentSizeFitter>();
        ajuste.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var grupo = go.GetComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.blocksRaycasts = false;
        grupo.interactable = false;

        var textoGO = new GameObject("Texto", typeof(RectTransform));
        textoGO.transform.SetParent(go.transform, false);
        var texto = textoGO.AddComponent<TextMeshProUGUI>();
        texto.fontSize = 32f;
        texto.color = Color.white;
        texto.alignment = izquierdo ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;
        texto.raycastTarget = false;
        texto.textWrappingMode = TextWrappingModes.NoWrap;

        return new Cuadro { grupo = grupo, texto = texto };
    }
}
