// Saatan Historia - Carga por zonas y neblina de la selva (va en la Main Camera; JungleBuilder lo agrega solo).
// Solo deja activas las zonas cercanas a Iktan, cambiando pocas por cuadro para que no haya tirones.
// La neblina se aplica al iniciar, así siempre se ve y tapa el borde de lo que está cargado.
using System.Collections.Generic;
using UnityEngine;

public class JungleStreamer : MonoBehaviour
{
    public Transform zonas;                 // Selva_Laberinto/Zonas
    public Transform jugador;               // vacío = se busca a Iktan
    public float radioCarga = 50f;          // metros alrededor de Iktan que se mantienen cargados
    public int cambiosPorCuadro = 2;

    [Header("Neblina")]
    public Color colorNiebla = new Color(0.55f, 0.66f, 0.58f);
    public float inicioNiebla = 6f;         // metros desde la cámara
    public float finNiebla = 38f;           // aquí ya no se ve nada (y la cámara deja de dibujar)

    readonly List<GameObject> lista = new List<GameObject>();
    int siguiente;

    void Start()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = colorNiebla;
        RenderSettings.fogStartDistance = inicioNiebla;
        RenderSettings.fogEndDistance = finNiebla;
        if (TryGetComponent(out Camera cam))
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = colorNiebla;
            cam.farClipPlane = finNiebla + 2f;
        }

        if (jugador == null && FindFirstObjectByType<IktanController>() is IktanController c) jugador = c.transform;
        if (zonas == null || jugador == null) { enabled = false; return; }

        foreach (Transform z in zonas)
        {
            lista.Add(z.gameObject);
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
        // Revisa las zonas en orden, pero solo cambia unas pocas por cuadro.
        for (int i = 0, cambios = 0; i < lista.Count && cambios < cambiosPorCuadro; i++)
        {
            var z = lista[siguiente = (siguiente + 1) % lista.Count];
            bool activa = Cerca(z.transform);
            if (z.activeSelf != activa) { z.SetActive(activa); cambios++; }
        }
    }
}
