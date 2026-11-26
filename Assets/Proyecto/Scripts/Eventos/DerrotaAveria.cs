using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class DerrotaAveria : MonoBehaviour
{
    public string titulo = "El ascensor se averió";
    [TextArea] public string mensaje = "El mantenimiento llegó a 0 y el ascensor ya no arranca.";
    public string textoBoton = "Volver al menú";
    [Tooltip("Nombre exacto de la escena del menú principal")]
    public string escenaMenu = "Menu inicio";
    [Tooltip("Segundos de espera antes de mostrar la pantalla")]
    public float retraso = 1.5f;
    public int ordenCanvas = 200;

    private bool mostrada = false;

    private void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnMantenimientoCero += AlAveriarse;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnMantenimientoCero -= AlAveriarse;
    }

    private void AlAveriarse()
    {
        if (mostrada) return;
        mostrada = true;

        if (PanelMensajesMonitor.Instance != null)
            PanelMensajesMonitor.Instance.MostrarMensajeInmediato("¡El ascensor se averió!", Color.red);

        StartCoroutine(MostrarConRetraso());
    }

    private IEnumerator MostrarConRetraso()
    {
        yield return new WaitForSecondsRealtime(retraso);

        PantallaFueraDeServicio.Mostrar(escenaMenu);
    }
}
