using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TrackEditor
{
    public class AssetPlayer
    {
        private static readonly Dictionary<Type, Type> PreviewTypes = FindPreviewTypes();

        private readonly Asset asset;
        private readonly PreviewContext context;
        private readonly List<IDirectableTimePointer> pointers = new List<IDirectableTimePointer>();
        private readonly List<StartTimePointer> activeRanges = new List<StartTimePointer>();
        private List<ClipSignal> signals = new List<ClipSignal>();
        private readonly Dictionary<ClipSignal, PreviewBase> signalPreviews = new Dictionary<ClipSignal, PreviewBase>();

        private bool hasSampled;
        private bool previewDirty = true;
        private bool rebuild = true;
        private float currentTime;

        public event Action<ClipSignal, bool> SignalCrossed;

        public AssetPlayer(Asset asset, PreviewContext context = null)
        {
            this.asset = asset;
            this.context = context;
        }

        public float Length => asset?.Length ?? 0;
        public float CurrentTime
        {
            get => currentTime;
            set => currentTime = Mathf.Clamp(value, 0, Length);
        }

        public bool IsActive { get; set; }
        public float PreviousTime { get; private set; }

        internal void InvalidatePreview(bool rebuild)
        {
            previewDirty = true;
            this.rebuild |= rebuild;
        }

        public bool Sample() => Sample(currentTime);

        public bool Sample(float time)
        {
            CurrentTime = time;
            if (hasSampled && currentTime == PreviousTime && !previewDirty)
            {
                return false;
            }

            var from = hasSampled ? PreviousTime : -float.Epsilon;
            if (rebuild)
            {
                RevertPreview();
                InitializePreviewPointers();
            }

            if (previewDirty)
            {
                pointers.Sort((left, right) =>
                {
                    var order = left.time.CompareTo(right.time);
                    return order != 0 ? order : left.order.CompareTo(right.order);
                });
                signals = signals.OrderBy(signal => signal.StartTime).ToList();
            }

            context?.BeginFrame(currentTime);
            try
            {
                foreach (var pointer in pointers)
                {
                    pointer.TriggerForward(currentTime, from);
                }

                for (var i = pointers.Count - 1; i >= 0; i--)
                {
                    pointers[i].TriggerBackward(currentTime, from);
                }

                foreach (var pointer in activeRanges)
                {
                    pointer.Update(currentTime, from);
                }

                EmitSignals(from, currentTime);
            }
            finally
            {
                context?.EndFrame();
            }

            PreviousTime = currentTime;
            hasSampled = true;
            previewDirty = false;
            return true;
        }

        private void EmitSignals(float from, float to)
        {
            if (from == to)
            {
                return;
            }

            var forward = from < to;
            for (var index = 0; index < signals.Count; index++)
            {
                var signal = signals[forward ? index : signals.Count - 1 - index];
                if (!signal.Crossed(from, to))
                {
                    continue;
                }

                if (signalPreviews.TryGetValue(signal, out var preview))
                {
                    preview.OnSignal(!forward);
                }

                SignalCrossed?.Invoke(signal, !forward);
            }
        }

        internal void RevertPreview()
        {
            try
            {
                if (hasSampled)
                {
                    for (var i = pointers.Count - 1; i >= 0; i--)
                    {
                        pointers[i].TriggerBackward(-float.Epsilon, PreviousTime);
                    }
                }
            }
            finally
            {
                context?.Dispose();
                hasSampled = false;
                PreviousTime = 0;
            }
        }

        private void InitializePreviewPointers()
        {
            pointers.Clear();
            activeRanges.Clear();
            signals.Clear();
            signalPreviews.Clear();
            context?.BindTarget();

            if (asset != null)
            {
                asset.Validate();
                foreach (var item in asset.directables)
                {
                    if (!item.IsActive)
                    {
                        continue;
                    }

                    PreviewBase preview = null;
                    for (var type = item.GetType(); type != null; type = type.BaseType)
                    {
                        if (PreviewTypes.TryGetValue(type, out var previewType))
                        {
                            preview = (PreviewBase)Activator.CreateInstance(previewType);
                            preview.Bind(item, context);
                            break;
                        }
                    }

                    if (item is ClipSignal signal)
                    {
                        signals.Add(signal);
                        if (preview != null) signalPreviews.Add(signal, preview);
                    }
                    else if (preview != null)
                    {
                        var start = new StartTimePointer(preview, pointers.Count);
                        activeRanges.Add(start);
                        pointers.Add(start);
                        pointers.Add(new EndTimePointer(preview, pointers.Count));
                    }
                }
            }

            rebuild = false;
        }

        private static Dictionary<Type, Type> FindPreviewTypes()
        {
            var types = new Dictionary<Type, Type>();
            foreach (var type in TypeCache.GetTypesWithAttribute<CustomPreviewAttribute>()
                         .Where(type => !type.IsAbstract && typeof(PreviewBase).IsAssignableFrom(type))
                         .OrderBy(type => type.FullName, StringComparer.Ordinal))
            {
                foreach (CustomPreviewAttribute attribute in type.GetCustomAttributes(typeof(CustomPreviewAttribute), false))
                {
                    if (!types.TryGetValue(attribute.PreviewType, out var current) || type.IsSubclassOf(current))
                    {
                        types[attribute.PreviewType] = type;
                    }
                }
            }

            return types;
        }
    }
}
