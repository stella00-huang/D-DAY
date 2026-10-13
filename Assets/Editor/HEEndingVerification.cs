using UnityEditor;

// Preserve the existing debug menu; the current formal narration supersedes the 32-second prototype test.
public static class HEEndingVerification
{
    [MenuItem("D-DAY/HE/Verify Ending in Play Mode")]
    public static void Start() => DDayDualEndingVerification.Start();
}
