using UnityEngine;
using System.Collections.Generic;

public class PasajeroSpawner : MonoBehaviour
{
    public static PasajeroSpawner Instance { get; private set; }

    public enum DireccionFlujo { Mixta, Bajada, Subida }

    [System.Serializable]
    public class FranjaHoraria
    {
        public string nombre;
        [Tooltip("Hora de inicio con decimales (7.5 = 07:30)")]
        public float desde;
        [Tooltip("Hora de término con decimales (9.5 = 09:30)")]
        public float hasta;
        [Tooltip("Multiplica la frecuencia de llegada. 1 = normal, 2 = el doble")]
        public float intensidad = 1f;
        public DireccionFlujo direccion = DireccionFlujo.Mixta;
    }

    [System.Serializable]
    public class SpawnConfig
    {
        [Tooltip("Nombre descriptivo")]
        public string nombre;

        [Tooltip("Primer puesto de la fila (el más cercano a la cabina)")]
        public Transform spawnPoint;

        [Tooltip("Índice del ascensor al que pertenece")]
        public int ascensorIndex = 0;

        [Tooltip("Rol del pasajero")]
        public RolPasajero rol;

        [Tooltip("(Ya no se usa: los pasajeros hacen fila)")]
        public float radioCumulo = 0.5f;

        [Tooltip("Escala de los pasajeros de este andén")]
        public float escalaEnSpawn = 1f;

        [Header("=== FILA ===")]
        [Tooltip("Distancia entre un pasajero y el siguiente. 0 = automático según el tamaño del pasajero")]
        public float espacioEnFila = 0f;
        [Tooltip("Marcar si la fila crece hacia el lado equivocado (por defecto se aleja de la cabina)")]
        public bool invertirFila = false;
    }

    [Header("=== REFERENCIAS ===")]
    public GameObject pasajeroPrefab;
    public AscensorManager ascensorManager;
    [Tooltip("Si se deja vacío, se busca solo en la escena")]
    public ClockManager clockManager;

    [Header("=== CONFIGURACIONES DE SPAWN ===")]
    public List<SpawnConfig> spawns = new List<SpawnConfig>();

    [Header("=== CONFIGURACIÓN ===")]
    public float intervaloSpawn = 2f;
    public int maxPasajerosPorCumulo = 5;
    [Tooltip("Máximo de pasajeros en la fila de un andén")]
    public int maxEsperandoPorAnden = 10;
    public bool spawnAutomatico = true;

    [Tooltip("Si está marcado, mientras el ascensor viaja los pasajeros aparecen en el lado al que va a llegar. Si no, solo aparecen con el ascensor detenido")]
    public bool spawnMientrasViaja = true;

    [Header("=== HORARIO ===")]
    [Tooltip("Desmarcado = llegan siempre igual, sin hora punta")]
    public bool usarHorario = true;
    [Tooltip("Intensidad en las horas que no están en ninguna franja (horas valle)")]
    public float intensidadFueraDeFranja = 0.4f;
    [Tooltip("En una franja de Bajada o Subida, probabilidad de que el grupo llegue a la estación dominante")]
    [Range(0.5f, 1f)] public float pesoDireccionDominante = 0.85f;
    public List<FranjaHoraria> horario = new List<FranjaHoraria>
    {
        new FranjaHoraria { nombre = "Peak mañana (cerro al plan)",  desde = 7.5f,  hasta = 9.5f,  intensidad = 2.0f, direccion = DireccionFlujo.Bajada },
        new FranjaHoraria { nombre = "Mediodía (compras, trámites)", desde = 12f,   hasta = 14.5f, intensidad = 1.4f, direccion = DireccionFlujo.Mixta },
        new FranjaHoraria { nombre = "Tarde (colegios, turistas)",   desde = 15.5f, hasta = 17.5f, intensidad = 1.4f, direccion = DireccionFlujo.Subida },
        new FranjaHoraria { nombre = "Peak tarde (regreso laboral)", desde = 18f,   hasta = 20.5f, intensidad = 2.0f, direccion = DireccionFlujo.Subida },
    };

    [Header("=== MOVIMIENTO ===")]
    [Tooltip("Puestos de la fila que avanza un pasajero por segundo")]
    public float puestosPorSegundo = 3f;
    [Tooltip("Desde cuántos puestos más allá del final de la fila entran caminando (y hasta dónde se van)")]
    public float puestosDeEntrada = 4f;
    [Tooltip("Segundos que pasan fuera de pantalla antes de entrar a la fila de la otra cabina")]
    public float segundosFueraAlCambiar = 3f;
    [Tooltip("Separación entre pasajeros respecto a su ancho (1 = pegados)")]
    public float separacionRelativa = 0.9f;

    private float timer = 0f;
    private bool diaCerrado = false;
    private float anchoPasajeroBase = 1f;

    private readonly Dictionary<SpawnConfig, List<Pasajero>> filas = new Dictionary<SpawnConfig, List<Pasajero>>();
    private readonly List<Pasajero> aMover = new List<Pasajero>();

    private class Transito
    {
        public Pasajero pasajero;
        public bool afuera;
        public float espera;
    }
    private readonly List<Transito> enTransito = new List<Transito>();

    private void Awake()
    {
        Instance = this;
    }

    [System.Obsolete]
    private void Start()
    {
        if (clockManager == null) clockManager = FindFirstObjectByType<ClockManager>();
        if (clockManager != null)
        {
            clockManager.OnCierreDelDia += CerrarDia;
            clockManager.OnDiaTerminado += CerrarDia;
        }

        if (pasajeroPrefab != null)
        {
            var sr = pasajeroPrefab.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null) anchoPasajeroBase = sr.sprite.bounds.size.x;
        }

        foreach (var c in spawns)
            if (c != null) filas[c] = new List<Pasajero>();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

        if (clockManager != null)
        {
            clockManager.OnCierreDelDia -= CerrarDia;
            clockManager.OnDiaTerminado -= CerrarDia;
        }
    }

    private void Update()
    {
        if (ascensorManager == null || spawns.Count == 0) return;
        if (diaCerrado) return;

        LimpiarFilas();
        ReubicarPasajeros();
        ProcesarTransito();
        OrdenarFilas();

        if (!spawnAutomatico) return;

        timer += Time.deltaTime * Mathf.Max(0f, FranjaActual(out _));
        if (timer >= intervaloSpawn)
        {
            timer = 0f;
            SpawnearCumulo();
        }
    }

    private float FranjaActual(out DireccionFlujo direccion)
    {
        direccion = DireccionFlujo.Mixta;
        if (!usarHorario || clockManager == null) return 1f;

        float hora = clockManager.HoraDecimal;
        foreach (var f in horario)
        {
            if (f != null && hora >= f.desde && hora < f.hasta)
            {
                direccion = f.direccion;
                return f.intensidad;
            }
        }
        return intensidadFueraDeFranja;
    }

    private float PesoDireccion(SpawnConfig c, DireccionFlujo direccion)
    {
        if (direccion == DireccionFlujo.Mixta) return 1f;

        bool esDominante = (direccion == DireccionFlujo.Bajada) == !EstacionAbajo(c.rol);
        return esDominante ? pesoDireccionDominante : 1f - pesoDireccionDominante;
    }

    private static bool EstacionAbajo(RolPasajero rol) => rol == RolPasajero.SubeEnInicio;

    private int CabinaQueAtiende(RolPasajero rol)
    {
        bool abajo = EstacionAbajo(rol);
        int laOtra = -1;

        for (int i = 0; i < ascensorManager.ascensores.Count; i++)
        {
            var asc = ascensorManager.ascensores[i];

            bool ladoAbajo = asc.enMovimiento ? !asc.estaAbajo : asc.estaAbajo;

            if (ladoAbajo != abajo)
            {
                if (!asc.enMovimiento && laOtra < 0) laOtra = i;
                continue;
            }

            if (asc.enMovimiento) return i;
            if (!PuertaCerrada(i)) return i;
        }

        return laOtra;
    }

    private bool PuertaCerrada(int indice)
    {
        return ProcesoPartida.Instance != null && ProcesoPartida.Instance.PuertaCerrada(indice);
    }

    private SpawnConfig AndenDe(int ascensorIndex, RolPasajero rol)
    {
        foreach (var c in spawns)
            if (c != null && c.spawnPoint != null && c.ascensorIndex == ascensorIndex && c.rol == rol) return c;
        return null;
    }

    private List<Pasajero> FilaDe(SpawnConfig c)
    {
        if (!filas.TryGetValue(c, out var fila))
        {
            fila = new List<Pasajero>();
            filas[c] = fila;
        }
        return fila;
    }

    private SpawnConfig AndenDelPasajero(Pasajero p)
    {
        foreach (var par in filas)
            if (par.Value.Contains(p)) return par.Key;
        return null;
    }

    private float Espacio(SpawnConfig c)
    {
        if (c.espacioEnFila > 0f) return c.espacioEnFila;
        return Mathf.Max(0.01f, anchoPasajeroBase * c.escalaEnSpawn * separacionRelativa);
    }

    private Vector3 Direccion(SpawnConfig c)
    {
        float xCabina = c.spawnPoint.position.x;

        if (c.ascensorIndex >= 0 && c.ascensorIndex < ascensorManager.ascensores.Count)
        {
            var asc = ascensorManager.ascensores[c.ascensorIndex];
            Transform punto = EstacionAbajo(c.rol) ? asc.puntoLejos : asc.puntoCerca;
            if (punto != null) xCabina = punto.position.x;
        }

        float signo = c.spawnPoint.position.x >= xCabina ? 1f : -1f;
        if (c.invertirFila) signo = -signo;
        return new Vector3(signo, 0f, 0f);
    }

    private Vector3 Puesto(SpawnConfig c, float indice)
    {
        return c.spawnPoint.position + Direccion(c) * (Espacio(c) * indice);
    }

    private Vector3 Entrada(SpawnConfig c)
    {
        return Puesto(c, maxEsperandoPorAnden - 1 + puestosDeEntrada);
    }

    private float Velocidad(SpawnConfig c) => Espacio(c) * puestosPorSegundo;

    private void LimpiarFilas()
    {
        foreach (var fila in filas.Values)
            fila.RemoveAll(p => p == null || !p.EstaAfuera);
    }

    private void OrdenarFilas()
    {
        foreach (var par in filas)
        {
            var c = par.Key;
            var fila = par.Value;

            for (int i = 0; i < fila.Count; i++)
            {
                var p = fila[i];
                if (p.estado == EstadoPasajero.Subiendo) continue;

                p.IrA(Puesto(c, i), Velocidad(c));
            }
        }
    }

    private bool PuedeSubir(Pasajero p)
    {
        var c = AndenDelPasajero(p);
        if (c == null) return true;

        int indice = FilaDe(c).IndexOf(p);
        var asc = ascensorManager.ascensores[p.AscensorIndex];
        return indice >= 0 && indice < asc.CapacidadMaxima - asc.PasajerosTotales;
    }

    private void SpawnearCumulo()
    {
        if (pasajeroPrefab == null) return;

        float factorLimpieza = 1f;
        if (GameManager.Instance != null)
            factorLimpieza = Mathf.Clamp01(GameManager.Instance.GameData.limpiezaDelDia / 100f);

        if (factorLimpieza <= 0f || Random.value > factorLimpieza) return;

        List<SpawnConfig> validos = new List<SpawnConfig>();
        foreach (var c in spawns)
        {
            if (c == null || c.spawnPoint == null) continue;
            if (!AndenValido(c)) continue;
            validos.Add(c);
        }

        if (validos.Count == 0) return;

        float intensidad = FranjaActual(out var direccion);
        SpawnConfig config = ElegirPorDireccion(validos, direccion);

        float factorGrupo = factorLimpieza * Mathf.Clamp(intensidad, 0.3f, 1f);
        int maxCumuloHoy = Mathf.Max(1, Mathf.RoundToInt(maxPasajerosPorCumulo * factorGrupo));
        int cantidad = Random.Range(1, maxCumuloHoy + 1);
        cantidad = Mathf.Min(cantidad, maxEsperandoPorAnden - FilaDe(config).Count);
        if (cantidad <= 0) return;

        Debug.Log($"👥 Llegan {cantidad} pasajeros a {config.nombre}");

        for (int i = 0; i < cantidad; i++)
        {
            CrearPasajero(config, i);
        }
    }

    private SpawnConfig ElegirPorDireccion(List<SpawnConfig> validos, DireccionFlujo direccion)
    {
        float total = 0f;
        foreach (var c in validos) total += PesoDireccion(c, direccion);

        float r = Random.Range(0f, total);
        foreach (var c in validos)
        {
            r -= PesoDireccion(c, direccion);
            if (r <= 0f) return c;
        }
        return validos[validos.Count - 1];
    }

    public bool DatosDeLlegada(int ascensorIndex, bool estacionAbajo, out Transform padre, out float escala, out float velocidad)
    {
        var c = AndenDe(ascensorIndex, estacionAbajo ? RolPasajero.SubeEnInicio : RolPasajero.SubeEnFinal);
        if (c == null)
        {
            padre = null; escala = 1f; velocidad = 1f;
            return false;
        }

        padre = c.spawnPoint;
        escala = c.escalaEnSpawn;
        velocidad = Velocidad(c);
        return true;
    }

    private bool AndenValido(SpawnConfig config)
    {
        if (config.ascensorIndex < 0 || config.ascensorIndex >= ascensorManager.ascensores.Count)
            return false;

        if (CabinaQueAtiende(config.rol) != config.ascensorIndex) return false;

        var asc = ascensorManager.ascensores[config.ascensorIndex];
        if (asc.enMovimiento && !spawnMientrasViaja) return false;

        bool estaAqui = !asc.enMovimiento && asc.estaAbajo == EstacionAbajo(config.rol);
        if (estaAqui && !asc.HayEspacio) return false;

        return FilaDe(config).Count < maxEsperandoPorAnden;
    }

    private void CrearPasajero(SpawnConfig config, int ordenEnGrupo)
    {
        Vector3 inicio = Entrada(config) + Direccion(config) * (Espacio(config) * ordenEnGrupo);

        GameObject nuevo = Instantiate(pasajeroPrefab, inicio, Quaternion.identity);
        Pasajero p = nuevo.GetComponent<Pasajero>();

        if (p == null)
        {
            Destroy(nuevo);
            return;
        }

        nuevo.transform.SetParent(config.spawnPoint, true);
        nuevo.transform.localScale = Vector3.one * config.escalaEnSpawn;

        p.Inicializar(config.rol, config.ascensorIndex, ascensorManager, config.spawnPoint, conPop: false);
        p.PuedeSubir = PuedeSubir;
        p.OnPasajeroTerminado += CuandoPasajeroTermina;

        var fila = FilaDe(config);
        fila.Add(p);
        p.IrA(Puesto(config, fila.Count - 1), Velocidad(config));
    }

    private void ReubicarPasajeros()
    {
        foreach (var origen in spawns)
        {
            if (origen == null || origen.spawnPoint == null) continue;

            int atiende = CabinaQueAtiende(origen.rol);
            if (atiende < 0 || atiende == origen.ascensorIndex) continue;

            var fila = FilaDe(origen);
            aMover.Clear();
            foreach (var p in fila)
                if (p != null && p.EstaEsperando) aMover.Add(p);

            foreach (var p in aMover)
            {
                fila.Remove(p);
                p.SalirParaCambiar(Entrada(origen), Velocidad(origen));
                enTransito.Add(new Transito { pasajero = p, afuera = false, espera = segundosFueraAlCambiar });
            }
        }
    }

    private void ProcesarTransito()
    {
        for (int i = enTransito.Count - 1; i >= 0; i--)
        {
            var t = enTransito[i];
            var p = t.pasajero;

            if (p == null || p.estado != EstadoPasajero.CambiandoDeAnden)
            {
                enTransito.RemoveAt(i);
                continue;
            }

            if (!t.afuera)
            {
                if (p.Deslizando) continue;
                t.afuera = true;
            }

            t.espera -= Time.deltaTime;
            if (t.espera > 0f) continue;

            var destino = AndenDe(CabinaQueAtiende(p.rol), p.rol);
            enTransito.RemoveAt(i);

            if (destino == null || FilaDe(destino).Count >= maxEsperandoPorAnden)
            {
                p.Retirarse(p.transform.position, 1f);
                continue;
            }

            var fila = FilaDe(destino);
            p.EntrarEnAnden(destino.ascensorIndex, destino.spawnPoint, destino.escalaEnSpawn, Entrada(destino));
            fila.Add(p);
            p.IrA(Puesto(destino, fila.Count - 1), Velocidad(destino));
        }
    }

    private void CerrarDia()
    {
        if (diaCerrado) return;
        diaCerrado = true;

        int total = 0;
        foreach (var par in filas)
        {
            foreach (var p in new List<Pasajero>(par.Value))
            {
                if (p == null) continue;
                p.Retirarse(Entrada(par.Key), Velocidad(par.Key));
                total++;
            }
            par.Value.Clear();
        }

        foreach (var t in enTransito)
            if (t.pasajero != null) t.pasajero.Retirarse(t.pasajero.transform.position, 1f);
        enTransito.Clear();

        Debug.Log($"🕔 Cierre del día: se retiran {total} pasajeros que esperaban");
    }

    /// <summary>
    /// Pone a un pasajero que acaba de bajar al principio de la fila de esa misma estación,
    /// para que tome el ascensor de vuelta altiro. Devuelve false si no se pudo.
    /// </summary>
    public bool VolverAHacerFila(Pasajero p, RolPasajero rolRegreso)
    {
        if (p == null || diaCerrado) return false;

        var c = AndenDe(p.AscensorIndex, rolRegreso);
        if (c == null) return false;

        var fila = FilaDe(c);
        if (fila.Count >= maxEsperandoPorAnden) return false;

        fila.Insert(0, p);
        return true;
    }

    public void Despedir(Pasajero p)
    {
        if (p == null) return;

        foreach (var par in filas)
        {
            if (par.Value.Remove(p))
            {
                p.Retirarse(Entrada(par.Key), Velocidad(par.Key));
                return;
            }
        }

        p.Retirarse(p.transform.position, 1f);
    }

    private void CuandoPasajeroTermina(Pasajero p)
    {
        if (p == null) return;
        p.OnPasajeroTerminado -= CuandoPasajeroTermina;
        foreach (var fila in filas.Values) fila.Remove(p);
    }
}
