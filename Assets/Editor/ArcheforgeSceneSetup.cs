using Archeforge.UnityPort;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ArcheforgeSceneSetup
{
    private const string ScenePath = "Assets/Scenes/MainScene.unity";

    [MenuItem("Archeforge/Setup Main Scene")]
    public static void SetupMainScene()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        SetupCamera();
        SetupCanvas();
        SetupEventSystem();
        SetupPrototypeRoot();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeObject = FindOrCreate("ArcheforgePrototype");

        Debug.Log("[Archeforge] MainScene configured for 2D prototype.");
    }

    private static void SetupCamera()
    {
        GameObject cameraObject = Camera.main != null
            ? Camera.main.gameObject
            : FindOrCreate("Main Camera");

        cameraObject.tag = "MainCamera";

        Camera camera = cameraObject.GetComponent<Camera>();
        if (camera == null)
        {
            camera = cameraObject.AddComponent<Camera>();
        }

        if (cameraObject.GetComponent<AudioListener>() == null)
        {
            cameraObject.AddComponent<AudioListener>();
        }

        camera.orthographic = true;
        camera.orthographicSize = 7.5f;
        camera.backgroundColor = new Color(0.17f, 0.24f, 0.31f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.transform.rotation = Quaternion.identity;
    }

    private static void SetupCanvas()
    {
        GameObject canvasObject = FindOrCreate("Canvas");

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = canvasObject.AddComponent<Canvas>();
        }
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvasObject.AddComponent<CanvasScaler>();
        }
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (canvasObject.GetComponent<GraphicRaycaster>() == null)
        {
            canvasObject.AddComponent<GraphicRaycaster>();
        }
    }

    private static void SetupEventSystem()
    {
        GameObject eventSystemObject = FindOrCreate("EventSystem");

        if (eventSystemObject.GetComponent<EventSystem>() == null)
        {
            eventSystemObject.AddComponent<EventSystem>();
        }

        if (eventSystemObject.GetComponent<StandaloneInputModule>() == null)
        {
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }
    }

    private static void SetupPrototypeRoot()
    {
        GameObject root = FindOrCreate("ArcheforgePrototype");
        if (root.GetComponent<ArcheforgePrototypeController>() == null)
        {
            root.AddComponent<ArcheforgePrototypeController>();
        }
    }

    private static GameObject FindOrCreate(string objectName)
    {
        GameObject existing = GameObject.Find(objectName);
        if (existing != null)
        {
            return existing;
        }

        GameObject created = new GameObject(objectName);
        Undo.RegisterCreatedObjectUndo(created, $"Create {objectName}");
        return created;
    }
}
