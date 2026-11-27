using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] public GameData gameData;   
    public GameData GameData => gameData;

    [SerializeField] private ResumenDiaData resumenDia;
    public ResumenDiaData ResumenDia => resumenDia;

    [Header("Mantenimiento del ascensor")]
    [Tooltip("Puntos de mantenimiento que se pierden al terminar cada día (se reparan en la tienda)")]
    public float desgasteDiarioMantenimiento = 10f;
    [Tooltip("El desgaste diario nunca deja el mantenimiento por debajo de este valor. Llegar a 0 solo ocurre jugando y hace perder")]
    public float minimoTrasDesgasteDiario = 1f;

    [Header("Popularidad al terminar el día (al darle a Continuar)")]
    [Tooltip("Si el mantenimiento o la limpieza están bajo este valor (%), restan popularidad; si están sobre él, suman")]
    public float umbralPopularidad = 60f;
    [Tooltip("Puntos de popularidad que se pierden por cada uno (mantenimiento y limpieza) que quede bajo el umbral")]
    public float popularidadPerdidaMediana = 8f;
    [Tooltip("Puntos de popularidad que se ganan por cada uno que quede sobre el umbral")]
    public float popularidadGanadaPequena = 3f;

    [Header("Economía: subsidio y cobros diarios")]
    public ConfigEconomia economia = new ConfigEconomia();

    [Header("Valores actuales (solo mirar, se actualizan solos)")]
    public float actualMantenimiento;
    public float actualLimpieza;
    public float actualPopularidad;

    public System.Action OnMantenimientoCero;

    public System.Action OnPasajeroCompleto;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += AlCargarEscena;
            if (gameData != null)
                gameData.FijarValoresDelDia();

            if (gameData != null)
                gameData.CapturarValoresIniciales();

            Debug.Log($"GameManager registrado y persistente. Escena: {gameObject.scene.name}");
        }
        else if (Instance != this)
        {
            Debug.LogWarning($"GameManager duplicado en {gameObject.name}. DESTRUYENDO.");

            Destroy(gameObject);
        }
    }
    public void ResetearJuego()
    {
        if (gameData != null)
            gameData.ResetData();
    }

    public void ModificarLimpieza(float cantidad)
    {
        gameData.contadorLimpieza = Mathf.Clamp(gameData.contadorLimpieza + cantidad, 0, 100);
        gameData.ActualizarPopularidad();
        gameData.OnDataChanged?.Invoke();
    }

    public void ModificarPolea(float cantidad)
    {
        gameData.contadorGrasa = Mathf.Clamp(gameData.contadorGrasa + cantidad, 0, 100);
        gameData.ActualizarPopularidad();
        gameData.OnDataChanged?.Invoke();

        if (gameData.contadorGrasa <= 0f && !gameData.ascensorAveriado)
        {
            gameData.ascensorAveriado = true;
            Debug.Log("El mantenimiento llegó a 0: el ascensor se averió");
            OnMantenimientoCero?.Invoke();
        }
    }

    public void ModificarPopularidad(float cantidad)
    {
        if (gameData == null) return;

        gameData.contadorPopularidad = Mathf.Clamp(gameData.contadorPopularidad + cantidad, 0f, 100f);
        gameData.OnDataChanged?.Invoke();
    }

    public void AplicarPopularidadDelDia()
    {
        if (gameData == null) return;

        float cambio = 0f;
        cambio += gameData.contadorGrasa < umbralPopularidad ? -popularidadPerdidaMediana : popularidadGanadaPequena;
        cambio += gameData.contadorLimpieza < umbralPopularidad ? -popularidadPerdidaMediana : popularidadGanadaPequena;

        ModificarPopularidad(cambio);

        Debug.Log($"Popularidad del día: {cambio:+0;-0;0} (mantenimiento {gameData.contadorGrasa:F0}%, limpieza {gameData.contadorLimpieza:F0}%). Ahora: {gameData.contadorPopularidad:F0}");
    }

    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        if (gameData != null)
            gameData.FijarValoresDelDia();
    }

    public void AplicarDesgasteDiario()
    {
        if (gameData == null) return;

        float nuevo = Mathf.Max(minimoTrasDesgasteDiario, gameData.contadorGrasa - desgasteDiarioMantenimiento);
        gameData.contadorGrasa = Mathf.Min(gameData.contadorGrasa, nuevo);
        gameData.ActualizarPopularidad();
        gameData.OnDataChanged?.Invoke();

        Debug.Log($"Desgaste diario aplicado. Mantenimiento: {gameData.contadorGrasa:F0}%");
    }

    public void SumarDinero(int cantidad)
    {
        gameData.dineroTotal += cantidad;
        gameData.RegistrarIngreso(cantidad);
        gameData.OnDataChanged?.Invoke();
        Debug.Log($" Dinero total: {gameData.dineroTotal}");
    }

    public void PasajeroCompleto()
    {
        OnPasajeroCompleto?.Invoke();
    }

    private void Update()
    {
        if (gameData == null) return;

        actualMantenimiento = gameData.contadorGrasa;
        actualLimpieza = gameData.contadorLimpieza;
        actualPopularidad = gameData.contadorPopularidad;
        gameData.RegistrarPopularidad();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;

        Debug.Log($"+ [{Time.time:F2}s] GameManager.OnDestroy() en escena: '{gameObject.scene.name}'");
        Debug.Log($"+Este GameObject: {gameObject.name}");
        Debug.Log($"+ Instance actual: {Instance}");
        Debug.Log($"+ ¿Soy el Instance?: {Instance == this}");

        if (Instance == this)
        {
            Instance = null;
        }

        Debug.Log($" Stack: {System.Environment.StackTrace}");
    }
}

public enum TipoSistema
{
    Limpieza,
    Polea,
    Puerta,
    Dinero
}