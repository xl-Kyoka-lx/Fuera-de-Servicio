using UnityEngine;
using System;

public enum EstadoJuego
{
    Jugando,        
    EnDialogo,      
    EnPausa,        
    EnMenu,         
    GameOver        
}

public class EstadoJuegoManager : MonoBehaviour
{
    public static EstadoJuegoManager Instance { get; private set; }

    public EstadoJuego estadoActual = EstadoJuego.Jugando;

    public event Action<EstadoJuego, EstadoJuego> OnEstadoCambio; 

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += AlCargarEscena;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= AlCargarEscena;
    }

    private void AlCargarEscena(UnityEngine.SceneManagement.Scene escena, UnityEngine.SceneManagement.LoadSceneMode modo)
    {
        if (modo == UnityEngine.SceneManagement.LoadSceneMode.Single)
            estadoActual = EstadoJuego.Jugando;
    }

    public void CambiarEstado(EstadoJuego nuevoEstado)
    {
        if (estadoActual == nuevoEstado) return;

        EstadoJuego anterior = estadoActual;
        estadoActual = nuevoEstado;

        if (nuevoEstado == EstadoJuego.Jugando) PausaJuego.Liberar(this);
        else PausaJuego.Pedir(this);

        Debug.Log($"Estado cambiado: {anterior} → {nuevoEstado}");
        OnEstadoCambio?.Invoke(anterior, nuevoEstado);
    }

    public bool EstaJugando() => estadoActual == EstadoJuego.Jugando;
    public bool EstaEnDialogo() => estadoActual == EstadoJuego.EnDialogo;
    public bool EstaPausado() => estadoActual != EstadoJuego.Jugando;
}