using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class MusicaMenu : MonoBehaviour
{
    [Tooltip("Segundos que tarda la música en apagarse al salir del menú")]
    public float duracionDesvanecer = 2f;

    private AudioSource fuente;
    private string escenaMenu;
    private float volumenBase;
    private bool desvaneciendo;

    private static MusicaMenu sonando;

    private void Awake()
    {
        // Si ya se perdió en esta sesión, en el menú suena la música final en lugar de esta
        if (MusicaFinal.PerdioEnEstaSesion)
        {
            GetComponent<AudioSource>().Stop();
            MusicaFinal.Reproducir(gameObject.scene.name);
            Destroy(gameObject);
            return;
        }

        if (sonando != null)
        {
            Destroy(gameObject);
            return;
        }
        sonando = this;

        fuente = GetComponent<AudioSource>();
        escenaMenu = gameObject.scene.name;
        volumenBase = fuente.volume;
        fuente.volume = volumenBase * Configuracion.VolumenMusica;

        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    private void Update()
    {
        // Volumen del Inspector por el de la barra "Volumen Musica" de Opciones (se nota al moverla)
        if (!desvaneciendo && fuente != null)
            fuente.volume = volumenBase * Configuracion.VolumenMusica;
    }

    private void OnDestroy()
    {
        if (sonando == this) sonando = null;
        SceneManager.sceneLoaded -= AlCargarEscena;
    }

    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        if (escena.name == escenaMenu) return;

        SceneManager.sceneLoaded -= AlCargarEscena;
        if (sonando == this) sonando = null;
        StartCoroutine(Desvanecer());
    }

    private IEnumerator Desvanecer()
    {
        desvaneciendo = true;
        float volumenInicial = fuente.volume;
        float t = 0f;

        while (t < duracionDesvanecer)
        {
            t += Time.unscaledDeltaTime;
            fuente.volume = Mathf.Lerp(volumenInicial, 0f, t / duracionDesvanecer);
            yield return null;
        }

        Destroy(gameObject);
    }
}
