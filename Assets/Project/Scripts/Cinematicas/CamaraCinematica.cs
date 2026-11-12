// Saatan Historia - Cámara de las cinemáticas. Dos modos:
//  - Recorrido: pasa suavemente por 'puntos' mirando a 'objetivo' (o a 'seguir' si se indica).
//  - Seguimiento: si 'puntos' está vacío y hay 'seguir', va detrás del personaje con 'distancia'.
// Al final mira poco a poco a 'objetivo' y, si se indica, carga 'escenaSiguiente'.
using UnityEngine;
using UnityEngine.SceneManagement;

public class CamaraCinematica : MonoBehaviour
{
    public Vector3[] puntos;
    public Vector3 objetivo;
    public Transform seguir;
    public Vector3 distancia = new Vector3(-4f, 3f, -8f);   // en seguimiento, relativo al mundo
    public float duracion = 22f;
    public string escenaSiguiente = "";

    float t;

    void LateUpdate()
    {
        t += Time.deltaTime / duracion;
        float final = Mathf.SmoothStep(0f, 1f, (t - 0.8f) / 0.2f);   // últimos segundos: mira al objetivo

        if (puntos != null && puntos.Length >= 2)
        {
            float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)) * (puntos.Length - 1);
            int i = Mathf.Min((int)u, puntos.Length - 2);
            float f = u - i;
            // Catmull-Rom entre puntos para que la curva no tenga esquinas.
            Vector3 p0 = puntos[Mathf.Max(i - 1, 0)], p1 = puntos[i], p2 = puntos[i + 1], p3 = puntos[Mathf.Min(i + 2, puntos.Length - 1)];
            transform.position = 0.5f * (2f * p1 + (p2 - p0) * f + (2f * p0 - 5f * p1 + 4f * p2 - p3) * f * f + (3f * p1 - p0 - 3f * p2 + p3) * f * f * f);
        }
        else if (seguir != null)
            transform.position = Vector3.Lerp(transform.position, seguir.position + distancia, 1f - Mathf.Exp(-2f * Time.deltaTime));

        Vector3 mira = seguir != null ? Vector3.Lerp(seguir.position + Vector3.up, objetivo, final) : objetivo;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(mira - transform.position), 1f - Mathf.Exp(-5f * Time.deltaTime));

        if (t >= 1f && !string.IsNullOrEmpty(escenaSiguiente) && Application.CanStreamedLevelBeLoaded(escenaSiguiente))
        {
            enabled = false;
            SceneManager.LoadScene(escenaSiguiente);
        }
    }
}
