using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable]
public class PasajeroBondadosoData
{
    public string id;
    public string nombre;
    public List<string> dialogos;
    public int dineroExtra;
    [Tooltip("Limpieza que sube al terminar el encuentro, o al subirse de vuelta si 'vuelve' es true (0 = nada)")]
    public float limpiezaExtra;
    [Tooltip("Si es true aparece una sola vez en toda la partida (ej: un turista)")]
    public bool unaSolaVez;
    [Tooltip("Si es true, después del diálogo vuelve a hacer fila en esa misma estación para devolverse (ej: se equivocó de dirección)")]
    public bool vuelve;
    [Tooltip("Si es true, la primera vez solo aparece en pasajeros que suben desde la estación de abajo")]
    public bool soloSubiendo;
    public bool soloTutorial;
    [Tooltip("Si es true, el primer diálogo solo se dice en el tutorial. En partidas normales siempre sale una de las otras variantes")]
    public bool primerDialogoSoloTutorial;

    [Tooltip("Diálogos para las apariciones que no son la primera de la partida. Se elige uno al azar. " +
             "Si está vacío se repite 'dialogos'")]
    public List<VarianteBondadoso> variantes;

    [Tooltip("Formato del JSON: lista de diálogos. El primero es el de la primera vez; los demás salen al azar después")]
    public List<LineasDialogo> variantesDialogo;

    public PasajeroBondadosoData Copia() => (PasajeroBondadosoData)MemberwiseClone();
}

[Serializable]
public class LineasDialogo
{
    public List<string> lineas;
}

[Serializable]
public class VarianteBondadoso
{
    public List<string> dialogos;
    public int dineroExtra;
    public float limpiezaExtra;
}

[Serializable]
public class PasajerosBondadososWrapper
{
    public List<PasajeroBondadosoData> pasajerosBondadosos;
}

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

    [Header("Diálogo raro (easter egg)")]
    [Tooltip("Cada pasajero que sube tiene 1 posibilidad en este número de decir el diálogo raro. 137500 = 0,000727 %. 0 = nunca")]
    public int dialogoRaroUnoEn = 137500;
    public string dialogoRaroHablante = "Pasajero";
    [TextArea] public string dialogoRaroTexto = "Sabes... si viste esto, habrías podido ganar el token pirata.";

    // Para pruebas: el próximo pasajero que suba dice el diálogo raro sí o sí
    public static bool ForzarDialogoRaro;

    public event Action OnCamilaAsignada;
    public event Action<string> OnEncuentroTerminado;

    public bool CamilaEnViaje { get; private set; }

    private PasajeroBondadosoData camila;
    private readonly List<PasajeroBondadosoData> pool = new List<PasajeroBondadosoData>();

    private float timer;
    private bool pendienteAleatorio = false;
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
            var wrapper = JsonUtility.FromJson<PasajerosBondadososWrapper>(ConvertirVariantes(archivoJson.text));

            foreach (var d in wrapper.pasajerosBondadosos)
            {
                AplicarVariantesDialogo(d);

                if (d.id == idCamila) camila = d;

                if (!d.soloTutorial && TieneDialogoFueraDelTutorial(d))
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
        if (TutorialManager.EnCurso) return;
        if (pendienteAleatorio || apariciones >= maximoPorDia || pool.Count == 0) return;
        if (Time.timeScale <= 0f) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
            pendienteAleatorio = true;
    }

    public void AlSubirPasajero(Pasajero p)
    {
        if (p == null || p.EsBondadoso) return;

        if (camilaPendiente)
        {
            AsignarCamila(p);
            return;
        }

        if (IntentarDialogoRaro(p)) return;

        if (!pendienteAleatorio || TutorialManager.EnCurso) return;

        var elegido = ElegirAleatorio(p);
        if (elegido == null)
        {
            // Si queda alguno pero no le sirve este pasajero (ej: Luis y alguien que baja), se espera al siguiente
            if (pool.Count == 0) pendienteAleatorio = false;
            return;
        }
        pendienteAleatorio = false;

        var aparicion = PrepararAparicion(elegido);
        MarcarVisto(elegido);

        p.MarcarBondadoso(aparicion);
        apariciones++;
        timer = UnityEngine.Random.Range(intervaloMin, intervaloMax);

        Debug.Log($"Bondadoso: {aparicion.nombre} subió al ascensor");
    }

    private bool IntentarDialogoRaro(Pasajero p)
    {
        if (TutorialManager.EnCurso || string.IsNullOrEmpty(dialogoRaroTexto)) return false;

        bool sale = ForzarDialogoRaro ||
                    (dialogoRaroUnoEn > 0 && UnityEngine.Random.Range(0, dialogoRaroUnoEn) == 0);
        if (!sale) return false;

        ForzarDialogoRaro = false;
        p.MarcarBondadoso(new PasajeroBondadosoData
        {
            id = "dialogo_raro",
            nombre = dialogoRaroHablante,
            dialogos = new List<string> { dialogoRaroTexto },
        });

        Debug.Log("Bondadosos: ¡salió el diálogo raro del token pirata!");
        return true;
    }

    private static bool VistoEnLaPartida(PasajeroBondadosoData d)
    {
        if (GameManager.Instance == null) return false;
        var vistos = GameManager.Instance.GameData.bondadososVistos;
        return vistos != null && vistos.Contains(d.id);
    }

    private static bool YaNoPuedeAparecer(PasajeroBondadosoData d)
    {
        return d.unaSolaVez && VistoEnLaPartida(d);
    }

    private static void MarcarVisto(PasajeroBondadosoData d)
    {
        if (GameManager.Instance == null) return;
        var data = GameManager.Instance.GameData;
        if (data.bondadososVistos == null) data.bondadososVistos = new List<string>();
        if (!data.bondadososVistos.Contains(d.id)) data.bondadososVistos.Add(d.id);
    }

    private static bool LeSirve(PasajeroBondadosoData d, Pasajero p)
    {
        if (!d.soloSubiendo || VistoEnLaPartida(d)) return true;
        return p.rol == RolPasajero.SubeEnInicio;
    }

    /// <summary>
    /// La primera vez en la partida se usa tal cual. Las siguientes, si tiene variantes,
    /// se usa una al azar (sin 'vuelve' ni 'soloSubiendo', que son de la primera vez).
    /// </summary>
    private static bool TieneDialogoFueraDelTutorial(PasajeroBondadosoData d)
    {
        if (!d.primerDialogoSoloTutorial) return true;
        return d.variantes != null && d.variantes.Exists(v => v != null && v.dialogos != null && v.dialogos.Count > 0);
    }

    private static PasajeroBondadosoData PrepararAparicion(PasajeroBondadosoData d)
    {
        bool usarPrimero = !VistoEnLaPartida(d) && !d.primerDialogoSoloTutorial;
        if (usarPrimero || d.variantes == null) return d;

        var validas = d.variantes.FindAll(v => v != null && v.dialogos != null && v.dialogos.Count > 0);
        if (validas.Count == 0) return d;

        var v = validas[UnityEngine.Random.Range(0, validas.Count)];
        var copia = d.Copia();
        copia.dialogos = v.dialogos;
        copia.dineroExtra = v.dineroExtra;
        copia.limpiezaExtra = v.limpiezaExtra;
        copia.vuelve = false;
        copia.soloSubiendo = false;
        return copia;
    }

    private PasajeroBondadosoData ElegirAleatorio(Pasajero p)
    {
        pool.RemoveAll(YaNoPuedeAparecer);
        if (pool.Count == 0) return null;

        var candidatos = pool.FindAll(d => LeSirve(d, p));
        if (candidatos.Count == 0) return null;

        var elegido = candidatos[UnityEngine.Random.Range(0, candidatos.Count)];
        pool.Remove(elegido);
        return elegido;
    }

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

    public void CancelarCamila()
    {
        camilaPendiente = false;
    }

    private void AsignarCamila(Pasajero p)
    {
        camilaPendiente = false;
        CamilaEnViaje = true;
        // Ya salió hoy en el tutorial: que no vuelva a aparecer el mismo día
        pool.RemoveAll(d => d.id == idCamila);
        p.MarcarBondadoso(camila);
        OnCamilaAsignada?.Invoke();
    }

    public IEnumerator Encuentro(Pasajero p, PasajeroBondadosoData d)
    {
        var dialogos = DialogoManager.Instance;

        float limite = Time.unscaledTime + 30f;
        while ((HayDialogo() || HayEvento()) && Time.unscaledTime < limite)
            yield return null;

        if (dialogos == null || HayDialogo() || HayEvento())
        {
            Pagar(d);
            TerminarEncuentro(d);
            yield break;
        }

        var lineas = CrearLineas(d);
        if (lineas.Count == 0)
        {
            Pagar(d);
            TerminarEncuentro(d);
            yield break;
        }

        string idDialogo = "bondadoso_" + d.id;
        bool terminado = false;
        Action<string> alTerminar = (id) => { if (id == idDialogo) terminado = true; };

        dialogos.OnDialogoTerminado += alTerminar;
        dialogos.MostrarDialogoDirecto(idDialogo, lineas);

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

        if (monto > 0)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.SumarDinero(monto);

            if (PanelMensajesMonitor.Instance != null)
                PanelMensajesMonitor.Instance.MostrarMensaje($"{d.nombre} te dejó ${monto} de propina", new Color(0.4f, 1f, 0.5f), 3f);
        }

        if (!d.vuelve)
            DarLimpieza(d);
    }

    public void AlSubirDeRegreso(Pasajero p)
    {
        if (p == null || p.datosRegreso == null) return;
        DarLimpieza(p.datosRegreso);
    }

    private static void DarLimpieza(PasajeroBondadosoData d)
    {
        if (d.limpiezaExtra <= 0f) return;

        if (GameManager.Instance != null)
            GameManager.Instance.ModificarLimpieza(d.limpiezaExtra);

        if (PanelMensajesMonitor.Instance != null)
            PanelMensajesMonitor.Instance.MostrarMensaje($"{d.nombre} te ayudó a limpiar el carro (+{d.limpiezaExtra:F0}% limpieza)", new Color(0.4f, 0.8f, 1f), 3f);
    }

    private bool HayDialogo()
    {
        return DialogoManager.Instance != null && DialogoManager.Instance.HayDialogoActivo();
    }

    private bool HayEvento()
    {
        return GameManager.Instance != null && GameManager.Instance.GameData.eventoOcurriendo;
    }

    /// <summary>
    /// Pasa "variantesDialogo" a 'dialogos' (la primera) y 'variantes' (el resto).
    /// Las variantes conservan la propina; la limpieza extra es solo de la primera vez.
    /// </summary>
    private static void AplicarVariantesDialogo(PasajeroBondadosoData d)
    {
        if (d.variantesDialogo == null || d.variantesDialogo.Count == 0) return;

        var listas = d.variantesDialogo.FindAll(v => v != null && v.lineas != null && v.lineas.Count > 0);
        if (listas.Count == 0) return;

        d.dialogos = listas[0].lineas;
        d.variantes = new List<VarianteBondadoso>();
        for (int i = 1; i < listas.Count; i++)
            d.variantes.Add(new VarianteBondadoso { dialogos = listas[i].lineas, dineroExtra = d.dineroExtra });
    }

    /// <summary>
    /// JsonUtility no entiende listas dentro de listas. Esto cambia
    /// "variantesDialogo": [[...], [...]]  por  "variantesDialogo": [{"lineas":[...]}, {"lineas":[...]}]
    /// antes de leer el JSON.
    /// </summary>
    private static string ConvertirVariantes(string json)
    {
        const string clave = "\"variantesDialogo\"";
        var sb = new StringBuilder(json.Length + 256);
        int i = 0;

        while (true)
        {
            int k = json.IndexOf(clave, i, StringComparison.Ordinal);
            if (k < 0)
            {
                sb.Append(json, i, json.Length - i);
                break;
            }

            int j = k + clave.Length;
            while (j < json.Length && (char.IsWhiteSpace(json[j]) || json[j] == ':')) j++;

            sb.Append(json, i, j - i);
            i = j;
            if (j >= json.Length || json[j] != '[') continue;

            // Recorre el arreglo y envuelve cada lista de primer nivel en {"lineas": ...}
            var envueltos = new Stack<bool>();
            bool enTexto = false;
            int llaves = 0;

            do
            {
                char c = json[j];

                if (enTexto)
                {
                    sb.Append(c);
                    if (c == '\\' && j + 1 < json.Length) { sb.Append(json[j + 1]); j += 2; continue; }
                    if (c == '"') enTexto = false;
                }
                else if (c == '"') { enTexto = true; sb.Append(c); }
                else if (c == '{') { llaves++; sb.Append(c); }
                else if (c == '}') { llaves--; sb.Append(c); }
                else if (c == '[')
                {
                    bool envolver = envueltos.Count == 1 && llaves == 0;
                    envueltos.Push(envolver);
                    sb.Append(envolver ? "{\"lineas\":[" : "[");
                }
                else if (c == ']')
                {
                    bool envuelto = envueltos.Count > 0 && envueltos.Pop();
                    sb.Append(envuelto ? "]}" : "]");
                }
                else sb.Append(c);

                j++;
            } while (j < json.Length && envueltos.Count > 0);

            i = j;
        }

        return sb.ToString();
    }

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

                // "Tu:" o "Tú:" es el protagonista
                if (hablante.Equals("Tu", StringComparison.OrdinalIgnoreCase) ||
                    hablante.Equals("Tú", StringComparison.OrdinalIgnoreCase))
                    hablante = "Tú";
            }

            lineas.Add(new LineaDialogo { hablante = hablante, texto = contenido, imagen = "" });
        }

        return lineas;
    }
}