using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogoManager : MonoBehaviour
{
    public static DialogoManager Instance { get; private set; }

    public event System.Action<string> OnDialogoTerminado;

    public TextAsset archivoJson;

    public GameObject panelDialogo;
    public TextMeshProUGUI textoHablante;
    public TextMeshProUGUI textoContenido;
    public GameObject indicadorContinuar; 

    [System.Serializable]
    public class Retrato
    {
        [Tooltip("Nombre del personaje tal como aparece en 'hablante' (por ejemplo Camila), o el valor de 'imagen' en el JSON")]
        public string nombre;
        public Sprite sprite;
    }

    [Header("Retratos")]
    [Tooltip("Image del panel de diálogo donde se muestra la cara de quien habla. Se oculta si el personaje no tiene retrato")]
    public Image imagenRetrato;
    [Tooltip("Retrato de cada personaje")]
    public List<Retrato> retratos = new List<Retrato>();

    [Tooltip("Ya no se usa: la velocidad sale de la barra Velocidad_Texto en Opciones")]
    public float velocidadTipeo = 0.03f;
    public InputActionAsset inputActions;
    public string actionMapName = "Gameplay";
    public string actionName = "Avanzar texto";

    private InputAction inputContinuar;

    private List<Dialogo> dialogosCargados = new List<Dialogo>();
    private Dialogo dialogoActual;
    private int indiceLineaActual = 0;
    private bool escribiendo = false;
    private bool esperandoInput = false;
    private Coroutine corrutinaTipeo;

    public GameData gameDataFallback;

    private void Awake()
    {
        Instance = this; 

        CargarDialogos();

        if (inputActions != null)
        {
            Controles.Aplicar(inputActions);
            inputContinuar = inputActions.FindActionMap(actionMapName)?.FindAction(actionName);
        }
    }
    private void Start()
    {
        if (panelDialogo != null) panelDialogo.SetActive(false);
        if (indicadorContinuar != null) indicadorContinuar.SetActive(false);

        ValidarPlaceholders();
    }

    private void ValidarPlaceholders()
    {
        string[] validos = {
        "dineroGanado", "pasajerosTransportados", "eventos",
        "dineroTotal", "limpieza", "polea", "hora", "dia", "semana",
        "teclaAcelerar", "teclaFrenar", "teclaTimbre", "teclaPausa", "teclaContinuar"
    };

        foreach (var dialogo in dialogosCargados)
        {
            foreach (var linea in dialogo.lineas)
            {
                var matches = System.Text.RegularExpressions.Regex.Matches(
                    linea.texto, @"\{([^}]+)\}");

                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    string clave = match.Groups[1].Value.Trim();

                    if (System.Array.IndexOf(validos, clave) < 0)
                    {
                        Debug.LogWarning($"Placeholder desconocido '{{{clave}}}' en diálogo '{dialogo.id}'");
                    }
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnEnable()
    {
        if (inputContinuar != null)
        {
            inputContinuar.Enable();
            inputContinuar.performed += OnContinuarPerformed;
        }
    }

    private void OnDisable()
    {
        if (inputContinuar != null)
        {
            inputContinuar.performed -= OnContinuarPerformed;
            inputContinuar.Disable();
        }
    }

    private void CargarDialogos()
    {
        if (archivoJson == null)
        {
            Debug.LogError("DialogoManager: archivoJson no asignado");
            return;
        }

        try
        {
            DialogosWrapper wrapper = JsonUtility.FromJson<DialogosWrapper>(archivoJson.text);
            dialogosCargados = wrapper.dialogos;
            Debug.Log($"{dialogosCargados.Count} diálogos cargados");
        }
        catch (System.Exception e)
        {
            Debug.LogError($" Error al parsear diálogos: {e.Message}");
        }
    }

    public void MostrarDialogo(string id)
    {
        Dialogo dialogo = dialogosCargados.Find(d => d.id == id);

        if (dialogo == null)
        {
            Debug.LogWarning($" Diálogo '{id}' no encontrado");
            return;
        }

        dialogoActual = dialogo;
        indiceLineaActual = 0;

        if (EstadoJuegoManager.Instance != null)
            EstadoJuegoManager.Instance.CambiarEstado(EstadoJuego.EnDialogo);

        if (panelDialogo != null) panelDialogo.SetActive(true);
        MostrarLineaActual();
    }

    public void MostrarDialogoDirecto(string id, List<LineaDialogo> lineas)
    {
        if (lineas == null || lineas.Count == 0) return;

        dialogoActual = new Dialogo { id = id, lineas = lineas };
        indiceLineaActual = 0;

        if (EstadoJuegoManager.Instance != null)
            EstadoJuegoManager.Instance.CambiarEstado(EstadoJuego.EnDialogo);

        if (panelDialogo != null) panelDialogo.SetActive(true);
        MostrarLineaActual();
    }

    private void MostrarLineaActual()
    {
        if (dialogoActual == null) return;
        if (indiceLineaActual >= dialogoActual.lineas.Count)
        {
            TerminarDialogo();
            return;
        }

        LineaDialogo linea = dialogoActual.lineas[indiceLineaActual];

        if (textoHablante != null)
            textoHablante.text = linea.hablante;

        MostrarRetrato(linea);

        if (textoContenido != null)
        {
            textoContenido.text = "";
            if (corrutinaTipeo != null) StopCoroutine(corrutinaTipeo);
            corrutinaTipeo = StartCoroutine(EscribirTexto(linea.texto));
        }

        if (indicadorContinuar != null)
            indicadorContinuar.SetActive(false);
    }
    private IEnumerator EscribirTexto(string textoCompleto)
    {
        float timeout = 3f;
        float timer = 0f;

        while ((GameManager.Instance == null || GameManager.Instance.gameData == null) && timer < timeout)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        if (GameManager.Instance == null || GameManager.Instance.gameData == null)
        {
            Debug.LogError("GameManager nunca estuvo listo. Mostrando texto sin reemplazar.");
        }
        else
        {
            Debug.Log($"GameManager listo después de {timer:F2}s");
        }

        textoCompleto = ReemplazarPlaceholders(textoCompleto);

        escribiendo = true;
        esperandoInput = false;

        for (int i = 0; i < textoCompleto.Length; i++)
        {
            textoContenido.text = textoCompleto.Substring(0, i + 1);
            yield return PausaManager.Esperar(Configuracion.SegundosPorLetra);
        }

        textoContenido.text = textoCompleto;
        escribiendo = false;
        esperandoInput = true;

        if (indicadorContinuar != null)
            indicadorContinuar.SetActive(true);

        corrutinaTipeo = null;
    }

    private void Update()
    {
        if (dialogoActual == null || PausaManager.juegoPausado) return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            Continuar();
    }

    private void OnContinuarPerformed(InputAction.CallbackContext context)
    {
        Continuar();
    }

    private void Continuar()
    {
        if (dialogoActual == null) return;

        if (escribiendo)
        {
            if (corrutinaTipeo != null) StopCoroutine(corrutinaTipeo);
            corrutinaTipeo = null;

            LineaDialogo linea = dialogoActual.lineas[indiceLineaActual];
            textoContenido.text = ReemplazarPlaceholders(linea.texto);

            escribiendo = false;
            esperandoInput = true;

            if (indicadorContinuar != null)
                indicadorContinuar.SetActive(true);
            return;
        }

        if (esperandoInput)
        {
            SiguienteLinea();
        }
    }

    private void SiguienteLinea()
    {
        indiceLineaActual++;

        if (indiceLineaActual >= dialogoActual.lineas.Count)
        {
            TerminarDialogo();
        }
        else
        {
            MostrarLineaActual();
        }
    }

    private void TerminarDialogo()
    {
        string idTerminado = dialogoActual != null ? dialogoActual.id : "";

        dialogoActual = null;
        indiceLineaActual = 0;
        escribiendo = false;
        esperandoInput = false;

        if (panelDialogo != null) panelDialogo.SetActive(false);
        if (indicadorContinuar != null) indicadorContinuar.SetActive(false);

        if (EstadoJuegoManager.Instance != null)
        EstadoJuegoManager.Instance.CambiarEstado(EstadoJuego.Jugando);

        OnDialogoTerminado?.Invoke(idTerminado);
    }

    private string ReemplazarPlaceholders(string texto)
    {
        texto = Controles.ReemplazarTeclas(texto);

        if (GameManager.Instance == null || GameManager.Instance.ResumenDia == null)
        {
            Debug.LogWarning("⚠️ No hay ResumenDiaData disponible");
            return texto;
        }

        var data = GameManager.Instance.GameData;
        var resumen = GameManager.Instance.ResumenDia;

        texto = texto.Replace("{dineroGanado}", resumen.DineroGanado.ToString());
        texto = texto.Replace("{pasajerosTransportados}", resumen.PasajerosTransportados.ToString());
        texto = texto.Replace("{eventos}", resumen.eventosOcurridos.ToString());

        if (data != null)
        {
            texto = texto.Replace("{dineroTotal}", data.dineroTotal.ToString());
            texto = texto.Replace("{limpieza}", data.contadorLimpieza.ToString("F0"));
            texto = texto.Replace("{polea}", data.contadorGrasa.ToString("F0"));
            texto = texto.Replace("{hora}", data.horaActual.ToString());
            texto = texto.Replace("{dia}", data.diaActual.ToString());
            texto = texto.Replace("{semana}", data.Semana.ToString());
        }

        return texto;
    }

    public bool HayDialogoActivo() => dialogoActual != null;

    private void MostrarRetrato(LineaDialogo linea)
    {
        if (imagenRetrato == null) return;

        Sprite sprite = BuscarRetrato(linea.imagen);
        if (sprite == null) sprite = BuscarRetrato(linea.hablante);

        imagenRetrato.sprite = sprite;
        imagenRetrato.enabled = sprite != null;
    }

    private Sprite BuscarRetrato(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return null;

        string buscado = nombre.Trim();
        foreach (var r in retratos)
        {
            if (r != null && r.sprite != null && string.Equals(r.nombre?.Trim(), buscado, System.StringComparison.OrdinalIgnoreCase))
                return r.sprite;
        }
        return null;
    }
}