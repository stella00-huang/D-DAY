using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class DDayBuildSetup
{
    const string ScenePath = "Assets/Scenes/TodayWhereToSit.unity";

    [MenuItem("D-DAY/Set Demo As Startup Scene")]
    public static void SetStartupScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
        {
            Debug.LogError("D-DAY scene missing: " + ScenePath);
            return;
        }

        var scenes = new List<EditorBuildSettingsScene>();
        foreach (var scene in EditorBuildSettings.scenes)
            if (scene.path != ScenePath) scenes.Add(scene);

        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("D-DAY: startup scene set to " + ScenePath + "; existing scenes preserved.");
    }
}
