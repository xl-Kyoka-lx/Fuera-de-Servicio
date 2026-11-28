using UnityEngine;

[CreateAssetMenu(fileName = "BibliotecaSonidos", menuName = "Ascensor/Biblioteca de sonidos")]
public class BibliotecaSonidos : ScriptableObject
{
    [Header("Interfaz")]
    public AudioClip clicBoton;
    [Range(0f, 1f)] public float volumenClic = 0.6f;

    [Header("Ascensor")]
    [Tooltip("Timbre que toca la cabina de abajo cuando avisa por su cuenta que está lista")]
    public AudioClip timbreCabinaAbajo;
    [Range(0f, 1f)] public float volumenTimbre = 0.8f;

    [Header("QTE")]
    public AudioClip agarrarBasura;
    [Tooltip("Suena al recoger el último círculo de la basura")]
    public AudioClip basura;
    [Tooltip("Polea trabada con el ascensor en movimiento, y frenado brusco o sin frenar")]
    public AudioClip ascensorDetenido;
    public AudioClip poleaDestrabada;
    public AudioClip puertaDestrabada;
    [Tooltip("Suena mientras el jugador engrasa (mantiene Espacio)")]
    public AudioClip engrasadoPolea;
    public AudioClip pestilloAcierto;
    public AudioClip pestilloFallo;
    [Range(0f, 1f)] public float volumenQte = 0.8f;

    [Header("Derrota")]
    [Tooltip("Música que suena en la pantalla de 'Ascensor fuera de servicio' al perder")]
    public AudioClip musicaFinal;
    [Range(0f, 1f)] public float volumenMusicaFinal = 0.8f;
    [Tooltip("Repetir la música hasta que el jugador vuelva al menú")]
    public bool repetirMusicaFinal = true;

    [Header("Gente (ambiente)")]
    public AudioClip[] genteNormal;
    public AudioClip[] genteEnojada;
    public AudioClip[] genteFeliz;
    [Range(0f, 1f)] public float volumenGente = 0.6f;
    [Tooltip("Segundos sin pasajeros antes de que suene la gente")]
    public float segundosSinPasajeros = 5f;
    [Tooltip("Desde esta popularidad (%) suena gente feliz")]
    public float popularidadFeliz = 80f;
    [Tooltip("Hasta esta popularidad (%) suena gente enojada")]
    public float popularidadEnojada = 40f;
}
