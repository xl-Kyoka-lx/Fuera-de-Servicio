using UnityEngine;

[CreateAssetMenu(fileName = "ResumenDiaData", menuName = "Ascensor/Resumen Día")]
public class ResumenDiaData : ScriptableObject
{
    [Header("=== SNAPSHOT DEL INICIO ===")]
    public int dineroInicioDia;
    public float limpiezaInicioDia;
    public float poleaInicioDia;

    [Header("=== CONTADORES DEL DÍA ===")]
    public int eventosOcurridos;
    public int pasajerosTotalesTransportados;

    [Header("=== SNAPSHOT DEL FINAL ===")]
    public int dineroFinDia;

    [Header("=== PROPIEDADES CALCULADAS ===")]
    public int DineroGanado => dineroFinDia - dineroInicioDia;
    public int PasajerosTransportados => pasajerosTotalesTransportados;

    public void IniciarDia(int dineroActual, float limpieza, float polea)
    {
        dineroInicioDia = dineroActual;
        limpiezaInicioDia = limpieza;
        poleaInicioDia = polea;

        eventosOcurridos = 0;
        pasajerosTotalesTransportados = 0;

        dineroFinDia = dineroActual;

        Debug.Log($"📊 Resumen del día iniciado. Dinero inicial: ${dineroInicioDia}");
    }

    public void TerminarDia(int dineroActual)
    {
        dineroFinDia = dineroActual;

        Debug.Log($"📊 Resumen cerrado. Dinero: ${dineroInicioDia} → ${dineroFinDia}. Ganancia: ${DineroGanado}");
    }

    public void RegistrarEvento() { eventosOcurridos++; }
    public void RegistrarPasajeroTransportado() { pasajerosTotalesTransportados++; }

    public void Resetear()
    {
        dineroInicioDia = 0;
        limpiezaInicioDia = 100;
        poleaInicioDia = 100;

        eventosOcurridos = 0;
        pasajerosTotalesTransportados = 0;

        dineroFinDia = 0;

        Debug.Log("📊 Resumen reseteado");
    }
}