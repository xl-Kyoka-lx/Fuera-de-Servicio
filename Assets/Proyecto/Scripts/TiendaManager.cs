using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TiendaManager : MonoBehaviour
{
    [System.Serializable]
    public class ItemData
    {
        public string id;
        public string nombre;
        public int precio;
        [TextArea(2, 5)]
        public string descripcion;
        public bool esMejoraUnica;
    }

    [System.Serializable]
    private class CatalogoWrapper
    {
        public List<ItemData> items;
    }

    public TextAsset archivoJson;

    public GameObject cuadroTexto;
    public TextMeshProUGUI textoDescripcion;
    public TextMeshProUGUI textoBotonComprar;
    public Button botonComprar;
    public TextMeshProUGUI textoDineroDisponible;

    [Tooltip("Escena a la que vuelve al salir de la tienda")]
    public string nombreEscenaNoche = "EscenaNoche";

    private List<ItemData> listaItems = new List<ItemData>();
    private ItemData itemSeleccionadoActual;

    private void Awake()
    {
        CargarDatosDesdeJson();
    }

    private void Start()
    {
        if (cuadroTexto != null) cuadroTexto.SetActive(false);
        ActualizarTextoDinero();

        if (GameManager.Instance != null && GameManager.Instance.gameData != null)
        {
            GameManager.Instance.gameData.OnDataChanged += ActualizarTextoDinero;
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null && GameManager.Instance.gameData != null)
        {
            GameManager.Instance.gameData.OnDataChanged -= ActualizarTextoDinero;
        }
    }

    private void CargarDatosDesdeJson()
    {
        if (archivoJson != null)
        {
            CatalogoWrapper catalogo = JsonUtility.FromJson<CatalogoWrapper>(archivoJson.text);
            listaItems = catalogo.items;
        }
    }

    public void SeleccionarItem(string itemId)
    {
        ItemData item = listaItems.Find(i => i.id.ToLower() == itemId.ToLower());

        if (item != null)
        {
            itemSeleccionadoActual = item;
            if (cuadroTexto != null) cuadroTexto.SetActive(true);

            ActualizarDescripcion();

            if (item.esMejoraUnica && EfectosManager.TieneMejora(item.id.ToLower()))
            {
                if (textoBotonComprar != null) textoBotonComprar.text = "Ya adquirido";
                if (botonComprar != null) botonComprar.interactable = false;
            }
            else
            {
                if (textoBotonComprar != null)
                    textoBotonComprar.text = $"Comprar (${item.precio})";

                if (botonComprar != null)
                    botonComprar.interactable = PuedePagar(item);
            }
        }
    }

    public void ComprarItemActual()
    {
        if (itemSeleccionadoActual == null) return;
        if (GameManager.Instance == null) return;

        if (PuedePagar(itemSeleccionadoActual))
        {
            GameManager.Instance.gameData.dineroTotal -= itemSeleccionadoActual.precio;
            GameManager.Instance.gameData.RegistrarGasto(itemSeleccionadoActual.precio);
            GameManager.Instance.gameData.OnDataChanged?.Invoke();
            ActualizarTextoDinero();

            EfectosManager.AplicarEfecto(itemSeleccionadoActual.id, itemSeleccionadoActual.esMejoraUnica);
            ActualizarDescripcion();

            if (itemSeleccionadoActual.esMejoraUnica)
            {
                if (textoBotonComprar != null) textoBotonComprar.text = "Ya adquirido";
                if (botonComprar != null) botonComprar.interactable = false;
            }
            else
            {
                if (botonComprar != null)
                    botonComprar.interactable = PuedePagar(itemSeleccionadoActual);
            }
        }
    }

    private void ActualizarDescripcion()
    {
        if (textoDescripcion == null || itemSeleccionadoActual == null) return;

        string texto = itemSeleccionadoActual.descripcion;
        if (itemSeleccionadoActual.id.ToLower() == "kit_limpieza")
            texto += $"\n\nTienes: {EfectosManager.Stock("kit_limpieza")}";
        if (itemSeleccionadoActual.id.ToLower() == "aceite" && EfectosManager.AceiteAgotado)
            texto = "Ya no existen las piezas necesarias para la mantención.";
        if (itemSeleccionadoActual.id.ToLower() == "mantenimiento_parcial" && GameManager.Instance != null)
        {
            float actual = GameManager.Instance.GameData.contadorGrasa;
            texto += $"\n\nMantenimiento: {actual:F0}% / límite esta semana: {EfectosManager.LimiteParche:F0}%";
            if (!EfectosManager.PuedeComprar("mantenimiento_parcial"))
                texto += "\nYa no se puede parchar más.";
        }
        textoDescripcion.text = texto;
    }

    private bool PuedePagar(ItemData item)
    {
        return ObtenerDinero() >= item.precio && EfectosManager.PuedeComprar(item.id);
    }

    private int ObtenerDinero()
    {
        if (GameManager.Instance == null) return 0;
        if (GameManager.Instance.gameData == null) return 0;
        return GameManager.Instance.gameData.dineroTotal;
    }

    private void ActualizarTextoDinero()
    {
        if (textoDineroDisponible != null)
            textoDineroDisponible.text = $"${ObtenerDinero()}";
    }

    public void SalirDeLaTienda()
    {
        Debug.Log($"Saliendo de la tienda. Volviendo a: {nombreEscenaNoche}");
        SceneManager.LoadScene(nombreEscenaNoche);
    }
}