using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

// Ventana para reemplazar el arte placeholder sin romper referencias.
// Menú: Herramientas > Inventario de Arte
//
// Clave: Unity enlaza escenas y prefabs al arte por el GUID del .meta, no por el nombre.
// Esta herramienta sobrescribe el contenido de la imagen manteniendo su .meta, así que
// todo lo que usaba el placeholder pasa a usar el arte nuevo automáticamente.
public class InventarioArte : EditorWindow
{
    private static readonly string[] Carpetas = { "Assets/Proyecto", "Assets/Scenes" };
    private const string CarpetaRespaldo = "Library/ArteRespaldo";

    private class Entrada
    {
        public string ruta;
        public Texture2D textura;
        public int ancho, alto, cortes;
        public float ppu;
        public List<string> usos = new List<string>();
    }

    private List<Entrada> entradas = new List<Entrada>();
    private Vector2 scroll;
    private bool mantenerTamano = true;
    private bool soloSinUso;
    private string filtro = "";

    [MenuItem("Herramientas/Inventario de Arte")]
    public static void Abrir()
    {
        var w = GetWindow<InventarioArte>("Inventario de Arte");
        w.minSize = new Vector2(620, 300);
        w.Escanear();
    }

    private void OnFocus()
    {
        if (entradas.Count == 0) Escanear();
    }

    private void Escanear()
    {
        entradas.Clear();

        // Quién usa cada imagen (escenas y prefabs, referencias directas).
        var usosPorRuta = new Dictionary<string, List<string>>();
        var usuarios = AssetDatabase.FindAssets("t:Scene t:Prefab t:AnimationClip t:ScriptableObject", Carpetas)
            .Select(AssetDatabase.GUIDToAssetPath).Distinct();
        foreach (var usuario in usuarios)
        {
            foreach (var dep in AssetDatabase.GetDependencies(usuario, false))
            {
                if (!usosPorRuta.TryGetValue(dep, out var lista))
                    usosPorRuta[dep] = lista = new List<string>();
                lista.Add(Path.GetFileNameWithoutExtension(usuario));
            }
        }

        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", Carpetas))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            if (!(AssetImporter.GetAtPath(ruta) is TextureImporter imp)) continue;

            imp.GetSourceTextureWidthAndHeight(out int w, out int h);
            var e = new Entrada
            {
                ruta = ruta,
                textura = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta),
                ancho = w,
                alto = h,
                ppu = imp.spritePixelsPerUnit,
                cortes = AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<Sprite>().Count(),
            };
            if (usosPorRuta.TryGetValue(ruta, out var u)) e.usos = u.Distinct().ToList();
            entradas.Add(e);
        }

        entradas = entradas.OrderBy(e => e.ruta).ToList();
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Para cambiar un placeholder pulsa \"Reemplazar…\" y elige la imagen nueva. " +
            "Se conserva el .meta, así que escenas y prefabs se actualizan solos. " +
            "No borres ni renombres las imágenes desde el Explorador de Windows.",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            mantenerTamano = EditorGUILayout.ToggleLeft(
                new GUIContent("Mantener tamaño en pantalla",
                    "Ajusta Pixels Per Unit para que el objeto ocupe lo mismo aunque la imagen nueva tenga otra resolución"),
                mantenerTamano, GUILayout.Width(200));
            soloSinUso = EditorGUILayout.ToggleLeft("Solo sin uso", soloSinUso, GUILayout.Width(100));
            filtro = EditorGUILayout.TextField(filtro, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("Actualizar", GUILayout.Width(80))) Escanear();
        }

        int pendientes = entradas.Count;
        EditorGUILayout.LabelField($"{pendientes} imágenes en {string.Join(", ", Carpetas)}", EditorStyles.miniLabel);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (var e in entradas.ToList())
        {
            if (soloSinUso && e.usos.Count > 0) continue;
            if (!string.IsNullOrEmpty(filtro) && e.ruta.IndexOf(filtro, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
            DibujarFila(e);
        }
        EditorGUILayout.EndScrollView();
    }

    private void DibujarFila(Entrada e)
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            var rectImg = GUILayoutUtility.GetRect(56, 56, GUILayout.Width(56));
            if (e.textura != null) GUI.DrawTexture(rectImg, e.textura, ScaleMode.ScaleToFit);

            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField(Path.GetFileName(e.ruta), EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    $"{e.ancho}×{e.alto} px · PPU {e.ppu:0.##} · {e.cortes} sprite(s)", EditorStyles.miniLabel);
                string usos = e.usos.Count == 0 ? "Sin uso (se puede ignorar o borrar)" : "Usado en: " + string.Join(", ", e.usos);
                EditorGUILayout.LabelField(usos, EditorStyles.wordWrappedMiniLabel);
            }

            using (new EditorGUILayout.VerticalScope(GUILayout.Width(100)))
            {
                if (GUILayout.Button("Reemplazar…")) Reemplazar(e);
                if (GUILayout.Button("Ver")) EditorGUIUtility.PingObject(e.textura);
            }
        }
    }

    private void Reemplazar(Entrada e)
    {
        string nuevo = EditorUtility.OpenFilePanel($"Arte nuevo para {Path.GetFileName(e.ruta)}", "", "png,jpg,jpeg,psd,tga");
        if (string.IsNullOrEmpty(nuevo)) return;

        string ruta = e.ruta;
        var imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
        imp.GetSourceTextureWidthAndHeight(out int anchoViejo, out int altoViejo);

        // Respaldo del placeholder (fuera de Assets, git no lo sube).
        Directory.CreateDirectory(CarpetaRespaldo);
        string respaldo = Path.Combine(CarpetaRespaldo,
            $"{System.DateTime.Now:yyyyMMdd_HHmmss}_{Path.GetFileName(ruta)}");
        File.Copy(ruta, respaldo, true);

        // Si cambia la extensión (p. ej. .jpg -> .png) se renombra con AssetDatabase, que conserva el GUID.
        string extNueva = Path.GetExtension(nuevo).ToLowerInvariant();
        if (extNueva != Path.GetExtension(ruta).ToLowerInvariant())
        {
            string rutaNueva = Path.ChangeExtension(ruta, extNueva).Replace('\\', '/');
            string error = AssetDatabase.MoveAsset(ruta, rutaNueva);
            if (!string.IsNullOrEmpty(error))
            {
                EditorUtility.DisplayDialog("No se pudo reemplazar", error, "OK");
                return;
            }
            ruta = rutaNueva;
        }

        File.Copy(nuevo, ruta, true);
        AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);

        imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
        imp.GetSourceTextureWidthAndHeight(out int anchoNuevo, out int altoNuevo);
        float fx = (float)anchoNuevo / anchoViejo;
        float fy = (float)altoNuevo / altoViejo;

        // Que Unity no reduzca la imagen si es más grande que el límite por defecto (2048).
        int lado = Mathf.Max(anchoNuevo, altoNuevo);
        while (imp.maxTextureSize < lado && imp.maxTextureSize < 8192) imp.maxTextureSize *= 2;

        if (mantenerTamano && anchoViejo > 0) imp.spritePixelsPerUnit = e.ppu * fx;

        // Ajusta los cortes del sprite a la resolución nueva. Se editan los cortes existentes
        // (no se crean nuevos) para que conserven su ID y no se pierdan las referencias.
        if (imp.spriteImportMode == SpriteImportMode.Multiple)
        {
            var fabricas = new SpriteDataProviderFactories();
            fabricas.Init();
            var datos = fabricas.GetSpriteEditorDataProviderFromObject(imp);
            datos.InitSpriteEditorDataProvider();
            var cortes = datos.GetSpriteRects();

            if (cortes.Length == 1)
            {
                cortes[0].rect = new Rect(0, 0, anchoNuevo, altoNuevo);
            }
            else
            {
                foreach (var c in cortes)
                {
                    var r = c.rect;
                    c.rect = new Rect(Mathf.Round(r.x * fx), Mathf.Round(r.y * fy),
                        Mathf.Round(r.width * fx), Mathf.Round(r.height * fy));
                }
            }

            datos.SetSpriteRects(cortes);
            datos.Apply();
        }

        imp.SaveAndReimport();
        Escanear();

        string aviso = e.cortes > 1
            ? $"\n\nEsta imagen tiene {e.cortes} sprites. Se escalaron los cortes; revisa en el Sprite Editor que calcen con el dibujo nuevo (mueve los cortes, no los borres)."
            : "";
        EditorUtility.DisplayDialog("Arte reemplazado",
            $"{Path.GetFileName(ruta)}: {anchoViejo}×{altoViejo} → {anchoNuevo}×{altoNuevo} px.\n" +
            $"Respaldo del placeholder en {respaldo}.{aviso}", "OK");
    }
}
