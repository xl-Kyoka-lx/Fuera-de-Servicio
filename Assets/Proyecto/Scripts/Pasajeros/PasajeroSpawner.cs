using UnityEngine;
using System.Collections.Generic;

// Crea a los pasajeros y los ordena en filas en los andenes.
//
// Cada estación (abajo = inicio, arriba = final) tiene un andén por cabina. En cada momento
// una sola cabina "atiende" la estación:
//   - la que está ahí con la puerta abierta, o
//   - si ya cerró (W o "cabina de abajo lista") o se está yendo, la otra, que es la próxima en llegar.
//
// - Los pasajeros nuevos entran deslizándose desde afuera hasta el final de la fila del andén que atiende.
// - La fila empieza en el spawnPoint y crece alejándose de la cabina (máximo maxEsperandoPorAnden).
// - Suben en orden: los primeros de la fila que caben en la cabina.
// - Los que no alcanzaron a subir se dan vuelta y salen por atrás de su fila (por donde llegaron),
//   esperan afuera unos segundos y entran por atrás de la fila de la otra cabina (si no hay lugar, no vuelven).
// - Al llegar la hora de cierre ya no entra nadie, y los que esperaban se van caminando.
// - Cuántos llegan y a qué estación depende del horario (franjas de la hora punta).
public class PasajeroSpawner : MonoBehaviour
{
    public static PasajeroSpawner Instance { get; private set; }

    // Bajada = del cerro al plan (suben arriba, en el andén "final").
    // Subida = del plan al cerro (suben abajo, en el andén "inicio").
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
    private float anchoPasajeroBase = 1f;   // ancho del sprite con escala 1

    // Fila de cada andén (el índice 0 es el primero, junto a la cabina)
    private readonly Dictionary<SpawnConfig, List<Pasajero>> filas = new Dictionary<SpawnConfig, List<Pasajero>>();
    private readonly List<Pasajero> aMover = new List<Pasajero>();

    // Pasajeros que salieron de su fila y van a entrar a la de la otra cabina
    private class Transito
    {
        public Pasajero pasajero;
        public bool afuera;      // ya llegó a la salida (fuera de pantalla)
        public float espera;     // segundos que faltan para entrar al otro andén
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

        // En hora punta el tiempo entre grupos se acorta
        timer += Time.deltaTime * Mathf.Max(0f, FranjaActual(out _));
        if (timer >= intervaloSpawn)
        {
            timer = 0f;
            SpawnearCumulo();
        }
    }

    // ---------- Horario ----------

    // Intensidad de la hora actual y la dirección dominante
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

    // Peso de un andén según la dirección dominante de la franja
    private float PesoDireccion(SpawnConfig c, DireccionFlujo direccion)
    {
        if (direccion == DireccionFlujo.Mixta) return 1f;

        // Bajada: la gente sube arriba (cerro). Subida: sube abajo (plan).
        bool esDominante = (direccion == DireccionFlujo.Bajada) == !EstacionAbajo(c.rol);
        return esDominante ? pesoDireccionDominante : 1f - pesoDireccionDominante;
    }

    // ---------- Qué cabina atiende cada estación ----------

    private static bool EstacionAbajo(RolPasajero rol) => rol == RolPasajero.SubeEnInicio;

    // Índice de la cabina que atiende la estación de ese rol, o -1 si no hay ninguna
    private int CabinaQueAtiende(RolPasajero rol)
    {
        bool abajo = EstacionAbajo(rol);
        int laOtra = -1;

        for (int i = 0; i < ascensorManager.ascensores.Count; i++)
        {
            var asc = ascensorManager.ascensores[i];

            // estaAbajo solo cambia al llegar, así que en viaje el destino es el lado contrario
            bool ladoAbajo = asc.enMovimiento ? !asc.estaAbajo : asc.estaAbajo;

            if (ladoAbajo != abajo)
            {
                // Detenida en la otra estación: será la próxima en venir
                if (!asc.enMovimiento && laOtra < 0) laOtra = i;
                continue;
            }

            if (asc.enMovimiento) return i;           // viene hacia esta estación
            if (!PuertaCerrada(i)) return i;          // está aquí y todavía se puede subir
            // está aquí pero ya cerró: atiende la otra
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

    // ---------- Geometría de la fila ----------

    private float Espacio(SpawnConfig c)
    {
        if (c.espacioEnFila > 0f) return c.espacioEnFila;
        return Mathf.Max(0.01f, anchoPasajeroBase * c.escalaEnSpawn * separacionRelativa);
    }

    // Hacia dónde crece la fila: alejándose de la cabina en esa estación
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

    // Afuera de la fila, por el lado de atrás: por ahí entran y se van
    private Vector3 Entrada(SpawnConfig c)
    {
        return Puesto(c, maxEsperandoPorAnden - 1 + puestosDeEntrada);
    }

    private float Velocidad(SpawnConfig c) => Espacio(c) * puestosPorSegundo;

    // ---------- Mantener las filas ----------

    // Quita a los que ya subieron, se fueron o se destruyeron
    private void LimpiarFilas()
    {
        foreach (var fila in filas.Values)
            fila.RemoveAll(p => p == null || !p.EstaAfuera);
    }

    // Cada uno camina a su puesto: si alguien sale de la fila, los de atrás avanzan
    private void OrdenarFilas()
    {
        foreach (var par in filas)
        {
            var c = par.Key;
            var fila = par.Value;

            for (int i = 0; i < fila.Count; i++)
            {
                var p = fila[i];
                if (p.estado == EstadoPasajero.Subiendo) continue;   // ya está subiendo, no se mueve

                p.IrA(Puesto(c, i), Velocidad(c));
            }
        }
    }

    // Sube solo si está entre los primeros de la fila que caben en la cabina
    private bool PuedeSubir(Pasajero p)
    {
        var c = AndenDelPasajero(p);
        if (c == null) return true;

        int indice = FilaDe(c).IndexOf(p);
        var asc = ascensorManager.ascensores[p.AscensorIndex];
        return indice >= 0 && indice < asc.CapacidadMaxima - asc.PasajerosTotales;
    }

    // ---------- Aparecer ----------

    private void SpawnearCumulo()
    {
        if (pasajeroPrefab == null) return;

        // 🔑 La limpieza del día define cuántos pasajeros suben (100% = varios, 0% = nadie)
        float factorLimpieza = 1f;
        if (GameManager.Instance != null)
            factorLimpieza = Mathf.Clamp01(GameManager.Instance.GameData.limpiezaDelDia / 100f);

        if (factorLimpieza <= 0f || Random.value > factorLimpieza) return;

        // 🔑 Solo los andenes de las cabinas que atienden cada estación, y con lugar en la fila
        List<SpawnConfig> validos = new List<SpawnConfig>();
        foreach (var c in spawns)
        {
            if (c == null || c.spawnPoint == null) continue;
            if (!AndenValido(c)) continue;
            validos.Add(c);
        }

        if (validos.Count == 0) return;

        // 🔑 La estación dominante del horario recibe la mayoría de los grupos
        float intensidad = FranjaActual(out var direccion);
        SpawnConfig config = ElegirPorDireccion(validos, direccion);

        // 🔑 Un grupo que llega junto (más chico en horas valle, sin pasarse del máximo de la fila)
        float factorGrupo = factorLimpieza * Mathf.Clamp(intensidad, 0.3f, 1f);
        int maxCumuloHoy = Mathf.Max(1, Mathf.RoundToInt(maxPasajerosPorCumulo * factorGrupo));
        int cantidad = Random.Range(1, maxCumuloHoy + 1);
        cantidad = Mathf.Min(cantidad, maxEsperandoPorAnden - FilaDe(config).Count);
        if (cantidad <= 0) return;

        Debug.Log($"👥 Llegan {cantidad} pasajeros a {config.nombre}");

        for (int i = 0; i < cantidad; i++)
        {
            // Entran uno detrás del otro, separados como en la fila
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

    // ---------- Llegada a la otra estación (la usa el Pasajero al bajar) ----------

    // Andén donde se baja un pasajero de esa cabina al llegar a esa estación: su padre, escala y velocidad
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

        // Si la cabina está aquí y ya está llena, no tiene sentido que llegue más gente
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

    // ---------- Cambiar de andén ----------

    // Los que esperan una cabina que ya no atiende su estación se dan vuelta y salen
    // por atrás de su fila (por donde llegaron)
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

    // Cuando ya están afuera esperan unos segundos y entran por atrás de la fila de la cabina que atiende ahora
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
                if (p.Deslizando) continue;   // todavía caminando hacia la salida
                t.afuera = true;
            }

            t.espera -= Time.deltaTime;
            if (t.espera > 0f) continue;

            // Se decide al entrar: puede que en estos segundos haya cambiado la cabina que atiende
            var destino = AndenDe(CabinaQueAtiende(p.rol), p.rol);
            enTransito.RemoveAt(i);

            if (destino == null || FilaDe(destino).Count >= maxEsperandoPorAnden)
            {
                // No hay lugar: ya está fuera de pantalla, simplemente no vuelve
                p.Retirarse(p.transform.position, 1f);
                continue;
            }

            var fila = FilaDe(destino);
            p.EntrarEnAnden(destino.ascensorIndex, destino.spawnPoint, destino.escalaEnSpawn, Entrada(destino));
            fila.Add(p);
            p.IrA(Puesto(destino, fila.Count - 1), Velocidad(destino));
        }
    }

    // ---------- Fin del día ----------

    // Hora de cierre: ya no llega nadie más y los que esperaban se van caminando
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

        // Los que estaban afuera esperando para cambiar de andén simplemente no vuelven
        foreach (var t in enTransito)
            if (t.pasajero != null) t.pasajero.Retirarse(t.pasajero.transform.position, 1f);
        enTransito.Clear();

        Debug.Log($"🕔 Cierre del día: se retiran {total} pasajeros que esperaban");
    }

    private void CuandoPasajeroTermina(Pasajero p)
    {
        if (p == null) return;
        p.OnPasajeroTerminado -= CuandoPasajeroTermina;
        foreach (var fila in filas.Values) fila.Remove(p);
    }
}
