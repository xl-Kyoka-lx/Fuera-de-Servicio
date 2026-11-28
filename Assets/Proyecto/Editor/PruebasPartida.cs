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

    [MenuItem("Herramientas/Pruebas/Lanzar timbre de la cabina de abajo")]
    private static void LanzarSenalDeAbajo()
    {
        var proceso = ProcesoPartida.Instance;
        if (proceso == null)
        {
            Debug.LogWarning("[Pruebas] No hay ProcesoPartida en la escena.");
            return;
        }

        string motivo = proceso.ForzarSenalDeAbajo();
        if (motivo != null)
            Debug.LogWarning("[Pruebas] No se pudo lanzar el timbre: " + motivo);
        else
            Debug.Log("[Pruebas] Timbre de la cabina de abajo lanzado: cobra ahora y suena en unos segundos.");
    }

    [MenuItem("Herramientas/Pruebas/Lanzar timbre de la cabina de abajo", true)]
    private static bool PuedeLanzarSenalDeAbajo() => Application.isPlaying;

    [MenuItem("Herramientas/Pruebas/Forzar diálogo raro (token pirata)")]
    private static void ForzarDialogoRaro()
    {
        BondadososManager.ForzarDialogoRaro = true;
        Debug.Log("[Pruebas] El próximo pasajero que suba dirá el diálogo raro al llegar.");
    }

    [MenuItem("Herramientas/Pruebas/Forzar diálogo raro (token pirata)", true)]
    private static bool PuedeForzarDialogoRaro() => Application.isPlaying;

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
