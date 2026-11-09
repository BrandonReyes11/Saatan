// Saatan Historia - Tutorial: sala de controles + zona de práctica.
// Colocar en: Assets/Editor/ConstructorTutorial.cs
// No necesita ningún otro script (solo construye el escenario).
//
// Uso:
//   1) Selecciona sala_controles.png en Project y elige  Saatan > Tutorial > Construir sala de controles
//   2) Selecciona practica.png en Project y elige        Saatan > Tutorial > Construir zona de práctica
//
// Los textos de controles están en las constantes de abajo: cámbialos para que coincidan con tu juego.
//
// Colores del mapa:
//   BLANCO piso            NEGRO  muro del panteón (colisión)      VERDE   hierba alta
//   GRIS   lápida          CAFÉ   tronco caído                     NARANJA veladora
//   ROSA   placa de control (sala)     AZUL CLARO cartel de práctica (zona)
//   AZUL   inicio de Iktan             AMARILLO   puerta de salida (portal de cempasúchil)
//   ROJO   guardia de práctica (marcador + aros de ruido)          CIAN    blanco para lanzar piedras/trompos
//   MORADO vitral con tablas     MAGENTA ofrenda     DURAZNO camino de cempasúchil
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class ConstructorTutorial
{
    enum Tipo { Piso, Limite, Pasto, Lapida, Tronco, Panel, Vela, Inicio, Meta, Guardia, Blanco, Vitral, Camino, Cartel, Ofrenda }

    // ---- CONTROLES (cámbialos para que coincidan con tu juego) ----
    const string T_MOVER = "W A S D / flechas";
    const string T_AGACHAR = "CTRL / C";
    const string T_PIEDRA = "clic izquierdo";
    const string T_TROMPO = "clic derecho";
    const string T_ABRAZAR = "ESPACIO";
    const string T_INTERACTUAR = "E";
    // Etiquetas de las teclas dibujadas en las placas
    const string TECLA_AGACHAR_1 = "CTRL";
    const string TECLA_AGACHAR_2 = "C";
    const string TECLA_ABRAZAR = "ESPACIO";
    const string TECLA_INTERACTUAR = "E";

    // Radio (en metros) hasta donde oye un guardia
    const float RADIO_CORRIENDO = 5.5f;
    const float RADIO_AGACHADO = 2.4f;

    // ---- TEXTOS DE LA SALA DE CONTROLES (9 placas, de arriba a abajo y de izquierda a derecha) ----
    static readonly string[] panelTitulo =
    {
        "1  MOVERSE", "2  AGACHARSE", "3  LANZAR PIEDRA",
        "4  LANZAR TROMPO", "5  ABRAZAR A ABBI", "6  INTERACTUAR",
        "7  ESCONDERSE", "8  RUIDO", "9  OFRENDA",
    };

    static readonly string[] panelTexto =
    {
        "Explora el panteón.\nCorrer hace más ruido.",
        "Más lento, pero casi sin ruido.\nÚsalo cerca de los guardias.",
        "Apunta con el ratón y lánzala.\nLos enemigos van hacia el ruido.",
        "Su zumbido calma a las almas\ny atrae a los guardias.",
        "En la bruma roja el miedo frena.\nAbraza a Abbi: recuperas valor.",
        "Derriba tablas, recoge recompensas\ny deja tus ofrendas.",
        "Hierba alta, troncos y lápidas\ntapan la vista del enemigo.",
        "Cada acción hace ruido.\nCorriendo se oye lejos.",
        "Cada recompensa sube un piso\ndel altar de tu familia.",
    };

    // ---- TEXTOS DE LA ZONA DE PRÁCTICA (7 carteles, de izquierda a derecha) ----
    static readonly string[] cartelTitulo =
    {
        "1  MOVERSE", "2  AGACHARSE Y RUIDO", "3  ESCONDERSE", "4  LANZAR PIEDRAS",
        "5  LANZAR TROMPOS", "6  VITRALES", "7  OFRENDA Y SALIDA",
    };

    static readonly string[] cartelTexto =
    {
        "Muévete con " + T_MOVER + ".\nSigue el camino de cempasúchil.\nCorrer hace ruido: ¡ojo!",
        "Mantén " + T_AGACHAR + " para agacharte.\nAro rojo: lo que oyen si corres.\nAro amarillo: si vas agachado.",
        "Agáchate en la hierba alta.\nTroncos y lápidas tapan la vista.\nNo entres al aro del guardia.",
        "Con " + T_PIEDRA + " lanzas una piedra.\nTírala al blanco: el guardia irá.\nEntonces pasa por su espalda.",
        "Con " + T_TROMPO + " lanzas un trompo.\nSu zumbido atrae a los guardias.\nLánzalos en fila para abrir paso.",
        "Junto a las tablas mantén " + T_INTERACTUAR + ".\nAl derribarlas entra la luz.\nLa luz daña a las sombras.",
        "Cada recompensa sube un piso.\nEl altar es el amor de tu familia.\nCruza la puerta para empezar.",
    };

    static readonly Color cTitulo = new Color(1.00f, 0.78f, 0.20f);
    static readonly Color cTexto = new Color(0.92f, 0.92f, 0.96f);
    static readonly Color cRojo = new Color(1.00f, 0.35f, 0.30f);
    static readonly Color cAmarillo = new Color(1.00f, 0.85f, 0.25f);
    static readonly Color cTecla = new Color(0.10f, 0.10f, 0.12f);

    static readonly (Color32 color, Tipo tipo)[] paleta =
    {
        (new Color32(255, 255, 255, 255), Tipo.Piso),
        (new Color32(0, 0, 0, 255),       Tipo.Limite),
        (new Color32(0, 170, 0, 255),     Tipo.Pasto),
        (new Color32(128, 128, 128, 255), Tipo.Lapida),
        (new Color32(140, 80, 30, 255),   Tipo.Tronco),
        (new Color32(255, 105, 180, 255), Tipo.Panel),
        (new Color32(255, 150, 0, 255),   Tipo.Vela),
        (new Color32(0, 100, 255, 255),   Tipo.Inicio),
        (new Color32(255, 220, 0, 255),   Tipo.Meta),
        (new Color32(230, 0, 0, 255),     Tipo.Guardia),
        (new Color32(0, 210, 210, 255),   Tipo.Blanco),
        (new Color32(150, 0, 200, 255),   Tipo.Vitral),
        (new Color32(255, 200, 120, 255), Tipo.Camino),
        (new Color32(80, 80, 255, 255),   Tipo.Cartel),
        (new Color32(255, 0, 255, 255),   Tipo.Ofrenda),
    };

    static readonly Vector2Int[] dirs = { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };

    static readonly Dictionary<string, Color> colores = new Dictionary<string, Color>
    {
        { "PisoA", new Color(0.30f, 0.25f, 0.36f) },
        { "PisoB", new Color(0.25f, 0.20f, 0.31f) },
        { "PiedraMuro", new Color(0.38f, 0.36f, 0.40f) },
        { "PiedraOscura", new Color(0.26f, 0.25f, 0.28f) },
        { "Piedra", new Color(0.52f, 0.52f, 0.55f) },
        { "PlacaPiedra", new Color(0.16f, 0.14f, 0.20f) },
        { "Musgo", new Color(0.20f, 0.40f, 0.16f) },
        { "PastoA", new Color(0.12f, 0.32f, 0.12f) },
        { "PastoB", new Color(0.18f, 0.42f, 0.14f) },
        { "Corteza", new Color(0.22f, 0.14f, 0.09f) },
        { "CortezaClara", new Color(0.36f, 0.25f, 0.15f) },
        { "MaderaOscura", new Color(0.30f, 0.17f, 0.09f) },
        { "MaderaClara", new Color(0.52f, 0.33f, 0.17f) },
        { "Madera", new Color(0.70f, 0.47f, 0.24f) },
        { "Cempasuchil", new Color(1.00f, 0.60f, 0.05f) },
        { "Cempasuchil2", new Color(1.00f, 0.78f, 0.10f) },
        { "Llama", new Color(1.00f, 0.65f, 0.15f) },
        { "VidrioRojo", new Color(0.80f, 0.10f, 0.15f) },
        { "VidrioAzul", new Color(0.15f, 0.30f, 0.85f) },
        { "Cera", new Color(0.98f, 0.94f, 0.80f) },
        { "TeclaBase", new Color(0.12f, 0.12f, 0.15f) },
        { "TeclaTapa", new Color(0.86f, 0.86f, 0.90f) },
        { "MarcadorInicio", new Color(0.00f, 0.40f, 1.00f) },
        { "MarcadorGuardia", new Color(0.90f, 0.05f, 0.05f) },
        { "AroRojo", new Color(1.00f, 0.20f, 0.20f) },
        { "AroAmarillo", new Color(1.00f, 0.85f, 0.15f) },
        { "BlancoRojo", new Color(0.90f, 0.15f, 0.15f) },
        { "BlancoBlanco", new Color(0.95f, 0.95f, 0.92f) },
        { "VitralRojo", new Color(0.90f, 0.10f, 0.15f) },
        { "VitralAzul", new Color(0.15f, 0.30f, 0.95f) },
        { "VitralVerde", new Color(0.10f, 0.70f, 0.30f) },
        { "VitralAmarillo", new Color(1.00f, 0.80f, 0.10f) },
        { "Portal", new Color(1.00f, 0.88f, 0.60f) },
        { "PapelMorado", new Color(0.55f, 0.15f, 0.75f) },
        { "PapelNaranja", new Color(1.00f, 0.50f, 0.05f) },
        { "AbbiBlanco", new Color(0.95f, 0.95f, 0.92f) },
        { "AbbiNariz", new Color(1.00f, 0.30f, 0.45f) },
        { "TrompoRojo", new Color(0.85f, 0.15f, 0.15f) },
    };

    // Materiales que brillan y con qué fuerza.
    static readonly Dictionary<string, float> brillo = new Dictionary<string, float>
    {
        { "Llama", 2.5f }, { "AroRojo", 2.0f }, { "AroAmarillo", 2.0f }, { "Cempasuchil", 0.5f },
        { "Cempasuchil2", 0.5f }, { "Portal", 1.5f }, { "VitralRojo", 2.5f }, { "VitralAzul", 2.5f },
        { "VitralVerde", 2.5f }, { "VitralAmarillo", 2.5f }, { "MarcadorGuardia", 0.8f },
        { "MarcadorInicio", 0.8f }, { "PapelNaranja", 0.5f },
    };

    static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

    [MenuItem("Saatan/Tutorial/Construir sala de controles")]
    static void SalaControles()
    {
        Construir("Sala_Controles", "sala_controles.png", true);
    }

    [MenuItem("Saatan/Tutorial/Construir zona de práctica")]
    static void ZonaPractica()
    {
        Construir("Zona_Practica", "practica.png", false);
    }

    static void Construir(string nombreRaiz, string nombreImagen, bool sala)
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Saatan", "Sal del modo Play antes de construir el escenario.", "OK");
            return;
        }

        var tex = Selection.activeObject as Texture2D;
        if (tex == null)
        {
            EditorUtility.DisplayDialog("Saatan", "Primero selecciona " + nombreImagen + " en la ventana Project y vuelve a intentarlo.", "OK");
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
        if (!AssetDatabase.IsValidFolder("Assets/Niveles/Tutorial")) AssetDatabase.CreateFolder("Assets/Niveles", "Tutorial");
        cache.Clear();

        var viejo = GameObject.Find(nombreRaiz);
        if (viejo != null) Object.DestroyImmediate(viejo);
        var raiz = new GameObject(nombreRaiz).transform;

        var mapa = new int[w, h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color32 p = px[y * w + x];
                mapa[x, y] = p.a < 128 ? -1 : (int)Clasificar(p);
            }

        var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Mesh cubo = tmp.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(tmp);

        var piso = new Dictionary<string, List<CombineInstance>>();
        var deco = new Dictionary<string, List<CombineInstance>>();
        var colision = new List<CombineInstance>();

        // ---- Piso, muros, hierba y pétalos (combinados en pocas mallas) ----
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (mapa[x, y] < 0) continue;
                Tipo t = (Tipo)mapa[x, y];
                Vector3 p = Pos(x, y, w, h);

                string mat = ((x + y) % 2 == 0) ? "PisoA" : "PisoB";
                SumarTRS(Lista(piso, mat), cubo, new Vector3(p.x, -0.1f, p.z), Quaternion.identity, new Vector3(0.98f, 0.2f, 0.98f));
                SumarTRS(colision, cubo, new Vector3(p.x, -0.1f, p.z), Quaternion.identity, new Vector3(1f, 0.2f, 1f));

                switch (t)
                {
                    case Tipo.Limite:
                        SumarTRS(colision, cubo, new Vector3(p.x, 3f, p.z), Quaternion.identity, new Vector3(1f, 6f, 1f));
                        MuroCelda(deco, cubo, p, x * 73 + y * 131);
                        break;
                    case Tipo.Pasto: PastoCelda(deco, cubo, p, x * 97 + y * 57); break;
                    case Tipo.Camino: Petalos(deco, cubo, p, x * 41 + y * 83); break;
                }
            }

        Publicar(raiz, piso, "Piso_");
        Publicar(raiz, deco, "Deco_");

        var lim = new GameObject("Colision_Muros");
        lim.transform.SetParent(raiz, false);
        lim.AddComponent<MeshCollider>().sharedMesh = GuardarMalla("Colision_" + nombreRaiz, colision);

        // ---- Objetos celda por celda ----
        var panelCeldas = new List<Vector2Int>();
        var cartelCeldas = new List<Vector2Int>();
        int nGuardia = 0;

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (mapa[x, y] < 0) continue;
                Vector3 p = Pos(x, y, w, h);
                switch ((Tipo)mapa[x, y])
                {
                    case Tipo.Lapida: Lapida(raiz, p, x * 53 + y * 29); break;
                    case Tipo.Vela: Veladora(raiz, p, x * 7 + y * 3, (x + y) % 2 == 0); break;
                    case Tipo.Inicio: Marcador(raiz, "Inicio_Jugador", p); break;
                    case Tipo.Guardia: nGuardia++; Guardia(raiz, p, nGuardia); break;
                    case Tipo.Blanco: Blanco(raiz, p); break;
                    case Tipo.Vitral: Vitral(raiz, p, 180f); break;
                    case Tipo.Ofrenda: Ofrenda(raiz, p); break;
                    case Tipo.Meta: Portal(raiz, p, sala ? 0f : 90f); break;
                    case Tipo.Panel: panelCeldas.Add(new Vector2Int(x, y)); break;
                    case Tipo.Cartel: cartelCeldas.Add(new Vector2Int(x, y)); break;
                }
            }

        foreach (var c in Componentes(mapa, w, h, Tipo.Tronco))
        {
            Rango(c, w, h, out Vector3 centro, out int ancho, out int fondo);
            TroncoCaido(raiz, centro, ancho, fondo);
        }

        // Placas de la sala: de arriba hacia abajo y de izquierda a derecha.
        panelCeldas.Sort((a, b) => a.y != b.y ? b.y.CompareTo(a.y) : a.x.CompareTo(b.x));
        for (int i = 0; i < panelCeldas.Count; i++)
            PanelControl(raiz, Pos(panelCeldas[i].x, panelCeldas[i].y, w, h), i);

        // Carteles de la práctica: de izquierda a derecha.
        cartelCeldas.Sort((a, b) => a.x.CompareTo(b.x));
        for (int i = 0; i < cartelCeldas.Count; i++)
            Cartel(raiz, Pos(cartelCeldas[i].x, cartelCeldas[i].y, w, h), i);

        ConfigurarEscena(sala);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkAllScenesDirty();
        Selection.activeGameObject = raiz.gameObject;
        Debug.Log(nombreRaiz + " creada (" + w + " x " + h + "). Mallas y materiales en Assets/Niveles/Tutorial.");
    }

    // ---------------- Decoración combinada ----------------

    static void MuroCelda(Dictionary<string, List<CombineInstance>> d, Mesh cubo, Vector3 p, int semilla)
    {
        var r = new System.Random(semilla);
        float alto = Rf(r, 2.0f, 2.8f);
        SumarTRS(Lista(d, "PiedraMuro"), cubo, p + Vector3.up * alto / 2f, Quaternion.identity, new Vector3(1.02f, alto, 1.02f));
        SumarTRS(Lista(d, "PiedraOscura"), cubo, p + Vector3.up * (alto + 0.08f), Quaternion.identity, new Vector3(1.14f, 0.16f, 1.14f));
    }

    static void PastoCelda(Dictionary<string, List<CombineInstance>> d, Mesh cubo, Vector3 p, int semilla)
    {
        var r = new System.Random(semilla);
        for (int k = 0; k < 6; k++)
        {
            float alto = Rf(r, 1.0f, 1.7f);
            Vector3 q = p + new Vector3(Rf(r, -0.42f, 0.42f), alto / 2f, Rf(r, -0.42f, 0.42f));
            Quaternion rot = Quaternion.Euler(Rf(r, -12f, 12f), Rf(r, 0f, 360f), Rf(r, -12f, 12f));
            SumarTRS(Lista(d, k % 2 == 0 ? "PastoA" : "PastoB"), cubo, q, rot, new Vector3(0.14f, alto, 0.14f));
        }
    }

    // Pétalos de cempasúchil sobre el camino.
    static void Petalos(Dictionary<string, List<CombineInstance>> d, Mesh cubo, Vector3 p, int semilla)
    {
        var r = new System.Random(semilla);
        for (int k = 0; k < 9; k++)
        {
            Vector3 q = p + new Vector3(Rf(r, -0.45f, 0.45f), 0.04f, Rf(r, -0.45f, 0.45f));
            Quaternion rot = Quaternion.Euler(0f, Rf(r, 0f, 360f), 0f);
            SumarTRS(Lista(d, k % 2 == 0 ? "Cempasuchil" : "Cempasuchil2"), cubo, q, rot, new Vector3(0.26f, 0.03f, 0.26f));
        }
    }

    // ---------------- Placas, carteles y texto ----------------

    static void PanelControl(Transform raiz, Vector3 pos, int i)
    {
        var g = Grupo(raiz, "Panel_" + (i + 1), pos);
        Placa(g, 9f, 7.2f);
        Texto(g, i < panelTitulo.Length ? panelTitulo[i] : "", new Vector3(0f, 0.13f, 2.85f), 0.8f, cTitulo);
        var icono = Grupo(g, "Icono", new Vector3(0f, 0.1f, 0.55f));
        Icono(icono, i);
        Texto(g, i < panelTexto.Length ? panelTexto[i] : "", new Vector3(0f, 0.13f, -2.4f), 0.4f, cTexto);
    }

    static void Cartel(Transform raiz, Vector3 pos, int i)
    {
        var g = Grupo(raiz, "Cartel_" + (i + 1), pos);
        Placa(g, 10.4f, 4.6f);
        Texto(g, i < cartelTitulo.Length ? cartelTitulo[i] : "", new Vector3(0f, 0.13f, 1.5f), 0.7f, cTitulo);
        Texto(g, i < cartelTexto.Length ? cartelTexto[i] : "", new Vector3(0f, 0.13f, -0.5f), 0.42f, cTexto);
    }

    // Placa de piedra con borde de cempasúchil, a ras de suelo (se puede caminar encima).
    static void Placa(Transform g, float ancho, float fondo)
    {
        Pieza(g, "Placa", PrimitiveType.Cube, new Vector3(0f, 0.05f, 0f), new Vector3(ancho, 0.1f, fondo), "PlacaPiedra");
        float bx = ancho / 2f, bz = fondo / 2f;
        Pieza(g, "BordeN", PrimitiveType.Cube, new Vector3(0f, 0.09f, bz - 0.05f), new Vector3(ancho, 0.08f, 0.1f), "Cempasuchil");
        Pieza(g, "BordeS", PrimitiveType.Cube, new Vector3(0f, 0.09f, -bz + 0.05f), new Vector3(ancho, 0.08f, 0.1f), "Cempasuchil");
        Pieza(g, "BordeE", PrimitiveType.Cube, new Vector3(bx - 0.05f, 0.09f, 0f), new Vector3(0.1f, 0.08f, fondo), "Cempasuchil");
        Pieza(g, "BordeO", PrimitiveType.Cube, new Vector3(-bx + 0.05f, 0.09f, 0f), new Vector3(0.1f, 0.08f, fondo), "Cempasuchil");
    }

    // Texto acostado en el suelo (se lee desde la cámara de arriba). altura = alto aproximado de una línea en metros.
    static TextMesh Texto(Transform padre, string texto, Vector3 pos, float altura, Color color)
    {
        var go = new GameObject("Texto");
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        var f = Fuente();
        var tm = go.AddComponent<TextMesh>();
        tm.text = texto;
        tm.font = f;
        tm.fontSize = 64;
        tm.characterSize = altura / 6.4f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;
        tm.richText = false;
        go.GetComponent<MeshRenderer>().sharedMaterial = f.material;
        return tm;
    }

    static Font Fuente()
    {
        var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return f;
    }

    // Dibujos de cada placa de la sala.
    static void Icono(Transform g, int i)
    {
        switch (i)
        {
            case 0: // W A S D
                Tecla(g, "W", new Vector3(-2.0f, 0f, 0.8f));
                Tecla(g, "A", new Vector3(-3.1f, 0f, -0.3f));
                Tecla(g, "S", new Vector3(-2.0f, 0f, -0.3f));
                Tecla(g, "D", new Vector3(-0.9f, 0f, -0.3f));
                Texto(g, "o\nflechas", new Vector3(2.3f, 0.36f, 0.2f), 0.5f, cTexto);
                break;
            case 1: // agacharse
                Tecla(g, TECLA_AGACHAR_1, new Vector3(-1.7f, 0f, 0f), 1.9f);
                Texto(g, "o", new Vector3(0f, 0.36f, 0f), 0.6f, cTexto);
                Tecla(g, TECLA_AGACHAR_2, new Vector3(1.4f, 0f, 0f));
                break;
            case 2: // piedra (clic izquierdo)
                Raton(g, new Vector3(-1.2f, 0f, 0f), true);
                Pieza(g, "Piedra", PrimitiveType.Sphere, new Vector3(1.8f, 0.3f, 0.2f), new Vector3(0.7f, 0.5f, 0.6f), "Piedra");
                Pieza(g, "Piedra", PrimitiveType.Sphere, new Vector3(2.6f, 0.2f, -0.3f), new Vector3(0.45f, 0.35f, 0.4f), "Piedra");
                break;
            case 3: // trompo (clic derecho)
                Raton(g, new Vector3(-1.6f, 0f, 0f), false);
                Pieza(g, "Trompo", PrimitiveType.Sphere, new Vector3(1.9f, 0.35f, 0f), new Vector3(0.6f, 0.45f, 0.6f), "TrompoRojo");
                Pieza(g, "Tallo", PrimitiveType.Cylinder, new Vector3(1.9f, 0.78f, 0f), new Vector3(0.08f, 0.15f, 0.08f), "Madera");
                AroPieza(g, new Vector3(1.9f, 0f, 0f), 1.5f, 24, "AroAmarillo");
                break;
            case 4: // abrazar a Abbi
                Tecla(g, TECLA_ABRAZAR, new Vector3(-1.6f, 0f, 0f), 4.2f);
                Pieza(g, "AbbiCuerpo", PrimitiveType.Sphere, new Vector3(2.9f, 0.4f, 0f), new Vector3(0.7f, 0.5f, 0.9f), "AbbiBlanco");
                Pieza(g, "AbbiCabeza", PrimitiveType.Sphere, new Vector3(2.9f, 0.65f, 0.55f), new Vector3(0.5f, 0.45f, 0.5f), "AbbiBlanco");
                Pieza(g, "AbbiNariz", PrimitiveType.Sphere, new Vector3(2.9f, 0.62f, 0.85f), Vector3.one * 0.13f, "AbbiNariz");
                Pieza(g, "AbbiOreja", PrimitiveType.Cube, new Vector3(2.68f, 0.95f, 0.5f), new Vector3(0.12f, 0.3f, 0.1f), "AbbiBlanco");
                Pieza(g, "AbbiOreja", PrimitiveType.Cube, new Vector3(3.12f, 0.95f, 0.5f), new Vector3(0.12f, 0.3f, 0.1f), "AbbiBlanco");
                break;
            case 5: // interactuar
                Tecla(g, TECLA_INTERACTUAR, new Vector3(-1.0f, 0f, 0f), 1.2f);
                var tabla = Pieza(g, "Tabla", PrimitiveType.Cube, new Vector3(2.2f, 0.12f, 0.2f), new Vector3(2.4f, 0.14f, 0.5f), "MaderaOscura");
                tabla.localRotation = Quaternion.Euler(0f, 15f, 0f);
                var tabla2 = Pieza(g, "Tabla", PrimitiveType.Cube, new Vector3(2.4f, 0.2f, -0.5f), new Vector3(2.2f, 0.14f, 0.5f), "MaderaClara");
                tabla2.localRotation = Quaternion.Euler(0f, -10f, 0f);
                break;
            case 6: // esconderse: hierba y lápida
                for (int k = 0; k < 8; k++)
                {
                    float alto = 0.9f + (k % 4) * 0.2f;
                    float ang = k * 45f;
                    var b = Pieza(g, "Hoja", PrimitiveType.Cube, new Vector3(-2.4f, 0f, 0f) + Dir(ang) * 0.5f + Vector3.up * alto / 2f, new Vector3(0.14f, alto, 0.14f), k % 2 == 0 ? "PastoA" : "PastoB");
                    b.localRotation = Quaternion.Euler(Dir(ang).z * 10f, ang, -Dir(ang).x * 10f);
                }
                Pieza(g, "Base", PrimitiveType.Cube, new Vector3(2.2f, 0.15f, 0f), new Vector3(1.1f, 0.3f, 0.7f), "PiedraOscura");
                Pieza(g, "Losa", PrimitiveType.Cube, new Vector3(2.2f, 0.95f, 0f), new Vector3(0.85f, 1.3f, 0.25f), "Piedra");
                Pieza(g, "Remate", PrimitiveType.Cube, new Vector3(2.2f, 1.68f, 0f), new Vector3(0.6f, 0.16f, 0.25f), "Piedra");
                break;
            case 7: // ruido: aros
                AroPieza(g, Vector3.zero, 1.9f, 40, "AroRojo");
                AroPieza(g, Vector3.zero, 0.85f, 20, "AroAmarillo");
                Pieza(g, "Centro", PrimitiveType.Sphere, new Vector3(0f, 0.25f, 0f), Vector3.one * 0.5f, "MarcadorInicio");
                break;
            case 8: // ofrenda: altar de tres pisos
                Pieza(g, "Piso1", PrimitiveType.Cube, new Vector3(0f, 0.25f, 0f), new Vector3(3.6f, 0.5f, 2.2f), "MaderaOscura");
                Pieza(g, "Piso2", PrimitiveType.Cube, new Vector3(0f, 0.75f, 0f), new Vector3(2.7f, 0.5f, 1.7f), "PapelMorado");
                Pieza(g, "Piso3", PrimitiveType.Cube, new Vector3(0f, 1.25f, 0f), new Vector3(1.8f, 0.5f, 1.2f), "MaderaClara");
                for (int k = 0; k < 6; k++)
                    Pieza(g, "Flor", PrimitiveType.Cube, new Vector3(-1.5f + k * 0.6f, 0.5f, -1.2f), Vector3.one * 0.3f, k % 2 == 0 ? "Cempasuchil" : "Cempasuchil2");
                foreach (float sx in new[] { -0.6f, 0.6f })
                {
                    Pieza(g, "Vela", PrimitiveType.Cylinder, new Vector3(sx, 1.65f, 0f), new Vector3(0.14f, 0.15f, 0.14f), "Cera");
                    Pieza(g, "Llama", PrimitiveType.Sphere, new Vector3(sx, 1.93f, 0f), new Vector3(0.1f, 0.18f, 0.1f), "Llama");
                }
                break;
        }
    }

    static void Tecla(Transform padre, string etiqueta, Vector3 pos, float ancho = 0.95f)
    {
        var g = Grupo(padre, "Tecla_" + etiqueta, pos);
        Pieza(g, "Base", PrimitiveType.Cube, new Vector3(0f, 0.1f, 0f), new Vector3(ancho, 0.2f, 0.95f), "TeclaBase");
        Pieza(g, "Tapa", PrimitiveType.Cube, new Vector3(0f, 0.27f, 0f), new Vector3(ancho - 0.12f, 0.14f, 0.83f), "TeclaTapa");
        Texto(g, etiqueta, new Vector3(0f, 0.36f, 0f), etiqueta.Length > 2 ? 0.34f : 0.6f, cTecla);
    }

    // Ratón visto desde arriba; resalta el botón que se usa.
    static void Raton(Transform padre, Vector3 pos, bool izquierdo)
    {
        var g = Grupo(padre, "Raton", pos);
        Pieza(g, "Cuerpo", PrimitiveType.Cube, new Vector3(0f, 0.2f, 0f), new Vector3(1.5f, 0.4f, 2.3f), "TeclaBase");
        Pieza(g, "BotonIzq", PrimitiveType.Cube, new Vector3(-0.38f, 0.43f, 0.62f), new Vector3(0.7f, 0.1f, 0.98f), izquierdo ? "Cempasuchil" : "TeclaTapa");
        Pieza(g, "BotonDer", PrimitiveType.Cube, new Vector3(0.38f, 0.43f, 0.62f), new Vector3(0.7f, 0.1f, 0.98f), izquierdo ? "TeclaTapa" : "Cempasuchil");
        Pieza(g, "Rueda", PrimitiveType.Cube, new Vector3(0f, 0.46f, 0.7f), new Vector3(0.12f, 0.12f, 0.4f), "TeclaBase");
    }

    // Círculo hecho con segmentos planos y brillantes, a ras de suelo.
    static void AroPieza(Transform padre, Vector3 centro, float radio, int seg, string mat)
    {
        var g = Grupo(padre, "Aro", centro);
        float arco = 2f * Mathf.PI * radio / seg * 1.08f;
        for (int i = 0; i < seg; i++)
        {
            float a = i * 360f / seg;
            var s = Pieza(g, "Seg", PrimitiveType.Cube, Dir(a) * radio + new Vector3(0f, 0.04f, 0f), new Vector3(0.14f, 0.04f, arco), mat);
            s.localRotation = Quaternion.Euler(0f, a + 90f, 0f);
        }
    }

    // ---------------- Zona de práctica ----------------

    // Marcador del guardia (aquí pones tu enemigo) con los aros de lo que oye.
    static void Guardia(Transform padre, Vector3 pos, int n)
    {
        var g = Grupo(padre, "Guardia_Practica_" + n, pos);
        Pieza(g, "Marcador", PrimitiveType.Sphere, new Vector3(0f, 0.6f, 0f), Vector3.one, "MarcadorGuardia");
        AroPieza(g, Vector3.zero, RADIO_CORRIENDO, 56, "AroRojo");
        AroPieza(g, Vector3.zero, RADIO_AGACHADO, 28, "AroAmarillo");
        Texto(g, "CORRIENDO", new Vector3(0f, 0.06f, -(RADIO_CORRIENDO + 0.7f)), 0.5f, cRojo);
        Texto(g, "AGACHADO", new Vector3(0f, 0.06f, -(RADIO_AGACHADO + 0.5f)), 0.45f, cAmarillo);
    }

    // Diana en el suelo para lanzar piedras o trompos.
    static void Blanco(Transform padre, Vector3 pos)
    {
        var g = Grupo(padre, "Blanco_Practica", pos);
        Pieza(g, "Aro3", PrimitiveType.Cylinder, new Vector3(0f, 0.03f, 0f), new Vector3(2.6f, 0.02f, 2.6f), "BlancoRojo");
        Pieza(g, "Aro2", PrimitiveType.Cylinder, new Vector3(0f, 0.045f, 0f), new Vector3(1.8f, 0.02f, 1.8f), "BlancoBlanco");
        Pieza(g, "Aro1", PrimitiveType.Cylinder, new Vector3(0f, 0.06f, 0f), new Vector3(0.9f, 0.02f, 0.9f), "BlancoRojo");
    }

    // Vitral con tablas podridas (mira hacia el sur: yaw 180 en un muro norte).
    static void Vitral(Transform padre, Vector3 pos, float yaw)
    {
        var g = Grupo(padre, "Vitral", pos);
        g.localRotation = Quaternion.Euler(0f, yaw, 0f);

        Pieza(g, "Base", PrimitiveType.Cube, new Vector3(0f, 0.2f, 0f), new Vector3(2.7f, 0.4f, 0.8f), "PiedraOscura");
        Pieza(g, "PilarIzq", PrimitiveType.Cube, new Vector3(-1.1f, 2.0f, 0f), new Vector3(0.5f, 4f, 0.7f), "PiedraOscura");
        Pieza(g, "PilarDer", PrimitiveType.Cube, new Vector3(1.1f, 2.0f, 0f), new Vector3(0.5f, 4f, 0.7f), "PiedraOscura");
        Pieza(g, "Dintel1", PrimitiveType.Cube, new Vector3(0f, 4.1f, 0f), new Vector3(2.7f, 0.45f, 0.7f), "PiedraOscura");
        Pieza(g, "Dintel2", PrimitiveType.Cube, new Vector3(0f, 4.5f, 0f), new Vector3(1.9f, 0.4f, 0.7f), "PiedraOscura");
        Pieza(g, "Dintel3", PrimitiveType.Cube, new Vector3(0f, 4.85f, 0f), new Vector3(1.0f, 0.35f, 0.7f), "PiedraOscura");

        string[] cristal = { "VitralRojo", "VitralAzul", "VitralAmarillo", "VitralVerde" };
        int k = 0;
        for (int fila = 0; fila < 4; fila++)
            for (int col = 0; col < 2; col++)
            {
                Pieza(g, "Cristal", PrimitiveType.Cube, new Vector3(col == 0 ? -0.5f : 0.5f, 0.9f + fila * 0.85f, -0.05f), new Vector3(0.9f, 0.8f, 0.1f), cristal[(k + fila) % 4]);
                k++;
            }

        float[] alturas = { 1.2f, 2.2f, 3.1f };
        float[] giros = { -6f, 4f, -3f };
        for (int i = 0; i < 3; i++)
        {
            var t = Pieza(g, "Tabla", PrimitiveType.Cube, new Vector3(0f, alturas[i], 0.18f), new Vector3(2.2f, 0.35f, 0.12f), "MaderaOscura");
            t.localRotation = Quaternion.Euler(0f, 0f, giros[i]);
        }

        Luz(g, new Vector3(0f, 2.2f, 1.5f), new Color(0.6f, 0.7f, 1f), 8f, 2f);

        var bc = g.gameObject.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, 2.2f, 0f);
        bc.size = new Vector3(2.7f, 4.4f, 0.8f);
    }

    // Altar de tres pisos con cempasúchil, velas y papel picado.
    static void Ofrenda(Transform padre, Vector3 pos)
    {
        var g = Grupo(padre, "Ofrenda", pos);
        float[] anchos = { 7.0f, 5.4f, 3.6f };
        float[] fondos = { 3.6f, 2.6f, 1.8f };
        string[] mats = { "MaderaOscura", "PapelMorado", "MaderaClara" };
        for (int i = 0; i < 3; i++)
        {
            float y = 0.35f + i * 0.75f;
            Pieza(g, "Piso" + (i + 1), PrimitiveType.Cube, new Vector3(0f, y, 0f), new Vector3(anchos[i], 0.7f, fondos[i]), mats[i]);
            int n = Mathf.FloorToInt(anchos[i] / 0.5f);
            for (int k = 0; k < n; k++)
                Pieza(g, "Flor", PrimitiveType.Cube, new Vector3(-anchos[i] / 2f + 0.3f + k * 0.5f, y + 0.1f, -fondos[i] / 2f - 0.12f), Vector3.one * 0.3f, k % 2 == 0 ? "Cempasuchil" : "Cempasuchil2");
        }
        foreach (float sx in new[] { -1.2f, 1.2f })
        {
            Pieza(g, "Vela", PrimitiveType.Cylinder, new Vector3(sx, 2.45f, 0f), new Vector3(0.2f, 0.25f, 0.2f), "Cera");
            Pieza(g, "Llama", PrimitiveType.Sphere, new Vector3(sx, 2.82f, 0f), new Vector3(0.16f, 0.26f, 0.16f), "Llama");
            Luz(g, new Vector3(sx, 3.1f, -0.3f), new Color(1f, 0.72f, 0.35f), 7f, 3f);
        }
        // Papel picado colgando de dos postes.
        foreach (float sx in new[] { -3.6f, 3.6f })
            Pieza(g, "Poste", PrimitiveType.Cylinder, new Vector3(sx, 2.3f, 1.0f), new Vector3(0.12f, 2.3f, 0.12f), "MaderaOscura");
        for (int k = 0; k < 10; k++)
            Pieza(g, "Papel", PrimitiveType.Cube, new Vector3(-3.4f + k * 0.76f, 4.2f, 1.0f), new Vector3(0.55f, 0.7f, 0.03f), k % 2 == 0 ? "PapelMorado" : "PapelNaranja");

        var bc = g.gameObject.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, 1.1f, 0f);
        bc.size = new Vector3(7f, 2.2f, 3.6f);
    }

    // Puerta de cempasúchil que lleva al siguiente nivel. Se cruza por el hueco.
    static void Portal(Transform padre, Vector3 pos, float yaw)
    {
        var g = Grupo(padre, "Meta_Siguiente_Nivel", pos);
        g.localRotation = Quaternion.Euler(0f, yaw, 0f);

        foreach (float sx in new[] { -1.7f, 1.7f })
        {
            Pieza(g, "Pilar", PrimitiveType.Cube, new Vector3(sx, 2.1f, 0f), new Vector3(0.8f, 4.2f, 0.8f), "PiedraOscura");
            for (int k = 0; k < 6; k++)
            {
                string m = k % 2 == 0 ? "Cempasuchil" : "Cempasuchil2";
                Pieza(g, "Flor", PrimitiveType.Cube, new Vector3(sx, 0.5f + k * 0.7f, 0.45f), Vector3.one * 0.3f, m);
                Pieza(g, "Flor", PrimitiveType.Cube, new Vector3(sx, 0.5f + k * 0.7f, -0.45f), Vector3.one * 0.3f, m);
            }
        }
        Pieza(g, "Dintel", PrimitiveType.Cube, new Vector3(0f, 4.3f, 0f), new Vector3(4.6f, 0.7f, 0.8f), "PiedraOscura");
        for (int k = 0; k < 7; k++)
        {
            string m = k % 2 == 0 ? "Cempasuchil" : "Cempasuchil2";
            Pieza(g, "Flor", PrimitiveType.Cube, new Vector3(-2.1f + k * 0.7f, 4.75f, 0.3f), Vector3.one * 0.3f, m);
        }
        Pieza(g, "Luz", PrimitiveType.Cube, new Vector3(0f, 2.0f, 0f), new Vector3(2.6f, 3.6f, 0.08f), "Portal");
        Luz(g, new Vector3(0f, 2.0f, 1.8f), new Color(1f, 0.85f, 0.6f), 9f, 3f);

        foreach (float sx in new[] { -1.7f, 1.7f })
        {
            var bc = g.gameObject.AddComponent<BoxCollider>();
            bc.center = new Vector3(sx, 2.1f, 0f);
            bc.size = new Vector3(0.8f, 4.2f, 0.8f);
        }
    }

    // ---------------- Panteón ----------------

    static void Lapida(Transform padre, Vector3 pos, int semilla)
    {
        var r = new System.Random(semilla);
        var g = Grupo(padre, "Lapida", pos);
        g.localRotation = Quaternion.Euler(0f, Rf(r, -10f, 10f), 0f);
        float alto = Rf(r, 1.1f, 1.7f);

        Pieza(g, "Base", PrimitiveType.Cube, new Vector3(0f, 0.15f, 0f), new Vector3(0.95f, 0.3f, 0.6f), "PiedraOscura");
        Pieza(g, "Losa", PrimitiveType.Cube, new Vector3(0f, 0.3f + alto / 2f, 0f), new Vector3(0.75f, alto, 0.22f), "Piedra");
        Pieza(g, "Remate", PrimitiveType.Cube, new Vector3(0f, 0.3f + alto + 0.08f, 0f), new Vector3(0.55f, 0.16f, 0.22f), "Piedra");
        if (r.NextDouble() < 0.35)
        {
            Pieza(g, "CruzV", PrimitiveType.Cube, new Vector3(0f, 0.3f + alto + 0.55f, 0f), new Vector3(0.12f, 0.9f, 0.12f), "Piedra");
            Pieza(g, "CruzH", PrimitiveType.Cube, new Vector3(0f, 0.3f + alto + 0.75f, 0f), new Vector3(0.5f, 0.12f, 0.12f), "Piedra");
        }
        if (r.NextDouble() < 0.5)
            Pieza(g, "Musgo", PrimitiveType.Cube, new Vector3(0f, 0.3f + alto * 0.2f, 0.12f), new Vector3(0.6f, alto * 0.35f, 0.04f), "Musgo");

        var bc = g.gameObject.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, (0.3f + alto) / 2f, 0f);
        bc.size = new Vector3(0.95f, 0.3f + alto, 0.6f);
    }

    static void Veladora(Transform padre, Vector3 pos, int semilla, bool conLuz)
    {
        var g = Grupo(padre, "Veladora", pos);
        string vidrio = semilla % 3 == 0 ? "VidrioRojo" : (semilla % 3 == 1 ? "VidrioAzul" : "Cempasuchil2");
        Pieza(g, "Vaso", PrimitiveType.Cylinder, new Vector3(0f, 0.3f, 0f), new Vector3(0.38f, 0.3f, 0.38f), vidrio);
        Pieza(g, "Llama", PrimitiveType.Sphere, new Vector3(0f, 0.72f, 0f), new Vector3(0.12f, 0.2f, 0.12f), "Llama");
        if (conLuz) Luz(g, new Vector3(0f, 0.95f, 0f), new Color(1f, 0.7f, 0.35f), 5f, 1.6f);
    }

    static void TroncoCaido(Transform padre, Vector3 centro, int ancho, int fondo)
    {
        var g = Grupo(padre, "TroncoCaido", centro);
        bool largoX = ancho >= fondo;
        float L = largoX ? ancho : fondo;
        if (!largoX) g.localRotation = Quaternion.Euler(0f, 90f, 0f);

        var t = Pieza(g, "Tronco", PrimitiveType.Cylinder, new Vector3(0f, 0.68f, 0f), new Vector3(1.3f, L / 2f, 1.3f), "Corteza");
        t.localRotation = Quaternion.Euler(0f, 0f, 90f);
        Pieza(g, "Musgo", PrimitiveType.Cube, new Vector3(0f, 1.3f, 0f), new Vector3(L * 0.7f, 0.1f, 0.6f), "Musgo");
        var rama = Pieza(g, "Rama", PrimitiveType.Cube, new Vector3(-L * 0.25f, 1.1f, 0.65f), new Vector3(0.18f, 0.18f, 0.9f), "CortezaClara");
        rama.localRotation = Quaternion.Euler(-20f, 0f, 0f);

        var bc = g.gameObject.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, 0.7f, 0f);
        bc.size = new Vector3(L, 1.4f, 1.3f);
    }

    static void Marcador(Transform padre, string nombre, Vector3 pos)
    {
        var g = Grupo(padre, nombre, pos);
        Pieza(g, "Marcador", PrimitiveType.Sphere, new Vector3(0f, 0.4f, 0f), Vector3.one * 0.8f, "MarcadorInicio");
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

    static Light Luz(Transform padre, Vector3 pos, Color color, float alcance, float intensidad)
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
        return new Vector3(x - w / 2f + 0.5f, 0f, y - h / 2f + 0.5f);
    }

    static float Rf(System.Random r, float a, float b)
    {
        return a + (float)r.NextDouble() * (b - a);
    }

    // Dirección horizontal para un ángulo en grados (0 = +Z).
    static Vector3 Dir(float grados)
    {
        float a = grados * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
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

    static List<CombineInstance> Lista(Dictionary<string, List<CombineInstance>> d, string clave)
    {
        if (!d.TryGetValue(clave, out var l))
        {
            l = new List<CombineInstance>();
            d[clave] = l;
        }
        return l;
    }

    static void SumarTRS(List<CombineInstance> lista, Mesh m, Vector3 pos, Quaternion rot, Vector3 esc)
    {
        var ci = new CombineInstance();
        ci.mesh = m;
        ci.transform = Matrix4x4.TRS(pos, rot, esc);
        lista.Add(ci);
    }

    static void Publicar(Transform raiz, Dictionary<string, List<CombineInstance>> d, string prefijo)
    {
        foreach (var par in d)
        {
            var go = new GameObject(prefijo + par.Key);
            go.transform.SetParent(raiz, false);
            go.AddComponent<MeshFilter>().sharedMesh = GuardarMalla(raiz.name + "_" + prefijo + par.Key, par.Value);
            go.AddComponent<MeshRenderer>().sharedMaterial = M(par.Key);
            go.isStatic = true;
        }
    }

    static Mesh GuardarMalla(string nombre, List<CombineInstance> lista)
    {
        var malla = new Mesh();
        malla.name = "Tutorial_" + nombre;
        malla.indexFormat = IndexFormat.UInt32;
        malla.CombineMeshes(lista.ToArray(), true, true);
        string ruta = "Assets/Niveles/Tutorial/Malla_" + nombre + ".asset";
        AssetDatabase.DeleteAsset(ruta);
        AssetDatabase.CreateAsset(malla, ruta);
        return malla;
    }

    static Material M(string nombre)
    {
        Material hecho;
        if (cache.TryGetValue(nombre, out hecho)) return hecho;

        string ruta = "Assets/Niveles/Tutorial/Mat_" + nombre + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            m = new Material(sh);
            AssetDatabase.CreateAsset(m, ruta);
        }
        m.color = colores[nombre];
        float fuerza;
        if (brillo.TryGetValue(nombre, out fuerza))
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", colores[nombre] * fuerza);
        }
        EditorUtility.SetDirty(m);
        cache[nombre] = m;
        return m;
    }

    // Atardecer en el panteón. Sin niebla para que los textos se lean bien.
    static void ConfigurarEscena(bool sala)
    {
        Color fondo = new Color(0.05f, 0.03f, 0.09f);

        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.26f, 0.22f, 0.34f);
        RenderSettings.fog = false;

        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type != LightType.Directional) continue;
            l.intensity = 0.8f;
            l.color = new Color(0.78f, 0.80f, 1f);
            l.shadows = LightShadows.None;
        }

        var cam = Camera.main;
        if (cam == null) return;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = fondo;
        cam.transform.rotation = Quaternion.Euler(65f, 0f, 0f);

        if (sala)
        {
            cam.transform.position = new Vector3(0f, 52f, -20f);   // ve toda la sala
        }
        else
        {
            var inicio = GameObject.Find("Inicio_Jugador");
            Vector3 b = inicio != null ? inicio.transform.position : Vector3.zero;
            cam.transform.position = b + new Vector3(0f, 26f, -12f);  // empieza sobre Iktan
        }
    }
}