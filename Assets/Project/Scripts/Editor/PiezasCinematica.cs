// Saatan Historia - Piezas compartidas para construir las cinemáticas (Saatan > Escenas > Crear ...).
// Personajes de Prefabs/Characters, comida (molletes, mandarinas), velas, trompos y juguetes hechos con cubos.
// Los objetos recogibles (Mollete, FotoIktan) se guardan como prefabs en Prefabs/Items.
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class PiezasCinematica
{
    public const string CARPETA = "Assets/Project/Niveles/Cinematica";
    const string PERSONAJES = "Assets/Project/Prefabs/Characters/";
    public const string ITEMS = "Assets/Project/Prefabs/Items/";
    static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
    public static System.Random rnd = new System.Random(5);

    // Crea una escena vacía (cámara + luz) lista para construir; null si el usuario cancela.
    public static Transform NuevaEscena(string nombreRaiz)
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return null;
        Carpetas();
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        cache.Clear();
        rnd = new System.Random(5);
        return new GameObject(nombreRaiz).transform;
    }

    static void Carpetas()
    {
        foreach (var c in new[] { "Assets/Project/Niveles", CARPETA, CARPETA + "/Piezas", ITEMS.TrimEnd('/') })
            if (!AssetDatabase.IsValidFolder(c)) AssetDatabase.CreateFolder(Path.GetDirectoryName(c).Replace('\\', '/'), Path.GetFileName(c));
    }

    public static void GuardarEscena(string ruta)
    {
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ruta);
        var lista = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!lista.Exists(s => s.path == ruta)) lista.Add(new EditorBuildSettingsScene(ruta, true));
        EditorBuildSettings.scenes = lista.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("Cinemática creada: " + ruta);
    }

    public static A Guardar<A>(A asset, string ruta) where A : Object
    {
        AssetDatabase.DeleteAsset(ruta);
        AssetDatabase.CreateAsset(asset, ruta);
        return asset;
    }

    // ---------------- Materiales ----------------

    public static Material Mat(string nombre, Color c, float brillo = 0f)
    {
        if (cache.TryGetValue(nombre, out var m)) return m;
        m = MatAsset(nombre, "Universal Render Pipeline/Lit");
        m.color = c;
        m.SetFloat("_Smoothness", 0.15f);
        if (brillo > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * brillo);
        }
        EditorUtility.SetDirty(m);
        return cache[nombre] = m;
    }

    // Material guardado en Niveles/Cinematica/Piezas; si ya existe se reutiliza (mismo GUID).
    static Material MatAsset(string nombre, string shader)
    {
        string ruta = CARPETA + "/Piezas/Mat_" + nombre + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null) AssetDatabase.CreateAsset(m = new Material(Shader.Find(shader)), ruta);
        else m.shader = Shader.Find(shader);
        return m;
    }

    // Transparente aditivo (destellos, halos de luz).
    public static Material Aditivo(string nombre, Color c)
    {
        if (cache.TryGetValue(nombre, out var m)) return m;
        m = MatAsset(nombre, "Universal Render Pipeline/Particles/Unlit");
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 2f);
        m.SetFloat("_ZWrite", 0f);
        m.SetFloat("_SrcBlend", (float)BlendMode.One);
        m.SetFloat("_DstBlend", (float)BlendMode.One);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.SetOverrideTag("RenderType", "Transparent");
        m.renderQueue = (int)RenderQueue.Transparent;
        m.SetColor("_BaseColor", c);
        EditorUtility.SetDirty(m);
        return cache[nombre] = m;
    }

    // ---------------- Formas ----------------

    public static Transform Grupo(Transform padre, string nombre, Vector3 pos, float giro = 0f)
    {
        var t = new GameObject(nombre).transform;
        t.SetParent(padre, false);
        t.localPosition = pos;
        t.localRotation = Quaternion.Euler(0f, giro, 0f);
        return t;
    }

    public static Transform Forma(PrimitiveType tipo, Transform padre, Vector3 pos, Vector3 esc, Material m, bool colision = false)
    {
        var g = GameObject.CreatePrimitive(tipo);
        if (!colision) Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.SetParent(padre, false);
        g.transform.localPosition = pos;
        g.transform.localScale = esc;
        g.GetComponent<Renderer>().sharedMaterial = m;
        return g.transform;
    }

    public static Transform Cubo(Transform padre, Vector3 pos, Vector3 esc, Material m, bool colision = false) =>
        Forma(PrimitiveType.Cube, padre, pos, esc, m, colision);

    public static float Azar(float a) => ((float)rnd.NextDouble() * 2f - 1f) * a;

    // ---------------- Ofrenda: comida, velas, flores y juguetes (medidas reales en metros) ----------------

    public static void Vela(Transform padre, Vector3 pos, float alto = 0.25f, bool luz = true)
    {
        var g = Grupo(padre, "Vela", pos);
        Forma(PrimitiveType.Cylinder, g, Vector3.up * alto / 2f, new Vector3(0.07f, alto / 2f, 0.07f), Mat("Cera", new Color(0.97f, 0.93f, 0.8f)));
        var llama = Forma(PrimitiveType.Sphere, g, Vector3.up * (alto + 0.045f), new Vector3(0.04f, 0.07f, 0.04f), Mat("Llama", new Color(1f, 0.6f, 0.15f), 4f));
        llama.name = "Llama";
        if (!luz) return;
        var l = new GameObject("Luz").AddComponent<Light>();
        l.transform.SetParent(g, false);
        l.transform.localPosition = Vector3.up * (alto + 0.2f);
        l.type = LightType.Point;
        l.color = new Color(1f, 0.68f, 0.32f);
        l.range = 3.5f;
        l.intensity = 1.3f;
        l.shadows = LightShadows.None;
        var viva = llama.gameObject.AddComponent<LlamaViva>();
        viva.luz = l;
    }

    // Mollete: bolillo partido con frijoles, queso gratinado y pico de gallo.
    public static void Mollete(Transform padre, Vector3 pos, float giro)
    {
        var g = Grupo(padre, "Mollete", pos, giro);
        Cubo(g, new Vector3(0f, 0.03f, 0f), new Vector3(0.2f, 0.06f, 0.1f), Mat("Pan", new Color(0.82f, 0.56f, 0.27f)));
        Cubo(g, new Vector3(0f, 0.065f, 0f), new Vector3(0.18f, 0.015f, 0.085f), Mat("Frijol", new Color(0.28f, 0.14f, 0.08f)));
        Cubo(g, new Vector3(0f, 0.077f, 0f), new Vector3(0.17f, 0.012f, 0.08f), Mat("Queso", new Color(1f, 0.86f, 0.45f)));
        string[] pico = { "Jitomate", "Cilantro", "Cebolla" };
        Color[] colores = { new Color(0.85f, 0.12f, 0.1f), new Color(0.2f, 0.6f, 0.15f), new Color(0.95f, 0.95f, 0.9f) };
        for (int i = 0; i < 5; i++)
        {
            int k = rnd.Next(3);
            Cubo(g, new Vector3(Azar(0.07f), 0.088f, Azar(0.03f)), Vector3.one * 0.018f, Mat(pico[k], colores[k]));
        }
    }

    public static Transform PlatoMolletes(Transform padre, Vector3 pos, int n = 4)
    {
        var g = Grupo(padre, "Plato_Molletes", pos);
        Forma(PrimitiveType.Cylinder, g, Vector3.up * 0.01f, new Vector3(0.42f, 0.01f, 0.42f), Mat("Barro", new Color(0.62f, 0.3f, 0.15f)));
        for (int i = 0; i < n; i++)
        {
            float a = i * Mathf.PI * 2f / n;
            Item(PrefabMollete(), g, new Vector3(Mathf.Cos(a) * 0.1f, 0.02f + (i % 2) * 0.02f, Mathf.Sin(a) * 0.08f), i * 47f);
        }
        return g;
    }

    // ---------------- Objetos recogibles (Prefabs/Items) ----------------

    public static Transform Item(GameObject prefab, Transform padre, Vector3 pos, float giro = 0f)
    {
        var t = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, padre)).transform;
        t.localPosition = pos;
        t.localRotation = Quaternion.Euler(0f, giro, 0f);
        return t;
    }

    public static GameObject PrefabMollete() => PrefabItem("Mollete", g => Mollete(g, Vector3.zero, 0f), new Vector3(0.3f, 0.2f, 0.2f));

    // Foto de Iktan en un marco de madera. La imagen se toma una vez del modelo de Iktan (FotoIktan.png).
    public static GameObject PrefabFotoIktan() => PrefabItem("FotoIktan", g =>
    {
        var marco = Mat("Marco", new Color(0.55f, 0.35f, 0.12f));
        Cubo(g, new Vector3(0f, 0.33f, 0.02f), new Vector3(0.5f, 0.62f, 0.04f), marco);
        Cubo(g, new Vector3(0f, 0.06f, 0.08f), new Vector3(0.06f, 0.12f, 0.12f), marco).localRotation = Quaternion.Euler(-20f, 0f, 0f);
        var foto = MatAsset("FotoIktan", "Universal Render Pipeline/Lit");
        var tex = TexturaFoto();
        foto.SetTexture("_BaseMap", tex);
        foto.EnableKeyword("_EMISSION");
        foto.SetTexture("_EmissionMap", tex);
        foto.SetColor("_EmissionColor", new Color(0.35f, 0.33f, 0.3f));   // se distingue aun con poca luz
        EditorUtility.SetDirty(foto);
        Cubo(g, new Vector3(0f, 0.33f, -0.001f), new Vector3(0.42f, 0.52f, 0.01f), foto);
    }, new Vector3(0.55f, 0.7f, 0.25f));

    static GameObject PrefabItem(string nombre, System.Action<Transform> armar, Vector3 tamColision)
    {
        string ruta = ITEMS + nombre + ".prefab";
        var existente = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
        if (existente != null) return existente;

        Carpetas();
        var g = new GameObject(nombre).transform;
        armar(g);
        var c = g.gameObject.AddComponent<BoxCollider>();
        c.isTrigger = true;
        c.center = Vector3.up * tamColision.y / 2f;
        c.size = tamColision;
        g.gameObject.AddComponent<Recogible>().objeto = nombre;
        var pf = PrefabUtility.SaveAsPrefabAsset(g.gameObject, ruta);
        Object.DestroyImmediate(g.gameObject);
        return pf;
    }

    // Retrato de Iktan: modelo + luz + cámara en un rincón apartado, render a textura y se guarda como PNG.
    static Texture2D TexturaFoto()
    {
        string ruta = ITEMS + "FotoIktan.png";
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        if (tex != null) return tex;

        var estudio = new GameObject("Estudio_Foto").transform;
        estudio.position = new Vector3(0f, -500f, 0f);
        Personaje("IktanVoxel", estudio, Vector3.zero, 180f, 1.4f);
        var l = new GameObject("Luz").AddComponent<Light>();
        l.transform.SetParent(estudio, false);
        l.transform.localPosition = new Vector3(0.6f, 1.8f, -1.5f);
        l.type = LightType.Point;
        l.range = 6f;
        l.intensity = 3f;
        l.color = new Color(1f, 0.9f, 0.75f);
        var cam = new GameObject("Camara").AddComponent<Camera>();
        cam.transform.SetParent(estudio, false);
        cam.transform.localPosition = new Vector3(0f, 1.05f, -1.6f);
        cam.transform.LookAt(estudio.position + Vector3.up);
        cam.fieldOfView = 32f;
        cam.farClipPlane = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.55f, 0.45f, 0.32f);   // fondo sepia de foto antigua
        var rt = new RenderTexture(256, 320, 24);
        cam.targetTexture = rt;
        try
        {
            var pedido = new RenderPipeline.StandardRequest { destination = rt };
            if (RenderPipeline.SupportsRenderRequest(cam, pedido)) RenderPipeline.SubmitRenderRequest(cam, pedido);
            else cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var img = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            img.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            img.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(ruta, img.EncodeToPNG());
            Object.DestroyImmediate(img);
        }
        catch (System.Exception e) { Debug.LogWarning("No se pudo tomar la foto de Iktan: " + e.Message); }
        finally
        {
            cam.targetTexture = null;
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(estudio.gameObject);
        }
        AssetDatabase.ImportAsset(ruta);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
    }

    public static void Mandarina(Transform padre, Vector3 pos)
    {
        var g = Grupo(padre, "Mandarina", pos, rnd.Next(360));
        Forma(PrimitiveType.Sphere, g, Vector3.up * 0.032f, new Vector3(0.075f, 0.062f, 0.075f), Mat("Mandarina", new Color(1f, 0.5f, 0.05f)));
        Cubo(g, new Vector3(0.012f, 0.066f, 0f), new Vector3(0.025f, 0.006f, 0.012f), Mat("Hoja", new Color(0.2f, 0.5f, 0.15f)));
    }

    // Trompo de madera: discos que se angostan hacia la punta, franja de color y cuerda enrollada.
    public static void Trompo(Transform padre, Vector3 pos, Color franja, bool acostado = false)
    {
        var g = Grupo(padre, "Trompo", pos, rnd.Next(360));
        if (acostado) g.localRotation *= Quaternion.Euler(0f, 0f, 75f);
        var madera = Mat("MaderaTrompo", new Color(0.72f, 0.48f, 0.25f));
        float[] radios = { 0.012f, 0.03f, 0.045f, 0.055f, 0.05f };
        for (int i = 0; i < radios.Length; i++)
            Forma(PrimitiveType.Cylinder, g, Vector3.up * (0.012f + i * 0.016f), new Vector3(radios[i] * 2f, 0.008f, radios[i] * 2f),
                i == 3 ? Mat("Franja_" + ColorUtility.ToHtmlStringRGB(franja), franja) : madera);
        Forma(PrimitiveType.Cylinder, g, Vector3.up * 0.1f, new Vector3(0.02f, 0.012f, 0.02f), madera);
        Cubo(g, Vector3.up * 0.004f, Vector3.one * 0.008f, Mat("Hierro", new Color(0.2f, 0.2f, 0.22f)));
    }

    public static void Carrito(Transform padre, Vector3 pos, float giro)
    {
        var g = Grupo(padre, "Carrito", pos, giro);
        Cubo(g, new Vector3(0f, 0.045f, 0f), new Vector3(0.1f, 0.04f, 0.2f), Mat("CarroRojo", new Color(0.75f, 0.12f, 0.1f)));
        Cubo(g, new Vector3(0f, 0.08f, -0.02f), new Vector3(0.08f, 0.035f, 0.09f), Mat("MaderaTrompo", new Color(0.72f, 0.48f, 0.25f)));
        foreach (float x in new[] { -0.055f, 0.055f })
            foreach (float z in new[] { -0.065f, 0.065f })
                Forma(PrimitiveType.Cylinder, g, new Vector3(x, 0.025f, z), new Vector3(0.05f, 0.008f, 0.05f), Mat("Hierro", new Color(0.2f, 0.2f, 0.22f))).localRotation = Quaternion.Euler(0f, 0f, 90f);
    }

    public static void Balero(Transform padre, Vector3 pos)
    {
        var g = Grupo(padre, "Balero", pos, rnd.Next(360));
        Forma(PrimitiveType.Cylinder, g, Vector3.up * 0.07f, new Vector3(0.018f, 0.07f, 0.018f), Mat("MaderaTrompo", new Color(0.72f, 0.48f, 0.25f)));
        Forma(PrimitiveType.Cylinder, g, new Vector3(0.09f, 0.035f, 0f), new Vector3(0.06f, 0.035f, 0.06f), Mat("CarroRojo", new Color(0.75f, 0.12f, 0.1f))).localRotation = Quaternion.Euler(0f, 0f, 90f);
        Cubo(g, new Vector3(0.045f, 0.13f, 0f), new Vector3(0.09f, 0.003f, 0.003f), Mat("Cuerda", new Color(0.9f, 0.85f, 0.7f)));
    }

    public static void Pelota(Transform padre, Vector3 pos)
    {
        var g = Grupo(padre, "Pelota", pos);
        Forma(PrimitiveType.Sphere, g, Vector3.up * 0.06f, Vector3.one * 0.12f, Mat("PelotaAzul", new Color(0.15f, 0.3f, 0.8f)));
        Forma(PrimitiveType.Sphere, g, Vector3.up * 0.06f, new Vector3(0.122f, 0.03f, 0.122f), Mat("Amarillo", new Color(1f, 0.8f, 0.1f)));
    }

    public static void Yoyo(Transform padre, Vector3 pos)
    {
        var g = Grupo(padre, "Yoyo", pos, rnd.Next(360));
        foreach (float x in new[] { -0.012f, 0.012f })
            Forma(PrimitiveType.Cylinder, g, new Vector3(x, 0.035f, 0f), new Vector3(0.07f, 0.008f, 0.07f), Mat("Verde", new Color(0.15f, 0.6f, 0.3f))).localRotation = Quaternion.Euler(0f, 0f, 90f);
    }

    // Cempasúchil: flor de cubos naranjas.
    public static void Flor(Transform padre, Vector3 pos, float tam = 0.08f)
    {
        var g = Grupo(padre, "Cempasuchil", pos, rnd.Next(360));
        Cubo(g, Vector3.up * tam / 2f, Vector3.one * tam, Mat("Cempasuchil", new Color(1f, 0.55f, 0.05f)));
        Cubo(g, Vector3.up * tam, Vector3.one * tam * 0.6f, Mat("Cempasuchil2", new Color(1f, 0.75f, 0.1f)));
    }

    // ---------------- Personajes ----------------

    // Personaje de Art/PersonajesCinematica, escalado a 'altura' metros, apoyado en el suelo y mirando a +Z del grupo.
    // Los FBX vienen de Blender (frente hacia -Y): el giro se calcula con la cabeza y la cola de Abbi o del Nahual.
    public static Transform Personaje(string archivo, Transform padre, Vector3 pos, float giro, float altura)
    {
        string ruta = PERSONAJES + archivo + ".fbx";
        if (AssetImporter.GetAtPath(ruta) is ModelImporter imp && (imp.importCameras || imp.importLights))
        {
            imp.importCameras = false;   // los FBX traen cámara y luz de Blender
            imp.importLights = false;
            imp.SaveAndReimport();
        }
        var pf = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
        var g = Grupo(padre, archivo.Replace("Voxel", ""), Vector3.zero);
        if (pf == null) { Debug.LogWarning("No se encontró " + ruta); return g; }

        var m = ((GameObject)PrefabUtility.InstantiatePrefab(pf, g)).transform;
        m.localPosition = Vector3.zero;
        m.rotation = Quaternion.Euler(0f, GiroFrente(m, archivo), 0f) * m.rotation;
        Bounds b = Limites(m);
        m.localScale *= altura / Mathf.Max(b.size.y, 0.001f);
        b = Limites(m);
        m.position -= new Vector3(b.center.x, b.min.y, b.center.z);   // centrado y con los pies en y = 0

        g.localPosition = pos;
        g.localRotation = Quaternion.Euler(0f, giro, 0f);
        return g;
    }

    static float giroGuardado = float.NaN;
    static float GiroFrente(Transform m, string archivo)
    {
        Transform cabeza = Buscar(m, "Head"), cola = Buscar(m, "Tail");
        if (cabeza != null && cola != null)
        {
            Vector3 d = cabeza.position - cola.position;
            d.y = 0f;
            if (d.sqrMagnitude > 1e-6f) return giroGuardado = Vector3.SignedAngle(d, Vector3.forward, Vector3.up);
        }
        if (float.IsNaN(giroGuardado) && archivo != "AbbiVoxel")
        {
            var abbi = AssetDatabase.LoadAssetAtPath<GameObject>(PERSONAJES + "AbbiVoxel.fbx");
            if (abbi != null)
            {
                var tmp = (GameObject)PrefabUtility.InstantiatePrefab(abbi);
                GiroFrente(tmp.transform, "AbbiVoxel");
                Object.DestroyImmediate(tmp);
            }
        }
        return float.IsNaN(giroGuardado) ? 0f : giroGuardado;
    }

    public static Transform Buscar(Transform t, string nombre)
    {
        foreach (var c in t.GetComponentsInChildren<Transform>(true)) if (c.name == nombre) return c;
        return null;
    }

    static Bounds Limites(Transform t)
    {
        var rs = t.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(t.position, Vector3.one);
        Bounds b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }
}
