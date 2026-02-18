using System;
using System.IO;
using UnityEditor.ShortcutManagement;
using UnityEngine;

public static class ScreenshotShortcut
{
    [Shortcut("Tools/Take Screenshot", KeyCode.Alpha7, ShortcutModifiers.Shift)]
    static void TakeScreenshot()
    {
        string folder = Path.Combine(Application.dataPath, "../Screenshots");
        Directory.CreateDirectory(folder);

        string filename = "screenshot_" + GetTime(DateTime.Now) + ".png";
        string fullPath = Path.GetFullPath(Path.Combine(folder, filename));

        ScreenCapture.CaptureScreenshot(fullPath, 4);
        Debug.Log("Captured screenshot: " + fullPath);
    }

    static string GetTime(DateTime t)
    {
        return t.Hour + "-" + t.Minute + "-" + t.Second;
    }
}