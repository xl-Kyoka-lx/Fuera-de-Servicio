using UnityEngine;

public static class EfectosManager
{
    public static float multiplicadorFrenadoConFrenos = 1.5f;
    public static float probabilidadPoleaConMalla = 0.3f;
    public static float topeAceiteInicial = 90f;
    public static float bajaTopeAceitePorMes = 10f;
    public static int semanasPorMes = 4;
    public static float limpiezaKit = 5f;
    public static float reparacionParche = 5f;
    public static float limiteParcheInicial = 60f;
    public static float bajaLimiteParchePorSemana = 5f;
    public static float limiteParcheMinimo = 30f;

    public static float LimiteParche
    {
        get
        {
            var gm = GameManager.Instance;
            int semana = gm != null && gm.GameData != null ? gm.GameData.Semana : 1;
            return Mathf.Max(limiteParcheMinimo, limiteParcheInicial - bajaLimiteParchePorSemana * (semana - 1));
        }
    }

    public static float TopeAceite
    {
        get
        {
            var gm = GameManager.Instance;
            int semana = gm != null && gm.GameData != null ? gm.GameData.Semana : 1;
            int mesesCumplidos = (semana - 1) / Mathf.Max(1, semanasPorMes);
            return Mathf.Max(0f, topeAceiteInicial - bajaTopeAceitePorMes * mesesCumplidos);
        }
    }

    public static bool AceiteAgotado => TopeAceite <= 0f;

    public static bool PuedeComprar(string idItem)
    {
        var gm = GameManager.Instance;
        if (idItem.ToLower() == "mantenimiento_parcial")
            return gm != null && gm.GameData.contadorGrasa < LimiteParche;
        if (idItem.ToLower() == "aceite")
            return gm != null && gm.GameData.contadorGrasa < TopeAceite;
        return true;
    }

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

    public static bool UsarKitLimpieza()
    {
        var gm = GameManager.Instance;
        if (gm == null || !Consumir("kit_limpieza")) return false;
        gm.ModificarLimpieza(limpiezaKit);
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
                gm.ModificarPolea(Mathf.Max(0f, TopeAceite - gm.GameData.contadorGrasa));
                Debug.Log($"[Tienda] Aceite: mantenimiento -> {gm.GameData.contadorGrasa:F0}% (tope {TopeAceite:F0}%)");
                break;

            case "kit_limpieza":
                e.SumarCantidad(id, 1);
                Debug.Log($"[Tienda] Kit de limpieza comprado. Tienes {e.ObtenerCantidad(id)}");
                break;

            case "mantenimiento_parcial":
                float subida = Mathf.Min(reparacionParche, LimiteParche - gm.GameData.contadorGrasa);
                if (subida > 0f) gm.ModificarPolea(subida);
                Debug.Log($"[Tienda] Parche: mantenimiento +{subida:F0} -> {gm.GameData.contadorGrasa:F0}% (límite {LimiteParche:F0}%)");
                break;

            default:
                e.SumarCantidad(id, 1);
                Debug.LogWarning($"[Tienda] '{id}' comprado, pero aún no tiene efecto en el juego");
                break;
        }
    }
}
