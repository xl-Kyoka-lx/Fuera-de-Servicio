using UnityEngine;

public class AmbienteGente : MonoBehaviour
{
    private AudioSource fuente;
    private float sinPasajeros;
    private float proximaRevision;
    private bool hayPasajeros;

    private void Awake()
    {
        fuente = gameObject.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
    }

    private void Update()
    {
        var b = Sonidos.Biblioteca;
        if (b == null || PausaJuego.Pausado) return;

        if (Time.time >= proximaRevision)
        {
            proximaRevision = Time.time + 0.5f;
            hayPasajeros = FindAnyObjectByType<Pasajero>() != null;
        }

        if (hayPasajeros || fuente.isPlaying)
        {
            sinPasajeros = 0f;
            return;
        }

        sinPasajeros += Time.deltaTime;
        if (sinPasajeros >= b.segundosSinPasajeros)
        {
            sinPasajeros = 0f;
            Sonar(b);
        }
    }

    private void Sonar(BibliotecaSonidos b)
    {
        float popularidad = GameManager.Instance != null ? GameManager.Instance.GameData.contadorPopularidad : 50f;

        AudioClip[] grupo;
        string texto;
        if (popularidad >= b.popularidadFeliz) { grupo = b.genteFeliz; texto = "*gente conversando contenta*"; }
        else if (popularidad <= b.popularidadEnojada) { grupo = b.genteEnojada; texto = "*gente reclamando*"; }
        else { grupo = b.genteNormal; texto = "*murmullo de gente*"; }

        if (grupo == null || grupo.Length == 0) return;
        AudioClip clip = grupo[Random.Range(0, grupo.Length)];
        if (clip == null) return;

        float pan = Random.Range(-0.6f, 0.6f);
        fuente.panStereo = pan;
        fuente.clip = clip;
        fuente.volume = b.volumenGente;
        fuente.Play();

        IndicadoresSonido.Mostrar(texto, IndicadoresSonido.LadoDesdePan(pan), true, 4f);
    }
}
