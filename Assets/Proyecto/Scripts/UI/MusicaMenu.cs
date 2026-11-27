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

    private static MusicaMenu sonando;

    private void Awake()
    {
        if (sonando != null)
        {
            Destroy(gameObject);
            return;
        }
        sonando = this;

        fuente = GetComponent<AudioSource>();
        escenaMenu = gameObject.scene.name;

        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += AlCargarEscena;
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
