using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PausaManager : MonoBehaviour
{
    [Header("Panel UI")]
    public GameObject panelOpcionesPausa;

    [Header("Input del Juego")]
    [Tooltip("Arrastra aqui el mismo Input Actions Asset que usa la Palanca")]
    public InputActionAsset inputActionsAsset;

    public static bool juegoPausado = false;

    [Header("Orden de dibujado")]
    [Tooltip("Orden del canvas del panel de pausa: tiene que ser mayor que cualquier otro canvas del juego (eventos, decisiones, indicadores, derrota...)")]
    public int ordenCanvasPausa = 5000;

    /// <summary>Delta de tiempo real que vale 0 mientras el menú de pausa está abierto. Usar en vez de Time.unscaledDeltaTime en cosas del juego.</summary>
    public static float Dt => juegoPausado ? 0f : Time.unscaledDeltaTime;

    /// <summary>Como WaitForSecondsRealtime, pero no descuenta el tiempo que el juego estuvo en pausa.</summary>
    public static IEnumerator Esperar(float segundos)
    {
        float t = 0f;
        while (t < segundos)
        {
            t += Dt;
            yield return null;
        }
    }

    private GameObject canvasPausa;
    private GameObject bloqueo;

    private void Awake()
    {
        if (panelOpcionesPausa == null) return;

        // Canvas raíz propio para la pausa (más confiable que un canvas anidado):
        // copia el escalado del canvas original para que el panel se vea igual
        var original = panelOpcionesPausa.GetComponentInParent<Canvas>(true);
        original = original != null ? original.rootCanvas : null;

        canvasPausa = new GameObject("CanvasPausa", typeof(RectTransform));
        var canvas = canvasPausa.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = ordenCanvasPausa;

        var escalador = canvasPausa.AddComponent<UnityEngine.UI.CanvasScaler>();
        var escOriginal = original != null ? original.GetComponent<UnityEngine.UI.CanvasScaler>() : null;
        if (escOriginal != null)
        {
            escalador.uiScaleMode = escOriginal.uiScaleMode;
            escalador.referenceResolution = escOriginal.referenceResolution;
            escalador.screenMatchMode = escOriginal.screenMatchMode;
            escalador.matchWidthOrHeight = escOriginal.matchWidthOrHeight;
            escalador.scaleFactor = escOriginal.scaleFactor;
            escalador.referencePixelsPerUnit = escOriginal.referencePixelsPerUnit;
        }
        canvasPausa.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Primero la capa oscura (atrás), después el panel (adelante)
        bloqueo = new GameObject("BloqueoPausa", typeof(RectTransform));
        bloqueo.transform.SetParent(canvasPausa.transform, false);
        var rt = (RectTransform)bloqueo.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = bloqueo.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0f, 0f, 0f, 0.5f);
        img.raycastTarget = true; // se come los clics para que no lleguen al juego

        panelOpcionesPausa.transform.SetParent(canvasPausa.transform, false);

        bloqueo.SetActive(false);
        panelOpcionesPausa.SetActive(false);
    }

    private void OnDestroy()
    {
        juegoPausado = false;
        if (canvasPausa != null) Destroy(canvasPausa);
    }

    private InputAction accionPausa;

    private void OnEnable()
    {
        // Copia propia de los controles: así la tecla de pausa sigue andando aunque se apague el resto
        accionPausa = Controles.Accion(Controles.Pausa);
        accionPausa?.Enable();
    }

    void Update()
    {
        bool pulsada = accionPausa != null
            ? accionPausa.WasPressedThisFrame()
            : Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;

        // No abrir la pausa encima de la pantalla de derrota
        if (pulsada && !juegoPausado && PantallaFueraDeServicio.Mostrando) pulsada = false;

        if (pulsada)
        {
            if (juegoPausado)
            {
                Reanudar();
            }
            else
            {
                Pausar();
            }
        }
    }

    public void Pausar()
    {
        if (panelOpcionesPausa != null) panelOpcionesPausa.SetActive(true);
        if (bloqueo != null) bloqueo.SetActive(true);
        PausaJuego.Pedir(this);
        juegoPausado = true;

        if (inputActionsAsset != null)
        {
            inputActionsAsset.FindActionMap("Ascensor")?.Disable();
        }
    }

    public void Reanudar()
    {
        if (panelOpcionesPausa != null) panelOpcionesPausa.SetActive(false);
        if (bloqueo != null) bloqueo.SetActive(false);
        PausaJuego.Liberar(this);
        juegoPausado = false;

        if (inputActionsAsset != null)
        {
            inputActionsAsset.FindActionMap("Ascensor")?.Enable();
        }
    }

    public void VolverAlMenuPrincipal()
    {
        PausaJuego.Limpiar();
        juegoPausado = false;
        SceneManager.LoadScene("Menu inicio");
    }
}