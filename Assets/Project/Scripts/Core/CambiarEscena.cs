// Saatan Historia - Cambio de escena.
//
// Uso (igual que en el video de apoyo):
//   1. Pon este script en un objeto de la escena (por ejemplo "GestorEscenas").
//   2. En el botón, en la lista On Click (), arrastra ese objeto.
//   3. Elige la función CambiarEscena > CargarEscena (string) y escribe el
//      nombre exacto de la escena, por ejemplo: Nivel_Selva
//
// IMPORTANTE: la escena debe estar agregada en File > Build Profiles
// (Scene List) o Unity no podrá cargarla.
using UnityEngine;
using UnityEngine.SceneManagement;

public class CambiarEscena : MonoBehaviour
{
    // Carga una escena por su nombre (ej. "Nivel_Selva").
    public void CargarEscena(string nombreEscena)
    {
        if (string.IsNullOrEmpty(nombreEscena))
        {
            Debug.LogWarning("CambiarEscena: no se indicó el nombre de la escena.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(nombreEscena))
        {
            Debug.LogError("CambiarEscena: la escena '" + nombreEscena +
                           "' no está en la lista de escenas del Build (File > Build Profiles).");
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(nombreEscena);
    }

    // Carga una escena por su número en la lista del Build (0 = la primera).
    public void CargarEscenaPorIndice(int indice)
    {
        if (indice < 0 || indice >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError("CambiarEscena: no existe una escena con el índice " + indice + " en el Build.");
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(indice);
    }

    // Carga la siguiente escena de la lista del Build.
    public void SiguienteEscena()
    {
        CargarEscenaPorIndice(SceneManager.GetActiveScene().buildIndex + 1);
    }

    // Cierra el juego (en el editor solo detiene el modo Play).
    public void SalirDelJuego()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
