// Saatan Historia - Iglesia con paredes, vitrales, altares y ofrendas de Hanal Pixán.
// Usa Assets/Project/Niveles/Mapas/iglesia_detallada (2).png (o la imagen seleccionada en Project).
// Cada píxel del mapa mide ESCALA metros. La iglesia está abandonada: techo en cúpula muy alto con
// vitrales por donde entra luz natural, cuatro pilares con un candelabro en el centro, cajas y escombros.
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

    const float ESCALA = 1.5f;              // metros por píxel del mapa (más grande y amplia)
    const float RadioPared = 19.6f * ESCALA;
    const int AltoPared = 20;               // altura de los muros
    const float AltoCupula = 14f;           // la cúpula sube 14 m más (cima a ~34 m)
    const float LadoPilares = 5f;           // pilares del candelabro en (±5, ±5)
    const float AltoPilares = 18f;          // pilares del candelabro
    const float NaveX = 2.6f, AltoNave = 16f;   // filas de pilares a ambos lados del pasillo central
    const int VitralDesde = 12, VitralFilas = 5;  // vitrales altos: de 12 a 17 m
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
        { "Techo", new Color(0.30f, 0.26f, 0.36f) },
        { "TechoOscuro", new Color(0.20f, 0.17f, 0.25f) },
        { "Hierro", new Color(0.12f, 0.11f, 0.10f) },
        { "RayoRojo", new Color(0.30f, 0.06f, 0.05f, 1f) },
        { "RayoAzul", new Color(0.06f, 0.10f, 0.30f, 1f) },
        { "RayoVerde", new Color(0.05f, 0.22f, 0.10f, 1f) },
        { "RayoAmarillo", new Color(0.30f, 0.24f, 0.08f, 1f) },
        { "RayoPolvo", new Color(0.55f, 0.50f, 0.40f, 1f) },
    };

    static readonly string[] vidrios = { "Rojo", "Azul", "Amarillo", "Verde" };
    static System.Random rnd;

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
        if (tex == null) tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Project/Niveles/Mapas/iglesia_detallada (2).png");
        if (tex == null)
        {
            EditorUtility.DisplayDialog("Saatan", "No se encontró iglesia_detallada (2).png en Assets/Project/Niveles/Mapas.", "OK");
            return;
        }
        rnd = new System.Random(7);

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

        if (!AssetDatabase.IsValidFolder("Assets/Project/Niveles")) AssetDatabase.CreateFolder("Assets/Project", "Niveles");
        if (!AssetDatabase.IsValidFolder("Assets/Project/Niveles/Iglesia")) AssetDatabase.CreateFolder("Assets/Project/Niveles", "Iglesia");
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

        // El centro y el lugar de los pilares de la nave quedan libres.
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Vector3 p = Pos(x, y, w, h);
                var t = (Tipo)mapa[x, y];
                bool libre = Mathf.Abs(p.x) < LadoPilares + 1.5f && Mathf.Abs(p.z) < LadoPilares + 1.5f;
                foreach (var c in PosNave()) if ((c - p).sqrMagnitude < 2.2f * 2.2f) libre = true;
                if (libre && (t == Tipo.Banca || t == Tipo.Caja || t == Tipo.Vela)) mapa[x, y] = (int)Tipo.Piso;
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
                    bool alfombra = Mathf.Abs(p.x) <= 0.51f * ESCALA && p.z > -16f * ESCALA && p.z < 15f * ESCALA;
                    if (alfombra) Sumar(visibles, "Alfombra", cubo, new Vector3(p.x, -0.09f, p.z), new Vector3(ESCALA, 0.22f, ESCALA));
                    else Sumar(visibles, ((x + y) % 2 == 0) ? "PisoA" : "PisoB", cubo, new Vector3(p.x, -0.1f, p.z), new Vector3(ESCALA * 0.97f, 0.2f, ESCALA * 0.97f));
                }

                SumarLista(colision, cubo, new Vector3(p.x, -0.1f, p.z), new Vector3(ESCALA, 0.2f, ESCALA));
                if (limite) SumarLista(colision, cubo, new Vector3(p.x, AltoPared / 2f, p.z), new Vector3(ESCALA, AltoPared, ESCALA));
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

        // ---- Paredes, vitrales altos, cúpula y pilares ----
        int[] pilastras = { 0, 8, 18, 41, 51, 59, 75, 90, 105 };
        var altas = new List<int>();   // vitrales altos (2 segmentos de ancho) lejos de las pilastras
        for (float k = 4f; k < Segmentos; k += 7.5f)
        {
            int i = Mathf.FloorToInt(k);
            bool libre = true;
            foreach (int pk in pilastras)
                if (Mathf.Abs(Mathf.DeltaAngle(i * 3f, pk * 3f)) < 9f || Mathf.Abs(Mathf.DeltaAngle((i + 1) * 3f, pk * 3f)) < 9f) libre = false;
            if (libre) altas.Add(i);
        }

        Pared(raiz, ventanas, altas);
        foreach (int k in ventanas) Ventana(raiz, k);
        for (int i = 0; i < altas.Count; i++) VentanaAlta(raiz, altas[i], vidrios[i % 4]);
        foreach (int k in pilastras) Pilastra(raiz, k);
        Banderines(raiz);
        Cupula(raiz);
        PilaresCandelabro(raiz);
        PilaresNave(raiz);
        Polvo(raiz);

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

        Abandono(raiz);
        // Al terminar el nivel (vencer al Diablo), llamar a  Fin_Nivel.GetComponent<CambiarEscena>().Cargar():
        // pasa a la cinemática del altar.
        Grupo(raiz, "Fin_Nivel", Vector3.zero).gameObject.AddComponent<CambiarEscena>().escena = "AltarCInematica";
        HerramientasIktan.DarVidaAlFuego(raiz.gameObject);
        ConfigurarEscena();
        HerramientasIktan.CrearJugador();   // Iktan en el nuevo punto de inicio, con la cámara en tercera persona

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkAllScenesDirty();
        Selection.activeGameObject = raiz.gameObject;
        Debug.Log("Iglesia creada con " + ventanas.Count + " vitrales. Mallas y materiales en Assets/Project/Niveles/Iglesia.");
    }

    // ---------------- Paredes ----------------

    // Muro curvo con caras solo hacia adentro: desde la cámara se ve el interior
    // de los muros del fondo y los muros cercanos no estorban la vista.
    static void Pared(Transform raiz, HashSet<int> ventanas, List<int> altas)
    {
        var abre = new HashSet<int>();
        foreach (int k in ventanas)
            for (int d = -1; d <= 1; d++) abre.Add(((k + d) % Segmentos + Segmentos) % Segmentos);
        var abreAlto = new HashSet<int>();
        foreach (int k in altas) { abreAlto.Add(k); abreAlto.Add((k + 1) % Segmentos); }

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
                if (abre.Contains(i) && j >= 1 && j <= 3) continue;        // hueco del vitral bajo
                if (abreAlto.Contains(i) && j >= VitralDesde && j < VitralDesde + VitralFilas) continue;   // hueco del vitral alto
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

            string rutaMalla = "Assets/Project/Niveles/Iglesia/Malla_" + nombres[t] + ".asset";
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
        Pieza(g, "Fuste", PrimitiveType.Cube, new Vector3(0f, AltoPared / 2f, 0.25f), new Vector3(1.1f, AltoPared, 0.7f), "Piedra");
        Pieza(g, "Capitel", PrimitiveType.Cube, new Vector3(0f, AltoPared - 0.2f, 0.3f), new Vector3(1.5f, 0.35f, 0.9f), "PiedraOscura");
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

    // Vitral alto de 2 segmentos (≈3 m). La luz natural del color del vidrio se abre y se difumina al bajar.
    static void VentanaAlta(Transform padre, int k, string color)
    {
        const float y0 = VitralDesde, alto = VitralFilas, ym = y0 + alto / 2f;
        var g = EnPared(padre, "Vitral_Alto", (k + 1f) * (360f / Segmentos), RadioPared);
        Pieza(g, "MarcoIzq", PrimitiveType.Cube, new Vector3(-1.65f, ym, 0.1f), new Vector3(0.3f, alto + 0.2f, 0.5f), "PiedraOscura");
        Pieza(g, "MarcoDer", PrimitiveType.Cube, new Vector3(1.65f, ym, 0.1f), new Vector3(0.3f, alto + 0.2f, 0.5f), "PiedraOscura");
        Pieza(g, "Repisa", PrimitiveType.Cube, new Vector3(0f, y0 - 0.1f, 0.2f), new Vector3(3.6f, 0.25f, 0.6f), "Piedra");
        Pieza(g, "Arco", PrimitiveType.Cube, new Vector3(0f, y0 + alto + 0.15f, 0.1f), new Vector3(3.6f, 0.35f, 0.5f), "PiedraOscura");
        for (int fila = 0; fila < VitralFilas; fila++)
            for (int col = 0; col < 3; col++)
            {
                string c = (fila + col) % 3 == 0 ? "Amarillo" : (fila + col) % 3 == 1 ? color : vidrios[(fila * 3 + col + k) % 4];
                if (rnd.NextDouble() < 0.12) continue;   // vidrios rotos
                Pieza(g, "Cristal", PrimitiveType.Cube, new Vector3(-1.05f + col * 1.05f, y0 + 0.5f + fila, -0.05f), new Vector3(1f, 0.95f, 0.08f), "Vitral" + c);
            }

        Vector3 dir = new Vector3(0f, -0.8f, 1f).normalized, desde = new Vector3(0f, ym, 0.6f);
        float largo = ym / -dir.y;   // hasta el piso
        Foco(g, desde, dir, ColorLuz(color), largo + 15f, 100f, 9f);                        // cono amplio y suave
        Luz(g, desde + dir * largo + Vector3.up * 1.5f, ColorLuz(color), 11f, 1.6f);          // luz que rebota en el piso
        Rayo(g, desde, dir, largo, 2.8f, "Rayo" + color);
        Rayo(g, desde, dir, largo * 0.9f, 4.5f, "Rayo" + color);                              // halo exterior, más tenue
    }

    // Cúpula de piedra sobre los muros, con un rosetón de vidrio en lo más alto.
    static void Cupula(Transform raiz)
    {
        const int anillos = 10;
        const float rOculo = 3f;
        var tonos = new[] { new Acum(), new Acum() };
        float paso = 360f / Segmentos * Mathf.Deg2Rad, tMax = Mathf.Acos(rOculo / RadioPared);
        System.Func<float, float, Vector3> P = (a, t) =>
            new Vector3(RadioPared * Mathf.Cos(t) * Mathf.Cos(a), AltoPared + AltoCupula * Mathf.Sin(t), RadioPared * Mathf.Cos(t) * Mathf.Sin(a));

        for (int i = 0; i < Segmentos; i++)
            for (int k = 0; k < anillos; k++)
            {
                float a0 = i * paso, a1 = (i + 1) * paso, t0 = tMax * k / anillos, t1 = tMax * (k + 1) / anillos;
                float am = (a0 + a1) / 2f, tm = (t0 + t1) / 2f;
                var n = new Vector3(-Mathf.Cos(am) * Mathf.Cos(tm), -Mathf.Sin(tm), -Mathf.Sin(am) * Mathf.Cos(tm));
                Quad(tonos[i % 15 < 2 || k == 0 ? 1 : 0], P(a0, t0), P(a1, t0), P(a1, t1), P(a0, t1), n);   // nervios más oscuros
            }

        string[] nombres = { "Techo", "TechoOscuro" };
        for (int t = 0; t < 2; t++)
        {
            var malla = new Mesh { name = "Iglesia_" + nombres[t] };
            malla.SetVertices(tonos[t].v);
            malla.SetNormals(tonos[t].n);
            malla.SetTriangles(tonos[t].t, 0);
            malla.RecalculateBounds();
            string ruta = "Assets/Project/Niveles/Iglesia/Malla_" + nombres[t] + ".asset";
            AssetDatabase.DeleteAsset(ruta);
            AssetDatabase.CreateAsset(malla, ruta);
            var go = new GameObject("Cupula_" + nombres[t]);
            go.transform.SetParent(raiz, false);
            go.AddComponent<MeshFilter>().sharedMesh = malla;
            go.AddComponent<MeshRenderer>().sharedMaterial = M(nombres[t]);
            go.isStatic = true;
        }

        // Rosetón: luz natural desde arriba sobre el candelabro.
        float yTop = AltoPared + AltoCupula * Mathf.Sin(tMax);
        var g = Grupo(raiz, "Roseton", new Vector3(0f, yTop, 0f));
        Pieza(g, "Aro", PrimitiveType.Cylinder, Vector3.zero, new Vector3(rOculo * 2.2f, 0.15f, rOculo * 2.2f), "PiedraOscura");
        Pieza(g, "Vidrio", PrimitiveType.Cylinder, Vector3.down * 0.05f, new Vector3(rOculo * 2f, 0.06f, rOculo * 2f), "VitralAzul");
        for (int i = 0; i < 8; i++)
        {
            var p = Pieza(g, "Petalo", PrimitiveType.Cube, Quaternion.Euler(0f, i * 45f, 0f) * new Vector3(0f, -0.1f, 1.5f), new Vector3(0.9f, 0.05f, 1.8f), "Vitral" + vidrios[i % 4]);
            p.localRotation = Quaternion.Euler(0f, i * 45f, 0f);
        }
        Pieza(g, "Centro", PrimitiveType.Cylinder, Vector3.down * 0.12f, new Vector3(1.2f, 0.06f, 1.2f), "VitralAmarillo");
        Foco(g, Vector3.down * 0.5f, Vector3.down, new Color(1f, 0.92f, 0.75f), yTop + 10f, 90f, 12f);
        Rayo(g, Vector3.down * 0.3f, Vector3.down, yTop, rOculo * 1.8f, "RayoAmarillo");
        Rayo(g, Vector3.down * 0.3f, Vector3.down, yTop * 0.9f, rOculo * 3f, "RayoAmarillo");
    }

    // Cuatro pilares de piedra con vigas que sostienen un candelabro de hierro colgante.
    static void PilaresCandelabro(Transform raiz)
    {
        const float H = AltoPilares, L = LadoPilares;
        var g = Grupo(raiz, "Pilares_Candelabro", Vector3.zero);
        foreach (float sx in new[] { -L, L })
            foreach (float sz in new[] { -L, L })
            {
                var p = Grupo(g, "Pilar", new Vector3(sx, 0f, sz));
                Pieza(p, "Base", PrimitiveType.Cube, new Vector3(0f, 0.35f, 0f), new Vector3(1.9f, 0.7f, 1.9f), "PiedraOscura");
                Pieza(p, "Fuste", PrimitiveType.Cylinder, new Vector3(0f, H / 2f, 0f), new Vector3(1.2f, H / 2f, 1.2f), "Piedra");
                Pieza(p, "Capitel", PrimitiveType.Cube, new Vector3(0f, H, 0f), new Vector3(1.8f, 0.6f, 1.8f), "PiedraOscura");
                var bc = p.gameObject.AddComponent<BoxCollider>();
                bc.center = new Vector3(0f, H / 2f, 0f);
                bc.size = new Vector3(1.6f, H, 1.6f);
            }

        // Vigas entre pilares y en diagonal hasta el centro.
        foreach (float s in new[] { -L, L })
        {
            Pieza(g, "Viga", PrimitiveType.Cube, new Vector3(0f, H + 0.6f, s), new Vector3(2f * L + 1.2f, 0.6f, 0.7f), "MaderaOscura");
            Pieza(g, "Viga", PrimitiveType.Cube, new Vector3(s, H + 0.6f, 0f), new Vector3(0.7f, 0.6f, 2f * L + 1.2f), "MaderaOscura");
        }
        foreach (float ang in new[] { 45f, -45f })
            Pieza(g, "Diagonal", PrimitiveType.Cube, new Vector3(0f, H + 1.1f, 0f), new Vector3(0.5f, 0.45f, 2.83f * L), "MaderaOscura").localRotation = Quaternion.Euler(0f, ang, 0f);

        // Candelabro colgando de una cadena.
        const float Y = 10f, R = 2.6f;
        Pieza(g, "Cadena", PrimitiveType.Cube, new Vector3(0f, (H + 1f + Y) / 2f, 0f), new Vector3(0.1f, H + 1f - Y, 0.1f), "Hierro");
        var c = Grupo(g, "Candelabro_Colgante", new Vector3(0f, Y, 0f));
        Pieza(c, "Centro", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.6f, 0.3f, 0.6f), "Hierro");
        for (int i = 0; i < 16; i++)
        {
            float a = i * 22.5f;
            Pieza(c, "Aro", PrimitiveType.Cube, Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0f, R), new Vector3(0.9f, 0.12f, 0.12f), "Hierro").localRotation = Quaternion.Euler(0f, a, 0f);
        }
        for (int i = 0; i < 4; i++)
            Pieza(c, "Brazo", PrimitiveType.Cube, Vector3.zero, new Vector3(0.1f, 0.1f, 2f * R), "Hierro").localRotation = Quaternion.Euler(0f, i * 45f, 0f);
        for (int i = 0; i < 8; i++)
        {
            Vector3 v = Quaternion.Euler(0f, i * 45f + 11.25f, 0f) * new Vector3(0f, 0f, R);
            Pieza(c, "Vela", PrimitiveType.Cylinder, v + Vector3.up * 0.3f, new Vector3(0.18f, 0.25f, 0.18f), "Cera");
            Pieza(c, "Llama", PrimitiveType.Sphere, v + Vector3.up * 0.68f, new Vector3(0.15f, 0.25f, 0.15f), "Llama");
            if (i % 2 == 0) Luz(c, v + Vector3.up * 1f, new Color(1f, 0.72f, 0.35f), 12f, 3f);
        }
        Luz(c, Vector3.down * 0.5f, new Color(1f, 0.75f, 0.4f), 20f, 2.5f);
    }

    // Aspecto abandonado: bancas movidas o volcadas, cajas extra, escombros y tablas tiradas.
    static void Abandono(Transform raiz)
    {
        var ocupado = new List<Vector3>();
        foreach (Transform t in raiz)
        {
            if (t.name == "Banca" && rnd.NextDouble() < 0.4)
            {
                bool volcada = rnd.NextDouble() < 0.35;
                t.localRotation *= Quaternion.Euler(volcada ? 80f : 0f, Azar(18f), volcada ? 0f : Azar(4f));
                if (volcada) t.localPosition += Vector3.up * 0.5f;
            }
            if (!t.name.StartsWith("Pared") && !t.name.StartsWith("Cupula") && !t.name.StartsWith("Piso") && t.name != "Banderin")
                ocupado.Add(t.localPosition);
        }

        System.Func<float, float, Vector3> Libre = (rMin, rMax) =>
        {
            for (int intento = 0; intento < 40; intento++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f, r = Mathf.Lerp(rMin, rMax, (float)rnd.NextDouble());
                var p = new Vector3(r * Mathf.Cos(a), 0f, r * Mathf.Sin(a));
                bool ok = Mathf.Abs(p.x) > LadoPilares + 2f || Mathf.Abs(p.z) > LadoPilares + 2f;
                foreach (var q in ocupado) if (ok && (q - p).sqrMagnitude < 6.5f) ok = false;
                if (ok) { ocupado.Add(p); return p; }
            }
            return Vector3.positiveInfinity;
        };

        for (int i = 0; i < 32; i++)   // cajas abandonadas, sobre todo junto a los muros
        {
            var p = Libre(i < 24 ? RadioPared - 6f : 8f, RadioPared - 2f);
            if (float.IsInfinity(p.x)) continue;
            Caja(raiz, p, rnd.Next(1000));
            if (rnd.NextDouble() < 0.4) Caja(raiz, p + new Vector3(Azar(1f), 0f, 1.1f), rnd.Next(1000));
        }

        for (int i = 0; i < 16; i++)   // escombros y tablas caídas
        {
            var p = Libre(6f, RadioPared - 1.5f);
            if (float.IsInfinity(p.x)) continue;
            var g = Grupo(raiz, "Escombros", p);
            for (int j = 0; j < 7; j++)
            {
                float s = Mathf.Lerp(0.25f, 0.9f, (float)rnd.NextDouble());
                Pieza(g, "Piedra", PrimitiveType.Cube, new Vector3(Azar(1.2f), s * 0.35f, Azar(1.2f)), Vector3.one * s, j % 3 == 0 ? "Piedra" : "PiedraOscura")
                    .localRotation = Quaternion.Euler(Azar(30f), Azar(180f), Azar(30f));
            }
            if (rnd.NextDouble() < 0.6)
                Pieza(g, "Tabla", PrimitiveType.Cube, new Vector3(0f, 0.08f, 0f), new Vector3(2.8f, 0.12f, 0.3f), "MaderaOscura")
                    .localRotation = Quaternion.Euler(0f, Azar(180f), Azar(8f));
        }
    }

    static Quaternion Mirar(Vector3 dir) => Quaternion.LookRotation(dir, Mathf.Abs(dir.y) > 0.99f ? Vector3.forward : Vector3.up);

    static float Azar(float a) => ((float)rnd.NextDouble() * 2f - 1f) * a;

    static Color ColorLuz(string vidrio) =>
        vidrio == "Rojo" ? new Color(1f, 0.55f, 0.5f) : vidrio == "Azul" ? new Color(0.6f, 0.7f, 1f)
        : vidrio == "Verde" ? new Color(0.6f, 1f, 0.7f) : new Color(1f, 0.9f, 0.6f);

    static void Foco(Transform padre, Vector3 pos, Vector3 dir, Color color, float alcance, float angulo, float intensidad)
    {
        var go = new GameObject("Luz_Natural");
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Mirar(dir);
        var l = go.AddComponent<Light>();
        l.type = LightType.Spot;
        l.color = color;
        l.range = alcance;
        l.spotAngle = angulo;
        l.intensity = intensidad;
        l.shadows = LightShadows.None;
    }

    // Haz de luz visible: cono que se abre (x3) y se desvanece hacia el final, con material aditivo.
    static void Rayo(Transform padre, Vector3 desde, Vector3 dir, float largo, float grosor, string material)
    {
        var go = new GameObject("Haz_Luz");
        go.transform.SetParent(padre, false);
        go.transform.localPosition = desde;
        go.transform.localRotation = Mirar(dir);
        go.transform.localScale = new Vector3(grosor, grosor, largo);
        go.AddComponent<MeshFilter>().sharedMesh = MallaHaz();
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = M(material);
        r.shadowCastingMode = ShadowCastingMode.Off;
    }

    static Mesh haz;
    static Mesh MallaHaz()
    {
        if (haz != null) return haz;
        const int lados = 24;
        float[] z = { 0f, 0.15f, 1f }, radio = { 0.5f, 0.7f, 1.5f }, brillo = { 0.5f, 1f, 0f };
        var v = new List<Vector3>(); var col = new List<Color>(); var t = new List<int>();
        for (int a = 0; a < 3; a++)
            for (int i = 0; i <= lados; i++)
            {
                float ang = i * Mathf.PI * 2f / lados;
                v.Add(new Vector3(Mathf.Cos(ang) * radio[a], Mathf.Sin(ang) * radio[a], z[a]));
                col.Add(new Color(brillo[a], brillo[a], brillo[a], brillo[a]));
            }
        for (int a = 0; a < 2; a++)
            for (int i = 0; i < lados; i++)
            {
                int p = a * (lados + 1) + i, q = p + lados + 1;
                t.AddRange(new[] { p, q, p + 1, p + 1, q, q + 1 });
            }
        haz = new Mesh { name = "Iglesia_Haz" };
        haz.SetVertices(v);
        haz.SetColors(col);
        haz.SetTriangles(t, 0);
        haz.RecalculateNormals();
        haz.RecalculateBounds();
        AssetDatabase.DeleteAsset("Assets/Project/Niveles/Iglesia/Malla_Haz.asset");
        AssetDatabase.CreateAsset(haz, "Assets/Project/Niveles/Iglesia/Malla_Haz.asset");
        return haz;
    }

    // Posiciones de los pilares de la nave: dos filas junto al pasillo central (sin tapar el candelabro).
    static List<Vector3> PosNave()
    {
        var l = new List<Vector3>();
        for (float z = -24f; z <= 22f; z += 6.5f)
            if (Mathf.Abs(z) > 7.5f)
                foreach (float x in new[] { -NaveX, NaveX }) l.Add(new Vector3(x, 0f, z));
        return l;
    }

    static void PilaresNave(Transform raiz)
    {
        var l = PosNave();
        for (int i = 0; i < l.Count; i++)
        {
            var p = Grupo(raiz, "Pilar_Nave", l[i]);
            Pieza(p, "Base", PrimitiveType.Cube, new Vector3(0f, 0.3f, 0f), new Vector3(1.5f, 0.6f, 1.5f), "PiedraOscura");
            Pieza(p, "Fuste", PrimitiveType.Cylinder, new Vector3(0f, AltoNave / 2f, 0f), new Vector3(1f, AltoNave / 2f, 1f), "Piedra");
            Pieza(p, "Capitel", PrimitiveType.Cube, new Vector3(0f, AltoNave, 0f), new Vector3(1.4f, 0.5f, 1.4f), "PiedraOscura");
            var cc = p.gameObject.AddComponent<CapsuleCollider>();
            cc.center = new Vector3(0f, AltoNave / 2f, 0f);
            cc.radius = 0.6f;
            cc.height = AltoNave;

            // Arco (dintel) hasta el siguiente pilar de la misma fila.
            if (i + 2 < l.Count && l[i + 2].z - l[i].z < 7f)
                Pieza(raiz, "Dintel_Nave", PrimitiveType.Cube, (l[i] + l[i + 2]) / 2f + Vector3.up * (AltoNave + 0.6f), new Vector3(0.9f, 0.8f, l[i + 2].z - l[i].z + 1.4f), "PiedraOscura");
        }
    }

    // Polvo flotando en el aire: se nota sobre todo dentro de los haces de luz.
    static void Polvo(Transform raiz)
    {
        var go = new GameObject("Polvo");
        go.transform.SetParent(raiz, false);
        go.transform.localPosition = Vector3.up * 8f;
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.startLifetime = 18f;
        main.startSpeed = 0.08f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
        main.startColor = new Color(1f, 0.9f, 0.7f, 0.6f);
        main.maxParticles = 1500;
        main.prewarm = true;
        var em = ps.emission;
        em.rateOverTime = 70f;
        var forma = ps.shape;
        forma.shapeType = ParticleSystemShapeType.Box;
        forma.scale = new Vector3(RadioPared * 1.4f, 16f, RadioPared * 1.4f);
        var ruido = ps.noise;
        ruido.enabled = true;
        ruido.strength = 0.15f;
        ruido.frequency = 0.2f;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = M("RayoPolvo");
        r.shadowCastingMode = ShadowCastingMode.Off;
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
        return new Vector3((x - w / 2f + 0.5f) * ESCALA, 0f, (y - h / 2f + 0.5f) * ESCALA);
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
        centro = new Vector3(((minX + maxX) / 2f - w / 2f + 0.5f) * ESCALA, 0f, ((minY + maxY) / 2f - h / 2f + 0.5f) * ESCALA);
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
        string ruta = "Assets/Project/Niveles/Iglesia/Malla_" + nombre + ".asset";
        AssetDatabase.DeleteAsset(ruta);
        AssetDatabase.CreateAsset(malla, ruta);
        return malla;
    }

    static Material M(string nombre)
    {
        Material hecho;
        if (cache.TryGetValue(nombre, out hecho)) return hecho;

        string ruta = "Assets/Project/Niveles/Iglesia/Mat_" + nombre + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            m = new Material(sh);
            AssetDatabase.CreateAsset(m, ruta);
        }
        if (nombre.StartsWith("Rayo"))
        {
            // Unlit transparente aditivo (sin ZWrite, visible por ambos lados).
            m.shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");   // usa el color de vértice (desvanecido)
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 2f);
            m.SetFloat("_Cull", 0f);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.One);
            m.SetFloat("_DstBlend", (float)BlendMode.One);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.One);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetColor("_BaseColor", colores[nombre]);
            EditorUtility.SetDirty(m);
            cache[nombre] = m;
            return m;
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

    // Interior en penumbra: la luz entra por los vitrales y el rosetón. Fondo negro, cámara de SeguirCamara.
    static void ConfigurarEscena()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.20f, 0.17f, 0.24f);
        RenderSettings.fog = true;   // polvo en el aire
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.05f, 0.04f, 0.06f);
        RenderSettings.fogStartDistance = 20f;
        RenderSettings.fogEndDistance = 75f;

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
        }
    }
}