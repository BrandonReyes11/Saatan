// Saatan Historia - Cinemáticas MolletesCinematica y AltarCInematica, hechas con Timeline
// (objeto "Cinematica" con PlayableDirector; se pueden abrir en Window > Sequencing > Timeline).
// Al terminar: Molletes -> CinematicaIglesia, Altar -> MenuPrincipal.
// Menú: Saatan > Escenas > Crear MolletesCinematica  |  Saatan > Escenas > Crear AltarCinematica
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Timeline;
using static PiezasCinematica;

public static class ConstructorCinematicas
{
    // ---------------- Molletes: el Nahual derrotado se vuelve luz y aparecen unos molletes ----------------

    [MenuItem("Saatan/Escenas/Crear MolletesCinematica")]
    static void Molletes()
    {
        var raiz = NuevaEscena("Cinematica_Molletes");
        if (raiz == null) return;
        Color niebla = new Color(0.55f, 0.66f, 0.58f);   // mismo color de neblina que el nivel de la selva

        // Escenario: las mismas ruinas del nivel de la selva (mismo constructor y misma semilla: muros, pilares,
        // losas, altar, musgo y la vegetación de alrededor), con el centro de la plaza en el origen.
        if (ConstructorSelva.RuinasParaCinematica(raiz) == null)
            Debug.LogWarning("No se pudieron construir las ruinas de la selva para la cinemática.");
        var cesped = AssetDatabase.LoadAssetAtPath<Material>("Assets/Project/Niveles/Selva/Mat_Cesped.mat");
        Cubo(raiz, new Vector3(0f, -0.4f, 0f), new Vector3(160f, 0.5f, 160f), cesped != null ? cesped : Mat("Cesped", new Color(0.30f, 0.52f, 0.18f)));

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

        Ambiente(niebla, 0.55f, new Color(0.45f, 0.5f, 0.45f), 4f, 24f);   // neblina espesa: el fondo se pierde
        var cam = Camera.main;
        cam.farClipPlane = 30f;

        // ---- Timeline (13 s): 2.5 s de calma, 4 s llenándose de luz, destello a los 6.5 s y aparecen los molletes ----
        const float dur = 13f, brilla = 2.5f, estalla = 6.5f;
        var d = Director(raiz, "Molletes", "CinematicaIglesia", out var tl);
        System.Func<float, float> Luz = t => { float a = Mathf.Clamp01((t - brilla) / (estalla - brilla)); return a * a; };   // 0..1 antes del destello

        // Nahual: tiembla cada vez más y desaparece en el destello.
        Vector3 baseNahual = nahual.localPosition;
        Quaternion rotNahual = nahual.localRotation;
        PistaMovimiento(tl, d, nahual.gameObject, estalla, t =>
            (baseNahual + new Vector3(Mathf.PerlinNoise(t * 25f, 1f) - 0.5f, 0f, Mathf.PerlinNoise(t * 25f, 7f) - 0.5f) * 0.14f * Luz(t), rotNahual), 0.05f);
        PistaActivacion(tl, d, nahual.gameObject, 0f, estalla);

        // Destello: crece mientras brilla, explota (0.35 s) y se apaga en 2 s. La luz lo acompaña.
        PistaAnimacion(tl, d, destello.gameObject, Muestreo(dur, 0.05f, typeof(Transform), new[] { "m_LocalScale.x", "m_LocalScale.y", "m_LocalScale.z" }, t =>
        {
            float e = Mathf.Clamp01((t - estalla) / 0.35f), f = Mathf.Clamp01((t - estalla - 0.35f) / 2f);
            float esc = t < estalla ? 3.2f * Luz(t) : Mathf.Lerp(3.2f, 7f, e) * (1f - f);
            return new[] { esc, esc, esc };
        }));
        PistaAnimacion(tl, d, luz.gameObject, Muestreo(dur, 0.05f, typeof(Light), new[] { "m_Intensity" }, t =>
            new[] { t < estalla ? 14f * Luz(t) : Mathf.Lerp(30f, 2f, Mathf.Clamp01((t - estalla - 0.35f) / 2f)) }));
        PistaParticulas(tl, d, chispas.gameObject, estalla, 3f);

        // Molletes: aparecen con un rebote y giran despacio.
        PistaAnimacion(tl, d, molletes.gameObject, Muestreo(dur, 0.05f, typeof(Transform),
            new[] { "m_LocalScale.x", "m_LocalScale.y", "m_LocalScale.z", "localEulerAnglesRaw.y" }, t =>
            {
                float m = Mathf.Clamp01((t - estalla - 0.25f) / 1.2f), esc = 3f * m * (1f + Mathf.Sin(m * Mathf.PI) * 0.25f);
                return new[] { esc, esc, esc, Mathf.Max(0f, t - estalla) * 20f };
            }));

        // Cámara: rodea despacio a los personajes mirando al Nahual.
        CamaraPorPuntos(tl, d, cam, new[] { new Vector3(-3.5f, 2.2f, -7.5f), new Vector3(-1.5f, 1.8f, -5.5f), new Vector3(2.5f, 1.6f, -3.5f), new Vector3(3f, 1.4f, -1f) },
            new Vector3(0f, 1f, 3f), dur);
        GuardarEscena("Assets/Project/Scenes/MolletesCinematica.unity");
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
        Item(PrefabFotoIktan(), altar, new Vector3(0f, Tope(2), 0.3f));
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
        RenderSettings.ambientLight = new Color(0.03f, 0.025f, 0.03f);   // casi negro: solo se distinguen las siluetas
        RenderSettings.fog = false;

        var cam = Camera.main;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        // Timeline (26 s): la cámara se acerca muy despacio desde el fondo del cuarto hasta la foto; luego al menú.
        var d = Director(raiz, "Altar", "MenuPrincipal", out var tl);
        CamaraPorPuntos(tl, d, cam, new[] { new Vector3(0f, 1.6f, -12f), new Vector3(0f, 1.8f, -7f), new Vector3(0f, 2.1f, -3.5f), new Vector3(0f, 2.45f, -1.15f) },
            new Vector3(0f, Tope(2) + 0.33f, 0.3f), 26f);
        GuardarEscena("Assets/Project/Scenes/AltarCInematica.unity");
    }

    // ---------------- Utilidades ----------------

    static void CamaraPorPuntos(TimelineAsset tl, UnityEngine.Playables.PlayableDirector d, Camera cam, Vector3[] puntos, Vector3 objetivo, float dur)
    {
        cam.transform.position = puntos[0];
        cam.transform.LookAt(objetivo);
        PistaMovimiento(tl, d, cam.gameObject, dur, t =>
        {
            Vector3 p = Recorrido(puntos, t / dur);
            return (p, Quaternion.LookRotation(objetivo - p));
        });
    }

    // Clip con varias propiedades muestreadas cada 'paso' segundos: valores(t) devuelve un número por propiedad.
    static AnimationClip Muestreo(float dur, float paso, System.Type tipo, string[] props, System.Func<float, float[]> valores)
    {
        int n = Mathf.CeilToInt(dur / paso) + 1;
        var t = new float[n];
        var v = new float[props.Length][];
        for (int k = 0; k < props.Length; k++) v[k] = new float[n];
        for (int i = 0; i < n; i++)
        {
            t[i] = Mathf.Min(i * paso, dur);
            var x = valores(t[i]);
            for (int k = 0; k < props.Length; k++) v[k][i] = x[k];
        }
        var clip = new AnimationClip();
        for (int k = 0; k < props.Length; k++) Curva(clip, tipo, props[k], t, v[k]);
        return clip;
    }

    // Modelo voxel de la selva escalado a 'altura' metros, con el material de colores por vértice.
    static void Modelo(string ruta, Transform padre, Vector3 pos, float giro, float altura, Material mat)
    {
        var pf = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
        if (pf == null) return;
        var t = ((GameObject)PrefabUtility.InstantiatePrefab(pf, padre)).transform;
        t.localPosition = pos;
        t.localRotation = Quaternion.Euler(0f, giro, 0f) * pf.transform.localRotation;
        var rs = t.GetComponentsInChildren<MeshRenderer>();
        if (rs.Length == 0) return;
        Bounds b = rs[0].bounds;
        foreach (var r in rs) { b.Encapsulate(r.bounds); if (mat != null) r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.Off; }
        t.localScale = pf.transform.localScale * (altura / Mathf.Max(b.size.y, 0.01f));
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
