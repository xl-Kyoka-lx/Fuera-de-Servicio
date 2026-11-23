using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    // Mientras sea true, ESPACIO no inicia viajes (lo lee PalancaController)
    public static bool ViajeBloqueado = false;

    public AscensorManager ascensorManager;
    public EventManager eventManager;
    public EventoEstadoManager estadoManager;
    public ClockManager clockManager;

    private enum Paso { Inactivo, Controles, EsperandoViaje, MensajeViaje, PrimerViaje, MensajePuerta, EventoPuerta, Fin }
    private Paso paso = Paso.Inactivo;

    private void Start()
    {
        ViajeBloqueado = false;

        if (PlayerPrefs.GetInt("TutorialHecho", 0) == 1)
        {
            enabled = false;
            return;
        }

        ViajeBloqueado = true;
        if (eventManager != null) eventManager.eventosActivos = false;
        if (estadoManager != null) estadoManager.enabled = false;   // tiene su propio Update

        if (DialogoManager.Instance != null)
            DialogoManager.Instance.OnDialogoTerminado += AlTerminarDialogo;
        if (clockManager != null)
            clockManager.OnDiaTerminado += AlTerminarDia;

        Invoke(nameof(Empezar), 0.5f);
    }

    private void OnDestroy()
    {
        ViajeBloqueado = false;

        if (DialogoManager.Instance != null)
            DialogoManager.Instance.OnDialogoTerminado -= AlTerminarDialogo;
        if (clockManager != null)
            clockManager.OnDiaTerminado -= AlTerminarDia;
        if (estadoManager != null)
            estadoManager.OnEventoTerminado -= AlTerminarEventoPuerta;
    }

    private void Empezar()
    {
        paso = Paso.Controles;
        DialogoManager.Instance.MostrarDialogo("tutorial_controles");
    }

    private void Update()
    {
        if (paso == Paso.EsperandoViaje && ascensorManager.enMovimiento)
        {
            paso = Paso.MensajeViaje;
            DialogoManager.Instance.MostrarDialogo("tutorial_viaje");
        }
        else if (paso == Paso.PrimerViaje && !ascensorManager.enMovimiento)
        {
            // el primer viaje terminó: aviso de la puerta antes del primer evento
            paso = Paso.MensajePuerta;
            ViajeBloqueado = true;
            DialogoManager.Instance.MostrarDialogo("tutorial_puerta");
        }
    }

    private void AlTerminarDialogo(string id)
    {
        if (id == "tutorial_controles")
        {
            paso = Paso.EsperandoViaje;
            ViajeBloqueado = false;          // ahora sí puede presionar ESPACIO
        }
        else if (id == "tutorial_viaje")
        {
            paso = Paso.PrimerViaje;
        }
        else if (id == "tutorial_puerta")
        {
            paso = Paso.EventoPuerta;
            if (estadoManager != null)
            {
                estadoManager.enabled = true;
                estadoManager.OnEventoTerminado += AlTerminarEventoPuerta;

                if (!estadoManager.ForzarEvento(TipoSistema.Puerta))
                {
                    Debug.LogWarning("Tutorial: no se pudo lanzar el evento de la puerta. Revisa la lista del EventoEstadoManager.");
                    AlTerminarEventoPuerta(null);
                }
            }
        }
    }

    private void AlTerminarEventoPuerta(GameObject evento)
    {
        if (estadoManager != null)
            estadoManager.OnEventoTerminado -= AlTerminarEventoPuerta;

        paso = Paso.Fin;
        ViajeBloqueado = false;
        if (eventManager != null) eventManager.eventosActivos = true;   // los eventos se activan
    }

    private void AlTerminarDia()
    {
        PlayerPrefs.SetInt("TutorialHecho", 1);
        PlayerPrefs.Save();
    }
}