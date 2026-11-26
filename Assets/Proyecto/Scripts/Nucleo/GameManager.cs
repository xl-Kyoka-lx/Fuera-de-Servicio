using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }


    [SerializeField] public GameData gameData;   
    public GameData GameData => gameData;

    // 🔑 Referencia al ScriptableObject del resumen
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

    [Header("Valores actuales (solo mirar, se actualizan solos)")]
    public float actualMantenimiento;
    public float actualLimpieza;
    public float actualPopularidad;

    [Header("Pruebas: marca la casilla y mueve los deslizadores")]
    [Tooltip("Marcada: cada cambio que hagas aquí se aplica al instante")]
    public bool aplicarValoresDePrueba = false;
    [Range(0, 100)] public float pruebaMantenimiento = 100f;
    [Range(0, 100)] public float pruebaLimpieza = 100f;
    [Range(0, 100)] public float pruebaPopularidad = 50f;

    // Se avisa cuando el mantenimiento llega a 0 durante la partida
    public System.Action OnMantenimientoCero;

    // Se avisa cada vez que un pasajero llega a su destino y paga
    public System.Action OnPasajeroCompleto;





    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            // Cada vez que se carga una escena se fijan los valores con los que se calcula el día
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

        // Mantenimiento en 0 jugando = el ascensor se averió y se pierde
        if (gameData.contadorGrasa <= 0f && !gameData.ascensorAveriado)
        {
            gameData.ascensorAveriado = true;
            Debug.Log("El mantenimiento llegó a 0: el ascensor se averió");
            OnMantenimientoCero?.Invoke();
        }
    }

    // Sube (positivo) o baja (negativo) la popularidad
    public void ModificarPopularidad(float cantidad)
    {
        if (gameData == null) return;

        gameData.contadorPopularidad = Mathf.Clamp(gameData.contadorPopularidad + cantidad, 0f, 100f);
        gameData.OnDataChanged?.Invoke();
    }

    // Al darle a Continuar en la noche: el mantenimiento y la limpieza ajustan la popularidad.
    // Cada uno, si está bajo el umbral resta una cantidad mediana; si no, suma una pequeña.
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

    // Se llama al terminar el día: el mantenimiento baja un valor fijo,
    // pero este desgaste nunca lo deja en 0 (solo se pierde jugando).
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
        gameData.OnDataChanged?.Invoke();
        Debug.Log($" Dinero total: {gameData.dineroTotal}");
    }


  
    // Un pasajero llegó a su destino. El pago lo hace el propio Pasajero (según su emoción).
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
    }

    // Se llama sola cuando cambias algo en el Inspector
    private void OnValidate()
    {
        if (!aplicarValoresDePrueba || gameData == null) return;

        gameData.contadorGrasa = pruebaMantenimiento;
        gameData.contadorLimpieza = pruebaLimpieza;
        gameData.contadorPopularidad = pruebaPopularidad;

        // Para que el cambio se note hoy mismo y no solo desde el día siguiente
        gameData.mantenimientoDelDia = Mathf.Clamp(pruebaMantenimiento, 1f, 100f);
        gameData.limpiezaDelDia = pruebaLimpieza;

        gameData.OnDataChanged?.Invoke();
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

        // Imprimir el stack trace para saber QUIÉN lo destruyó
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