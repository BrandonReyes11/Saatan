// Saatan Historia - Cambio de escena (con fundido a negro, ver SceneTransition).
//
// Uso (igual que en el video de apoyo):
//   1. Pon este script en un objeto de la escena (por ejemplo "GestorEscenas").
//   2. En el botón, en la lista On Click (), arrastra ese objeto.
//   3. Elige SceneChanger > CargarEscena (string) y escribe el nombre de la escena, p. ej. NivelSelva.
//      O llena el campo "escena" y elige SceneChanger > Cargar ().
//   Desde código: GetComponent<SceneChanger>().Cargar();
//
// La escena debe estar en File > Build Profiles (Scene List). Orden del juego:
//   MenuPrincipal > NivelSelva > MolletesCinematica > CinematicaIglesia > NivelIglesia > AltarCInematica
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    [Tooltip("Escena que carga Cargar(). Vacío = la siguiente de la lista del Build.")]
    public string escena = "";

    public void Cargar()
    {
        if (string.IsNullOrEmpty(escena)) SiguienteEscena();
        else CargarEscena(escena);
    }

    public void CargarEscena(string nombreEscena)
    {
        if (string.IsNullOrEmpty(nombreEscena)) { Debug.LogWarning("SceneChanger: no se indicó el nombre de la escena."); return; }
        SceneTransition.Cargar(nombreEscena);
    }

    public void CargarEscenaPorIndice(int indice)
    {
        if (indice < 0 || indice >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError("SceneChanger: no existe una escena con el índice " + indice + " en el Build.");
            return;
        }
        SceneTransition.Cargar(System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(indice)));
    }

    public void SiguienteEscena() => CargarEscenaPorIndice(SceneManager.GetActiveScene().buildIndex + 1);

    public void SalirDelJuego()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
