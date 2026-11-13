// Saatan Historia - Herramientas del editor: jugador de prueba y fuego vivo.
// Requiere en Assets/Scripts: IktanController.cs, FollowCamera.cs y LivingFlame.cs
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class IktanTools
{
    [MenuItem("Saatan/Crear jugador de prueba (Iktan)")]
    public static void CrearJugador()
    {
        var viejo = GameObject.Find("Iktan_Jugador");
        if (viejo != null) Object.DestroyImmediate(viejo);

        var inicio = GameObject.Find("Inicio_Jugador");
        Vector3 pos = inicio != null ? inicio.transform.position : Vector3.zero;
        pos.y = 0.05f;
        if (inicio != null) inicio.SetActive(false);

        var raiz = new GameObject("Iktan_Jugador");
        raiz.transform.position = pos;

        var cc = raiz.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.4f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.stepOffset = 0.3f;

        var mat = MatJugador();

        var bola = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bola.name = "Bolita_Iktan";
        bola.transform.SetParent(raiz.transform, false);
        bola.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        bola.transform.localScale = Vector3.one;
        Object.DestroyImmediate(bola.GetComponent<Collider>());
        bola.GetComponent<Renderer>().sharedMaterial = mat;

        var frente = GameObject.CreatePrimitive(PrimitiveType.Cube);
        frente.name = "Frente";
        frente.transform.SetParent(raiz.transform, false);
        frente.transform.localPosition = new Vector3(0f, 0.6f, 0.5f);
        frente.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
        Object.DestroyImmediate(frente.GetComponent<Collider>());
        frente.GetComponent<Renderer>().sharedMaterial = mat;

        var luzGo = new GameObject("Luz_Iktan");
        luzGo.transform.SetParent(raiz.transform, false);
        luzGo.transform.localPosition = new Vector3(0f, 2.5f, 0f);
        var luz = luzGo.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.85f, 0.6f);
        luz.range = 9f;
        luz.intensity = 2.5f;
        luz.shadows = LightShadows.None;

        var control = raiz.AddComponent<IktanController>();
        control.visual = bola.transform;

        var cam = Camera.main;
        if (cam != null)
        {
            var seguir = cam.GetComponent<FollowCamera>();
            if (seguir == null) seguir = cam.gameObject.AddComponent<FollowCamera>();
            seguir.objetivo = raiz.transform;
            seguir.AjustarYa();
        }

        EditorSceneManager.MarkAllScenesDirty();
        Selection.activeGameObject = raiz;
        Debug.Log("Jugador creado. Pulsa Play, haz clic en la ventana Game y usa WASD. Ctrl o C para agacharte.");
    }

    [MenuItem("Saatan/Dar vida al fuego")]
    static void MenuFuego()
    {
        var raiz = GameObject.Find("Iglesia_Detallada");
        if (raiz == null)
        {
            EditorUtility.DisplayDialog("Saatan", "Primero construye la iglesia (Saatan > Construir iglesia detallada).", "OK");
            return;
        }
        int n = DarVidaAlFuego(raiz);
        Debug.Log("Llamas animadas: " + n);
    }

    // Agrega LivingFlame a cada llama y la une con su luz más cercana.
    public static int DarVidaAlFuego(GameObject raiz)
    {
        int n = 0;
        foreach (var t in raiz.GetComponentsInChildren<Transform>(true))
        {
            if (t.name != "Llama") continue;
            var viva = t.GetComponent<LivingFlame>();
            if (viva == null) viva = t.gameObject.AddComponent<LivingFlame>();

            Light mejor = null;
            float menor = float.MaxValue;
            if (t.parent != null)
            {
                foreach (var l in t.parent.GetComponentsInChildren<Light>())
                {
                    float d = (l.transform.position - t.position).sqrMagnitude;
                    if (d < menor) { menor = d; mejor = l; }
                }
            }
            viva.luz = mejor;
            n++;
        }
        return n;
    }

    static Material MatJugador()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Project/Niveles")) AssetDatabase.CreateFolder("Assets/Project", "Niveles");
        string ruta = "Assets/Project/Niveles/Mat_Jugador.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            m = new Material(sh);
            AssetDatabase.CreateAsset(m, ruta);
        }
        m.color = new Color(0.15f, 0.45f, 1f);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", new Color(0.1f, 0.3f, 0.8f));
        EditorUtility.SetDirty(m);
        return m;
    }
}