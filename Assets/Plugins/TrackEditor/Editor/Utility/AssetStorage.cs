using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TrackEditor
{
    public sealed class AssetStorage : IAssetStorage
    {
        public static readonly IAssetStorage Default = new AssetStorage();

        public Asset Load(Object source, Type assetType)
        {
            if (!(source is TextAsset text) || !IsJson(source))
            {
                return null;
            }

            return TimelineJson.Deserialize(text.text, assetType);
        }

        public void Save(Asset asset, Object source)
        {
            var path = AssetDatabase.GetAssetPath(source);
            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException("The timeline JSON is no longer in the Project.");
            }

            Write(asset, path);
        }

        public static bool TryOpen<TAsset, TWindow>(Object source)
            where TAsset : Asset
            where TWindow : TrackEditorWindow =>
            IsTimeline<TAsset>(source) && TrackEditorWindow.TryShowWindow<TWindow>(source);

        public static bool IsTimeline<TAsset>(Object source) where TAsset : Asset
        {
            try
            {
                return Default.Load(source, typeof(TAsset)) != null;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        public static bool IsJson(Object source) => string.Equals(
            Path.GetExtension(AssetDatabase.GetAssetPath(source)), ".json", StringComparison.OrdinalIgnoreCase);

        private static void Write(Asset asset, string path)
        {
            var json = TimelineJson.Serialize(asset);
            var temp = Path.Combine(Path.GetDirectoryName(path), "." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                File.WriteAllText(temp, json);
                if (File.Exists(path))
                {
                    File.Replace(temp, path, null);
                }
                else
                {
                    File.Move(temp, path);
                }
            }
            finally
            {
                if (File.Exists(temp))
                {
                    File.Delete(temp);
                }
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }

        public static void Create<T>(string defaultName) where T : Asset, new()
        {
            var action = ScriptableObject.CreateInstance<CreateTimelineJson>();
            action.assetType = typeof(T).AssemblyQualifiedName;
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, action, defaultName + ".json", null, null);
        }

        public static TextAsset CreateAtPath<T>(string path) where T : Asset, new() =>
            CreateAtPath(new T(), path);

        private static TextAsset CreateAtPath(Asset asset, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Specify a project-relative path under Assets.", nameof(path));
            }

            path = Path.ChangeExtension(path.Replace('\\', '/'), ".json");
            var folder = path.Substring(0, path.LastIndexOf('/') + 1).TrimEnd('/');
            if (string.IsNullOrEmpty(Path.GetFileNameWithoutExtension(path))
                || (folder != "Assets" && !folder.StartsWith("Assets/", StringComparison.Ordinal))
                || !AssetDatabase.IsValidFolder(folder))
            {
                throw new ArgumentException("Specify a file path in an existing folder under Assets.", nameof(path));
            }

            path = AssetDatabase.GenerateUniqueAssetPath(path);
            asset.name = Path.GetFileNameWithoutExtension(path);
            Write(asset, path);
            return AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        }

        public static Object Resolve(TimelineAssetReference reference, Type type)
        {
            if (string.IsNullOrEmpty(reference.guid))
            {
                return null;
            }

            var path = AssetDatabase.GUIDToAssetPath(reference.guid);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            return AssetDatabase.LoadAllAssetsAtPath(path).FirstOrDefault(asset =>
                asset != null && type.IsInstanceOfType(asset)
                && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string _, out long id) && id == reference.fileId);
        }

        public static TimelineAssetReference ReferenceOf(Object asset)
        {
            return asset != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long id)
                ? new TimelineAssetReference { guid = guid, fileId = id }
                : default;
        }

        private sealed class CreateTimelineJson : UnityEditor.ProjectWindowCallback.EndNameEditAction
        {
            [SerializeField] internal string assetType;

            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var asset = (Asset)Activator.CreateInstance(Type.GetType(assetType));
                ProjectWindowUtil.ShowCreatedAsset(CreateAtPath(asset, pathName));
            }
        }
    }
}
