// Saatan Historia - Nivel I: La selva (laberinto).
// Colocar en: Assets/Editor/ConstructorSelva.cs
//
// Construye el laberinto a partir de Assets/Mapas/selva_laberinto.png (1 píxel = 1 metro)
// usando los modelos FBX de Assets/Art/LOD0:
//   negro         -> borde del laberinto: fila de árboles + arbustos, con colisión.
//   verde oscuro  -> bosque cerrado (islas y exterior): árboles de relleno, sin paso.
//   verde         -> hierba alta (capim / helechos), se puede caminar entre ella.
//   blanco        -> suelo del pasillo con decoración pequeña (piedras, pasto bajo).
//   azul          -> inicio del jugador.      rosa -> salida del nivel.
//
// Menú: Saatan > Construir selva (Laberinto)
// Para reconstruir NivelSelva completo (abre, construye, crea a Iktan y guarda):
//       Saatan > Escenas > Reconstruir NivelSelva

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class ConstructorSelva
{
    enum Tipo { Piso, Limite, Pasto, Arbol, Tronco, Ruina, Columna, Trampa, Nahual, Rama, Trompos, Inicio, Salida }

    public const string MAPA = "Assets/Mapas/selva_laberinto.png";
    const string ARTE = "Assets/Art/LOD0/";
    const string CARPETA = "Assets/Niveles/Selva";
    const string RAIZ = "Selva_Laberinto";
    const int SEMILLA = 2026;

    // ---- Ajustes ----
    const float ALTO_MURO = 4f;            // altura de la colisión invisible del borde
    const float SEPARACION_ARBOLES = 4.2f; // distancia mínima entre troncos en el borde
    const float SEPARACION_BOSQUE = 5.5f;  // distancia mínima entre troncos en el bosque cerrado
    const int MAX_ARBOLES_GIGANTES = 3;    // el árbol "hero" es muy pesado
    const float DENSIDAD_NIEBLA = 0.016f;

    static readonly (Color32 color, Tipo tipo)[] paleta =
    {
        (new Color32(255, 255, 255, 255), Tipo.Piso),
        (new Color32(0, 0, 0, 255), Tipo.Limite),
        (new Color32(0, 170, 0, 255), Tipo.Pasto),
        (new Color32(0, 90, 0, 255), Tipo.Arbol),
        (new Color32(140, 80, 30, 255), Tipo.Tronco),
        (new Color32(128, 128, 128, 255), Tipo.Ruina),
        (new Color32(200, 200, 200, 255), Tipo.Columna),
        (new Color32(0, 210, 210, 255), Tipo.Trampa),
        (new Color32(230, 0, 0, 255), Tipo.Nahual),
        (new Color32(255, 150, 0, 255), Tipo.Rama),
        (new Color32(255, 220, 0, 255), Tipo.Trompos),
        (new Color32(0, 100, 255, 255), Tipo.Inicio),
        (new Color32(255, 105, 180, 255), Tipo.Salida),
    };

    // ---------------- Catálogo de modelos (alturas en metros; Iktan mide ~1.8 m) ----------------

    class Modelo
    {
        public string archivo;
        public float min, max;      // altura final deseada
        public float peso;          // probabilidad relativa dentro de su grupo
        public GameObject prefab;
        public float alto;          // altura original del FBX
        public float radio;         // radio original de la copa / follaje
    }

    static Modelo M(string archivo, float min, float max, float peso = 1f) =>
        new Modelo { archivo = archivo, min = min, max = max, peso = peso };

    static readonly Modelo[] arbolesBorde =
    {
        M("acacia_australiana_01", 8f, 10f, 1.0f),
        M("arvore_folhas_largas_01", 7f, 9f, 1.2f),
        M("arvore_torta_floresta_01", 7f, 9f, 1.0f),
        M("arvore_casca_rugosa_01", 9f, 11f, 0.8f),
        M("eucalipto_alto_02", 12f, 15f, 0.5f),
    };

    static readonly Modelo[] arbolesBosque =
    {
        M("acacia_australiana_01", 8f, 11f, 1.0f),
        M("arvore_folhas_largas_01", 7f, 10f, 1.0f),
        M("arvore_torta_floresta_01", 7f, 9f, 0.8f),
        M("arvore_casca_rugosa_01", 10f, 12f, 0.8f),
        M("eucalipto_alto_01", 13f, 16f, 0.6f),
        M("eucalipto_alto_02", 12f, 15f, 0.6f),
    };

    static readonly Modelo arbolGigante = M("arvore_copa_larga_hero_01", 12f, 14f);

    static readonly Modelo[] arbustos =
    {
        M("arbusto_arredondado_01", 1.6f, 2.2f, 1.0f),
        M("arbusto_irregular_01", 1.8f, 2.5f, 1.0f),
        M("arbusto_folhas_largas_01", 1.2f, 1.7f, 0.8f),
        M("moita_seca_01", 1.2f, 1.6f, 0.4f),
        M("samambaia_arborea_01", 2.8f, 3.6f, 0.6f),
        M("samambaia_larga_hero_01", 1.3f, 1.7f, 0.6f),
    };

    static readonly Modelo[] hierbaAlta =
    {
        M("capim_alto_estilizado_01", 1.3f, 1.7f, 1.0f),
        M("samambaia_media_01", 0.8f, 1.1f, 0.3f),
        M("grama_media_x_01", 0.8f, 1.0f, 0.3f),
    };

    static readonly Modelo[] decoSuelo =
    {
        M("grama_baixa_x_01", 0.2f, 0.3f, 1.0f),
        M("grama_media_x_01", 0.4f, 0.6f, 1.0f),
        M("folhagem_rasteira_01", 0.12f, 0.2f, 0.8f),
        M("samambaia_baixa_densa_01", 0.3f, 0.4f, 0.6f),
        M("planta_roteada_01", 0.7f, 0.9f, 0.3f),
    };

    static readonly Modelo[] piedras =
    {
        M("pedra_01", 0.5f, 0.8f),
        M("pedra_02", 0.4f, 0.6f),
        M("pedra_03", 0.4f, 0.6f),
        M("pedra_04", 0.8f, 1.2f, 0.5f),
        M("pedra_05", 1.0f, 1.5f, 0.4f),
    };

    static Material matVoxel;
    static System.Random rnd;

    // ---------------- Menú ----------------

    [MenuItem("Saatan/Construir selva (Laberinto)")]
    static void Construir()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Saatan", "Sal del modo Play antes de construir el nivel.", "OK");
            return;
        }

        var tex = Selection.activeObject as Texture2D;
        if (tex == null) tex = AssetDatabase.LoadAssetAtPath<Texture2D>(MAPA);
        if (tex == null)
        {
            EditorUtility.DisplayDialog("Saatan", "No se encontró " + MAPA + ".", "OK");
            return;
        }

        ConstruirDesdeTextura(tex);
    }

    public static void ConstruirDesdeTextura(Texture2D tex)
    {
        try
        {
            Construir(tex);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    // ---------------- Construcción ----------------

    static void Construir(Texture2D tex)
    {
        string ruta = AssetDatabase.GetAssetPath(tex);
        var imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
        if (!imp.isReadable || imp.textureCompression != TextureImporterCompression.Uncompressed ||
            imp.npotScale != TextureImporterNPOTScale.None || imp.mipmapEnabled)
        {
            imp.isReadable = true;
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        }

        Color32[] px = tex.GetPixels32();
        int w = tex.width, h = tex.height;

        var mapa = new int[w, h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color32 p = px[y * w + x];
                mapa[x, y] = p.a < 128 ? -1 : (int)Clasificar(p);
            }

        CrearCarpetas();
        rnd = new System.Random(SEMILLA);

        Progreso("Preparando materiales", 0.02f);
        matVoxel = MaterialVoxel();
        UsarMaterialEnFBX(matVoxel);
        if (!CargarModelos()) return;

        foreach (string viejo in new[] { RAIZ, "Selva_Detallada" })
        {
            var go = GameObject.Find(viejo);
            if (go != null) Object.DestroyImmediate(go);
        }

        var raiz = new GameObject(RAIZ).transform;

        Progreso("Suelo y colisiones", 0.1f);
        Suelo(raiz, mapa, w, h);
        Colisiones(raiz, mapa, w, h);

        var arboles = Grupo(raiz, "Arboles");
        var arbustosG = Grupo(raiz, "Arbustos");
        var hierba = Grupo(raiz, "Hierba_Alta");
        var deco = Grupo(raiz, "Decoracion");

        var troncos = new List<Vector3>();   // posiciones de árboles ya puestos

        // ---- 1. Árboles del borde (siguen las líneas negras del boceto) ----
        Progreso("Árboles del borde", 0.25f);
        foreach (var c in Celdas(mapa, w, h, Tipo.Limite, true))
        {
            Vector3 p = Pos(c.x, c.y, w, h) + Jitter(0.3f);
            if (Cerca(troncos, p, SEPARACION_ARBOLES)) continue;
            Poner(Elegir(arbolesBorde), arboles, p, "Arbol");
            troncos.Add(p);
        }

        // ---- 2. Bosque cerrado (dentro de las islas y alrededor) ----
        Progreso("Bosque", 0.45f);
        int gigantes = 0;
        foreach (var c in Celdas(mapa, w, h, Tipo.Arbol, true))
        {
            Vector3 p = Pos(c.x, c.y, w, h) + Jitter(0.45f);
            bool gigante = gigantes < MAX_ARBOLES_GIGANTES && arbolGigante.prefab != null &&
                           DistanciaA(mapa, w, h, c.x, c.y, Tipo.Limite, 7) > 6;
            float sep = gigante ? 9f : SEPARACION_BOSQUE;
            if (Cerca(troncos, p, sep)) continue;

            if (gigante)
            {
                Poner(arbolGigante, arboles, p, "Arbol_Gigante");
                gigantes++;
            }
            else
            {
                Poner(Elegir(arbolesBosque), arboles, p, "Arbol");
            }
            troncos.Add(p);
        }

        // ---- 3. Arbustos para tapar los huecos entre troncos del borde ----
        Progreso("Arbustos", 0.6f);
        var matas = new List<Vector3>();
        foreach (var c in Celdas(mapa, w, h, Tipo.Limite, true))
        {
            Vector3 p = Pos(c.x, c.y, w, h) + Jitter(0.35f);
            if (Cerca(troncos, p, 2.2f) || Cerca(matas, p, 1.7f)) continue;
            Poner(Elegir(arbustos), arbustosG, p, "Arbusto");
            matas.Add(p);
        }
        foreach (var c in Celdas(mapa, w, h, Tipo.Arbol, true))
        {
            if (rnd.NextDouble() > 0.12) continue;
            Vector3 p = Pos(c.x, c.y, w, h) + Jitter(0.4f);
            if (Cerca(troncos, p, 2f) || Cerca(matas, p, 2.5f)) continue;
            Poner(Elegir(arbustos), arbustosG, p, "Arbusto");
            matas.Add(p);
        }

        // ---- 4. Hierba alta (zonas verdes del boceto) ----
        Progreso("Hierba alta", 0.75f);
        foreach (var c in Celdas(mapa, w, h, Tipo.Pasto, false))
        {
            Vector3 b = Pos(c.x, c.y, w, h);
            Poner(Elegir(hierbaAlta), hierba, b + Jitter(0.3f), "Hierba");
            if (rnd.NextDouble() < 0.35)
                Poner(hierbaAlta[0], hierba, b + Jitter(0.45f), "Hierba");
        }

        // ---- 5. Decoración del suelo de los pasillos (sin colisión) ----
        Progreso("Decoración", 0.88f);
        foreach (var c in Celdas(mapa, w, h, Tipo.Piso, false))
        {
            Vector3 p = Pos(c.x, c.y, w, h) + Jitter(0.4f);
            bool juntoAlBorde = DistanciaA(mapa, w, h, c.x, c.y, Tipo.Limite, 1) <= 1;
            double r = rnd.NextDouble();

            if (juntoAlBorde && r < 0.05)
                Poner(Elegir(piedras), deco, p, "Piedra");
            else if (juntoAlBorde && r < 0.22)
                Poner(Elegir(decoSuelo), deco, p, "Planta");
            else if (!juntoAlBorde && r < 0.05)
                Poner(Elegir(decoSuelo, 2), deco, p, "Pasto_Bajo");
        }

        // ---- 6. Inicio y salida ----
        Vector3 salidaMin = Vector3.positiveInfinity, salidaMax = Vector3.negativeInfinity;
        bool haySalida = false;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (mapa[x, y] == (int)Tipo.Inicio)
                {
                    var ini = new GameObject("Inicio_Jugador");
                    ini.transform.SetParent(raiz, false);
                    ini.transform.localPosition = Pos(x, y, w, h);
                    ini.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                }
                else if (mapa[x, y] == (int)Tipo.Salida)
                {
                    Vector3 p = Pos(x, y, w, h);
                    salidaMin = Vector3.Min(salidaMin, p);
                    salidaMax = Vector3.Max(salidaMax, p);
                    haySalida = true;
                }
            }

        if (haySalida)
        {
            var meta = new GameObject("Meta_Salida");
            meta.transform.SetParent(raiz, false);
            meta.transform.localPosition = (salidaMin + salidaMax) * 0.5f + Vector3.up;
            var col = meta.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(salidaMax.x - salidaMin.x + 1f, 2f, salidaMax.z - salidaMin.z + 1f);
            meta.AddComponent<MetaNivel>();
        }

        Progreso("Escena", 0.97f);
        ConfigurarEscena(arboles);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkAllScenesDirty();
        Selection.activeGameObject = raiz.gameObject;

        Debug.Log("Laberinto de la selva creado (" + w + " x " + h + " m): " +
                  arboles.childCount + " árboles, " + arbustosG.childCount + " arbustos, " +
                  hierba.childCount + " matas de hierba alta, " + deco.childCount + " decoraciones.");
    }

    // ---------------- Suelo (mallas combinadas) ----------------

    static void Suelo(Transform raiz, int[,] mapa, int w, int h)
    {
        var colores = new Dictionary<string, Color>
        {
            { "TierraA", new Color(0.20f, 0.15f, 0.09f) },
            { "TierraB", new Color(0.17f, 0.13f, 0.08f) },
            { "Musgo", new Color(0.16f, 0.30f, 0.11f) },
            { "Hojarasca", new Color(0.12f, 0.10f, 0.06f) },
        };

        var partes = new Dictionary<string, MallaSimple>();
        foreach (var k in colores.Keys) partes[k] = new MallaSimple();

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int t = mapa[x, y];
                if (t < 0) continue;

                string mat;
                if (t == (int)Tipo.Pasto) mat = "Musgo";
                else if (t == (int)Tipo.Arbol || t == (int)Tipo.Limite) mat = "Hojarasca";
                else mat = ((x * 7 + y * 13) % 5 < 2) ? "TierraB" : "TierraA";

                Vector3 p = Pos(x, y, w, h);
                partes[mat].Quad(p, 1f);
            }

        var piso = Grupo(raiz, "Piso");
        foreach (var kv in partes)
        {
            if (kv.Value.Vacia) continue;
            var go = new GameObject("Piso_" + kv.Key);
            go.transform.SetParent(piso, false);
            go.AddComponent<MeshFilter>().sharedMesh = kv.Value.Guardar("Lab_Piso_" + kv.Key, true);
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = MaterialLit(kv.Key, colores[kv.Key]);
            mr.shadowCastingMode = ShadowCastingMode.Off;
        }
    }

    static void Colisiones(Transform raiz, int[,] mapa, int w, int h)
    {
        var go = new GameObject("Colisiones");
        go.transform.SetParent(raiz, false);

        // Suelo: una sola caja que cubre todo el mapa.
        var suelo = go.AddComponent<BoxCollider>();
        suelo.center = new Vector3(0f, -0.25f, 0f);
        suelo.size = new Vector3(w, 0.5f, h);

        // Bordes: cajas de ALTO_MURO uniendo celdas seguidas de cada fila.
        var muro = new MallaSimple();
        for (int y = 0; y < h; y++)
        {
            int x = 0;
            while (x < w)
            {
                if (!Bloquea(mapa[x, y])) { x++; continue; }
                int x0 = x;
                while (x < w && Bloquea(mapa[x, y])) x++;

                Vector3 a = Pos(x0, y, w, h);
                Vector3 b = Pos(x - 1, y, w, h);
                muro.Caja(new Vector3((a.x + b.x) * 0.5f, ALTO_MURO * 0.5f, a.z),
                          new Vector3(b.x - a.x + 1f, ALTO_MURO, 1f));
            }
        }

        var bordes = new GameObject("Bordes_Invisibles");
        bordes.transform.SetParent(go.transform, false);
        bordes.AddComponent<MeshCollider>().sharedMesh = muro.Guardar("Lab_Colision", false);
    }

    // Solo el borde necesita colisión: el bosque está siempre detrás de él.
    static bool Bloquea(int t) => t == (int)Tipo.Limite;

    // ---------------- Modelos ----------------

    static bool CargarModelos()
    {
        var todos = new List<Modelo>();
        todos.AddRange(arbolesBorde);
        todos.AddRange(arbolesBosque);
        todos.Add(arbolGigante);
        todos.AddRange(arbustos);
        todos.AddRange(hierbaAlta);
        todos.AddRange(decoSuelo);
        todos.AddRange(piedras);

        var faltan = new List<string>();
        foreach (var m in todos)
        {
            m.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ARTE + m.archivo + "_LOD0.fbx");
            if (m.prefab == null) { faltan.Add(m.archivo); continue; }

            // Mide el modelo tal como se importa (con la rotación del FBX incluida).
            var tmp = (GameObject)PrefabUtility.InstantiatePrefab(m.prefab);
            tmp.transform.position = Vector3.zero;
            var rs = tmp.GetComponentsInChildren<Renderer>();
            Bounds b = rs.Length > 0 ? rs[0].bounds : new Bounds(Vector3.zero, Vector3.one);
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            Object.DestroyImmediate(tmp);

            m.alto = Mathf.Max(b.size.y, 0.01f);
            m.radio = Mathf.Max(b.extents.x, b.extents.z);
        }

        if (faltan.Count > 0)
        {
            EditorUtility.DisplayDialog("Saatan",
                "Faltan estos modelos en " + ARTE + ":\n\n" + string.Join("\n", faltan), "OK");
            return false;
        }
        return true;
    }

    static GameObject Poner(Modelo m, Transform padre, Vector3 pos, string nombre)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(m.prefab, padre);
        go.name = nombre + "_" + m.archivo;

        float alto = Mathf.Lerp(m.min, m.max, (float)rnd.NextDouble());
        float k = alto / m.alto;

        Quaternion baseRot = m.prefab.transform.localRotation;
        Vector3 baseEsc = m.prefab.transform.localScale;

        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f) * baseRot;
        go.transform.localScale = baseEsc * k;

        foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
        {
            var mats = r.sharedMaterials;
            bool cambiar = false;
            for (int i = 0; i < mats.Length; i++)
                if (mats[i] != matVoxel) { mats[i] = matVoxel; cambiar = true; }
            if (cambiar) r.sharedMaterials = mats;
            r.shadowCastingMode = ShadowCastingMode.On;
            r.lightProbeUsage = LightProbeUsage.Off;
            r.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }
        return go;
    }

    static Modelo Elegir(Modelo[] lista, int maxIndice = int.MaxValue)
    {
        int n = Mathf.Min(lista.Length, maxIndice);
        float total = 0f;
        for (int i = 0; i < n; i++) total += lista[i].peso;
        float r = (float)rnd.NextDouble() * total;
        for (int i = 0; i < n; i++)
        {
            r -= lista[i].peso;
            if (r <= 0f) return lista[i];
        }
        return lista[n - 1];
    }

    // ---------------- Materiales ----------------

    static Material MaterialVoxel()
    {
        string ruta = CARPETA + "/Mat_Vegetacion_Voxel.mat";

        Shader sh = Shader.Find("Saatan/VegetacionVoxel");
        if (sh == null || ShaderUtil.ShaderHasError(sh))
        {
            Debug.LogWarning("El shader Saatan/VegetacionVoxel no compiló; se usa Particles/Simple Lit (colores de vértice).");
            sh = Shader.Find("Universal Render Pipeline/Particles/Simple Lit");
        }

        var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null)
        {
            m = new Material(sh);
            AssetDatabase.CreateAsset(m, ruta);
        }
        else if (m.shader != sh)
        {
            m.shader = sh;
        }

        m.enableInstancing = true;
        EditorUtility.SetDirty(m);
        return m;
    }

    // Hace que todos los FBX de Assets/Art usen el material voxel en vez del que traen.
    static void UsarMaterialEnFBX(Material mat)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/Art" }))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            var imp = AssetImporter.GetAtPath(ruta) as ModelImporter;
            if (imp == null) continue;

            var mapa = imp.GetExternalObjectMap();
            bool cambio = false;

            foreach (var o in AssetDatabase.LoadAllAssetRepresentationsAtPath(ruta))
            {
                if (!(o is Material)) continue;
                var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), o.name);
                if (mapa.TryGetValue(id, out var actual) && actual == mat) continue;
                imp.AddRemap(id, mat);
                cambio = true;
            }

            // Si los materiales ya estaban remapeados no aparecen como sub-asset: revisa el nombre conocido.
            var voxel = new AssetImporter.SourceAssetIdentifier(typeof(Material), "Floresta_Voxel");
            if (mapa.TryGetValue(voxel, out var previo) && previo != mat)
            {
                imp.AddRemap(voxel, mat);
                cambio = true;
            }

            if (cambio) imp.SaveAndReimport();
        }
    }

    static Material MaterialLit(string nombre, Color color)
    {
        string ruta = CARPETA + "/Mat_" + nombre + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            m = new Material(sh);
            AssetDatabase.CreateAsset(m, ruta);
        }
        m.color = color;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.05f);
        EditorUtility.SetDirty(m);
        return m;
    }

    // ---------------- Escena ----------------

    static void ConfigurarEscena(Transform arboles)
    {
        Color niebla = new Color(0.10f, 0.20f, 0.13f);

        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.30f, 0.36f, 0.30f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = niebla;
        RenderSettings.fogDensity = DENSIDAD_NIEBLA;

        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type != LightType.Directional) continue;
            l.intensity = 0.9f;
            l.color = new Color(1f, 0.95f, 0.80f);
            l.shadows = LightShadows.None;
            l.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
        }

        var cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = niebla;
            cam.farClipPlane = 200f;
            cam.transform.rotation = Quaternion.Euler(65f, 0f, 0f);

            var ocultar = cam.GetComponent<OcultarCopas>();
            if (ocultar == null) ocultar = cam.gameObject.AddComponent<OcultarCopas>();
            ocultar.arboles = arboles;
        }
    }

    // ---------------- Utilidades ----------------

    static void CrearCarpetas()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Niveles")) AssetDatabase.CreateFolder("Assets", "Niveles");
        if (!AssetDatabase.IsValidFolder(CARPETA)) AssetDatabase.CreateFolder("Assets/Niveles", "Selva");
    }

    static Transform Grupo(Transform padre, string nombre)
    {
        var t = new GameObject(nombre).transform;
        t.SetParent(padre, false);
        return t;
    }

    static Vector3 Pos(int x, int y, int w, int h) =>
        new Vector3(x - w / 2f + 0.5f, 0f, y - h / 2f + 0.5f);

    static Vector3 Jitter(float a) =>
        new Vector3(((float)rnd.NextDouble() * 2f - 1f) * a, 0f, ((float)rnd.NextDouble() * 2f - 1f) * a);

    // Celdas de un tipo; en orden aleatorio si se pide (para que los árboles no queden en filas).
    static List<Vector2Int> Celdas(int[,] mapa, int w, int h, Tipo t, bool mezclar)
    {
        var l = new List<Vector2Int>();
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (mapa[x, y] == (int)t) l.Add(new Vector2Int(x, y));

        if (mezclar)
            for (int i = l.Count - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                (l[i], l[j]) = (l[j], l[i]);
            }
        return l;
    }

    static bool Cerca(List<Vector3> puntos, Vector3 p, float dist)
    {
        float d2 = dist * dist;
        foreach (var q in puntos)
        {
            float dx = q.x - p.x, dz = q.z - p.z;
            if (dx * dx + dz * dz < d2) return true;
        }
        return false;
    }

    // Distancia (en celdas, cuadrada) a la celda más cercana de un tipo, hasta 'max'.
    static int DistanciaA(int[,] mapa, int w, int h, int cx, int cy, Tipo t, int max)
    {
        for (int r = 0; r <= max; r++)
            for (int y = cy - r; y <= cy + r; y++)
                for (int x = cx - r; x <= cx + r; x++)
                {
                    if (x < 0 || y < 0 || x >= w || y >= h) continue;
                    if (Mathf.Max(Mathf.Abs(x - cx), Mathf.Abs(y - cy)) != r) continue;
                    if (mapa[x, y] == (int)t) return r;
                }
        return max + 1;
    }

    static void Progreso(string paso, float t) =>
        EditorUtility.DisplayProgressBar("Saatan - Selva", paso, t);

    static Tipo Clasificar(Color32 p)
    {
        Tipo mejor = Tipo.Piso;
        int mejorDist = int.MaxValue;
        foreach (var (c, t) in paleta)
        {
            int dr = p.r - c.r, dg = p.g - c.g, db = p.b - c.b;
            int d = dr * dr + dg * dg + db * db;
            if (d < mejorDist) { mejorDist = d; mejor = t; }
        }
        return mejor;
    }

    // Malla armada a mano (cuadros para el piso, cajas para la colisión).
    class MallaSimple
    {
        readonly List<Vector3> v = new List<Vector3>();
        readonly List<Vector3> n = new List<Vector3>();
        readonly List<Vector2> uv = new List<Vector2>();
        readonly List<int> tri = new List<int>();

        public bool Vacia => v.Count == 0;

        public void Quad(Vector3 c, float lado)
        {
            float s = lado * 0.5f;
            int i = v.Count;
            v.Add(c + new Vector3(-s, 0f, -s));
            v.Add(c + new Vector3(-s, 0f, s));
            v.Add(c + new Vector3(s, 0f, s));
            v.Add(c + new Vector3(s, 0f, -s));
            for (int k = 0; k < 4; k++) n.Add(Vector3.up);
            uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(0, 1)); uv.Add(new Vector2(1, 1)); uv.Add(new Vector2(1, 0));
            tri.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
        }

        public void Caja(Vector3 c, Vector3 tam)
        {
            Vector3 e = tam * 0.5f;
            int i = v.Count;
            for (int k = 0; k < 8; k++)
            {
                v.Add(c + new Vector3((k & 1) == 0 ? -e.x : e.x, (k & 2) == 0 ? -e.y : e.y, (k & 4) == 0 ? -e.z : e.z));
                n.Add(Vector3.up);
                uv.Add(Vector2.zero);
            }
            int[] caras =
            {
                0, 2, 3, 0, 3, 1,   // -z
                4, 5, 7, 4, 7, 6,   // +z
                0, 4, 6, 0, 6, 2,   // -x
                1, 3, 7, 1, 7, 5,   // +x
                0, 1, 5, 0, 5, 4,   // -y
                2, 6, 7, 2, 7, 3,   // +y
            };
            foreach (int k in caras) tri.Add(i + k);
        }

        public Mesh Guardar(string nombre, bool visual)
        {
            var m = new Mesh { name = nombre, indexFormat = IndexFormat.UInt32 };
            m.SetVertices(v);
            if (visual) { m.SetNormals(n); m.SetUVs(0, uv); }
            m.SetTriangles(tri, 0);
            if (!visual) m.RecalculateNormals();
            m.RecalculateBounds();

            string ruta = CARPETA + "/Malla_" + nombre + ".asset";
            if (AssetDatabase.LoadAssetAtPath<Mesh>(ruta) != null) AssetDatabase.DeleteAsset(ruta);
            AssetDatabase.CreateAsset(m, ruta);
            return m;
        }
    }
}
