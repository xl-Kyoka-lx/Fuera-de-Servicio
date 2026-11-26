using UnityEngine;

public static class EfectosManager
{
    public static float multiplicadorFrenadoConFrenos = 1.5f;
    public static float probabilidadPoleaConMalla = 0.3f;
    public static float reparacionAceite = 25f;
    public static float limpiezaKit = 25f;

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

    public static float MultiplicadorMargenFrenado => TieneMejora("frenos") ? multiplicadorFrenadoConFrenos : 1f;
    public static float ProbabilidadBasuraPoleas => TieneMejora("malla") ? probabilidadPoleaConMalla : 1f;

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
                e.SumarCantidad(id, 1);
                Debug.LogWarning($"[Tienda] '{id}' comprado, pero aún no tiene efecto en el juego");
                break;
        }
    }
}
