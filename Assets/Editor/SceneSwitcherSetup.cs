using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// One-click wiring for the SceneSwitcher doorway object, so the scene asset
// does not have to be hand-edited while the editor has it open.
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

    [MenuItem ("Tools/Wire Scene Switcher")]
    private static void Wire () {
        GameObject switcher = GameObject.Find (ObjectName);
        if (switcher == null) {
            EditorUtility.DisplayDialog ("Scene Switcher",
                $"No object named '{ObjectName}' in the open scene. Open Lab and try again.", "OK");
            return;
        }

        Undo.SetCurrentGroupName ("Wire Scene Switcher");
        int group = Undo.GetCurrentGroup ();

        BoxCollider2D box = switcher.GetComponent<BoxCollider2D> ();
        if (box == null)
            box = Undo.AddComponent<BoxCollider2D> (switcher);

        Undo.RecordObject (box, "Wire Scene Switcher");
        box.isTrigger = true;

        // Match the doorway sprite so the walkable area lines up with what is drawn.
        SpriteRenderer renderer = switcher.GetComponent<SpriteRenderer> ();
        if (renderer != null && renderer.sprite != null) {
            Bounds bounds = renderer.sprite.bounds;
            box.offset = bounds.center;
            box.size = bounds.size;
        }

        SceneTrigger trigger = switcher.GetComponent<SceneTrigger> ();
        if (trigger == null)
            trigger = Undo.AddComponent<SceneTrigger> (switcher);

        // sceneName and friends are private [SerializeField]s, so go through the serialized object.
        SerializedObject so = new SerializedObject (trigger);
        so.FindProperty ("sceneName").stringValue = DestinationScene;
        so.FindProperty ("requireKeyPress").boolValue = false;
        so.ApplyModifiedProperties ();

        Undo.CollapseUndoOperations (group);
        EditorSceneManager.MarkSceneDirty (switcher.scene);
        EditorSceneManager.SaveScene (switcher.scene);

        FixBuildSceneList ();

        Debug.Log ($"SceneSwitcher wired: walking into it loads '{DestinationScene}'.", switcher);
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
