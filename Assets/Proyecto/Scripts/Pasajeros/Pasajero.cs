using System;
using System.Collections;
using UnityEngine;

public enum EstadoPasajero
{
    EsperandoEnAnden,
    Subiendo,
    DentroDelAscensor,
    Bajando,
    Terminado,
    CambiandoDeAnden,   // va al andén de la otra cabina (la suya ya cerró la puerta)
    Retirandose         // terminó el día: se va sin subir
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

    // Pasajero bondadoso: al bajar conversa con el jugador y deja una propina
    // NonSerialized: si Unity serializa este campo, al instanciar el prefab lo crea vacío (nunca null)
    // y TODOS los pasajeros quedan marcados como bondadosos -> Camila nunca se asigna.
    [NonSerialized] public PasajeroBondadosoData datosBondadoso;
    public bool EsBondadoso => datosBondadoso != null && !string.IsNullOrEmpty(datosBondadoso.id);

    private AscensorManager ascensorManager;
    private int ascensorIndex = 0;
    private bool inicializado = false;
    private Vector3 escalaOriginal;
    private Transform spawnPadre; // 🔑 Para volver al spawn al bajar

    // Cabina que está esperando (la cambia el PasajeroSpawner si esa cabina ya cerró la puerta)
    public int AscensorIndex => ascensorIndex;
    public bool EstaEsperando => estado == EstadoPasajero.EsperandoEnAnden;
    public bool EstaAfuera => estado == EstadoPasajero.EsperandoEnAnden
                           || estado == EstadoPasajero.Subiendo
                           || estado == EstadoPasajero.CambiandoDeAnden;

    public Action<Pasajero> OnPasajeroTerminado;

    // === FILA EN EL ANDÉN ===
    // El PasajeroSpawner le dice a qué puesto de la fila ir y si ya le toca subir.
    public Func<Pasajero, bool> PuedeSubir;
    private Vector3 destinoFila;
    private float velocidadFila;
    private bool tieneDestino = false;
    public bool Deslizando => tieneDestino;

    // Saliendo de pantalla o esperando afuera para entrar al andén de la otra cabina
    private bool enTransito = false;

    [Header("=== AL CAMINAR ===")]
    [Tooltip("Hacia dónde mira el dibujo original. Al caminar se da vuelta (espejo) para mirar hacia donde va")]
    public bool spriteMiraALaDerecha = true;

    public void MarcarBondadoso(PasajeroBondadosoData datos)
    {
        datosBondadoso = datos;
        RefrescarEmocionPorPopularidad();
    }

    // === INICIALIZACIÓN ===
    // conPop = false: aparece de inmediato (entra deslizándose a la fila en vez de hacer pop)
    public void Inicializar(RolPasajero rolAsignado, int index, AscensorManager manager, Transform padre = null, bool conPop = true)
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

        if (!conPop)
        {
            MostrarEmoji();
            return;
        }

        transform.localScale = Vector3.zero;

        StartCoroutine(EfectoPop());

        // 🔑 Mostrar emoji al aparecer (después del pop)
        StartCoroutine(MostrarEmojiDespuesDePop());
    }

    // Se desliza hasta esa posición (su puesto en la fila)
    public void IrA(Vector3 posicion, float velocidad)
    {
        posicion.z = transform.position.z;
        velocidadFila = velocidad;

        if ((posicion - transform.position).sqrMagnitude < 0.000001f)
        {
            tieneDestino = false;
            return;
        }

        destinoFila = posicion;
        tieneDestino = true;
    }

    // Espejo: mira hacia el lado al que camina
    private void MirarHacia(float dx)
    {
        if (Mathf.Abs(dx) < 0.0001f) return;

        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        bool vaALaDerecha = dx > 0f;
        sr.flipX = vaALaDerecha != spriteMiraALaDerecha;
    }

    private void MoverHaciaDestino()
    {
        if (!tieneDestino) return;

        MirarHacia(destinoFila.x - transform.position.x);
        transform.position = Vector3.MoveTowards(transform.position, destinoFila, velocidadFila * Time.deltaTime);

        if ((transform.position - destinoFila).sqrMagnitude < 0.000001f)
        {
            transform.position = destinoFila;
            tieneDestino = false;
        }
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

        if (estado == EstadoPasajero.EsperandoEnAnden || estado == EstadoPasajero.CambiandoDeAnden)
            MoverHaciaDestino();

        // Llegó a su puesto en la fila del otro andén
        if (estado == EstadoPasajero.CambiandoDeAnden && !enTransito && !tieneDestino)
        {
            timerEmojiEspera = 0f;
            estado = EstadoPasajero.EsperandoEnAnden;
        }

        if (estado == EstadoPasajero.EsperandoEnAnden)
        {
            ProcesarEspera();
        }
    }

    // === CAMBIO DE ANDÉN ===
    // Su cabina cerró la puerta sin que alcanzara a subir. El cambio tiene dos partes:
    //  1) SalirParaCambiar: se da vuelta y sale caminando por atrás de su fila (por donde llegó).
    //  2) EntrarEnAnden: después de esperar afuera, aparece por atrás de la fila de la otra cabina
    //     y camina hasta su puesto (el spawner le da el puesto con IrA).
    public void SalirParaCambiar(Vector3 salida, float velocidad)
    {
        if (estado != EstadoPasajero.EsperandoEnAnden) return;

        estado = EstadoPasajero.CambiandoDeAnden;
        enTransito = true;
        IrA(salida, velocidad);
    }

    public void EntrarEnAnden(int nuevoIndice, Transform nuevoPadre, float escala, Vector3 entrada)
    {
        if (estado != EstadoPasajero.CambiandoDeAnden) return;

        ascensorIndex = nuevoIndice;
        spawnPadre = nuevoPadre;
        transform.SetParent(nuevoPadre, true);
        entrada.z = transform.position.z;
        transform.position = entrada;
        escalaOriginal = Vector3.one * escala;
        transform.localScale = escalaOriginal;

        tieneDestino = false;
        enTransito = false;   // desde aquí camina a su puesto y vuelve a esperar
    }

    // === RETIRARSE (fin del día, o no cabe en la fila) ===
    // Se va deslizando hasta 'salida' y desaparece.
    public void Retirarse(Vector3 salida, float velocidad)
    {
        if (!EstaAfuera) return;

        StopAllCoroutines();   // corta una subida a medias
        tieneDestino = false;
        transform.localScale = escalaOriginal;
        estado = EstadoPasajero.Retirandose;
        salida.z = transform.position.z;
        StartCoroutine(SecuenciaRetirarse(salida, velocidad));
    }

    private IEnumerator SecuenciaRetirarse(Vector3 salida, float velocidad)
    {
        // Tiempo real: el día puede terminar con el juego congelado
        MirarHacia(salida.x - transform.position.x);

        while ((transform.position - salida).sqrMagnitude > 0.000001f)
        {
            transform.position = Vector3.MoveTowards(transform.position, salida, velocidad * Time.unscaledDeltaTime);
            yield return null;
        }

        estado = EstadoPasajero.Terminado;
        OnPasajeroTerminado?.Invoke(this);
        Destroy(gameObject);
    }

    private void ProcesarEspera()
    {
        var asc = ascensorManager.ascensores[ascensorIndex];
        bool ascensorEstaAbajo = asc.estaAbajo;
        bool ascensorEnMovimiento = asc.enMovimiento;

        bool ascensorEnMiLugar = (rol == RolPasajero.SubeEnInicio && ascensorEstaAbajo)
                              || (rol == RolPasajero.SubeEnFinal && !ascensorEstaAbajo);

        // La W (o la cabina ya lista) cierra la puerta: nadie más sube
        bool puertaCerrada = ProcesoPartida.Instance != null && ProcesoPartida.Instance.PuertaCerrada(ascensorIndex);

        // Sube en orden: ya en su puesto y solo si le toca (los primeros de la fila que caben)
        bool leToca = !tieneDestino && (PuedeSubir == null || PuedeSubir(this));

        if (ascensorEnMiLugar && !ascensorEnMovimiento && !puertaCerrada && leToca)
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

        // Si durante ese segundo se pulsó W, la puerta se cerró y no alcanza a subir
        bool puertaCerrada = ProcesoPartida.Instance != null && ProcesoPartida.Instance.PuertaCerrada(ascensorIndex);

        if (puertaCerrada || !ascensorManager.HayEspacioEn(ascensorIndex))
        {
            estado = EstadoPasajero.EsperandoEnAnden;
            yield break;
        }

        yield return StartCoroutine(PopYDesaparecer());

        // Mientras hacía el pop pudo llenarse la cabina (otro pasajero subió antes) o cerrarse la puerta.
        // Si no cabe, vuelve al andén en vez de quedar "dentro" sin contar.
        puertaCerrada = ProcesoPartida.Instance != null && ProcesoPartida.Instance.PuertaCerrada(ascensorIndex);
        if (puertaCerrada || !ascensorManager.IntentarSubirPasajero(ascensorIndex, this))
        {
            transform.localScale = escalaOriginal;
            estado = EstadoPasajero.EsperandoEnAnden;
            yield break;
        }

        estado = EstadoPasajero.DentroDelAscensor;

        if (BondadososManager.Instance != null)
            BondadososManager.Instance.AlSubirPasajero(this);

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

    // Llegó a su estación: sale por la puerta de la cabina, paga y se va caminando hasta la salida.
    // retraso: para que, si bajan varios, salgan uno detrás del otro.
    public void BajarDelAscensor(Vector3 posicionSalida, Vector3 puerta, bool estacionAbajo, float retraso = 0f)
    {
        if (estado != EstadoPasajero.DentroDelAscensor) return;

        // 🔑 Detener emojis constantes
        DetenerEmojisConstantes();

        // 🔑 Cambiar estado ANTES de activar
        estado = EstadoPasajero.Bajando;

        StartCoroutine(SecuenciaBajada(posicionSalida, puerta, estacionAbajo, retraso));
    }

    private IEnumerator SecuenciaBajada(Vector3 posicionSalida, Vector3 puerta, bool estacionAbajo, float retraso)
    {
        if (retraso > 0f)
            yield return new WaitForSeconds(retraso);

        // Tamaño y suelo del andén donde se baja (no del andén donde subió: es la otra estación)
        Transform padre = spawnPadre;
        float velocidad = 1f;
        Vector3 escalaLlegada = escalaOriginal;
        if (PasajeroSpawner.Instance != null &&
            PasajeroSpawner.Instance.DatosDeLlegada(ascensorIndex, estacionAbajo, out var p, out var escala, out var vel))
        {
            padre = p;
            escalaLlegada = Vector3.one * escala;
            velocidad = vel;
            // Camina por el suelo del andén
            puerta.y = p.position.y;
            posicionSalida.y = p.position.y;
        }

        transform.SetParent(padre, true);
        puerta.z = transform.position.z;
        posicionSalida.z = transform.position.z;
        transform.position = puerta;
        escalaOriginal = escalaLlegada;
        transform.localScale = Vector3.zero;

        // 🔑 Mostrar el sprite de nuevo
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = true;

        if (puntoEmoji != null)
        {
            var srHijo = puntoEmoji.GetComponent<SpriteRenderer>();
            if (srHijo != null) srHijo.enabled = true;
        }

        MirarHacia(posicionSalida.x - puerta.x);

        // Sale de la cabina y paga
        yield return StartCoroutine(EfectoPop());

        int pago = CalcularPago();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PasajeroCompleto();
            GameManager.Instance.SumarDinero(pago);
            Debug.Log($"💰 Pasajero pagó ${pago} (Emoción: {emocionActual})");
        }
        MostrarPago(pago);

        // 🔑 Mostrar emoji al aparecer
        MostrarEmoji();

        if (EsBondadoso && BondadososManager.Instance != null)
            yield return StartCoroutine(BondadososManager.Instance.Encuentro(this, datosBondadoso));

        // Se va caminando hasta la salida del andén
        MirarHacia(posicionSalida.x - transform.position.x);
        while ((transform.position - posicionSalida).sqrMagnitude > 0.000001f)
        {
            transform.position = Vector3.MoveTowards(transform.position, posicionSalida, velocidad * Time.deltaTime);
            yield return null;
        }

        estado = EstadoPasajero.Terminado;
        OnPasajeroTerminado?.Invoke(this);
        Destroy(gameObject);
    }

    [Header("=== PAGO ===")]
    public Color colorPago = new Color(0.4f, 1f, 0.4f);
    public Color colorSinPago = new Color(1f, 0.4f, 0.4f);

    // "+$20" sobre la cabeza (o "$0" en rojo si no pagó)
    private void MostrarPago(int pago)
    {
        float alto = 1f;
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null) alto = sr.bounds.size.y;

        Vector3 cabeza = transform.position + Vector3.up * (alto * 0.6f);

        if (pago > 0) TextoFlotante.Mostrar($"+${pago}", cabeza, colorPago, alto);
        else TextoFlotante.Mostrar("$0", cabeza, colorSinPago, alto);
    }

    // === PAGO SEGÚN EMOCIÓN ===
    public int CalcularPago()
    {
        RefrescarEmocionPorPopularidad();

        int pagoBase = 10;
        if (GameManager.Instance != null && GameManager.Instance.GameData != null)
            pagoBase = GameManager.Instance.GameData.pagoPorPasajero;

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
        if (EsBondadoso)
        {
            felicidad = 100f;
            emocionActual = EmocionPasajero.Feliz;
            return;
        }

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