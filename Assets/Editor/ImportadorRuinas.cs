// Saatan Historia - Convierte los .glb de Assets/Art/RuinsGLB en prefabs de Unity (sin paquetes extra).
// Unity no importa .glb por sí solo: aquí se leen la malla, la textura (pixel art) y la posición de cada pieza,
// y se guardan como mallas, materiales URP y prefabs en Assets/Art/Ruinas. Los esqueletos (Skely) se omiten.
// Menú: Saatan > Importar ruinas (GLB). ConstructorSelva lo llama solo si faltan los prefabs.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class ImportadorRuinas
{
    const string ORIGEN = "Assets/Art/RuinsGLB", DESTINO = "Assets/Art/Ruinas";

    // Solo los campos de glTF que usan estos modelos.
    [Serializable] class Gltf { public Nodo[] nodes; public Malla[] meshes; public Acc[] accessors; public Vista[] bufferViews; public Mat[] materials; public Tex[] textures; public Img[] images; }
    [Serializable] class Nodo { public string name; public int mesh = -1; public float[] translation; }
    [Serializable] class Malla { public Prim[] primitives; }
    [Serializable] class Prim { public Attr attributes; public int indices = -1; public int material = -1; }
    [Serializable] class Attr { public int POSITION = -1, NORMAL = -1, TEXCOORD_0 = -1; }
    [Serializable] class Acc { public int bufferView, byteOffset, componentType, count; }
    [Serializable] class Vista { public int byteOffset, byteLength; }
    [Serializable] class Mat { public Pbr pbrMetallicRoughness; }
    [Serializable] class Pbr { public Ref baseColorTexture; }
    [Serializable] class Ref { public int index; }
    [Serializable] class Tex { public int source; }
    [Serializable] class Img { public int bufferView; }

    [MenuItem("Saatan/Importar ruinas (GLB)")]
    public static void ImportarTodo()
    {
        foreach (var sub in new[] { "", "/Mallas", "/Texturas", "/Materiales" })
            if (!AssetDatabase.IsValidFolder(DESTINO + sub))
                AssetDatabase.CreateFolder(sub == "" ? "Assets/Art" : DESTINO, sub == "" ? "Ruinas" : sub.Substring(1));

        var archivos = Directory.GetFiles(ORIGEN, "*.glb", SearchOption.AllDirectories);
        try
        {
            for (int i = 0; i < archivos.Length; i++)
            {
                string ruta = archivos[i].Replace('\\', '/');
                if (ruta.Contains("/Skely/")) continue;
                EditorUtility.DisplayProgressBar("Saatan - Ruinas", Path.GetFileName(ruta), (float)i / archivos.Length);
                Importar(ruta);
            }
        }
        finally { EditorUtility.ClearProgressBar(); }
        AssetDatabase.SaveAssets();
        Debug.Log("Ruinas importadas en " + DESTINO);
    }

    // Prefab de una pieza por su nombre (p. ej. "WallBrokenXl"); importa todo si aún no existe.
    static bool importado;
    public static GameObject Prefab(string nombre)
    {
        string ruta = DESTINO + "/" + nombre + ".prefab";
        if (!File.Exists(ruta) && !importado) { importado = true; ImportarTodo(); }
        return AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
    }

    static void Importar(string ruta)
    {
        string nombre = Path.GetFileNameWithoutExtension(ruta);
        byte[] b = File.ReadAllBytes(ruta);
        int largoJson = BitConverter.ToInt32(b, 12);
        var g = JsonUtility.FromJson<Gltf>(Encoding.UTF8.GetString(b, 20, largoJson));
        int bin = 20 + largoJson + 8;   // inicio del bloque binario

        // Textura (pixel art: sin filtrado) y material URP de doble cara.
        Material mat = null;
        if (g.images != null && g.images.Length > 0)
        {
            var vista = g.bufferViews[g.images[0].bufferView];
            string rutaTex = DESTINO + "/Texturas/" + nombre + ".png";
            File.WriteAllBytes(rutaTex, Bytes(b, bin + vista.byteOffset, vista.byteLength));
            AssetDatabase.ImportAsset(rutaTex);
            var imp = (TextureImporter)AssetImporter.GetAtPath(rutaTex);
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();

            mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = nombre };
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(rutaTex));
            mat.SetFloat("_Smoothness", 0f);
            mat.SetFloat("_Cull", 0f);
            mat.enableInstancing = true;
            string rutaMat = DESTINO + "/Materiales/" + nombre + ".mat";
            AssetDatabase.DeleteAsset(rutaMat);
            AssetDatabase.CreateAsset(mat, rutaMat);
        }

        var raiz = new GameObject(nombre);
        string rutaMallas = DESTINO + "/Mallas/" + nombre + ".asset";
        AssetDatabase.DeleteAsset(rutaMallas);
        bool primera = true;
        Bounds limites = new Bounds();

        foreach (var nodo in g.nodes)
        {
            if (nodo.mesh < 0 || nodo.mesh >= g.meshes.Length) continue;
            var m = CrearMalla(g, b, bin, g.meshes[nodo.mesh].primitives[0], nodo.name);
            if (primera) AssetDatabase.CreateAsset(m, rutaMallas); else AssetDatabase.AddObjectToAsset(m, rutaMallas);

            var hijo = new GameObject(nodo.name);
            hijo.transform.SetParent(raiz.transform, false);
            var t = nodo.translation;
            if (t != null && t.Length == 3) hijo.transform.localPosition = new Vector3(-t[0], t[1], t[2]);
            hijo.AddComponent<MeshFilter>().sharedMesh = m;
            hijo.AddComponent<MeshRenderer>().sharedMaterial = mat;

            var bm = new Bounds(m.bounds.center + hijo.transform.localPosition, m.bounds.size);
            if (primera) limites = bm; else limites.Encapsulate(bm);
            primera = false;
        }

        // Colisión: el musgo no tiene, las escaleras usan su malla, el resto una caja.
        if (ruta.Contains("/Stairs/"))
            foreach (var mf in raiz.GetComponentsInChildren<MeshFilter>()) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
        else if (!ruta.Contains("/Moss/"))
        {
            var c = raiz.AddComponent<BoxCollider>();
            c.center = limites.center;
            c.size = limites.size;
        }

        PrefabUtility.SaveAsPrefabAsset(raiz, DESTINO + "/" + nombre + ".prefab");
        UnityEngine.Object.DestroyImmediate(raiz);
    }

    // glTF usa mano derecha: se invierte X y el orden de los triángulos; la V de la textura va al revés.
    static Mesh CrearMalla(Gltf g, byte[] b, int bin, Prim p, string nombre)
    {
        var m = new Mesh { name = nombre };
        var pos = Floats(g, b, bin, p.attributes.POSITION, 3);
        var v = new Vector3[pos.Length / 3];
        for (int i = 0; i < v.Length; i++) v[i] = new Vector3(-pos[i * 3], pos[i * 3 + 1], pos[i * 3 + 2]);
        m.vertices = v;

        if (p.attributes.TEXCOORD_0 >= 0)
        {
            var uv = Floats(g, b, bin, p.attributes.TEXCOORD_0, 2);
            var u = new Vector2[uv.Length / 2];
            for (int i = 0; i < u.Length; i++) u[i] = new Vector2(uv[i * 2], 1f - uv[i * 2 + 1]);
            m.uv = u;
        }

        var a = g.accessors[p.indices];
        int o = bin + g.bufferViews[a.bufferView].byteOffset + a.byteOffset;
        var tri = new int[a.count];
        for (int i = 0; i < a.count; i++)
            tri[i] = a.componentType == 5125 ? (int)BitConverter.ToUInt32(b, o + i * 4)
                   : a.componentType == 5123 ? BitConverter.ToUInt16(b, o + i * 2) : b[o + i];
        for (int i = 0; i + 2 < tri.Length; i += 3) (tri[i + 1], tri[i + 2]) = (tri[i + 2], tri[i + 1]);
        if (v.Length > 65535) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m.triangles = tri;

        if (p.attributes.NORMAL >= 0)
        {
            var nr = Floats(g, b, bin, p.attributes.NORMAL, 3);
            var n = new Vector3[nr.Length / 3];
            for (int i = 0; i < n.Length; i++) n[i] = new Vector3(-nr[i * 3], nr[i * 3 + 1], nr[i * 3 + 2]);
            m.normals = n;
        }
        else m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    static float[] Floats(Gltf g, byte[] b, int bin, int acc, int comps)
    {
        var a = g.accessors[acc];
        int o = bin + g.bufferViews[a.bufferView].byteOffset + a.byteOffset;
        var r = new float[a.count * comps];
        for (int i = 0; i < r.Length; i++) r[i] = BitConverter.ToSingle(b, o + i * 4);
        return r;
    }

    static byte[] Bytes(byte[] b, int desde, int largo)
    {
        var r = new byte[largo];
        Array.Copy(b, desde, r, 0, largo);
        return r;
    }
}
