using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TrackEditor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ActionEditor
{
    internal sealed class ActionAssetStorage : IAssetStorage
    {
        private const string Extension = ".action";

        public bool CanLoad(Object source)
        {
            var path = GetAssetPath(source);
            return !string.IsNullOrEmpty(path) && string.Equals(Path.GetExtension(path), Extension, StringComparison.OrdinalIgnoreCase);
        }

        public Asset Load(Object source)
        {
            var path = GetAssetPath(source);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            var json = File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : string.Empty;
            var asset = string.IsNullOrWhiteSpace(json) ? CreateDefaultAsset(path) : Deserialize(json, path);
            return asset;
        }

        public void Save(Asset asset, Object source)
        {
            var path = GetAssetPath(source);
            if (asset == null || string.IsNullOrEmpty(path))
            {
                return;
            }

            File.WriteAllText(path, Serialize(asset), Encoding.UTF8);
            AssetDatabase.ImportAsset(path);
            AssetDatabase.Refresh();
        }

        public void SaveAssetIntoObject(Object childAsset, Object masterAsset)
        {
            if (childAsset == null)
            {
                return;
            }

            childAsset.hideFlags |= HideFlags.DontSave | HideFlags.HideInHierarchy;
        }

        private static string GetAssetPath(Object source)
        {
            if (source == null)
            {
                return string.Empty;
            }

            if (source is AssetImporter importer)
            {
                return importer.assetPath;
            }

            return AssetDatabase.GetAssetPath(source);
        }

        private static ActionAsset CreateDefaultAsset(string path)
        {
            var asset = ScriptableObject.CreateInstance<ActionAsset>();
            asset.name = Path.GetFileNameWithoutExtension(path);
            asset.hideFlags = HideFlags.DontSave;
            return asset;
        }

        private static string Serialize(Asset asset)
        {
            var document = new JsonDocument { asset = ToNode(asset), groups = new List<GroupNode>() };

            foreach (var group in asset.groups)
            {
                if (group == null)
                {
                    continue;
                }

                var groupNode = new GroupNode { data = ToNode(group), tracks = new List<TrackNode>() };
                document.groups.Add(groupNode);

                if (group.Tracks == null)
                {
                    continue;
                }

                foreach (var track in group.Tracks)
                {
                    if (track == null)
                    {
                        continue;
                    }

                    var trackNode = new TrackNode { data = ToNode(track), clips = new List<Node>() };
                    groupNode.tracks.Add(trackNode);

                    if (track.Clips == null)
                    {
                        continue;
                    }

                    foreach (var clip in track.Clips)
                    {
                        if (clip == null)
                        {
                            continue;
                        }

                        trackNode.clips.Add(ToNode(clip));
                    }
                }
            }

            return JsonUtility.ToJson(document, true);
        }

        private static Asset Deserialize(string json, string path)
        {
            try
            {
                var document = JsonUtility.FromJson<JsonDocument>(json);
                var asset = CreateAsset(document?.asset, path);
                asset.groups.Clear();

                if (document?.groups != null)
                {
                    foreach (var groupNode in document.groups)
                    {
                        var group = CreateDirectable<Group>(groupNode.data, typeof(ActionJsonGroup));
                        if (group == null)
                        {
                            continue;
                        }

                        group.Parent = asset;
                        group.Tracks = new List<Track>();
                        asset.groups.Add(group);

                        if (groupNode.tracks == null)
                        {
                            continue;
                        }

                        foreach (var trackNode in groupNode.tracks)
                        {
                            var track = CreateDirectable<Track>(trackNode.data, typeof(ActionJsonTrack));
                            if (track == null)
                            {
                                continue;
                            }

                            track.Parent = group;
                            track.Clips = new List<ActionClip>();
                            group.Tracks.Add(track);

                            if (trackNode.clips == null)
                            {
                                continue;
                            }

                            foreach (var clipNode in trackNode.clips)
                            {
                                var clip = CreateDirectable<ActionClip>(clipNode, typeof(ActionJsonClip));
                                if (clip == null)
                                {
                                    continue;
                                }

                                clip.Parent = track;
                                track.Clips.Add(clip);
                            }
                        }
                    }
                }

                return asset;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Action json load failed: {path}\n{exception}");
                return CreateDefaultAsset(path);
            }
        }

        private static Asset CreateAsset(Node node, string path)
        {
            var asset = CreateDirectable<Asset>(node, typeof(ActionAsset));
            if (asset == null)
            {
                asset = CreateDefaultAsset(path);
            }

            if (string.IsNullOrEmpty(asset.name))
            {
                asset.name = Path.GetFileNameWithoutExtension(path);
            }

            asset.hideFlags = HideFlags.DontSave;
            return asset;
        }

        private static T CreateDirectable<T>(Node node, Type fallbackType)
            where T : DirectableAsset
        {
            var type = ResolveType(node?.type);
            if (type == null || !typeof(T).IsAssignableFrom(type) || type.IsAbstract)
            {
                type = fallbackType;
            }

            var directable = ScriptableObject.CreateInstance(type) as T;
            if (directable == null)
            {
                return null;
            }

            directable.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
            if (!string.IsNullOrEmpty(node?.json))
            {
                EditorJsonUtility.FromJsonOverwrite(node.json, directable);
            }

            directable.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
            return directable;
        }

        private static Type ResolveType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return null;
            }

            return Type.GetType(typeName);
        }

        private static Node ToNode(DirectableAsset directable)
        {
            return new Node { type = directable.GetType().AssemblyQualifiedName, json = EditorJsonUtility.ToJson(directable) };
        }

        [Serializable]
        private class JsonDocument
        {
            public Node asset;
            public List<GroupNode> groups;
        }

        [Serializable]
        private class GroupNode
        {
            public Node data;
            public List<TrackNode> tracks;
        }

        [Serializable]
        private class TrackNode
        {
            public Node data;
            public List<Node> clips;
        }

        [Serializable]
        private class Node
        {
            public string type;
            public string json;
        }
    }
}
