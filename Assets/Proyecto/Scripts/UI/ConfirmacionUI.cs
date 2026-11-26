using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class ConfirmacionUI
{
    private static GameObject actual;

    public static void Preguntar(string mensaje, Action alAceptar, string textoSi = "Sí", string textoNo = "No")
    {
        Crear(mensaje, textoSi, alAceptar, textoNo);
    }

    public static void Avisar(string mensaje, string textoOk = "OK")
    {
        Crear(mensaje, textoOk, null, null);
    }

    private static void Crear(string mensaje, string textoAceptar, Action alAceptar, string textoCancelar)
    {
        Cerrar();

        float escala = Screen.height / 1080f;

        actual = new GameObject("CanvasConfirmacion", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = actual.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        RectTransform fondo = CrearImagen("Fondo", actual.transform, new Color(0f, 0f, 0f, 0.6f));
        fondo.anchorMin = Vector2.zero;
        fondo.anchorMax = Vector2.one;
        fondo.offsetMin = fondo.offsetMax = Vector2.zero;

        RectTransform panel = CrearImagen("Panel", actual.transform, new Color(0.15f, 0.15f, 0.15f, 0.95f));
        panel.sizeDelta = new Vector2(900f, 380f) * escala;

        CrearTexto(panel, mensaje, 38f * escala, new Vector2(0f, 60f) * escala, new Vector2(820f, 200f) * escala);

        bool dosBotones = textoCancelar != null;
        float x = dosBotones ? 200f : 0f;

        CrearBoton(panel, textoAceptar, new Vector2(-x, -110f) * escala, escala, new Color(0.25f, 0.55f, 0.3f, 1f), () =>
        {
            Cerrar();
            alAceptar?.Invoke();
        });

        if (dosBotones)
            CrearBoton(panel, textoCancelar, new Vector2(x, -110f) * escala, escala, new Color(0.45f, 0.25f, 0.25f, 1f), Cerrar);
    }

    public static void Cerrar()
    {
        if (actual != null) UnityEngine.Object.Destroy(actual);
        actual = null;
    }

    private static RectTransform CrearImagen(string nombre, Transform padre, Color color)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(padre, false);
        go.GetComponent<Image>().color = color;
        return go.GetComponent<RectTransform>();
    }

    private static void CrearBoton(RectTransform padre, string texto, Vector2 pos, float escala, Color color, Action alPulsar)
    {
        RectTransform rt = CrearImagen("Boton_" + texto, padre, color);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(300f, 90f) * escala;
        rt.gameObject.AddComponent<Button>().onClick.AddListener(() => alPulsar());
        CrearTexto(rt, texto, 36f * escala, Vector2.zero, rt.sizeDelta);
    }

    private static void CrearTexto(RectTransform padre, string texto, float tam, Vector2 pos, Vector2 dim)
    {
        GameObject go = new GameObject("Texto", typeof(RectTransform));
        go.transform.SetParent(padre, false);

        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text = texto;
        t.fontSize = tam;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        t.raycastTarget = false;

        RectTransform rt = t.rectTransform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = dim;
    }
}
