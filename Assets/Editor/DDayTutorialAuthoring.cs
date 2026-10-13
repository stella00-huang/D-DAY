using DDay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DDayTutorialAuthoring
{
    [MenuItem("D-DAY/Tutorial/Bind Day1 Tutorial")]
    public static void Bind()
    {
        if (EditorApplication.isPlaying) return;
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/TodayWhereToSit.unity")
        { Debug.LogWarning("Open TodayWhereToSit before binding the Day1 tutorial."); return; }
        var game = Object.FindFirstObjectByType<DDayGame>();
        if (game == null || Resources.Load<TextAsset>("DDay/Tutorial/Day1Tutorial") == null)
        { Debug.LogError("Day1 tutorial source or campaign object missing."); return; }
        if (game.GetComponent<DDayTutorialController>() == null) Undo.AddComponent<DDayTutorialController>(game.gameObject);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Debug.Log("Day1 tutorial bound to existing D-DAY campaign; existing scene preserved.");
    }
}
