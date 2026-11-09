// Saatan Historia - La cámara sigue a Iktan sin cambiar su ángulo.
// Colocar en: Assets/Scripts/SeguirCamara.cs
using UnityEngine;

public class SeguirCamara : MonoBehaviour
{
    public Transform objetivo;
    public Vector3 offset = new Vector3(0f, 20f, -9f);
    public float suavizado = 6f;

    public void AjustarYa()
    {
        if (objetivo != null) transform.position = objetivo.position + offset;
    }

    void LateUpdate()
    {
        if (objetivo == null) return;
        Vector3 meta = objetivo.position + offset;
        transform.position = Vector3.Lerp(transform.position, meta, 1f - Mathf.Exp(-suavizado * Time.deltaTime));
    }
}