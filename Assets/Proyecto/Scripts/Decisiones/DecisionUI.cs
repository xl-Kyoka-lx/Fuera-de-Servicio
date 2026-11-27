using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class DecisionUI
{
    public struct Opcion
    {
        public string texto;
        public Action accion;

        public Opcion(string texto, Action accion)
        {
            this.texto = texto;
            this.accion = accion;
        }
    }

    private static GameObject actual;

    public static bool Abierta => actual != null;

    public static void Mostrar(string hablante, string texto, params Opcion[] opciones)
    {
        Cerrar();

        actual = new GameObject("CanvasDecision", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = actual.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600;

        var escalador = actual.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920, 1080);
        escalador.matchWidthOrHeight = 1f;

        var fondo = CrearImagen("Fondo", actual.transform, new Color(0f, 0f, 0f, 0.5f));
        fondo.anchorMin = Vector2.zero;
        fondo.anchorMax = Vector2.one;
        fondo.offsetMin = fondo.offsetMax = Vector2.zero;

        var panel = CrearImagen("Panel", actual.transform, new Color(0.1f, 0.1f, 0.1f, 0.95f));
        panel.sizeDelta = new Vector2(1100f, 420f);

        var nombre = CrearTexto(panel, hablante, 40f, new Vector2(0f, 150f), new Vector2(1000f, 60f), TextAlignmentOptions.Left);
        nombre.color = new Color(1f, 0.85f, 0.4f);
        CrearTexto(panel, texto, 36f, new Vector2(0f, 40f), new Vector2(1000f, 160f), TextAlignmentOptions.TopLeft);

        float ancho = 1000f / Mathf.Max(1, opciones.Length);
        for (int i = 0; i < opciones.Length; i++)
        {
            var op = opciones[i];
            float x = -500f + ancho * (i + 0.5f);
            CrearBoton(panel, op.texto, new Vector2(x, -140f), new Vector2(ancho - 30f, 90f), () =>
            {
                Cerrar();
                op.accion?.Invoke();
            });
        }
    }

    public static void Cerrar()
    {
        if (actual != null) UnityEngine.Object.Destroy(actual);
        actual = null;
    }

    private static RectTransform CrearImagen(string nombre, Transform padre, Color color)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(padre, false);
        go.GetComponent<Image>().color = color;
        return go.GetComponent<RectTransform>();
    }

    private static void CrearBoton(RectTransform padre, string texto, Vector2 pos, Vector2 tam, Action alPulsar)
    {
        var rt = CrearImagen("Boton_" + texto, padre, new Color(0.3f, 0.3f, 0.3f, 1f));
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
        rt.gameObject.AddComponent<Button>().onClick.AddListener(() => alPulsar());
        CrearTexto(rt, texto, 32f, Vector2.zero, tam, TextAlignmentOptions.Center);
    }

    private static TextMeshProUGUI CrearTexto(RectTransform padre, string texto, float tam, Vector2 pos, Vector2 dim, TextAlignmentOptions alineacion)
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
}
