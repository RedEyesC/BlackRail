using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TrackEditor
{
    public class PreferencesWindow : PopupWindowContent
    {
        private static Rect myRect;
        private bool firstPass = true;

        public static void Show(Rect rect)
        {
            myRect = rect;
            PopupWindow.Show(new Rect(rect.x, rect.y, 0, 0), new PreferencesWindow());
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(myRect.width, myRect.height);
        }

        public override void OnGUI(Rect rect)
        {
            GUILayout.BeginVertical("box");

            GUI.color = new Color(0, 0, 0, 0.3f);

            GUILayout.BeginHorizontal(Styles.headerBoxStyle);
            GUI.color = Color.white;
            GUILayout.Label($"<size=22><b>{Lan.PreferencesTitle}</b></size>");
            GUILayout.EndHorizontal();
            GUILayout.Space(2);

            GUILayout.BeginVertical("box");
            Prefs.timeStepMode = (Prefs.TimeStepMode)EditorGUILayout.EnumPopup(Lan.PreferencesTimeStepMode, Prefs.timeStepMode);
            if (Prefs.timeStepMode == Prefs.TimeStepMode.Seconds)
            {
                Prefs.snapInterval = EditorTools.CleanPopup<float>(
                    Lan.PreferencesSnapInterval,
                    Prefs.snapInterval,
                    Prefs.snapIntervals.ToList()
                );
            }
            else
            {
                Prefs.frameRate = EditorTools.CleanPopup<int>(Lan.PreferencesFrameRate, Prefs.frameRate, Prefs.frameRates.ToList());
            }

            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            Prefs.magnetSnapping = EditorGUILayout.Toggle(
                new GUIContent(Lan.PreferencesMagnetSnapping, Lan.PreferencesMagnetSnappingTips),
                Prefs.magnetSnapping
            );
            Prefs.scrollWheelZooms = EditorGUILayout.Toggle(
                new GUIContent(Lan.PreferencesScrollWheelZooms, Lan.PreferencesScrollWheelZoomsTips),
                Prefs.scrollWheelZooms
            );
            GUILayout.EndVertical();

            GUILayout.EndVertical();

            if (firstPass || Event.current.type == EventType.Repaint)
            {
                firstPass = false;
                myRect.height = GUILayoutUtility.GetLastRect().yMax + 5;
            }
        }
    }
}
