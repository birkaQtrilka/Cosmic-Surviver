using System;
using UnityEditor.ShortcutManagement;
using UnityEngine;

public static class ScreenshotShortcut
{
    [Shortcut("Tools/Take Screenshot", KeyCode.Alpha7, ShortcutModifiers.Shift)]
    static void TakeScreenshot()
    {
        ScreenCapture.CaptureScreenshot("screenshot" + "_" + GetTime(DateTime.Now) + ".png",4);
        Debug.Log("Captured screenshot: " + "screenshot" + "_" + GetTime(DateTime.Now) + ".png");
    }

    static string GetTime(DateTime t)
    {
        return t.Hour + "-" + t.Minute + "-" + t.Second;
    }
}
