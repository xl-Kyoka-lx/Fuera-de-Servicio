using UnityEngine;

// Desgaste del ascensor según los pasajeros transportados:
//  - La limpieza baja un poco cada cierta cantidad de pasajeros.
//  - El mantenimiento baja un poco cada cierta cantidad de pasajeros.
// Ya no hay desgaste por tiempo ni por cada vez que el ascensor arranca o se detiene.
public class StatusManager : MonoBehaviour
{
    [Header("Limpieza")]
    [Tooltip("Cada cuántos pasajeros transportados baja la limpieza")]
    public int pasajerosPorDescuentoLimpieza = 30;
    [Tooltip("Cuánto baja la limpieza (en puntos de porcentaje)")]
    public float descuentoLimpieza = 5f;

    [Header("Mantenimiento")]
    [Tooltip("Cada cuántos pasajeros transportados baja el mantenimiento")]
    public int pasajerosPorDescuentoMantenimiento = 50;
    [Tooltip("Cuánto baja el mantenimiento (en puntos de porcentaje)")]
    public float descuentoMantenimiento = 5f;

    private void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnPasajeroCompleto += AlCompletarPasajero;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnPasajeroCompleto -= AlCompletarPasajero;
    }

    private void AlCompletarPasajero()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        var data = gm.GameData;

        data.pasajerosParaLimpieza++;
        data.pasajerosParaMantenimiento++;

        if (pasajerosPorDescuentoLimpieza > 0 && data.pasajerosParaLimpieza >= pasajerosPorDescuentoLimpieza)
        {
            data.pasajerosParaLimpieza -= pasajerosPorDescuentoLimpieza;
            gm.ModificarLimpieza(-descuentoLimpieza);
            Debug.Log($"Desgaste por pasajeros: limpieza -{descuentoLimpieza}");
        }

        if (pasajerosPorDescuentoMantenimiento > 0 && data.pasajerosParaMantenimiento >= pasajerosPorDescuentoMantenimiento)
        {
            data.pasajerosParaMantenimiento -= pasajerosPorDescuentoMantenimiento;
            gm.ModificarPolea(-descuentoMantenimiento);
            Debug.Log($"Desgaste por pasajeros: mantenimiento -{descuentoMantenimiento}");
        }
    }
}