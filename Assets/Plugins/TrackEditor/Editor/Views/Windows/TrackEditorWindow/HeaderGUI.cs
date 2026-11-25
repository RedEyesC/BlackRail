using UnityEditor;
using UnityEngine;

namespace TrackEditor
{
    public partial class TrackEditorWindow
    {
        public virtual void DrawHeaderGUI()
        {
            DrawToolbar();
            DrawPlayControl();
        }

        protected void DrawPlayControl()
        {
            GUILayout.BeginArea(view.TopLeftRect);
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                DrawPlayControlLeft();
            }

            DrawPlayControlRight();
            GUILayout.Space(4);
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                DrawPlayheadField();
            }

            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            GUI.contentColor = Color.white;
        }

        protected virtual void DrawPlayControlLeft()
        {
            DrawPlaybackButton(Styles.playBackwardIcon, Lan.JumpToStartTips, () => Seek(0));
            DrawPlaybackButton(Styles.stepBackwardIcon, Lan.StepBackwardTips, StepBackward);
            DrawPlaybackButton(IsPlay ? Styles.pauseIcon : Styles.playIcon, IsPlay ? Lan.PauseTips : Lan.PlayTips, TogglePlayback);
            DrawPlaybackButton(Styles.stepForwardIcon, Lan.StepForwardTips, StepForward);
            DrawPlaybackButton(Styles.playForwardIcon, Lan.PlayForwardTips, () => Seek(player.Length));
        }

        private static void DrawPlaybackButton(Texture icon, string tooltip, System.Action action)
        {
            if (GUILayout.Button(new GUIContent(icon, tooltip), EditorStyles.toolbarButton, GUILayout.Width(24)))
            {
                action();
            }
        }

        protected virtual void DrawPlayControlRight()
        {
            EditorGUI.BeginChangeCheck();
            var isLoop = GUILayout.Toggle(
                EditorPlaybackWrapMode == WrapMode.Loop,
                new GUIContent(Styles.playLoopIcon, Lan.PlayLoopTips),
                EditorStyles.toolbarButton,
                GUILayout.Width(26)
            );
            if (EditorGUI.EndChangeCheck())
            {
                EditorPlaybackWrapMode = isLoop ? WrapMode.Loop : WrapMode.Once;
            }
        }

        protected virtual void DrawToolbar()
        {
            if (!EditorGUIUtility.isProSkin)
            {
                GUI.contentColor = Color.black.WithAlpha(0.7f);
            }

            GUI.enabled = !IsPlay; //是否禁用工具条。播放时

            GUI.backgroundColor = Color.white;
            GUI.color = Color.white;
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            DrawToolbarLeft();
            GUI.enabled = true;

            GUILayout.FlexibleSpace();

            DrawToolbarRight();

            GUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;

            GUI.enabled = true;
            GUI.contentColor = Color.white;
        }

        protected virtual void DrawToolbarLeft()
        {
            isLocked = GUILayout.Toggle(isLocked, new GUIContent(Styles.lockIcon, Lan.LockMenuTips), EditorStyles.toolbarButton);

            if (GUILayout.Button(new GUIContent(Styles.plusIcon, Lan.GroupAdd), EditorStyles.toolbarButton, GUILayout.Width(26)))
            {
                DrawGroupContextMenu();
            }

            Prefs.magnetSnapping = GUILayout.Toggle(
                Prefs.magnetSnapping,
                new GUIContent(Styles.magnetIcon, Lan.OpenMagnetSnappingTips),
                EditorStyles.toolbarButton
            );

            var gName = asset != null ? asset.name : string.Empty;

            var con = new GUIContent(string.Format(Lan.HeaderSelectAsset, gName), Lan.SelectAssetTips);
            if (GUILayout.Button(con, EditorStyles.toolbarDropDown, GUILayout.Width(120)))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Open JSON"), false, () =>
                    EditorGUIUtility.ShowObjectPicker<TextAsset>(assetSource as TextAsset, false, "t:TextAsset", GetInstanceID()));
                menu.AddItem(new GUIContent("Asset Properties"), false, () => SelectedObject = asset);
                var selectedTarget = Selection.activeGameObject;
                if (selectedTarget != null && selectedTarget.scene.IsValid())
                {
                    menu.AddItem(new GUIContent("Use Selected Preview Target"), selectedTarget == previewTarget, () =>
                    {
                        previewTarget = selectedTarget;
                        player.InvalidatePreview(rebuild: true);
                    });
                }

                menu.ShowAsContext();
            }
        }

        private void DrawPlayheadField()
        {
            var frames = Prefs.timeStepMode == Prefs.TimeStepMode.Frames;
            var frameRate = Mathf.Max(1, Prefs.frameRate);
            EditorGUI.BeginChangeCheck();
            var time = frames
                ? EditorGUILayout.DelayedIntField(Mathf.RoundToInt(player.CurrentTime * frameRate), GUILayout.Width(60)) / (float)frameRate
                : EditorGUILayout.DelayedFloatField(player.CurrentTime, GUILayout.Width(60));
            if (EditorGUI.EndChangeCheck() && !float.IsNaN(time))
            {
                Seek(time);
            }
        }

        protected virtual void DrawToolbarRight()
        {
            //显示保持状态
            GUI.color = Color.white.WithAlpha(0.3f);
            //GUILayout.Label($"<size=11>{string.Format(Lan.HeaderLastSaveTime, App.LastSaveTime.ToString("HH:mm:ss"))}</size>");
            GUI.color = Color.white;

            //暂时不需要保存按钮
            //if (GUILayout.Button(new GUIContent(Styles.saveIcon, Lan.Save), EditorStyles.toolbarButton, GUILayout.Width(26)))
            //{
            //    SaveCurrentAsset();
            //}

            if (
                GUILayout.Button(
                    new GUIContent(Styles.settingsIcon, Lan.OpenPreferencesTips),
                    EditorStyles.toolbarButton,
                    GUILayout.Width(26)
                )
            )
            {
                PreferencesWindow.Show(
                    new Rect(view.ScreenWidth - 5 - 400, Styles.TOOLBAR_HEIGHT + 5, 400, view.ScreenHeight - Styles.TOOLBAR_HEIGHT - 50)
                );
            }
        }
    }
}
