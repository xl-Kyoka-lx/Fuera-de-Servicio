using UnityEngine;

// Efectos de los ítems de la tienda.
//
// Las compras se guardan en GameData.estadoTienda (no en variables static), así que:
//  - duran entre escenas y días, igual que el dinero,
//  - se borran al empezar una partida nueva (GameData.ResetData).
//
// Mejoras únicas (efecto permanente, lo leen los sistemas del día):
//   frenos  -> EventoFrenado: más tiempo para frenar (ventana ×1.5)
//   malla   -> DirectorEventos: menos eventos de polea trabada por basura
//
// Consumibles (se aplican al comprarlos, en la noche):
//   aceite       -> repara el mantenimiento (poleas)
//   kit_limpieza -> sube la limpieza (menos eventos de basura al día siguiente)
//
// Pendientes (su mecánica aún no existe en el juego): cristal, repuesto.
public static class EfectosManager
{
    // ---------- Ajustes ----------
    public static float multiplicadorFrenadoConFrenos = 1.5f;
    public static float probabilidadPoleaConMalla = 0.3f;     // -70%
    public static float reparacionAceite = 25f;
    public static float limpiezaKit = 25f;

    // ---------- Estado ----------
    private static EstadoTienda Estado
    {
        get
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.GameData == null) return null;
            if (gm.GameData.estadoTienda == null) gm.GameData.estadoTienda = new EstadoTienda();
            return gm.GameData.estadoTienda;
        }
    }

    public static bool TieneMejora(string id)
    {
        var e = Estado;
        return e != null && e.mejorasCompradas.Contains(id);
    }

    public static int Stock(string id)
    {
        var e = Estado;
        return e != null ? e.ObtenerCantidad(id) : 0;
    }

    public static bool Consumir(string id)
    {
        var e = Estado;
        if (e == null || e.ObtenerCantidad(id) <= 0) return false;
        e.SumarCantidad(id, -1);
        return true;
    }

    // ---------- Valores que leen los sistemas del juego ----------
    public static float MultiplicadorMargenFrenado => TieneMejora("frenos") ? multiplicadorFrenadoConFrenos : 1f;
    public static float ProbabilidadBasuraPoleas => TieneMejora("malla") ? probabilidadPoleaConMalla : 1f;

    // ---------- Compra ----------
    public static void AplicarEfecto(string idItem, bool esMejoraUnica)
    {
        var e = Estado;
        var gm = GameManager.Instance;
        if (e == null || gm == null) return;

        string id = idItem.ToLower();

        if (esMejoraUnica)
        {
            if (!e.mejorasCompradas.Contains(id)) e.mejorasCompradas.Add(id);
            Debug.Log($"[Tienda] Mejora instalada: {id}");
            return;
        }

        switch (id)
        {
            case "aceite":
                gm.ModificarPolea(reparacionAceite);
                Debug.Log($"[Tienda] Aceite: mantenimiento +{reparacionAceite} -> {gm.GameData.contadorGrasa:F0}%");
                break;

            case "kit_limpieza":
                gm.ModificarLimpieza(limpiezaKit);
                Debug.Log($"[Tienda] Kit de limpieza: limpieza +{limpiezaKit} -> {gm.GameData.contadorLimpieza:F0}%");
                break;

            default:
                // cristal, repuesto: se guardan, pero todavía no hay mecánica que los use
                e.SumarCantidad(id, 1);
                Debug.LogWarning($"[Tienda] '{id}' comprado, pero aún no tiene efecto en el juego");
                break;
        }
    }
}
