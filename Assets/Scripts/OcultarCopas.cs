// Saatan Historia - Oculta las copas de los árboles que tapan a Iktan desde la cámara.
// Colocar en: Assets/Scripts/OcultarCopas.cs  (va en la Main Camera; ConstructorSelva lo agrega solo)
//
// Los árboles del laberinto son altos y sus copas cubren los pasillos. Cada cuadro se revisa
// qué árboles quedan entre la cámara y el jugador y se ocultan (siguen en la escena y su
// colisión no cambia; solo dejan de dibujarse).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class OcultarCopas : MonoBehaviour
{
    public Transform arboles;            // raíz con los árboles (Selva_Laberinto/Arboles)
    public Transform jugador;            // si está vacío se busca a Iktan
    public float margen = 0.5f;          // metros extra alrededor de cada copa
    public float alturaMinima = 3f;      // solo los modelos más altos que esto pueden ocultarse

    struct Copa
    {
        public Renderer[] renderers;
        public Vector2 centro;
        public float radio;
        public bool oculta;
    }

    readonly List<Copa> copas = new List<Copa>();

    void Start()
    {
        if (jugador == null)
        {
            var c = FindFirstObjectByType<ControlIktan>();
            if (c != null) jugador = c.transform;
        }

        if (arboles == null) return;

        foreach (Transform t in arboles)
        {
            var rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) continue;

            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            if (b.size.y < alturaMinima) continue;

            copas.Add(new Copa
            {
                renderers = rs,
                centro = new Vector2(b.center.x, b.center.z),
                radio = Mathf.Max(b.extents.x, b.extents.z) * 0.8f + margen,
                oculta = false
            });
        }
    }

    void LateUpdate()
    {
        if (jugador == null || copas.Count == 0) return;

        Vector2 cam = new Vector2(transform.position.x, transform.position.z);
        Vector2 jug = new Vector2(jugador.position.x, jugador.position.z);
        Vector2 seg = jug - cam;
        float largo2 = Mathf.Max(seg.sqrMagnitude, 0.0001f);

        for (int i = 0; i < copas.Count; i++)
        {
            Copa c = copas[i];

            // Punto del segmento cámara -> jugador más cercano al centro de la copa.
            float t = Mathf.Clamp01(Vector2.Dot(c.centro - cam, seg) / largo2);
            float d = Vector2.Distance(c.centro, cam + seg * t);
            bool tapa = d < c.radio;

            if (tapa == c.oculta) continue;

            c.oculta = tapa;
            var modo = tapa ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
            foreach (var r in c.renderers)
                if (r != null) r.shadowCastingMode = modo;
            copas[i] = c;
        }
    }
}
