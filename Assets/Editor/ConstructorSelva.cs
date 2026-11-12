
// Saatan Historia - Nivel II: La selva.
// Colocar en: Assets/Editor/ConstructorSelva.cs
// Se usa con la imagen selva_detallada.png.
// Solo construye el escenario (visual y colisiones).

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class ConstructorSelva
{
    enum Tipo { Piso, Limite, Pasto, Arbol, Tronco, Ruina, Columna, Trampa, Nahual, Rama, Trompos, Inicio, Salida }

    // ---- Ajustes ----
    const float ALTURA_TECHO = 4.6f;
    const bool TECHO_TRANSPARENTE = true;
    const float DENSIDAD_NIEBLA = 0.018f;

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

    static readonly Vector2Int[] dirs =
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1)
    };

    static readonly Dictionary<string, Color> colores = new Dictionary<string, Color>
    {
        { "TierraA", new Color(0.20f, 0.15f, 0.09f) },
        { "TierraB", new Color(0.16f, 0.12f, 0.07f) },
        { "LosaA", new Color(0.28f, 0.30f, 0.27f) },
        { "LosaB", new Color(0.23f, 0.25f, 0.22f) },
        { "PastoA", new Color(0.16f, 0.42f, 0.12f) },
        { "PastoB", new Color(0.24f, 0.54f, 0.15f) },
        { "Corteza", new Color(0.22f, 0.14f, 0.09f) },
        { "CortezaClara", new Color(0.36f, 0.25f, 0.15f) },
        { "Raiz", new Color(0.27f, 0.18f, 0.10f) },
        { "Liana", new Color(0.14f, 0.36f, 0.12f) },
        { "Musgo", new Color(0.20f, 0.42f, 0.14f) },
        { "Piedra", new Color(0.45f, 0.46f, 0.42f) },
        { "PiedraOscura", new Color(0.30f, 0.31f, 0.29f) },
        { "TechoRoca", new Color(0.42f, 0.43f, 0.40f, 0.55f) },
        { "Grieta", new Color(0.05f, 0.05f, 0.05f) },
        { "Madera", new Color(0.70f, 0.47f, 0.24f) },
        { "TrompoRojo", new Color(0.85f, 0.15f, 0.15f) },
        { "TrompoAzul", new Color(0.15f, 0.35f, 0.85f) },
        { "TrompoVerde", new Color(0.15f, 0.65f, 0.25f) },
        { "TrompoAmarillo", new Color(0.95f, 0.80f, 0.15f) },
        { "Brillo", new Color(1.00f, 0.80f, 0.30f) },
        { "MarcadorInicio", new Color(0.00f, 0.40f, 1.00f) },
    };

    static readonly Dictionary<string, Material> cache =
        new Dictionary<string, Material>();

    [MenuItem("Saatan/Construir selva (nivel II)")]
    static void Construir()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Saatan",
                "Sal del modo Play antes de construir la selva.",
                "OK"
            );
            return;
        }

        var tex = Selection.activeObject as Texture2D;

        if (tex == null)
        {
            EditorUtility.DisplayDialog(
                "Saatan",
                "Primero selecciona selva_detallada.png en la ventana Project y vuelve a intentarlo.",
                "OK"
            );
            return;
        }

        ConstruirDesdeTextura(tex);
    }

    // Construye la selva en la escena abierta a partir de la imagen del mapa.
    // Lo usa también ConstructorEscenas para generar la escena Nivel_Selva.
    public static void ConstruirDesdeTextura(Texture2D tex)
    {
        string ruta = AssetDatabase.GetAssetPath(tex);
        var imp = (TextureImporter)AssetImporter.GetAtPath(ruta);

        imp.isReadable = true;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.npotScale = TextureImporterNPOTScale.None;
        imp.SaveAndReimport();

        tex = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);

        Color32[] px = tex.GetPixels32();
        int w = tex.width;
        int h = tex.height;

        if (!AssetDatabase.IsValidFolder("Assets/Niveles"))
            AssetDatabase.CreateFolder("Assets", "Niveles");

        if (!AssetDatabase.IsValidFolder("Assets/Niveles/Selva"))
            AssetDatabase.CreateFolder("Assets/Niveles", "Selva");

        cache.Clear();

        var viejo = GameObject.Find("Selva_Detallada");

        if (viejo != null)
            Object.DestroyImmediate(viejo);

        var raiz = new GameObject("Selva_Detallada").transform;

        // Mapa de tipos (-1 = vacío).
        var mapa = new int[w, h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Color32 p = px[y * w + x];
                mapa[x, y] = p.a < 128 ? -1 : (int)Clasificar(p);
            }
        }

        var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Mesh cubo = tmp.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(tmp);

        tmp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Mesh cilindro = tmp.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(tmp);

        var piso = new Dictionary<string, List<CombineInstance>>();
        var deco = new Dictionary<string, List<CombineInstance>>();
        var colision = new List<CombineInstance>();

        // ---- Piso, colisiones, árboles del borde y hierba ----
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (mapa[x, y] < 0) continue;

                Tipo t = (Tipo)mapa[x, y];
                Vector3 p = Pos(x, y, w, h);

                bool losa = t == Tipo.Ruina ||
                            t == Tipo.Columna ||
                            t == Tipo.Trampa;

                string mat = losa
                    ? (((x + y) % 2 == 0) ? "LosaA" : "LosaB")
                    : (((x + y) % 2 == 0) ? "TierraA" : "TierraB");

                SumarTRS(
                    Lista(piso, mat),
                    cubo,
                    new Vector3(p.x, -0.1f, p.z),
                    Quaternion.identity,
                    new Vector3(0.98f, 0.2f, 0.98f)
                );

                SumarTRS(
                    colision,
                    cubo,
                    new Vector3(p.x, -0.1f, p.z),
                    Quaternion.identity,
                    new Vector3(1f, 0.2f, 1f)
                );

                if (t == Tipo.Limite)
                {
                    SumarTRS(
                        colision,
                        cubo,
                        new Vector3(p.x, 3f, p.z),
                        Quaternion.identity,
                        new Vector3(1f, 6f, 1f)
                    );

                    BordeArbol(deco, cubo, cilindro, p, x * 73 + y * 131);
                }
                else if (t == Tipo.Pasto)
                {
                    PastoCelda(deco, cubo, p, x * 97 + y * 57);
                }
            }
        }

        Publicar(raiz, piso, "Piso_");
        Publicar(raiz, deco, "Deco_");

        var lim = new GameObject("Limite_Invisible");
        lim.transform.SetParent(raiz, false);
        lim.AddComponent<MeshCollider>().sharedMesh =
            GuardarMalla("Colision", colision);

        // ---- Troncos caídos ----
        foreach (var c in Componentes(mapa, w, h, Tipo.Tronco))
        {
            Rango(c, w, h, out Vector3 centro, out int ancho, out int fondo);
            TroncoCaido(raiz, centro, ancho, fondo);
        }

        // ---- Techo de roca sobre las ruinas ----
        foreach (var c in Componentes(mapa, w, h, Tipo.Trampa))
        {
            Rango(c, w, h, out Vector3 centro, out int ancho, out int fondo);
            Techo(raiz, centro, ancho, fondo);
        }

        // ---- Salida: árboles que bloquean el camino al norte ----
        var puerta = new GameObject("Puerta_Arboles").transform;
        puerta.SetParent(raiz, false);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (mapa[x, y] == (int)Tipo.Salida)
                {
                    ArbolGigante(
                        puerta,
                        "Arbol_Bloqueo",
                        Pos(x, y, w, h),
                        x * 13 + y * 7,
                        false,
                        out _,
                        out _
                    );
                }
            }
        }

        // ---- Objetos celda por celda ----
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (mapa[x, y] < 0) continue;

                Vector3 p = Pos(x, y, w, h);

                switch ((Tipo)mapa[x, y])
                {
                    case Tipo.Ruina:
                        MuroRuina(raiz, p, x * 53 + y * 29);
                        break;

                    case Tipo.Arbol:
                        ArbolGigante(
                            raiz, "Arbol", p,
                            x * 31 + y * 17,
                            false, out _, out _
                        );
                        break;

                    case Tipo.Rama:
                        ArbolGigante(
                            raiz, "Arbol_Percha", p,
                            x * 31 + y * 17,
                            true, out _, out _
                        );
                        break;

                    case Tipo.Nahual:
                        // Se conserva el árbol inicial, pero sin el Nahual.
                        ArbolGigante(
                            raiz,
                            "Arbol_Percha_Inicial",
                            p,
                            x * 31 + y * 17,
                            true,
                            out _,
                            out _
                        );
                        break;

                    case Tipo.Columna:
                        Columna(raiz, p);
                        break;

                    case Tipo.Trompos:
                        Trompos(raiz, p);
                        break;

                    case Tipo.Inicio:
                        Marcador(raiz, "Inicio_Jugador", p);
                        break;
                }
            }
        }

        ConfigurarEscena();

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkAllScenesDirty();

        Selection.activeGameObject = raiz.gameObject;

        Debug.Log(
            "Selva creada (" + w + " x " + h +
            "). Mallas y materiales en Assets/Niveles/Selva."
        );
    }

    // ---------------- Decoración combinada ----------------

    // Árboles del borde.
    static void BordeArbol(
        Dictionary<string, List<CombineInstance>> d,
        Mesh cubo,
        Mesh cil,
        Vector3 p,
        int semilla)
    {
        var r = new System.Random(semilla);

        p += new Vector3(
            Rf(r, -0.3f, 0.3f),
            0f,
            Rf(r, -0.3f, 0.3f)
        );

        float rad = Rf(r, 0.75f, 1.1f);
        float alto = Rf(r, 7f, 10f);

        SumarTRS(
            Lista(d, "Corteza"),
            cil,
            p + Vector3.up * alto / 2f,
            Quaternion.identity,
            new Vector3(rad * 2f, alto / 2f, rad * 2f)
        );

        SumarTRS(
            Lista(d, "Corteza"),
            cil,
            p + Vector3.up * 0.4f,
            Quaternion.identity,
            new Vector3(rad * 3f, 0.4f, rad * 3f)
        );

        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f + Rf(r, -30f, 30f);

            SumarTRS(
                Lista(d, "Raiz"),
                cubo,
                p + Dir(a) * (rad + 0.9f) + Vector3.up * 0.4f,
                Quaternion.Euler(18f, a, 0f),
                new Vector3(0.4f, 0.36f, 2.2f)
            );
        }
    }

    // Hierba alta.
    static void PastoCelda(
        Dictionary<string, List<CombineInstance>> d,
        Mesh cubo,
        Vector3 p,
        int semilla)
    {
        var r = new System.Random(semilla);

        for (int k = 0; k < 6; k++)
        {
            float alto = Rf(r, 1.0f, 1.7f);

            Vector3 q = p + new Vector3(
                Rf(r, -0.42f, 0.42f),
                alto / 2f,
                Rf(r, -0.42f, 0.42f)
            );

            Quaternion rot = Quaternion.Euler(
                Rf(r, -12f, 12f),
                Rf(r, 0f, 360f),
                Rf(r, -12f, 12f)
            );

            SumarTRS(
                Lista(d, k % 2 == 0 ? "PastoA" : "PastoB"),
                cubo,
                q,
                rot,
                new Vector3(0.14f, alto, 0.14f)
            );
        }
    }

    // ---------------- Objetos ----------------

    static Transform ArbolGigante(
        Transform padre,
        string nombre,
        Vector3 pos,
        int semilla,
        bool conRama,
        out Transform punto,
        out float yaw)
    {
        var r = new System.Random(semilla);
        var g = Grupo(padre, nombre, pos);

        float rad = Rf(r, 0.85f, 1.05f);
        float alto = Rf(r, 7f, 9f);

        Pieza(
            g, "Tronco",
            PrimitiveType.Cylinder,
            new Vector3(0f, alto / 2f, 0f),
            new Vector3(rad * 2f, alto / 2f, rad * 2f),
            "Corteza"
        );

        Pieza(
            g, "BaseTronco",
            PrimitiveType.Cylinder,
            new Vector3(0f, 0.45f, 0f),
            new Vector3(rad * 3f, 0.45f, rad * 3f),
            "Corteza"
        );

        // Raíces retorcidas.
        int nRaices = 6;

        for (int i = 0; i < nRaices; i++)
        {
            float a = i * 360f / nRaices + Rf(r, -15f, 15f);
            Vector3 dir = Dir(a);

            var r1 = Pieza(
                g, "Raiz",
                PrimitiveType.Cube,
                dir * (rad + 1.1f) + Vector3.up * 0.45f,
                new Vector3(0.42f, 0.38f, 2.4f),
                "Raiz"
            );

            r1.localRotation = Quaternion.Euler(16f, a, 0f);

            float giro = (i % 2 == 0 ? 1f : -1f) *
                         Rf(r, 25f, 50f);

            var r2 = Pieza(
                g, "RaizRetorcida",
                PrimitiveType.Cube,
                dir * (rad + 2.7f) + Vector3.up * 0.15f,
                new Vector3(0.3f, 0.28f, 1.8f),
                "Raiz"
            );

            r2.localRotation = Quaternion.Euler(6f, a + giro, 0f);
        }

        yaw = Rf(r, 0f, 360f);
        punto = null;

        if (conRama)
        {
            Vector3 d = Dir(yaw);

            var b1 = Pieza(
                g, "Rama",
                PrimitiveType.Cube,
                d * 2.8f + Vector3.up * 5.6f,
                new Vector3(0.55f, 0.5f, 5.2f),
                "Corteza"
            );

            b1.localRotation = Quaternion.Euler(-4f, yaw, 0f);

            var b2 = Pieza(
                g, "RamaFina",
                PrimitiveType.Cube,
                d * 5.2f + Dir(yaw + 40f) * 0.6f +
                Vector3.up * 6.2f,
                new Vector3(0.3f, 0.28f, 2.2f),
                "Corteza"
            );

            b2.localRotation = Quaternion.Euler(
                -10f, yaw + 40f, 0f
            );

            Pieza(
                g, "Liana",
                PrimitiveType.Cylinder,
                d * 3.6f + Vector3.up * 4.3f,
                new Vector3(0.08f, 1.4f, 0.08f),
                "Liana"
            );

            Pieza(
                g, "Liana",
                PrimitiveType.Cylinder,
                d * 4.8f + Vector3.up * 4.5f,
                new Vector3(0.08f, 1.1f, 0.08f),
                "Liana"
            );

            var pgo = new GameObject("Punto_Rama");
            pgo.transform.SetParent(g, false);
            pgo.transform.localPosition =
                d * 3.8f + Vector3.up * 5.9f;

            punto = pgo.transform;
        }

        var cc = g.gameObject.AddComponent<CapsuleCollider>();
        cc.center = new Vector3(0f, alto / 2f, 0f);
        cc.radius = rad + 0.1f;
        cc.height = alto;

        return g;
    }

    static void TroncoCaido(
        Transform padre,
        Vector3 centro,
        int ancho,
        int fondo)
    {
        var g = Grupo(padre, "TroncoCaido", centro);

        bool largoX = ancho >= fondo;
        float L = largoX ? ancho : fondo;

        if (!largoX)
            g.localRotation = Quaternion.Euler(0f, 90f, 0f);

        var t = Pieza(
            g, "Tronco",
            PrimitiveType.Cylinder,
            new Vector3(0f, 0.68f, 0f),
            new Vector3(1.3f, L / 2f, 1.3f),
            "Corteza"
        );

        t.localRotation = Quaternion.Euler(0f, 0f, 90f);

        Pieza(
            g, "Musgo",
            PrimitiveType.Cube,
            new Vector3(0f, 1.3f, 0f),
            new Vector3(L * 0.7f, 0.1f, 0.6f),
            "Musgo"
        );

        var rama = Pieza(
            g, "Rama",
            PrimitiveType.Cube,
            new Vector3(-L * 0.25f, 1.1f, 0.65f),
            new Vector3(0.18f, 0.18f, 0.9f),
            "CortezaClara"
        );

        rama.localRotation = Quaternion.Euler(-20f, 0f, 0f);

        var bc = g.gameObject.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, 0.7f, 0f);
        bc.size = new Vector3(L, 1.4f, 1.3f);
    }

    static void MuroRuina(
        Transform padre,
        Vector3 pos,
        int semilla)
    {
        var r = new System.Random(semilla);

        float alto = Rf(r, 1.3f, 2.9f);
        var g = Grupo(padre, "MuroRuina", pos);

        g.localRotation = Quaternion.Euler(
            0f, Rf(r, -3f, 3f), 0f
        );

        Pieza(
            g, "Bloque",
            PrimitiveType.Cube,
            new Vector3(0f, alto / 2f, 0f),
            new Vector3(1.02f, alto, 1.02f),
            "Piedra"
        );

        Pieza(
            g, "Roto",
            PrimitiveType.Cube,
            new Vector3(
                Rf(r, -0.2f, 0.2f),
                alto + 0.15f,
                Rf(r, -0.2f, 0.2f)
            ),
            new Vector3(0.55f, 0.3f, 0.55f),
            "PiedraOscura"
        );

        if (r.NextDouble() < 0.75)
        {
            Pieza(
                g, "Musgo",
                PrimitiveType.Cube,
                new Vector3(0f, alto + 0.01f, 0f),
                new Vector3(1.06f, 0.1f, 1.06f),
                "Musgo"
            );
        }

        if (r.NextDouble() < 0.6)
        {
            Pieza(
                g, "MusgoLado",
                PrimitiveType.Cube,
                new Vector3(0f, alto * 0.3f, 0.5f),
                new Vector3(0.8f, alto * 0.5f, 0.06f),
                "Musgo"
            );
        }

        var bc = g.gameObject.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, alto / 2f, 0f);
        bc.size = new Vector3(1f, alto, 1f);
    }

    // Techo de roca.
    static void Techo(
        Transform raiz,
        Vector3 centro,
        int ancho,
        int fondo)
    {
        var r = new System.Random(5);

        float W = ancho + 1f;
        float D = fondo + 1f;

        var g = Grupo(
            raiz,
            "Techo_Roca",
            centro + new Vector3(0f, ALTURA_TECHO + 0.45f, 0f)
        );

        Pieza(
            g, "Losa",
            PrimitiveType.Cube,
            Vector3.zero,
            new Vector3(W, 0.9f, D),
            "TechoRoca"
        );

        for (int i = 0; i < 6; i++)
        {
            Pieza(
                g, "Musgo",
                PrimitiveType.Cube,
                new Vector3(
                    Rf(r, -W / 2f + 0.5f, W / 2f - 0.5f),
                    0.5f,
                    Rf(r, -D / 2f + 0.5f, D / 2f - 0.5f)
                ),
                new Vector3(
                    Rf(r, 0.6f, 1.4f),
                    0.12f,
                    Rf(r, 0.6f, 1.4f)
                ),
                "Musgo"
            );
        }

        for (int i = 0; i < 3; i++)
        {
            var gr = Pieza(
                g, "Grieta",
                PrimitiveType.Cube,
                new Vector3(
                    Rf(r, -W / 3f, W / 3f),
                    0f,
                    Rf(r, -D / 3f, D / 3f)
                ),
                new Vector3(0.06f, 0.92f, D * 0.5f),
                "Grieta"
            );

            gr.localRotation = Quaternion.Euler(
                0f, Rf(r, -35f, 35f), 0f
            );
        }

        var bc = g.gameObject.AddComponent<BoxCollider>();
        bc.size = new Vector3(W, 0.9f, D);

        // Pilares rotos.
        foreach (float sx in new[] { -1f, 1f })
        {
            var p = Grupo(
                raiz,
                "PilarRoto",
                centro + new Vector3(
                    sx * (W / 2f - 0.3f),
                    0f,
                    D / 2f - 0.3f
                )
            );

            Pieza(
                p, "Fuste",
                PrimitiveType.Cube,
                new Vector3(0f, ALTURA_TECHO / 2f, 0f),
                new Vector3(0.7f, ALTURA_TECHO, 0.7f),
                "PiedraOscura"
            );

            Pieza(
                p, "Musgo",
                PrimitiveType.Cube,
                new Vector3(0f, 0.8f, 0f),
                new Vector3(0.74f, 1.2f, 0.74f),
                "Musgo"
            );

            var pc = p.gameObject.AddComponent<BoxCollider>();
            pc.center = new Vector3(
                0f, ALTURA_TECHO / 2f, 0f
            );
            pc.size = new Vector3(0.7f, ALTURA_TECHO, 0.7f);
        }
    }

    static void Columna(Transform raiz, Vector3 pos)
    {
        var g = Grupo(raiz, "Columna_Agrietada", pos);
        float H = ALTURA_TECHO;

        Pieza(
            g, "Fuste",
            PrimitiveType.Cube,
            new Vector3(0f, H / 2f, 0f),
            new Vector3(1.0f, H, 1.0f),
            "Piedra"
        );

        Pieza(
            g, "Base",
            PrimitiveType.Cube,
            new Vector3(0f, 0.25f, 0f),
            new Vector3(1.35f, 0.5f, 1.35f),
            "PiedraOscura"
        );

        Pieza(
            g, "Capitel",
            PrimitiveType.Cube,
            new Vector3(0f, H - 0.2f, 0f),
            new Vector3(1.35f, 0.4f, 1.35f),
            "PiedraOscura"
        );

        var g1 = Pieza(
            g, "Grieta",
            PrimitiveType.Cube,
            new Vector3(0f, H * 0.5f, 0f),
            new Vector3(0.06f, H * 0.7f, 1.02f),
            "Grieta"
        );

        g1.localRotation = Quaternion.Euler(0f, 0f, 8f);

        var g2 = Pieza(
            g, "Grieta",
            PrimitiveType.Cube,
            new Vector3(0f, H * 0.6f, 0f),
            new Vector3(1.02f, H * 0.5f, 0.06f),
            "Grieta"
        );

        g2.localRotation = Quaternion.Euler(0f, 0f, -10f);

        Pieza(
            g, "Musgo",
            PrimitiveType.Cube,
            new Vector3(0f, 0.9f, 0f),
            new Vector3(1.04f, 0.7f, 1.04f),
            "Musgo"
        );

        var bc = g.gameObject.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, H / 2f, 0f);
        bc.size = new Vector3(1.35f, H, 1.35f);
    }

    // Trompos de madera.
    static void Trompos(Transform raiz, Vector3 pos)
    {
        var g = Grupo(raiz, "Recompensa_Trompos", pos);

        Pieza(
            g, "Plinto",
            PrimitiveType.Cylinder,
            new Vector3(0f, 0.15f, 0f),
            new Vector3(1.8f, 0.15f, 1.8f),
            "PiedraOscura"
        );

        var visual = new GameObject("Visual").transform;
        visual.SetParent(g, false);

        string[] mats =
        {
            "TrompoRojo",
            "TrompoAzul",
            "TrompoVerde",
            "TrompoAmarillo"
        };

        for (int i = 0; i < 4; i++)
        {
            var t = Grupo(
                visual,
                "Trompo",
                Dir(i * 90f + 20f) * 0.45f +
                new Vector3(0f, 0.3f, 0f)
            );

            Pieza(
                t, "Cuerpo",
                PrimitiveType.Sphere,
                new Vector3(0f, 0.26f, 0f),
                new Vector3(0.45f, 0.38f, 0.45f),
                mats[i]
            );

            var punta = Pieza(
                t, "Punta",
                PrimitiveType.Cube,
                new Vector3(0f, 0.04f, 0f),
                Vector3.one * 0.12f,
                "Madera"
            );

            punta.localRotation = Quaternion.Euler(45f, 0f, 45f);

            Pieza(
                t, "Tallo",
                PrimitiveType.Cylinder,
                new Vector3(0f, 0.55f, 0f),
                new Vector3(0.07f, 0.12f, 0.07f),
                "Madera"
            );

            Pieza(
                t, "OrejaGato",
                PrimitiveType.Cube,
                new Vector3(-0.1f, 0.46f, 0.12f),
                new Vector3(0.1f, 0.14f, 0.05f),
                mats[i]
            );

            Pieza(
                t, "OrejaGato",
                PrimitiveType.Cube,
                new Vector3(0.1f, 0.46f, 0.12f),
                new Vector3(0.1f, 0.14f, 0.05f),
                mats[i]
            );
        }

        Pieza(
            visual, "Brillo",
            PrimitiveType.Sphere,
            new Vector3(0f, 1.0f, 0f),
            Vector3.one * 0.3f,
            "Brillo"
        );

        Luz(
            visual,
            new Vector3(0f, 1.3f, 0f),
            new Color(1f, 0.8f, 0.4f),
            5f,
            2.5f
        );
    }

    // Esfera azul que marca dónde empieza Iktan.
    static void Marcador(
        Transform padre,
        string nombre,
        Vector3 pos)
    {
        var g = Grupo(padre, nombre, pos);

        Pieza(
            g, "Marcador",
            PrimitiveType.Sphere,
            new Vector3(0f, 0.4f, 0f),
            Vector3.one * 0.8f,
            "MarcadorInicio"
        );
    }

    // ---------------- Utilidades ----------------

    static Transform Grupo(
        Transform padre,
        string nombre,
        Vector3 pos)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        return go.transform;
    }

    static Transform Pieza(
        Transform padre,
        string nombre,
        PrimitiveType tipo,
        Vector3 pos,
        Vector3 esc,
        string material)
    {
        var g = GameObject.CreatePrimitive(tipo);

        g.name = nombre;
        g.transform.SetParent(padre, false);
        g.transform.localPosition = pos;
        g.transform.localScale = esc;

        Object.DestroyImmediate(g.GetComponent<Collider>());

        g.GetComponent<Renderer>().sharedMaterial = M(material);

        return g.transform;
    }

    static Light Luz(
        Transform padre,
        Vector3 pos,
        Color color,
        float alcance,
        float intensidad)
    {
        var go = new GameObject("Luz");

        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;

        var l = go.AddComponent<Light>();

        l.type = LightType.Point;
        l.color = color;
        l.range = alcance;
        l.intensity = intensidad;
        l.shadows = LightShadows.None;

        return l;
    }

    static Vector3 Pos(int x, int y, int w, int h)
    {
        return new Vector3(
            x - w / 2f + 0.5f,
            0f,
            y - h / 2f + 0.5f
        );
    }

    static float Rf(System.Random r, float a, float b)
    {
        return a + (float)r.NextDouble() * (b - a);
    }

    // Dirección horizontal para un ángulo en grados.
    static Vector3 Dir(float grados)
    {
        float a = grados * Mathf.Deg2Rad;

        return new Vector3(
            Mathf.Sin(a),
            0f,
            Mathf.Cos(a)
        );
    }

    static void Rango(
        List<Vector2Int> c,
        int w,
        int h,
        out Vector3 centro,
        out int ancho,
        out int fondo)
    {
        int minX = int.MaxValue;
        int maxX = int.MinValue;
        int minY = int.MaxValue;
        int maxY = int.MinValue;

        foreach (var p in c)
        {
            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.y > maxY) maxY = p.y;
        }

        ancho = maxX - minX + 1;
        fondo = maxY - minY + 1;

        centro = new Vector3(
            (minX + maxX) / 2f - w / 2f + 0.5f,
            0f,
            (minY + maxY) / 2f - h / 2f + 0.5f
        );
    }

    static List<List<Vector2Int>> Componentes(
        int[,] mapa,
        int w,
        int h,
        Tipo t)
    {
        var visto = new bool[w, h];
        var res = new List<List<Vector2Int>>();

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (visto[x, y] || mapa[x, y] != (int)t)
                    continue;

                var comp = new List<Vector2Int>();
                var pila = new Stack<Vector2Int>();

                pila.Push(new Vector2Int(x, y));
                visto[x, y] = true;

                while (pila.Count > 0)
                {
                    var c = pila.Pop();
                    comp.Add(c);

                    foreach (var d in dirs)
                    {
                        int nx = c.x + d.x;
                        int ny = c.y + d.y;

                        if (nx < 0 || ny < 0 || nx >= w || ny >= h)
                            continue;

                        if (visto[nx, ny] ||
                            mapa[nx, ny] != (int)t)
                            continue;

                        visto[nx, ny] = true;
                        pila.Push(new Vector2Int(nx, ny));
                    }
                }

                res.Add(comp);
            }
        }

        return res;
    }

    static Tipo Clasificar(Color32 p)
    {
        Tipo mejor = Tipo.Piso;
        int mejorDist = int.MaxValue;

        foreach (var (c, t) in paleta)
        {
            int dr = p.r - c.r;
            int dg = p.g - c.g;
            int db = p.b - c.b;

            int d = dr * dr + dg * dg + db * db;

            if (d < mejorDist)
            {
                mejorDist = d;
                mejor = t;
            }
        }

        return mejor;
    }

    static List<CombineInstance> Lista(
        Dictionary<string, List<CombineInstance>> d,
        string clave)
    {
        if (!d.TryGetValue(clave, out var l))
        {
            l = new List<CombineInstance>();
            d[clave] = l;
        }

        return l;
    }

    static void SumarTRS(
        List<CombineInstance> lista,
        Mesh m,
        Vector3 pos,
        Quaternion rot,
        Vector3 esc)
    {
        var ci = new CombineInstance();

        ci.mesh = m;
        ci.transform = Matrix4x4.TRS(pos, rot, esc);

        lista.Add(ci);
    }

    static void Publicar(
        Transform raiz,
        Dictionary<string, List<CombineInstance>> d,
        string prefijo)
    {
        foreach (var par in d)
        {
            var go = new GameObject(prefijo + par.Key);

            go.transform.SetParent(raiz, false);

            go.AddComponent<MeshFilter>().sharedMesh =
                GuardarMalla(prefijo + par.Key, par.Value);

            go.AddComponent<MeshRenderer>().sharedMaterial =
                M(par.Key);

            go.isStatic = true;
        }
    }

    static Mesh GuardarMalla(
        string nombre,
        List<CombineInstance> lista)
    {
        var malla = new Mesh();

        malla.name = "Selva_" + nombre;
        malla.indexFormat = IndexFormat.UInt32;
        malla.CombineMeshes(lista.ToArray(), true, true);

        string ruta =
            "Assets/Niveles/Selva/Malla_" + nombre + ".asset";

        AssetDatabase.DeleteAsset(ruta);
        AssetDatabase.CreateAsset(malla, ruta);

        return malla;
    }

    static Material M(string nombre)
    {
        Material hecho;

        if (cache.TryGetValue(nombre, out hecho))
            return hecho;

        string ruta =
            "Assets/Niveles/Selva/Mat_" + nombre + ".mat";

        var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);

        if (m == null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");

            if (sh == null)
                sh = Shader.Find("Standard");

            m = new Material(sh);

            AssetDatabase.CreateAsset(m, ruta);
        }

        m.color = colores[nombre];

        if (nombre == "Brillo")
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor(
                "_EmissionColor",
                colores[nombre] * 2.5f
            );
        }

        if (nombre == "TechoRoca" && TECHO_TRANSPARENTE)
            Transparente(m);

        EditorUtility.SetDirty(m);

        cache[nombre] = m;

        return m;
    }

    // Material transparente.
    static void Transparente(Material m)
    {
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);

        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        m.renderQueue = (int)RenderQueue.Transparent;
    }

    // Calor húmedo y niebla verde.
    static void ConfigurarEscena()
    {
        Color niebla = new Color(0.10f, 0.22f, 0.13f);

        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;

        RenderSettings.ambientLight =
            new Color(0.16f, 0.24f, 0.17f);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = niebla;
        RenderSettings.fogDensity = DENSIDAD_NIEBLA;

        foreach (var l in Object.FindObjectsByType<Light>(
            FindObjectsSortMode.None))
        {
            if (l.type != LightType.Directional)
                continue;

            l.intensity = 0.35f;
            l.color = new Color(0.65f, 0.95f, 0.70f);
            l.shadows = LightShadows.None;
        }

        var cam = Camera.main;

        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = niebla;

            cam.transform.position =
                new Vector3(0f, 52f, -22f);

            cam.transform.rotation =
                Quaternion.Euler(65f, 0f, 0f);
        }
    }
}
