using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

[ScriptedImporter(1, "action")]
public class ActionAssetImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        string text = File.ReadAllText(ctx.assetPath, Encoding.UTF8);

        var textAsset = new TextAsset(text) { name = Path.GetFileNameWithoutExtension(ctx.assetPath) };

        ctx.AddObjectToAsset("TextAsset", textAsset);
        ctx.SetMainObject(textAsset);
    }

    private const string DefaultFileName = "NewAction.action";

    [MenuItem("Assets/Create/Action Config", false, 30)]
    private static void CreateActionConfig()
    {
        string folderPath = GetSelectedFolderPath();
        string assetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folderPath, DefaultFileName).Replace("\\", "/"));

        File.WriteAllText(assetPath, "{}", Encoding.UTF8);

        AssetDatabase.ImportAsset(assetPath);
        AssetDatabase.Refresh();

        Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
        Selection.activeObject = asset;

        EditorGUIUtility.PingObject(asset);
    }

    private static string GetSelectedFolderPath()
    {
        Object selected = Selection.activeObject;

        if (selected == null)
            return "Assets";

        string path = AssetDatabase.GetAssetPath(selected);

        if (string.IsNullOrEmpty(path))
            return "Assets";

        if (Directory.Exists(path))
            return path;

        return Path.GetDirectoryName(path)?.Replace("\\", "/") ?? "Assets";
    }
}
