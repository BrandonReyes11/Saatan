// Saatan Historia - Escena CinematicaIglesia: la iglesia (Art/Iglesia, ch_iso) sobre una pequeña colina,
// escaleras de piedra para subir, cementerio alrededor, árboles y neblina que oculta el borde del mapa.
// Iktan y Abbi suben la escalera y llegan a la puerta gigante (hecha con cubos, cierra el hueco de la entrada).
// La cinemática es un Timeline (objeto "Cinematica"); al terminar pasa a NivelIglesia.
// Menú: Saatan > Escenas > Crear CinematicaIglesia  (crea Assets/Project/Scenes/CinematicaIglesia.unity y la agrega al Build)
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class ConstructorCinematica
{
    const string ESCENA = "Assets/Project/Scenes/CinematicaIglesia.unity";
    const string ARTE = "Assets/Project/Prefabs/Environment/Iglesia";
    const string CARPETA = "Assets/Project/Niveles/Cinematica";
    const float ALTO = 14f;           // altura de la colina
    const float R_CIMA = 34f;         // radio de la cima plana
    const float SUBIDA = 40f;         // largo de la escalera (y de la ladera)
    const float ANCHO = 7f;           // ancho de la escalera
    const int ESCALONES = 40;
    const float ESCALA_IGLESIA = 3f;  // 1 vóxel = 0.3 m: la iglesia mide ~26 x 37 x 40 m
    static readonly Color NIEBLA = new Color(0.45f, 0.48f, 0.56f);

    static System.Random rnd;
    static Transform raiz;
    static readonly Dictionary<string, Material> materiales = new Dictionary<string, Material>();

    [MenuItem("Saatan/Escenas/Crear CinematicaIglesia")]
    static void Crear()
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!AssetDatabase.IsValidFolder(CARPETA)) AssetDatabase.CreateFolder("Assets/Project/Niveles", "Cinematica");

        var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        rnd = new System.Random(31);
        materiales.Clear();
        raiz = new GameObject("Cinematica_Iglesia").transform;

        Colina();
        Escalera();
        Poner("ch_iso", new Vector3(0f, ALTO, 0f), 0f, ESCALA_IGLESIA);   // fachada hacia -Z, hacia la escalera
        Puerta();
        Cementerio();
        Arboles();
        Ambiente();
        Timeline();

        EditorSceneManager.SaveScene(escena, ESCENA);
        var lista = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!lista.Exists(s => s.path == ESCENA)) lista.Add(new EditorBuildSettingsScene(ESCENA, true));
        EditorBuildSettings.scenes = lista.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("CinematicaIglesia creada: " + ESCENA);
    }

    // ---------------- Colina ----------------

    // Altura del terreno: cima plana, ladera suave con algo de ruido y llano alrededor.
    // En el camino de la escalera (frente, -Z) el terreno baja en línea recta debajo de los escalones.
    static float Altura(float x, float z)
    {
        float r = Mathf.Sqrt(x * x + z * z), s = Mathf.Clamp01((r - R_CIMA) / SUBIDA);
        float h = ALTO * (1f - s * s * (3f - 2f * s)) + Mathf.Sin(s * Mathf.PI) * 1.5f * (Mathf.PerlinNoise(x * 0.05f + 7f, z * 0.05f + 3f) - 0.5f);
        if (Mathf.Abs(x) < ANCHO / 2f + 2f && z < -R_CIMA + 1f)
            h = Mathf.Min(h, ALTO * Mathf.Clamp01(1f - (-z - R_CIMA) / SUBIDA) - 0.2f);
        return h;
    }

    static void Colina()
    {
        var radios = new List<float>();
        for (float r = 0f; r < R_CIMA; r += 4f) radios.Add(r);
        for (float r = R_CIMA; r < R_CIMA + SUBIDA; r += 1.5f) radios.Add(r);
        for (float r = R_CIMA + SUBIDA; r <= 260f; r += 10f) radios.Add(r);
        const int lados = 192;

        var v = new List<Vector3>(); var col = new List<Color>(); var tri = new List<int>();
        foreach (float r in radios)
            for (int j = 0; j <= lados; j++)
            {
                float a = j * Mathf.PI * 2f / lados, x = Mathf.Cos(a) * r, z = Mathf.Sin(a) * r;
                v.Add(new Vector3(x, Altura(x, z), z));
                bool camino = Mathf.Abs(x) < ANCHO / 2f + 2f && z < -R_CIMA + 1f && z > -R_CIMA - SUBIDA;
                Color c = camino ? new Color(0.36f, 0.30f, 0.22f) : new Color(0.30f, 0.46f, 0.20f);
                col.Add(c * (0.8f + 0.4f * Mathf.PerlinNoise(x * 0.15f, z * 0.15f)));
            }
        for (int i = 0; i + 1 < radios.Count; i++)
            for (int j = 0; j < lados; j++)
            {
                int a = i * (lados + 1) + j, b = a + 1, c = a + lados + 2, d = a + lados + 1;
                tri.AddRange(new[] { a, b, c, a, c, d });
            }

        var m = new Mesh { name = "Colina", indexFormat = IndexFormat.UInt32 };
        m.SetVertices(v);
        m.SetColors(col);
        m.SetTriangles(tri, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        Guardar(m, CARPETA + "/Malla_Colina.asset");

        var go = new GameObject("Colina");
        go.transform.SetParent(raiz, false);
        go.AddComponent<MeshFilter>().sharedMesh = m;
        go.AddComponent<MeshRenderer>().sharedMaterial = MaterialVoxel();   // colores por vértice
        go.AddComponent<MeshCollider>().sharedMesh = m;
        go.isStatic = true;
    }

    // ---------------- Escalera ----------------

    static void Escalera()
    {
        var piedra = Lit("Piedra", new Color(0.55f, 0.54f, 0.52f));
        var oscura = Lit("PiedraOscura", new Color(0.38f, 0.37f, 0.36f));
        var g = new GameObject("Escalera").transform;
        g.SetParent(raiz, false);
        float alto = ALTO / ESCALONES, fondo = SUBIDA / ESCALONES;

        for (int k = 0; k < ESCALONES; k++)
        {
            float tope = (k + 1) * alto, z = -(R_CIMA + SUBIDA) + (k + 0.5f) * fondo;
            Cubo(g, new Vector3(0f, tope / 2f, z), new Vector3(ANCHO, tope, fondo + 0.02f), k % 2 == 0 ? piedra : oscura);
            foreach (float s in new[] { -1f, 1f })   // barandal de piedra
                Cubo(g, new Vector3(s * (ANCHO / 2f + 0.35f), (tope + 0.9f) / 2f, z), new Vector3(0.7f, tope + 0.9f, fondo + 0.02f), oscura);
            if (k % 8 == 4)
                foreach (float s in new[] { -1f, 1f }) Farol(new Vector3(s * (ANCHO / 2f + 0.35f), tope + 0.9f, z));
        }

        // Camino empedrado de la escalera a la puerta, y postes de entrada.
        Cubo(g, new Vector3(0f, ALTO + 0.05f, -(R_CIMA + 20f) / 2f), new Vector3(ANCHO, 0.3f, R_CIMA - 20f + 1f), piedra);
        foreach (float s in new[] { -1f, 1f })
        {
            Poner("gy_wallpost", new Vector3(s * (ANCHO / 2f + 0.6f), ALTO, -R_CIMA + 0.5f), 0f, 2f);
            Farol(new Vector3(s * (ANCHO / 2f + 0.6f), ALTO + 3.4f, -R_CIMA + 0.5f));
        }
    }

    static void Farol(Vector3 pos)
    {
        var f = Poner("lantern", pos, 0f, 2.5f);
        var luz = new GameObject("Luz").AddComponent<Light>();
        luz.transform.SetParent(f, false);
        luz.transform.position = pos + Vector3.up * 0.6f;
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.75f, 0.45f);
        luz.range = 9f;
        luz.intensity = 2.5f;
        luz.shadows = LightShadows.None;
    }

    // ---------------- Cementerio y árboles ----------------

    static void Cementerio()
    {
        string[] tumbas = { "gy_tombstone1", "gy_tombstone1", "gy_dirtgrave", "gy_monu1", "gy_monu2" };
        foreach (float s in new[] { -1f, 1f })
            for (float z = -26f; z <= 14f; z += 4.5f)
                for (float x = 17f; x <= 28f; x += 3.5f)
                {
                    if (rnd.NextDouble() < 0.3 || x * x + z * z > (R_CIMA - 3f) * (R_CIMA - 3f)) continue;
                    var p = new Vector3(s * x + Azar(0.6f), ALTO, z + Azar(0.6f));
                    string t = tumbas[rnd.Next(tumbas.Length)];
                    Poner(t, p, 180f + Azar(10f), t.StartsWith("gy_monu") ? 1.4f : 1.2f);
                }

        foreach (var (nombre, pos, esc) in new[] {
            ("crypt", new Vector3(-24f, ALTO, 20f), 2f), ("crypt", new Vector3(23f, ALTO, 19f), 2f),
            ("skullpile", new Vector3(-12f, ALTO, -24f), 1.5f), ("stonepillarfallen", new Vector3(13f, ALTO, -27f), 2f),
            ("wagon", new Vector3(16f, ALTO, -14f), 2f), ("pumpkin", new Vector3(-6f, ALTO, -26f), 1.2f),
            ("pumpkin", new Vector3(6.5f, ALTO, -29f), 1.2f), ("mushroom", new Vector3(-9f, ALTO, -30f), 2f) })
            Poner(nombre, pos, rnd.Next(360), esc);

        // Reja de hierro alrededor del cementerio, en el borde de la cima (sin tapar la escalera).
        for (float a = 0f; a < 360f; a += 5f)
        {
            if (Mathf.Abs(Mathf.DeltaAngle(a, 270f)) < 12f || rnd.NextDouble() < 0.15) continue;   // 270° = frente (-Z)
            var p = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), 0f, Mathf.Sin(a * Mathf.Deg2Rad)) * (R_CIMA - 1f);
            Poner(rnd.NextDouble() < 0.5 ? "gy_fencemetalal" : "gy_fencemetal2", p + Vector3.up * ALTO, -a + 90f, 2f);
        }
    }

    static void Arboles()
    {
        for (int i = 0; i < 70; i++)
        {
            float a = (float)rnd.NextDouble() * 360f, r = Mathf.Lerp(R_CIMA - 4f, R_CIMA + SUBIDA + 35f, (float)rnd.NextDouble());
            if (Mathf.Abs(Mathf.DeltaAngle(a, 270f)) < 14f) continue;   // sin árboles sobre la escalera
            float x = Mathf.Cos(a * Mathf.Deg2Rad) * r, z = Mathf.Sin(a * Mathf.Deg2Rad) * r;
            if (r < R_CIMA && z < 5f && Mathf.Abs(x) < 16f) continue;  // frente de la iglesia libre
            Poner("ch_tree", new Vector3(x, Altura(x, z) - 0.3f, z), rnd.Next(360), Mathf.Lerp(2.5f, 3.5f, (float)rnd.NextDouble()));
        }
    }

    // ---------------- Luz, neblina y cámara ----------------

    // ---------------- Puerta gigante ----------------

    // Hueco de la entrada en ch_iso (vóxeles): x ±1.85, de y -6.0 a -3.7, la cara del arco en z -5.55.
    // Puerta de dos hojas de tablones con herrajes, un arco escalonado arriba y un bloque oscuro detrás.
    static void Puerta()
    {
        float E = ESCALA_IGLESIA, ancho = 3.7f * E, abajo = ALTO + 0.2f * E, alto = 2.3f * E, z = -5.5f * E + 0.15f;
        var g = new GameObject("Puerta_Gigante").transform;
        g.SetParent(raiz, false);
        g.localPosition = new Vector3(0f, abajo, z);
        var madera = Lit("PuertaMadera", new Color(0.32f, 0.18f, 0.09f));
        var maderaOsc = Lit("PuertaMaderaOscura", new Color(0.22f, 0.12f, 0.06f));
        var hierro = Lit("PuertaHierro", new Color(0.12f, 0.12f, 0.13f));

        int tablas = 12;
        for (int i = 0; i < tablas; i++)   // tablones verticales, uno sí y uno no más oscuro
        {
            float x = -ancho / 2f + (i + 0.5f) * ancho / tablas;
            Cubo(g, new Vector3(x, alto / 2f, 0.3f), new Vector3(ancho / tablas - 0.04f, alto, 0.6f), i % 2 == 0 ? madera : maderaOsc);
        }
        for (int k = 0; k < 3; k++)   // arco escalonado que tapa la parte de arriba
        {
            float w = ancho * (0.8f - k * 0.22f);
            Cubo(g, new Vector3(0f, alto + 0.35f + k * 0.7f, 0.3f), new Vector3(w, 0.7f, 0.6f), k % 2 == 0 ? maderaOsc : madera);
        }
        Cubo(g, new Vector3(0f, alto / 2f + 0.6f, 0.25f), new Vector3(0.25f, alto + 1.2f, 0.65f), hierro);   // unión de las dos hojas
        foreach (float y in new[] { 0.18f, 0.5f, 0.82f })   // bandas de hierro con clavos
        {
            Cubo(g, new Vector3(0f, alto * y, -0.02f), new Vector3(ancho, 0.25f, 0.1f), hierro);
            for (float x = -ancho / 2f + 0.4f; x < ancho / 2f; x += 0.8f)
                Cubo(g, new Vector3(x, alto * y, -0.1f), new Vector3(0.14f, 0.14f, 0.1f), hierro);
        }
        foreach (float s in new[] { -1f, 1f })   // argollas
        {
            Cubo(g, new Vector3(s * 0.7f, alto * 0.45f, -0.12f), new Vector3(0.5f, 0.08f, 0.08f), hierro);
            Cubo(g, new Vector3(s * 0.7f, alto * 0.45f - 0.25f, -0.12f), new Vector3(0.08f, 0.5f, 0.08f), hierro);
        }
        // Bloque oscuro detrás: aunque algo se asome por un hueco, la iglesia no se ve hueca.
        Cubo(g, new Vector3(0f, alto * 0.75f, 6f), new Vector3(ancho + 1f, alto * 1.5f + 1f, 11f), Lit("Interior", new Color(0.03f, 0.03f, 0.035f)));
    }

    // ---------------- Timeline: Iktan y Abbi suben y la cámara los sigue ----------------

    static void Timeline()
    {
        var d = PiezasCinematica.Director(raiz, "Iglesia", "NivelIglesia", out var tl);
        // Línea de la escalera: de y 0 (z -74) a y ALTO (z -34); luego el camino y el pórtico hasta la puerta.
        System.Func<float, float, Vector3[]> Ruta = (x, z0) => new[]
        {
            new Vector3(x, 0.05f, z0), new Vector3(x, 0.2f, -(R_CIMA + SUBIDA) - 0.5f), new Vector3(x, ALTO + 0.2f, -R_CIMA + 0.5f),
            new Vector3(x, ALTO + 0.2f, -21.5f), new Vector3(x * 0.6f, ALTO + 0.65f, -18.6f),
        };
        const float vel = 2f, espera = 1.5f;
        Vector3[] rutaIktan = Ruta(-0.9f, -82f), rutaAbbi = Ruta(1.1f, -83.5f);
        float dur = espera + PiezasCinematica.LargoRuta(rutaAbbi) / vel + 3.5f;   // + unos segundos frente a la puerta

        foreach (var (archivo, ruta, alto) in new[] { ("IktanVoxel", rutaIktan, 1.4f), ("AbbiVoxel", rutaAbbi, 0.55f) })
        {
            var p = PiezasCinematica.Personaje(archivo, raiz, ruta[0], 0f, alto);
            p.gameObject.AddComponent<AndarVoxel>().pasosPorMetro = archivo == "AbbiVoxel" ? 4f : 2.2f;
            PiezasCinematica.PistaMovimiento(tl, d, p.gameObject, dur, t =>
            {
                var (pos, dir) = PiezasCinematica.EnRuta(ruta, vel, espera, t);
                dir.y = 0f;
                return (pos, Quaternion.LookRotation(dir.sqrMagnitude > 0.0001f ? dir : Vector3.forward));
            });
        }

        // Cámara: va detrás de Iktan con suavizado y en los últimos segundos levanta la vista hacia la puerta.
        var cam = Camera.main;
        if (cam == null) return;
        Vector3 distancia = new Vector3(-3.5f, 3.2f, -7.5f), objetivo = new Vector3(0f, ALTO + 7f, -16f);
        const int fps = 30;
        int n = Mathf.CeilToInt(dur * fps) + 1;
        var posiciones = new Vector3[n];
        var rotaciones = new Quaternion[n];
        Vector3 c = rutaIktan[0] + distancia;
        Quaternion r = Quaternion.LookRotation(rutaIktan[0] + Vector3.up - c);
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)fps;
            Vector3 iktan = PiezasCinematica.EnRuta(rutaIktan, vel, espera, t).pos;
            c = Vector3.Lerp(c, iktan + distancia, 1f - Mathf.Exp(-2f / fps));
            Vector3 mira = Vector3.Lerp(iktan + Vector3.up, objetivo, Mathf.SmoothStep(0f, 1f, (t / dur - 0.8f) / 0.2f));
            r = Quaternion.Slerp(r, Quaternion.LookRotation(mira - c), 1f - Mathf.Exp(-5f / fps));
            posiciones[i] = c;
            rotaciones[i] = r;
        }
        cam.transform.SetPositionAndRotation(posiciones[0], rotaciones[0]);
        PiezasCinematica.PistaMovimiento(tl, d, cam.gameObject, dur, t =>
        {
            int i = Mathf.Clamp(Mathf.RoundToInt(t * fps), 0, n - 1);
            return (posiciones[i], rotaciones[i]);
        }, 0.2f);
    }

    static void Ambiente()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.42f, 0.44f, 0.52f);
        // Neblina: a partir de ~40 m empieza y a 170 m ya no se ve nada; el suelo llega a 260 m.
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = NIEBLA;
        RenderSettings.fogStartDistance = 40f;
        RenderSettings.fogEndDistance = 170f;

        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional)
            {
                l.intensity = 0.9f;
                l.color = new Color(0.85f, 0.8f, 0.95f);
                l.shadows = LightShadows.Soft;
                l.transform.rotation = Quaternion.Euler(30f, 20f, 0f);   // ilumina la fachada
            }

        var cam = Camera.main;
        if (cam == null) return;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = NIEBLA;
        cam.farClipPlane = 200f;
    }

    // ---------------- Utilidades ----------------

    // Instancia un prefab de Art/Iglesia, cambia su material (Standard) por uno URP y lo apoya en el suelo.
    static Transform Poner(string nombre, Vector3 pos, float giro, float escala)
    {
        var archivos = Directory.GetFiles(ARTE, "church-*-" + nombre + ".prefab");
        if (archivos.Length == 0) { Debug.LogWarning("No se encontró " + nombre + " en " + ARTE); return raiz; }
        var pf = AssetDatabase.LoadAssetAtPath<GameObject>(archivos[0].Replace('\\', '/'));
        var t = ((GameObject)PrefabUtility.InstantiatePrefab(pf, raiz)).transform;
        t.localPosition = pos;
        t.localRotation = Quaternion.Euler(0f, giro, 0f);
        t.localScale = Vector3.one * escala;

        var rs = t.GetComponentsInChildren<Renderer>();
        foreach (var r in rs)
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = URP(nombre, mats[i]);
            r.sharedMaterials = mats;
        }
        if (rs.Length > 0)
        {
            Bounds b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            t.position += Vector3.up * (pos.y - b.min.y);   // la base del modelo queda en pos.y
        }
        return t;
    }

    static Material URP(string nombre, Material original)
    {
        if (original == null || original.shader.name.StartsWith("Universal")) return original;
        if (materiales.TryGetValue(nombre, out var m)) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = nombre };
        m.SetTexture("_BaseMap", original.mainTexture);
        m.SetFloat("_Smoothness", 0.05f);
        return materiales[nombre] = Guardar(m, CARPETA + "/Mat_" + nombre + ".mat");
    }

    static Material Lit(string nombre, Color c)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = c };
        m.SetFloat("_Smoothness", 0.1f);
        return Guardar(m, CARPETA + "/Mat_" + nombre + ".mat");
    }

    static Material MaterialVoxel()
    {
        Shader sh = Shader.Find("Saatan/VegetacionVoxel");
        if (sh == null || ShaderUtil.ShaderHasError(sh)) sh = Shader.Find("Universal Render Pipeline/Particles/Simple Lit");
        return Guardar(new Material(sh), CARPETA + "/Mat_Colina.mat");
    }

    static void Cubo(Transform padre, Vector3 pos, Vector3 esc, Material m)
    {
        var c = GameObject.CreatePrimitive(PrimitiveType.Cube);   // conserva su BoxCollider
        c.transform.SetParent(padre, false);
        c.transform.localPosition = pos;
        c.transform.localScale = esc;
        c.GetComponent<MeshRenderer>().sharedMaterial = m;
        c.isStatic = true;
    }

    static float Azar(float a) => ((float)rnd.NextDouble() * 2f - 1f) * a;

    static A Guardar<A>(A asset, string ruta) where A : Object
    {
        AssetDatabase.DeleteAsset(ruta);
        AssetDatabase.CreateAsset(asset, ruta);
        return asset;
    }
}
