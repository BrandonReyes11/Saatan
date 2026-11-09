// Saatan Historia - Iglesia con paredes, vitrales, altares y ofrendas de Hanal Pixán.
// Colocar en: Assets/Editor/ConstructorIglesia.cs
// Se usa con la imagen iglesia_detallada.png (seleccionarla en Project antes de usar el menú).
//
// Colores del mapa:
//   BLANCO piso          NEGRO    límite invisible (colisión)
//   CAFÉ   banca         NARANJA  caja de madera (una por píxel)
//   GRIS   altar mayor   MORADO   ofrenda lateral de Hanal Pixán
//   AMARILLO vitral (se coloca en la pared, en el ángulo donde lo pintes)
//   ROJO   aparición del Diablo   AZUL inicio de Iktan
//   ROSA   candelabro suelto (opcional)
// Los candelabros de cada altar y de cada ofrenda se crean solos a sus lados.
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class ConstructorIglesia
{
    enum Tipo { Piso, Limite, Banca, Caja, Vela, Altar, Vitral, Diablo, Inicio, Ofrenda }

    const float RadioPared = 19.6f;
    const int AltoPared = 6;
    const int Segmentos = 120;      // 3 grados cada uno

    static readonly (Color32 color, Tipo tipo)[] paleta =
    {
        (new Color32(255, 255, 255, 255), Tipo.Piso),
        (new Color32(0, 0, 0, 255),       Tipo.Limite),
        (new Color32(140, 80, 30, 255),   Tipo.Banca),
        (new Color32(255, 150, 0, 255),   Tipo.Caja),
        (new Color32(255, 105, 180, 255), Tipo.Vela),
        (new Color32(128, 128, 128, 255), Tipo.Altar),
        (new Color32(255, 220, 0, 255),   Tipo.Vitral),
        (new Color32(230, 0, 0, 255),     Tipo.Diablo),
        (new Color32(0, 100, 255, 255),   Tipo.Inicio),
        (new Color32(160, 40, 200, 255),  Tipo.Ofrenda),
    };

    static readonly Vector2Int[] dirs = { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };

    static readonly Dictionary<string, Color> colores = new Dictionary<string, Color>
    {
        { "PisoA", new Color(0.32f, 0.22f, 0.38f) },
        { "PisoB", new Color(0.26f, 0.17f, 0.32f) },
        { "Alfombra", new Color(0.60f, 0.08f, 0.18f) },
        { "ParedA", new Color(0.42f, 0.36f, 0.50f) },
        { "ParedB", new Color(0.36f, 0.30f, 0.44f) },
        { "ParedC", new Color(0.28f, 0.23f, 0.35f) },
        { "MaderaOscura", new Color(0.30f, 0.17f, 0.09f) },
        { "MaderaClara", new Color(0.52f, 0.33f, 0.17f) },
        { "Piedra", new Color(0.55f, 0.54f, 0.50f) },
        { "PiedraOscura", new Color(0.35f, 0.34f, 0.33f) },
        { "Tela", new Color(0.62f, 0.08f, 0.25f) },
        { "Dorado", new Color(0.95f, 0.75f, 0.20f) },
        { "Cera", new Color(0.98f, 0.94f, 0.80f) },
        { "Cempasuchil", new Color(1.00f, 0.60f, 0.05f) },
        { "Cempasuchil2", new Color(1.00f, 0.78f, 0.10f) },
        { "Llama", new Color(1.00f, 0.65f, 0.15f) },
        { "VitralRojo", new Color(0.90f, 0.10f, 0.15f) },
        { "VitralAzul", new Color(0.15f, 0.30f, 0.95f) },
        { "VitralVerde", new Color(0.10f, 0.70f, 0.30f) },
        { "VitralAmarillo", new Color(1.00f, 0.80f, 0.10f) },
        { "Pico1", new Color(0.88f, 0.27f, 0.48f) },
        { "Pico2", new Color(1.00f, 0.65f, 0.12f) },
        { "Pico3", new Color(0.35f, 0.82f, 0.77f) },
        { "Pico4", new Color(0.61f, 0.42f, 1.00f) },
        { "Cuerda", new Color(0.90f, 0.85f, 0.70f) },
        { "Hoja", new Color(0.25f, 0.55f, 0.25f) },
        { "Mucbi", new Color(0.80f, 0.60f, 0.25f) },
        { "Ojo", new Color(0.05f, 0.03f, 0.07f) },
        { "MarcadorDiablo", new Color(0.90f, 0.00f, 0.00f) },
        { "MarcadorInicio", new Color(0.00f, 0.40f, 1.00f) },
    };

    static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

    class Acum
    {
        public List<Vector3> v = new List<Vector3>();
        public List<Vector3> n = new List<Vector3>();
        public List<int> t = new List<int>();
    }

    [MenuItem("Saatan/Construir iglesia detallada")]
    static void Construir()
    {
        var tex = Selection.activeObject as Texture2D;
        if (tex == null)
        {
            EditorUtility.DisplayDialog("Saatan", "Primero selecciona iglesia_detallada.png en la ventana Project y vuelve a intentarlo.", "OK");
            return;
        }

        string ruta = AssetDatabase.GetAssetPath(tex);
        var imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
        imp.isReadable = true;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.npotScale = TextureImporterNPOTScale.None;
        imp.SaveAndReimport();
        tex = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);

        Color32[] px = tex.GetPixels32();
        int w = tex.width, h = tex.height;

        if (!AssetDatabase.IsValidFolder("Assets/Niveles")) AssetDatabase.CreateFolder("Assets", "Niveles");
        if (!AssetDatabase.IsValidFolder("Assets/Niveles/Iglesia")) AssetDatabase.CreateFolder("Assets/Niveles", "Iglesia");
        cache.Clear();

        var viejo = GameObject.Find("Iglesia_Detallada");
        if (viejo != null) Object.DestroyImmediate(viejo);
        var raiz = new GameObject("Iglesia_Detallada").transform;

        // Mapa de tipos (-1 = vacío).
        var mapa = new int[w, h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color32 p = px[y * w + x];
                mapa[x, y] = p.a < 128 ? -1 : (int)Clasificar(p);
            }

        // Vitrales: cada mancha amarilla se convierte en una ventana dentro de la pared.
        var ventanas = new HashSet<int>();
        foreach (var c in Componentes(mapa, w, h, Tipo.Vitral))
        {
            Vector3 centro; int ancho, fondo;
            Rango(c, w, h, out centro, out ancho, out fondo);
            float ang = Mathf.Atan2(centro.z, centro.x) * Mathf.Rad2Deg;
            if (ang < 0f) ang += 360f;
            ventanas.Add(Mathf.FloorToInt(ang / (360f / Segmentos)) % Segmentos);
            foreach (var celda in c) mapa[celda.x, celda.y] = (int)Tipo.Limite;
        }

        // ---- Piso con baldosas, alfombra y colisiones (el límite NO se ve) ----
        var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Mesh cubo = tmp.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(tmp);

        var visibles = new Dictionary<string, List<CombineInstance>>();
        var colision = new List<CombineInstance>();

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (mapa[x, y] < 0) continue;
                Vector3 p = Pos(x, y, w, h);
                bool limite = mapa[x, y] == (int)Tipo.Limite;

                if (!limite)
                {
                    bool alfombra = Mathf.Abs(p.x) <= 0.51f && p.z > -16f && p.z < 15f;
                    if (alfombra) Sumar(visibles, "Alfombra", cubo, new Vector3(p.x, -0.09f, p.z), new Vector3(1f, 0.22f, 1f));
                    else Sumar(visibles, ((x + y) % 2 == 0) ? "PisoA" : "PisoB", cubo, new Vector3(p.x, -0.1f, p.z), new Vector3(0.96f, 0.2f, 0.96f));
                }

                SumarLista(colision, cubo, new Vector3(p.x, -0.1f, p.z), new Vector3(1f, 0.2f, 1f));
                if (limite) SumarLista(colision, cubo, new Vector3(p.x, 3f, p.z), new Vector3(1f, 6f, 1f));
            }

        foreach (var par in visibles)
        {
            var go = new GameObject("Piso_" + par.Key);
            go.transform.SetParent(raiz, false);
            go.AddComponent<MeshFilter>().sharedMesh = GuardarMalla(par.Key, par.Value);
            go.AddComponent<MeshRenderer>().sharedMaterial = M(par.Key);
            go.isStatic = true;
        }

        var lim = new GameObject("Limite_Invisible");
        lim.transform.SetParent(raiz, false);
        lim.AddComponent<MeshCollider>().sharedMesh = GuardarMalla("Colision", colision);

        // ---- Paredes de la iglesia ----
        Pared(raiz, ventanas);
        foreach (int k in ventanas) Ventana(raiz, k);
        int[] pilastras = { 0, 8, 18, 41, 51, 59 };
        foreach (int k in pilastras) Pilastra(raiz, k);
        Banderines(raiz);

        // ---- Objetos ----
        foreach (var c in Componentes(mapa, w, h, Tipo.Banca))
        {
            Vector3 centro; int ancho, fondo;
            Rango(c, w, h, out centro, out ancho, out fondo);
            Banca(raiz, centro, ancho, fondo);
        }
        foreach (var c in Componentes(mapa, w, h, Tipo.Altar))
        {
            Vector3 centro; int ancho, fondo;
            Rango(c, w, h, out centro, out ancho, out fondo);
            Altar(raiz, centro, ancho, fondo);
        }
        foreach (var c in Componentes(mapa, w, h, Tipo.Ofrenda))
        {
            Vector3 centro; int ancho, fondo;
            Rango(c, w, h, out centro, out ancho, out fondo);
            Ofrenda(raiz, centro);
        }
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Vector3 p = Pos(x, y, w, h);
                switch ((Tipo)mapa[x, y])
                {
                    case Tipo.Caja:   Caja(raiz, p, x * 7 + y * 13); break;
                    case Tipo.Vela:   Candelabro(raiz, p); break;
                    case Tipo.Diablo: Marcador(raiz, "Aparicion_Diablo", p, 1.2f, "MarcadorDiablo"); break;
                    case Tipo.Inicio: Marcador(raiz, "Inicio_Jugador", p, 0.8f, "MarcadorInicio"); break;
                }
            }

        HerramientasIktan.DarVidaAlFuego(raiz.gameObject);
        ConfigurarEscena();

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkAllScenesDirty();
        Selection.activeGameObject = raiz.gameObject;
        Debug.Log("Iglesia creada con " + ventanas.Count + " vitrales. Mallas y materiales en Assets/Niveles/Iglesia.");
    }

    // ---------------- Paredes ----------------

    // Muro curvo con caras solo hacia adentro: desde la cámara se ve el interior
    // de los muros del fondo y los muros cercanos no estorban la vista.
    static void Pared(Transform raiz, HashSet<int> ventanas)
    {
        var abre = new HashSet<int>();
        foreach (int k in ventanas)
            for (int d = -1; d <= 1; d++) abre.Add(((k + d) % Segmentos + Segmentos) % Segmentos);

        var tonos = new Acum[] { new Acum(), new Acum(), new Acum() };
        float paso = 360f / Segmentos;

        for (int i = 0; i < Segmentos; i++)
        {
            float a0 = i * paso * Mathf.Deg2Rad;
            float a1 = (i + 1) * paso * Mathf.Deg2Rad;
            float am = (a0 + a1) / 2f;
            Vector3 normal = new Vector3(-Mathf.Cos(am), 0f, -Mathf.Sin(am));

            for (int j = 0; j < AltoPared; j++)
            {
                if (abre.Contains(i) && j >= 1 && j <= 3) continue;   // hueco del vitral
                int tono = (j == 0 || j == AltoPared - 1) ? 2 : ((i + j) % 2);
                Quad(tonos[tono], PuntoPared(a0, j), PuntoPared(a1, j), PuntoPared(a1, j + 1), PuntoPared(a0, j + 1), normal);
            }
        }

        string[] nombres = { "ParedA", "ParedB", "ParedC" };
        for (int t = 0; t < 3; t++)
        {
            var malla = new Mesh();
            malla.name = "Iglesia_" + nombres[t];
            malla.SetVertices(tonos[t].v);
            malla.SetNormals(tonos[t].n);
            malla.SetTriangles(tonos[t].t, 0);
            malla.RecalculateBounds();

            string rutaMalla = "Assets/Niveles/Iglesia/Malla_" + nombres[t] + ".asset";
            AssetDatabase.DeleteAsset(rutaMalla);
            AssetDatabase.CreateAsset(malla, rutaMalla);

            var go = new GameObject("Pared_" + nombres[t]);
            go.transform.SetParent(raiz, false);
            go.AddComponent<MeshFilter>().sharedMesh = malla;
            go.AddComponent<MeshRenderer>().sharedMaterial = M(nombres[t]);
            go.isStatic = true;
        }
    }

    static Vector3 PuntoPared(float ang, float y)
    {
        return new Vector3(RadioPared * Mathf.Cos(ang), y, RadioPared * Mathf.Sin(ang));
    }

    static void Quad(Acum a, Vector3 A, Vector3 B, Vector3 C, Vector3 D, Vector3 normal)
    {
        int i = a.v.Count;
        a.v.Add(A); a.v.Add(B); a.v.Add(C); a.v.Add(D);
        for (int k = 0; k < 4; k++) a.n.Add(normal);
        a.t.Add(i + 1); a.t.Add(i + 2); a.t.Add(i + 3);
        a.t.Add(i + 1); a.t.Add(i + 3); a.t.Add(i);
    }

    // Grupo pegado a la pared; su eje Z local apunta al centro de la iglesia.
    static Transform EnPared(Transform padre, string nombre, float anguloGrados, float radio)
    {
        float a = anguloGrados * Mathf.Deg2Rad;
        var g = Grupo(padre, nombre, new Vector3(radio * Mathf.Cos(a), 0f, radio * Mathf.Sin(a)));
        g.localRotation = Quaternion.LookRotation(new Vector3(-Mathf.Cos(a), 0f, -Mathf.Sin(a)), Vector3.up);
        return g;
    }

    static void Ventana(Transform padre, int k)
    {
        var g = EnPared(padre, "Vitral", (k + 0.5f) * (360f / Segmentos), RadioPared);

        // Marco de piedra con arco escalonado
        Pieza(g, "PilarIzq", PrimitiveType.Cube, new Vector3(-1.75f, 2.55f, 0.1f), new Vector3(0.35f, 3.3f, 0.5f), "PiedraOscura");
        Pieza(g, "PilarDer", PrimitiveType.Cube, new Vector3(1.75f, 2.55f, 0.1f), new Vector3(0.35f, 3.3f, 0.5f), "PiedraOscura");
        Pieza(g, "Repisa", PrimitiveType.Cube, new Vector3(0f, 1.0f, 0.2f), new Vector3(3.7f, 0.2f, 0.6f), "Piedra");
        Pieza(g, "Dintel1", PrimitiveType.Cube, new Vector3(0f, 4.15f, 0.1f), new Vector3(3.8f, 0.3f, 0.5f), "PiedraOscura");
        Pieza(g, "Dintel2", PrimitiveType.Cube, new Vector3(0f, 4.45f, 0.1f), new Vector3(2.8f, 0.3f, 0.5f), "PiedraOscura");
        Pieza(g, "Dintel3", PrimitiveType.Cube, new Vector3(0f, 4.75f, 0.1f), new Vector3(1.6f, 0.3f, 0.5f), "PiedraOscura");

        // Cristales de colores dentro del hueco del muro
        string[] cristal = { "VitralRojo", "VitralAzul", "VitralAmarillo", "VitralVerde" };
        int n = 0;
        for (int fila = 0; fila < 3; fila++)
            for (int col = 0; col < 3; col++)
            {
                Pieza(g, "Cristal", PrimitiveType.Cube, new Vector3(-1.0f + col * 1.0f, 1.5f + fila * 1.0f, -0.1f), new Vector3(0.95f, 0.95f, 0.1f), cristal[(n * 3 + fila) % 4]);
                n++;
            }

        // Tablas podridas que Iktan debe derribar
        float[] alturas = { 1.7f, 2.6f, 3.5f };
        float[] giros = { -6f, 4f, -3f };
        for (int i = 0; i < 3; i++)
        {
            var t = Pieza(g, "Tabla", PrimitiveType.Cube, new Vector3(0f, alturas[i], 0.18f), new Vector3(3.3f, 0.35f, 0.12f), "MaderaOscura");
            t.localRotation = Quaternion.Euler(0f, 0f, giros[i]);
        }

        // Guirnalda de cempasúchil sobre la repisa
        for (int i = 0; i < 9; i++)
            Pieza(g, "Flor", PrimitiveType.Cube, new Vector3(-1.6f + i * 0.4f, 1.2f, 0.4f), new Vector3(0.26f, 0.26f, 0.26f), (i % 2 == 0) ? "Cempasuchil" : "Cempasuchil2");

        // Luz fría que entra por el vitral
        Luz(g, new Vector3(0f, 2.6f, 1.6f), new Color(0.6f, 0.7f, 1f), 8f, 2f);

        var meta = new GameObject("Meta_Vitral");
        meta.transform.SetParent(g, false);
        var tr = meta.AddComponent<BoxCollider>();
        tr.isTrigger = true;
        tr.center = new Vector3(0f, 1.5f, 1.6f);
        tr.size = new Vector3(4f, 3f, 3f);
    }

    static void Pilastra(Transform padre, int k)
    {
        var g = EnPared(padre, "Pilastra", (k + 0.5f) * (360f / Segmentos), RadioPared);
        Pieza(g, "Base", PrimitiveType.Cube, new Vector3(0f, 0.25f, 0.3f), new Vector3(1.4f, 0.5f, 0.9f), "PiedraOscura");
        Pieza(g, "Fuste", PrimitiveType.Cube, new Vector3(0f, 3f, 0.25f), new Vector3(1.1f, 6f, 0.7f), "Piedra");
        Pieza(g, "Capitel", PrimitiveType.Cube, new Vector3(0f, 5.8f, 0.3f), new Vector3(1.5f, 0.35f, 0.9f), "PiedraOscura");
    }

    // Papel picado colgando de la pared, como en Hanal Pixán.
    static void Banderines(Transform padre)
    {
        string[] cols = { "Pico1", "Pico2", "Pico3", "Pico4" };
        int n = 0;
        for (float ang = 6f; ang <= 174f; ang += 3f)
        {
            var g = EnPared(padre, "Banderin", ang, RadioPared - 0.8f);
            Pieza(g, "Papel", PrimitiveType.Cube, new Vector3(0f, 5.5f, 0f), new Vector3(0.55f, 0.7f, 0.03f), cols[n % 4]);
            Pieza(g, "Cuerda", PrimitiveType.Cube, new Vector3(0f, 5.88f, 0f), new Vector3(1.05f, 0.03f, 0.03f), "Cuerda");
            n++;
        }
    }

    // ---------------- Objetos ----------------

    static void Banca(Transform padre, Vector3 pos, int ancho, int fondo)
    {
        var g = Grupo(padre, "Banca", pos);
        bool largoX = ancho >= fondo;
        float L = largoX ? ancho : fondo, S = largoX ? fondo : ancho;
        if (!largoX) g.localRotation = Quaternion.Euler(0f, 90f, 0f);

        Pieza(g, "Asiento", PrimitiveType.Cube, new Vector3(0f, 0.5f, 0f), new Vector3(L - 0.2f, 0.15f, S * 0.7f), "MaderaClara");
        Pieza(g, "Respaldo", PrimitiveType.Cube, new Vector3(0f, 1.0f, -S * 0.35f + 0.06f), new Vector3(L - 0.2f, 0.8f, 0.14f), "MaderaOscura");
        Pieza(g, "LateralIzq", PrimitiveType.Cube, new Vector3(-(L / 2f - 0.2f), 0.45f, 0f), new Vector3(0.16f, 0.9f, S * 0.7f), "MaderaOscura");
        Pieza(g, "LateralDer", PrimitiveType.Cube, new Vector3(L / 2f - 0.2f, 0.45f, 0f), new Vector3(0.16f, 0.9f, S * 0.7f), "MaderaOscura");
        Pieza(g, "Rodillera", PrimitiveType.Cube, new Vector3(0f, 0.15f, S * 0.35f + 0.15f), new Vector3(L - 0.4f, 0.3f, 0.3f), "MaderaOscura");

        var bc = g.gameObject.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, 0.6f, 0f);
        bc.size = new Vector3(L, 1.2f, S);
    }

    static void Caja(Transform padre, Vector3 pos, int semilla)
    {
        var g = Grupo(padre, "Caja", pos);
        g.localRotation = Quaternion.Euler(0f, (semilla % 5 - 2) * 7f, 0f);

        Pieza(g, "Cuerpo", PrimitiveType.Cube, new Vector3(0f, 0.45f, 0f), new Vector3(0.9f, 0.9f, 0.9f), "MaderaClara");
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
                Pieza(g, "Esquina", PrimitiveType.Cube, new Vector3(sx * 0.43f, 0.45f, sz * 0.43f), new Vector3(0.12f, 0.94f, 0.12f), "MaderaOscura");
        Pieza(g, "Tapa", PrimitiveType.Cube, new Vector3(0f, 0.92f, 0f), new Vector3(0.96f, 0.06f, 0.96f), "MaderaOscura");

        var bc = g.gameObject.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, 0.5f, 0f);
        bc.size = new Vector3(0.95f, 1f, 0.95f);

        if (semilla % 3 == 0)
        {
            var s = Pieza(g, "CajaApilada", PrimitiveType.Cube, new Vector3(0.05f, 1.25f, 0f), new Vector3(0.6f, 0.6f, 0.6f), "MaderaClara");
            s.localRotation = Quaternion.Euler(0f, 20f, 0f);
        }
    }

    static void Candelabro(Transform padre, Vector3 pos)
    {
        var g = Grupo(padre, "Candelabro", pos);
        Pieza(g, "Base", PrimitiveType.Cylinder, new Vector3(0f, 0.1f, 0f), new Vector3(0.7f, 0.1f, 0.7f), "PiedraOscura");
        Pieza(g, "Poste", PrimitiveType.Cylinder, new Vector3(0f, 0.95f, 0f), new Vector3(0.14f, 0.85f, 0.14f), "Dorado");
        Pieza(g, "Copa", PrimitiveType.Cylinder, new Vector3(0f, 1.75f, 0f), new Vector3(0.4f, 0.06f, 0.4f), "Dorado");
        Pieza(g, "Cera", PrimitiveType.Cylinder, new Vector3(0f, 2.0f, 0f), new Vector3(0.2f, 0.25f, 0.2f), "Cera");
        Pieza(g, "Llama", PrimitiveType.Sphere, new Vector3(0f, 2.35f, 0f), new Vector3(0.16f, 0.26f, 0.16f), "Llama");
        Luz(g, new Vector3(0f, 2.6f, 0f), new Color(1f, 0.72f, 0.35f), 6f, 3f);

        var cc = g.gameObject.AddComponent<CapsuleCollider>();
        cc.center = new Vector3(0f, 1.2f, 0f);
        cc.radius = 0.3f;
        cc.height = 2.4f;
    }

    static void Altar(Transform padre, Vector3 pos, int ancho, int fondo)
    {
        var g = Grupo(padre, "Altar", pos);
        bool anchoX = ancho >= fondo;
        float W = anchoX ? ancho : fondo, D = anchoX ? fondo : ancho;
        if (!anchoX) g.localRotation = Quaternion.Euler(0f, 90f, 0f);

        Pieza(g, "Peana1", PrimitiveType.Cube, new Vector3(0f, 0.15f, 0f), new Vector3(W, 0.3f, D), "Piedra");
        Pieza(g, "Peana2", PrimitiveType.Cube, new Vector3(0f, 0.45f, 0f), new Vector3(W - 1.2f, 0.3f, D - 0.5f), "PiedraOscura");
        Pieza(g, "Mesa", PrimitiveType.Cube, new Vector3(0f, 1.05f, 0f), new Vector3(W - 3f, 0.9f, D - 1f), "Piedra");
        Pieza(g, "Mantel", PrimitiveType.Cube, new Vector3(0f, 1.54f, 0f), new Vector3(W - 2.6f, 0.08f, D - 0.8f), "Tela");
        Pieza(g, "Respaldo", PrimitiveType.Cube, new Vector3(0f, 2.2f, D / 2f + 0.3f), new Vector3(W + 1f, 4.4f, 0.4f), "PiedraOscura");

        // Cruz dorada
        Pieza(g, "CruzV", PrimitiveType.Cube, new Vector3(0f, 2.38f, D / 4f), new Vector3(0.14f, 1.6f, 0.14f), "Dorado");
        Pieza(g, "CruzH", PrimitiveType.Cube, new Vector3(0f, 2.75f, D / 4f), new Vector3(0.7f, 0.14f, 0.14f), "Dorado");

        // Velas en los extremos del mantel
        foreach (float sx in new[] { -1f, 1f })
        {
            float x = sx * ((W - 2.6f) / 2f - 0.4f);
            Pieza(g, "Vela", PrimitiveType.Cylinder, new Vector3(x, 1.83f, 0f), new Vector3(0.2f, 0.25f, 0.2f), "Cera");
            Pieza(g, "Llama", PrimitiveType.Sphere, new Vector3(x, 2.2f, 0f), new Vector3(0.16f, 0.26f, 0.16f), "Llama");
            Luz(g, new Vector3(x, 2.5f, -0.3f), new Color(1f, 0.72f, 0.35f), 7f, 3.5f);
        }

        // Ofrenda: mucbipollo y calaveras de azúcar sobre el mantel
        Mucbipollo(g, new Vector3(0f, 1.58f, -0.15f));
        Calavera(g, new Vector3(-1.5f, 1.58f, -0.1f), -1f);
        Calavera(g, new Vector3(1.5f, 1.58f, -0.1f), -1f);

        // Cempasúchil en las peanas
        int n = Mathf.FloorToInt(W / 0.45f) - 1;
        for (int i = 0; i < n; i++)
        {
            float x = -W / 2f + 0.3f + i * 0.45f;
            string mat = (i % 2 == 0) ? "Cempasuchil" : "Cempasuchil2";
            Pieza(g, "Flor", PrimitiveType.Cube, new Vector3(x, 0.42f, -D / 2f - 0.12f), new Vector3(0.3f, 0.3f, 0.3f), mat);
            if (x > -(W - 1.2f) / 2f && x < (W - 1.2f) / 2f)
                Pieza(g, "Flor", PrimitiveType.Cube, new Vector3(x, 0.72f, -(D - 0.5f) / 2f - 0.12f), new Vector3(0.28f, 0.28f, 0.28f), mat);
        }

        // Candelabros a cada lado del altar
        Candelabro(g, new Vector3(-(W / 2f + 1.3f), 0f, -0.2f));
        Candelabro(g, new Vector3(W / 2f + 1.3f, 0f, -0.2f));

        var bc = g.gameObject.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, 1f, 0f);
        bc.size = new Vector3(W, 2f, D);
    }

    // Altar pequeño de Hanal Pixán con arco de xpujuc (cempasúchil), mucbipollo, foto y velas.
    static void Ofrenda(Transform padre, Vector3 pos)
    {
        var g = Grupo(padre, "Ofrenda", pos);
        Vector3 haciaCentro = new Vector3(-pos.x, 0f, -pos.z);
        if (haciaCentro.sqrMagnitude > 0.01f) g.localRotation = Quaternion.LookRotation(haciaCentro.normalized, Vector3.up);

        Pieza(g, "Mesa", PrimitiveType.Cube, new Vector3(0f, 0.45f, 0f), new Vector3(2.6f, 0.9f, 1.1f), "MaderaOscura");
        Pieza(g, "Mantel", PrimitiveType.Cube, new Vector3(0f, 0.93f, 0f), new Vector3(2.7f, 0.05f, 1.2f), "Tela");
        string[] franjas = { "Pico2", "Pico3", "Pico4" };
        for (int i = 0; i < 3; i++)
            Pieza(g, "Franja", PrimitiveType.Cube, new Vector3(-0.9f + i * 0.9f, 0.94f, 0f), new Vector3(0.4f, 0.05f, 1.22f), franjas[i]);

        // Arco de cempasúchil
        Pieza(g, "PostIzq", PrimitiveType.Cube, new Vector3(-1.3f, 1.5f, -0.4f), new Vector3(0.12f, 2.1f, 0.12f), "MaderaClara");
        Pieza(g, "PostDer", PrimitiveType.Cube, new Vector3(1.3f, 1.5f, -0.4f), new Vector3(0.12f, 2.1f, 0.12f), "MaderaClara");
        Pieza(g, "Viga", PrimitiveType.Cube, new Vector3(0f, 2.55f, -0.4f), new Vector3(2.7f, 0.12f, 0.12f), "MaderaClara");
        for (int t = 0; t < 7; t++)
        {
            float y = 0.65f + t * 0.3f;
            string m = (t % 2 == 0) ? "Cempasuchil" : "Cempasuchil2";
            Pieza(g, "Flor", PrimitiveType.Cube, new Vector3(-1.3f, y, -0.4f), new Vector3(0.26f, 0.26f, 0.26f), m);
            Pieza(g, "Flor", PrimitiveType.Cube, new Vector3(1.3f, y, -0.4f), new Vector3(0.26f, 0.26f, 0.26f), m);
        }
        for (int t = 0; t < 9; t++)
            Pieza(g, "Flor", PrimitiveType.Cube, new Vector3(-1.2f + t * 0.3f, 2.55f, -0.4f), new Vector3(0.26f, 0.26f, 0.26f), (t % 2 == 0) ? "Cempasuchil2" : "Cempasuchil");

        // Foto de la persona que se recuerda
        Pieza(g, "Marco", PrimitiveType.Cube, new Vector3(0f, 1.5f, -0.38f), new Vector3(0.62f, 0.82f, 0.06f), "Dorado");
        Pieza(g, "Foto", PrimitiveType.Cube, new Vector3(0f, 1.5f, -0.34f), new Vector3(0.46f, 0.66f, 0.05f), "Cera");

        // Comida y calaveras de azúcar
        Mucbipollo(g, new Vector3(-0.55f, 0.95f, 0.1f));
        Calavera(g, new Vector3(0.55f, 0.95f, 0.1f), 1f);
        Calavera(g, new Vector3(0.95f, 0.95f, 0.15f), 1f);

        // Vela central
        Pieza(g, "Vela", PrimitiveType.Cylinder, new Vector3(0f, 1.13f, 0.15f), new Vector3(0.18f, 0.18f, 0.18f), "Cera");
        Pieza(g, "Llama", PrimitiveType.Sphere, new Vector3(0f, 1.4f, 0.15f), new Vector3(0.14f, 0.22f, 0.14f), "Llama");
        Luz(g, new Vector3(0f, 1.75f, 0.3f), new Color(1f, 0.72f, 0.35f), 6f, 2.5f);

        // Candelabros a cada lado
        Candelabro(g, new Vector3(-2.0f, 0f, 0.3f));
        Candelabro(g, new Vector3(2.0f, 0f, 0.3f));

        var bc = g.gameObject.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, 0.5f, 0f);
        bc.size = new Vector3(2.7f, 1f, 1.2f);
    }

    static void Mucbipollo(Transform padre, Vector3 pos)
    {
        var g = Grupo(padre, "Mucbipollo", pos);
        Pieza(g, "Hoja", PrimitiveType.Cube, new Vector3(0f, 0.02f, 0f), new Vector3(0.9f, 0.04f, 0.6f), "Hoja");
        Pieza(g, "Tamal", PrimitiveType.Cube, new Vector3(0f, 0.15f, 0f), new Vector3(0.7f, 0.22f, 0.45f), "Mucbi");
    }

    // frente = -1 si la cara mira hacia -Z, +1 si mira hacia +Z
    static void Calavera(Transform padre, Vector3 pos, float frente)
    {
        var g = Grupo(padre, "CalaveraAzucar", pos);
        Pieza(g, "Craneo", PrimitiveType.Cube, new Vector3(0f, 0.17f, 0f), new Vector3(0.3f, 0.3f, 0.3f), "Cera");
        Pieza(g, "OjoIzq", PrimitiveType.Cube, new Vector3(-0.07f, 0.2f, 0.152f * frente), new Vector3(0.07f, 0.07f, 0.02f), "Ojo");
        Pieza(g, "OjoDer", PrimitiveType.Cube, new Vector3(0.07f, 0.2f, 0.152f * frente), new Vector3(0.07f, 0.07f, 0.02f), "Ojo");
        Pieza(g, "Adorno", PrimitiveType.Cube, new Vector3(0f, 0.28f, 0.152f * frente), new Vector3(0.07f, 0.05f, 0.02f), "Pico1");
    }

    static void Marcador(Transform padre, string nombre, Vector3 pos, float escala, string material)
    {
        var g = Grupo(padre, nombre, pos);
        Pieza(g, "Marcador", PrimitiveType.Sphere, new Vector3(0f, 0.6f, 0f), Vector3.one * escala, material);
    }

    // ---------------- Utilidades ----------------

    static Transform Grupo(Transform padre, string nombre, Vector3 pos)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        return go.transform;
    }

    static Transform Pieza(Transform padre, string nombre, PrimitiveType tipo, Vector3 pos, Vector3 esc, string material)
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

    static void Luz(Transform padre, Vector3 pos, Color color, float alcance, float intensidad)
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
    }

    static Vector3 Pos(int x, int y, int w, int h)
    {
        return new Vector3(x - w / 2f + 0.5f, 0f, y - h / 2f + 0.5f);
    }

    static void Rango(List<Vector2Int> c, int w, int h, out Vector3 centro, out int ancho, out int fondo)
    {
        int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
        foreach (var p in c)
        {
            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.y > maxY) maxY = p.y;
        }
        ancho = maxX - minX + 1;
        fondo = maxY - minY + 1;
        centro = new Vector3((minX + maxX) / 2f - w / 2f + 0.5f, 0f, (minY + maxY) / 2f - h / 2f + 0.5f);
    }

    static List<List<Vector2Int>> Componentes(int[,] mapa, int w, int h, Tipo t)
    {
        var visto = new bool[w, h];
        var res = new List<List<Vector2Int>>();
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (visto[x, y] || mapa[x, y] != (int)t) continue;
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
                        int nx = c.x + d.x, ny = c.y + d.y;
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                        if (visto[nx, ny] || mapa[nx, ny] != (int)t) continue;
                        visto[nx, ny] = true;
                        pila.Push(new Vector2Int(nx, ny));
                    }
                }
                res.Add(comp);
            }
        return res;
    }

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

    static void Sumar(Dictionary<string, List<CombineInstance>> g, string clave, Mesh m, Vector3 pos, Vector3 esc)
    {
        if (!g.ContainsKey(clave)) g[clave] = new List<CombineInstance>();
        SumarLista(g[clave], m, pos, esc);
    }

    static void SumarLista(List<CombineInstance> lista, Mesh m, Vector3 pos, Vector3 esc)
    {
        var ci = new CombineInstance();
        ci.mesh = m;
        ci.transform = Matrix4x4.TRS(pos, Quaternion.identity, esc);
        lista.Add(ci);
    }

    static Mesh GuardarMalla(string nombre, List<CombineInstance> lista)
    {
        var malla = new Mesh();
        malla.name = "Iglesia_" + nombre;
        malla.indexFormat = IndexFormat.UInt32;
        malla.CombineMeshes(lista.ToArray(), true, true);
        string ruta = "Assets/Niveles/Iglesia/Malla_" + nombre + ".asset";
        AssetDatabase.DeleteAsset(ruta);
        AssetDatabase.CreateAsset(malla, ruta);
        return malla;
    }

    static Material M(string nombre)
    {
        Material hecho;
        if (cache.TryGetValue(nombre, out hecho)) return hecho;

        string ruta = "Assets/Niveles/Iglesia/Mat_" + nombre + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            m = new Material(sh);
            AssetDatabase.CreateAsset(m, ruta);
        }
        m.color = colores[nombre];
        if (nombre.StartsWith("Llama") || nombre.StartsWith("Vitral"))
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", colores[nombre] * 2.5f);
        }
        EditorUtility.SetDirty(m);
        cache[nombre] = m;
        return m;
    }

    // Oscuridad ambiental: sin cielo, fondo negro.
    static void ConfigurarEscena()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.14f, 0.10f, 0.20f);

        foreach (var l in Object.FindObjectsByType<Light>())
        {
            if (l.type != LightType.Directional) continue;
            l.intensity = 0.25f;
            l.color = new Color(0.6f, 0.65f, 1f);
            l.shadows = LightShadows.None;
        }

        var cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.transform.position = new Vector3(0f, 30f, -12f);
            cam.transform.rotation = Quaternion.Euler(65f, 0f, 0f);
        }
    }
}