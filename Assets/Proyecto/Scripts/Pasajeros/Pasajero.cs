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
    CambiandoDeAnden,
    Retirandose
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
    public float tiempoAntesDeSubir = 1f;
    public float tiempoAntesDeBajar = 1f;

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
    public Vector3 escalaEmoji = new Vector3(0.15f, 0.15f, 1f);

    [Header("=== EMOJIS CONSTANTES (DENTRO) ===")]
    public float intervaloEmojiMin = 3f;
    public float intervaloEmojiMax = 5f;

    private Coroutine corrutinaEmojisConstantes;

    [Header("=== ESTADO ===")]
    public EstadoPasajero estado = EstadoPasajero.EsperandoEnAnden;
    public RolPasajero rol = RolPasajero.SubeEnInicio;

    [NonSerialized] public PasajeroBondadosoData datosBondadoso;
    public bool EsBondadoso => datosBondadoso != null && !string.IsNullOrEmpty(datosBondadoso.id);

    [HideInInspector] public bool viajaGratis = false;
    private bool yaSePreguntoSiTienePlata = false;

    private AscensorManager ascensorManager;
    private int ascensorIndex = 0;
    private bool inicializado = false;
    private Vector3 escalaOriginal;
    private Transform spawnPadre;

    public int AscensorIndex => ascensorIndex;
    public bool EstaEsperando => estado == EstadoPasajero.EsperandoEnAnden;
    public bool EstaAfuera => estado == EstadoPasajero.EsperandoEnAnden
                           || estado == EstadoPasajero.Subiendo
                           || estado == EstadoPasajero.CambiandoDeAnden;

    public Action<Pasajero> OnPasajeroTerminado;

    public Func<Pasajero, bool> PuedeSubir;
    private Vector3 destinoFila;
    private float velocidadFila;
    private bool tieneDestino = false;
    public bool Deslizando => tieneDestino;

    private bool enTransito = false;

    [Header("=== AL CAMINAR ===")]
    [Tooltip("Hacia dónde mira el dibujo original. Al caminar se da vuelta (espejo) para mirar hacia donde va")]
    public bool spriteMiraALaDerecha = true;

    public void MarcarBondadoso(PasajeroBondadosoData datos)
    {
        datosBondadoso = datos;
        RefrescarEmocionPorPopularidad();
    }

    public void Inicializar(RolPasajero rolAsignado, int index, AscensorManager manager, Transform padre = null, bool conPop = true)
    {
        rol = rolAsignado;
        ascensorIndex = index;
        ascensorManager = manager;
        spawnPadre = padre;
        estado = EstadoPasajero.EsperandoEnAnden;
        inicializado = true;

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

        StartCoroutine(MostrarEmojiDespuesDePop());
    }

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

    private void Update()
    {
        if (!inicializado || ascensorManager == null) return;

        if (estado == EstadoPasajero.EsperandoEnAnden || estado == EstadoPasajero.CambiandoDeAnden)
            MoverHaciaDestino();

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
        enTransito = false;
    }

    public void Retirarse(Vector3 salida, float velocidad)
    {
        if (!EstaAfuera) return;

        StopAllCoroutines();
        tieneDestino = false;
        transform.localScale = escalaOriginal;
        estado = EstadoPasajero.Retirandose;
        salida.z = transform.position.z;
        StartCoroutine(SecuenciaRetirarse(salida, velocidad));
    }

    private IEnumerator SecuenciaRetirarse(Vector3 salida, float velocidad)
    {
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

        bool puertaCerrada = ProcesoPartida.Instance != null && ProcesoPartida.Instance.PuertaCerrada(ascensorIndex);

        bool leToca = !tieneDestino && (PuedeSubir == null || PuedeSubir(this));

        if (ascensorEnMiLugar && !ascensorEnMovimiento && !puertaCerrada && leToca)
        {
            IniciarSubida();
            return;
        }

        timerEmojiEspera += Time.deltaTime;
        if (timerEmojiEspera >= intervaloEmojiEspera)
        {
            timerEmojiEspera = 0f;
            MostrarEmoji();
        }
    }

    private float timerEmojiEspera = 0f;
    public float intervaloEmojiEspera = 3f;

    private void IniciarSubida()
    {
        estado = EstadoPasajero.Subiendo;
        StartCoroutine(SecuenciaSubida());
    }

    private IEnumerator SecuenciaSubida()
    {
        yield return new WaitForSeconds(tiempoAntesDeSubir);

        bool puertaCerrada = ProcesoPartida.Instance != null && ProcesoPartida.Instance.PuertaCerrada(ascensorIndex);

        if (puertaCerrada || !ascensorManager.HayEspacioEn(ascensorIndex))
        {
            estado = EstadoPasajero.EsperandoEnAnden;
            yield break;
        }

        if (!yaSePreguntoSiTienePlata)
        {
            yaSePreguntoSiTienePlata = true;

            var decisiones = DecisionesManager.Instance;
            if (decisiones != null && decisiones.QuierePreguntarSinPlata(this))
            {
                bool respondio = false;
                bool dejarSubir = false;
                decisiones.PreguntarSinPlata(r => { dejarSubir = r; respondio = true; });

                while (!respondio) yield return null;

                if (!dejarSubir)
                {
                    estado = EstadoPasajero.EsperandoEnAnden;
                    if (PasajeroSpawner.Instance != null) PasajeroSpawner.Instance.Despedir(this);
                    else Retirarse(transform.position, 1f);
                    yield break;
                }

                viajaGratis = true;

                puertaCerrada = ProcesoPartida.Instance != null && ProcesoPartida.Instance.PuertaCerrada(ascensorIndex);
                if (puertaCerrada || !ascensorManager.HayEspacioEn(ascensorIndex))
                {
                    estado = EstadoPasajero.EsperandoEnAnden;
                    yield break;
                }
            }
        }

        yield return StartCoroutine(PopYDesaparecer());

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

        var asc = ascensorManager.ascensores[ascensorIndex];
        transform.SetParent(asc.ascensorTransform);

        if (asc.puntoInterior != null)
        {
            transform.position = asc.puntoInterior.position;
            transform.position += new Vector3(
                UnityEngine.Random.Range(-0.2f, 0.2f),
                UnityEngine.Random.Range(-0.1f, 0.1f),
                0
            );
        }

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        if (puntoEmoji != null)
        {
            var srHijo = puntoEmoji.GetComponent<SpriteRenderer>();
            if (srHijo != null) srHijo.enabled = false;
        }

        IniciarEmojisConstantes();
    }

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

    public void BajarDelAscensor(Vector3 posicionSalida, Vector3 puerta, bool estacionAbajo, float retraso = 0f)
    {
        if (estado != EstadoPasajero.DentroDelAscensor) return;

        DetenerEmojisConstantes();

        estado = EstadoPasajero.Bajando;

        StartCoroutine(SecuenciaBajada(posicionSalida, puerta, estacionAbajo, retraso));
    }

    private IEnumerator SecuenciaBajada(Vector3 posicionSalida, Vector3 puerta, bool estacionAbajo, float retraso)
    {
        if (retraso > 0f)
            yield return new WaitForSeconds(retraso);

        Transform padre = spawnPadre;
        float velocidad = 1f;
        Vector3 escalaLlegada = escalaOriginal;
        if (PasajeroSpawner.Instance != null &&
            PasajeroSpawner.Instance.DatosDeLlegada(ascensorIndex, estacionAbajo, out var p, out var escala, out var vel))
        {
            padre = p;
            escalaLlegada = Vector3.one * escala;
            velocidad = vel;
            puerta.y = p.position.y;
            posicionSalida.y = p.position.y;
        }

        transform.SetParent(padre, true);
        puerta.z = transform.position.z;
        posicionSalida.z = transform.position.z;
        transform.position = puerta;
        escalaOriginal = escalaLlegada;
        transform.localScale = Vector3.zero;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = true;

        if (puntoEmoji != null)
        {
            var srHijo = puntoEmoji.GetComponent<SpriteRenderer>();
            if (srHijo != null) srHijo.enabled = true;
        }

        MirarHacia(posicionSalida.x - puerta.x);

        yield return StartCoroutine(EfectoPop());

        int pago = CalcularPago();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PasajeroCompleto();
            GameManager.Instance.SumarDinero(pago);
            Debug.Log($"💰 Pasajero pagó ${pago} (Emoción: {emocionActual})");
        }
        MostrarPago(pago);

        MostrarEmoji();

        if (EsBondadoso && BondadososManager.Instance != null)
            yield return StartCoroutine(BondadososManager.Instance.Encuentro(this, datosBondadoso));

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

    private void MostrarPago(int pago)
    {
        float alto = 1f;
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null) alto = sr.bounds.size.y;

        Vector3 cabeza = transform.position + Vector3.up * (alto * 0.6f);

        if (viajaGratis) TextoFlotante.Mostrar("Gratis", cabeza, colorPago, alto);
        else if (pago > 0) TextoFlotante.Mostrar($"+${pago}", cabeza, colorPago, alto);
        else TextoFlotante.Mostrar("$0", cabeza, colorSinPago, alto);
    }

    public int CalcularPago()
    {
        RefrescarEmocionPorPopularidad();

        if (viajaGratis) return 0;

        int pagoBase = 10;
        if (GameManager.Instance != null && GameManager.Instance.GameData != null)
            pagoBase = GameManager.Instance.GameData.pagoPorPasajero;

        switch (emocionActual)
        {
            case EmocionPasajero.Feliz:
                return UnityEngine.Random.value > 0.5f ? pagoBase * 2 : pagoBase;

            case EmocionPasajero.Neutro:
                return pagoBase;

            case EmocionPasajero.Enojado:
                return UnityEngine.Random.value > 0.5f ? 0 : pagoBase / 2;

            default:
                return pagoBase;
        }
    }

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

        if (anterior != emocionActual && inicializado && estado == EstadoPasajero.DentroDelAscensor)
        {
            MostrarEmoji();
        }
    }

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

        emoji.transform.localScale = escalaEmoji;

        FloatingEmoji floatScript = emoji.GetComponent<FloatingEmoji>();
        if (floatScript == null)
            floatScript = emoji.AddComponent<FloatingEmoji>();

        floatScript.Inicializar(0.5f, 1.5f, escalaEmoji);
    }

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
            float espera = UnityEngine.Random.Range(intervaloEmojiMin, intervaloEmojiMax);
            yield return new WaitForSeconds(espera);

            if (estado != EstadoPasajero.DentroDelAscensor) yield break;

            MostrarEmoji();
        }
    }
}