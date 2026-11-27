using UnityEngine;

public class EstadisticasDePrueba : MonoBehaviour
{
    [Tooltip("Marcada: se aplican los valores de abajo (y cada cambio se aplica al instante). Desmarcada: todo vuelve a como estaba. Solo funciona en Play")]
    public bool establecerEstadisticasDePrueba = false;

    [Header("Valores actuales del juego (solo mirar)")]
    [SoloLectura] public int dineroActual;
    [SoloLectura] public float popularidadActual;
    [SoloLectura] public float mantenimientoActual;
    [SoloLectura] public float limpiezaActual;
    [SoloLectura] public int horaActual;
    [SoloLectura] public int diaActual;
    [SoloLectura] public int semanaActual;
    [SoloLectura] public int kitsDeLimpiezaActuales;
    [SoloLectura] public bool tieneFrenoReforzadoActual;
    [SoloLectura] public bool tieneMallaProtectoraActual;
    [SoloLectura] public int cobroDeHoy;
    [SoloLectura] public int dineroRecaudadoTotalActual;
    [SoloLectura] public float popularidadMinimaActual;
    [SoloLectura] public int maxGanadoEnUnDiaActual;
    [SoloLectura] public int maxPerdidoEnUnDiaActual;

    [Header("Estado del ascensor (prueba)")]
    public int dinero = 10000;
    [Range(0, 100)] public float popularidad = 50f;
    [Range(0, 100)] public float mantenimiento = 100f;
    [Range(0, 100)] public float limpieza = 100f;

    [Header("Tiempo")]
    [Range(7, 21)] public int hora = 7;
    [Tooltip("Semana = (día - 1) / 7 + 1. Día 8 = primer día de la semana 2")]
    [Min(1)] public int dia = 1;

    [Header("Tienda")]
    [Min(0)] public int kitsDeLimpieza = 0;
    public bool tieneFrenoReforzado = false;
    public bool tieneMallaProtectora = false;

    [Header("Estadísticas de la partida (pantalla Fuera de Servicio)")]
    public int dineroRecaudadoTotal = 0;
    [Range(0, 100)] public float popularidadMinima = 50f;
    public int maxGanadoEnUnDia = 0;
    public int maxPerdidoEnUnDia = 0;

    private bool activo;
    private string respaldo;
    private float respaldoMantenimientoDelDia;
    private float respaldoLimpiezaDelDia;
    private int respaldoHoraReloj;

    private GameData Data =>
        GameManager.Instance != null ? GameManager.Instance.GameData : null;

    private void Start()
    {
        Sincronizar();
    }

    private void Update()
    {
        var d = Data;
        if (d == null) return;

        dineroActual = d.dineroTotal;
        popularidadActual = d.contadorPopularidad;
        mantenimientoActual = d.contadorGrasa;
        limpiezaActual = d.contadorLimpieza;
        var reloj = FindAnyObjectByType<ClockManager>();
        horaActual = reloj != null ? reloj.horaActual : d.horaActual;
        diaActual = d.diaActual;
        semanaActual = d.Semana;
        kitsDeLimpiezaActuales = EfectosManager.Stock("kit_limpieza");
        tieneFrenoReforzadoActual = EfectosManager.TieneMejora("frenos");
        tieneMallaProtectoraActual = EfectosManager.TieneMejora("malla");
        var c = Economia.Vistazo(d);
        cobroDeHoy = c.Neto;
        dineroRecaudadoTotalActual = d.dineroRecaudadoTotal;
        popularidadMinimaActual = d.popularidadMinima;
        maxGanadoEnUnDiaActual = d.maxGanadoEnUnDia;
        maxPerdidoEnUnDiaActual = d.maxPerdidoEnUnDia;
    }

    private void OnValidate()
    {
        if (Application.isPlaying) Sincronizar();
    }

    private void OnDisable()
    {
        if (activo) Restaurar();
    }

    private void Sincronizar()
    {
        if (Data == null) return;

        if (establecerEstadisticasDePrueba)
        {
            if (!activo) Respaldar();
            Aplicar();
        }
        else if (activo)
        {
            Restaurar();
        }
    }

    private void Respaldar()
    {
        var d = Data;
        respaldo = JsonUtility.ToJson(d);
        respaldoMantenimientoDelDia = d.mantenimientoDelDia;
        respaldoLimpiezaDelDia = d.limpiezaDelDia;

        var reloj = FindAnyObjectByType<ClockManager>();
        respaldoHoraReloj = reloj != null ? reloj.horaActual : d.horaActual;

        activo = true;
    }

    private void Aplicar()
    {
        var d = Data;

        d.dineroTotal = dinero;
        d.contadorPopularidad = popularidad;
        d.contadorGrasa = mantenimiento;
        d.contadorLimpieza = limpieza;
        d.mantenimientoDelDia = Mathf.Clamp(mantenimiento, 1f, 100f);
        d.limpiezaDelDia = limpieza;

        d.horaActual = hora;
        var reloj = FindAnyObjectByType<ClockManager>();
        if (reloj != null) reloj.horaActual = hora;

        if (d.diaActual != dia)
        {
            d.diaActual = dia;
            d.semanaCobrosCalculada = 0;
        }

        if (d.estadoTienda == null) d.estadoTienda = new EstadoTienda();
        d.estadoTienda.SumarCantidad("kit_limpieza", kitsDeLimpieza - d.estadoTienda.ObtenerCantidad("kit_limpieza"));
        PonerMejora(d, "frenos", tieneFrenoReforzado);
        PonerMejora(d, "malla", tieneMallaProtectora);

        d.dineroRecaudadoTotal = dineroRecaudadoTotal;
        d.popularidadMinima = popularidadMinima;
        d.maxGanadoEnUnDia = maxGanadoEnUnDia;
        d.maxPerdidoEnUnDia = maxPerdidoEnUnDia;

        d.OnDataChanged?.Invoke();
    }

    private void Restaurar()
    {
        var d = Data;
        activo = false;
        if (d == null || respaldo == null) return;

        JsonUtility.FromJsonOverwrite(respaldo, d);
        d.mantenimientoDelDia = respaldoMantenimientoDelDia;
        d.limpiezaDelDia = respaldoLimpiezaDelDia;

        var reloj = FindAnyObjectByType<ClockManager>();
        if (reloj != null) reloj.horaActual = respaldoHoraReloj;

        d.OnDataChanged?.Invoke();
    }

    private static void PonerMejora(GameData d, string id, bool tener)
    {
        var lista = d.estadoTienda.mejorasCompradas;
        if (tener && !lista.Contains(id)) lista.Add(id);
        if (!tener) lista.Remove(id);
    }

    [ContextMenu("Copiar valores actuales del juego")]
    private void CopiarValoresActuales()
    {
        var d = Data;
        if (d == null) return;

        dinero = d.dineroTotal;
        popularidad = d.contadorPopularidad;
        mantenimiento = d.contadorGrasa;
        limpieza = d.contadorLimpieza;
        hora = Mathf.Clamp(d.horaActual, 7, 21);
        dia = d.diaActual;
        kitsDeLimpieza = d.estadoTienda != null ? d.estadoTienda.ObtenerCantidad("kit_limpieza") : 0;
        tieneFrenoReforzado = EfectosManager.TieneMejora("frenos");
        tieneMallaProtectora = EfectosManager.TieneMejora("malla");
        dineroRecaudadoTotal = d.dineroRecaudadoTotal;
        popularidadMinima = d.popularidadMinima;
        maxGanadoEnUnDia = d.maxGanadoEnUnDia;
        maxPerdidoEnUnDia = d.maxPerdidoEnUnDia;
    }

    [ContextMenu("Mostrar pantalla Fuera de Servicio")]
    private void ProbarPantallaDerrota()
    {
        if (Application.isPlaying) PantallaFueraDeServicio.Mostrar();
    }
}
