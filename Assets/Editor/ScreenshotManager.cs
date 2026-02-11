using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

public static class ScreenshotShortcut
{
    [Shortcut("Tools/Take Screenshot", KeyCode.Alpha7, ShortcutModifiers.Shift)]
    static void TakeScreenshot()
    {
        ScreenCapture.CaptureScreenshot("screenshot.png",4);
        Debug.Log("Captured screenshot");
    }
}
