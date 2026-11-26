using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PausaManager : MonoBehaviour
{
    [Header("Panel UI")]
    public GameObject panelOpcionesPausa;

    [Header("Input del Juego")]
    [Tooltip("Arrastra aqui el mismo Input Actions Asset que usa la Palanca")]
    public InputActionAsset inputActionsAsset;

    public static bool juegoPausado = false;

    private void OnDestroy()
    {
        // Es estática: si se cambia de escena con la pausa abierta no debe quedar marcada
        juegoPausado = false;
    }

    void Update()
    {

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (juegoPausado)
            {
                Reanudar();
            }
            else
            {
                Pausar();
            }
        }
    }

    public void Pausar()
    {
        if (panelOpcionesPausa != null) panelOpcionesPausa.SetActive(true);
        PausaJuego.Pedir(this);
        juegoPausado = true;

        
        if (inputActionsAsset != null)
        {
            inputActionsAsset.FindActionMap("Ascensor")?.Disable();
        }
    }

    public void Reanudar()
    {
        if (panelOpcionesPausa != null) panelOpcionesPausa.SetActive(false);
        PausaJuego.Liberar(this);
        juegoPausado = false;

        
        if (inputActionsAsset != null)
        {
            inputActionsAsset.FindActionMap("Ascensor")?.Enable();
        }
    }

    public void VolverAlMenuPrincipal()
    {
        PausaJuego.Limpiar();
        juegoPausado = false;
        SceneManager.LoadScene("Menu inicio");
    }
}