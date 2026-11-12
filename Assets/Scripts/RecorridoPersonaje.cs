// Saatan Historia - Hace caminar a un personaje de cinemática por unos puntos (va en el grupo del personaje).
// Mueve piernas, brazos y cola de los modelos voxel (piezas Leg_*, Arm_*, Tail) y da un pequeño rebote al andar.
using System.Collections.Generic;
using UnityEngine;

public class RecorridoPersonaje : MonoBehaviour
{
    public Vector3[] puntos;
    public float velocidad = 2f;      // m/s
    public float espera = 0f;         // segundos antes de empezar
    public float paso = 9f;           // velocidad del balanceo de piernas
    public float angulo = 35f;        // amplitud del balanceo

    readonly List<(Transform t, Quaternion baseRot, float fase)> partes = new List<(Transform, Quaternion, float)>();
    Transform cuerpo;
    Vector3 cuerpoBase;
    int tramo;
    float tiempo, ciclo;

    void Start()
    {
        if (puntos != null && puntos.Length > 0) transform.position = puntos[0];
        if (transform.childCount > 0) { cuerpo = transform.GetChild(0); cuerpoBase = cuerpo.localPosition; }

        // Fase de cada pieza: lados opuestos se mueven al revés (en cuadrúpedos, en diagonal).
        var fases = new Dictionary<string, float>
        {
            { "Leg_L", 0f }, { "Leg_R", Mathf.PI }, { "Arm_L", Mathf.PI }, { "Arm_R", 0f },
            { "Leg_FL", 0f }, { "Leg_BR", 0f }, { "Leg_FR", Mathf.PI }, { "Leg_BL", Mathf.PI },
        };
        foreach (var t in GetComponentsInChildren<Transform>(true))
            if (fases.TryGetValue(t.name, out float f) || t.name == "Tail")
                partes.Add((t, t.localRotation, t.name == "Tail" ? -1f : f));
    }

    void Update()
    {
        tiempo += Time.deltaTime;
        bool andando = puntos != null && tramo < puntos.Length - 1 && tiempo > espera;

        if (andando)
        {
            Vector3 meta = puntos[tramo + 1];
            Vector3 d = meta - transform.position;
            float avance = velocidad * Time.deltaTime;
            if (d.magnitude <= avance) { transform.position = meta; tramo++; }
            else transform.position += d.normalized * avance;

            Vector3 plano = new Vector3(d.x, 0f, d.z);
            if (plano.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(plano), 6f * Time.deltaTime);
            ciclo += Time.deltaTime * paso;
        }
        else ciclo = Mathf.MoveTowards(ciclo, Mathf.Round(ciclo / Mathf.PI) * Mathf.PI, Time.deltaTime * paso);   // se detiene con las piernas juntas

        foreach (var (t, baseRot, fase) in partes)
        {
            if (fase < 0f) t.localRotation = baseRot * Quaternion.Euler(0f, 0f, Mathf.Sin(tiempo * 10f) * 25f);   // la cola siempre se mueve
            else t.localRotation = baseRot * Quaternion.Euler(Mathf.Sin(ciclo + fase) * angulo, 0f, 0f);
        }
        if (cuerpo != null) cuerpo.localPosition = cuerpoBase + Vector3.up * Mathf.Abs(Mathf.Sin(ciclo)) * 0.04f * transform.lossyScale.y;
    }

    public bool Termino => puntos == null || tramo >= puntos.Length - 1;
}
