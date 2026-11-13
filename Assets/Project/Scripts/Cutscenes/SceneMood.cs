// Saatan Historia - Ambientación de un escenario del tráiler (neblina, luz ambiente, sol y fondo de la cámara).
// Va en la raíz de cada escenario; cuando el Timeline activa ese escenario, se aplica su ambientación.
using UnityEngine;
using UnityEngine.Rendering;

public class SceneMood : MonoBehaviour
{
    public Color niebla = new Color(0.55f, 0.66f, 0.58f);
    public bool conNiebla = true;
    public float inicioNiebla = 6f, finNiebla = 40f;
    public Color ambiente = new Color(0.4f, 0.45f, 0.4f);
    public float sol = 0.9f;
    public Color colorSol = new Color(1f, 0.96f, 0.85f);
    public Vector3 giroSol = new Vector3(50f, -30f, 0f);
    public float alcanceCamara = 200f;

    void OnEnable()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = ambiente;
        RenderSettings.fog = conNiebla;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = niebla;
        RenderSettings.fogStartDistance = inicioNiebla;
        RenderSettings.fogEndDistance = finNiebla;

        foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional)
            {
                l.enabled = sol > 0f;
                l.intensity = sol;
                l.color = colorSol;
                l.shadows = LightShadows.None;
                l.transform.rotation = Quaternion.Euler(giroSol);
            }

        var cam = Camera.main;
        if (cam == null) return;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = conNiebla ? niebla : Color.black;
        cam.farClipPlane = alcanceCamara;
    }
}
