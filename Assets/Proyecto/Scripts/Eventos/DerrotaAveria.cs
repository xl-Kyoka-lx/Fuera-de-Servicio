using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// Pantalla de derrota cuando el mantenimiento llega a 0 durante el día.
// Crea su propia UI por código: solo hay que ponerlo en un objeto de la escena del día.
public class DerrotaAveria : MonoBehaviour
{
    public string titulo = "El ascensor se averió";
    [TextArea] public string mensaje = "El mantenimiento llegó a 0 y el ascensor ya no arranca.";
    public string textoBoton = "Volver al menú";
    [Tooltip("Nombre exacto de la escena del menú principal")]
    public string escenaMenu = "Menu inicio";
    [Tooltip("Segundos de espera antes de mostrar la pantalla")]
    public float retraso = 1.5f;
    public int ordenCanvas = 200;

    private bool mostrada = false;

    private void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnMantenimientoCero += AlAveriarse;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnMantenimientoCero -= AlAveriarse;
    }

    private void AlAveriarse()
    {
        if (mostrada) return;
        mostrada = true;

        if (PanelMensajesMonitor.Instance != null)
            PanelMensajesMonitor.Instance.MostrarMensajeInmediato("¡El ascensor se averió!", Color.red);

        StartCoroutine(MostrarConRetraso());
    }

    private IEnumerator MostrarConRetraso()
    {
        yield return new WaitForSecondsRealtime(retraso);

        CrearPantalla();
        PausaJuego.Pedir(this);
    }

    private void CrearPantalla()
    {
        GameObject canvasGO = new GameObject("CanvasDerrotaAveria", typeof(Canvas), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = ordenCanvas;

        // Fondo oscuro que absorbe los clics
        GameObject fondoGO = new GameObject("Fondo", typeof(RectTransform), typeof(Image));
        fondoGO.transform.SetParent(canvasGO.transform, false);
        RectTransform fondoRT = fondoGO.GetComponent<RectTransform>();
        fondoRT.anchorMin = Vector2.zero;
        fondoRT.anchorMax = Vector2.one;
        fondoRT.offsetMin = Vector2.zero;
        fondoRT.offsetMax = Vector2.zero;
        fondoGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.9f);

        float escala = Screen.height / 1080f;

        CrearTexto(canvasGO.transform, titulo, 72f * escala, new Vector2(0f, 150f * escala), new Vector2(1400f * escala, 120f * escala), Color.red);
        CrearTexto(canvasGO.transform, mensaje, 36f * escala, new Vector2(0f, 20f * escala), new Vector2(1200f * escala, 160f * escala), Color.white);

        // Botón
        GameObject botonGO = new GameObject("Boton", typeof(RectTransform), typeof(Image), typeof(Button));
        botonGO.transform.SetParent(canvasGO.transform, false);
        RectTransform botonRT = botonGO.GetComponent<RectTransform>();
        botonRT.anchorMin = botonRT.anchorMax = new Vector2(0.5f, 0.5f);
        botonRT.anchoredPosition = new Vector2(0f, -150f * escala);
        botonRT.sizeDelta = new Vector2(420f * escala, 90f * escala);
        botonGO.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.25f, 1f);
        botonGO.GetComponent<Button>().onClick.AddListener(VolverAlMenu);

        CrearTexto(botonGO.transform, textoBoton, 36f * escala, Vector2.zero, botonRT.sizeDelta, Color.white);
    }

    private void CrearTexto(Transform padre, string texto, float tam, Vector2 pos, Vector2 dim, Color color)
    {
        GameObject go = new GameObject("Texto", typeof(RectTransform));
        go.transform.SetParent(padre, false);

        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text = texto;
        t.fontSize = tam;
        t.alignment = TextAlignmentOptions.Center;
        t.color = color;
        t.raycastTarget = false;

        RectTransform rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = dim;
    }

    private void VolverAlMenu()
    {
        PausaJuego.Limpiar();

        if (GameManager.Instance != null)
            GameManager.Instance.ResetearJuego();

        SceneManager.LoadScene(escenaMenu);
    }
}