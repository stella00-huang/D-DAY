#if UNITY_EDITOR
using System;
using System.Collections;
using UnityEngine;

// Temporary Play Mode capture driver; never saved into the scene or shipped.
public static class DDayPVFramePump
{
    public static IEnumerator AfterRender(Action frame)
    {
        yield return new WaitForEndOfFrame(); frame();
    }
}
#endif
