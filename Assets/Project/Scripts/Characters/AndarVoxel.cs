// Saatan Historia - Animación de caminar de los personajes voxel (piezas Leg_*, Arm_*, Tail).
// No mueve al personaje: lo mueve el Timeline. Este script mira cuánto avanzó en cada cuadro
// y balancea piernas y brazos en proporción; al detenerse, las piernas vuelven a juntarse.
using System.Collections.Generic;
using UnityEngine;

public class AndarVoxel : MonoBehaviour
{
    public float pasosPorMetro = 2.2f;
    public float angulo = 35f;

    readonly List<(Transform t, Quaternion baseRot, float fase)> partes = new List<(Transform, Quaternion, float)>();
    Transform cuerpo;
    Vector3 cuerpoBase, ultima;
    float ciclo;

    void Start()
    {
        if (transform.childCount > 0) { cuerpo = transform.GetChild(0); cuerpoBase = cuerpo.localPosition; }
        ultima = transform.position;
        // Lados opuestos se mueven al revés (en cuadrúpedos, en diagonal). La cola usa fase -1.
        var fases = new Dictionary<string, float>
        {
            { "Leg_L", 0f }, { "Leg_R", Mathf.PI }, { "Arm_L", Mathf.PI }, { "Arm_R", 0f },
            { "Leg_FL", 0f }, { "Leg_BR", 0f }, { "Leg_FR", Mathf.PI }, { "Leg_BL", Mathf.PI },
        };
        foreach (var t in GetComponentsInChildren<Transform>(true))
            if (fases.TryGetValue(t.name, out float f) || t.name == "Tail")
                partes.Add((t, t.localRotation, t.name == "Tail" ? -1f : f));
    }

    void LateUpdate()
    {
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 d = transform.position - ultima;
        ultima = transform.position;
        float velocidad = new Vector2(d.x, d.z).magnitude / dt;

        if (velocidad > 0.15f) ciclo += velocidad * pasosPorMetro * Mathf.PI * dt;
        else ciclo = Mathf.MoveTowards(ciclo, Mathf.Round(ciclo / Mathf.PI) * Mathf.PI, dt * 8f);

        foreach (var (t, baseRot, fase) in partes)
            t.localRotation = fase < 0f
                ? baseRot * Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 10f) * 25f)
                : baseRot * Quaternion.Euler(Mathf.Sin(ciclo + fase) * angulo, 0f, 0f);
        if (cuerpo != null) cuerpo.localPosition = cuerpoBase + Vector3.up * Mathf.Abs(Mathf.Sin(ciclo)) * 0.04f * transform.lossyScale.y;
    }
}
