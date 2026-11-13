// Saatan Historia - Tráiler de 1 minuto (escena Trailer, hecha con Timeline).
// Menú: Saatan > Escenas > Crear Trailer. Para el video: Window > General > Recorder (paquete Unity Recorder).
//
// Escenarios reales del juego en una sola escena, separados y activados por el Timeline (cada uno con su
// SceneMood: neblina, luz y fondo). Guion (segundos):
//   0-7   la selva desde el aire                       "Perdido en la selva..."
//   7-17  Iktan y Abbi por el pasillo de las ruinas     "...algo acecha entre las ruinas"
//  13-17  aparece el Nahual
//  17-29  la pelea: embestidas, esquives, piedras y la mordida de Abbi   "Enfrenta al Nahual"
//  29-33  el Nahual se vuelve luz y aparecen los molletes
//  33-42  la iglesia en la colina; suben la escalera  "Sube a la iglesia abandonada"
//  42-48  dentro de la iglesia                        "Descubre sus secretos"
//  48-53  el altar de muertos y la foto de Iktan      "...y recuerda quién eres"
//  53-60  título SAATANPAAL
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;
using static CutsceneParts;

public static class TrailerBuilder
{
    const string ESCENA = "Assets/Project/Scenes/Trailer.unity";
    const string TITULO = "SAATANPAAL";
    const float DUR = 60f;
    static readonly Vector3 FUERA = new Vector3(-1500f, 0f, 0f), DENTRO = new Vector3(1500f, 0f, 0f), CUARTO = new Vector3(0f, 0f, -1500f);
    static readonly float[] cortes = { 7f, 13f, 17f, 21f, 24f, 33f, 37f, 42f, 48f };

    // Recorrido por puntos con tiempo: posición lineal entre claves.
    class Ruta
    {
        readonly List<(float t, Vector3 p)> claves = new List<(float, Vector3)>();
        public Ruta En(float t, Vector3 p) { claves.Add((t, p)); return this; }
        public Vector3 Pos(float t)
        {
            if (t <= claves[0].t) return claves[0].p;
            for (int i = 0; i < claves.Count - 1; i++)
                if (t <= claves[i + 1].t) return Vector3.Lerp(claves[i].p, claves[i + 1].p, Mathf.InverseLerp(claves[i].t, claves[i + 1].t, t));
            return claves[claves.Count - 1].p;
        }
    }

    [MenuItem("Saatan/Escenas/Crear Trailer")]
    static void Crear()
    {
        var raiz = NuevaEscena("Trailer");
        if (raiz == null) return;
        try { Construir(raiz); }
        finally { EditorUtility.ClearProgressBar(); }
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ESCENA);
        AssetDatabase.SaveAssets();
        Debug.Log("Tráiler creado: " + ESCENA + ". Grábalo con Window > General > Recorder (60 s, 1920x1080, 30 fps).");
    }

    static void Construir(Transform raiz)
    {
        var cam = Camera.main;

        // ---------------- Escenarios ----------------
        EditorUtility.DisplayProgressBar("Saatan - Tráiler", "Selva y ruinas", 0.1f);
        var selva = Escenario(raiz, "Set_Selva", Vector3.zero, m => { m.inicioNiebla = 12f; m.finNiebla = 95f; m.alcanceCamara = 140f; });
        var mapa = AssetDatabase.LoadAssetAtPath<Texture2D>(JungleBuilder.MAPA);
        JungleBuilder.ConstruirDesdeTextura(mapa);
        foreach (var n in new[] { "Selva_Laberinto", "Ruinas" })
            if (GameObject.Find(n) is GameObject go) go.transform.SetParent(selva, true);
        foreach (var n in new[] { "Linea_Ruinas", "Cierre_Entrada", "Pelea" })
            if (PiezasEn(selva, n) is Transform t) UnityEngine.Object.DestroyImmediate(t.gameObject);
        if (cam.GetComponent<JungleStreamer>() is JungleStreamer js) UnityEngine.Object.DestroyImmediate(js);   // sin jugador: todo cargado

        EditorUtility.DisplayProgressBar("Saatan - Tráiler", "Iglesia en la colina", 0.4f);
        var fuera = Escenario(raiz, "Set_Iglesia_Fuera", FUERA, m =>
        {
            m.niebla = new Color(0.45f, 0.48f, 0.56f); m.inicioNiebla = 40f; m.finNiebla = 170f; m.ambiente = new Color(0.42f, 0.44f, 0.52f);
            m.colorSol = new Color(0.85f, 0.8f, 0.95f); m.giroSol = new Vector3(30f, 20f, 0f);
        });
        ChurchCutsceneBuilder.ConstruirDecorado().SetParent(fuera, false);

        EditorUtility.DisplayProgressBar("Saatan - Tráiler", "Iglesia por dentro", 0.6f);
        var dentro = Escenario(raiz, "Set_Iglesia_Dentro", DENTRO, m =>
        {
            m.niebla = new Color(0.05f, 0.04f, 0.06f); m.inicioNiebla = 20f; m.finNiebla = 75f; m.ambiente = new Color(0.2f, 0.17f, 0.24f);
            m.sol = 0.25f; m.colorSol = new Color(0.6f, 0.65f, 1f);
        });
        var iglesia = ChurchBuilder.ConstruirParaTrailer();
        if (iglesia != null) iglesia.SetParent(dentro, false);

        EditorUtility.DisplayProgressBar("Saatan - Tráiler", "Altar", 0.8f);
        var cuarto = Escenario(raiz, "Set_Altar", CUARTO, m =>
        {
            m.conNiebla = false; m.ambiente = new Color(0.03f, 0.025f, 0.03f); m.sol = 0f; m.alcanceCamara = 60f;
        });
        CutsceneBuilder.AltarDecorado(cuarto);

        // ---------------- Timeline ----------------
        var d = Director(raiz, "Trailer", "", out var tl);
        d.GetComponent<CutsceneEnd>().sePuedeSaltar = false;   // que no se corte mientras se graba
        PistaActivacion(tl, d, selva.gameObject, 0f, 33f);
        PistaActivacion(tl, d, fuera.gameObject, 33f, 42f);
        PistaActivacion(tl, d, dentro.gameObject, 42f, 48f);
        PistaActivacion(tl, d, cuarto.gameObject, 48f, DUR);

        Vector3 C = JungleBuilder.CentroRuinas, E = JungleBuilder.InicioPasillo;
        Pelea(tl, d, selva, C, E, out Ruta iktanSelva, out Ruta nahual);
        Ruta iktanIglesia = Subida(tl, d, fuera);

        // ---------------- Cámara ----------------
        Func<float, (Vector3, Vector3)> plano = t =>
        {
            Vector3 ik = iktanSelva.Pos(t), nh = nahual.Pos(t);
            if (t < 7f)  // selva desde el aire
            {
                float u = Mathf.SmoothStep(0f, 1f, t / 7f);
                Vector3 p = Vector3.Lerp(new Vector3(-90f, 40f, -55f), new Vector3(15f, 24f, -10f), u);
                return (p, p + new Vector3(30f, -22f, 22f));
            }
            if (t < 13f) return (ik + new Vector3(-5f, 2.2f, -1.6f), ik + new Vector3(5f, 0.8f, 0f));               // detrás de Iktan
            if (t < 17f)                                                                                              // la plaza y el Nahual
            {
                float u = Mathf.SmoothStep(0f, 1f, (t - 13f) / 4f);
                return (C + Vector3.Lerp(new Vector3(-16f, 2f, 7f), new Vector3(-12f, 9f, 11f), u), C + new Vector3(4f, 1f, 0f));
            }
            if (t < 21f) return (C + new Vector3(-3f + (t - 17f) * 0.3f, 2.6f, -13f), C + new Vector3(-5.5f, 1f, -1f)); // de lado
            if (t < 24f) return (C + new Vector3(-4f, 1.8f, -9.5f), C + new Vector3(-9f, 1f, -0.5f));                  // tras el hombro
            if (t < 29f)                                                                                              // alrededor del Nahual
            {
                float a = Mathf.Lerp(200f, 300f, (t - 24f) / 5f) * Mathf.Deg2Rad;
                Vector3 centro = C + new Vector3(-6.5f, 1f, -2.5f);
                return (centro + new Vector3(Mathf.Cos(a) * 7.5f, Mathf.Lerp(1.6f, 2.2f, (t - 24f) / 5f), Mathf.Sin(a) * 7.5f), centro);
            }
            if (t < 33f)                                                                                              // destello y molletes
            {
                Vector3 n = nahual.Pos(29f);
                return (n + Vector3.Lerp(new Vector3(0.5f, 2.4f, -9f), new Vector3(0.3f, 1.6f, -5f), (t - 29f) / 4f), n + Vector3.up);
            }
            if (t < 37f)                                                                                              // la iglesia en la colina
            {
                float u = (t - 33f) / 4f;
                return (FUERA + Vector3.Lerp(new Vector3(-35f, 5f, -130f), new Vector3(-22f, 9f, -112f), u), FUERA + new Vector3(0f, 24f, -10f));
            }
            if (t < 42f)                                                                                              // subiendo la escalera
            {
                Vector3 p = iktanIglesia.Pos(t);
                return (p + new Vector3(-3.5f, 3.2f, -7.5f), Vector3.Lerp(p + Vector3.up, FUERA + new Vector3(0f, ChurchCutsceneBuilder.ALTO + 10f, -16f), 0.5f));
            }
            if (t < 48f)                                                                                              // la nave de la iglesia
            {
                float u = Mathf.SmoothStep(0f, 1f, (t - 42f) / 6f);
                return (DENTRO + Vector3.Lerp(new Vector3(0f, 2.5f, -23f), new Vector3(0f, 6f, 3f), u), DENTRO + new Vector3(0f, 8f, 28f));
            }
            {                                                                                                         // el altar (y se queda ahí bajo el título)
                float u = Mathf.SmoothStep(0f, 1f, (t - 48f) / 5f);
                return (CUARTO + Vector3.Lerp(new Vector3(0f, 1.7f, -7f), new Vector3(0f, 2.4f, -1.4f), u), CUARTO + CutsceneBuilder.FotoAltar);
            }
        };
        var (p0, m0) = plano(0f);
        cam.transform.SetPositionAndRotation(p0, Quaternion.LookRotation(m0 - p0));
        PistaMovimiento(tl, d, cam.gameObject, DUR, t => { var (p, m) = plano(t); return (p, Quaternion.LookRotation(m - p)); }, 0.05f);

        Textos(tl, d, raiz);

        // Solo la selva empieza activa: así su SceneMood es el que vale al empezar; el Timeline activa los demás.
        foreach (var set in new[] { fuera, dentro, cuarto }) set.gameObject.SetActive(false);
    }

    // ---------------- La pelea en las ruinas ----------------

    static void Pelea(TimelineAsset tl, PlayableDirector d, Transform selva, Vector3 C, Vector3 E, out Ruta iktan, out Ruta nahual)
    {
        iktan = new Ruta().En(7f, E + new Vector3(1f, 0f, -0.8f)).En(17f, C + new Vector3(-6f, 0f, -1f))
            .En(19f, C + new Vector3(-6f, 0f, -1f)).En(19.3f, C + new Vector3(-6f, 1f, -2.5f)).En(19.6f, C + new Vector3(-6f, 0f, -4f))   // esquiva saltando
            .En(24.6f, C + new Vector3(-6f, 0f, -4f)).En(25f, C + new Vector3(-6.5f, 0f, -4.8f));
        var abbi = new Ruta().En(7.3f, E + new Vector3(0f, 0f, 0.8f)).En(17f, C + new Vector3(-6f, 0f, 1.5f))
            .En(22f, C + new Vector3(-6f, 0f, 1.5f)).En(22.3f, C + new Vector3(-7.2f, 0.4f, 1.2f)).En(22.6f, C + new Vector3(-8.3f, 0f, 1f))  // salta y muerde
            .En(23.4f, C + new Vector3(-6f, 0f, 1.8f));
        nahual = new Ruta().En(14.5f, C + new Vector3(8f, 0f, 0f)).En(17.5f, C + new Vector3(4f, 0f, 2f)).En(18.6f, C + new Vector3(2f, 0f, -1f))
            .En(19.5f, C + new Vector3(-8.5f, 0f, -1.2f)).En(20.4f, C + new Vector3(-9.5f, 0f, -0.4f))                                   // embestida
            .En(21.5f, C + new Vector3(-9.5f, 0f, -0.4f)).En(22f, C + new Vector3(-11f, 0f, 0f))                                       // piedrazo 1
            .En(24f, C + new Vector3(-11f, 0f, 0f)).En(24.9f, C + new Vector3(-7f, 0f, -3.2f))                                         // segunda embestida
            .En(25.2f, C + new Vector3(-7f, 0f, -3.2f)).En(25.5f, C + new Vector3(-8f, 0f, -2.6f));                                    // piedrazo 2
        Ruta ik = iktan, nh = nahual;

        var tIktan = Personaje("IktanVoxel", selva, ik.Pos(0f), 90f, 1.4f);
        var tAbbi = Personaje("AbbiVoxel", selva, abbi.Pos(0f), 90f, 0.55f);
        var tNahual = Personaje("NahualVoxel", selva, nh.Pos(0f), -90f, 2.4f);
        tIktan.gameObject.AddComponent<VoxelWalk>();
        tAbbi.gameObject.AddComponent<VoxelWalk>().pasosPorMetro = 4f;
        tNahual.gameObject.AddComponent<VoxelWalk>().pasosPorMetro = 1.2f;

        // Mira hacia donde camina; quieto, mira a su rival. El Nahual tiembla antes del destello.
        Animar(tl, d, tIktan, ik, t => nh.Pos(t));
        Animar(tl, d, tAbbi, abbi, t => nh.Pos(t));
        Animar(tl, d, tNahual, nh, t => ik.Pos(t), t =>
        {
            float a = Mathf.Clamp01((t - 26f) / 3f) * Mathf.Clamp01((t - 26f) / 3f);
            return new Vector3(Mathf.PerlinNoise(t * 25f, 1f) - 0.5f, 0f, Mathf.PerlinNoise(t * 25f, 7f) - 0.5f) * 0.16f * a;
        });
        PistaActivacion(tl, d, tNahual.gameObject, 14.5f, 29.2f);

        // Piedras que lanza Iktan, con impacto (destello, luz y chispas).
        var piedra = Mat("PiedraLanzada", new Color(0.35f, 0.33f, 0.3f));
        foreach (var (sale, pega) in new[] { (21f, 21.5f), (24.8f, 25.2f) })
        {
            Vector3 desde = ik.Pos(sale) + new Vector3(0f, 1.1f, 0f), hasta = nh.Pos(pega) + new Vector3(0f, 1.2f, 0f);
            var roca = Cubo(selva, desde, Vector3.one * 0.22f, piedra);
            roca.name = "Piedra";
            PistaMovimiento(tl, d, roca.gameObject, DUR, t =>
            {
                float u = Mathf.InverseLerp(sale, pega, t);
                return (Vector3.Lerp(desde, hasta, u) + Vector3.up * Mathf.Sin(u * Mathf.PI) * 0.7f, Quaternion.Euler(t * 700f, t * 500f, 0f));
            }, 0.02f);
            PistaActivacion(tl, d, roca.gameObject, sale, pega);
            Destello(tl, d, selva, hasta, pega, 0.15f, 0.25f, 1.6f, 8f, 60);
        }

        // Final: el Nahual se llena de luz (26-29 s), estalla y en su lugar aparecen los molletes.
        Vector3 fin = nh.Pos(29f);
        Destello(tl, d, selva, fin + Vector3.up * 1.2f, 26f, 3.2f, 2.35f, 7f, 30f, 160);
        var molletes = Grupo(selva, "Molletes", fin + Vector3.up * 0.05f);
        PlatoMolletes(molletes, Vector3.zero, 5);
        PistaAnimacion(tl, d, molletes.gameObject, Clip(typeof(Transform), new[] { "m_LocalScale.x", "m_LocalScale.y", "m_LocalScale.z", "localEulerAnglesRaw.y" }, t =>
        {
            float m = Mathf.Clamp01((t - 29.45f) / 1.2f), e = 3f * m * (1f + Mathf.Sin(m * Mathf.PI) * 0.25f);
            return new[] { e, e, e, Mathf.Max(0f, t - 29.2f) * 20f };
        }));
    }

    // Iktan y Abbi suben la escalera de la iglesia (33-42 s).
    static Ruta Subida(TimelineAsset tl, PlayableDirector d, Transform fuera)
    {
        Ruta Por(Vector3[] puntos, float inicio)
        {
            var r = new Ruta();
            for (float t = 0f; t <= DUR; t += 0.25f) r.En(t, FUERA + EnRuta(puntos, 2f, inicio, t).pos);
            return r;
        }
        var iktan = Por(ChurchCutsceneBuilder.Ruta(-0.9f, -79f), 33f);
        var abbi = Por(ChurchCutsceneBuilder.Ruta(1.1f, -80.5f), 33.2f);
        var tI = Personaje("IktanVoxel", fuera, iktan.Pos(0f) - FUERA, 0f, 1.4f);
        var tA = Personaje("AbbiVoxel", fuera, abbi.Pos(0f) - FUERA, 0f, 0.55f);
        tI.gameObject.AddComponent<VoxelWalk>();
        tA.gameObject.AddComponent<VoxelWalk>().pasosPorMetro = 4f;
        Vector3 puerta = FUERA + new Vector3(0f, ChurchCutsceneBuilder.ALTO, -16f);
        Animar(tl, d, tI, iktan, t => puerta, null, FUERA);
        Animar(tl, d, tA, abbi, t => puerta, null, FUERA);
        return iktan;
    }

    // ---------------- Ayudantes ----------------

    // Mueve un personaje por su ruta (coordenadas del mundo; 'origen' = posición de su escenario).
    static void Animar(TimelineAsset tl, PlayableDirector d, Transform actor, Ruta ruta, Func<float, Vector3> rival,
        Func<float, Vector3> temblor = null, Vector3 origen = default)
    {
        Quaternion previo = Quaternion.identity;
        PistaMovimiento(tl, d, actor.gameObject, DUR, t =>
        {
            Vector3 p = ruta.Pos(t), v = ruta.Pos(t + 0.05f) - ruta.Pos(t - 0.05f), mira = rival(t) - p;
            v.y = 0f; mira.y = 0f;
            Vector3 dir = v.sqrMagnitude > 0.0004f ? v : mira;
            if (dir.sqrMagnitude > 0.0001f) previo = Quaternion.LookRotation(dir);
            return (p - origen + (temblor != null ? temblor(t) : Vector3.zero), previo);
        }, 0.05f);
    }

    // Esfera de luz aditiva + luz puntual + chispas: crece 'crece' s desde 'cuando', estalla y se apaga.
    static void Destello(TimelineAsset tl, PlayableDirector d, Transform padre, Vector3 pos, float cuando, float crece, float apaga,
        float tam, float luzMax, int chispas)
    {
        var esfera = Forma(PrimitiveType.Sphere, padre, pos, Vector3.zero, Aditivo("Destello", new Color(1f, 0.85f, 0.55f)));
        esfera.name = "Destello";
        var luz = new GameObject("Luz_Destello").AddComponent<Light>();
        luz.transform.SetParent(padre, false);
        luz.transform.position = pos + Vector3.up * 0.4f;
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.92f, 0.7f);
        luz.range = 14f;
        luz.intensity = 0f;
        float pico = cuando + crece;
        Func<float, float> f = t => t < cuando ? 0f : t < pico ? Mathf.Pow((t - cuando) / crece, 2f) * 0.5f
            : t < pico + 0.3f ? Mathf.Lerp(0.5f, 1f, (t - pico) / 0.3f) : Mathf.Lerp(1f, 0f, (t - pico - 0.3f) / apaga);
        PistaAnimacion(tl, d, esfera.gameObject, Clip(typeof(Transform), new[] { "m_LocalScale.x", "m_LocalScale.y", "m_LocalScale.z" },
            t => { float e = f(t) * tam; return new[] { e, e, e }; }));
        PistaAnimacion(tl, d, luz.gameObject, Clip(typeof(Light), new[] { "m_Intensity" }, t => new[] { f(t) * luzMax }));

        var ps = new GameObject("Chispas").AddComponent<ParticleSystem>();
        ps.transform.SetParent(padre, false);
        ps.transform.position = pos;
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 1f;
        main.startLifetime = 1.6f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
        main.gravityModifier = 0.2f;
        main.startColor = new Color(1f, 0.85f, 0.5f);
        var em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)chispas) });
        var forma = ps.shape;
        forma.shapeType = ParticleSystemShapeType.Sphere;
        forma.radius = 0.5f;
        ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = Aditivo("Chispa", new Color(1f, 0.8f, 0.4f));
        PistaParticulas(tl, d, ps.gameObject, pico, 2.5f);
    }

    static AnimationClip Clip(Type tipo, string[] props, Func<float, float[]> valores, float paso = 0.05f)
    {
        int n = Mathf.CeilToInt(DUR / paso) + 1;
        var t = new float[n];
        var v = new float[props.Length][];
        for (int k = 0; k < props.Length; k++) v[k] = new float[n];
        for (int i = 0; i < n; i++)
        {
            t[i] = Mathf.Min(i * paso, DUR);
            var x = valores(t[i]);
            for (int k = 0; k < props.Length; k++) v[k][i] = x[k];
        }
        var clip = new AnimationClip();
        for (int k = 0; k < props.Length; k++) Curva(clip, tipo, props[k], t, v[k]);
        return clip;
    }

    static Transform Escenario(Transform raiz, string nombre, Vector3 pos, Action<SceneMood> ajustar)
    {
        var t = Grupo(raiz, nombre, pos);
        ajustar(t.gameObject.AddComponent<SceneMood>());
        return t;
    }

    static Transform PiezasEn(Transform t, string nombre)
    {
        foreach (var c in t.GetComponentsInChildren<Transform>(true)) if (c.name == nombre) return c;
        return null;
    }

    // ---------------- Textos, franjas de cine y fundidos ----------------

    static void Textos(TimelineAsset tl, PlayableDirector d, Transform raiz)
    {
        var canvasGo = new GameObject("Trailer_UI", typeof(RectTransform));
        canvasGo.transform.SetParent(raiz, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var esc = canvasGo.AddComponent<CanvasScaler>();
        esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        esc.referenceResolution = new Vector2(1920f, 1080f);
        esc.matchWidthOrHeight = 0.5f;
        var c = canvasGo.transform;
        var fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Fundido a negro en cada corte y al final (debajo de los textos).
        var negro = Panel(c, "Fundido", Vector2.zero, Vector2.one);
        PistaAlfa(tl, d, negro, t =>
        {
            if (t < 0.8f) return 1f - t / 0.8f;
            if (t >= 53f) return Mathf.Clamp01((t - 53f) / 0.6f);
            float a = 0f;
            foreach (float k in cortes) a = Mathf.Max(a, 1f - Mathf.Abs(t - k) / 0.3f);
            return a;
        });
        Panel(c, "Franja_Arriba", new Vector2(0f, 0.88f), Vector2.one);   // formato de cine
        Panel(c, "Franja_Abajo", Vector2.zero, new Vector2(1f, 0.12f));

        foreach (var (texto, desde, hasta) in new[] {
            ("Perdido en la selva...", 1f, 5.5f), ("...algo acecha entre las ruinas", 8f, 12f), ("Enfrenta al Nahual", 17.5f, 20.5f),
            ("Sube a la iglesia abandonada", 34f, 38.5f), ("Descubre sus secretos", 43f, 47f), ("...y recuerda quién eres", 49f, 52.5f) })
        {
            var g = Texto(c, texto, fuente, 58, new Color(1f, 0.95f, 0.85f), new Vector2(0.5f, 0.2f));
            PistaAlfa(tl, d, g, t => Mathf.Clamp01(Mathf.Min((t - desde) / 0.6f, (hasta - t) / 0.6f)));
        }
        var titulo = Texto(c, TITULO, fuente, 190, new Color(1f, 0.6f, 0.1f), new Vector2(0.5f, 0.55f));
        titulo.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;
        PistaAlfa(tl, d, titulo, t => Mathf.Clamp01((t - 54f) / 1.2f));
        var sub = Texto(c, "Próximamente", fuente, 48, new Color(0.9f, 0.88f, 0.82f), new Vector2(0.5f, 0.38f));
        PistaAlfa(tl, d, sub, t => Mathf.Clamp01((t - 56f) / 1f));
    }

    static GameObject Panel(Transform padre, string nombre, Vector2 min, Vector2 max)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        go.AddComponent<Image>().color = Color.black;
        var rt = (RectTransform)go.transform;
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return go;
    }

    static GameObject Texto(Transform padre, string contenido, Font fuente, int tam, Color color, Vector2 ancla)
    {
        var go = new GameObject("Texto_" + contenido.Trim('.', ' ').Replace(' ', '_'), typeof(RectTransform));
        go.transform.SetParent(padre, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = ancla;
        rt.sizeDelta = new Vector2(1800f, tam * 1.6f);
        var t = go.AddComponent<Text>();
        t.text = contenido;
        t.font = fuente;
        t.fontSize = tam;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.raycastTarget = false;
        var sombra = go.AddComponent<Shadow>();
        sombra.effectColor = new Color(0f, 0f, 0f, 0.85f);
        sombra.effectDistance = new Vector2(4f, -4f);
        return go;
    }

    static void PistaAlfa(TimelineAsset tl, PlayableDirector d, GameObject go, Func<float, float> alfa)
    {
        var g = go.AddComponent<CanvasGroup>();
        g.alpha = alfa(0f);
        g.blocksRaycasts = false;
        PistaAnimacion(tl, d, go, Clip(typeof(CanvasGroup), new[] { "m_Alpha" }, t => new[] { alfa(t) }));
    }
}
