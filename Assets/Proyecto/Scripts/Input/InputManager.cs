using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public AscensorController ascensorController;
    private AscensorControls controls;

    private void Awake()
    {
        controls = new AscensorControls();
        controls.Gameplay.AvisarCabina.performed += OnAvisarCabina;
    }

    private void OnEnable()  { controls?.Enable(); }
    private void OnDisable() { controls?.Disable(); }

    private void OnDestroy()
    {
        if (controls != null)
            controls.Gameplay.AvisarCabina.performed -= OnAvisarCabina;
    }

    private void OnAvisarCabina(InputAction.CallbackContext context)
    {
        Debug.Log("W: avisar cabina");
    }
}