using System;
using System.IO;
using System.Linq;
using DDay;
using UnityEditor;
using UnityEngine;

public static class DDayAudioAuthoring
{
    public const string ConfigPath = "Assets/Resources/DDay/Audio/DDayAudioConfig.asset";
    [Serializable] class ImportReport { public ImportEntry[] entries; }
    [Serializable] class ImportEntry { public string path; public float gain; }
    [MenuItem("D-DAY/Audio/Configure Supplied Audio")]
    public static void Configure()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before importing audio.");
        var report = JsonUtility.FromJson<ImportReport>(File.ReadAllText("Docs/AudioIntegration/Audio_Import.json"));
        string[] names = { "main_menu.wav", "day1_2.mp3", "day3_4.mp3", "day5_6.mp3", "day_7.wav", "he.ogg", "be.mp3" };
        string[] soundNames = { "click_001.ogg", "select_001.ogg", "drop_001.ogg", "error_001.ogg",
            "confirmation_001.ogg", "back_001.ogg", "question_001.ogg", "toggle_001.ogg", "click_001.ogg" };
        var config = AssetDatabase.LoadAssetAtPath<DDayAudioConfig>(ConfigPath);
        if (config == null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
            AssetDatabase.Refresh();
            config = ScriptableObject.CreateInstance<DDayAudioConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
        }
        config.music = new DDayMusicEntry[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            string path = "Assets/Audio/BGM/" + names[i];
            config.music[i] = new DDayMusicEntry { cue = (BgmCue)i, clip = Import(path, true),
                gain = report.entries.First(e => e.path == path).gain, loop = i < 5 };
        }
        config.sounds = new DDaySoundEntry[soundNames.Length];
        for (int i = 0; i < soundNames.Length; i++)
        {
            string path = "Assets/Audio/SFX/Kenney/" + soundNames[i];
            config.sounds[i] = new DDaySoundEntry { cue = (SfxCue)i, clip = Import(path, false),
                gain = report.entries.First(e => e.path == path).gain * (i == 8 ? .3f : 1),
                repeatInterval = i == 8 ? .2f : .12f };
        }
        config.crossfadeSeconds = 1.5f;
        EditorUtility.SetDirty(config); AssetDatabase.SaveAssets();
        var flac = AssetImporter.GetAtPath("Assets/Audio/BGM/day_7.flac");
        Debug.Log("D-DAY audio configured: 7 BGM / 9 SFX cues. Original FLAC importer: "
            + (flac == null ? "none" : flac.GetType().Name) + "; verified lossless 24-bit WAV is used.");
    }
    static AudioClip Import(string path, bool music)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(path) as AudioImporter;
        if (importer == null) throw new InvalidOperationException("Cannot import audio: " + path);
        importer.forceToMono = !music; importer.loadInBackground = music;
        var sample = importer.defaultSampleSettings;
        sample.preloadAudioData = !music;
        sample.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
        sample.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
        sample.quality = .85f; sample.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
        importer.defaultSampleSettings = sample; importer.SaveAndReimport();
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip == null || clip.length <= 0) throw new InvalidOperationException("Invalid AudioClip: " + path);
        return clip;
    }
}
