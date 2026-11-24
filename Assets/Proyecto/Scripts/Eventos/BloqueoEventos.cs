using UnityEngine;

// Bloqueos temporales compartidos entre los eventos, el frenado y la palanca.
public static class BloqueoEventos
{
    private static float eventosBloqueadosHasta = 0f;
    private static float arranqueBloqueadoHasta = 0f;

    // Verdadero mientras no deben lanzarse eventos (por ejemplo, durante el frenado)
    public static bool EventosBloqueados => Time.unscaledTime < eventosBloqueadosHasta;

    // Verdadero mientras Espacio no debe arrancar el ascensor (justo después de un evento)
    public static bool ArranqueBloqueado => Time.unscaledTime < arranqueBloqueadoHasta;

    public static void BloquearEventos(float segundos)
    {
        eventosBloqueadosHasta = Mathf.Max(eventosBloqueadosHasta, Time.unscaledTime + segundos);
    }

    public static void BloquearArranque(float segundos)
    {
        arranqueBloqueadoHasta = Mathf.Max(arranqueBloqueadoHasta, Time.unscaledTime + segundos);
    }

    // Evita que valores de una partida anterior queden pegados al entrar en Play
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar()
    {
        eventosBloqueadosHasta = 0f;
        arranqueBloqueadoHasta = 0f;
    }
}