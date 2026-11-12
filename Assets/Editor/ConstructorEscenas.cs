// Saatan Historia - Crea las escenas del juego y las agrega al Build.
// Colocar en: Assets/Editor/ConstructorEscenas.cs
// Requiere: Assets/Scripts/CambiarEscena.cs, ConstructorSelva.cs y HerramientasIktan.cs
//
// Menú: Saatan > Escenas > Crear menú principal y nivel Selva
//   - Assets/Scenes/MenuPrincipal.unity : título + botón "Jugar".
//   - Assets/Scenes/NivelSelva.unity    : el laberinto de la selva (selva_laberinto.png)
//                                          con el jugador de prueba (Iktan).
//   - Las agrega a la lista de escenas del Build (el menú queda en el índice 0).
//
// El botón "Jugar" llama a CambiarEscena.CargarEscena("NivelSelva") desde su
// evento On Click (), tal como se configura a mano en el video de apoyo.
//
// Menú: Saatan > Escenas > Reconstruir NivelSelva
//   Abre NivelSelva.unity, vuelve a construir el laberinto, coloca a Iktan y guarda.

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ConstructorEscenas
{
    public const string CARPETA = "Assets/Scenes";
    public const string ESCENA_MENU = "MenuPrincipal";
    public const string ESCENA_SELVA = "NivelSelva";
    const string MAPA_SELVA = ConstructorSelva.MAPA;

    static string Ruta(string nombre) => CARPETA + "/" + nombre + ".unity";

    [MenuItem("Saatan/Escenas/Crear menú principal y nivel Selva")]
    static void CrearTodo()
    {
        if (!Preparar()) return;

        bool selva = CrearNivelSelva();
        CrearMenuPrincipal();
        AgregarAlBuild();

        // Deja abierto el menú, listo para darle Play.
        EditorSceneManager.OpenScene(Ruta(ESCENA_MENU), OpenSceneMode.Single);

        EditorUtility.DisplayDialog(
            "Saatan",
            "Escenas creadas en " + CARPETA + ":\n\n" +
            "  0. " + ESCENA_MENU + "\n" +
            "  1. " + ESCENA_SELVA + (selva ? "" : "  (vacía: no se encontró " + MAPA_SELVA + ")") + "\n\n" +
            "Pulsa Play y haz clic en \"Jugar\" para pasar a la selva.",
            "OK"
        );
    }

    [MenuItem("Saatan/Escenas/Crear solo el menú principal")]
    static void SoloMenu()
    {
        if (!Preparar()) return;
        CrearMenuPrincipal();
        AgregarAlBuild();
    }

    [MenuItem("Saatan/Escenas/Crear solo el nivel Selva")]
    static void SoloSelva()
    {
        if (!Preparar()) return;
        CrearNivelSelva();
        AgregarAlBuild();
    }

    [MenuItem("Saatan/Escenas/Reconstruir NivelSelva")]
    static void ReconstruirSelva()
    {
        if (!Preparar()) return;

        var mapa = AssetDatabase.LoadAssetAtPath<Texture2D>(MAPA_SELVA);
        if (mapa == null)
        {
            EditorUtility.DisplayDialog("Saatan", "No se encontró " + MAPA_SELVA + ".", "OK");
            return;
        }

        Scene escena = AssetDatabase.LoadAssetAtPath<SceneAsset>(Ruta(ESCENA_SELVA)) != null
            ? EditorSceneManager.OpenScene(Ruta(ESCENA_SELVA), OpenSceneMode.Single)
            : EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        ConstructorSelva.ConstruirDesdeTextura(mapa);
        HerramientasIktan.CrearJugador();

        EditorSceneManager.SaveScene(escena, Ruta(ESCENA_SELVA));
        AgregarAlBuild();
        Debug.Log("NivelSelva reconstruido y guardado.");
    }

    [MenuItem("Saatan/Escenas/Agregar escenas al Build")]
    static void AgregarAlBuild()
    {
        var lista = new List<EditorBuildSettingsScene>();
        var vistas = new HashSet<string>();

        // Primero el menú y la selva, en ese orden; después las que ya existían.
        foreach (string ruta in new[] { Ruta(ESCENA_MENU), Ruta(ESCENA_SELVA) })
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ruta) == null) continue;
            lista.Add(new EditorBuildSettingsScene(ruta, true));
            vistas.Add(ruta);
        }

        foreach (var s in EditorBuildSettings.scenes)
        {
            if (string.IsNullOrEmpty(s.path) || vistas.Contains(s.path)) continue;
            lista.Add(s);
            vistas.Add(s.path);
        }

        EditorBuildSettings.scenes = lista.ToArray();
        Debug.Log("Escenas en el Build: " + string.Join(", ", vistas));
    }

    static bool Preparar()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Saatan", "Sal del modo Play antes de crear las escenas.", "OK");
            return false;
        }

        // Pregunta si quieres guardar la escena abierta antes de cambiarla.
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;

        if (!AssetDatabase.IsValidFolder(CARPETA))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        return true;
    }

    static bool Sobrescribir(string nombre)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Ruta(nombre)) == null) return true;
        return EditorUtility.DisplayDialog(
            "Saatan",
            "La escena " + nombre + " ya existe. ¿Quieres reemplazarla?",
            "Reemplazar", "Conservar"
        );
    }

    // ---------------- Menú principal ----------------

    static void CrearMenuPrincipal()
    {
        if (!Sobrescribir(ESCENA_MENU)) return;

        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Color fondo = new Color(0.06f, 0.12f, 0.08f);
        Color dorado = new Color(1.00f, 0.78f, 0.30f);

        // Cámara.
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = fondo;
        camGo.AddComponent<AudioListener>();
        camGo.transform.position = new Vector3(0f, 1f, -10f);

        // Objeto que hace el cambio de escena.
        var gestor = new GameObject("GestorEscenas").AddComponent<CambiarEscena>();

        // EventSystem (con el módulo del Input System nuevo, que es el activo en el proyecto).
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();

        // Canvas.
        var canvasGo = new GameObject("Canvas", typeof(RectTransform));
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var escalador = canvasGo.AddComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        Font fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Título.
        var titulo = Texto(canvasGo.transform, "Titulo", "SAATAN", fuente, 160, dorado);
        Colocar(titulo.rectTransform, new Vector2(0.5f, 0.68f), new Vector2(1400f, 240f));
        titulo.fontStyle = FontStyle.Bold;
        var sombra = titulo.gameObject.AddComponent<Shadow>();
        sombra.effectColor = new Color(0f, 0f, 0f, 0.8f);
        sombra.effectDistance = new Vector2(6f, -6f);

        var sub = Texto(canvasGo.transform, "Subtitulo", "La historia de Iktan", fuente, 48,
            new Color(0.85f, 0.92f, 0.85f));
        Colocar(sub.rectTransform, new Vector2(0.5f, 0.55f), new Vector2(1400f, 80f));

        // Botón "Jugar".
        var boton = Boton(canvasGo.transform, "Boton_Jugar", "Jugar", fuente);
        Colocar((RectTransform)boton.transform, new Vector2(0.5f, 0.35f), new Vector2(420f, 110f));

        // On Click () -> GestorEscenas.CambiarEscena.CargarEscena("NivelSelva")
        UnityEventTools.AddStringPersistentListener(
            boton.onClick,
            new UnityAction<string>(gestor.CargarEscena),
            ESCENA_SELVA
        );

        // Para que el botón también se pueda usar con teclado o control.
        es.GetComponent<EventSystem>().firstSelectedGameObject = boton.gameObject;

        EditorSceneManager.SaveScene(escena, Ruta(ESCENA_MENU));
        Debug.Log("Menú principal creado: " + Ruta(ESCENA_MENU));
    }

    static Text Texto(Transform padre, string nombre, string contenido, Font fuente, int tam, Color color)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        var t = go.AddComponent<Text>();
        t.text = contenido;
        t.font = fuente;
        t.fontSize = tam;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static Button Boton(Transform padre, string nombre, string etiqueta, Font fuente)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);

        var img = go.AddComponent<Image>();
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type = Image.Type.Sliced;
        img.color = Color.white;

        var b = go.AddComponent<Button>();
        b.targetGraphic = img;
        var colores = b.colors;
        colores.normalColor = new Color(0.55f, 0.32f, 0.12f);
        colores.highlightedColor = new Color(0.75f, 0.47f, 0.18f);
        colores.selectedColor = new Color(0.75f, 0.47f, 0.18f);
        colores.pressedColor = new Color(0.38f, 0.22f, 0.08f);
        b.colors = colores;

        var t = Texto(go.transform, "Texto", etiqueta, fuente, 56, new Color(1f, 0.95f, 0.85f));
        var rt = t.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        return b;
    }

    static void Colocar(RectTransform rt, Vector2 ancla, Vector2 tam)
    {
        rt.anchorMin = ancla;
        rt.anchorMax = ancla;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = tam;
    }

    // ---------------- Nivel I: Selva ----------------

    static bool CrearNivelSelva()
    {
        if (!Sobrescribir(ESCENA_SELVA)) return true;

        // Cámara + luz direccional por defecto (ConstructorSelva las ajusta).
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var mapa = AssetDatabase.LoadAssetAtPath<Texture2D>(MAPA_SELVA);
        bool ok = mapa != null;

        if (ok)
        {
            ConstructorSelva.ConstruirDesdeTextura(mapa);
            HerramientasIktan.CrearJugador();
        }
        else
        {
            Debug.LogWarning("No se encontró " + MAPA_SELVA + ". La escena " + ESCENA_SELVA +
                             " se creó vacía; usa Saatan > Construir selva (Laberinto) dentro de ella.");
        }

        EditorSceneManager.SaveScene(escena, Ruta(ESCENA_SELVA));
        Debug.Log("Nivel Selva creado: " + Ruta(ESCENA_SELVA));
        return ok;
    }
}
