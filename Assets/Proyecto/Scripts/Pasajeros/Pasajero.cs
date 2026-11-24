using System;
using System.Collections;
using UnityEngine;

public enum EstadoPasajero
{
    EsperandoEnAnden,
    Subiendo,
    DentroDelAscensor,
    Bajando,
    Terminado
}

public enum RolPasajero
{
    SubeEnInicio,
    SubeEnFinal
}

public enum EmocionPasajero
{
    Feliz,
    Neutro,
    Enojado
}

public class Pasajero : MonoBehaviour
{
    [Header("=== CONFIGURACIÓN ===")]
    public float duracionPop = 0.2f;
    public float tiempoAntesDeSubir = 1f; // 🔑 1 segundo antes de desaparecer
    public float tiempoAntesDeBajar = 1f; // 🔑 1 segundo antes de desaparecer al bajar

    [Header("=== EMOCIONES ===")]
    [Range(0, 100)] public float felicidad = 50f;
    [Tooltip("Cuánto se aleja cada pasajero, hacia arriba o hacia abajo, de la popularidad general")]
    public float variacionFelicidad = 25f;
    private float sesgoFelicidad = 0f;
    public EmocionPasajero emocionActual = EmocionPasajero.Neutro;

    [Header("=== EMOJIS FLOTANTES ===")]
    public GameObject emojiFelizPrefab;
    public GameObject emojiEnojadoPrefab;
    public Transform puntoEmoji;
    public Vector3 escalaEmoji = new Vector3(0.15f, 0.15f, 1f); // 🔑 NUEVO

    [Header("=== EMOJIS CONSTANTES (DENTRO) ===")]
    public float intervaloEmojiMin = 3f;
    public float intervaloEmojiMax = 5f;

    private Coroutine corrutinaEmojisConstantes;

    [Header("=== ESTADO ===")]
    public EstadoPasajero estado = EstadoPasajero.EsperandoEnAnden;
    public RolPasajero rol = RolPasajero.SubeEnInicio;

    private AscensorManager ascensorManager;
    private int ascensorIndex = 0;
    private bool inicializado = false;
    private Vector3 escalaOriginal;
    private Transform spawnPadre; // 🔑 Para volver al spawn al bajar

    public Action<Pasajero> OnPasajeroTerminado;

    // === INICIALIZACIÓN ===
    public void Inicializar(RolPasajero rolAsignado, int index, AscensorManager manager, Transform padre = null)
    {
        rol = rolAsignado;
        ascensorIndex = index;
        ascensorManager = manager;
        spawnPadre = padre;
        estado = EstadoPasajero.EsperandoEnAnden;
        inicializado = true;

        // 🔑 Emoción aleatoria al spawnear
        sesgoFelicidad = UnityEngine.Random.Range(-variacionFelicidad, variacionFelicidad);
        RefrescarEmocionPorPopularidad();
        ActualizarEmocion();

        escalaOriginal = transform.localScale;
        transform.localScale = Vector3.zero;

        StartCoroutine(EfectoPop());

        // 🔑 Mostrar emoji al aparecer (después del pop)
        StartCoroutine(MostrarEmojiDespuesDePop());
    }

    private IEnumerator MostrarEmojiDespuesDePop()
    {
        yield return new WaitForSeconds(duracionPop);
        MostrarEmoji();
    }

    // === UPDATE ===
    private void Update()
    {
        if (!inicializado || ascensorManager == null) return;

        if (estado == EstadoPasajero.EsperandoEnAnden)
        {
            ProcesarEspera();
        }
    }

    private void ProcesarEspera()
    {
        var asc = ascensorManager.ascensores[ascensorIndex];
        bool ascensorEstaAbajo = asc.estaAbajo;
        bool ascensorEnMovimiento = asc.enMovimiento;

        bool ascensorEnMiLugar = (rol == RolPasajero.SubeEnInicio && ascensorEstaAbajo)
                              || (rol == RolPasajero.SubeEnFinal && !ascensorEstaAbajo);

        if (ascensorEnMiLugar && !ascensorEnMovimiento)
        {
            IniciarSubida();
            return;
        }

        // 🔑 Mostrar emoji periódicamente mientras espera
        timerEmojiEspera += Time.deltaTime;
        if (timerEmojiEspera >= intervaloEmojiEspera)
        {
            timerEmojiEspera = 0f;
            MostrarEmoji();
        }
    }

    private float timerEmojiEspera = 0f;
    public float intervaloEmojiEspera = 3f; 

    // === SUBIENDO ===
    private void IniciarSubida()
    {
        estado = EstadoPasajero.Subiendo;
        StartCoroutine(SecuenciaSubida());
    }

    private IEnumerator SecuenciaSubida()
    {
        yield return new WaitForSeconds(tiempoAntesDeSubir);

        if (!ascensorManager.HayEspacioEn(ascensorIndex))
        {
            estado = EstadoPasajero.EsperandoEnAnden;
            yield break;
        }

        yield return StartCoroutine(PopYDesaparecer());

        estado = EstadoPasajero.DentroDelAscensor;
        ascensorManager.IntentarSubirPasajero(ascensorIndex, this);

        if (GameManager.Instance != null && GameManager.Instance.ResumenDia != null)
            GameManager.Instance.ResumenDia.RegistrarPasajeroTransportado();

        // 🔑 Mover al punto interior del ascensor
        var asc = ascensorManager.ascensores[ascensorIndex];
        transform.SetParent(asc.ascensorTransform);

        if (asc.puntoInterior != null)
        {
            transform.position = asc.puntoInterior.position;
            // 🔑 Offset pequeño para que no se apilen exactamente
            transform.position += new Vector3(
                UnityEngine.Random.Range(-0.2f, 0.2f),
                UnityEngine.Random.Range(-0.1f, 0.1f),
                0
            );
        }

        // 🔑 Ocultar sprite (NO el GameObject)
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        if (puntoEmoji != null)
        {
            var srHijo = puntoEmoji.GetComponent<SpriteRenderer>();
            if (srHijo != null) srHijo.enabled = false;
        }

        // 🔑 Iniciar emojis constantes
        IniciarEmojisConstantes();
    }

    // === POP Y DESAPARECER ===
    private IEnumerator PopYDesaparecer()
    {
        float timer = 0f;
        while (timer < duracionPop)
        {
            timer += Time.deltaTime;
            float t = timer / duracionPop;
            float escala = Mathf.Lerp(1.5f, 0f, t);
            transform.localScale = escalaOriginal * escala;
            yield return null;
        }
    }

    // === EFECTO POP DE APARICIÓN ===
    private IEnumerator EfectoPop()
    {
        float timer = 0f;
        while (timer < duracionPop)
        {
            timer += Time.deltaTime;
            float t = timer / duracionPop;
            float escala = Mathf.Sin(t * Mathf.PI) * 1.2f;
            transform.localScale = escalaOriginal * escala;
            yield return null;
        }
        transform.localScale = escalaOriginal;
    }

    public void BajarDelAscensor(Vector3 posicionSalida)
    {
        if (estado != EstadoPasajero.DentroDelAscensor) return;

        // 🔑 Detener emojis constantes
        DetenerEmojisConstantes();

        // 🔑 Cambiar estado ANTES de activar
        estado = EstadoPasajero.Bajando;

        // 🔑 Iniciar corrutina (el GameObject ya está activo, solo el sprite está oculto)
        StartCoroutine(SecuenciaBajada(posicionSalida));
    }

    private IEnumerator SecuenciaBajada(Vector3 posicionSalida)
    {
        transform.SetParent(spawnPadre);
        transform.position = posicionSalida;

        // 🔑 Mostrar el sprite de nuevo
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = true;

        if (puntoEmoji != null)
        {
            var srHijo = puntoEmoji.GetComponent<SpriteRenderer>();
            if (srHijo != null) srHijo.enabled = true;
        }

        transform.localScale = Vector3.zero;

        int pago = CalcularPago();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PasajeroCompleto();
            GameManager.Instance.SumarDinero(pago);
            Debug.Log($"💰 Pasajero pagó ${pago} (Emoción: {emocionActual})");
        }

        // 🔑 Mostrar emoji al aparecer
        MostrarEmoji();

        yield return StartCoroutine(EfectoPop());
        yield return new WaitForSeconds(tiempoAntesDeBajar);
        yield return StartCoroutine(PopYDesaparecer());

        estado = EstadoPasajero.Terminado;
        OnPasajeroTerminado?.Invoke(this);
        Destroy(gameObject);
    }

    // === PAGO SEGÚN EMOCIÓN ===
    public int CalcularPago()
    {
        RefrescarEmocionPorPopularidad();

        int pagoBase = 10;

        switch (emocionActual)
        {
            case EmocionPasajero.Feliz:
                // 50% de pagar doble
                return UnityEngine.Random.value > 0.5f ? pagoBase * 2 : pagoBase;

            case EmocionPasajero.Neutro:
                return pagoBase;

            case EmocionPasajero.Enojado:
                // 50% de no pagar, 50% de pagar la mitad
                return UnityEngine.Random.value > 0.5f ? 0 : pagoBase / 2;

            default:
                return pagoBase;
        }
    }

    // === EMOCIONES ===
    // La felicidad sigue la popularidad general (promedio de mantenimiento y limpieza),
    // con una variación propia de cada pasajero.
    private void RefrescarEmocionPorPopularidad()
    {
        if (GameManager.Instance == null) return;

        float popularidad = GameManager.Instance.GameData.contadorPopularidad;
        felicidad = Mathf.Clamp(popularidad + sesgoFelicidad, 0f, 100f);

        if (felicidad >= 70f) emocionActual = EmocionPasajero.Feliz;
        else if (felicidad <= 30f) emocionActual = EmocionPasajero.Enojado;
        else emocionActual = EmocionPasajero.Neutro;
    }

    public void ModificarFelicidad(float cantidad)
    {
        felicidad = Mathf.Clamp(felicidad + cantidad, 0, 100);
        ActualizarEmocion();
    }

    private void ActualizarEmocion()
    {
        EmocionPasajero anterior = emocionActual;

        if (felicidad >= 70) emocionActual = EmocionPasajero.Feliz;
        else if (felicidad <= 30) emocionActual = EmocionPasajero.Enojado;
        else emocionActual = EmocionPasajero.Neutro;

        // 🔑 Si cambió la emoción, mostrar emoji flotante
        if (anterior != emocionActual && inicializado && estado == EstadoPasajero.DentroDelAscensor)
        {
            MostrarEmoji();
        }
    }

    // === EMOJI FLOTANTE ===
    private void MostrarEmoji()
    {
        RefrescarEmocionPorPopularidad();

        GameObject prefabEmoji = emocionActual switch
        {
            EmocionPasajero.Feliz => emojiFelizPrefab,
            EmocionPasajero.Enojado => emojiEnojadoPrefab,
            _ => null
        };

        if (prefabEmoji == null) return;

        Vector3 posicionEmoji = puntoEmoji != null ? puntoEmoji.position : transform.position + Vector3.up * 0.5f;

        GameObject emoji = Instantiate(prefabEmoji, posicionEmoji, Quaternion.identity);
        emoji.transform.SetParent(null);

        // 🔑 Aplicar escala
        emoji.transform.localScale = escalaEmoji;

        // 🔑 Animación
        FloatingEmoji floatScript = emoji.GetComponent<FloatingEmoji>();
        if (floatScript == null)
            floatScript = emoji.AddComponent<FloatingEmoji>();

        floatScript.Inicializar(0.5f, 1.5f, escalaEmoji);
    }
    // 🔑 Iniciar la corrutina de emojis constantes
    private void IniciarEmojisConstantes()
    {
        if (corrutinaEmojisConstantes != null)
            StopCoroutine(corrutinaEmojisConstantes);

        corrutinaEmojisConstantes = StartCoroutine(LoopEmojisConstantes());
    }

    private void DetenerEmojisConstantes()
    {
        if (corrutinaEmojisConstantes != null)
        {
            StopCoroutine(corrutinaEmojisConstantes);
            corrutinaEmojisConstantes = null;
        }
    }

    private IEnumerator LoopEmojisConstantes()
    {
        while (estado == EstadoPasajero.DentroDelAscensor)
        {
            // 🔑 Esperar un tiempo aleatorio entre min y max
            float espera = UnityEngine.Random.Range(intervaloEmojiMin, intervaloEmojiMax);
            yield return new WaitForSeconds(espera);

            // 🔑 Verificar que seguimos dentro
            if (estado != EstadoPasajero.DentroDelAscensor) yield break;

            // 🔑 Mostrar el emoji (si no es neutro)
            MostrarEmoji();
        }
    }
}