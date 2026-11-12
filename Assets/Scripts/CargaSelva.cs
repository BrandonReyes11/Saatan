// Saatan Historia - Carga por zonas de la selva (va en la Main Camera; ConstructorSelva lo agrega solo).
// 1. Solo deja activas las zonas cercanas a Iktan; la neblina oculta el borde de lo cargado.
//    Activa o desactiva pocas zonas por cuadro para que no haya tirones.
// 2. Oculta las copas de los árboles que quedan entre la cámara y Iktan.
using System.Collections.Generic;
using UnityEngine;

public class CargaSelva : MonoBehaviour
{
    public Transform zonas;                 // Selva_Laberinto/Zonas
    public Transform jugador;               // vacío = se busca a Iktan
    public float radioCarga = 48f;          // metros alrededor de Iktan que se mantienen cargados
    public int cambiosPorCuadro = 2;

    readonly List<GameObject> lista = new List<GameObject>();
    readonly List<(Renderer r, Vector2 c, float radio)> copas = new List<(Renderer, Vector2, float)>();
    int siguiente;

    void Start()
    {
        if (jugador == null && FindFirstObjectByType<ControlIktan>() is ControlIktan c) jugador = c.transform;
        if (zonas == null || jugador == null) { enabled = false; return; }

        foreach (Transform z in zonas)
        {
            lista.Add(z.gameObject);
            foreach (Transform o in z)
                if (o.name.StartsWith("Arbol"))
                    foreach (var r in o.GetComponentsInChildren<Renderer>())
                        copas.Add((r, new Vector2(r.bounds.center.x, r.bounds.center.z), Mathf.Max(r.bounds.extents.x, r.bounds.extents.z) * 0.8f + 0.5f));
            z.gameObject.SetActive(Cerca(z));   // estado inicial de golpe, antes del primer cuadro
        }
    }

    bool Cerca(Transform z)
    {
        Vector3 d = z.position - jugador.position;
        return d.x * d.x + d.z * d.z < radioCarga * radioCarga;
    }

    void LateUpdate()
    {
        // Revisa todas las zonas, pero solo cambia unas pocas por cuadro.
        for (int i = 0, cambios = 0; i < lista.Count && cambios < cambiosPorCuadro; i++)
        {
            var z = lista[siguiente = (siguiente + 1) % lista.Count];
            bool activa = Cerca(z.transform);
            if (z.activeSelf != activa) { z.SetActive(activa); cambios++; }
        }

        // Copas entre la cámara y el jugador (en planta, distancia al segmento).
        Vector2 cam = new Vector2(transform.position.x, transform.position.z);
        Vector2 seg = new Vector2(jugador.position.x, jugador.position.z) - cam;
        float l2 = Mathf.Max(seg.sqrMagnitude, 0.0001f);
        foreach (var (r, c, radio) in copas)
        {
            if (!r.gameObject.activeInHierarchy) continue;
            float t = Mathf.Clamp01(Vector2.Dot(c - cam, seg) / l2);
            bool visible = Vector2.Distance(c, cam + seg * t) >= radio;
            if (r.enabled != visible) r.enabled = visible;
        }
    }
}
