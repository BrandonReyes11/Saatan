// Saatan Historia - Transición entre escenas: fundido a negro, carga la escena y vuelve a aparecer la imagen.
// Uso desde cualquier script: SceneTransition.Cargar("NivelSelva");
// Se crea sola la primera vez y sobrevive a los cambios de escena (DontDestroyOnLoad).
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransition : MonoBehaviour
{
    public static float duracion = 0.7f;   // segundos del fundido (de ida y de vuelta)

    static SceneTransition instancia;
    CanvasGroup grupo;
    bool cargando;

    public static bool Cargando => instancia != null && instancia.cargando;

    public static void Cargar(string escena)
    {
        if (!Application.CanStreamedLevelBeLoaded(escena))
        {
            Debug.LogError("SceneTransition: la escena '" + escena + "' no está en la lista de escenas del Build (File > Build Profiles).");
            return;
        }
        var t = Instancia();
        if (!t.cargando) t.StartCoroutine(t.Ir(escena));
    }

    static SceneTransition Instancia()
    {
        if (instancia != null) return instancia;
        var go = new GameObject("SceneTransition");
        DontDestroyOnLoad(go);
        instancia = go.AddComponent<SceneTransition>();

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;   // por encima de cualquier menú
        instancia.grupo = go.AddComponent<CanvasGroup>();
        instancia.grupo.alpha = 0f;
        instancia.grupo.blocksRaycasts = false;

        var negro = new GameObject("Negro", typeof(RectTransform)).AddComponent<Image>();
        negro.transform.SetParent(go.transform, false);
        negro.color = Color.black;
        var rt = negro.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return instancia;
    }

    IEnumerator Ir(string escena)
    {
        cargando = true;
        grupo.blocksRaycasts = true;            // no se puede hacer clic durante la transición
        yield return Fundido(0f, 1f);
        Time.timeScale = 1f;
        var op = SceneManager.LoadSceneAsync(escena);
        while (!op.isDone) yield return null;
        yield return Fundido(1f, 0f);
        grupo.blocksRaycasts = false;
        cargando = false;
    }

    IEnumerator Fundido(float desde, float hasta)
    {
        for (float t = 0f; t < duracion; t += Time.unscaledDeltaTime)
        {
            grupo.alpha = Mathf.Lerp(desde, hasta, t / duracion);
            yield return null;
        }
        grupo.alpha = hasta;
    }
}
