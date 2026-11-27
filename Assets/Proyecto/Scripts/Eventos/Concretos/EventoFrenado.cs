using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class EventoFrenado : MonoBehaviour
{
    [Header("Ventana de frenado")]
    [Tooltip("Número con el que empieza la cuenta en pantalla. Baja más lento que el tiempo real para llegar a 0 al final de la ventana real")]
    public float segundosEnPantalla = 1.5f;
    [Tooltip("Segundos que de verdad tiene el jugador para frenar (no se muestra)")]
    public float ventanaReal = 2.5f;
    [Tooltip("Marcado: el punto de frenado del AscensorManager se ajusta para que el ascensor llegue a la estación justo cuando la cuenta llega a 0")]
    public bool sincronizarLlegada = true;

    [Header("Notas (segundos desde que aparece ¡FRENA!)")]
    [Tooltip("Segundos reales: frenar antes de esto es perfecto")]
    public float perfectoHasta = 1f;
    [Tooltip("Segundos reales: frenar antes de esto es aceptable. Después, hasta el final de la ventana real, es brusco")]
    public float aceptableHasta = 1.5f;

    [Header("Castigos")]
    [Tooltip("Popularidad que se pierde con un frenado aceptable")]
    public float popularidadAceptable = 3f;
    [Tooltip("Popularidad que se pierde con un frenado brusco o si no frena")]
    public float popularidadBrusco = 8f;
    [Tooltip("Mantenimiento que se pierde con un frenado brusco o si no frena")]
    public float dañoFallo = 10f;

    [Tooltip("Segundos sin eventos después de la ventana de frenado")]
    public float segundosSinEventosTrasFrenar = 2f;

    [Header("Frenar a mitad del viaje (mantener Shift)")]
    [Tooltip("Popularidad que se pierde al detener el ascensor por completo fuera de la ventana de frenado")]
    public float penalizacionFrenoEnRuta = 15f;
    public string mensajeFrenoEnRuta = "¡Frenaste a mitad del viaje! Presiona ESPACIO para continuar";
    [Tooltip("Frenazos a medias (soltar Shift antes de detenerse) por viaje antes de que baje la popularidad")]
    public int frenazosParaQueja = 3;
    [Tooltip("Popularidad que se pierde cada vez que se llega a esa cantidad de frenazos")]
    public float penalizacionFrenazos = 8f;
    public string mensajeFrenazos = "Los pasajeros se quejan de tantos frenazos";
    public Color colorFrenoEnRuta = new Color(1f, 0.35f, 0.35f);

    public AscensorManager ascensorManager;
    public InputActionAsset inputActions;
    public string actionMapName = "Gameplay";
    public string actionName = "resolver";

    private InputAction inputFrenar;
    private bool eventoActivo = false;
    private bool yaPresiono = false;
    private float segundosAlPresionar = -1f;
    private float inicioVentana;

    private bool mensajeFrenoEnPantalla = false;
    private bool frenandoAntes = false;
    private int frenazosEsteViaje = 0;

    private float Margen => EfectosManager.MultiplicadorMargenFrenado;
    private float VentanaVisible => segundosEnPantalla * Margen;
    private float VentanaActual => Mathf.Max(0.1f, ventanaReal) * Margen;

    private void Start()
    {
        if (sincronizarLlegada && ascensorManager != null && ascensorManager.duracionViaje > 0f)
            ascensorManager.umbralFrenado = Mathf.Clamp(1f - VentanaActual / ascensorManager.duracionViaje, 0.05f, 0.95f);
    }

    private void Awake()
    {
        if (inputActions != null)
        {
            inputFrenar = inputActions.FindActionMap(actionMapName)?.FindAction(actionName);
        }
    }

    private void OnEnable()
    {
        if (inputFrenar != null)
        {
            inputFrenar.Enable();
            inputFrenar.performed += OnFrenarPerformed;
        }

        if (ascensorManager != null)
        {
            ascensorManager.OnPuntoDeFrenado += IniciarEvento;
            ascensorManager.OnArranque += AlArrancar;
            ascensorManager.OnSalioDelPunto += AlEmpezarViaje;
            ascensorManager.OnDetenidoEnRuta += AlDetenerseEnRuta;
        }
    }

    private void OnDisable()
    {
        if (inputFrenar != null)
        {
            inputFrenar.performed -= OnFrenarPerformed;
            inputFrenar.Disable();
        }

        if (ascensorManager != null)
        {
            ascensorManager.OnPuntoDeFrenado -= IniciarEvento;
            ascensorManager.OnArranque -= AlArrancar;
            ascensorManager.OnSalioDelPunto -= AlEmpezarViaje;
            ascensorManager.OnDetenidoEnRuta -= AlDetenerseEnRuta;
        }
    }

    private void IniciarEvento()
    {
        if (eventoActivo) return;
        
        eventoActivo = true;

        BloqueoEventos.BloquearEventos(VentanaActual + segundosSinEventosTrasFrenar);
        yaPresiono = false;
        segundosAlPresionar = -1f;
        inicioVentana = Time.unscaledTime;
        SoltarFrenoManual(false);

        if (PanelAccion.Instance != null)
        {
            PanelAccion.Instance.MostrarAccion("¡FRENA! [SHIFT]", VentanaActual, VentanaVisible);
        }

        StartCoroutine(EsperarFrenado());
    }

    private void OnFrenarPerformed(InputAction.CallbackContext context)
    {
        RegistrarFrenoEnVentana();
    }

    private void RegistrarFrenoEnVentana()
    {
        if (!eventoActivo || yaPresiono) return;

        yaPresiono = true;
        segundosAlPresionar = Time.unscaledTime - inicioVentana;
        ascensorManager.NotificarFreno();
    }

    private void Update()
    {
        if (ascensorManager == null) return;

        bool puedeFrenar = !eventoActivo
            && ascensorManager.enMovimiento
            && !ascensorManager.DetenidoEnRuta
            && !PausaJuego.Pausado
            && !(GameManager.Instance != null && GameManager.Instance.GameData.eventoOcurriendo)
            && !(DialogoManager.Instance != null && DialogoManager.Instance.HayDialogoActivo());

        bool presionado = inputFrenar != null && inputFrenar.IsPressed();

        if (eventoActivo && presionado) RegistrarFrenoEnVentana();

        if (puedeFrenar && presionado)
        {
            ascensorManager.frenoManual = true;
            frenandoAntes = true;
        }
        else if (frenandoAntes)
        {
            SoltarFrenoManual(!ascensorManager.DetenidoEnRuta);
        }
    }

    private void SoltarFrenoManual(bool contarFrenazo)
    {
        if (ascensorManager != null) ascensorManager.frenoManual = false;
        if (!frenandoAntes) return;
        frenandoAntes = false;

        if (!contarFrenazo || TutorialManager.EnCurso) return;

        frenazosEsteViaje++;
        if (frenazosParaQueja > 0 && frenazosEsteViaje % frenazosParaQueja == 0)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.ModificarPopularidad(-penalizacionFrenazos);

            if (PanelMensajesMonitor.Instance != null)
                PanelMensajesMonitor.Instance.MostrarMensajeInmediato(mensajeFrenazos, colorFrenoEnRuta);
        }
    }

    private void AlEmpezarViaje()
    {
        frenazosEsteViaje = 0;
        frenandoAntes = false;
    }

    private void AlDetenerseEnRuta()
    {
        frenandoAntes = false;

        if (!TutorialManager.EnCurso && GameManager.Instance != null)
            GameManager.Instance.ModificarPopularidad(-penalizacionFrenoEnRuta);

        if (PanelMensajesMonitor.Instance != null)
        {
            PanelMensajesMonitor.Instance.MostrarMensajeInmediato(mensajeFrenoEnRuta, colorFrenoEnRuta);
            mensajeFrenoEnPantalla = true;
        }
    }

    private void AlArrancar()
    {
        if (!mensajeFrenoEnPantalla) return;
        mensajeFrenoEnPantalla = false;

        if (PanelMensajesMonitor.Instance != null)
            PanelMensajesMonitor.Instance.MostrarMensajeInmediato("", Color.white);
    }

    private IEnumerator EsperarFrenado()
    {
        yield return new WaitForSecondsRealtime(VentanaActual);
        EvaluarFrenado();
    }

    private void EvaluarFrenado()
    {
        if (!eventoActivo) return;

        eventoActivo = false;
        
        if (PanelAccion.Instance != null)
            PanelAccion.Instance.OcultarAccion();

        if (!yaPresiono)
        {
            AplicarResultado(ResultadoFrenado.Malo, "¡No frenaste a tiempo!");
            return;
        }

        if (segundosAlPresionar < perfectoHasta * Margen)
            AplicarResultado(ResultadoFrenado.Perfecto, "¡Frenado perfecto!");
        else if (segundosAlPresionar < aceptableHasta * Margen)
            AplicarResultado(ResultadoFrenado.Bueno, "Frenado aceptable");
        else
            AplicarResultado(ResultadoFrenado.Malo, "¡Frenado brusco!");
    }

    private enum ResultadoFrenado { Perfecto, Bueno, Malo }

    private void AplicarResultado(ResultadoFrenado resultado, string mensaje)
    {
        if (PanelAccion.Instance != null)
        {
            Color color = resultado switch
            {
                ResultadoFrenado.Perfecto => Color.green,
                ResultadoFrenado.Bueno => Color.yellow,
                ResultadoFrenado.Malo => Color.red,
                _ => Color.white
            };
            
            if (PanelAccion.Instance != null)
                PanelAccion.Instance.MostrarResultado(mensaje, color, 2f);
        }

        if (GameManager.Instance == null) return;

        float popularidad = resultado switch
        {
            ResultadoFrenado.Bueno => popularidadAceptable,
            ResultadoFrenado.Malo => popularidadBrusco,
            _ => 0f
        };
        float daño = resultado == ResultadoFrenado.Malo ? dañoFallo : 0f;

        if (popularidad > 0f && !TutorialManager.EnCurso)
            GameManager.Instance.ModificarPopularidad(-popularidad);

        if (daño > 0f)
            GameManager.Instance.ModificarPolea(-daño);
    }
}