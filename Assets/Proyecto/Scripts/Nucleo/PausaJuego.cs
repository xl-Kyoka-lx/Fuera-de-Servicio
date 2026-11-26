using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PausaJuego
{
    private static readonly HashSet<object> motivos = new HashSet<object>();

    public static bool Pausado => motivos.Count > 0;

    public static void Pedir(object motivo)
    {
        if (motivo == null) return;
        motivos.Add(motivo);
        Aplicar();
    }

    public static void Liberar(object motivo)
    {
        if (motivo == null) return;
        motivos.Remove(motivo);
        Aplicar();
    }

    public static void Limpiar()
    {
        motivos.Clear();
        Aplicar();
    }

    private static void Aplicar()
    {
        motivos.RemoveWhere(m => m is Object o && o == null);
        Time.timeScale = motivos.Count > 0 ? 0f : 1f;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Inicializar()
    {
        motivos.Clear();
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    private static void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        if (modo == LoadSceneMode.Single) Limpiar();
    }
}
