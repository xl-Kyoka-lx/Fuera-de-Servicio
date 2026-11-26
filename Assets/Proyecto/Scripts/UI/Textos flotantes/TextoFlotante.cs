using UnityEngine;
using TMPro;

public class TextoFlotante : MonoBehaviour
{
    private TextMeshPro tmp;
    private Color colorInicial;
    private float duracion;
    private float distanciaSubida;
    private float tiempo;
    private Vector3 inicio;

    public static void Mostrar(string texto, Vector3 posicion, Color color, float alturaReferencia, float duracion = 1.2f)
    {
        if (TMP_Settings.defaultFontAsset == null)
        {
            Debug.LogWarning("TextoFlotante: no hay fuente por defecto de TextMeshPro (TMP Settings)");
            return;
        }

        var go = new GameObject("TextoFlotante");
        go.transform.position = posicion;

        var t = go.AddComponent<TextMeshPro>();
        t.text = texto;
        t.font = TMP_Settings.defaultFontAsset;
        t.fontSize = Mathf.Max(0.5f, alturaReferencia * 2.5f);
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.color = color;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.sortingOrder = 100;

        var f = go.AddComponent<TextoFlotante>();
        f.tmp = t;
        f.colorInicial = color;
        f.duracion = duracion;
        f.distanciaSubida = alturaReferencia * 0.5f;
        f.inicio = posicion;
    }

    private void Update()
    {
        tiempo += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(tiempo / duracion);

        transform.position = inicio + Vector3.up * (distanciaSubida * t);

        Color c = colorInicial;
        c.a = 1f - t * t;
        tmp.color = c;

        if (t >= 1f) Destroy(gameObject);
    }
}
