using UnityEngine;

public class AmbienteAves : MonoBehaviour
{
    [System.Serializable]
    public class Tramo
    {
        public string nombre;
        [Tooltip("Hora del juego en que empieza (11.5 = 11:30)")]
        public float desde;
        [Tooltip("Hora del juego en que termina (20.25 = 20:15)")]
        public float hasta;
        [Tooltip("Segundos mínimos entre cantos")]
        public float intervaloMin;
        [Tooltip("Segundos máximos entre cantos")]
        public float intervaloMax;
        [Range(0f, 1f)] public float volumen;
        [Tooltip("Suena apagado y lejano (filtro de graves)")]
        public bool lejano;

        public Tramo(string nombre, float desde, float hasta, float min, float max, float volumen, bool lejano = false)
        {
            this.nombre = nombre;
            this.desde = desde;
            this.hasta = hasta;
            intervaloMin = min;
            intervaloMax = max;
            this.volumen = volumen;
            this.lejano = lejano;
        }
    }

    [System.Serializable]
    public class Ave
    {
        public string nombre;
        [Tooltip("Texto del indicador de sonido. {distancia} se cambia por a lo lejos / a media distancia / cerca")]
        public string textoIndicador;
        [Tooltip("Uno o más sonidos; se elige uno al azar cada vez")]
        public AudioClip[] sonidos;
        public Tramo[] tramos;

        [System.NonSerialized] public AudioSource fuente;
        [System.NonSerialized] public AudioLowPassFilter filtro;
        [System.NonSerialized] public float espera = -1f;
        [System.NonSerialized] public Tramo tramoActual;
    }

    [Tooltip("Variación de tono para que no suene siempre igual")]
    public Vector2 rangoTono = new Vector2(0.92f, 1.08f);
    [Tooltip("Frecuencia de corte del filtro cuando el sonido es lejano")]
    public float corteLejano = 2500f;
    [Tooltip("Cuánto se reparten los cantos entre izquierda y derecha (0 = siempre al frente, 1 = totalmente a un lado)")]
    [Range(0f, 1f)] public float repartoEstereo = 0.8f;

    public Ave chincol = new Ave
    {
        nombre = "Chincol",
        textoIndicador = "*canto de chincol {distancia}*",
        tramos = new[]
        {
            new Tramo("Mañana", 7f, 11.5f, 12f, 25f, 0.8f),
            new Tramo("Mediodía y tarde", 11.5f, 17.5f, 45f, 90f, 0.5f),
            new Tramo("Atardecer", 17.5f, 19.5f, 12f, 25f, 0.8f),
            new Tramo("Anochecer", 19.5f, 20.25f, 120f, 180f, 0.4f),
        }
    };

    public Ave gaviota = new Ave
    {
        nombre = "Gaviota",
        textoIndicador = "*gaviotas {distancia}*",
        tramos = new[]
        {
            new Tramo("Mañana", 7f, 11.5f, 15f, 30f, 0.6f),
            new Tramo("Mediodía y tarde", 11.5f, 17.5f, 25f, 50f, 0.45f),
            new Tramo("Atardecer", 17.5f, 19.5f, 12f, 22f, 0.65f),
            new Tramo("Anochecer", 19.5f, 21f, 45f, 90f, 0.35f, true),
        }
    };

    private ClockManager reloj;

    private void Start()
    {
        reloj = FindAnyObjectByType<ClockManager>();
        Preparar(chincol);
        Preparar(gaviota);
    }

    private void Preparar(Ave ave)
    {
        var go = new GameObject("Sonido_" + ave.nombre);
        go.transform.SetParent(transform, false);
        ave.fuente = go.AddComponent<AudioSource>();
        ave.fuente.playOnAwake = false;
        ave.fuente.spatialBlend = 0f;
        ave.filtro = go.AddComponent<AudioLowPassFilter>();
        ave.filtro.cutoffFrequency = 22000f;
    }

    private void Update()
    {
        if (PausaJuego.Pausado) return;

        float hora = HoraActual();
        Avanzar(chincol, hora);
        Avanzar(gaviota, hora);
    }

    private float HoraActual()
    {
        if (reloj != null) return reloj.HoraDecimal;
        if (GameManager.Instance != null) return GameManager.Instance.GameData.horaActual;
        return 7f;
    }

    private void Avanzar(Ave ave, float hora)
    {
        if (ave.fuente == null || ave.sonidos == null || ave.sonidos.Length == 0) return;

        Tramo tramo = BuscarTramo(ave, hora);
        if (tramo != ave.tramoActual)
        {
            ave.tramoActual = tramo;
            if (tramo != null && (ave.espera < 0f || ave.espera > tramo.intervaloMax))
                ave.espera = Random.Range(0f, tramo.intervaloMax);
        }

        if (tramo == null) return;

        ave.espera -= Time.deltaTime;
        if (ave.espera > 0f) return;

        Cantar(ave, tramo);
        ave.espera = Random.Range(tramo.intervaloMin, tramo.intervaloMax);
    }

    private Tramo BuscarTramo(Ave ave, float hora)
    {
        if (ave.tramos == null) return null;
        foreach (var t in ave.tramos)
            if (t != null && hora >= t.desde && hora < t.hasta) return t;
        return null;
    }

    private void Cantar(Ave ave, Tramo tramo)
    {
        AudioClip clip = ave.sonidos[Random.Range(0, ave.sonidos.Length)];
        if (clip == null) return;

        float pan = Random.Range(-repartoEstereo, repartoEstereo);
        ave.fuente.panStereo = pan;
        ave.fuente.pitch = Random.Range(rangoTono.x, rangoTono.y);
        ave.filtro.cutoffFrequency = tramo.lejano ? corteLejano : 22000f;
        ave.fuente.PlayOneShot(clip, tramo.volumen * Configuracion.VolumenAmbiente);

        string plantilla = string.IsNullOrEmpty(ave.textoIndicador)
            ? (ave.nombre == "Gaviota" ? "*gaviotas {distancia}*" : $"*canto de {ave.nombre.ToLower()} {{distancia}}*")
            : ave.textoIndicador;

        {
            string distancia = tramo.lejano || tramo.volumen < 0.45f ? "a lo lejos"
                : tramo.volumen < 0.7f ? "a media distancia" : "cerca";
            IndicadoresSonido.Mostrar(plantilla.Replace("{distancia}", distancia),
                IndicadoresSonido.LadoDesdePan(pan), true);
        }
    }
}
