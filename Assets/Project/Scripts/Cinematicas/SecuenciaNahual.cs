// Saatan Historia - Cinemática del Nahual derrotado: tiembla, se llena de luz, desaparece en un destello
// y en su lugar aparecen unos molletes.
using UnityEngine;

public class SecuenciaNahual : MonoBehaviour
{
    public Transform nahual;
    public Transform destello;      // esfera de luz (material aditivo)
    public Transform molletes;
    public Light luz;
    public ParticleSystem chispas;
    public float inicio = 2.5f;     // segundos antes de empezar a brillar
    public float brillo = 4f;       // duración del brillo antes del destello

    Vector3 posNahual, escMolletes;
    bool estallo;
    float t;

    void Start()
    {
        if (nahual != null) posNahual = nahual.localPosition;
        if (molletes != null) { escMolletes = molletes.localScale; molletes.localScale = Vector3.zero; }
        if (destello != null) destello.localScale = Vector3.zero;
        if (luz != null) luz.intensity = 0f;
    }

    void Update()
    {
        t += Time.deltaTime;
        float a = Mathf.Clamp01((t - inicio) / brillo);          // 0..1 mientras se llena de luz
        float d = t - inicio - brillo;                           // tiempo desde el destello

        if (d < 0f)
        {
            if (nahual != null) nahual.localPosition = posNahual + Random.insideUnitSphere * 0.06f * a * a;   // tiembla cada vez más
            if (destello != null) destello.localScale = Vector3.one * 3.2f * a * a;
            if (luz != null) luz.intensity = 14f * a * a;
            return;
        }

        if (!estallo)
        {
            estallo = true;
            if (nahual != null) nahual.gameObject.SetActive(false);
            if (chispas != null) chispas.Play();
        }

        // Destello: crece rápido y se apaga; los molletes aparecen con un pequeño rebote.
        float e = Mathf.Clamp01(d / 0.35f), f = Mathf.Clamp01((d - 0.35f) / 2f);
        if (destello != null) destello.localScale = Vector3.one * Mathf.Lerp(3.2f, 7f, e) * (1f - f);
        if (luz != null) luz.intensity = Mathf.Lerp(30f, 2f, f);
        float m = Mathf.Clamp01((d - 0.25f) / 1.2f);
        float rebote = 1f + Mathf.Sin(m * Mathf.PI) * 0.25f;
        if (molletes != null)
        {
            molletes.localScale = escMolletes * m * rebote;
            molletes.Rotate(0f, 20f * Time.deltaTime, 0f);
        }
    }
}
