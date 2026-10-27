using UnityEngine;

// A text box at the top of the screen for status and error messages when a scene has no
// TextMeshPro assigned for them. It has no buttons and needs nothing in the scene.
public static class StatusBox
{
    static GUIStyle s_style;

    public static void Draw(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        if (s_style == null)
        {
            s_style = new GUIStyle(GUI.skin.box);
            s_style.wordWrap = true;
            s_style.alignment = TextAnchor.UpperLeft;
            s_style.fontSize = 16;
            s_style.normal.textColor = Color.white;
        }
        GUI.Box(new Rect(10, 10, Screen.width - 20, 70), text, s_style);
    }
}
