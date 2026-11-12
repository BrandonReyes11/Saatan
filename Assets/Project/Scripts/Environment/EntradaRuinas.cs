// Saatan Historia - Línea de entrada a las ruinas: al cruzarla Iktan, se elimina el laberinto,
// se cierra la puerta detrás de él y empieza la pelea (se activa el objeto "pelea" y el evento alEntrar).
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider))]
public class EntradaRuinas : MonoBehaviour
{
    public GameObject laberinto;     // Selva_Laberinto
    public GameObject cierre;        // puerta + colisión que bloquea el regreso (empieza desactivada)
    public GameObject pelea;         // enemigos / lógica de la pelea (empieza desactivada)
    public UnityEvent alEntrar;

    bool usada;

    void Reset() => GetComponent<BoxCollider>().isTrigger = true;

    void OnTriggerEnter(Collider otro)
    {
        if (usada || otro.GetComponentInParent<ControlIktan>() == null) return;
        usada = true;

        // La carga por zonas ya no hace falta: su lista apunta al laberinto que se borra.
        foreach (var c in FindObjectsByType<CargaSelva>(FindObjectsSortMode.None)) c.enabled = false;
        if (laberinto != null) Destroy(laberinto);
        if (cierre != null) cierre.SetActive(true);
        if (pelea != null) pelea.SetActive(true);
        alEntrar.Invoke();
        Debug.Log("Iktan entró a las ruinas: ¡empieza la pelea!");
    }
}
