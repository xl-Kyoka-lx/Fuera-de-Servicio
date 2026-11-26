using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Datos de un pasajero bondadoso (vienen de Dialogo_Personajes_Buenos.json)
[Serializable]
public class PasajeroBondadosoData
{
    public string id;
    public string nombre;
    public List<string> dialogos;
    public int dineroExtra;
    public bool soloTutorial;   // true = no sale en las partidas normales (Camila)
}

[Serializable]
public class PasajerosBondadososWrapper
{
    public List<PasajeroBondadosoData> pasajerosBondadosos;
}

// Pasajeros bondadosos: de vez en cuando uno de los que suben es "bondadoso".
// Cuando baja del ascensor se acerca a conversar (diálogo) y deja una propina.
//
// - En el tutorial, el TutorialManager pide a Camila con PedirCamila().
// - En las partidas normales sale uno cada cierto tiempo, sin repetirse en el mismo día.
//
// Poner este componente en un objeto de la escena del día.
public class BondadososManager : MonoBehaviour
{
    public static BondadososManager Instance { get; private set; }

    [Header("Referencias")]
    [Tooltip("Dialogo_Personajes_Buenos.json")]
    public TextAsset archivoJson;
    [Tooltip("Si se deja vacío se busca solo")]
    public AscensorManager ascensorManager;

    [Header("Cada cuánto aparece uno (segundos de juego, sin contar diálogos ni eventos)")]
    public float primeraAparicionMin = 60f;
    public float primeraAparicionMax = 90f;
    public float intervaloMin = 90f;
    public float intervaloMax = 150f;
    [Tooltip("Máximo de bondadosos por día")]
    public int maximoPorDia = 4;

    [Header("Dinero")]
    [Tooltip("Multiplica la propina del JSON. 1 = tal cual. Sirve para ajustar la economía sin tocar el JSON")]
    public float multiplicadorDinero = 1f;

    [Header("Tutorial")]
    public string idCamila = "camila_vecina";

    // El tutorial lo usa para saber cuándo seguir
    public event Action OnCamilaAsignada;
    public event Action<string> OnEncuentroTerminado;   // id del bondadoso que terminó de conversar

    // true desde que Camila quedó asignada a un pasajero hasta que termina de conversar
    public bool CamilaEnViaje { get; private set; }

    private PasajeroBondadosoData camila;
    private readonly List<PasajeroBondadosoData> pool = new List<PasajeroBondadosoData>();

    private float timer;
    private bool pendienteAleatorio = false;   // le toca a uno: será el próximo que suba
    private bool camilaPendiente = false;
    private int apariciones = 0;

    private void Awake()
    {
        Instance = this;
        CargarJson();
        timer = UnityEngine.Random.Range(primeraAparicionMin, primeraAparicionMax);
    }

    [Obsolete]
    private void Start()
    {
        if (ascensorManager == null)
            ascensorManager = FindFirstObjectByType<AscensorManager>();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void CargarJson()
    {
        if (archivoJson == null)
        {
            Debug.LogError("BondadososManager: falta asignar el archivo Dialogo_Personajes_Buenos.json");
            return;
        }

        try
        {
            var wrapper = JsonUtility.FromJson<PasajerosBondadososWrapper>(archivoJson.text);

            foreach (var d in wrapper.pasajerosBondadosos)
            {
                if (d.id == idCamila) camila = d;

                if (!d.soloTutorial && d.id != idCamila)
                    pool.Add(d);
            }

            Debug.Log($"Bondadosos: {pool.Count} para partidas normales. Camila: {(camila != null ? "OK" : "NO ENCONTRADA")}");
        }
        catch (Exception e)
        {
            Debug.LogError($"BondadososManager: error al leer el JSON: {e.Message}");
        }
    }

    private void Update()
    {
        // En el tutorial solo aparece Camila, cuando el tutorial la pide
        if (TutorialManager.EnCurso) return;
        if (pendienteAleatorio || apariciones >= maximoPorDia || pool.Count == 0) return;
        if (Time.timeScale <= 0f) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
            pendienteAleatorio = true;
    }

    // ---------- Quién es bondadoso ----------

    // Lo llama Pasajero cada vez que sube a una cabina
    public void AlSubirPasajero(Pasajero p)
    {
        if (p == null || p.EsBondadoso) return;

        if (camilaPendiente)
        {
            AsignarCamila(p);
            return;
        }

        if (!pendienteAleatorio || TutorialManager.EnCurso) return;

        var elegido = ElegirAleatorio();
        pendienteAleatorio = false;
        if (elegido == null) return;

        p.MarcarBondadoso(elegido);
        apariciones++;
        timer = UnityEngine.Random.Range(intervaloMin, intervaloMax);

        Debug.Log($"Bondadoso: {elegido.nombre} subió al ascensor");
    }

    private PasajeroBondadosoData ElegirAleatorio()
    {
        if (pool.Count == 0) return null;

        int i = UnityEngine.Random.Range(0, pool.Count);
        var d = pool[i];
        pool.RemoveAt(i);   // no se repite en el mismo día
        return d;
    }

    // El tutorial pide a Camila: será un pasajero que ya esté dentro o, si no hay, el próximo que suba
    public void PedirCamila()
    {
        if (camila == null)
        {
            Debug.LogWarning("Bondadosos: no se encontró a Camila en el JSON. El tutorial sigue sin ella.");
            OnCamilaAsignada?.Invoke();
            return;
        }

        camilaPendiente = true;

        if (ascensorManager == null) return;

        foreach (var asc in ascensorManager.ascensores)
        {
            foreach (var p in asc.pasajerosDentro)
            {
                if (p != null && !p.EsBondadoso)
                {
                    AsignarCamila(p);
                    return;
                }
            }
        }
    }

    // El tutorial se cansó de esperar (no subió nadie): Camila ya no se asigna a destiempo
    public void CancelarCamila()
    {
        camilaPendiente = false;
    }

    private void AsignarCamila(Pasajero p)
    {
        camilaPendiente = false;
        CamilaEnViaje = true;
        p.MarcarBondadoso(camila);
        OnCamilaAsignada?.Invoke();
    }

    // ---------- El encuentro (cuando baja del ascensor) ----------

    // Lo ejecuta el Pasajero al bajar: conversa y deja la propina
    public IEnumerator Encuentro(Pasajero p, PasajeroBondadosoData d)
    {
        var dialogos = DialogoManager.Instance;

        // Esperar a que no haya otro diálogo ni un evento en curso (un evento congela el tiempo y no se puede mezclar)
        float limite = Time.unscaledTime + 30f;
        while ((HayDialogo() || HayEvento()) && Time.unscaledTime < limite)
            yield return null;

        if (dialogos == null || HayDialogo() || HayEvento())
        {
            // No se pudo conversar: igual deja la propina
            Pagar(d);
            TerminarEncuentro(d);
            yield break;
        }

        var lineas = CrearLineas(d);
        if (lineas.Count == 0)
        {
            // Sin textos no hay nada que mostrar (y esperar el diálogo dejaría al pasajero colgado)
            Pagar(d);
            TerminarEncuentro(d);
            yield break;
        }

        string idDialogo = "bondadoso_" + d.id;
        bool terminado = false;
        Action<string> alTerminar = (id) => { if (id == idDialogo) terminado = true; };

        dialogos.OnDialogoTerminado += alTerminar;
        dialogos.MostrarDialogoDirecto(idDialogo, lineas);

        // El diálogo pausa el juego (timeScale 0): se espera por frames, no por tiempo
        while (!terminado)
            yield return null;

        dialogos.OnDialogoTerminado -= alTerminar;

        Pagar(d);
        TerminarEncuentro(d);
    }

    private void TerminarEncuentro(PasajeroBondadosoData d)
    {
        if (d.id == idCamila) CamilaEnViaje = false;
        OnEncuentroTerminado?.Invoke(d.id);
    }

    private void Pagar(PasajeroBondadosoData d)
    {
        int monto = Mathf.Max(0, Mathf.RoundToInt(d.dineroExtra * multiplicadorDinero));

        if (GameManager.Instance != null)
            GameManager.Instance.SumarDinero(monto);

        if (PanelMensajesMonitor.Instance != null)
            PanelMensajesMonitor.Instance.MostrarMensaje($"{d.nombre} te dejó ${monto} de propina", new Color(0.4f, 1f, 0.5f), 3f);
    }

    private bool HayDialogo()
    {
        return DialogoManager.Instance != null && DialogoManager.Instance.HayDialogoActivo();
    }

    private bool HayEvento()
    {
        return GameManager.Instance != null && GameManager.Instance.GameData.eventoOcurriendo;
    }

    // Convierte los textos del JSON en líneas de diálogo.
    // Si un texto empieza con "Nombre: " (como en Delilah y su papá), ese nombre es el que habla.
    private static List<LineaDialogo> CrearLineas(PasajeroBondadosoData d)
    {
        var lineas = new List<LineaDialogo>();
        if (d.dialogos == null) return lineas;

        foreach (string texto in d.dialogos)
        {
            string hablante = d.nombre;
            string contenido = texto;

            int idx = texto.IndexOf(':');
            if (idx > 0 && idx <= 15 && !texto.Substring(0, idx).Contains(" "))
            {
                hablante = texto.Substring(0, idx).Trim();
                contenido = texto.Substring(idx + 1).Trim();
            }

            lineas.Add(new LineaDialogo { hablante = hablante, texto = contenido, imagen = "" });
        }

        return lineas;
    }
}