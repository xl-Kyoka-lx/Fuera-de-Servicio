using System.Collections;
using UnityEngine;

public class FloatingEmoji : MonoBehaviour
{
    private float duracion = 1f;
    private float velocidadSubida = 1f;
    private Vector3 escalaBase = Vector3.one;

    public void Inicializar(float duracion, float velocidad, Vector3 escala)
    {
        this.duracion = duracion;
        this.velocidadSubida = velocidad;
        this.escalaBase = escala;
        StartCoroutine(AnimarEmoji());
    }

    private IEnumerator AnimarEmoji()
    {
        transform.localScale = Vector3.zero;
        float timer = 0f;
        while (timer < 0.2f)
        {
            timer += Time.deltaTime;
            float t = timer / 0.2f;
            transform.localScale = escalaBase * (Mathf.Sin(t * Mathf.PI) * 1.2f);
            yield return null;
        }
        transform.localScale = escalaBase;

        timer = 0f;
        while (timer < duracion)
        {
            timer += Time.deltaTime;
            transform.position += Vector3.up * velocidadSubida * Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}