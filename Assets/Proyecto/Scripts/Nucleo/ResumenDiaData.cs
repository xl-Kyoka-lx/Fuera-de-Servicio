using UnityEngine;

[CreateAssetMenu(fileName = "ResumenDiaData", menuName = "Ascensor/Resumen Día")]
public class ResumenDiaData : ScriptableObject
{
    [Header("=== SNAPSHOT DEL INICIO ===")]
    public int dineroInicioDia;
    public int pasajerosInicioDia;
    public float limpiezaInicioDia;
    public float poleaInicioDia;
    public float puertaInicioDia;

    [Header("=== CONTADORES DEL DÍA ===")]
    public int eventosOcurridos;
    public int basuraTirada;
    public int grafitisHechos;
    public int pasajerosTotalesTransportados;

    [Header("=== SNAPSHOT DEL FINAL ===")]
    public int dineroFinDia;
    public int pasajerosFinDia;

    [Header("=== PROPIEDADES CALCULADAS ===")]
    public int DineroGanado => dineroFinDia - dineroInicioDia;
    public int PasajerosTransportados => pasajerosTotalesTransportados;

    // === MÉTODOS ===
    public void IniciarDia(int dineroActual, int pasajerosActuales,
                            float limpieza, float polea, float puerta)
    {
        dineroInicioDia = dineroActual;
        pasajerosInicioDia = pasajerosActuales;
        limpiezaInicioDia = limpieza;
        poleaInicioDia = polea;
        puertaInicioDia = puerta;

        eventosOcurridos = 0;
        basuraTirada = 0;
        grafitisHechos = 0;
        pasajerosTotalesTransportados = 0;

        dineroFinDia = dineroActual;
        pasajerosFinDia = pasajerosActuales;

        Debug.Log($"📊 Resumen del día iniciado. Dinero inicial: ${dineroInicioDia}");
    }

    public void TerminarDia(int dineroActual, int pasajerosActuales)
    {
        dineroFinDia = dineroActual;
        pasajerosFinDia = pasajerosActuales;

        Debug.Log($"📊 Resumen cerrado. Dinero: ${dineroInicioDia} → ${dineroFinDia}. Ganancia: ${DineroGanado}");
    }

    public void RegistrarEvento() { eventosOcurridos++; }
    public void RegistrarBasura() { basuraTirada++; }
    public void RegistrarGrafiti() { grafitisHechos++; }
    public void RegistrarPasajeroTransportado() { pasajerosTotalesTransportados++; }

    // 🔑 Reset completo (para nueva partida)
    public void Resetear()
    {
        dineroInicioDia = 0;
        pasajerosInicioDia = 0;
        limpiezaInicioDia = 100;
        poleaInicioDia = 100;
        puertaInicioDia = 100;

        eventosOcurridos = 0;
        basuraTirada = 0;
        grafitisHechos = 0;
        pasajerosTotalesTransportados = 0;

        dineroFinDia = 0;
        pasajerosFinDia = 0;

        Debug.Log("📊 Resumen reseteado");
    }
}