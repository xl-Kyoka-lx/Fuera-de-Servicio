using UnityEngine;

// Tutorial de "Nueva partida". Ocupa la primera parte del primer día y enseña todo en este orden:
//
//   Viaje 1 (sin eventos): controles (W, ESPACIO, SHIFT, ESC, ENTER, mouse)
//   Llegada 1  -> Puerta atascada (ESPACIO repetido)
//   Camila sube -> Pestillo (W, luego ESPACIO en el momento justo)
//   Cabina lista -> ESPACIO -> Engrase (mantener ESPACIO)
//   Viaje 2    -> Polea trabada (ESPACIO al ritmo del brillo)
//   Llegada 2  -> Camila baja, conversa y deja propina -> Colillas y latas (mouse)
//   Fin        -> se activan los eventos normales y el resto del día se juega normal
//
// Los diálogos pausan el reloj, así que solo cuenta el tiempo de juego real (≈ 2 a 3 minutos de los 10 del día).
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    // Mientras sea true, W y ESPACIO no hacen nada (lo leen ProcesoPartida y PalancaController)
    public static bool ViajeBloqueado = false;

    // Verdadero mientras dura el tutorial (el proceso de partida no penaliza errores en este tiempo)
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

    private enum Paso
    {
        Inactivo,
        Controles,          // diálogo con todas las teclas
        EsperandoViaje1,    // el jugador hace W y ESPACIO
        MensajeViaje,       // diálogo con el ascensor en marcha
        Viaje1,
        MensajePuerta,
        EventoPuerta,
        MensajePasajeros,
        EsperandoCamila,    // W y ESPACIO bloqueadas hasta que Camila esté dentro
        MensajePestillo,
        EsperandoW,         // el jugador cierra con W y sale el pestillo
        EventoPestillo,
        EsperandoEspacio,   // al pulsar ESPACIO sale el aviso del engrase
        MensajeEngrase,
        EventoEngrase,
        EsperandoPartida2,  // engrasado: ESPACIO de nuevo para partir
        Viaje2,
        MensajePolea,
        EventoPolea,
        Viaje2Final,
        Llegada2,           // se espera a que Camila baje y converse
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
        if (estadoManager != null) estadoManager.enabled = false;   // se vuelve a activar al lanzar el primer evento

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
        Dialogo("tutorial_controles");
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
                    // el primer viaje terminó: aviso de la puerta antes del primer evento
                    paso = Paso.MensajePuerta;
                    ViajeBloqueado = true;
                    Dialogo("tutorial_puerta");
                }
                break;

            case Paso.EsperandoCamila:
                esperaCamila += Time.deltaTime;
                if (esperaCamila > esperaMaximaCamila)
                {
                    // No subió ningún pasajero: se sigue sin Camila
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
                // Si Camila viene en el ascensor se espera su diálogo (AlTerminarEncuentro).
                // Solo si no viene nadie (no subió ningún pasajero) se sigue tras un rato.
                if (bondadosos != null && bondadosos.CamilaEnViaje) break;
                if (Time.unscaledTime - horaLlegada2 > esperaMaximaDespedida && !HayDialogoActivo())
                    SeguirTrasDespedida();
                break;
        }
    }

    // ---------- Diálogos ----------

    private void AlTerminarDialogo(string id)
    {
        switch (id)
        {
            case "tutorial_controles":
                paso = Paso.EsperandoViaje1;
                ViajeBloqueado = false;          // ahora sí puede usar W y ESPACIO
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

    // ---------- Eventos (QTE) ----------

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
                paso = Paso.EsperandoEspacio;    // falta que la TV diga "cabina de abajo lista"
                ViajeBloqueado = false;
                break;

            case Paso.EventoEngrase:
                paso = Paso.EsperandoPartida2;   // ESPACIO otra vez para partir
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
            AlTerminarEvento(null);   // se salta este paso para no dejar el tutorial colgado
        }
    }

    // ---------- Momentos del jugador ----------

    // El jugador pulsó W (se acepta solo si no está bloqueada)
    private void AlPulsarW()
    {
        if (paso != Paso.EsperandoW) return;

        paso = Paso.EventoPestillo;
        Lanzar<EventoPestillo>();
    }

    // Lo llama ProcesoPartida cuando el jugador pulsa ESPACIO con la cabina lista.
    // Devuelve true si el tutorial lo usa para mostrar el engrase (el ascensor no parte todavía).
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

    // ---------- Fin ----------

    private void Terminar()
    {
        paso = Paso.Fin;
        ViajeBloqueado = false;
        EnCurso = false;

        if (estadoManager != null) estadoManager.enabled = true;
        if (eventManager != null) eventManager.eventosActivos = true;   // el Director programa el resto del día

        PlayerPrefs.SetInt("TutorialHecho", 1);
        PlayerPrefs.Save();
    }

    private void AlTerminarDia()
    {
        // Por si el día se acabó antes de terminar el tutorial
        PlayerPrefs.SetInt("TutorialHecho", 1);
        PlayerPrefs.Save();
    }

    // ---------- Utilidades ----------

    private bool HayDialogoActivo()
    {
        return DialogoManager.Instance != null && DialogoManager.Instance.HayDialogoActivo();
    }

    // Muestra un diálogo. Si no existe en el JSON, se salta para no dejar el tutorial colgado.
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