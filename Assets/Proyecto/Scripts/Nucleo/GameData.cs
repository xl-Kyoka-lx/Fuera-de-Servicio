using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Datos", menuName = "GameData")]

[System.Serializable]
public class EstadoTienda
{
    public List<string> mejorasCompradas = new List<string>();
    public List<string> consumiblesIds = new List<string>();
    public List<int> consumiblesCantidades = new List<int>();

    public int ObtenerCantidad(string id)
    {
        int i = consumiblesIds.IndexOf(id);
        return i >= 0 && i < consumiblesCantidades.Count ? consumiblesCantidades[i] : 0;
    }

    public void SumarCantidad(string id, int cantidad)
    {
        int i = consumiblesIds.IndexOf(id);
        if (i < 0 || i >= consumiblesCantidades.Count)
        {
            while (consumiblesCantidades.Count < consumiblesIds.Count) consumiblesCantidades.Add(0);
            if (i < 0)
            {
                consumiblesIds.Add(id);
                consumiblesCantidades.Add(0);
                i = consumiblesIds.Count - 1;
            }
        }
        consumiblesCantidades[i] = Mathf.Max(0, consumiblesCantidades[i] + cantidad);
    }
}

public class GameData : ScriptableObject
{
    public int dineroTotal = 0;

    [Range(0, 100)] public float contadorPopularidad = 50f;

    [Range(0, 100)] public float contadorLimpieza = 100f;
    [Range(0, 100)] public float contadorGrasa = 100f;

    [Header("Valores de una partida nueva")]
    [Tooltip("Con lo que empieza el mantenimiento al darle Nueva partida (no depende de lo que haya quedado guardado)")]
    [Range(0, 100)] public float mantenimientoPartidaNueva = 100f;
    [Range(0, 100)] public float limpiezaPartidaNueva = 100f;

    [Tooltip("Pago base de cada pasajero (feliz puede pagar el doble, enojado la mitad o nada)")]
    public int pagoPorPasajero = 10;

    public int horaActual = 7;

    [Tooltip("Día de la partida, empieza en 1. Cada 7 días es una semana")]
    public int diaActual = 1;
    public int Semana => (diaActual - 1) / 7 + 1;
    public int DiaDeLaSemana => (diaActual - 1) % 7 + 1;

    [Header("Cobros de la semana (se calculan al empezar cada semana)")]
    public int semanaCobrosCalculada = 0;
    public int cobroElectricidad = 0;
    public int cobroDeudas = 0;
    public int cobroMultas = 0;

    [Header("Estadísticas de la partida")]
    public int dineroRecaudadoTotal = 0;
    public float popularidadMinima = 50f;
    public int maxGanadoEnUnDia = 0;
    public int maxPerdidoEnUnDia = 0;
    public int ganadoHoy = 0;
    public int perdidoHoy = 0;

    public void RegistrarIngreso(int cantidad)
    {
        if (cantidad <= 0) return;
        ganadoHoy += cantidad;
        dineroRecaudadoTotal += cantidad;
    }

    public void RegistrarGasto(int cantidad)
    {
        if (cantidad > 0) perdidoHoy += cantidad;
    }

    public void RegistrarPopularidad()
    {
        popularidadMinima = Mathf.Min(popularidadMinima, contadorPopularidad);
    }

    public void CerrarEstadisticasDelDia()
    {
        maxGanadoEnUnDia = Mathf.Max(maxGanadoEnUnDia, ganadoHoy);
        maxPerdidoEnUnDia = Mathf.Max(maxPerdidoEnUnDia, perdidoHoy);
        ganadoHoy = 0;
        perdidoHoy = 0;
    }

    public bool eventoOcurriendo = false;
    public bool ascensorEnMovimiento = false;

    public bool resumenNocheMostrado = false;

    [System.NonSerialized] public float mantenimientoDelDia = 100f;
    [System.NonSerialized] public float limpiezaDelDia = 100f;

    [System.NonSerialized] public bool ascensorAveriado = false;

    [System.NonSerialized] public int pasajerosParaLimpieza = 0;
    [System.NonSerialized] public int pasajerosParaMantenimiento = 0;

    public void ActualizarPopularidad()
    {
        contadorPopularidad = Mathf.Clamp(contadorPopularidad, 0f, 100f);
    }

    public void FijarValoresDelDia()
    {
        ActualizarPopularidad();
        mantenimientoDelDia = Mathf.Clamp(contadorGrasa, 1f, 100f);
        limpiezaDelDia = Mathf.Clamp(contadorLimpieza, 0f, 100f);
    }

    public EstadoTienda estadoTienda = new EstadoTienda();

    public System.Action OnDataChanged;

    [System.NonSerialized] private bool valoresInicialesCapturados = false;
    [System.NonSerialized] private int _dineroTotalInicial;
    [System.NonSerialized] private float _popularidadInicial;
    [System.NonSerialized] private float _limpiezaInicial;
    [System.NonSerialized] private float _poleaInicial;
    [System.NonSerialized] private int _horaInicial;

    public void CapturarValoresIniciales()
    {
        _dineroTotalInicial = dineroTotal;
        _popularidadInicial = contadorPopularidad;
        _limpiezaInicial = contadorLimpieza;
        _poleaInicial = contadorGrasa;
        _horaInicial = horaActual;

        valoresInicialesCapturados = true;
    }

    public void ResetData()
    {
        if (!valoresInicialesCapturados)
            CapturarValoresIniciales();

        dineroTotal = _dineroTotalInicial;
        contadorPopularidad = _popularidadInicial;
        contadorLimpieza = limpiezaPartidaNueva;
        contadorGrasa = mantenimientoPartidaNueva;
        horaActual = _horaInicial;
        diaActual = 1;
        semanaCobrosCalculada = 0;
        cobroElectricidad = cobroDeudas = cobroMultas = 0;
        dineroRecaudadoTotal = maxGanadoEnUnDia = maxPerdidoEnUnDia = ganadoHoy = perdidoHoy = 0;
        ActualizarPopularidad();
        popularidadMinima = contadorPopularidad;

        eventoOcurriendo = false;
        ascensorEnMovimiento = false;
        ascensorAveriado = false;
        pasajerosParaLimpieza = 0;
        pasajerosParaMantenimiento = 0;

        estadoTienda = new EstadoTienda();

        OnDataChanged?.Invoke();
    }
}
