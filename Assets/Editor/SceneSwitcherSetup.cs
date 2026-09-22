using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Keeps the SceneSwitcher doorway wired up. The components are applied in the
// editor rather than hand-written into the scene asset, because the editor holds
// the open scene in memory and would overwrite any external edit on its next save.
[InitializeOnLoad]
public static class SceneSwitcherSetup {

    private const string ObjectName = "SceneSwitcher";
    private const string DestinationScene = "HumanErrorReport";

    // The level order the game plays in; also the order they are listed in the build.
    private static readonly string[] PlayOrder = {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/Lab.unity",
        "Assets/Scenes/HumanErrorReport.unity",
        "Assets/Scenes/Rooftop.unity",
    };

    static SceneSwitcherSetup () {
        // Whenever the doorway could matter: on load, when its scene is opened,
        // and before play mode starts, so a forgotten setup step cannot break the exit.
        EditorApplication.delayCall += WireOpenScenes;
        EditorApplication.delayCall += EnsureBuildSceneList;
        EditorSceneManager.sceneOpened += (scene, mode) => Wire (scene, false);
        EditorApplication.playModeStateChanged += state => {
            if (state == PlayModeStateChange.ExitingEditMode)
                WireOpenScenes ();
        };
    }

    [MenuItem ("Tools/Wire Scene Switcher")]
    private static void WireAndSave () {
        WireOpenScenes (true);
        FixBuildSceneList ();
    }

    private static void WireOpenScenes () {
        WireOpenScenes (false);
    }

    private static void WireOpenScenes (bool save) {
        bool found = false;
        for (int i = 0; i < SceneManager.sceneCount; i++)
            found |= Wire (SceneManager.GetSceneAt (i), save);

        if (save && !found)
            EditorUtility.DisplayDialog ("Scene Switcher",
                $"No object named '{ObjectName}' in the open scene. Open Lab and try again.", "OK");
    }

    private static bool Wire (Scene scene, bool save) {
        GameObject switcher = FindInScene (scene, ObjectName);
        if (switcher == null)
            return false;

        // Already wired: leave the scene alone so it is not marked dirty on every reload.
        SceneTrigger existing = switcher.GetComponent<SceneTrigger> ();
        BoxCollider2D existingBox = switcher.GetComponent<BoxCollider2D> ();
        bool complete = existing != null && existingBox != null && existingBox.isTrigger;

        BoxCollider2D box = existingBox != null ? existingBox : Undo.AddComponent<BoxCollider2D> (switcher);
        box.isTrigger = true;

        // Match the doorway sprite so the walkable area lines up with what is drawn.
        SpriteRenderer renderer = switcher.GetComponent<SpriteRenderer> ();
        if (!complete && renderer != null && renderer.sprite != null) {
            Bounds bounds = renderer.sprite.bounds;
            box.offset = bounds.center;
            box.size = bounds.size;
        }

        SceneTrigger trigger = existing != null ? existing : Undo.AddComponent<SceneTrigger> (switcher);

        // sceneName and friends are private [SerializeField]s, so go through the serialized object.
        SerializedObject so = new SerializedObject (trigger);
        SerializedProperty sceneName = so.FindProperty ("sceneName");
        if (!complete || string.IsNullOrWhiteSpace (sceneName.stringValue)) {
            sceneName.stringValue = DestinationScene;
            so.FindProperty ("requireKeyPress").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo ();
        }

        if (!complete) {
            EditorSceneManager.MarkSceneDirty (scene);
            Debug.Log ($"SceneSwitcher wired in '{scene.name}': walking into it loads '{sceneName.stringValue}'.", switcher);
        }

        if (save && scene.isDirty)
            EditorSceneManager.SaveScene (scene);

        return true;
    }

    private static GameObject FindInScene (Scene scene, string objectName) {
        if (!scene.isLoaded)
            return null;

        foreach (GameObject root in scene.GetRootGameObjects ()) {
            if (root.name == objectName)
                return root;

            Transform child = root.transform.Find (objectName);
            if (child != null)
                return child.gameObject;
        }
        return null;
    }

    // A trigger can only load a scene that is in the build list, so repair it
    // rather than let the doorway fail at runtime after a rename.
    private static void EnsureBuildSceneList () {
        EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
        List<string> wanted = new List<string> ();
        foreach (string path in PlayOrder)
            if (File.Exists (path))
                wanted.Add (path);

        bool matches = current.Length == wanted.Count;
        for (int i = 0; matches && i < current.Length; i++)
            matches = current[i].enabled && current[i].path == wanted[i];

        if (!matches)
            FixBuildSceneList ();
    }

    // LoadScene only reaches scenes that are in the build list, and the list still
    // holds pre-rename paths, so rebuild it from the play order above.
    [MenuItem ("Tools/Fix Build Scene List")]
    private static void FixBuildSceneList () {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene> ();
        foreach (string path in PlayOrder) {
            if (!File.Exists (path)) {
                Debug.LogWarning ($"Build list: '{path}' does not exist, skipping.");
                continue;
            }
            scenes.Add (new EditorBuildSettingsScene (path, true));
        }

        EditorBuildSettings.scenes = scenes.ToArray ();
        Debug.Log ($"Build list rebuilt: {string.Join (", ", scenes.ConvertAll (s => Path.GetFileNameWithoutExtension (s.path)))}.");
    }
}
