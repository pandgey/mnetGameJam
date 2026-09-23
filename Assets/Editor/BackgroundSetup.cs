using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// Adds the indoor backdrop to the open scene and sizes it to the level. Done from
// the editor rather than hand-written into the scene asset, because the editor holds
// the open scene in memory and would overwrite any external edit on its next save.
public static class BackgroundSetup {

    private const string ObjectName = "Background_Indoor";
    private const string TexturePath = "Assets/Art2D/Backgrounds/Background_Indoor.png";
    private const int SortingOrder = -100;

    // Close to 1 so the backdrop reads as distant without needing to be enormous.
    private const float DefaultFollow = 0.9f;

    // Wider screens than this would show the backdrop's edges.
    private const float MinAspect = 16f / 9f;
    private const float Margin = 1.05f;

    [MenuItem ("Tools/Art/Add or Refit Indoor Background")]
    private static void AddOrRefit () {
        Sprite sprite = LoadSprite ();
        if (sprite == null) {
            Debug.LogError ($"BackgroundSetup: no sprite found at {TexturePath}.");
            return;
        }

        Camera camera = Camera.main;
        if (camera == null || !camera.orthographic) {
            Debug.LogError ("BackgroundSetup: the scene needs an orthographic Main Camera.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene ();
        GameObject background = scene.GetRootGameObjects ().FirstOrDefault (go => go.name == ObjectName);

        if (background == null) {
            background = new GameObject (ObjectName);
            Undo.RegisterCreatedObjectUndo (background, "Add Indoor Background");
        }

        SpriteRenderer spriteRenderer = background.GetComponent<SpriteRenderer> ();
        if (spriteRenderer == null)
            spriteRenderer = Undo.AddComponent<SpriteRenderer> (background);

        ParallaxLayer parallax = background.GetComponent<ParallaxLayer> ();
        bool newParallax = parallax == null;
        if (newParallax)
            parallax = Undo.AddComponent<ParallaxLayer> (background);

        // Refitting keeps whatever follow values were tuned in the Inspector.
        SerializedObject parallaxData = new SerializedObject (parallax);
        SerializedProperty followX = parallaxData.FindProperty ("followX");
        SerializedProperty followY = parallaxData.FindProperty ("followY");
        if (newParallax) {
            followX.floatValue = DefaultFollow;
            followY.floatValue = DefaultFollow;
            parallaxData.ApplyModifiedProperties ();
        }

        Undo.RecordObject (spriteRenderer, "Fit Indoor Background");
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingOrder = SortingOrder;

        Bounds level = LevelBounds (scene, background);
        Vector2 cameraStart = CameraStart (camera);
        Vector2 viewHalf = new Vector2 (camera.orthographicSize * Mathf.Max (camera.aspect, MinAspect), camera.orthographicSize);

        float x = FitAxis (level.min.x, level.max.x, viewHalf.x, cameraStart.x, followX.floatValue, out float halfWidth);
        float y = FitAxis (level.min.y, level.max.y, viewHalf.y, cameraStart.y, followY.floatValue, out float halfHeight);

        // One uniform scale so the art is never stretched out of proportion.
        Vector2 spriteSize = sprite.bounds.size;
        float scale = Mathf.Max (2f * halfWidth / spriteSize.x, 2f * halfHeight / spriteSize.y) * Margin;

        Undo.RecordObject (background.transform, "Fit Indoor Background");
        background.transform.position = new Vector3 (x, y, 0f);
        background.transform.localScale = new Vector3 (scale, scale, 1f);

        EditorSceneManager.MarkSceneDirty (scene);
        Selection.activeGameObject = background;
        Debug.Log ($"BackgroundSetup: fitted {ObjectName} to level bounds {level.min:F1} to {level.max:F1} at scale {scale:F2}.");
    }

    // The layer moves by follow * camera travel, so it only has to cover the remaining
    // (1 - follow) of that travel. Placing it at this start point centres that slack.
    private static float FitAxis (float levelMin, float levelMax, float viewHalf, float cameraStart, float follow, out float half) {
        // CameraFollow does not clamp to the level, so the camera can reach wherever the player can.
        float travelHalf = (levelMax - levelMin) * 0.5f;
        float travelMid = (levelMin + levelMax) * 0.5f;

        half = viewHalf + (1f - follow) * travelHalf;
        return follow * cameraStart + (1f - follow) * travelMid;
    }

    private static Vector2 CameraStart (Camera camera) {
        // CameraFollow snaps onto the player in Start, so that is where parallax begins.
        PlayerMovement player = Object.FindFirstObjectByType<PlayerMovement> ();
        return player != null ? (Vector2) player.transform.position : (Vector2) camera.transform.position;
    }

    private static Bounds LevelBounds (Scene scene, GameObject background) {
        Bounds bounds = new Bounds ();
        bool any = false;

        foreach (GameObject root in scene.GetRootGameObjects ()) {
            if (root == background)
                continue;

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer> ()) {
                if (!renderer.enabled || !(renderer is SpriteRenderer || renderer is TilemapRenderer))
                    continue;

                if (any) {
                    bounds.Encapsulate (renderer.bounds);
                } else {
                    bounds = renderer.bounds;
                    any = true;
                }
            }
        }

        return any ? bounds : new Bounds (Vector3.zero, Vector3.one * 10f);
    }

    private static Sprite LoadSprite () {
        TextureImporter importer = AssetImporter.GetAtPath (TexturePath) as TextureImporter;
        if (importer == null)
            return null;

        // Painted pixel art turns blurry under bilinear filtering and compression.
        if (importer.filterMode != FilterMode.Point || importer.textureCompression != TextureImporterCompression.Uncompressed) {
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport ();
        }

        return AssetDatabase.LoadAllAssetsAtPath (TexturePath).OfType<Sprite> ().FirstOrDefault ();
    }
}
