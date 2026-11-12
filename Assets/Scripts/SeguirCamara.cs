// Saatan Historia - Cámara en tercera persona detrás de Iktan (estilo Minecraft, un poco más alta).
// Ratón o palanca derecha: girar la cámara. Esc: soltar el cursor. Clic: volver a capturarlo.
// Si hay un muro entre Iktan y la cámara, la cámara se acerca para no atravesarlo.
using UnityEngine;
using UnityEngine.InputSystem;

public class SeguirCamara : MonoBehaviour
{
    public Transform objetivo;
    public float distancia = 4.5f;        // metros detrás de Iktan
    public float alturaMira = 1.6f;       // punto al que mira (sobre los pies de Iktan)
    public float inclinacion = 22f;       // grados hacia abajo (más = cámara más alta)
    public float inclinacionMin = 5f, inclinacionMax = 55f;
    public float sensibilidad = 0.12f;    // ratón
    public float giroControl = 140f;      // grados por segundo con la palanca
    public float suavizado = 12f;

    float giro;

    void Start()
    {
        AjustarYa();
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void AjustarYa()
    {
        if (objetivo == null) return;
        giro = objetivo.eulerAngles.y;
        Colocar(1f);
    }

    void LateUpdate()
    {
        if (objetivo == null) return;

        Vector2 d = Vector2.zero;
        if (Mouse.current != null)
        {
            if (Cursor.lockState == CursorLockMode.Locked) d = Mouse.current.delta.ReadValue() * sensibilidad;
            else if (Mouse.current.leftButton.wasPressedThisFrame) Cursor.lockState = CursorLockMode.Locked;
        }
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Cursor.lockState = CursorLockMode.None;
        if (Gamepad.current != null) d += Gamepad.current.rightStick.ReadValue() * giroControl * Time.deltaTime;

        giro += d.x;
        inclinacion = Mathf.Clamp(inclinacion - d.y, inclinacionMin, inclinacionMax);
        Colocar(1f - Mathf.Exp(-suavizado * Time.deltaTime));
    }

    void Colocar(float t)
    {
        Vector3 mira = objetivo.position + Vector3.up * alturaMira;
        Vector3 atras = Quaternion.Euler(inclinacion, giro, 0f) * Vector3.back;
        float dist = Physics.SphereCast(mira, 0.3f, atras, out RaycastHit hit, distancia, ~0, QueryTriggerInteraction.Ignore)
            ? hit.distance : distancia;

        if (dist < distancia) t = 1f;   // con un muro detrás, sin suavizado para no atravesarlo
        transform.position = Vector3.Lerp(transform.position, mira + atras * dist, t);
        transform.rotation = Quaternion.LookRotation(mira - transform.position);
    }
}
