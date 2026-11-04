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
    public float intervaloSpawn = 3f;
    public int maxPasajerosPorCumulo = 5;
    public bool spawnAutomatico = true;

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

        // 🔑 Elegir un spawn config al azar
        SpawnConfig config = spawns[Random.Range(0, spawns.Count)];

        // 🔑 Verificar que el ascensor tenga espacio
        if (!ascensorManager.HayEspacioEn(config.ascensorIndex)) return;

        // 🔑 Crear el cúmulo
        int cantidad = Random.Range(1, maxPasajerosPorCumulo + 1);

        Debug.Log($"👥 Creando cúmulo de {cantidad} pasajeros en {config.nombre}");

        for (int i = 0; i < cantidad; i++)
        {
            CrearPasajero(config);
        }
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

        // 🔑 Aplicar escala
        nuevo.transform.localScale = Vector3.one * config.escalaEnSpawn;

        // 🔑 Inicializar
        p.Inicializar(config.rol, config.ascensorIndex, ascensorManager);

        // 🔑 Suscribirse al evento
        p.OnPasajeroTerminado += CuandoPasajeroTermina;
    }

    private void CuandoPasajeroTermina(Pasajero p)
    {
        if (p != null)
            p.OnPasajeroTerminado -= CuandoPasajeroTermina;
    }
}