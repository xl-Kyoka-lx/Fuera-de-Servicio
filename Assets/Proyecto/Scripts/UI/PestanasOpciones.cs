using UnityEngine;
using UnityEngine.UI;

public class PestanasOpciones : MonoBehaviour
{
    [System.Serializable]
    public class Pestana
    {
        public string nombre;
        [Tooltip("Botón del menú de opciones que abre este panel")]
        public Button boton;
        public GameObject panel;
        [Tooltip("Botón dentro del panel que vuelve al menú de opciones (solo en modo submenú)")]
        public Button botonVolver;
    }

    public Pestana[] pestanas;

    [Header("Modo")]
    [Tooltip("Marcado: al elegir una opción se oculta el menú de opciones y se ve solo ese panel, con su botón Volver. Desmarcado: pestañas, el menú queda visible y solo cambia el panel")]
    public bool modoSubmenu = true;
    [Tooltip("Solo en modo submenú: oculta también la imagen de fondo de este objeto mientras hay un panel abierto")]
    public bool ocultarFondoAlAbrir = false;

    [Header("Solo en modo pestañas")]
    [Tooltip("Pestaña que se abre al mostrar las opciones (0 = la primera)")]
    public int pestanaInicial = 0;
    public Color colorSeleccionada = Color.white;
    public Color colorNormal = new Color(0.6f, 0.6f, 0.6f, 1f);

    private Image fondo;

    private void Awake()
    {
        fondo = GetComponent<Image>();

        for (int i = 0; i < pestanas.Length; i++)
        {
            int indice = i;
            if (pestanas[i].boton != null)
                pestanas[i].boton.onClick.AddListener(() => Mostrar(indice));
            if (pestanas[i].botonVolver != null)
                pestanas[i].botonVolver.onClick.AddListener(VolverAlMenu);
        }
    }

    private void OnEnable()
    {
        if (modoSubmenu) VolverAlMenu();
        else Mostrar(pestanaInicial);
    }

    public void Mostrar(int indice)
    {
        if (modoSubmenu)
        {
            PonerMenuVisible(false);
            for (int i = 0; i < pestanas.Length; i++)
                if (pestanas[i].panel != null) pestanas[i].panel.SetActive(i == indice);
            return;
        }

        for (int i = 0; i < pestanas.Length; i++)
        {
            bool activa = i == indice;

            if (pestanas[i].panel != null)
                pestanas[i].panel.SetActive(activa);

            if (pestanas[i].boton != null && pestanas[i].boton.targetGraphic != null)
                pestanas[i].boton.targetGraphic.color = activa ? colorSeleccionada : colorNormal;
        }
    }

    public void VolverAlMenu()
    {
        foreach (var p in pestanas)
            if (p.panel != null) p.panel.SetActive(false);

        PonerMenuVisible(true);
    }

    private void PonerMenuVisible(bool visible)
    {
        foreach (Transform hijo in transform)
        {
            if (EsPanel(hijo.gameObject)) continue;
            hijo.gameObject.SetActive(visible);
        }

        if (ocultarFondoAlAbrir && fondo != null)
            fondo.enabled = visible;
    }

    private bool EsPanel(GameObject go)
    {
        foreach (var p in pestanas)
            if (p.panel == go) return true;
        return false;
    }
}
