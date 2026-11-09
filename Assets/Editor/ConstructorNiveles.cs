// Saatan Historia - Constructor de niveles a partir de una imagen de cuadritos.
// Colocar este archivo en: Assets/Editor/ConstructorNiveles.cs
//
// Cada pixel de la imagen es un cubo de 1 x 1 unidad:
//   BLANCO  piso            NEGRO   muro / árboles (alto)
//   VERDE   hierba alta     CAFÉ    banca / caja / tronco (bajo)
//   GRIS    piedra / ruinas ROJO    aparición del enemigo (Diablo / Nahual)
//   AZUL    inicio de Iktan AMARILLO meta (vitral / recompensa)
//   Píxel transparente = vacío
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class ConstructorNiveles
{
    enum Tipo { Piso, Muro, Hierba, Obstaculo, Piedra, Enemigo, Inicio, Meta }

    static readonly (Color32 color, Tipo tipo)[] paleta =
    {
        (new Color32(255, 255, 255, 255), Tipo.Piso),
        (new Color32(0, 0, 0, 255),       Tipo.Muro),
        (new Color32(0, 170, 0, 255),     Tipo.Hierba),
        (new Color32(140, 80, 30, 255),   Tipo.Obstaculo),
        (new Color32(128, 128, 128, 255), Tipo.Piedra),
        (new Color32(230, 0, 0, 255),     Tipo.Enemigo),
        (new Color32(0, 100, 255, 255),   Tipo.Inicio),
        (new Color32(255, 220, 0, 255),   Tipo.Meta),
    };

    [MenuItem("Saatan/Construir nivel: Iglesia")]
    static void Iglesia() { Construir("Iglesia", 3f, false); }

    [MenuItem("Saatan/Construir nivel: Selva")]
    static void Selva() { Construir("Selva", 4f, true); }

    static void Construir(string nombre, float altoMuro, bool selva)
    {
        var tex = Selection.activeObject as Texture2D;
        if (tex == null)
        {
            EditorUtility.DisplayDialog("Saatan", "Primero selecciona la imagen del nivel en la ventana Project y vuelve a intentarlo.", "OK");
            return;
        }

        // La imagen debe poder leerse y sin suavizado ni compresión.
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

        var raiz = new GameObject("Nivel_" + nombre);
        var grupos = new Dictionary<Tipo, List<CombineInstance>>();

        var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Mesh cubo = tmp.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(tmp);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Color32 p = px[y * w + x];
                if (p.a < 128) continue;

                Tipo t = Clasificar(p);

                // Piso debajo de todo.
                Sumar(grupos, Tipo.Piso, cubo, new Vector3(x, -0.1f, y), new Vector3(1f, 0.2f, 1f));

                switch (t)
                {
                    case Tipo.Muro:      Sumar(grupos, t, cubo, new Vector3(x, altoMuro / 2f, y), new Vector3(1f, altoMuro, 1f)); break;
                    case Tipo.Piedra:    Sumar(grupos, t, cubo, new Vector3(x, 1.5f, y), new Vector3(1f, 3f, 1f)); break;
                    case Tipo.Obstaculo: Sumar(grupos, t, cubo, new Vector3(x, 0.6f, y), new Vector3(1f, 1.2f, 1f)); break;
                    case Tipo.Hierba:    CrearHierba(raiz.transform, nombre, x, y); break;
                    case Tipo.Enemigo:   Marcador(raiz.transform, nombre, "Aparicion_Enemigo", x, y, new Color(0.9f, 0f, 0f), false); break;
                    case Tipo.Inicio:    Marcador(raiz.transform, nombre, "Inicio_Jugador", x, y, new Color(0f, 0.4f, 1f), false); break;
                    case Tipo.Meta:      Marcador(raiz.transform, nombre, "Meta", x, y, new Color(1f, 0.86f, 0f), true); break;
                }
            }
        }

        // Une todos los cubos de cada tipo en una sola malla (más rápido).
        foreach (var par in grupos)
        {
            var malla = new Mesh();
            malla.name = nombre + "_" + par.Key;
            malla.indexFormat = IndexFormat.UInt32;
            malla.CombineMeshes(par.Value.ToArray(), true, true);

            string rutaMalla = "Assets/Niveles/" + nombre + "_" + par.Key + ".asset";
            AssetDatabase.DeleteAsset(rutaMalla);
            AssetDatabase.CreateAsset(malla, rutaMalla);

            var go = new GameObject(par.Key.ToString());
            go.transform.SetParent(raiz.transform);
            go.AddComponent<MeshFilter>().sharedMesh = malla;
            go.AddComponent<MeshRenderer>().sharedMaterial = Mat(nombre + "_" + par.Key, ColorDe(par.Key, selva));
            go.AddComponent<MeshCollider>().sharedMesh = malla;
            go.isStatic = true;
        }

        // Centra el nivel en el origen.
        raiz.transform.position = new Vector3(-w / 2f, 0f, -h / 2f);

        AssetDatabase.SaveAssets();
        Selection.activeGameObject = raiz;
        Debug.Log("Nivel " + nombre + " creado (" + w + " x " + h + "). Mallas y materiales en Assets/Niveles.");
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

    static void Sumar(Dictionary<Tipo, List<CombineInstance>> g, Tipo t, Mesh m, Vector3 pos, Vector3 esc)
    {
        if (!g.ContainsKey(t)) g[t] = new List<CombineInstance>();
        var ci = new CombineInstance();
        ci.mesh = m;
        ci.transform = Matrix4x4.TRS(pos, Quaternion.identity, esc);
        g[t].Add(ci);
    }

    // La hierba alta es un trigger: el jugador la atraviesa y se esconde dentro.
    static void CrearHierba(Transform padre, string nombre, int x, int y)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = "HierbaAlta";
        g.transform.SetParent(padre);
        g.transform.localPosition = new Vector3(x, 0.75f, y);
        g.transform.localScale = new Vector3(1f, 1.5f, 1f);
        g.GetComponent<BoxCollider>().isTrigger = true;
        g.GetComponent<Renderer>().sharedMaterial = Mat(nombre + "_Hierba", new Color(0.2f, 0.65f, 0.2f));
    }

    static void Marcador(Transform padre, string nombre, string nombreObjeto, int x, int y, Color color, bool trigger)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        g.name = nombreObjeto;
        g.transform.SetParent(padre);
        g.transform.localPosition = new Vector3(x, 0.5f, y);
        g.transform.localScale = Vector3.one * 0.8f;
        Object.DestroyImmediate(g.GetComponent<Collider>());
        if (trigger)
        {
            var b = g.AddComponent<BoxCollider>();
            b.isTrigger = true;
            b.size = Vector3.one * 1.5f;
        }
        g.GetComponent<Renderer>().sharedMaterial = Mat(nombre + "_" + nombreObjeto, color);
    }

    static Color ColorDe(Tipo t, bool selva)
    {
        switch (t)
        {
            case Tipo.Piso:      return selva ? new Color(0.30f, 0.20f, 0.12f) : new Color(0.28f, 0.16f, 0.35f);
            case Tipo.Muro:      return selva ? new Color(0.05f, 0.25f, 0.10f) : new Color(0.35f, 0.25f, 0.50f);
            case Tipo.Obstaculo: return new Color(0.55f, 0.33f, 0.15f);
            case Tipo.Piedra:    return new Color(0.50f, 0.50f, 0.45f);
            default:             return Color.white;
        }
    }

    static Material Mat(string nombre, Color color)
    {
        string ruta = "Assets/Niveles/Mat_" + nombre + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            m = new Material(sh);
            AssetDatabase.CreateAsset(m, ruta);
        }
        m.color = color;
        EditorUtility.SetDirty(m);
        return m;
    }
}

