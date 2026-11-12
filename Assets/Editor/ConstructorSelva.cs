// Saatan Historia - Nivel I: La selva (laberinto).
// Construye el laberinto desde Assets/Mapas/selva_laberinto.png (1 px = 1 m) con los FBX de Assets/Art/LOD2:
//   negro = borde (árboles + arbustos, con colisión) | verde oscuro = bosque cerrado | verde = hierba alta
//   blanco = pasillo (decoración pequeña) | azul = inicio | rosa = salida
// Optimización: modelos LOD2, suelo en un solo quad con textura, colisiones con cajas,
// y todo agrupado en zonas de ZONA x ZONA m que CargaSelva activa solo cerca del jugador (con neblina).
// Menú: Saatan > Construir selva (Laberinto)   |   Saatan > Escenas > Reconstruir NivelSelva

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class ConstructorSelva
{
    public const string MAPA = "Assets/Mapas/selva_laberinto.png";
    const string ARTE = "Assets/Art/LOD2/", CARPETA = "Assets/Niveles/Selva", RAIZ = "Selva_Laberinto";
    const int ZONA = 16, SEMILLA = 2026;
    const float ALTO_MURO = 4f;

    enum T { Vacio, Piso, Borde, Pasto, Bosque, Inicio, Salida }

    static readonly Dictionary<Color32, T> paleta = new Dictionary<Color32, T>
    {
        { new Color32(255, 255, 255, 255), T.Piso },   { new Color32(0, 0, 0, 255), T.Borde },
        { new Color32(0, 170, 0, 255), T.Pasto },      { new Color32(0, 90, 0, 255), T.Bosque },
        { new Color32(0, 100, 255, 255), T.Inicio },   { new Color32(255, 105, 180, 255), T.Salida },
    };

    // Modelo: archivo, altura final mínima y máxima (m), peso. Iktan mide ~1.8 m.
    class Modelo { public string archivo; public float min, max, peso, alto; public GameObject prefab; }
    static Modelo[] Lista(params (string, float, float, float)[] l) =>
        System.Array.ConvertAll(l, m => new Modelo { archivo = m.Item1, min = m.Item2, max = m.Item3, peso = m.Item4 });

    static readonly Modelo[] arboles = Lista(("acacia_australiana_01", 8, 10, 1), ("arvore_folhas_largas_01", 7, 9, 1.2f),
        ("arvore_torta_floresta_01", 7, 9, 1), ("arvore_casca_rugosa_01", 9, 11, 0.8f), ("eucalipto_alto_01", 13, 16, 0.3f),
        ("eucalipto_alto_02", 12, 15, 0.5f));
    static readonly Modelo[] gigante = Lista(("arvore_copa_larga_hero_01", 12, 14, 1));
    static readonly Modelo[] arbustos = Lista(("arbusto_arredondado_01", 1.6f, 2.2f, 1), ("arbusto_irregular_01", 1.8f, 2.5f, 1),
        ("arbusto_folhas_largas_01", 1.2f, 1.7f, 0.8f), ("moita_seca_01", 1.2f, 1.6f, 0.4f),
        ("samambaia_arborea_01", 2.8f, 3.6f, 0.6f), ("samambaia_larga_hero_01", 1.3f, 1.7f, 0.6f));
    static readonly Modelo[] hierba = Lista(("capim_alto_estilizado_01", 1.3f, 1.7f, 1), ("samambaia_media_01", 0.8f, 1.1f, 0.3f));
    static readonly Modelo[] plantas = Lista(("grama_baixa_x_01", 0.2f, 0.3f, 1), ("grama_media_x_01", 0.4f, 0.6f, 1),
        ("folhagem_rasteira_01", 0.12f, 0.2f, 0.8f), ("samambaia_baixa_densa_01", 0.3f, 0.4f, 0.6f), ("planta_roteada_01", 0.7f, 0.9f, 0.3f));
    static readonly Modelo[] piedras = Lista(("pedra_01", 0.5f, 0.8f, 1), ("pedra_02", 0.4f, 0.6f, 1), ("pedra_03", 0.4f, 0.6f, 1),
        ("pedra_04", 0.8f, 1.2f, 0.5f), ("pedra_05", 1f, 1.5f, 0.4f));

    static T[,] mapa;
    static int w, h;
    static System.Random rnd;
    static Material matVoxel;
    static Transform zonas;
    static readonly Dictionary<Vector2Int, Transform> zonaDe = new Dictionary<Vector2Int, Transform>();

    [MenuItem("Saatan/Construir selva (Laberinto)")]
    static void Menu()
    {
        if (EditorApplication.isPlaying) return;
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(MAPA);
        if (tex != null) ConstruirDesdeTextura(tex);
        else EditorUtility.DisplayDialog("Saatan", "No se encontró " + MAPA, "OK");
    }

    public static void ConstruirDesdeTextura(Texture2D tex)
    {
        try { Construir(tex); }
        finally { EditorUtility.ClearProgressBar(); }
    }

    static void Construir(Texture2D tex)
    {
        LeerMapa(tex);
        rnd = new System.Random(SEMILLA);
        if (!AssetDatabase.IsValidFolder(CARPETA)) AssetDatabase.CreateFolder("Assets/Niveles", "Selva");
        Progreso("Modelos", 0f);
        matVoxel = MaterialVoxel();
        if (!CargarModelos()) return;

        foreach (var viejo in new[] { RAIZ, "Selva_Detallada" })
            if (GameObject.Find(viejo) is GameObject go) Object.DestroyImmediate(go);
        var raiz = new GameObject(RAIZ).transform;
        zonas = Hijo(raiz, "Zonas");
        zonaDe.Clear();

        Progreso("Suelo y colisiones", 0.1f);
        Suelo(raiz);
        Colisiones(raiz);

        var troncos = new List<Vector3>();
        var matas = new List<Vector3>();
        int gigantes = 0;

        Progreso("Árboles del borde", 0.3f);
        foreach (var p in Celdas(T.Borde, 0.3f))
            if (!Cerca(troncos, p, 4.2f)) { Poner(arboles, p, "Arbol"); troncos.Add(p); }

        Progreso("Bosque", 0.5f);
        foreach (var p in Celdas(T.Bosque, 0.45f))
        {
            bool grande = gigantes < 3 && Distancia(p, T.Borde, 7) > 6;
            if (Cerca(troncos, p, grande ? 9f : 5.5f)) continue;
            Poner(grande ? gigante : arboles, p, "Arbol");
            troncos.Add(p);
            if (grande) gigantes++;
        }

        Progreso("Arbustos", 0.65f);
        foreach (var p in Celdas(T.Borde, 0.35f))
            if (!Cerca(troncos, p, 2.2f) && !Cerca(matas, p, 1.8f)) { Poner(arbustos, p, "Arbusto"); matas.Add(p); }

        Progreso("Hierba alta y decoración", 0.8f);
        foreach (var p in Celdas(T.Pasto, 0.35f))
            if (rnd.NextDouble() < 0.75) Poner(hierba, p, "Hierba");
        foreach (var p in Celdas(T.Piso, 0.4f))
        {
            bool junto = Distancia(p, T.Borde, 1) <= 1;
            double r = rnd.NextDouble();
            if (junto && r < 0.04) Poner(piedras, p, "Piedra");
            else if (junto ? r < 0.16 : r < 0.06) Poner(plantas, p, "Planta");
        }

        // Inicio y salida.
        var ini = Celdas(T.Inicio, 0f);
        if (ini.Count > 0) Hijo(raiz, "Inicio_Jugador").localPosition = ini[0];
        var sal = Celdas(T.Salida, 0f);
        if (sal.Count > 0)
        {
            Vector3 a = sal[0], b = sal[0];
            foreach (var p in sal) { a = Vector3.Min(a, p); b = Vector3.Max(b, p); }
            var meta = Hijo(raiz, "Meta_Salida");
            meta.localPosition = (a + b) * 0.5f + Vector3.up;
            var col = meta.gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = b - a + new Vector3(1f, 2f, 1f);
            meta.gameObject.AddComponent<MetaNivel>();
        }

        ConfigurarEscena();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkAllScenesDirty();
        Debug.Log($"Selva creada ({w} x {h} m) en {zonas.childCount} zonas de {ZONA} m.");
    }

    // ---------------- Mapa ----------------

    static void LeerMapa(Texture2D tex)
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex));
        if (!imp.isReadable || imp.npotScale != TextureImporterNPOTScale.None)
        {
            imp.isReadable = true;
            imp.mipmapEnabled = false;
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.SaveAndReimport();
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(MAPA);
        }

        var px = tex.GetPixels32();
        w = tex.width; h = tex.height;
        mapa = new T[w, h];
        for (int i = 0; i < px.Length; i++)
            mapa[i % w, i / w] = px[i].a < 128 ? T.Vacio : paleta.TryGetValue(px[i], out var t) ? t : T.Piso;
    }

    static Vector3 Pos(int x, int y) => new Vector3(x - w / 2f + 0.5f, 0f, y - h / 2f + 0.5f);

    static T En(Vector3 p)
    {
        int x = Mathf.FloorToInt(p.x + w / 2f), y = Mathf.FloorToInt(p.z + h / 2f);
        return x < 0 || y < 0 || x >= w || y >= h ? T.Vacio : mapa[x, y];
    }

    // Posiciones (con un poco de desorden) de las celdas de un tipo, en orden aleatorio.
    static List<Vector3> Celdas(T t, float desorden)
    {
        var l = new List<Vector3>();
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (mapa[x, y] == t)
                    l.Add(Pos(x, y) + new Vector3(Azar(desorden), 0f, Azar(desorden)));
        for (int i = l.Count - 1; i > 0; i--) { int j = rnd.Next(i + 1); (l[i], l[j]) = (l[j], l[i]); }
        return l;
    }

    static float Azar(float a) => ((float)rnd.NextDouble() * 2f - 1f) * a;

    static bool Cerca(List<Vector3> puntos, Vector3 p, float d)
    {
        foreach (var q in puntos) if ((q.x - p.x) * (q.x - p.x) + (q.z - p.z) * (q.z - p.z) < d * d) return true;
        return false;
    }

    // Distancia en celdas (anillos cuadrados) a la celda más cercana de un tipo, hasta 'max'.
    static int Distancia(Vector3 p, T t, int max)
    {
        for (int r = 0; r <= max; r++)
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) == r && En(p + new Vector3(dx, 0, dy)) == t) return r;
        return max + 1;
    }

    // ---------------- Suelo y colisiones ----------------

    // Un solo quad con textura para todo el suelo (1 draw call).
    static void Suelo(Transform raiz)
    {
        // Césped en bloques de 25 cm (4 píxeles por metro) con tonos variados, al estilo voxel.
        const int R = 4;
        Color cesped = new Color(0.30f, 0.55f, 0.18f), hierbaAlta = new Color(0.20f, 0.42f, 0.12f), sombra = new Color(0.15f, 0.30f, 0.10f);
        var tex = new Texture2D(w * R, h * R, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "Tex_Suelo" };
        for (int y = 0; y < h * R; y++)
            for (int x = 0; x < w * R; x++)
            {
                T t = mapa[x / R, y / R];
                Color c = t == T.Pasto ? hierbaAlta : t == T.Borde || t == T.Bosque || t == T.Vacio ? sombra : cesped;
                tex.SetPixel(x, y, c * (0.85f + 0.3f * (float)rnd.NextDouble()));
            }
        tex.Apply();
        Guardar(tex, CARPETA + "/Tex_Suelo.asset");

        var mat = Guardar(new Material(Shader.Find("Universal Render Pipeline/Lit")), CARPETA + "/Mat_Suelo.mat");
        mat.SetTexture("_BaseMap", tex);
        mat.SetFloat("_Smoothness", 0.05f);

        var suelo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        suelo.name = "Suelo";
        Object.DestroyImmediate(suelo.GetComponent<Collider>());
        suelo.transform.SetParent(raiz, false);
        suelo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        suelo.transform.localScale = new Vector3(w, h, 1f);
        var mr = suelo.GetComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = ShadowCastingMode.Off;
    }

    // Una caja para el suelo y una por cada tramo seguido de borde en cada fila.
    static void Colisiones(Transform raiz)
    {
        var go = Hijo(raiz, "Colisiones").gameObject;
        go.isStatic = true;
        var piso = go.AddComponent<BoxCollider>();
        piso.center = new Vector3(0f, -0.25f, 0f);
        piso.size = new Vector3(w, 0.5f, h);

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (mapa[x, y] != T.Borde) continue;
                int x0 = x;
                while (x + 1 < w && mapa[x + 1, y] == T.Borde) x++;
                var c = go.AddComponent<BoxCollider>();
                c.center = (Pos(x0, y) + Pos(x, y)) * 0.5f + Vector3.up * ALTO_MURO * 0.5f;
                c.size = new Vector3(x - x0 + 1, ALTO_MURO, 1f);
            }
    }

    // ---------------- Modelos ----------------

    static bool CargarModelos()
    {
        var faltan = new List<string>();
        foreach (var lista in new[] { arboles, gigante, arbustos, hierba, plantas, piedras })
            foreach (var m in lista)
            {
                string ruta = ARTE + m.archivo + "_LOD2.fbx";
                if (AssetImporter.GetAtPath(ruta) is ModelImporter imp)
                    foreach (var o in AssetDatabase.LoadAllAssetRepresentationsAtPath(ruta))
                        if (o is Material) { imp.AddRemap(new AssetImporter.SourceAssetIdentifier(o), matVoxel); imp.SaveAndReimport(); break; }

                m.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
                if (m.prefab == null) { faltan.Add(m.archivo); continue; }

                var tmp = (GameObject)PrefabUtility.InstantiatePrefab(m.prefab);
                tmp.transform.position = Vector3.zero;
                m.alto = Mathf.Max(tmp.GetComponentInChildren<Renderer>().bounds.size.y, 0.01f);
                Object.DestroyImmediate(tmp);
            }

        if (faltan.Count == 0) return true;
        EditorUtility.DisplayDialog("Saatan", "Faltan modelos en " + ARTE + ":\n" + string.Join("\n", faltan), "OK");
        return false;
    }

    static void Poner(Modelo[] lista, Vector3 pos, string nombre)
    {
        // Elegir según el peso.
        float total = 0f;
        foreach (var x in lista) total += x.peso;
        float r = (float)rnd.NextDouble() * total;
        Modelo m = lista[lista.Length - 1];
        foreach (var x in lista) if ((r -= x.peso) <= 0f) { m = x; break; }

        var zona = Zona(pos);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(m.prefab, zona);
        go.name = nombre + "_" + m.archivo;
        var t = go.transform;
        t.localPosition = pos - zona.localPosition;
        t.localRotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f) * m.prefab.transform.localRotation;
        t.localScale = m.prefab.transform.localScale * (Mathf.Lerp(m.min, m.max, (float)rnd.NextDouble()) / m.alto);

        foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
        {
            if (mr.sharedMaterial != matVoxel) mr.sharedMaterial = matVoxel;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }
    }

    static Transform Zona(Vector3 p)
    {
        var k = new Vector2Int(Mathf.FloorToInt((p.x + w / 2f) / ZONA), Mathf.FloorToInt((p.z + h / 2f) / ZONA));
        if (zonaDe.TryGetValue(k, out var z)) return z;
        z = Hijo(zonas, $"Zona_{k.x}_{k.y}");
        z.localPosition = new Vector3((k.x + 0.5f) * ZONA - w / 2f, 0f, (k.y + 0.5f) * ZONA - h / 2f);
        return zonaDe[k] = z;
    }

    // ---------------- Materiales y escena ----------------

    // Los FBX guardan el color en los vértices: se usa el shader Saatan/VegetacionVoxel.
    static Material MaterialVoxel()
    {
        Shader sh = Shader.Find("Saatan/VegetacionVoxel");
        if (sh == null || ShaderUtil.ShaderHasError(sh)) sh = Shader.Find("Universal Render Pipeline/Particles/Simple Lit");
        string ruta = CARPETA + "/Mat_Vegetacion_Voxel.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null) m = Guardar(new Material(sh), ruta);
        m.shader = sh;
        m.enableInstancing = true;
        EditorUtility.SetDirty(m);
        return m;
    }

    // La neblina, el color de fondo y el alcance de la cámara los aplica CargaSelva al iniciar.
    static void ConfigurarEscena()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.40f, 0.45f, 0.40f);
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional)
            {
                l.intensity = 1f;
                l.color = new Color(1f, 0.96f, 0.85f);
                l.shadows = LightShadows.None;
                l.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

        var cam = Camera.main;
        if (cam == null) return;
        var carga = cam.GetComponent<CargaSelva>();
        if (carga == null) carga = cam.gameObject.AddComponent<CargaSelva>();
        carga.zonas = zonas;
    }

    // ---------------- Utilidades ----------------

    static Transform Hijo(Transform padre, string nombre)
    {
        var t = new GameObject(nombre).transform;
        t.SetParent(padre, false);
        return t;
    }

    static A Guardar<A>(A asset, string ruta) where A : Object
    {
        AssetDatabase.DeleteAsset(ruta);
        AssetDatabase.CreateAsset(asset, ruta);
        return asset;
    }

    static void Progreso(string paso, float t) => EditorUtility.DisplayProgressBar("Saatan - Selva", paso, t);
}
