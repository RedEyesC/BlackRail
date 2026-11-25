using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TrackEditor
{
    internal enum ItemDragType
    {
        None, Position, TrimStart, TrimEnd, BlendIn, BlendOut, ScaleStart, ScaleEnd, Track
    }

    internal sealed class ItemDragger
    {
        private readonly struct Snapshot
        {
            internal readonly Clip Clip;
            internal readonly float Start;
            internal readonly float Length;
            internal float End => Start + Length;
            internal readonly float BlendIn;
            internal readonly float BlendOut;

            internal Snapshot(Clip clip)
            {
                Clip = clip;
                Start = clip.StartTime;
                Length = clip.Length;
                BlendIn = clip.BlendIn;
                BlendOut = clip.BlendOut;
            }

            internal bool Apply(float start, float length, float blendIn, float blendOut)
            {
                if (Clip.StartTime == start && Clip.Length == length && Clip.BlendIn == blendIn && Clip.BlendOut == blendOut)
                {
                    return false;
                }

                Clip.StartTime = start;
                Clip.Length = length;
                Clip.BlendIn = 0;
                Clip.BlendOut = blendOut;
                Clip.BlendIn = blendIn;
                return true;
            }

            internal bool SetRange(float start, float end)
            {
                var length = end - start;
                var blendIn = Mathf.Clamp(BlendIn, 0, length);
                var blendOut = Mathf.Clamp(BlendOut, 0, length - blendIn);
                return Apply(start, length, blendIn, blendOut);
            }

            internal bool Restore() => Apply(Start, Length, BlendIn, BlendOut);
        }

        private Snapshot[] items;
        private Snapshot[] followers;
        private HashSet<Clip> moving;
        private Dictionary<Track, List<Clip>> originalTrackOrder;
        private Vector2[] blockedOffsets;
        private float[] snapTimes;
        private float[] rippleSnapTimes;
        private float startPointer;
        private float duration;
        private Snapshot primary;
        private Clip previous;
        private Clip next;
        private float rangeStart;
        private float rangeEnd;
        private bool rippleActive;

        internal ItemDragType Type { get; private set; }
        internal bool IsDragging => Type != ItemDragType.None;
        internal bool IsClipDrag => items != null;
        internal Clip ActiveClip => IsClipDrag ? primary.Clip : null;
        internal Track ActiveTrack { get; private set; }
        internal float? SnappedTime { get; private set; }

        internal void BeginClips(ItemDragType type, Clip clip, IEnumerable<Clip> selection, Asset asset, float pointer, float playhead)
        {
            Reset();
            Type = type;
            startPointer = pointer;
            duration = asset.Length;
            primary = new Snapshot(clip);
            items = selection.Where(item => !item.IsLocked).Select(item => new Snapshot(item)).ToArray();
            moving = new HashSet<Clip>(items.Select(item => item.Clip));
            originalTrackOrder = moving.Select(item => item.Parent).Distinct().ToDictionary(track => track, track => track.Clips);
            rangeStart = items.Min(item => item.Start);
            rangeEnd = items.Max(item => item.End);

            if (!(clip is ClipSignal))
            {
                var clips = clip.Parent.Clips;
                var index = clips.IndexOf(clip);
                previous = clips.Take(index).LastOrDefault(BlocksRange);
                next = clips.Skip(index + 1).FirstOrDefault(BlocksRange);
            }

            followers = type == ItemDragType.Position && items.Length == 1
                ? clip.Parent.Clips.Where(item => item.StartTime > primary.Start && !item.IsLocked)
                    .Select(item => new Snapshot(item)).ToArray()
                : Array.Empty<Snapshot>();
            blockedOffsets = type == ItemDragType.Position ? BuildBlockedOffsets() : Array.Empty<Vector2>();
            snapTimes = GetSnapTimes(asset, playhead, moving);
            rippleSnapTimes = followers.Length == 0 ? snapTimes
                : GetSnapTimes(asset, playhead, new HashSet<Clip>(moving.Concat(followers.Select(item => item.Clip))));
        }

        internal void BeginTrack(Track track)
        {
            Reset();
            Type = ItemDragType.Track;
            ActiveTrack = track;
        }

        internal bool CanDropTrack(Group destination, int index) =>
            ActiveTrack != null && destination.CanMoveTrack(ActiveTrack, index);

        internal bool DropTrack(Group destination, int index) =>
            CanDropTrack(destination, index) && destination.MoveTrack(ActiveTrack, index);

        internal bool UpdateClips(float pointer, bool snapToGrid, bool ripple, bool magnet, float tolerance)
        {
            var delta = pointer - startPointer;
            SnappedTime = null;
            if (delta == 0) return RestoreClips();

            switch (Type)
            {
                case ItemDragType.Position:
                    return Move(delta, snapToGrid, ripple && items.Length == 1, magnet, tolerance);
                case ItemDragType.TrimStart:
                case ItemDragType.TrimEnd:
                    return Trim(delta, snapToGrid, magnet, tolerance);
                case ItemDragType.ScaleStart:
                case ItemDragType.ScaleEnd:
                    return Scale(delta, snapToGrid, magnet, tolerance);
                case ItemDragType.BlendIn:
                case ItemDragType.BlendOut:
                    return Blend(delta);
                default:
                    return false;
            }
        }

        internal bool Cancel()
        {
            var changed = IsClipDrag && RestoreClips();
            Reset();
            return changed;
        }

        private bool RestoreClips()
        {
            var changed = false;
            foreach (var item in items) changed |= item.Restore();
            if (rippleActive)
            {
                foreach (var follower in followers) changed |= follower.Restore();
            }

            foreach (var track in originalTrackOrder)
            {
                if (track.Key.Clips == track.Value) continue;
                track.Key.Clips = track.Value;
                changed = true;
            }

            rippleActive = false;
            return changed;
        }

        internal void Reset()
        {
            Type = ItemDragType.None;
            ActiveTrack = null;
            items = null;
            followers = null;
            moving = null;
            originalTrackOrder = null;
            blockedOffsets = null;
            snapTimes = null;
            rippleSnapTimes = null;
            previous = null;
            next = null;
            primary = default;
            rippleActive = false;
            SnappedTime = null;
        }

        private bool Move(float delta, bool grid, bool ripple, bool magnet, float tolerance)
        {
            if (grid)
            {
                delta = Prefs.SnapTime(primary.Start + delta) - primary.Start;
            }

            var limit = duration - rangeEnd;
            var minimum = -rangeStart;
            var moved = false;
            if (ripple)
            {
                if (previous != null)
                {
                    minimum = Mathf.Max(minimum, previous.EndTime - primary.Start - AllowedOverlap(primary, previous));
                }

                foreach (var follower in followers) limit = Mathf.Min(limit, duration - follower.End);
            }
            else if (rippleActive)
            {
                foreach (var follower in followers) moved |= follower.Restore();
            }

            rippleActive = ripple;
            // Existing clips beyond the sequence end can still move back into range.
            minimum = Mathf.Min(0, minimum);
            limit = Mathf.Max(0, limit);

            if (magnet)
            {
                var best = float.PositiveInfinity;
                float? guide = null;
                var correction = 0f;
                var candidates = ripple ? rippleSnapTimes : snapTimes;
                foreach (var item in items)
                {
                    SnapEdge(item.Start + delta, candidates, tolerance, ref best, ref correction, ref guide);
                    SnapEdge(item.End + delta, candidates, tolerance, ref best, ref correction, ref guide);
                }

                delta += correction;
                SnappedTime = guide;
            }

            var wanted = delta;
            delta = Mathf.Clamp(delta, minimum, limit);
            if (!ripple)
            {
                foreach (var obstacle in blockedOffsets)
                {
                    if (delta <= obstacle.x || delta >= obstacle.y)
                    {
                        continue;
                    }

                    var left = obstacle.x >= minimum;
                    var right = obstacle.y <= limit;
                    if (!left && !right)
                    {
                        delta = 0;
                    }
                    else
                    {
                        delta = left && (!right || delta - obstacle.x <= obstacle.y - delta) ? obstacle.x : obstacle.y;
                    }

                    break;
                }
            }

            if (!Mathf.Approximately(delta, wanted)) SnappedTime = null;
            foreach (var item in items) moved |= MoveItem(item, delta);
            if (ripple)
            {
                foreach (var follower in followers) moved |= MoveItem(follower, delta);
            }

            return moved;
        }

        private static bool MoveItem(Snapshot item, float delta) =>
            item.Apply(item.Start + delta, item.Length, item.BlendIn, item.BlendOut);

        private bool Trim(float delta, bool grid, bool magnet, float tolerance)
        {
            var start = Type == ItemDragType.TrimStart;
            var wanted = SnapBoundary((start ? primary.Start : primary.End) + delta, grid, magnet, tolerance);
            var min = start ? previous?.EndTime - AllowedOverlap(primary, previous) ?? 0 : primary.Start;
            var max = start ? primary.End : next?.StartTime + AllowedOverlap(primary, next) ?? duration;
            var time = Mathf.Clamp(wanted, Mathf.Min(min, primary.Start), Mathf.Max(max, primary.End));
            if (time != wanted) SnappedTime = null;
            return primary.SetRange(start ? time : primary.Start, start ? primary.End : time);
        }

        private bool Scale(float delta, bool grid, bool magnet, float tolerance)
        {
            if (rangeEnd <= rangeStart)
            {
                return false;
            }

            var start = Type == ItemDragType.ScaleStart;
            var wanted = SnapBoundary((start ? rangeStart : rangeEnd) + delta, grid, magnet, tolerance);
            var minimum = start ? Mathf.Clamp(wanted, 0, rangeEnd) : rangeStart;
            var maximum = start ? rangeEnd : Mathf.Clamp(wanted, rangeStart, Mathf.Max(rangeEnd, duration));
            foreach (var item in items)
            {
                if (item.Clip is ClipSignal) continue;
                var a = ScaleTime(item.Start, minimum, maximum);
                var b = ScaleTime(item.End, minimum, maximum);
                foreach (var other in item.Clip.Parent.Clips)
                {
                    if (BlocksRange(other) && RangesConflict(item, a, b, other))
                    {
                        SnappedTime = null;
                        return false;
                    }
                }
            }

            if ((start ? minimum : maximum) != wanted) SnappedTime = null;
            var changed = false;
            foreach (var item in items)
            {
                changed |= item.SetRange(ScaleTime(item.Start, minimum, maximum), ScaleTime(item.End, minimum, maximum));
            }

            return changed;
        }

        private bool Blend(float delta)
        {
            var length = primary.Length;
            var blendIn = Type == ItemDragType.BlendIn
                ? Mathf.Clamp(primary.BlendIn + delta, 0, length - primary.BlendOut)
                : primary.BlendIn;
            var blendOut = Type == ItemDragType.BlendOut
                ? Mathf.Clamp(primary.BlendOut - delta, 0, length - primary.BlendIn)
                : primary.BlendOut;
            return primary.Apply(primary.Start, primary.Length, blendIn, blendOut);
        }

        private float ScaleTime(float time, float min, float max) =>
            Mathf.Lerp(min, max, Mathf.InverseLerp(rangeStart, rangeEnd, time));

        private static float AllowedOverlap(Snapshot item, Clip other) =>
            item.Clip.CanCrossBlend(other) ? Mathf.Min(item.Length, other.Length) * 0.5f : 0;

        private bool BlocksRange(Clip other) => !(other is ClipSignal) && !moving.Contains(other);

        private static bool RangesConflict(Snapshot item, float start, float end, Clip other)
        {
            var allowance = item.Clip.CanCrossBlend(other) ? Mathf.Min(end - start, other.Length) * 0.5f : 0;
            return start < other.EndTime - allowance && end > other.StartTime + allowance;
        }

        private Vector2[] BuildBlockedOffsets()
        {
            var obstacles = new List<Vector2>();
            foreach (var item in items)
            {
                if (item.Clip is ClipSignal) continue;
                foreach (var other in item.Clip.Parent.Clips)
                {
                    if (!BlocksRange(other)) continue;
                    var allowance = AllowedOverlap(item, other);
                    obstacles.Add(new Vector2(other.StartTime + allowance - item.End, other.EndTime - allowance - item.Start));
                }
            }

            var merged = new List<Vector2>();
            foreach (var offset in obstacles.OrderBy(interval => interval.x))
            {
                var last = merged.Count - 1;
                if (last >= 0 && offset.x < merged[last].y)
                {
                    merged[last] = new Vector2(merged[last].x, Mathf.Max(merged[last].y, offset.y));
                }
                else
                {
                    merged.Add(offset);
                }
            }

            return merged.ToArray();
        }

        private float SnapBoundary(float time, bool grid, bool magnet, float tolerance)
        {
            if (grid) time = Prefs.SnapTime(time);
            SnappedTime = magnet ? FindSnap(time, snapTimes, tolerance) : null;
            return SnappedTime ?? time;
        }

        private static void SnapEdge(float time, float[] candidates, float tolerance, ref float distance, ref float delta, ref float? guide)
        {
            var candidate = FindSnap(time, candidates, tolerance);
            if (candidate.HasValue && Mathf.Abs(candidate.Value - time) < distance)
            {
                distance = Mathf.Abs(candidate.Value - time);
                delta = candidate.Value - time;
                guide = candidate;
            }
        }

        internal static float[] GetSnapTimes(Asset asset, float playhead, HashSet<Clip> excluded = null)
        {
            var times = new SortedSet<float> { 0, asset.Length, playhead };
            foreach (var group in asset.groups)
            {
                foreach (var track in group.Tracks)
                {
                    if (!track.IsActive) continue;
                    foreach (var clip in track.Clips)
                    {
                        if (excluded?.Contains(clip) == true) continue;
                        times.Add(clip.StartTime);
                        times.Add(clip.EndTime);
                    }
                }
            }

            return times.ToArray();
        }

        internal static float? FindSnap(float time, float[] candidates, float tolerance)
        {
            var index = Array.BinarySearch(candidates, time);
            if (index >= 0) return time;
            index = ~index;
            var left = index == 0 ? float.NegativeInfinity : candidates[index - 1];
            var right = index == candidates.Length ? float.PositiveInfinity : candidates[index];
            var nearest = time - left <= right - time ? left : right;
            return Mathf.Abs(nearest - time) <= tolerance ? nearest : (float?)null;
        }
    }
}
