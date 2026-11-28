using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Música de derrota. Empieza en la pantalla "Ascensor fuera de servicio", sigue sonando
// en el menú y, desde que se pierde, reemplaza a la música del menú hasta que se cierra el juego.
public class MusicaFinal : MonoBehaviour
{
    // Solo vive en memoria: al cerrar y abrir el juego vuelve a false y suena la música de siempre
    public static bool PerdioEnEstaSesion { get; private set; }

    private static MusicaFinal instancia;

    // El proyecto tiene la recarga de dominio desactivada: sin esto, en el editor
    // la derrota quedaría recordada entre una prueba (Play) y la siguiente
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar()
    {
        PerdioEnEstaSesion = false;
        instancia = null;
    }

    private AudioSource fuente;
    private string escenaMenu;
    private bool desvaneciendo;
    private float duracionDesvanecer = 2f;

    // Llamado al perder: empieza la música y la deja sonar al volver al menú
    public static void Iniciar(string escenaMenu)
    {
        PerdioEnEstaSesion = true;
        Reproducir(escenaMenu);
    }

    // Llamado por MusicaMenu al entrar al menú si ya se perdió en esta sesión
    public static void Reproducir(string escenaMenu)
    {
        if (instancia != null && !instancia.desvaneciendo)
        {
            instancia.escenaMenu = escenaMenu;
            return;
        }

        var b = Sonidos.Biblioteca;
        if (b == null || b.musicaFinal == null) return;

        var go = new GameObject("MusicaFinal");
        DontDestroyOnLoad(go);
        instancia = go.AddComponent<MusicaFinal>();
        instancia.escenaMenu = escenaMenu;

        var fuente = go.AddComponent<AudioSource>();
        fuente.clip = b.musicaFinal;
        fuente.loop = b.repetirMusicaFinal;
        fuente.playOnAwake = false;
        fuente.ignoreListenerPause = true;
        instancia.fuente = fuente;
        instancia.ActualizarVolumen();
        fuente.Play();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
    }

    private void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }

    private void Update()
    {
        if (!desvaneciendo) ActualizarVolumen();
    }

    private void ActualizarVolumen()
    {
        var b = Sonidos.Biblioteca;
        if (fuente != null && b != null)
            fuente.volume = b.volumenMusicaFinal * Configuracion.VolumenMusica;
    }

    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        // En el menú sigue sonando; al empezar a jugar se apaga como la música del menú
        if (escena.name == escenaMenu || desvaneciendo) return;
        if (instancia == this) instancia = null;
        StartCoroutine(Desvanecer());
    }

    private IEnumerator Desvanecer()
    {
        desvaneciendo = true;
        float volumenInicial = fuente != null ? fuente.volume : 0f;
        float t = 0f;

        while (t < duracionDesvanecer && fuente != null)
        {
            t += Time.unscaledDeltaTime;
            fuente.volume = Mathf.Lerp(volumenInicial, 0f, t / duracionDesvanecer);
            yield return null;
        }

        Destroy(gameObject);
    }
}
