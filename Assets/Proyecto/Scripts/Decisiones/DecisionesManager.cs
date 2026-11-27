using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DecisionesManager : MonoBehaviour
{
    public static DecisionesManager Instance { get; private set; }

    [Header("Pasajero sin plata")]
    [Range(0f, 1f)]
    [Tooltip("Probabilidad de que un pasajero que va a subir no tenga plata")]
    public float probabilidadSinPlata = 0.06f;
    public int maximoSinPlataPorDia = 2;
    [Tooltip("Popularidad que se gana al dejarlo subir gratis")]
    public float popularidadSiAyuda = 2f;
    [Tooltip("Popularidad que se pierde al no dejarlo subir")]
    public float popularidadSiRechaza = 4f;

    public string[] textosSinPlata =
    {
        "Disculpe... no me alcanza para el pasaje. ¿Me deja subir igual?",
        "Oiga, se me quedó la billetera en la casa. ¿Me podría llevar esta vez?",
        "No tengo ni una moneda y tengo que subir al cerro... ¿me ayuda?",
    };

    private int sinPlataHoy;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Registrar()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    private static void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        if (Instance == null && FindAnyObjectByType<AscensorManager>() != null)
            new GameObject("DecisionesManager").AddComponent<DecisionesManager>();
    }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        DecisionUI.Cerrar();
        PausaJuego.Liberar(this);
    }

    public bool QuierePreguntarSinPlata(Pasajero p)
    {
        if (p == null || p.EsBondadoso) return false;
        if (sinPlataHoy >= maximoSinPlataPorDia) return false;
        if (TutorialManager.EnCurso || DecisionUI.Abierta || PausaJuego.Pausado) return false;
        if (DialogoManager.Instance != null && DialogoManager.Instance.HayDialogoActivo()) return false;
        if (GameManager.Instance != null && GameManager.Instance.GameData.eventoOcurriendo) return false;

        return UnityEngine.Random.value < probabilidadSinPlata;
    }

    public void PreguntarSinPlata(Action<bool> alResponder)
    {
        sinPlataHoy++;
        PausaJuego.Pedir(this);

        string hablante = UnityEngine.Random.value < 0.5f ? "Desconocido" : "Desconocida";
        string texto = textosSinPlata[UnityEngine.Random.Range(0, textosSinPlata.Length)];

        DecisionUI.Mostrar(hablante, texto,
            new DecisionUI.Opcion("Dejar subir gratis", () => Responder(true, alResponder)),
            new DecisionUI.Opcion("No dejar subir", () => Responder(false, alResponder)));
    }

    private void Responder(bool dejarSubir, Action<bool> alResponder)
    {
        PausaJuego.Liberar(this);

        if (GameManager.Instance != null)
            GameManager.Instance.ModificarPopularidad(dejarSubir ? popularidadSiAyuda : -popularidadSiRechaza);

        if (PanelMensajesMonitor.Instance != null)
        {
            if (dejarSubir)
                PanelMensajesMonitor.Instance.MostrarMensaje("Le diste el viaje gratis", new Color(0.4f, 1f, 0.5f), 2f);
            else
                PanelMensajesMonitor.Instance.MostrarMensaje("El pasajero se fue a pie", new Color(1f, 0.6f, 0.3f), 2f);
        }

        alResponder?.Invoke(dejarSubir);
    }
}
