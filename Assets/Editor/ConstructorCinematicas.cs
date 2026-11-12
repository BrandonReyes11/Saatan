// Saatan Historia - Cinemáticas MolletesCinematica y AltarCInematica.
// Menú: Saatan > Escenas > Crear MolletesCinematica  |  Saatan > Escenas > Crear AltarCinematica
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static PiezasCinematica;

public static class ConstructorCinematicas
{
    // ---------------- Molletes: el Nahual derrotado se vuelve luz y aparecen unos molletes ----------------

    [MenuItem("Saatan/Escenas/Crear MolletesCinematica")]
    static void Molletes()
    {
        var raiz = NuevaEscena("Cinematica_Molletes");
        if (raiz == null) return;
        Color niebla = new Color(0.55f, 0.66f, 0.58f);

        // Claro de la selva: suelo de césped, ruinas y árboles alrededor.
        Cubo(raiz, new Vector3(0f, -0.25f, 0f), new Vector3(120f, 0.5f, 120f), Mat("Cesped", new Color(0.30f, 0.52f, 0.18f)));
        var veg = AssetDatabase.LoadAssetAtPath<Material>("Assets/Niveles/Selva/Mat_Vegetacion_Voxel.mat");
        string[] arboles = { "acacia_australiana_01", "arvore_folhas_largas_01", "arvore_torta_floresta_01", "samambaia_arborea_01" };
        for (int i = 0; i < 26; i++)
        {
            float a = i * 360f / 26f + Azar(6f), r = 14f + Azar(4f);
            Modelo("Assets/Art/LOD2/" + arboles[i % 4] + "_LOD2.fbx", raiz, new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * r, 0f, Mathf.Sin(a * Mathf.Deg2Rad) * r), rnd.Next(360), 0.7f, veg);
        }
        foreach (var (pieza, pos, giro) in new[] { ("PillarRoundCut", new Vector3(-6f, 0f, 6f), 0f), ("WallBrokenMD", new Vector3(5f, 0f, 8f), 30f),
            ("PillarRound", new Vector3(7f, 0f, 2f), 0f), ("BlockMossXS", new Vector3(0f, -0.38f, 3f), 0f), ("GroundMossMD", new Vector3(-4f, 0f, 2f), 0f) })
        {
            var pf = ImportadorRuinas.Prefab(pieza);
            if (pf == null) continue;
            var t = ((GameObject)PrefabUtility.InstantiatePrefab(pf, raiz)).transform;
            t.localPosition = pos;
            t.localRotation = Quaternion.Euler(0f, giro, 0f);
        }

        // Personajes: el Nahual frente a Iktan y Abbi.
        var nahual = Personaje("NahualVoxel", raiz, new Vector3(0f, 0f, 3f), 180f, 2.4f);
        Personaje("IktanVoxel", raiz, new Vector3(-1.1f, 0f, -2.2f), 10f, 1.4f);
        Personaje("AbbiVoxel", raiz, new Vector3(0.9f, 0f, -1.8f), -15f, 0.55f);

        // Luz, destello, chispas y los molletes (más grandes que en la vida real, para que se vean).
        var luz = new GameObject("Luz_Nahual").AddComponent<Light>();
        luz.transform.SetParent(raiz, false);
        luz.transform.localPosition = new Vector3(0f, 1.6f, 3f);
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.92f, 0.7f);
        luz.range = 16f;
        var destello = Forma(PrimitiveType.Sphere, raiz, new Vector3(0f, 1.2f, 3f), Vector3.one, Aditivo("Destello", new Color(1f, 0.85f, 0.55f)));
        destello.name = "Destello";
        var molletes = Grupo(raiz, "Molletes", new Vector3(0f, 0.05f, 3f));
        PlatoMolletes(molletes, Vector3.zero, 5);
        molletes.localScale = Vector3.one * 3f;

        var chispas = new GameObject("Chispas").AddComponent<ParticleSystem>();
        chispas.transform.SetParent(raiz, false);
        chispas.transform.localPosition = new Vector3(0f, 1.2f, 3f);
        var main = chispas.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 1f;
        main.startLifetime = 2.2f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
        main.gravityModifier = 0.15f;
        main.startColor = new Color(1f, 0.85f, 0.5f);
        var em = chispas.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, 160) });
        var forma = chispas.shape;
        forma.shapeType = ParticleSystemShapeType.Sphere;
        forma.radius = 0.8f;
        chispas.GetComponent<ParticleSystemRenderer>().sharedMaterial = Aditivo("Chispa", new Color(1f, 0.8f, 0.4f));

        var sec = raiz.gameObject.AddComponent<SecuenciaNahual>();
        sec.nahual = nahual;
        sec.destello = destello;
        sec.molletes = molletes;
        sec.luz = luz;
        sec.chispas = chispas;

        Ambiente(niebla, 0.55f, new Color(0.45f, 0.5f, 0.45f), 25f, 70f);
        var cam = Camera.main;
        var cine = cam.gameObject.AddComponent<CamaraCinematica>();
        cine.puntos = new[] { new Vector3(-3.5f, 2.2f, -7.5f), new Vector3(-1.5f, 1.8f, -5.5f), new Vector3(2.5f, 1.6f, -3.5f), new Vector3(3f, 1.4f, -1f) };
        cine.objetivo = new Vector3(0f, 1f, 3f);
        cine.duracion = 13f;
        cam.transform.position = cine.puntos[0];
        cam.transform.LookAt(cine.objetivo);
        GuardarEscena("Assets/Scenes/MolletesCinematica.unity");
    }

    // ---------------- Altar: habitación oscura, la cámara se acerca a la foto de Iktan ----------------

    [MenuItem("Saatan/Escenas/Crear AltarCinematica")]
    static void Altar()
    {
        var raiz = NuevaEscena("Cinematica_Altar");
        if (raiz == null) return;

        // Cuarto oscuro: solo lo iluminan las velas.
        var muro = Mat("MuroOscuro", new Color(0.09f, 0.07f, 0.07f));
        Cubo(raiz, new Vector3(0f, -0.1f, -4f), new Vector3(10f, 0.2f, 20f), Mat("PisoOscuro", new Color(0.12f, 0.09f, 0.07f)));
        Cubo(raiz, new Vector3(0f, 2.5f, 1.4f), new Vector3(10f, 5f, 0.2f), muro);
        foreach (float s in new[] { -1f, 1f }) Cubo(raiz, new Vector3(s * 5f, 2.5f, -4f), new Vector3(0.2f, 5f, 20f), muro);

        // Altar de tres pisos con mantel morado y orilla de papel picado.
        var altar = Grupo(raiz, "Altar_Muertos", Vector3.zero);
        float[] anchos = { 3.2f, 2.4f, 1.6f }, fondos = { 1.2f, 0.9f, 0.6f }, z = { -0.1f, 0.05f, 0.2f };
        string[] papel = { "PapelRosa", "PapelNaranja", "PapelMorado", "PapelVerde" };
        Color[] colPapel = { new Color(0.9f, 0.2f, 0.5f), new Color(1f, 0.55f, 0.1f), new Color(0.5f, 0.2f, 0.75f), new Color(0.2f, 0.7f, 0.35f) };
        for (int p = 0; p < 3; p++)
        {
            float y = p * 0.6f;
            Cubo(altar, new Vector3(0f, y + 0.3f, z[p]), new Vector3(anchos[p], 0.6f, fondos[p]), Mat("Mantel", new Color(0.35f, 0.08f, 0.35f)));
            Cubo(altar, new Vector3(0f, y + 0.605f, z[p]), new Vector3(anchos[p] + 0.04f, 0.02f, fondos[p] + 0.04f), Mat("MantelBlanco", new Color(0.9f, 0.88f, 0.82f)));
            for (float x = -anchos[p] / 2f + 0.1f; x < anchos[p] / 2f; x += 0.2f)
            {
                int k = Mathf.RoundToInt((x + 2f) * 5f) % 4;
                Cubo(altar, new Vector3(x, y + 0.52f, z[p] - fondos[p] / 2f - 0.025f), new Vector3(0.17f, 0.15f, 0.01f), Mat(papel[k], colPapel[k]));
            }
        }
        float Tope(int p) => (p + 1) * 0.6f + 0.02f;
        float Frente(int p) => z[p] - fondos[p] / 2f + 0.15f;

        // Piso 3 (cima): la foto de Iktan, velas, un trompo y mandarinas.
        Foto(altar, new Vector3(0f, Tope(2), 0.3f));
        foreach (float s in new[] { -1f, 1f })
        {
            Vela(altar, new Vector3(s * 0.55f, Tope(2), Frente(2)), 0.3f);
            Mandarina(altar, new Vector3(s * 0.32f, Tope(2), Frente(2) - 0.05f));
        }
        Trompo(altar, new Vector3(0.65f, Tope(2), 0.3f), new Color(0.85f, 0.15f, 0.15f));

        // Piso 2: molletes, mandarinas, velas, carrito y balero.
        PlatoMolletes(altar, new Vector3(-0.6f, Tope(1), Frente(1) + 0.15f));
        for (int i = 0; i < 5; i++) Mandarina(altar, new Vector3(0.35f + (i % 3) * 0.08f, Tope(1) + (i / 3) * 0.06f, Frente(1) + 0.1f + (i % 2) * 0.06f));
        foreach (float x in new[] { -1.05f, 0f, 1.05f }) Vela(altar, new Vector3(x, Tope(1), Frente(1)), 0.22f);
        Carrito(altar, new Vector3(0.85f, Tope(1), Frente(1) + 0.2f), 20f);
        Balero(altar, new Vector3(-0.15f, Tope(1), Frente(1) + 0.3f));

        // Piso 1: hilera de velas, molletes, trompos, pelota, yoyo, mandarinas y flores.
        for (int i = 0; i < 6; i++) Vela(altar, new Vector3(-1.4f + i * 0.56f, Tope(0), Frente(0) - 0.05f), 0.18f, i % 2 == 0);
        PlatoMolletes(altar, new Vector3(0.75f, Tope(0), Frente(0) + 0.3f), 3);
        Trompo(altar, new Vector3(-0.9f, Tope(0), Frente(0) + 0.3f), new Color(0.15f, 0.35f, 0.85f));
        Trompo(altar, new Vector3(-0.6f, Tope(0), Frente(0) + 0.35f), new Color(0.95f, 0.8f, 0.15f), true);
        Trompo(altar, new Vector3(1.3f, Tope(0), Frente(0) + 0.35f), new Color(0.15f, 0.65f, 0.25f));
        Pelota(altar, new Vector3(-1.25f, Tope(0), Frente(0) + 0.4f));
        Yoyo(altar, new Vector3(0.25f, Tope(0), Frente(0) + 0.3f));
        for (int i = 0; i < 6; i++) Mandarina(altar, new Vector3(-0.2f + Azar(0.1f), Tope(0), Frente(0) + 0.35f + Azar(0.08f)));
        for (float x = -1.5f; x <= 1.5f; x += 0.25f) Flor(altar, new Vector3(x, Tope(0), z[0] + fondos[0] / 2f - 0.08f));

        // En el piso: camino de pétalos de cempasúchil hacia el altar y velas a los lados.
        var petalo = Mat("Cempasuchil", new Color(1f, 0.55f, 0.05f));
        for (int i = 0; i < 220; i++)
        {
            float zz = Mathf.Lerp(-0.9f, -9f, (float)rnd.NextDouble());
            Cubo(altar, new Vector3(Azar(0.45f), 0.006f, zz), new Vector3(0.05f, 0.01f, 0.05f), petalo).localRotation = Quaternion.Euler(0f, rnd.Next(90), 0f);
        }
        for (int i = 0; i < 4; i++)
            foreach (float s in new[] { -1f, 1f }) Vela(altar, new Vector3(s * 0.9f, 0f, -1.2f - i * 1.8f), 0.3f, i < 2);

        // Totalmente oscuro: sin luz direccional, sin ambiente, fondo negro.
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) Object.DestroyImmediate(l.gameObject);
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.01f, 0.01f, 0.012f);
        RenderSettings.fog = false;

        var cam = Camera.main;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        var cine = cam.gameObject.AddComponent<CamaraCinematica>();
        cine.puntos = new[] { new Vector3(0f, 1.6f, -12f), new Vector3(0f, 1.8f, -7f), new Vector3(0f, 2.1f, -3.5f), new Vector3(0f, 2.45f, -1.15f) };
        cine.objetivo = new Vector3(0f, Tope(2) + 0.33f, 0.3f);
        cine.duracion = 26f;
        cam.transform.position = cine.puntos[0];
        cam.transform.LookAt(cine.objetivo);
        GuardarEscena("Assets/Scenes/AltarCInematica.unity");
    }

    // Foto de Iktan en un marco: una cámara aparte retrata al modelo de Iktan (escondido bajo el cuarto)
    // en una RenderTexture que se muestra en el cuadro, con tono sepia.
    static void Foto(Transform padre, Vector3 pos)
    {
        var g = Grupo(padre, "Foto_Iktan", pos);
        var marco = Mat("Marco", new Color(0.55f, 0.35f, 0.12f));
        Cubo(g, new Vector3(0f, 0.33f, 0.02f), new Vector3(0.5f, 0.62f, 0.04f), marco);
        Cubo(g, new Vector3(0f, 0.06f, 0.08f), new Vector3(0.06f, 0.12f, 0.12f), marco).localRotation = Quaternion.Euler(-20f, 0f, 0f);

        var rt = new RenderTexture(256, 320, 16) { name = "RT_FotoIktan" };
        Guardar(rt, CARPETA + "/Piezas/RT_FotoIktan.renderTexture");
        var foto = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        foto.SetTexture("_BaseMap", rt);
        foto.EnableKeyword("_EMISSION");
        foto.SetTexture("_EmissionMap", rt);
        foto.SetColor("_EmissionColor", new Color(0.35f, 0.33f, 0.3f));   // se distingue aun con poca luz
        Guardar(foto, CARPETA + "/Piezas/Mat_FotoIktan.mat");
        Cubo(g, new Vector3(0f, 0.33f, -0.001f), new Vector3(0.42f, 0.52f, 0.01f), foto);

        // Estudio de retrato a 100 m bajo el piso: Iktan, una luz suave y la cámara que llena la RenderTexture.
        var estudio = Grupo(padre.root, "Estudio_Foto", new Vector3(0f, -100f, 0f));
        Personaje("IktanVoxel", estudio, Vector3.zero, 180f, 1.4f);
        var l = new GameObject("Luz_Foto").AddComponent<Light>();
        l.transform.SetParent(estudio, false);
        l.transform.localPosition = new Vector3(0.6f, 1.8f, -1.5f);
        l.type = LightType.Point;
        l.range = 6f;
        l.intensity = 3f;
        l.color = new Color(1f, 0.9f, 0.75f);
        var c = new GameObject("Camara_Foto").AddComponent<Camera>();
        c.transform.SetParent(estudio, false);
        c.transform.localPosition = new Vector3(0f, 1.05f, -1.6f);
        c.transform.LookAt(estudio.position + Vector3.up * 1.0f);
        c.fieldOfView = 32f;
        c.nearClipPlane = 0.1f;
        c.farClipPlane = 6f;
        c.clearFlags = CameraClearFlags.SolidColor;
        c.backgroundColor = new Color(0.55f, 0.45f, 0.32f);   // fondo sepia de foto antigua
        c.targetTexture = rt;
    }

    // ---------------- Utilidades ----------------

    static void Modelo(string ruta, Transform padre, Vector3 pos, float giro, float escala, Material mat)
    {
        var pf = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
        if (pf == null) return;
        var t = ((GameObject)PrefabUtility.InstantiatePrefab(pf, padre)).transform;
        t.localPosition = pos;
        t.localRotation = Quaternion.Euler(0f, giro, 0f) * pf.transform.localRotation;
        t.localScale = pf.transform.localScale * escala;
        if (mat != null) foreach (var r in t.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterial = mat;
    }

    static void Ambiente(Color niebla, float sol, Color ambiente, float inicio, float fin)
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = ambiente;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = niebla;
        RenderSettings.fogStartDistance = inicio;
        RenderSettings.fogEndDistance = fin;
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) { l.intensity = sol; l.shadows = LightShadows.Soft; l.transform.rotation = Quaternion.Euler(40f, 30f, 0f); }
        var cam = Camera.main;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = niebla;
    }
}
