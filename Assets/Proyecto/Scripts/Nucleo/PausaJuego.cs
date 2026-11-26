using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Único lugar que decide si el juego está congelado (Time.timeScale).
//
// Antes cada sistema (diálogos, eventos, menú de pausa...) ponía Time.timeScale a 0 o a 1 por su cuenta,
// y los eventos guardaban el valor "anterior" para restaurarlo. Si dos de esas cosas se cruzaban
// (por ejemplo un evento que empieza mientras un diálogo o la pausa tienen el tiempo en 0),
// el evento "restauraba" un 0 y el juego quedaba congelado hasta pausar y reanudar.
//
// Ahora cada sistema PIDE la pausa con un motivo y la LIBERA al terminar.
// El juego corre solo cuando no queda ningún motivo activo.
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

    // Para cambios de escena, reintentar, volver al menú, etc.
    public static void Limpiar()
    {
        motivos.Clear();
        Aplicar();
    }

    private static void Aplicar()
    {
        // Los objetos de Unity destruidos sin liberar su pausa no deben dejar el juego congelado
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
        // Una escena nueva siempre empieza con el juego corriendo
        if (modo == LoadSceneMode.Single) Limpiar();
    }
}
