using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Guarda las teclas que el jugador eligió en la pantalla de controles y las aplica
/// a cualquier copia de AscensorControls (el asset que usan los scripts o el que se crea por código).
/// También traduce {teclaAcelerar}, {teclaFrenar}, {teclaTimbre}, {teclaPausa} y {teclaContinuar}
/// en los textos al nombre de la tecla actual.
/// </summary>
public static class Controles
{
    private const string Clave = "ControlesTeclas";
    private const string Mapa = "Gameplay";

    public const string Acelerar = "MoverAscensor";
    public const string Frenar = "Frenar";
    public const string Timbre = "AvisarCabina";
    public const string Pausa = "Pausa";
    public const string Continuar = "Avanzar texto";

    // Token del texto -> acción
    private static readonly Dictionary<string, string> tokens = new Dictionary<string, string>
    {
        { "{teclaAcelerar}", Acelerar },
        { "{teclaFrenar}", Frenar },
        { "{teclaTimbre}", Timbre },
        { "{teclaPausa}", Pausa },
        { "{teclaContinuar}", Continuar },
    };

    public static IEnumerable<string> Tokens => tokens.Keys;

    public static event Action OnCambiaron;

    private static AscensorControls propios;

    /// <summary>Copia propia de los controles, con las teclas del jugador ya aplicadas.</summary>
    public static InputActionAsset Asset
    {
        get
        {
            // El proyecto no recarga el dominio al entrar en Play: el asset de una prueba anterior puede estar destruido
            if (propios == null || propios.asset == null)
            {
                propios = new AscensorControls();
                Aplicar(propios.asset);
            }
            return propios.asset;
        }
    }

    public static InputAction Accion(string nombre) => Asset.FindActionMap(Mapa)?.FindAction(nombre);

    /// <summary>Pone las teclas guardadas en este asset. Llamar antes de usar sus acciones.</summary>
    public static void Aplicar(InputActionAsset asset)
    {
        if (asset == null) return;

        asset.RemoveAllBindingOverrides();

        string json = PlayerPrefs.GetString(Clave, "");
        if (string.IsNullOrEmpty(json)) return;

        try
        {
            asset.LoadBindingOverridesFromJson(json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Controles] No se pudieron cargar las teclas guardadas, se usan las de siempre: {e.Message}");
        }
    }

    public static void Guardar()
    {
        PlayerPrefs.SetString(Clave, Asset.SaveBindingOverridesAsJson());
        PlayerPrefs.Save();
        OnCambiaron?.Invoke();
    }

    public static void Restablecer()
    {
        Asset.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey(Clave);
        PlayerPrefs.Save();
        OnCambiaron?.Invoke();
    }

    /// <summary>Índice del primer binding de teclado de la acción (no compuesto).</summary>
    public static int IndiceBinding(InputAction accion)
    {
        if (accion == null) return -1;
        for (int i = 0; i < accion.bindings.Count; i++)
        {
            var b = accion.bindings[i];
            if (b.isComposite || b.isPartOfComposite) continue;
            return i;
        }
        return -1;
    }

    /// <summary>
    /// Nombre de la tecla de una acción, en mayúsculas (ej: "ESPACIO").
    /// Primero busca el código de la tecla (space, enter, leftShift...) en 'nombres'.
    /// Si no está ahí (letras, números, Ñ...), usa el nombre que da el teclado del computador.
    /// </summary>
    public static string Tecla(string nombreAccion)
    {
        var accion = Accion(nombreAccion);
        int i = IndiceBinding(accion);
        if (i < 0) return "?";

        string ruta = accion.bindings[i].effectivePath ?? "";
        string codigo = ruta.Substring(ruta.LastIndexOf('/') + 1);

        if (nombres.TryGetValue(codigo, out var nombre)) return nombre;

        string delTeclado = accion.GetBindingDisplayString(i, InputBinding.DisplayStringOptions.DontIncludeInteractions);
        return string.IsNullOrEmpty(delTeclado) ? codigo.ToUpperInvariant() : delTeclado.ToUpperInvariant();
    }

    public static string ReemplazarTeclas(string texto)
    {
        if (string.IsNullOrEmpty(texto) || texto.IndexOf("{tecla", StringComparison.Ordinal) < 0) return texto;

        foreach (var par in tokens)
            if (texto.Contains(par.Key))
                texto = texto.Replace(par.Key, Tecla(par.Value));

        return texto;
    }

    // NOMBRES DE LAS TECLAS EN PANTALLA
    // Izquierda: código de la tecla en Unity (no cambiar). Derecha: lo que se muestra en el juego (cámbialo a gusto).
    // Para ver el código de una tecla: Window > Analysis > Input Debugger, o la lista de Keyboard en la doc del Input System.
    private static readonly Dictionary<string, string> nombres = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "space", "ESPACIO" },
        { "enter", "ENTER" },
        { "numpadEnter", "ENTER (NUM)" },
        { "escape", "ESC" },
        { "tab", "TAB" },
        { "backspace", "BORRAR" },
        { "delete", "SUPR" },
        { "insert", "INSERT" },
        { "home", "INICIO" },
        { "end", "FIN" },
        { "pageUp", "RE PÁG" },
        { "pageDown", "AV PÁG" },
        { "capsLock", "BLOQ MAYÚS" },
        { "leftShift", "SHIFT IZQ." },
        { "rightShift", "SHIFT DER." },
        { "leftCtrl", "CTRL IZQ." },
        { "rightCtrl", "CTRL DER." },
        { "leftAlt", "ALT" },
        { "rightAlt", "ALT GR" },
        { "upArrow", "FLECHA ARRIBA" },
        { "downArrow", "FLECHA ABAJO" },
        { "leftArrow", "FLECHA IZQ." },
        { "rightArrow", "FLECHA DER." },
    };
}
