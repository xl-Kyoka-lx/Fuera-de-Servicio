using UnityEditor;
using UnityEngine;

public static class PruebasPartida
{
    private const string ClavePartida = "PartidaIniciada";
    private const string ClaveTutorial = "TutorialHecho";

    [MenuItem("Herramientas/Pruebas/Simular partida guardada")]
    private static void SimularPartida()
    {
        PlayerPrefs.SetInt(ClavePartida, 1);
        Guardar("Partida guardada simulada: el botón Continuar queda activo y Nueva partida pide confirmación.");
    }

    [MenuItem("Herramientas/Pruebas/Borrar partida guardada")]
    private static void BorrarPartida()
    {
        PlayerPrefs.DeleteKey(ClavePartida);
        Guardar("Partida guardada borrada: Continuar queda desactivado.");
    }

    [MenuItem("Herramientas/Pruebas/Marcar tutorial como hecho")]
    private static void SaltarTutorial()
    {
        PlayerPrefs.SetInt(ClaveTutorial, 1);
        Guardar("Tutorial marcado como hecho: no aparecerá al jugar.");
    }

    [MenuItem("Herramientas/Pruebas/Repetir tutorial")]
    private static void RepetirTutorial()
    {
        PlayerPrefs.DeleteKey(ClaveTutorial);
        Guardar("El tutorial volverá a aparecer al jugar.");
    }

    private static void Guardar(string mensaje)
    {
        PlayerPrefs.Save();

        if (Application.isPlaying)
        {
            var menu = Object.FindAnyObjectByType<MenuPrincipal>();
            if (menu != null) menu.ActualizarBotones();
        }

        Debug.Log("[Pruebas] " + mensaje);
    }
}
