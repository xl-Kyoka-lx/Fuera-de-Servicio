using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SonidoAscensor : MonoBehaviour
{
    [Tooltip("Si se deja vacío, se busca solo en la escena")]
    public AscensorManager ascensorManager;

    [Header("Motor (suena apenas se presiona Espacio)")]
    public AudioClip sonidoMotor;
    [Range(0f, 1f)] public float volumenMotor = 0.7f;
    [Tooltip("Marcado: el motor se repite durante todo el viaje")]
    public bool motorEnBucle = true;

    [Header("Ascensor (empieza después del motor)")]
    public AudioClip sonido;
    [Range(0f, 1f)] public float volumen = 0.7f;
    [Tooltip("Segundos entre el motor y el sonido del ascensor")]
    public float retrasoSonidoAscensor = 1f;

    [Header("Indicadores de sonido (accesibilidad)")]
    public string indicadorArranque = "*el motor del ascensor arranca*";
    public string indicadorDetencion = "*el ascensor se detiene*";

    [Header("Al frenar o llegar")]
    [Tooltip("Segundos que tardan los dos sonidos en apagarse")]
    public float fundidoSalida = 1f;
    [Tooltip("Tono de los sonidos cuando el ascensor casi se detiene con Shift mantenido (1 = normal)")]
    [Range(0.5f, 1f)] public float tonoAlFrenar = 0.8f;

    private AudioSource fuenteAscensor;

    // Volumen del Inspector por el de la barra "Volumen Ascensor" de Opciones
    private float VolMotor => volumenMotor * Configuracion.VolumenAscensor;
    private float VolAscensor => volumen * Configuracion.VolumenAscensor;
    private AudioSource fuenteMotor;
    private float esperaAscensor = -1f;
    private bool apagando;
    private bool pausadoPorJuego;

    private void Awake()
    {
        fuenteAscensor = GetComponent<AudioSource>();
        fuenteAscensor.playOnAwake = false;
        fuenteAscensor.loop = true;
        fuenteAscensor.clip = sonido;

        var motorGO = new GameObject("SonidoMotor");
        motorGO.transform.SetParent(transform, false);
        fuenteMotor = motorGO.AddComponent<AudioSource>();
        fuenteMotor.playOnAwake = false;
        fuenteMotor.loop = motorEnBucle;
        fuenteMotor.clip = sonidoMotor;
    }

    private void Start()
    {
        if (ascensorManager == null)
            ascensorManager = FindAnyObjectByType<AscensorManager>();

        if (ascensorManager != null)
        {
            ascensorManager.OnArranque += Arrancar;
            ascensorManager.OnFreno += Apagar;
        }
    }

    private void OnDestroy()
    {
        if (ascensorManager != null)
        {
            ascensorManager.OnArranque -= Arrancar;
            ascensorManager.OnFreno -= Apagar;
        }
    }

    private void Arrancar()
    {
        apagando = false;
        IndicadoresSonido.Mostrar(indicadorArranque, IndicadoresSonido.Lado.Frente, false);

        if (fuenteMotor.clip != null)
        {
            fuenteMotor.volume = VolMotor;
            fuenteMotor.Play();
        }

        fuenteAscensor.Stop();
        fuenteAscensor.volume = VolAscensor;
        esperaAscensor = retrasoSonidoAscensor;
    }

    private void Apagar()
    {
        esperaAscensor = -1f;
        if ((fuenteMotor.isPlaying || fuenteAscensor.isPlaying) && !apagando)
        {
            apagando = true;
            IndicadoresSonido.Mostrar(indicadorDetencion, IndicadoresSonido.Lado.Frente, false);
        }
    }

    private void Update()
    {
        if (PausaJuego.Pausado)
        {
            if (!pausadoPorJuego)
            {
                pausadoPorJuego = true;
                fuenteMotor.Pause();
                fuenteAscensor.Pause();
            }
            return;
        }

        if (pausadoPorJuego)
        {
            pausadoPorJuego = false;
            fuenteMotor.UnPause();
            fuenteAscensor.UnPause();
        }

        if (esperaAscensor >= 0f)
        {
            esperaAscensor -= Time.deltaTime;
            if (esperaAscensor < 0f && fuenteAscensor.clip != null)
                fuenteAscensor.Play();
        }

        if (!apagando && ascensorManager != null)
        {
            float f = ascensorManager.FactorVelocidad;
            float tono = Mathf.Lerp(tonoAlFrenar, 1f, f);
            fuenteMotor.volume = VolMotor * f;
            fuenteAscensor.volume = VolAscensor * f;
            fuenteMotor.pitch = tono;
            fuenteAscensor.pitch = tono;
        }

        bool sonando = fuenteMotor.isPlaying || fuenteAscensor.isPlaying;
        bool quieto = ascensorManager == null || !ascensorManager.enMovimiento || ascensorManager.DetenidoEnRuta;
        if (quieto && sonando) Apagar();

        if (!apagando) return;

        float duracion = Mathf.Max(0.01f, fundidoSalida);
        fuenteMotor.volume = Mathf.MoveTowards(fuenteMotor.volume, 0f, Mathf.Max(VolMotor, 0.01f) / duracion * Time.deltaTime);
        fuenteAscensor.volume = Mathf.MoveTowards(fuenteAscensor.volume, 0f, Mathf.Max(VolAscensor, 0.01f) / duracion * Time.deltaTime);

        if (fuenteMotor.volume <= 0f && fuenteAscensor.volume <= 0f)
        {
            fuenteMotor.Stop();
            fuenteAscensor.Stop();
            apagando = false;
        }
    }
}
