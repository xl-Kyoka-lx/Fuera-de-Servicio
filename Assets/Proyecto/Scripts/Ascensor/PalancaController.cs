using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PalancaController : MonoBehaviour
{
   //Creo que deberia cambiar este script, no se esta usando la palanca...

    public AscensorManager ascensorManager;

    public InputActionAsset inputActions;

    private InputAction accionMoverAscensor;

    public float anguloIzquierda = 0f;
    public float anguloDerecha = 180f;
    public float velocidadRotacion = 360f;
    public float duracionAnimacion = 0.4f;

    public bool palancaADerecha = false;
    public bool enAnimacion = false;

    private void Awake()
    {
    if (inputActions != null)
    {
        accionMoverAscensor = inputActions.FindActionMap("Gameplay")?.FindAction("MoverAscensor");
    }
    }
    private void OnEnable()
    {
    if (accionMoverAscensor != null)
    {
        accionMoverAscensor.Enable();
        accionMoverAscensor.performed += OnMoverAscensorPerformed;
    }
    }

    private void OnDisable()
    {
        if (accionMoverAscensor != null)
        {
            accionMoverAscensor.performed -= OnMoverAscensorPerformed;
        }
    }

    private void OnMoverAscensorPerformed(InputAction.CallbackContext context)
{
    if (enAnimacion) return;

    if (GameManager.Instance != null && GameManager.Instance.GameData.ascensorAveriado)
    {
        Debug.LogWarning("PALANCA: el ascensor está averiado y no arranca");
        return;
    }

    if (ascensorManager != null && ascensorManager.DetenidoEnRuta)
    {
        if (PausaJuego.Pausado) return;
        if (DialogoManager.Instance != null && DialogoManager.Instance.HayDialogoActivo()) return;
        if (GameManager.Instance != null && GameManager.Instance.GameData.eventoOcurriendo) return;

        ascensorManager.Reanudar();
        return;
    }

    if (BloqueoEventos.ArranqueBloqueado)
    {
        Debug.LogWarning("PALANCA: bloqueado, acaba de terminar un evento");
        return;
    }
    if (ascensorManager != null && ascensorManager.enMovimiento) return;

    if (TutorialManager.ViajeBloqueado)
    {
        Debug.LogWarning("PALANCA: bloqueado por el tutorial");
        return;
    }
    if (DialogoManager.Instance != null && DialogoManager.Instance.HayDialogoActivo())
    {
        Debug.LogWarning("PALANCA: bloqueado, hay un dialogo abierto");
        return;
    }
    if (GameManager.Instance != null && GameManager.Instance.GameData.eventoOcurriendo)
    {
        Debug.LogWarning("PALANCA: bloqueado, eventoOcurriendo = true");
        return;
    }

    if (ProcesoPartida.Instance != null && !ProcesoPartida.Instance.IntentarPartir())
    {
        return;
    }

    MoverPalanca();
}

    private void MoverPalanca()
    {
        palancaADerecha = !palancaADerecha;
        float anguloObjetivo = palancaADerecha ? anguloDerecha : anguloIzquierda;

        StartCoroutine(RotarPalanca(anguloObjetivo));

        if (ascensorManager != null)
        {
            ascensorManager.IniciarViaje();
        }
    }

    private IEnumerator RotarPalanca(float anguloObjetivo)
    {
        enAnimacion = true;

        Quaternion rotacionInicial = transform.localRotation;
        Quaternion rotacionFinal = Quaternion.Euler(0, anguloObjetivo, 0);
        float tiempoTranscurrido = 0f;

        float duracion = duracionAnimacion;
        if (velocidadRotacion > 0f)
        {
            float diferenciaAngulo = Quaternion.Angle(rotacionInicial, rotacionFinal);
            duracion = diferenciaAngulo / velocidadRotacion;
        }

        while (tiempoTranscurrido < duracion)
        {
            tiempoTranscurrido += Time.deltaTime;
            float t = Mathf.Clamp01(tiempoTranscurrido / duracion);
            t = Mathf.SmoothStep(0f, 1f, t);

            transform.localRotation = Quaternion.Slerp(rotacionInicial, rotacionFinal, t);
            yield return null;
        }

        transform.localRotation = rotacionFinal;
        enAnimacion = false;
    }
}