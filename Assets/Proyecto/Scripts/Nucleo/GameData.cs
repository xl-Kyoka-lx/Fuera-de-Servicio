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
            // Listas desparejas (datos viejos): se reparan antes de agregar
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
        ActualizarPopularidad();

        eventoOcurriendo = false;
        ascensorEnMovimiento = false;
        ascensorAveriado = false;
        pasajerosParaLimpieza = 0;
        pasajerosParaMantenimiento = 0;

        // Partida nueva: sin mejoras ni consumibles de la tienda
        estadoTienda = new EstadoTienda();

        OnDataChanged?.Invoke();

        
    }
}

