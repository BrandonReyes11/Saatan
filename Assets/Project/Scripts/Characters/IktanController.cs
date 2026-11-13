// Saatan Historia - Movimiento de Iktan (sistema de Input nuevo de Unity 6).
// Controles: WASD o flechas, o palanca izquierda del control (relativo a la cámara).
// Cuclillas (sigilo): Ctrl izquierdo, C, o botón B / círculo del control.
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class IktanController : MonoBehaviour
{
    public float velocidad = 4.5f;
    public float velocidadCuclillas = 2f;
    public float gravedad = -20f;
    public float giro = 12f;
    public Transform visual;          // lo que se aplasta al agacharse

    public bool EnCuclillas { get; private set; }
    public bool EnMovimiento { get; private set; }

    CharacterController cc;
    float velY;
    float escalaBaseY = 1f;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (visual != null) escalaBaseY = visual.localScale.y;
    }

    void Update()
    {
        Vector2 m = Vector2.zero;
        bool agachado = false;

        var k = Keyboard.current;
        if (k != null)
        {
            if (k.wKey.isPressed || k.upArrowKey.isPressed) m.y += 1f;
            if (k.sKey.isPressed || k.downArrowKey.isPressed) m.y -= 1f;
            if (k.dKey.isPressed || k.rightArrowKey.isPressed) m.x += 1f;
            if (k.aKey.isPressed || k.leftArrowKey.isPressed) m.x -= 1f;
            if (k.leftCtrlKey.isPressed || k.cKey.isPressed) agachado = true;
        }

        var g = Gamepad.current;
        if (g != null)
        {
            Vector2 s = g.leftStick.ReadValue();
            if (s.sqrMagnitude > 0.04f) m = s;
            if (g.buttonEast.isPressed) agachado = true;
        }

        if (m.sqrMagnitude > 1f) m.Normalize();
        EnCuclillas = agachado;
        EnMovimiento = m.sqrMagnitude > 0.01f;

        // Movimiento relativo a la cámara: W siempre avanza hacia donde mira la cámara.
        Vector3 dir = new Vector3(m.x, 0f, m.y);
        var cam = Camera.main;
        if (cam != null)
        {
            Vector3 frente = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
            dir = frente * m.y + Vector3.Cross(Vector3.up, frente) * m.x;
        }
        float v = agachado ? velocidadCuclillas : velocidad;

        if (cc.isGrounded && velY < 0f) velY = -2f;
        velY += gravedad * Time.deltaTime;
        cc.Move((dir * v + Vector3.up * velY) * Time.deltaTime);

        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), giro * Time.deltaTime);

        if (visual != null)
        {
            Vector3 e = visual.localScale;
            e.y = Mathf.Lerp(e.y, agachado ? escalaBaseY * 0.6f : escalaBaseY, 12f * Time.deltaTime);
            visual.localScale = e;
        }
    }
}