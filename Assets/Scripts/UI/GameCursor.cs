using UnityEngine;

internal static class GameCursor
{
    private const string ResourcePath = "Art/Cursor/EmberCursor";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Apply()
    {
        Texture2D cursor = Resources.Load<Texture2D>(ResourcePath);
        if (cursor != null)
        {
            Cursor.SetCursor(cursor, new Vector2(2f, 2f), CursorMode.Auto);
        }
    }
}
