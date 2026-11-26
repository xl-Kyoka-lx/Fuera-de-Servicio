using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ConfigEconomia
{
    [Header("Subsidio municipal")]
    [Tooltip("Lo que da la municipalidad cada noche. Elemento 0 = día 1. Después del último ya no hay subsidio")]
    public int[] subsidioPorDia = { 1000, 1000, 1000, 750, 500, 500, 500, 500, 500, 250, 250, 0 };

    [Header("Cobros (fijos durante cada semana)")]
    [Tooltip("Semana en que empiezan los cobros")]
    public int semanaInicioCobros = 2;

    [Tooltip("Luz: cobro diario base")]
    public int electricidadBase = 150;
    [Tooltip("Luz: cuánto sube por cada semana que pasa")]
    public int electricidadPorSemana = 75;
    [Tooltip("Luz: cuánto se suma por cada punto de mantenimiento que falta (motor gastado consume más)")]
    public float electricidadPorDesgaste = 3f;

    [Tooltip("Deudas del padre: cobro diario de la primera semana de cobro")]
    public int deudaBase = 120;
    [Tooltip("Deudas del padre: interés por mora que se acumula cada semana (0.2 = 20%)")]
    public float interesSemanal = 0.2f;

    [Tooltip("Permiso provisorio de operación: cobro diario base")]
    public int permisoBase = 80;
    [Tooltip("Permiso provisorio: cuánto sube cada semana")]
    public int permisoPorSemana = 60;
    [Tooltip("Multa por cada punto de limpieza que falta")]
    public float multaPorSuciedad = 2f;
    [Tooltip("Multa por cada punto de popularidad bajo el mínimo (quejas de pasajeros)")]
    public float multaPorQueja = 3f;
    [Tooltip("Bajo esta popularidad empiezan las quejas al inspector")]
    public float popularidadSinQuejas = 50f;
}

public struct CobroDelDia
{
    public int subsidio;
    public int electricidad;
    public int deudas;
    public int multas;

    public int Cobros => electricidad + deudas + multas;
    public int Neto => Cobros - subsidio;
}

public static class Economia
{
    private static readonly ConfigEconomia porDefecto = new ConfigEconomia();

    public static ConfigEconomia Config =>
        GameManager.Instance != null && GameManager.Instance.economia != null ? GameManager.Instance.economia : porDefecto;

    public static int SubsidioDelDia(int dia)
    {
        var c = Config;
        int i = dia - 1;
        return c.subsidioPorDia != null && i >= 0 && i < c.subsidioPorDia.Length ? c.subsidioPorDia[i] : 0;
    }

    public static bool HayCobros(GameData data) => data.Semana >= Config.semanaInicioCobros;

    public static CobroDelDia Calcular(GameData data)
    {
        var cobro = new CobroDelDia { subsidio = SubsidioDelDia(data.diaActual) };

        if (HayCobros(data))
        {
            CalcularCobrosDeLaSemana(data);
            cobro.electricidad = data.cobroElectricidad;
            cobro.deudas = data.cobroDeudas;
            cobro.multas = data.cobroMultas;
        }

        return cobro;
    }

    private static void CalcularCobrosDeLaSemana(GameData data)
    {
        if (data.semanaCobrosCalculada == data.Semana) return;

        var c = Config;
        int semanasDeCobro = data.Semana - c.semanaInicioCobros;

        float desgaste = 100f - data.contadorGrasa;
        float suciedad = 100f - data.contadorLimpieza;
        float quejas = Mathf.Max(0f, c.popularidadSinQuejas - data.contadorPopularidad);

        data.cobroElectricidad = Mathf.RoundToInt(c.electricidadBase + c.electricidadPorSemana * semanasDeCobro + desgaste * c.electricidadPorDesgaste);
        data.cobroDeudas = Mathf.RoundToInt(c.deudaBase * Mathf.Pow(1f + c.interesSemanal, semanasDeCobro));
        data.cobroMultas = Mathf.RoundToInt(c.permisoBase + c.permisoPorSemana * semanasDeCobro + suciedad * c.multaPorSuciedad + quejas * c.multaPorQueja);
        data.semanaCobrosCalculada = data.Semana;

        Debug.Log($"[Economía] Semana {data.Semana}: luz ${data.cobroElectricidad}, deudas ${data.cobroDeudas}, multas ${data.cobroMultas} por día");
    }

    public static List<LineaDialogo> LineasDeLaNoche(GameData data)
    {
        var lineas = new List<LineaDialogo>();
        int dia = data.diaActual;
        int subsidioHoy = SubsidioDelDia(dia);
        int subsidioAyer = dia > 1 ? SubsidioDelDia(dia - 1) : 0;

        if (dia == 1)
        {
            Agregar(lineas, "Tú", "Cierto... desde la mañana hay una carta.");
            Agregar(lineas, "Carta: Municipalidad", $"Subsidio municipal: por el valor patrimonial del ascensor, se le otorgará un aporte de ${subsidioHoy} diarios para mantener el servicio.");
            Agregar(lineas, "Tú", "Menos mal. Algo es algo.");
        }
        else if (subsidioHoy < subsidioAyer)
        {
            if (subsidioHoy > 0)
                Agregar(lineas, "Carta: Municipalidad", $"Por reajuste presupuestario, el subsidio se reduce a ${subsidioHoy} diarios.");
            else
                Agregar(lineas, "Carta: Municipalidad", "El subsidio ha terminado. Los fondos serán destinados a otros proyectos.");
        }

        bool primeraSemanaDeCobro = data.Semana == Config.semanaInicioCobros;
        if (HayCobros(data) && data.DiaDeLaSemana == 1)
        {
            CobroDelDia c = Calcular(data);

            Agregar(lineas, "Carta: Compañía eléctrica", $"Recargo por sobredemanda / tarifa punta. Su motor consume cada vez más. Cobro diario esta semana: ${c.electricidad}.");

            if (primeraSemanaDeCobro)
                Agregar(lineas, "Carta: Oficina de cobranza", $"Su padre mantenía cuentas impagas. Vencido el plazo de repactación, se cobrarán ${c.deudas} diarios más intereses por mora.");
            else
                Agregar(lineas, "Carta: Oficina de cobranza", $"Los intereses por mora siguen sumando. Cobro diario esta semana: ${c.deudas}.");

            Agregar(lineas, "Carta: Inspección municipal", $"Por el estado del ascensor y las quejas recibidas, debe pagar multas y un permiso provisorio de operación: ${c.multas} diarios.");

            if (primeraSemanaDeCobro)
                Agregar(lineas, "Tú", "Papá... ¿cómo aguantaste esto tanto tiempo?");
        }

        lineas.AddRange(LineasDeCuentas(data));
        return lineas;
    }

    public static List<LineaDialogo> LineasDeCuentas(GameData data)
    {
        var lineas = new List<LineaDialogo>();
        CobroDelDia c = Calcular(data);
        const string hablante = "Cuentas de hoy";

        string detalle = "";
        if (c.subsidio > 0) detalle += $"Subsidio municipal: +${c.subsidio}\n";
        if (c.electricidad > 0) detalle += $"Luz: -${c.electricidad}\n";
        if (c.deudas > 0) detalle += $"Deudas de papá: -${c.deudas}\n";
        if (c.multas > 0) detalle += $"Multas y permisos: -${c.multas}\n";

        if (detalle.Length > 0) Agregar(lineas, hablante, detalle.TrimEnd('\n'));

        Agregar(lineas, hablante, c.Neto <= 0
            ? $"Hoy recibes ${-c.Neto}."
            : $"Hoy debes pagar ${c.Neto}. Tienes ${data.dineroTotal}.");

        return lineas;
    }

    private static void Agregar(List<LineaDialogo> lineas, string hablante, string texto)
    {
        lineas.Add(new LineaDialogo { hablante = hablante, texto = texto, imagen = "" });
    }
}
