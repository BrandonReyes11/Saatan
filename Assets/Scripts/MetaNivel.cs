// Saatan Historia - Salida del nivel: al entrar Iktan, se carga la escena indicada.
// Colocar en: Assets/Scripts/MetaNivel.cs  (requiere un Collider marcado como Is Trigger)
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class MetaNivel : MonoBehaviour
{
    [Tooltip("Escena que se carga al llegar a la salida. Vacío = solo avisa en la consola.")]
    public string escenaSiguiente = "MenuPrincipal";

    bool usada;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider otro)
    {
        if (usada || otro.GetComponentInParent<ControlIktan>() == null) return;
        usada = true;

        Debug.Log("¡Iktan salió del laberinto!");

        if (string.IsNullOrEmpty(escenaSiguiente)) return;

        if (Application.CanStreamedLevelBeLoaded(escenaSiguiente))
            SceneManager.LoadScene(escenaSiguiente);
        else
            Debug.LogWarning("MetaNivel: la escena '" + escenaSiguiente + "' no está en la lista del Build.");
    }
}
