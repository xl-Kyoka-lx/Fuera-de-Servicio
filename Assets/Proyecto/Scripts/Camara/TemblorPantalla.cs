using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(10000)]
public class TemblorPantalla : MonoBehaviour
{
    private static TemblorPantalla actual;

    private float duracion;
    private float intensidad;
    private float tiempo;

    private readonly List<Camera> camaras = new List<Camera>();
    private readonly List<Vector3> offsetCamaras = new List<Vector3>();
    private readonly List<RectTransform> paneles = new List<RectTransform>();
    private readonly List<Vector2> posicionPaneles = new List<Vector2>();

    public static void Temblar(float duracion = 0.4f, float intensidad = 1f)
    {
        if (actual != null)
        {
            actual.duracion = Mathf.Max(actual.duracion - actual.tiempo, duracion);
            actual.intensidad = Mathf.Max(actual.intensidad, intensidad);
            actual.tiempo = 0f;
            return;
        }

        var go = new GameObject("TemblorPantalla");
        actual = go.AddComponent<TemblorPantalla>();
        actual.duracion = duracion;
        actual.intensidad = intensidad;
        actual.Preparar();
    }

    private void Preparar()
    {
        foreach (var cam in Camera.allCameras)
        {
            camaras.Add(cam);
            offsetCamaras.Add(Vector3.zero);
        }

        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
        {
            if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;

            foreach (Transform hijo in canvas.transform)
            {
                if (hijo is RectTransform rt)
                {
                    paneles.Add(rt);
                    posicionPaneles.Add(rt.anchoredPosition);
                }
            }
        }
    }

    private void Update()
    {
        for (int i = 0; i < camaras.Count; i++)
        {
            if (camaras[i] != null) camaras[i].transform.position -= offsetCamaras[i];
            offsetCamaras[i] = Vector3.zero;
        }
    }

    private void LateUpdate()
    {
        tiempo += Time.unscaledDeltaTime;
        float fuerza = intensidad * (1f - Mathf.Clamp01(tiempo / duracion));

        if (fuerza <= 0f)
        {
            Terminar();
            return;
        }

        Vector2 sacudida = Random.insideUnitCircle * fuerza;

        for (int i = 0; i < camaras.Count; i++)
        {
            var cam = camaras[i];
            if (cam == null) continue;

            float tam = cam.orthographic ? cam.orthographicSize : 5f;
            Vector3 offset = (Vector3)(sacudida * tam * 0.03f);
            cam.transform.position += offset;
            offsetCamaras[i] = offset;
        }

        float pixeles = 14f * Screen.height / 1080f;
        for (int i = 0; i < paneles.Count; i++)
        {
            if (paneles[i] != null)
                paneles[i].anchoredPosition = posicionPaneles[i] + sacudida * pixeles;
        }
    }

    private void Terminar()
    {
        for (int i = 0; i < paneles.Count; i++)
            if (paneles[i] != null) paneles[i].anchoredPosition = posicionPaneles[i];

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        for (int i = 0; i < camaras.Count; i++)
            if (camaras[i] != null) camaras[i].transform.position -= offsetCamaras[i];

        for (int i = 0; i < paneles.Count; i++)
            if (paneles[i] != null) paneles[i].anchoredPosition = posicionPaneles[i];

        if (actual == this) actual = null;
    }
}
