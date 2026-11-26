using UnityEngine;
using System;
using System.Collections.Generic;

public class AscensorManager : MonoBehaviour
{
    [System.Serializable]
    public class AscensorData
    {
        public string nombre;
        public Transform ascensorTransform;

        [Header("Puntos de ruta")]
        public Transform puntoLejos;
        public Transform puntoCerca;

        [Header("Escalas")]
        public float escalaLejos = 0.5f;
        public float escalaCerca = 1.2f;

        [Header("Punto interior")]
        public Transform puntoInterior;

        [Header("=== PUNTOS DE SALIDA ===")]
        public Transform puntoSalidaLejos;
        public Transform puntoSalidaCerca;

        [Header("=== ESTADO INICIAL ===")]
        public bool empiezaAbajo = true;

        [Header("=== ESTADO ACTUAL (Runtime) ===")]
        [Range(0f, 1f)]
        public float progreso = 0f;
        [HideInInspector] public bool estaAbajo = true;
        [HideInInspector] public bool enMovimiento = false;

        [HideInInspector] public List<Pasajero> pasajerosDentro = new List<Pasajero>();

        public int CapacidadMaxima => 5;
        public bool HayEspacio => pasajerosDentro.Count < CapacidadMaxima;
        public int PasajerosTotales => pasajerosDentro.Count;
    }

    public float umbralFrenado = 0.8f;

    public bool frenadoDisparado = false;

    public event Action OnPuntoDeFrenado;

    public List<AscensorData> ascensores = new List<AscensorData>();

    public float duracionViaje = 30f;

    [Tooltip("Segundos entre un pasajero y el siguiente al bajar de la cabina")]
    public float separacionAlBajar = 0.4f;

    public event Action OnLlegoAlInicio;
    public event Action OnLlegoAlFinal;
    public event Action OnSalioDelPunto;

    public event Action OnPasajerosCambiados;

    public bool enMovimiento
    {
        get
        {
            foreach (var asc in ascensores)
                if (asc.enMovimiento) return true;
            return false;
        }
    }
    public Transform ObtenerPuntoSalida(int ascensorIndex)
    {
        if (ascensorIndex < 0 || ascensorIndex >= ascensores.Count) return null;

        var asc = ascensores[ascensorIndex];
        return asc.estaAbajo ? asc.puntoSalidaLejos : asc.puntoSalidaCerca;
    }

    private void Start()
    {
        foreach (var asc in ascensores)
        {
            asc.estaAbajo = asc.empiezaAbajo;
            asc.progreso = asc.empiezaAbajo ? 0f : 1f;
            asc.enMovimiento = false;

            AplicarProgresoA(asc, asc.progreso);
        }
    }

    private void Update()
    {
        if (!enMovimiento) return;

        float delta = Time.deltaTime / duracionViaje;
        foreach (var asc in ascensores)
        {
            if (!asc.enMovimiento) continue;

            if (asc.estaAbajo)
            {
                asc.progreso += delta;
                if (asc.progreso >= 1f)
                {
                    asc.progreso = 1f;
                    asc.enMovimiento = false;
                    asc.estaAbajo = false;
                }
            }
            else
            {
                asc.progreso -= delta;
                if (asc.progreso <= 0f)
                {
                    asc.progreso = 0f;
                    asc.enMovimiento = false;
                    asc.estaAbajo = true;
                }
            }

            AplicarProgresoA(asc, asc.progreso);
        }

        VerificarUmbralFrenado();
        if (!enMovimiento)
        {
            LlegarADestinoGlobal();
        }
    }

    public bool HayEspacioEn(int ascensorIndex)
    {
        if (ascensorIndex < 0 || ascensorIndex >= ascensores.Count) return false;
        return ascensores[ascensorIndex].HayEspacio;
    }

    public bool IntentarSubirPasajero(int ascensorIndex, Pasajero pasajero)
    {
        if (ascensorIndex < 0 || ascensorIndex >= ascensores.Count) return false;

        var asc = ascensores[ascensorIndex];
        if (!asc.HayEspacio) return false;

        asc.pasajerosDentro.Add(pasajero);
        OnPasajerosCambiados?.Invoke();
        Debug.Log($"👥 Ascensor {ascensorIndex}: {asc.PasajerosTotales}/{asc.CapacidadMaxima} pasajeros");
        return true;
    }

    public int ObtenerPasajeros(int ascensorIndex)
    {
        if (ascensorIndex < 0 || ascensorIndex >= ascensores.Count) return 0;
        return ascensores[ascensorIndex].PasajerosTotales;
    }

    private void VerificarUmbralFrenado()
    {
        if (frenadoDisparado) return;

        bool pasoElUmbral = false;

        if (ascensores.Count == 0) return;

        var asc = ascensores[0]; 

        if (asc.estaAbajo && asc.progreso >= umbralFrenado)
        {
            pasoElUmbral = true;
        }
        else if (!asc.estaAbajo && asc.progreso <= (1f - umbralFrenado))
        {
            pasoElUmbral = true;
        }

        if (pasoElUmbral)
        {
            frenadoDisparado = true;
            OnPuntoDeFrenado?.Invoke();
        }
    }

    private void AplicarProgresoA(AscensorData asc, float t)
    {
        if (asc.ascensorTransform == null) return;
        if (asc.puntoLejos == null || asc.puntoCerca == null) return;

        float tSuave = Mathf.SmoothStep(0f, 1f, t);

        Vector3 posicion = Vector3.Lerp(asc.puntoLejos.position, asc.puntoCerca.position, tSuave);
        asc.ascensorTransform.position = posicion;

        float escala = Mathf.Lerp(asc.escalaLejos, asc.escalaCerca, tSuave);
        asc.ascensorTransform.localScale = Vector3.one * escala;
    }

    private void LlegarADestinoGlobal()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.GameData.ascensorEnMovimiento = false;

        for (int i = 0; i < ascensores.Count; i++)
        {
            BajarPasajerosDelAscensor(i);
        }
        
        bool todosAbajo = true;
        bool todosArriba = true;

        foreach (var asc in ascensores)
        {
            if (asc.estaAbajo) todosArriba = false;
            else todosAbajo = false;
        }

        if (todosAbajo)
        {
            OnLlegoAlInicio?.Invoke();
        }
        else if (todosArriba)
        {
            OnLlegoAlFinal?.Invoke();
        }
        else
        {
            Debug.Log("Los ascensores llegaron a destinos mixtos");
        }
    }

    public void IniciarViaje()
    {
        if (enMovimiento) return;

        frenadoDisparado = false;

        OnSalioDelPunto?.Invoke();

        foreach (var asc in ascensores)
        {
            asc.enMovimiento = true;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.GameData.ascensorEnMovimiento = true;
    }

    public bool EstaEnPuntoInicio() => !enMovimiento && TodosAbajo();
    public bool EstaEnPuntoFin() => !enMovimiento && TodosArriba();

    private bool TodosAbajo()
    {
        foreach (var asc in ascensores)
            if (!asc.estaAbajo) return false;
        return true;
    }

    private bool TodosArriba()
    {
        foreach (var asc in ascensores)
            if (asc.estaAbajo) return false;
        return true;
    }

    public bool EstaAbajo(int index)
    {
        if (index < 0 || index >= ascensores.Count) return false;
        return ascensores[index].estaAbajo;
    }

    public float ObtenerProgreso(int index)
    {
        if (index < 0 || index >= ascensores.Count) return 0f;
        return ascensores[index].progreso;
    }

    private void BajarPasajerosDelAscensor(int ascensorIndex)
    {
        var asc = ascensores[ascensorIndex];

        if (asc.pasajerosDentro.Count == 0) return;

        Debug.Log($"🚶 Ascensor {ascensorIndex}: bajando {asc.pasajerosDentro.Count} pasajeros");

        Transform puntoSalida = asc.estaAbajo ? asc.puntoSalidaLejos : asc.puntoSalidaCerca;
        if (puntoSalida == null) return;

        var pasajerosACopiar = new List<Pasajero>(asc.pasajerosDentro);
        asc.pasajerosDentro.Clear();
        OnPasajerosCambiados?.Invoke();

        Vector3 puerta = asc.ascensorTransform != null ? asc.ascensorTransform.position : puntoSalida.position;
        int orden = 0;
        foreach (var pasajero in pasajerosACopiar)
        {
            if (pasajero != null)
            {
                pasajero.BajarDelAscensor(puntoSalida.position, puerta, asc.estaAbajo, orden * separacionAlBajar);
                orden++;
            }
        }
    }
}