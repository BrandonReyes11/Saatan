// Saatan Historia - Nivel I: La selva (laberinto).
// Construye el laberinto desde Assets/Project/Niveles/Mapas/selva_laberinto.png (1 px = 1 m) con los FBX de Prefabs/Environment/Selva (LOD2):
//   negro = borde (árboles + arbustos, con colisión) | verde oscuro = bosque cerrado | verde = hierba alta
//   blanco = pasillo (decoración pequeña) | azul = inicio | rosa = salida (lleva a las ruinas)
// Hay claros con árboles más pequeños, carteles de flecha discretos hacia la salida y, al final, unas
// ruinas abiertas (Prefabs/Environment/Ruinas) donde es la pelea: al cruzar su entrada se borra el laberinto.
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
    public const string MAPA = "Assets/Project/Niveles/Mapas/selva_laberinto.png";
    const string ARTE = "Assets/Project/Prefabs/Environment/Selva/", CARPETA = "Assets/Project/Niveles/Selva", RAIZ = "Selva_Laberinto";
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
    static readonly Modelo[] arbolesPequenos = Lista(("arvore_torta_floresta_01", 4, 5.5f, 1), ("arvore_folhas_largas_01", 4, 5.5f, 1),
        ("acacia_australiana_01", 4.5f, 6, 0.8f), ("samambaia_arborea_01", 3, 4, 1));
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
        if (!AssetDatabase.IsValidFolder(CARPETA)) AssetDatabase.CreateFolder("Assets/Project/Niveles", "Selva");
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

        // Árboles más separados; en los "claros" (ruido de Perlin) son pequeños para que no se sienta saturado.
        Progreso("Árboles del borde", 0.3f);
        foreach (var p in Celdas(T.Borde, 0.3f))
        {
            bool claro = Claro(p);
            if (Cerca(troncos, p, claro ? 4f : 5.5f) || (!claro && rnd.NextDouble() < 0.15)) continue;
            Poner(claro ? arbolesPequenos : arboles, p, "Arbol");
            troncos.Add(p);
        }

        Progreso("Bosque", 0.5f);
        foreach (var p in Celdas(T.Bosque, 0.45f))
        {
            bool grande = gigantes < 3 && Distancia(p, T.Borde, 7) > 6;
            if (Cerca(troncos, p, grande ? 9f : 7.5f)) continue;
            Poner(grande ? gigante : Claro(p) ? arbolesPequenos : arboles, p, "Arbol");
            troncos.Add(p);
            if (grande) gigantes++;
        }

        Progreso("Arbustos", 0.65f);
        foreach (var p in Celdas(T.Borde, 0.35f))
            if (!Cerca(troncos, p, 2.4f) && !Cerca(matas, p, 2.6f) && rnd.NextDouble() < 0.75) { Poner(arbustos, p, "Arbusto"); matas.Add(p); }

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
        Progreso("Carteles", 0.85f);
        Carteles();
        var sal = Celdas(T.Salida, 0f);
        if (sal.Count > 0)
        {
            Vector3 a = sal[0], b = sal[0];
            foreach (var p in sal) { a = Vector3.Min(a, p); b = Vector3.Max(b, p); }
            Progreso("Ruinas", 0.9f);
            Ruinas(raiz, a, b);
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

        // Abrir la salida hasta el borde del mapa: detrás de ella el mapa trae una fila de borde (colisión y
        // árboles) y bosque que tapaban el paso a las ruinas. Queda un pasillo con borde arriba y abajo.
        int y0 = h, y1 = -1, xs = -1;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (mapa[x, y] == T.Salida) { y0 = Mathf.Min(y0, y); y1 = Mathf.Max(y1, y); xs = Mathf.Max(xs, x); }
        for (int x = xs + 1; xs >= 0 && x < w; x++)
        {
            for (int y = y0; y <= y1; y++) mapa[x, y] = T.Piso;
            if (y0 > 0) mapa[x, y0 - 1] = T.Borde;
            if (y1 < h - 1) mapa[x, y1 + 1] = T.Borde;
        }
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
        foreach (var lista in new[] { arboles, arbolesPequenos, gigante, arbustos, hierba, plantas, piedras })
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

    static void Poner(Modelo[] lista, Vector3 pos, string nombre, Transform padre = null)
    {
        // Elegir según el peso.
        float total = 0f;
        foreach (var x in lista) total += x.peso;
        float r = (float)rnd.NextDouble() * total;
        Modelo m = lista[lista.Length - 1];
        foreach (var x in lista) if ((r -= x.peso) <= 0f) { m = x; break; }

        var zona = padre != null ? padre : Zona(pos);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(m.prefab, zona);
        go.name = nombre + "_" + m.archivo;
        var t = go.transform;
        t.localPosition = pos - (padre != null ? Vector3.zero : zona.localPosition);
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

    // Pieza de ruinas (prefabs en Prefabs/Environment/Ruinas, convertidos de los .glb originales).
    public static GameObject Ruina(string nombre) =>
        AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/Prefabs/Environment/Ruinas/" + nombre + ".prefab");

    static bool Claro(Vector3 p) => Mathf.PerlinNoise(p.x * 0.035f + 50f, p.z * 0.035f + 50f) > 0.52f;

    // ---------------- Carteles de flecha ----------------

    static bool Caminable(int x, int y) => x >= 0 && y >= 0 && x < w && y < h &&
        (mapa[x, y] == T.Piso || mapa[x, y] == T.Pasto || mapa[x, y] == T.Inicio || mapa[x, y] == T.Salida);

    // Camino más corto inicio -> salida (búsqueda en anchura desde la salida). Cada ~30 m se pone un cartel
    // pequeño de madera oscura a un lado del pasillo, junto a los árboles, apuntando hacia donde sigue el camino.
    static void Carteles()
    {
        var dist = new int[w, h];
        var cola = new Queue<Vector2Int>();
        Vector2Int inicio = new Vector2Int(-1, -1);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                dist[x, y] = -1;
                if (mapa[x, y] == T.Salida) { dist[x, y] = 0; cola.Enqueue(new Vector2Int(x, y)); }
                if (mapa[x, y] == T.Inicio) inicio = new Vector2Int(x, y);
            }
        var dirs = new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
        while (cola.Count > 0)
        {
            var c = cola.Dequeue();
            foreach (var d in dirs)
            {
                var n = c + d;
                if (Caminable(n.x, n.y) && dist[n.x, n.y] < 0) { dist[n.x, n.y] = dist[c.x, c.y] + 1; cola.Enqueue(n); }
            }
        }
        if (inicio.x < 0 || dist[inicio.x, inicio.y] < 0) return;

        var camino = new List<Vector2Int> { inicio };
        while (dist[camino[camino.Count - 1].x, camino[camino.Count - 1].y] > 0)
        {
            var c = camino[camino.Count - 1];
            foreach (var d in dirs)
                if (Caminable(c.x + d.x, c.y + d.y) && dist[c.x + d.x, c.y + d.y] == dist[c.x, c.y] - 1) { camino.Add(c + d); break; }
        }

        var madera = Guardar(new Material(Shader.Find("Universal Render Pipeline/Lit")), CARPETA + "/Mat_Cartel.mat");
        madera.color = new Color(0.22f, 0.15f, 0.09f);
        madera.SetFloat("_Smoothness", 0f);

        for (int i = 20; i < camino.Count - 12; i += 30)
        {
            Vector3 p = Pos(camino[i].x, camino[i].y), dir = Pos(camino[i + 10].x, camino[i + 10].y) - p;
            // A un lado: la celda caminable junto a un borde más cercana.
            float mejor = float.MaxValue;
            for (int dy = -5; dy <= 5; dy++)
                for (int dx = -5; dx <= 5; dx++)
                {
                    int x = camino[i].x + dx, y = camino[i].y + dy;
                    if (!Caminable(x, y) || dx * dx + dy * dy >= mejor) continue;
                    foreach (var d in dirs)
                        if (x + d.x >= 0 && y + d.y >= 0 && x + d.x < w && y + d.y < h && mapa[x + d.x, y + d.y] == T.Borde)
                        { mejor = dx * dx + dy * dy; p = Pos(x, y); break; }
                }

            var z = Zona(p);
            var g = new GameObject("Cartel_Flecha").transform;
            g.SetParent(z, false);
            g.localPosition = p - z.localPosition;
            g.localRotation = Quaternion.LookRotation(dir.normalized) * Quaternion.Euler(Azar(5f), 0f, Azar(6f));
            Cubo(g, new Vector3(0f, 0.55f, 0f), new Vector3(0.1f, 1.1f, 0.1f), madera);
            Cubo(g, new Vector3(0f, 0.95f, 0.15f), new Vector3(0.04f, 0.14f, 0.5f), madera);
            Cubo(g, new Vector3(0f, 0.95f, 0.4f), new Vector3(0.04f, 0.17f, 0.17f), madera).localRotation = Quaternion.Euler(45f, 0f, 0f);
        }
    }

    static Transform Cubo(Transform padre, Vector3 pos, Vector3 esc, Material m)
    {
        var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(c.GetComponent<Collider>());
        c.transform.SetParent(padre, false);
        c.transform.localPosition = pos;
        c.transform.localScale = esc;
        var r = c.GetComponent<MeshRenderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = ShadowCastingMode.Off;
        return c.transform;
    }

    // ---------------- Ruinas al final del laberinto (zona de la pelea) ----------------

    const float R_ARENA = 20f, PASILLO = 8f;

    // a y b: extremos de la salida del laberinto; el pasillo a las ruinas empieza en el borde derecho del mapa (x = w/2).
    static void Ruinas(Transform laberinto, Vector3 a, Vector3 b)
    {
        if (GameObject.Find("Ruinas") is GameObject vieja) Object.DestroyImmediate(vieja);
        var raiz = new GameObject("Ruinas").transform;
        var veg = Hijo(raiz, "Vegetacion");
        float x0 = w / 2f, zc = (a.z + b.z) / 2f, ancho = b.z - a.z + 1f, rMuro = R_ARENA + 1.5f;
        var c = new Vector3(x0 + PASILLO + R_ARENA, 0f, zc);
        System.Func<float, float, Vector3> EnAro = (ang, r) => c + new Vector3(Mathf.Cos(ang * Mathf.Deg2Rad), 0f, Mathf.Sin(ang * Mathf.Deg2Rad)) * r;
        System.Func<string, Vector3, float, Transform> Pieza = (nombre, pos, giro) =>
        {
            var pf = ConstructorSelva.Ruina(nombre);
            if (pf == null) { Debug.LogWarning("Falta la pieza de ruinas " + nombre); return null; }
            var t = ((GameObject)PrefabUtility.InstantiatePrefab(pf, raiz)).transform;
            t.localPosition = pos;
            t.localRotation = Quaternion.Euler(0f, giro, 0f);
            return t;
        };
        float hueco = Mathf.Asin((ancho / 2f + 0.5f) / rMuro) * Mathf.Rad2Deg;   // abertura de la entrada (lado oeste, 180°)

        // Suelo de césped (textura pequeña repetida) y colisiones: plaza circular + pasillo.
        var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "Tex_Cesped" };
        for (int i = 0; i < 32 * 32; i++) tex.SetPixel(i % 32, i / 32, new Color(0.30f, 0.55f, 0.18f) * (0.85f + 0.3f * (float)rnd.NextDouble()));
        tex.Apply();
        Guardar(tex, CARPETA + "/Tex_Cesped.asset");
        var cesped = Guardar(new Material(Shader.Find("Universal Render Pipeline/Lit")), CARPETA + "/Mat_Cesped.mat");
        cesped.SetTexture("_BaseMap", tex);
        cesped.SetFloat("_Smoothness", 0f);
        Vector3 min = new Vector3(x0, 0f, zc - rMuro - 12f), max = c + new Vector3(rMuro + 12f, 0f, rMuro + 12f), tam = max - min;
        cesped.SetTextureScale("_BaseMap", new Vector2(tam.x / 8f, tam.z / 8f));
        var suelo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        suelo.name = "Suelo_Ruinas";
        Object.DestroyImmediate(suelo.GetComponent<Collider>());
        suelo.transform.SetParent(raiz, false);
        suelo.transform.localPosition = (min + max) / 2f;
        suelo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        suelo.transform.localScale = new Vector3(tam.x, tam.z, 1f);
        suelo.GetComponent<MeshRenderer>().sharedMaterial = cesped;

        var col = Hijo(raiz, "Colisiones").gameObject;
        System.Action<Vector3, Vector3> Caja = (centro, size) => { var bc = col.AddComponent<BoxCollider>(); bc.center = centro; bc.size = size; };
        Caja((min + max) / 2f + Vector3.down * 0.25f, new Vector3(tam.x, 0.5f, tam.z));
        for (float ang = 0f; ang < 360f; ang += 7.5f)
            if (Mathf.Abs(Mathf.DeltaAngle(ang, 180f)) > hueco)
            {
                var bc = Hijo(col.transform, "Muro");
                bc.localPosition = EnAro(ang, rMuro) + Vector3.up * ALTO_MURO / 2f;
                bc.localRotation = Quaternion.Euler(0f, -ang, 0f);
                bc.gameObject.AddComponent<BoxCollider>().size = new Vector3(1f, ALTO_MURO, rMuro * 0.14f);
            }
        float xEntrada = EnAro(180f + hueco, rMuro).x;
        foreach (float s in new[] { -1f, 1f })
            Caja(new Vector3((x0 + xEntrada) / 2f, ALTO_MURO / 2f, zc + s * (ancho / 2f + 0.5f)), new Vector3(xEntrada - x0 + 1f, ALTO_MURO, 1f));

        // Pasillo de entrada: cercas rotas y vegetación a los lados.
        for (float x = x0 + 1.5f; x < xEntrada - 1f; x += 2.8f)
            foreach (float s in new[] { -1f, 1f })
            {
                if (rnd.NextDouble() < 0.3) continue;
                Pieza("Fence", new Vector3(x, 0f, zc + s * (ancho / 2f + 0.2f)), 90f);
                Poner(arbustos, new Vector3(x + Azar(1f), 0f, zc + s * (ancho / 2f + 2f + Azar(1f))), "Arbusto", veg);
                if (rnd.NextDouble() < 0.5) Poner(arboles, new Vector3(x, 0f, zc + s * (ancho / 2f + 5f + Azar(2f))), "Arbol", veg);
            }

        // Muro exterior en ruinas: tramos enteros, rotos o caídos (con musgo y arbustos en los huecos).
        string[] muros = { "WallNormalXL", "WallBrokenXl", "WallBrokenMD", "WallBrokenXS", "WallNormalMD" };
        for (float ang = 0f; ang < 360f; ang += 18f)
        {
            if (Mathf.Abs(Mathf.DeltaAngle(ang, 180f)) < hueco + 3f) continue;
            Vector3 p = EnAro(ang, rMuro);
            if (rnd.NextDouble() < 0.18) { Poner(arbustos, p, "Arbusto", veg); continue; }
            Pieza(muros[rnd.Next(muros.Length)], p, -ang);   // el largo del muro (eje Z) sigue el círculo
            if (rnd.NextDouble() < 0.5) Pieza(rnd.NextDouble() < 0.5 ? "GroundMossMD" : "GroundMossXS", EnAro(ang + Azar(5f), rMuro - 1f), rnd.Next(360));
        }
        foreach (float s in new[] { -1f, 1f }) Pieza("PillarSquare", EnAro(180f + s * (hueco + 2f), rMuro), 0f);

        // Interior: losas, anillo de pilares (algunos rotos), plataformas y el altar al fondo.
        string[] losas = { "BlockNormalXS", "BlockCrackXS", "BlockMossXS" };
        for (int i = -2; i <= 2; i++)
            for (int j = -2; j <= 2; j++)
            {
                var p = c + new Vector3(i * 6.6f, -0.38f, j * 6.6f);
                if ((p - c).magnitude < 14f && rnd.NextDouble() < 0.75) Pieza(losas[rnd.Next(3)], p, 90f * rnd.Next(4));
            }
        string[] pilares = { "PillarRound", "PillarRoundCut", "PillarSquareCut", "PillarRoundCut" };
        for (float ang = 18f; ang < 360f; ang += 36f)
        {
            if (rnd.NextDouble() < 0.15) continue;
            Pieza(pilares[rnd.Next(4)], EnAro(ang, 11f), rnd.Next(4) * 90f);
            if (rnd.NextDouble() < 0.5) Pieza("MossXS", EnAro(ang + 4f, 11.2f), rnd.Next(360));
        }
        foreach (float ang in new[] { 120f, 235f }) Pieza(rnd.NextDouble() < 0.5 ? "BlockMossMD" : "BlockCrackMD", EnAro(ang, 16f), Azar(30f));

        Vector3 fondo = c + Vector3.right * (R_ARENA - 5f);
        Pieza("Altar", fondo, -90f);
        foreach (var (nombre, off) in new[] { ("Skull", new Vector3(1f, 0, 3f)), ("SkullCracked", new Vector3(-1f, 0, -3.5f)), ("Vase", new Vector3(2f, 0, -3f)),
            ("Vase", new Vector3(0f, 0, 4.5f)), ("ChestBase", new Vector3(3f, 0, 5f)), ("AncientCoinRuby", new Vector3(-2f, 0, 2f)) })
            Pieza(nombre, fondo + off, rnd.Next(360));

        // Vegetación: ruinas abiertas, hierba alta dentro y bosque alrededor.
        for (int i = 0; i < 40; i++)
        {
            var p = EnAro(rnd.Next(360), Mathf.Lerp(6f, R_ARENA, (float)rnd.NextDouble()));
            Poner(rnd.NextDouble() < 0.7 ? hierba : plantas, p, "Hierba", veg);
        }
        for (int i = 0; i < 4; i++) Poner(arbolesPequenos, EnAro(40f + i * 85f + Azar(15f), R_ARENA - 3f), "Arbol", veg);
        for (float ang = 0f; ang < 360f; ang += 14f)
        {
            if (Mathf.Abs(Mathf.DeltaAngle(ang, 180f)) < hueco + 6f) continue;
            Poner(arbustos, EnAro(ang + Azar(5f), rMuro + 2.5f), "Arbusto", veg);
            Poner(arboles, EnAro(ang + Azar(5f), rMuro + 6f + Azar(3f)), "Arbol", veg);
        }

        // Línea de entrada: al cruzarla se borra el laberinto, se cierra la puerta y empieza la pelea.
        var cierre = Hijo(raiz, "Cierre_Entrada");
        var puerta = Pieza("Gate", new Vector3(xEntrada - 0.5f, 0f, zc), 0f);
        if (puerta != null) puerta.SetParent(cierre, true);
        var bloqueo = cierre.gameObject.AddComponent<BoxCollider>();
        bloqueo.center = new Vector3(xEntrada - 0.5f, ALTO_MURO / 2f, zc);
        bloqueo.size = new Vector3(1f, ALTO_MURO, ancho + 2f);
        cierre.gameObject.SetActive(false);

        var pelea = Hijo(raiz, "Pelea");
        Hijo(pelea, "Aparicion_Enemigo").localPosition = c + Vector3.right * 6f;
        // Al vencer al Nahual, el script de la pelea debe llamar a  pelea.GetComponent<CambiarEscena>().Cargar()
        // (o, desde un evento, a CambiarEscena > Cargar ()): pasa a la cinemática de los molletes.
        pelea.gameObject.AddComponent<CambiarEscena>().escena = "MolletesCinematica";
        pelea.gameObject.SetActive(false);

        var linea = Hijo(raiz, "Linea_Ruinas");
        linea.localPosition = new Vector3(xEntrada + 2f, ALTO_MURO / 2f, zc);
        var trig = linea.gameObject.AddComponent<BoxCollider>();
        trig.isTrigger = true;
        trig.size = new Vector3(1.5f, ALTO_MURO, ancho + 2f);
        var entrada = linea.gameObject.AddComponent<EntradaRuinas>();
        entrada.laberinto = laberinto.gameObject;
        entrada.cierre = cierre.gameObject;
        entrada.pelea = pelea.gameObject;
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
