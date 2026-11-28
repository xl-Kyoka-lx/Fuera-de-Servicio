using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Sonidos : MonoBehaviour
{
    private static Sonidos instancia;
    private static BibliotecaSonidos biblioteca;

    private AudioSource fuente;
    private readonly List<RaycastResult> resultados = new List<RaycastResult>();

    public static BibliotecaSonidos Biblioteca
    {
        get
        {
            if (biblioteca == null) biblioteca = Resources.Load<BibliotecaSonidos>("BibliotecaSonidos");
            return biblioteca;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Iniciar()
    {
        if (instancia != null) return;

        var go = new GameObject("Sonidos");
        DontDestroyOnLoad(go);
        instancia = go.AddComponent<Sonidos>();
        instancia.fuente = go.AddComponent<AudioSource>();
        instancia.fuente.playOnAwake = false;

        SceneManager.sceneLoaded += AlCargarEscena;
    }

    private static void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        if (Object.FindAnyObjectByType<AscensorManager>() != null && Object.FindAnyObjectByType<AmbienteGente>() == null)
            new GameObject("AmbienteGente").AddComponent<AmbienteGente>();
    }

    public static void Reproducir(AudioClip clip, float volumen = 1f)
    {
        if (clip == null || instancia == null) return;
        instancia.fuente.PlayOneShot(clip, volumen);
    }

    public static void ReproducirQte(System.Func<BibliotecaSonidos, AudioClip> elegir)
    {
        var b = Biblioteca;
        if (b != null) Reproducir(elegir(b), b.volumenQte * Configuracion.VolumenQte);
    }

    private void Update()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
        if (EventSystem.current == null || Biblioteca == null) return;

        var datos = new PointerEventData(EventSystem.current) { position = Mouse.current.position.ReadValue() };
        resultados.Clear();
        EventSystem.current.RaycastAll(datos, resultados);
        if (resultados.Count == 0) return;

        var selectable = resultados[0].gameObject.GetComponentInParent<Selectable>();
        if (selectable == null || !selectable.IsInteractable()) return;
        if (selectable is Button || selectable is Toggle)
            Reproducir(biblioteca.clicBoton, biblioteca.volumenClic);
    }
}
