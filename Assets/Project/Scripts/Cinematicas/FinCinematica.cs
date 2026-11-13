// Saatan Historia - Va junto al PlayableDirector (Timeline) de cada cinemática.
// Cuando el Timeline termina, o si el jugador la salta (Espacio, Enter, Esc, clic o A / Start del control),
// pasa a la siguiente escena con fundido.
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

[RequireComponent(typeof(PlayableDirector))]
public class FinCinematica : MonoBehaviour
{
    public string escenaSiguiente = "";
    public bool sePuedeSaltar = true;

    PlayableDirector director;
    bool terminada;

    void Start() => director = GetComponent<PlayableDirector>();

    void Update()
    {
        if (terminada || director == null) return;
        bool alFinal = director.playableAsset != null && director.time >= director.duration - 0.05;
        if (alFinal || (sePuedeSaltar && Saltar())) Terminar();
    }

    static bool Saltar()
    {
        var k = Keyboard.current;
        var m = Mouse.current;
        var g = Gamepad.current;
        return (k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.escapeKey.wasPressedThisFrame))
            || (m != null && m.leftButton.wasPressedThisFrame)
            || (g != null && (g.buttonSouth.wasPressedThisFrame || g.startButton.wasPressedThisFrame));
    }

    void Terminar()
    {
        terminada = true;
        if (!string.IsNullOrEmpty(escenaSiguiente)) Transicion.Cargar(escenaSiguiente);
    }
}
