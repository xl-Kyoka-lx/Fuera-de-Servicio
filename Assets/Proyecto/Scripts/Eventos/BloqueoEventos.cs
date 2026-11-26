using UnityEngine;

public static class BloqueoEventos
{
    private static float eventosBloqueadosHasta = 0f;
    private static float arranqueBloqueadoHasta = 0f;

    public static bool EventosBloqueados => Time.unscaledTime < eventosBloqueadosHasta;

    public static bool ArranqueBloqueado => Time.unscaledTime < arranqueBloqueadoHasta;

    public static void BloquearEventos(float segundos)
    {
        eventosBloqueadosHasta = Mathf.Max(eventosBloqueadosHasta, Time.unscaledTime + segundos);
    }

    public static void BloquearArranque(float segundos)
    {
        arranqueBloqueadoHasta = Mathf.Max(arranqueBloqueadoHasta, Time.unscaledTime + segundos);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar()
    {
        eventosBloqueadosHasta = 0f;
        arranqueBloqueadoHasta = 0f;
    }
}