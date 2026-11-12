// Saatan Historia - Cámara de la cinemática: recorre unos puntos con movimiento suave mirando un objetivo.
// Al terminar puede cargar otra escena (escenaSiguiente vacío = se queda en el último plano).
using UnityEngine;
using UnityEngine.SceneManagement;

public class CamaraCinematica : MonoBehaviour
{
    public Vector3[] puntos;            // recorrido de la cámara
    public Vector3 objetivo;            // a dónde mira
    public float duracion = 22f;        // segundos de todo el recorrido
    public string escenaSiguiente = "";

    float t;

    void LateUpdate()
    {
        if (puntos == null || puntos.Length < 2) return;
        t += Time.deltaTime / duracion;
        float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)) * (puntos.Length - 1);
        int i = Mathf.Min((int)u, puntos.Length - 2);
        float f = u - i;

        // Catmull-Rom entre puntos para que la curva no tenga esquinas.
        Vector3 p0 = puntos[Mathf.Max(i - 1, 0)], p1 = puntos[i], p2 = puntos[i + 1], p3 = puntos[Mathf.Min(i + 2, puntos.Length - 1)];
        transform.position = 0.5f * (2f * p1 + (p2 - p0) * f + (2f * p0 - 5f * p1 + 4f * p2 - p3) * f * f + (3f * p1 - p0 - 3f * p2 + p3) * f * f * f);
        transform.rotation = Quaternion.LookRotation(objetivo - transform.position);

        if (t >= 1f && !string.IsNullOrEmpty(escenaSiguiente) && Application.CanStreamedLevelBeLoaded(escenaSiguiente))
        {
            enabled = false;
            SceneManager.LoadScene(escenaSiguiente);
        }
    }
}
