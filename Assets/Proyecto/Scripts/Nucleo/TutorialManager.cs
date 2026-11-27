using System.Collections;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    public static bool ViajeBloqueado = false;

    public static bool EnCurso = false;

    [Header("Referencias")]
    public AscensorManager ascensorManager;
    public EventManager eventManager;
    public EventoEstadoManager estadoManager;
    public ClockManager clockManager;
    [Tooltip("Si se deja vacío se busca solo")]
    public ProcesoPartida procesoPartida;
    [Tooltip("Si se deja vacío se busca solo")]
    public BondadososManager bondadosos;

    [Header("Tiempos")]
    [Tooltip("Segundos de viaje (con el juego corriendo) antes de que se trabe la polea en el segundo viaje")]
    public float segundosAntesDePolea = 8f;
    [Tooltip("Segundos máximos que se espera a que Camila termine de conversar al llegar (por si algo falla)")]
    public float esperaMaximaDespedida = 6f;
    [Tooltip("Segundos máximos esperando a que suba un pasajero para ser Camila (solo si no sube nadie)")]
    public float esperaMaximaCamila = 15f;

    [Header("Demostración de indicadores de sonido")]
    [Tooltip("Segundos que se ve cada cuadro de la demostración")]
    public float segundosPorIndicador = 2.5f;
    public string demoIzquierda = "Te hablo por aquí";
    public string demoDerecha = "Y ahora por aquí";
    public string demoFrente = "Y ahora de frente";

    private enum Paso
    {
        Inactivo,
        Controles,
        EsperandoViaje1,
        MensajeViaje,
        Viaje1,
        MensajePuerta,
        EventoPuerta,
        MensajePasajeros,
        EsperandoCamila,
        MensajePestillo,
        EsperandoW,
        EventoPestillo,
        EsperandoEspacio,
        MensajeEngrase,
        EventoEngrase,
        EsperandoPartida2,
        Viaje2,
        MensajePolea,
        EventoPolea,
        Viaje2Final,
        Llegada2,
        MensajePropina,
        MensajeBasura,
        EventoBasura,
        MensajeFin,
        Fin
    }

    private Paso paso = Paso.Inactivo;
    private float tiempoViaje2 = 0f;
    private float horaLlegada2 = 0f;
    private float esperaCamila = 0f;

    private void Awake()
    {
        Instance = this;
    }

    [System.Obsolete]
    private void Start()
    {
        ViajeBloqueado = false;
        EnCurso = false;

        if (PlayerPrefs.GetInt("TutorialHecho", 0) == 1)
        {
            enabled = false;
            return;
        }

        if (procesoPartida == null) procesoPartida = ProcesoPartida.Instance;
        if (procesoPartida == null) procesoPartida = FindFirstObjectByType<ProcesoPartida>();
        if (bondadosos == null) bondadosos = BondadososManager.Instance;
        if (bondadosos == null) bondadosos = FindFirstObjectByType<BondadososManager>();

        ViajeBloqueado = true;
        EnCurso = true;
        if (eventManager != null) eventManager.eventosActivos = false;
        if (estadoManager != null) estadoManager.enabled = false;

        if (DialogoManager.Instance != null)
            DialogoManager.Instance.OnDialogoTerminado += AlTerminarDialogo;
        if (clockManager != null)
            clockManager.OnDiaTerminado += AlTerminarDia;
        if (estadoManager != null)
            estadoManager.OnEventoTerminado += AlTerminarEvento;
        if (procesoPartida != null)
            procesoPartida.OnWPulsada += AlPulsarW;
        if (bondadosos != null)
        {
            bondadosos.OnCamilaAsignada += AlCamilaAsignada;
            bondadosos.OnEncuentroTerminado += AlTerminarEncuentro;
        }

        Invoke(nameof(Empezar), 0.5f);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

        ViajeBloqueado = false;
        EnCurso = false;

        if (DialogoManager.Instance != null)
            DialogoManager.Instance.OnDialogoTerminado -= AlTerminarDialogo;
        if (clockManager != null)
            clockManager.OnDiaTerminado -= AlTerminarDia;
        if (estadoManager != null)
            estadoManager.OnEventoTerminado -= AlTerminarEvento;
        if (procesoPartida != null)
            procesoPartida.OnWPulsada -= AlPulsarW;
        if (bondadosos != null)
        {
            bondadosos.OnCamilaAsignada -= AlCamilaAsignada;
            bondadosos.OnEncuentroTerminado -= AlTerminarEncuentro;
        }
    }

    private void Empezar()
    {
        paso = Paso.Controles;
        Dialogo("tutorial_inicio");
    }

    private void Update()
    {
        if (!EnCurso || ascensorManager == null) return;

        switch (paso)
        {
            case Paso.EsperandoViaje1:
                if (ascensorManager.enMovimiento)
                {
                    paso = Paso.MensajeViaje;
                    Dialogo("tutorial_viaje");
                }
                break;

            case Paso.Viaje1:
                if (!ascensorManager.enMovimiento)
                {
                    paso = Paso.MensajePuerta;
                    ViajeBloqueado = true;
                    Dialogo("tutorial_puerta");
                }
                break;

            case Paso.EsperandoCamila:
                esperaCamila += Time.deltaTime;
                if (esperaCamila > esperaMaximaCamila)
                {
                    Debug.LogWarning("Tutorial: no subió ningún pasajero para ser Camila. Se sigue sin ella.");
                    if (bondadosos != null) bondadosos.CancelarCamila();
                    AlCamilaAsignada();
                }
                break;

            case Paso.EsperandoPartida2:
                if (ascensorManager.enMovimiento)
                {
                    paso = Paso.Viaje2;
                    tiempoViaje2 = 0f;
                }
                break;

            case Paso.Viaje2:
                if (!ascensorManager.enMovimiento) { IrALlegada2(); break; }

                tiempoViaje2 += Time.deltaTime;
                if (tiempoViaje2 >= segundosAntesDePolea)
                {
                    paso = Paso.MensajePolea;
                    Dialogo("tutorial_polea");
                }
                break;

            case Paso.Viaje2Final:
                if (!ascensorManager.enMovimiento) IrALlegada2();
                break;

            case Paso.Llegada2:
                if (bondadosos != null && bondadosos.CamilaEnViaje) break;
                if (Time.unscaledTime - horaLlegada2 > esperaMaximaDespedida && !HayDialogoActivo())
                    SeguirTrasDespedida();
                break;
        }
    }

    private void AlTerminarDialogo(string id)
    {
        switch (id)
        {
            case "tutorial_inicio":
                if (IndicadoresSonido.Activos) Dialogo("tutorial_indicadores");
                else Dialogo("tutorial_controles");
                break;

            case "tutorial_indicadores":
                StartCoroutine(DemostrarIndicadores());
                break;

            case "tutorial_controles":
                paso = Paso.EsperandoViaje1;
                ViajeBloqueado = false;
                break;

            case "tutorial_viaje":
                paso = Paso.Viaje1;
                break;

            case "tutorial_puerta":
                paso = Paso.EventoPuerta;
                Lanzar<EventoPuertaAtascada>();
                break;

            case "tutorial_pasajeros":
                esperaCamila = 0f;
                paso = Paso.EsperandoCamila;
                ViajeBloqueado = true;
                MostrarEnTV("Esperando al siguiente pasajero...", Color.white, 3f);
                if (bondadosos != null) bondadosos.PedirCamila();
                else AlCamilaAsignada();
                break;

            case "tutorial_pestillo":
                paso = Paso.EsperandoW;
                ViajeBloqueado = false;
                break;

            case "tutorial_engrase":
                paso = Paso.EventoEngrase;
                Lanzar<EventoEngrase>();
                break;

            case "tutorial_polea":
                paso = Paso.EventoPolea;
                Lanzar<EventoPoleaTrabada>();
                break;

            case "tutorial_propina":
                paso = Paso.MensajeBasura;
                Dialogo("tutorial_basura");
                break;

            case "tutorial_basura":
                paso = Paso.EventoBasura;
                Lanzar<EventoColillasLatas>();
                break;

            case "tutorial_fin":
                Terminar();
                break;
        }
    }

    private void AlTerminarEvento(GameObject evento)
    {
        switch (paso)
        {
            case Paso.EventoPuerta:
                paso = Paso.MensajePasajeros;
                ViajeBloqueado = true;
                Dialogo("tutorial_pasajeros");
                break;

            case Paso.EventoPestillo:
                paso = Paso.EsperandoEspacio;
                ViajeBloqueado = false;
                break;

            case Paso.EventoEngrase:
                paso = Paso.EsperandoPartida2;
                ViajeBloqueado = false;
                break;

            case Paso.EventoPolea:
                paso = Paso.Viaje2Final;
                break;

            case Paso.EventoBasura:
                paso = Paso.MensajeFin;
                Dialogo("tutorial_fin");
                break;
        }
    }

    private void Lanzar<T>() where T : EventoBase
    {
        if (estadoManager != null) estadoManager.enabled = true;

        if (estadoManager == null || !estadoManager.ForzarEvento<T>())
        {
            Debug.LogWarning($"Tutorial: no se pudo lanzar {typeof(T).Name}. Revisa que esté en la lista del EventoEstadoManager.");
            AlTerminarEvento(null);
        }
    }

    private void AlPulsarW()
    {
        if (paso != Paso.EsperandoW) return;

        paso = Paso.EventoPestillo;
        Lanzar<EventoPestillo>();
    }

    public bool IntentarEventoAlArrancar()
    {
        if (!EnCurso || paso != Paso.EsperandoEspacio) return false;

        paso = Paso.MensajeEngrase;
        Dialogo("tutorial_engrase");
        return true;
    }

    private void AlCamilaAsignada()
    {
        if (paso != Paso.EsperandoCamila) return;

        paso = Paso.MensajePestillo;
        Dialogo("tutorial_pestillo");
    }

    private void AlTerminarEncuentro(string idBondadoso)
    {
        if (paso != Paso.Llegada2) return;
        if (bondadosos != null && idBondadoso != bondadosos.idCamila) return;

        SeguirTrasDespedida();
    }

    private void IrALlegada2()
    {
        paso = Paso.Llegada2;
        horaLlegada2 = Time.unscaledTime;
        ViajeBloqueado = true;
    }

    private void SeguirTrasDespedida()
    {
        if (paso != Paso.Llegada2) return;

        paso = Paso.MensajePropina;
        Dialogo("tutorial_propina");
    }

    private void Terminar()
    {
        paso = Paso.Fin;
        ViajeBloqueado = false;
        EnCurso = false;

        if (estadoManager != null) estadoManager.enabled = true;
        if (eventManager != null) eventManager.eventosActivos = true;

        PlayerPrefs.SetInt("TutorialHecho", 1);
        PlayerPrefs.Save();
    }

    private void AlTerminarDia()
    {
        PlayerPrefs.SetInt("TutorialHecho", 1);
        PlayerPrefs.Save();
    }

    private bool HayDialogoActivo()
    {
        return DialogoManager.Instance != null && DialogoManager.Instance.HayDialogoActivo();
    }

    private IEnumerator DemostrarIndicadores()
    {
        yield return new WaitForSecondsRealtime(0.4f);

        float espera = segundosPorIndicador + 0.4f;
        IndicadoresSonido.Mostrar(demoIzquierda, IndicadoresSonido.Lado.Izquierda, true, segundosPorIndicador, true);
        yield return new WaitForSecondsRealtime(espera);
        IndicadoresSonido.Mostrar(demoDerecha, IndicadoresSonido.Lado.Derecha, true, segundosPorIndicador, true);
        yield return new WaitForSecondsRealtime(espera);
        IndicadoresSonido.Mostrar(demoFrente, IndicadoresSonido.Lado.Frente, true, segundosPorIndicador, true);
        yield return new WaitForSecondsRealtime(espera);

        Dialogo("tutorial_controles");
    }

    private void Dialogo(string id)
    {
        var dm = DialogoManager.Instance;

        if (dm == null)
        {
            AlTerminarDialogo(id);
            return;
        }

        dm.MostrarDialogo(id);

        if (!dm.HayDialogoActivo())
        {
            Debug.LogWarning($"Tutorial: el diálogo '{id}' no existe en introduccion.json");
            AlTerminarDialogo(id);
        }
    }

    private void MostrarEnTV(string texto, Color color, float duracion)
    {
        if (PanelMensajesMonitor.Instance != null)
            PanelMensajesMonitor.Instance.MostrarMensaje(texto, color, duracion);
    }
}