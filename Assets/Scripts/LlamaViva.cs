// Saatan Historia - Llama que parpadea y se inclina cuando Iktan pasa cerca.
// Colocar en: Assets/Scripts/LlamaViva.cs
using UnityEngine;

public class LlamaViva : MonoBehaviour
{
    public Light luz;
    public float agitacion = 0.45f;     // qué tanto cambia el tamaño
    public float velocidad = 5f;        // qué tan rápido parpadea
    public float radioViento = 3f;      // distancia a la que Iktan mueve la llama

    Vector3 escalaBase, posBase;
    float intensidadBase, semilla;
    Transform jugador;

    void Start()
    {
        escalaBase = transform.localScale;
        posBase = transform.localPosition;
        semilla = Random.value * 100f;
        if (luz != null) intensidadBase = luz.intensity;
        var c = FindFirstObjectByType<ControlIktan>();
        if (c != null) jugador = c.transform;
    }

    void Update()
    {
        float t = Time.time * velocidad + semilla;
        float n = Mathf.PerlinNoise(t, 0.5f);
        float lado = Mathf.PerlinNoise(0.5f, t * 1.3f) - 0.5f;

        // Viento: la llama se aleja de Iktan cuando pasa cerca.
        Vector3 empuje = Vector3.zero;
        float viento = 0f;
        if (jugador != null)
        {
            Vector3 d = transform.position - jugador.position;
            d.y = 0f;
            float dist = d.magnitude;
            if (dist < radioViento && dist > 0.01f)
            {
                viento = 1f - dist / radioViento;
                empuje = d.normalized * viento;
            }
        }

        float k = 1f + (n - 0.5f) * agitacion * (1f + viento * 2f);
        transform.localScale = new Vector3(escalaBase.x * (1f - (k - 1f) * 0.5f), escalaBase.y * k, escalaBase.z * (1f - (k - 1f) * 0.5f));

        Vector3 desvio = new Vector3(lado * 0.06f, 0f, 0f) + empuje * 0.08f;
        if (transform.parent != null) desvio = transform.parent.InverseTransformVector(desvio);
        transform.localPosition = posBase + desvio;
        transform.localRotation = Quaternion.Euler(empuje.z * 35f, 0f, -empuje.x * 35f);

        if (luz != null) luz.intensity = intensidadBase * (0.7f + 0.6f * n) * (1f + viento * 0.3f);
    }
}