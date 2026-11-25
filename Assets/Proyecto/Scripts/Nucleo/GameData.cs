using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Datos", menuName = "GameData")]



[System.Serializable]
public class EstadoTienda
{
    public List<string> mejorasCompradas = new List<string>();
    public List<string> consumiblesIds = new List<string>();
    public List<int> consumiblesCantidades = new List<int>();
}

public class GameData : ScriptableObject
{
    public int pasajerosTotales = 0;
    public int dineroTotal = 0;
    public int capacidadMaxima = 10;

    [Range(0, 100)] public float contadorPopularidad = 50f;

    [Range(0, 100)] public float contadorLimpieza = 100f;
    [Range(0, 100)] public float contadorGrasa = 100f;
    [Range(0, 100)] public float contadorPuerta = 100f;

    [Header("Valores de una partida nueva")]
    [Tooltip("Con lo que empieza el mantenimiento al darle Nueva partida (no depende de lo que haya quedado guardado)")]
    [Range(0, 100)] public float mantenimientoPartidaNueva = 100f;
    [Range(0, 100)] public float limpiezaPartidaNueva = 100f;
    [Range(0, 100)] public float puertaPartidaNueva = 100f;

   
    public float spawnBase = 2f;
    public int pagoPorPasajero = 10;
    [Range(0f, 1f)] public float probabilidadPago = 0.5f;

   
    public int horaActual = 8;
    public ClimaEnum climaActual = ClimaEnum.Soleado;


    public bool eventoOcurriendo = false;
    public bool ascensorEnMovimiento = false;

    public bool resumenNocheMostrado = false;

    // Valores con los que se calculan los efectos del día. Se fijan al cargar la escena:
    // los cambios que ocurran durante el día solo cuentan para el día siguiente.
    [System.NonSerialized] public float mantenimientoDelDia = 100f;
    [System.NonSerialized] public float limpiezaDelDia = 100f;

    // Verdadero cuando el mantenimiento llegó a 0 jugando: el ascensor ya no arranca
    [System.NonSerialized] public bool ascensorAveriado = false;

    // Pasajeros transportados desde el último descuento de limpieza y de mantenimiento
    [System.NonSerialized] public int pasajerosParaLimpieza = 0;
    [System.NonSerialized] public int pasajerosParaMantenimiento = 0;

    // La popularidad ya no se calcula: se mueve con GameManager.ModificarPopularidad.
    // Aquí solo se mantiene entre 0 y 100.
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

    public bool HayEspacio => pasajerosTotales < capacidadMaxima;
    public bool EstaVacio => pasajerosTotales == 0;

    

    public EstadoTienda estadoTienda = new EstadoTienda();


    public System.Action OnDataChanged;

    [System.NonSerialized] private bool valoresInicialesCapturados = false;
    [System.NonSerialized] private int _pasajerosTotalesInicial;
    [System.NonSerialized] private int _dineroTotalInicial;
    [System.NonSerialized] private float _popularidadInicial;
    [System.NonSerialized] private float _limpiezaInicial;
    [System.NonSerialized] private float _poleaInicial;
    [System.NonSerialized] private float _puertaInicial;
    [System.NonSerialized] private int _horaInicial;
    [System.NonSerialized] private ClimaEnum _climaInicial;

  
    public void CapturarValoresIniciales()
    {
        _pasajerosTotalesInicial = pasajerosTotales;
        _dineroTotalInicial = dineroTotal;
        _popularidadInicial = contadorPopularidad;
        _limpiezaInicial = contadorLimpieza;
        _poleaInicial = contadorGrasa;
        _puertaInicial = contadorPuerta;
        _horaInicial = horaActual;
        _climaInicial = climaActual;

        valoresInicialesCapturados = true;

    }
    public void SetGrasa(float nuevoValor)
    {
        contadorGrasa = Mathf.Clamp(nuevoValor, 0f, 100f);
        ActualizarPopularidad();
        OnDataChanged?.Invoke();
    }

    public void ResetData()
    {
        if (!valoresInicialesCapturados)
            CapturarValoresIniciales();

        pasajerosTotales = _pasajerosTotalesInicial;
        dineroTotal = _dineroTotalInicial;
        contadorPopularidad = _popularidadInicial;
        contadorLimpieza = limpiezaPartidaNueva;
        contadorGrasa = mantenimientoPartidaNueva;
        contadorPuerta = puertaPartidaNueva;
        horaActual = _horaInicial;
        climaActual = _climaInicial;
        ActualizarPopularidad();

        eventoOcurriendo = false;
        ascensorEnMovimiento = false;
        ascensorAveriado = false;
        pasajerosParaLimpieza = 0;
        pasajerosParaMantenimiento = 0;

        OnDataChanged?.Invoke();

        
    }
}

public enum ClimaEnum
{
    Soleado,
    Lluvioso,
    Ventoso,
    Neblinoso
}