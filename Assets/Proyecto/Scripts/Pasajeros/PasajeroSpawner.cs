using UnityEngine;
using System.Collections.Generic;

public class PasajeroSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnConfig
    {
        [Tooltip("Nombre descriptivo")]
        public string nombre;

        [Tooltip("Punto central del cúmulo")]
        public Transform spawnPoint;

        [Tooltip("Índice del ascensor al que pertenece")]
        public int ascensorIndex = 0;

        [Tooltip("Rol del pasajero")]
        public RolPasajero rol;

        [Header("=== CÚMULO ===")]
        [Tooltip("Radio de dispersión para el cúmulo (más bajo = más juntos)")]
        public float radioCumulo = 0.5f;

        [Tooltip("Escala de los pasajeros del cúmulo")]
        public float escalaEnSpawn = 1f;
    }

    [Header("=== REFERENCIAS ===")]
    public GameObject pasajeroPrefab;
    public AscensorManager ascensorManager;

    [Header("=== CONFIGURACIONES DE SPAWN ===")]
    public List<SpawnConfig> spawns = new List<SpawnConfig>();

    [Header("=== CONFIGURACIÓN ===")]
    public float intervaloSpawn = 2f;
    public int maxPasajerosPorCumulo = 5;
    public bool spawnAutomatico = true;

    [Tooltip("Si está marcado, mientras el ascensor viaja los pasajeros aparecen en el lado al que va a llegar. Si no, solo aparecen con el ascensor detenido")]
    public bool spawnMientrasViaja = true;

    private float timer = 0f;

    private void Update()
    {
        if (!spawnAutomatico || ascensorManager == null || spawns.Count == 0) return;

        timer += Time.deltaTime;
        if (timer >= intervaloSpawn)
        {
            timer = 0f;
            SpawnearCumulo();
        }
    }

    private void SpawnearCumulo()
    {
        if (pasajeroPrefab == null) return;

        // 🔑 Solo los spawns del lado donde está (o hacia donde va) su ascensor
        List<SpawnConfig> validos = new List<SpawnConfig>();
        foreach (var c in spawns)
        {
            if (c.spawnPoint == null) continue;
            if (!AscensorEnEseLado(c)) continue;
            if (!ascensorManager.HayEspacioEn(c.ascensorIndex)) continue;
            validos.Add(c);
        }

        if (validos.Count == 0) return;

        // 🔑 Elegir uno al azar entre los válidos
        SpawnConfig config = validos[Random.Range(0, validos.Count)];

        // 🔑 Crear el cúmulo
        int cantidad = Random.Range(1, maxPasajerosPorCumulo + 1);

        Debug.Log($"👥 Creando cúmulo de {cantidad} pasajeros en {config.nombre}");

        for (int i = 0; i < cantidad; i++)
        {
            CrearPasajero(config);
        }
    }

    // Verdadero si el ascensor de este spawn está en el lado del spawn,
    // o va a llegar a él (si está viajando y spawnMientrasViaja está marcado).
    private bool AscensorEnEseLado(SpawnConfig config)
    {
        if (config.ascensorIndex < 0 || config.ascensorIndex >= ascensorManager.ascensores.Count)
            return false;

        var asc = ascensorManager.ascensores[config.ascensorIndex];

        if (asc.enMovimiento && !spawnMientrasViaja)
            return false;

        // estaAbajo solo cambia al llegar, así que en viaje el destino es el lado contrario
        bool ladoAbajo = asc.enMovimiento ? !asc.estaAbajo : asc.estaAbajo;

        return (config.rol == RolPasajero.SubeEnInicio && ladoAbajo)
            || (config.rol == RolPasajero.SubeEnFinal && !ladoAbajo);
    }

    private void CrearPasajero(SpawnConfig config)
    {
        // 🔑 Posición aleatoria dentro del radio del cúmulo
        Vector2 offset = Random.insideUnitCircle * config.radioCumulo;
        Vector3 posicion = config.spawnPoint.position + new Vector3(offset.x, offset.y, 0);

        // 🔑 Instanciar
        GameObject nuevo = Instantiate(pasajeroPrefab, posicion, Quaternion.identity);
        Pasajero p = nuevo.GetComponent<Pasajero>();

        if (p == null)
        {
            Destroy(nuevo);
            return;
        }

        // 🔑 Hacer hijo del spawn (para que se mueva con él si es necesario)
        nuevo.transform.SetParent(config.spawnPoint);

        // 🔑 Aplicar escala
        nuevo.transform.localScale = Vector3.one * config.escalaEnSpawn;

        // 🔑 Inicializar con referencia al padre
        p.Inicializar(config.rol, config.ascensorIndex, ascensorManager, config.spawnPoint);

        // 🔑 Suscribirse al evento
        p.OnPasajeroTerminado += CuandoPasajeroTermina;
    }

    private void CuandoPasajeroTermina(Pasajero p)
    {
        if (p != null)
            p.OnPasajeroTerminado -= CuandoPasajeroTermina;
    }
}