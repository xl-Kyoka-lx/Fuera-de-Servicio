using System;
using System.Collections;
using UnityEngine;

public enum EstadoPasajero
{
    EsperandoEnAnden,  // Esperando a que llegue el ascensor
    Subiendo,          // Haciendo "Pop" para subir
    Terminado          // Ya se subió
}

public enum RolPasajero
{
    SubeEnInicio,  // Sube cuando el ascensor está lejos (abajo)
    SubeEnFinal    // Sube cuando el ascensor está cerca (arriba)
}

public class Pasajero : MonoBehaviour
{
    [Header("=== CONFIGURACIÓN ===")]
    public float duracionPop = 0.2f; // Cuánto tarda el efecto "Pop"

    [Header("=== ESTADO ===")]
    public EstadoPasajero estado = EstadoPasajero.EsperandoEnAnden;
    public RolPasajero rol = RolPasajero.SubeEnInicio;

    private AscensorManager ascensorManager;
    private int ascensorIndex = 0;
    private bool inicializado = false;
    private Vector3 escalaOriginal;

    public Action<Pasajero> OnPasajeroTerminado;

    // === INICIALIZACIÓN ===
    public void Inicializar(RolPasajero rolAsignado, int index, AscensorManager manager)
    {
        rol = rolAsignado;
        ascensorIndex = index;
        ascensorManager = manager;
        estado = EstadoPasajero.EsperandoEnAnden;
        inicializado = true;

        // 🔑 Guardar la escala original y empezar desde 0
        escalaOriginal = transform.localScale;
        transform.localScale = Vector3.zero;

        // 🔑 Hacer "Pop" al aparecer
        StartCoroutine(EfectoPop());
    }

    // === UPDATE ===
    private void Update()
    {
        if (!inicializado || ascensorManager == null) return;

        if (estado == EstadoPasajero.EsperandoEnAnden)
        {
            ProcesarEspera();
        }
    }

    // === ESPERANDO ===
    private void ProcesarEspera()
    {
        var asc = ascensorManager.ascensores[ascensorIndex];
        bool ascensorEstaAbajo = asc.estaAbajo;
        bool ascensorEnMovimiento = asc.enMovimiento;

        bool ascensorEnMiLugar = (rol == RolPasajero.SubeEnInicio && ascensorEstaAbajo)
                              || (rol == RolPasajero.SubeEnFinal && !ascensorEstaAbajo);

        if (ascensorEnMiLugar && !ascensorEnMovimiento)
        {
            IniciarSubida();
        }
    }

    // === SUBIENDO (POP) ===
    private void IniciarSubida()
    {
        estado = EstadoPasajero.Subiendo;

        // 🔑 Verificar que haya espacio
        if (!ascensorManager.HayEspacioEn(ascensorIndex))
        {
            Debug.LogWarning($"⚠️ Ascensor {ascensorIndex} lleno. Pasajero se queda esperando.");
            estado = EstadoPasajero.EsperandoEnAnden;
            return;
        }

        // 🔑 Sumar al contador del ascensor
        ascensorManager.IntentarSubirPasajero(ascensorIndex);

        // 🔑 Registrar en el resumen
        if (GameManager.Instance != null && GameManager.Instance.ResumenDia != null)
            GameManager.Instance.ResumenDia.RegistrarPasajeroTransportado();

        // 🔑 Hacer "Pop" y desaparecer
        StartCoroutine(PopYDesaparecer());
    }

    private IEnumerator PopYDesaparecer()
    {
        // 🔑 Pop: crecer rápido y desaparecer
        float timer = 0f;
        while (timer < duracionPop)
        {
            timer += Time.deltaTime;
            float t = timer / duracionPop;

            // Crecer y achicar al mismo tiempo
            float escala = Mathf.Lerp(1.5f, 0f, t);
            transform.localScale = escalaOriginal * escala;

            yield return null;
        }

        // 🔑 Terminar
        estado = EstadoPasajero.Terminado;
        OnPasajeroTerminado?.Invoke(this);
        Destroy(gameObject);
    }

    // === POP DE APARICIÓN ===
    private IEnumerator EfectoPop()
    {
        float timer = 0f;
        while (timer < duracionPop)
        {
            timer += Time.deltaTime;
            float t = timer / duracionPop;

            // 🔑 Curva de rebote (crece y se pasa un poco, luego vuelve)
            float escala = Mathf.Sin(t * Mathf.PI) * 1.2f;
            transform.localScale = escalaOriginal * escala;

            yield return null;
        }
        transform.localScale = escalaOriginal;
    }
}