using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ConstructorSelva
{
    enum Tipo { Piso, Limite, Pasto, Arbol, Tronco, Ruina, Columna, Trampa, Nahual, Rama, Trompos, Inicio, Salida }

    // ---- Referencias a Prefabs (Arrástralos desde tu carpeta de Prefabs al Inspector o asigna materiales) ----
    public static GameObject prefabArbolBorde;     // Árbol grande con colisión para los bordes
    public static GameObject prefabArbolPequeno;   // Árbol más pequeño para obstáculos
    public static GameObject prefabHierba;         // Objeto o plano para la hierba alta
    public static GameObject prefabTronco;         // Troncos caídos

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

    [MenuItem("Saatan/Construir selva (Laberinto)")]
    static void Construir()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Saatan", "Sal del modo Play antes de construir el nivel.", "OK");
            return;
        }

        var tex = Selection.activeObject as Texture2D;
        if (tex == null)
        {
            EditorUtility.DisplayDialog("Saatan", "Primero selecciona tu imagen selva_detallada en la ventana Project.", "OK");
            return;
        }

        ConstruirDesdeTextura(tex);
    }

    public static void ConstruirDesdeTextura(Texture2D tex)
    {
        string ruta = AssetDatabase.GetAssetPath(tex);
        var imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
        imp.isReadable = true;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.SaveAndReimport();

        tex = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        Color32[] px = tex.GetPixels32();
        int w = tex.width;
        int h = tex.height;

        var viejo = GameObject.Find("Selva_Laberinto");
        if (viejo != null) Object.DestroyImmediate(viejo);

        var raiz = new GameObject("Selva_Laberinto").transform;

        var mapa = new int[w, h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Color32 p = px[y * w + x];
                mapa[x, y] = p.a < 128 ? -1 : (int)Clasificar(p);
            }
        }

        // ---- 1. Generación de Suelo y Elementos del Laberinto ----
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (mapa[x, y] < 0) continue;

                Tipo t = (Tipo)mapa[x, y];
                Vector3 pos = new Vector3(x - w / 2f + 0.5f, 0f, y - h / 2f + 0.5f);

                // Todo bloque válido en el mapa genera su placa de suelo base
                CrearSueloBase(raiz, pos);

                switch (t)
                {
                    case Tipo.Limite:
                        InstanciarPrefab(prefabArbolBorde, raiz, "ArbolBorde", pos);
                        break;

                    case Tipo.Arbol:
                        InstanciarPrefab(prefabArbolPequeno, raiz, "ObstaculoArbol", pos);
                        break;

                    case Tipo.Pasto:
                        InstanciarPrefab(prefabHierba, raiz, "HierbaAlta", pos);
                        break;

                    case Tipo.Tronco:
                        InstanciarPrefab(prefabTronco, raiz, "TroncoCaido", pos);
                        break;
                }
            }
        }

        EditorSceneManager.MarkAllScenesDirty();
        Selection.activeGameObject = raiz.gameObject;
        Debug.Log("Laberinto de la selva construido exitosamente con suelo y Prefabs.");
    }

    static void CrearSueloBase(Transform padre, Vector3 pos)
    {
        var suelo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        suelo.name = "Piso_Celda";
        suelo.transform.SetParent(padre, false);
        suelo.transform.localPosition = new Vector3(pos.x, -0.1f, pos.z);
        suelo.transform.localScale = new Vector3(0.98f, 0.2f, 0.98f);
        
        // Mantiene la colisión del suelo para que el jugador no caiga al vacío
        // (Si prefieres usar un material específico de tu carpeta Assets/Niveles, puedes asignarlo aquí)
    }

    static void InstanciarPrefab(GameObject prefab, Transform padre, string nombre, Vector3 pos)
    {
        if (prefab != null)
        {
            var obj = GameObject.Instantiate(prefab, padre);
            obj.name = nombre;
            obj.transform.localPosition = pos;
            obj.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }
        else
        {
            // Fallback visual si el prefab no está asignado: crea un cubo indicador
            var cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubo.name = nombre + "_Faltante";
            cubo.transform.SetParent(padre, false);
            cubo.transform.localPosition = pos + Vector3.up * 0.5f;
            cubo.transform.localScale = Vector3.one * 0.5f;
        }
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
}