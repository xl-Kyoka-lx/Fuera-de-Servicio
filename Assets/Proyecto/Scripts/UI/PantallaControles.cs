using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Para cambiar el nombre de las teclas hay que ir a controles.cs
public class PantallaControles : MonoBehaviour
{
    [Serializable]
    public class Fila
    {
        [Tooltip("Acción del Input: MoverAscensor, Frenar, AvisarCabina, Pausa o Avanzar texto")]
        public string accion;
        [Tooltip("Botón que se pulsa para cambiar la tecla")]
        public Button boton;
        [Tooltip("Texto donde se muestra la tecla. Vacío = el texto dentro del botón")]
        public TextMeshProUGUI texto;
    }

    [Tooltip("Vacío = se buscan solas por nombre entre los hijos")]
    public List<Fila> filas = new List<Fila>();

    [Tooltip("Opcional: botón para volver a las teclas de siempre")]
    public Button botonRestablecer;

    [Header("Textos")]
    public string textoEsperando = "Presiona una tecla...";
    public Color colorEsperando = new Color(1f, 0.85f, 0.3f);
    [Tooltip("Tamaño mínimo al que se achica el texto de la tecla para caber en su cuadro")]
    public float tamanoMinimo = 18f;

    // Nombre del objeto de la fila -> acción
    private static readonly Dictionary<string, string> porNombre = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "Acelerar", Controles.Acelerar },
        { "Frenar", Controles.Frenar },
        { "Timbre", Controles.Timbre },
        { "Pausa", Controles.Pausa },
        { "Continuar", Controles.Continuar },
    };

    private InputActionRebindingExtensions.RebindingOperation operacion;
    private Fila filaEsperando;
    private Color colorOriginal;

    /// <summary>True mientras se espera una tecla (para que otros menús no reaccionen a Esc, por ejemplo).</summary>
    public static bool Esperando { get; private set; }

    private void Awake()
    {
        if (filas == null || filas.Count == 0) BuscarFilas();

        foreach (var f in filas)
        {
            if (f == null || f.boton == null) continue;
            if (f.texto == null) f.texto = f.boton.GetComponentInChildren<TextMeshProUGUI>(true);
            AjustarTexto(f.texto);

            var fila = f;
            f.boton.onClick.AddListener(() => EmpezarCambio(fila));
        }

        if (botonRestablecer == null)
        {
            var t = BuscarHijo(transform, "Restablecer");
            if (t != null) botonRestablecer = t.GetComponent<Button>();
        }
        if (botonRestablecer != null) botonRestablecer.onClick.AddListener(Restablecer);
    }

    private void OnEnable()
    {
        Controles.OnCambiaron += ActualizarTextos;
        ActualizarTextos();
    }

    private void OnDisable()
    {
        Controles.OnCambiaron -= ActualizarTextos;
        Cancelar();
    }

    private void BuscarFilas()
    {
        filas = new List<Fila>();

        foreach (Transform hijo in transform)
        {
            if (!porNombre.TryGetValue(hijo.name.Trim(), out var accion)) continue;

            Button boton = null;
            foreach (Transform nieto in hijo)
            {
                boton = nieto.GetComponent<Button>();
                if (boton != null) break;
            }
            if (boton == null) boton = hijo.GetComponent<Button>();
            if (boton == null) continue;

            filas.Add(new Fila { accion = accion, boton = boton });
        }

        if (filas.Count == 0)
            Debug.LogWarning("[Controles] No encontré filas (Acelerar, Frenar, Timbre, Pausa, Continuar) dentro de " + name);
    }

    /// <summary>
    /// Nombres como "ESPACIO" o "SHIFT IZQ." no caben en cuadros chicos: sin esto se parten letra por letra.
    /// Se dejan en una sola línea y se achican solos para caber (nunca más grandes que la fuente original).
    /// </summary>
    private void AjustarTexto(TextMeshProUGUI t)
    {
        if (t == null) return;

        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        t.fontSizeMax = t.fontSize;
        t.fontSizeMin = Mathf.Min(tamanoMinimo, t.fontSize);
        t.enableAutoSizing = true;
    }

    private static Transform BuscarHijo(Transform padre, string nombre)
    {
        foreach (Transform t in padre.GetComponentsInChildren<Transform>(true))
            if (t.name.Trim().Equals(nombre, StringComparison.OrdinalIgnoreCase)) return t;
        return null;
    }

    private void ActualizarTextos()
    {
        foreach (var f in filas)
        {
            if (f == null || f.texto == null || f == filaEsperando) continue;
            f.texto.text = Controles.Tecla(f.accion);
        }
    }

    private void EmpezarCambio(Fila fila)
    {
        Cancelar();

        var accion = Controles.Accion(fila.accion);
        int indice = Controles.IndiceBinding(accion);
        if (indice < 0)
        {
            Debug.LogWarning($"[Controles] La acción '{fila.accion}' no existe o no tiene tecla");
            return;
        }

        string rutaAnterior = accion.bindings[indice].effectivePath;
        bool estabaActiva = accion.enabled;
        accion.Disable();

        filaEsperando = fila;
        Esperando = true;
        if (fila.texto != null)
        {
            colorOriginal = fila.texto.color;
            fila.texto.text = textoEsperando;
            fila.texto.color = colorEsperando;
        }

        operacion = accion.PerformInteractiveRebinding(indice)
            .WithControlsHavingToMatchPath("<Keyboard>")
            .WithControlsExcluding("<Keyboard>/anyKey")
            .WithCancelingThrough("<Mouse>/leftButton")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(op =>
            {
                IntercambiarSiSeRepite(accion, indice, rutaAnterior);
                Terminar(accion, estabaActiva);
                Controles.Guardar();
            })
            .OnCancel(op => Terminar(accion, estabaActiva))
            .Start();
    }

    /// <summary>Si otra acción ya usaba la tecla nueva, le da la tecla que tenía esta.</summary>
    private static void IntercambiarSiSeRepite(InputAction accion, int indice, string rutaAnterior)
    {
        string nueva = accion.bindings[indice].effectivePath;

        foreach (var otra in accion.actionMap.actions)
        {
            if (otra == accion) continue;

            int i = Controles.IndiceBinding(otra);
            if (i < 0) continue;

            if (string.Equals(otra.bindings[i].effectivePath, nueva, StringComparison.OrdinalIgnoreCase))
                otra.ApplyBindingOverride(i, rutaAnterior);
        }
    }

    private void Terminar(InputAction accion, bool estabaActiva)
    {
        operacion?.Dispose();
        operacion = null;

        if (estabaActiva) accion.Enable();

        if (filaEsperando != null && filaEsperando.texto != null)
            filaEsperando.texto.color = colorOriginal;

        filaEsperando = null;
        Esperando = false;
        ActualizarTextos();
    }

    private void Cancelar()
    {
        if (operacion != null) operacion.Cancel();
    }

    public void Restablecer()
    {
        Cancelar();
        Controles.Restablecer();
    }
}
