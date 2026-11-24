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


    public Transform targetLimpieza;
    public Transform targetPolea;
    public Transform targetPuerta;
    public Transform targetDinero;

    [Header("Mantenimiento del ascensor")]
    [Tooltip("Puntos de mantenimiento que se pierden al terminar cada día (se reparan en la tienda)")]
    public float desgasteDiarioMantenimiento = 10f;
    [Tooltip("El desgaste diario nunca deja el mantenimiento por debajo de este valor. Llegar a 0 solo ocurre jugando y hace perder")]
    public float minimoTrasDesgasteDiario = 1f;

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

    public Transform ObtenerTarget(TipoSistema tipo)
    {
        return tipo switch
        {
            TipoSistema.Limpieza => targetLimpieza,
            TipoSistema.Polea => targetPolea,
            TipoSistema.Puerta => targetPuerta,
            TipoSistema.Dinero => targetDinero,
            _ => null
        };
    }

    public void ModificarLimpieza(float cantidad)
    {
        gameData.contadorLimpieza = Mathf.Clamp(gameData.contadorLimpieza + cantidad, 0, 100);
        //FloatingTextManager.Instance.MostrarTexto(cantidad, TipoSistema.Limpieza);
        gameData.ActualizarPopularidad();
        gameData.OnDataChanged?.Invoke();
    }

    public void ModificarPolea(float cantidad)
    {
        gameData.contadorGrasa = Mathf.Clamp(gameData.contadorGrasa + cantidad, 0, 100);
        //FloatingTextManager.Instance.MostrarTexto(cantidad, TipoSistema.Polea);
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

    public void ModificarPuerta(float cantidad)
    {
        gameData.contadorPuerta = Mathf.Clamp(gameData.contadorPuerta + cantidad, 0, 100);
        //FloatingTextManager.Instance.MostrarTexto(cantidad, TipoSistema.Puerta);
        gameData.OnDataChanged?.Invoke();
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

    public bool IntentarSubirPasajero()
    {
        if (!gameData.HayEspacio)
        {
            Debug.Log(" Ascensor lleno");
            return false;
        }

        gameData.pasajerosTotales++;
        gameData.OnDataChanged?.Invoke();
        Debug.Log($" Pasajeros: {gameData.pasajerosTotales}/{gameData.capacidadMaxima}");
        return true;
    }

    
    public void SumarDinero(int cantidad)
    {
        gameData.dineroTotal += cantidad;
        //FloatingTextManager.Instance.MostrarTexto(cantidad, TipoSistema.Dinero);
        gameData.OnDataChanged?.Invoke();
        Debug.Log($" Dinero total: {gameData.dineroTotal}");
    }


    public void BajarTodosLosPasajeros()
    {
        int cantidad = gameData.pasajerosTotales;
        gameData.pasajerosTotales = 0;
        gameData.OnDataChanged?.Invoke();
        Debug.Log($" Bajaron {cantidad} pasajeros");
    }
  
    public void PasajeroCompleto()
    {
        gameData.pasajerosTotales = Mathf.Max(0, gameData.pasajerosTotales - 1);
        gameData.dineroTotal += gameData.pagoPorPasajero;
        gameData.OnDataChanged?.Invoke();
        OnPasajeroCompleto?.Invoke();

        Debug.Log($" +{gameData.pagoPorPasajero} dinero. Pasajeros: {gameData.pasajerosTotales}");
    }


    public void PasajeroSubio()
    {
        if (!gameData.HayEspacio)
        {
            Debug.Log(" Ascensor lleno, no puede subir más pasajeros");
            return;
        }

        gameData.pasajerosTotales++;
        gameData.OnDataChanged?.Invoke();
        Debug.Log($" Pasajero subió. Total: {gameData.pasajerosTotales}");
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