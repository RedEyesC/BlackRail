using System;
using System.IO;
using UnityEngine;

namespace TrackEditor
{
    public static class Prefs
    {
        private const int DefaultFrameRate = 30;
        public static readonly string CONFIG_PATH = $"{Application.dataPath}/../ProjectSettings/ActionEditor.txt";

        [Serializable]
        public enum TimeStepMode
        {
            Seconds,
            Frames,
        }

        [Serializable]
        class SerializedData
        {
            public TimeStepMode TimeStepMode = TimeStepMode.Seconds;
            public float SnapInterval = 0.1f;
            public int FrameRate = DefaultFrameRate;

            public int AutoSaveSeconds;
            public string SavePath = "Assets/";
            public bool ScrollWheelZooms = true;

            public bool MagnetSnapping = true;
            public float TrackListLeftMargin = Styles.MIN_LEFT_MARGIN;
        }

        private static SerializedData _data;

        private static SerializedData data
        {
            get
            {
                if (_data == null)
                {
                    if (File.Exists(CONFIG_PATH))
                    {
                        var json = File.ReadAllText(CONFIG_PATH);
                        _data = JsonUtility.FromJson<SerializedData>(json);
                    }

                    if (_data == null)
                    {
                        _data = new SerializedData();
                    }
                }

                return _data;
            }
        }

        public static readonly float[] snapIntervals = new float[] { 0.001f, 0.01f, 0.1f };
        public static readonly int[] frameRates = new int[] { 24, 25, 30, 60 };

        public static bool scrollWheelZooms
        {
            get => data.ScrollWheelZooms;
            set
            {
                if (data.ScrollWheelZooms != value)
                {
                    data.ScrollWheelZooms = value;
                    Save();
                }
            }
        }

        public static int autoSaveSeconds
        {
            get => data.AutoSaveSeconds;
            set
            {
                if (data.AutoSaveSeconds != value)
                {
                    data.AutoSaveSeconds = value;
                    Save();
                }
            }
        }

        public static string savePath
        {
            get => data.SavePath;
            set
            {
                if (data.SavePath != value)
                {
                    data.SavePath = value;
                    Save();
                }
            }
        }

        public static bool magnetSnapping
        {
            get => data.MagnetSnapping;
            set
            {
                if (data.MagnetSnapping != value)
                {
                    data.MagnetSnapping = value;
                    Save();
                }
            }
        }

        public static float trackListLeftMargin
        {
            get => data.TrackListLeftMargin;
            set
            {
                if (Math.Abs(data.TrackListLeftMargin - value) > 0.001f)
                {
                    data.TrackListLeftMargin = value;
                    Save();
                }
            }
        }

        public static TimeStepMode timeStepMode
        {
            get => data.TimeStepMode;
            set
            {
                if (data.TimeStepMode != value)
                {
                    data.TimeStepMode = value;
                    Save();
                }
            }
        }

        public static int frameRate
        {
            get => Array.IndexOf(frameRates, data.FrameRate) >= 0 ? data.FrameRate : DefaultFrameRate;
            set
            {
                if (Array.IndexOf(frameRates, value) >= 0 && data.FrameRate != value)
                {
                    data.FrameRate = value;
                    Save();
                }
            }
        }

        public static float snapInterval
        {
            get => Mathf.Max(data.SnapInterval, 0.001f);
            set
            {
                var interval = Mathf.Max(value, 0.001f);
                if (!TimelineTime.AreEqual(data.SnapInterval, interval))
                {
                    data.SnapInterval = interval;
                    Save();
                }
            }
        }

        internal static bool DisplayFrames => timeStepMode == TimeStepMode.Frames;

        internal static int SecondsToFrames(float seconds) => Mathf.RoundToInt(seconds * frameRate);

        internal static float FramesToSeconds(float frames) => frames / frameRate;

        public static float SnapTime(float seconds) => GridUnitsToSeconds(Mathf.Round(SecondsToGridUnits(seconds)));

        internal static float StepTime(float seconds, int direction)
        {
            var units = SecondsToGridUnits(seconds);
            var nearest = Mathf.Round(units);
            if (TimelineTime.AreEqual(seconds, GridUnitsToSeconds(nearest)))
            {
                units = nearest;
            }

            var next = direction > 0 ? Mathf.Floor(units) + 1 : Mathf.Ceil(units) - 1;
            return GridUnitsToSeconds(next);
        }

        private static float SecondsToGridUnits(float seconds) => DisplayFrames ? seconds * frameRate : seconds / snapInterval;

        private static float GridUnitsToSeconds(float units) => DisplayFrames ? FramesToSeconds(units) : units * snapInterval;

        static void Save()
        {
            System.IO.File.WriteAllText(CONFIG_PATH, JsonUtility.ToJson(data));
        }
    }
}
