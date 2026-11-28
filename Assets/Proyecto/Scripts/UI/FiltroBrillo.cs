using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Brillo de Opciones: una capa encima de todo el juego.
/// Bajo 0.5 la capa es negra y oscurece; sobre 0.5 es blanca y aclara. En 0.5 no se ve.
/// Se crea sola y sobrevive a los cambios de escena.
/// </summary>
public static class FiltroBrillo
{
    // Qué tan oscuro queda con la barra al mínimo (0 a 1)
    public static float OscuridadMaxima = 0.6f;
    // Qué tan claro queda con la barra al máximo (0 a 1)
    public static float ClaridadMaxima = 0.25f;

    private static Image capa;

    public static void Aplicar(float brillo)
    {
        brillo = Mathf.Clamp01(brillo);

        Color color;
        if (brillo < 0.5f) color = new Color(0f, 0f, 0f, (0.5f - brillo) * 2f * OscuridadMaxima);
        else color = new Color(1f, 1f, 1f, (brillo - 0.5f) * 2f * ClaridadMaxima);

        if (capa == null)
        {
            if (color.a <= 0.001f) return;
            Crear();
        }

        capa.color = color;
        capa.enabled = color.a > 0.001f;
    }

    private static void Crear()
    {
        var go = new GameObject("FiltroBrillo");
        Object.DontDestroyOnLoad(go);

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;

        var hijo = new GameObject("Capa", typeof(RectTransform));
        hijo.transform.SetParent(go.transform, false);

        // Un poco más grande que la pantalla, para que el temblor de cámara no deje bordes sin cubrir
        var rt = (RectTransform)hijo.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-100f, -100f);
        rt.offsetMax = new Vector2(100f, 100f);

        capa = hijo.AddComponent<Image>();
        capa.raycastTarget = false;
    }
}
