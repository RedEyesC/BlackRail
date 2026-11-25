using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TrackEditor.Draws;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TrackEditor
{
    public partial class TrackEditorWindow : EditorWindow
    {
        [NonSerialized]
        private float editorPreviousTime;
        private bool previewRequested;

        [NonSerialized]
        internal bool WillRepaint;

        [SerializeField]
        internal bool isLocked;

        [NonSerialized] public Asset asset;

        [SerializeField]
        public UnityEngine.Object assetSource;

        [SerializeField] private GameObject previewTarget;
        private InspectorPreviewAsset inspectorSelection;
        private string appliedSnapshot;
        private bool editPending;
        private bool assetDirty;
        private double nextAutoSave;
        internal Clip CopiedClip { get; private set; }

        internal DirectableAsset SelectedObject
        {
            get => inspectorSelection.SelectedObject;
            set
            {
                SetTimelineSelection(value ?? asset);
                if (SelectedObject != null && Selection.activeObject != inspectorSelection)
                {
                    Selection.activeObject = inspectorSelection;
                }
            }
        }

        private void SetTimelineSelection(DirectableAsset selection)
        {
            if (!(selection is Clip clip) || !selectedClips.Contains(clip))
            {
                selectedClips.Clear();
                if (selection is Clip single) selectedClips.Add(single);
            }

            inspectorSelection.SelectedObject = selection;
            WillRepaint = true;
        }

        #region Window

        private static readonly Type[] PreferredDockTabs =
        {
            typeof(AnimationWindow),
            typeof(EditorWindow).Assembly.GetType("UnityEditor.ProjectBrowser"),
            typeof(EditorWindow).Assembly.GetType("UnityEditor.ConsoleWindow"),
            typeof(SceneView)
        };

        [MenuItem("Window/Track Editor")]
        public static void ShowWindow()
        {
            ShowWindow<TrackEditorWindow>();
        }

        public static void ShowWindow<TWindow>(UnityEngine.Object source = null)
            where TWindow : TrackEditorWindow => TryShowWindow<TWindow>(source);

        internal static bool TryShowWindow<TWindow>(UnityEngine.Object source)
            where TWindow : TrackEditorWindow
        {
            var existingWindow = FindWindow<TWindow>();
            var window = existingWindow ?? CreateDockedWindow<TWindow>();
            if (source == null)
            {
                window.InitializeAll();
            }
            else if (!window.TryOpenAsset(source))
            {
                if (existingWindow == null) window.Close();
                return false;
            }

            window.SelectedObject = source != null ? window.asset : window.SelectedObject;
            window.Focus();
            return true;
        }

        private static TWindow CreateDockedWindow<TWindow>() where TWindow : TrackEditorWindow
        {
            var dockedTabTypes = Resources.FindObjectsOfTypeAll<EditorWindow>()
                .Where(window => window.docked)
                .Select(window => window.GetType())
                .ToArray();
            var dockTargets = PreferredDockTabs.Where(dockedTabTypes.Contains)
                .Concat(dockedTabTypes)
                .Distinct()
                .ToArray();
            return CreateWindow<TWindow>(dockTargets);
        }

        private static TWindow FindWindow<TWindow>() where TWindow : TrackEditorWindow =>
            Resources.FindObjectsOfTypeAll<TWindow>().FirstOrDefault(window => window.GetType() == typeof(TWindow));

        public static void CloseWindow()
        {
            CloseWindow<TrackEditorWindow>();
        }

        protected static void CloseWindow<TWindow>()
            where TWindow : TrackEditorWindow
        {
            FindWindow<TWindow>()?.Close();
        }

        #endregion

        #region 生命周期接口

        void OnEnable()
        {
            Styles.Load();
            view.LeftMargin = Styles.LEFT_MARGIN;
            inspectorSelection = CreateInstance<InspectorPreviewAsset>();
            inspectorSelection.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
            InspectorPreviewAssetInspector.SelectionEdited += OnInspectorSelectionEdited;
            Undo.undoRedoPerformed += RestoreUndoSnapshot;

            EditorSceneManager.sceneSaving -= OnWillSaveScene;
            EditorSceneManager.sceneSaving += OnWillSaveScene;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;

            Selection.selectionChanged += OnSelectionChanged;

            Tools.hidden = false;
            ApplyWindowTitle();
            minSize = new Vector2(500, 250);

            WillRepaint = true;

            pendingGuides = new List<GuideLine>();

            RestoreEditorState();
        }

        void OnDisable()
        {
            EditorSceneManager.sceneSaving -= OnWillSaveScene;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

            EditorApplication.update -= OnEditorUpdate;

            Selection.selectionChanged -= OnSelectionChanged;
            InspectorPreviewAssetInspector.SelectionEdited -= OnInspectorSelectionEdited;
            Undo.undoRedoPerformed -= RestoreUndoSnapshot;
            FlushEdits();
            DestroyImmediate(inspectorSelection);

            Tools.hidden = false;
            if (asset != null && !Application.isPlaying)
            {
                Stop(true);
            }
        }

        void OnPlay()
        {
            editorPreviousTime = Time.realtimeSinceStartup;
        }

        void OnStop()
        {
            WillRepaint = true;
        }

        void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                if (asset != null)
                {
                    Stop(true);
                }

                return;
            }

            if (state == PlayModeStateChange.EnteredEditMode)
            {
                RestoreEditorState();
            }
        }

        void OnEditorUpdate()
        {
            if (asset != null && !Application.isPlaying)
            {
                if (EditorApplication.isCompiling)
                {
                    if (previewRequested)
                    {
                        Stop(true);
                    }
                }
                else
                {
                    CommitEdits();
                    if (assetDirty && !itemDragger.IsDragging && Prefs.autoSaveSeconds > 0 && EditorApplication.timeSinceStartup >= nextAutoSave)
                    {
                        nextAutoSave = EditorApplication.timeSinceStartup + Prefs.autoSaveSeconds;
                        TrySaveCurrentAsset();
                    }

                    UpdatePreview();
                }
            }

            if (WillRepaint)
            {
                WillRepaint = false;
                Repaint();
            }
        }

        private void UpdatePreview()
        {
            if (sortClips && !itemDragger.IsDragging)
            {
                SortClips();
            }

            if (IsPlay)
            {
                var now = Time.realtimeSinceStartup;
                var delta = (now - editorPreviousTime) * Time.timeScale;
                editorPreviousTime = now;
                var time = player.CurrentTime + (EditorPlaybackState == EditorPlaybackState.PlayingForwards ? delta : -delta);
                if (time >= asset.Length && EditorPlaybackWrapMode == WrapMode.Loop)
                {
                    player.Sample(asset.Length);
                    player.RevertPreview();
                    time = Mathf.Repeat(time, asset.Length);
                }
                else if (EditorPlaybackState == EditorPlaybackState.PlayingForwards ? time >= asset.Length : time <= 0)
                {
                    Pause();
                }

                player.CurrentTime = time;
            }

            if (previewRequested)
            {
                WillRepaint |= player.Sample();
            }
        }

        void OnGUI()
        {
            GUI.skin.label.richText = true;
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
            EditorStyles.label.richText = true;
            EditorStyles.textField.wordWrap = true;
            EditorStyles.foldout.richText = true;
            var e = Event.current;
            if (e.commandName == "ObjectSelectorClosed" && EditorGUIUtility.GetObjectPickerControlID() == GetInstanceID())
            {
                if (TryOpenAsset(EditorGUIUtility.GetObjectPickerObject()))
                {
                    SelectedObject = asset;
                }

                e.Use();
            }

            if (asset == null)
            {
                return;
            }

            //避免编译时编辑
            if (EditorApplication.isCompiling)
            {
                Stop(true);
                ShowNotification(new GUIContent(Lan.CompilingTips));
                return;
            }

            if (e.type == EventType.MouseDown)
            {
                RemoveNotification();
            }

            view.Reset(position, GetTrackContentHeight());
            DrawHeaderGUI();

            if (asset == null)
                return;

            DrawHeaderTimeInfoGUI();

            DrawTimelineInfo();

            DrawBottomGUI();

            var ruler = view.TopMiddleRect;
            EditorGUI.DrawRect(new Rect(ruler.xMin - 1, ruler.yMin, 1, view.ScreenHeight - ruler.yMin), Color.black);

            DoKeyboardShortcuts();
        }

        void OnWillSaveScene(UnityEngine.SceneManagement.Scene scene, string path)
        {
            if (previewRequested)
            {
                Stop(true);
                Debug.LogWarning(
                    "Scene Saved while a cutscene was in preview mode. Cutscene was reverted before saving the scene along with changes it affected."
                );
            }
        }

        void OnSelectionChanged()
        {
            if (!(Selection.activeObject is InspectorPreviewAsset) && !isLocked && (asset == null || focusedWindow == this))
            {
                TryOpenAsset(GetSelectedAssetSource());
            }
        }

        internal bool TryOpenAsset(UnityEngine.Object source)
        {
            if (source == null || (source == assetSource && asset != null))
            {
                return asset != null && source == assetSource;
            }

            Asset selected;
            try
            {
                selected = LoadAssetFromSource(source);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return false;
            }

            if (selected == null || !FlushEdits())
            {
                return false;
            }

            asset = selected;
            assetSource = source;
            BeginDocument();
            return true;
        }

        protected virtual UnityEngine.Object GetSelectedAssetSource() => Selection.activeObject;

        protected virtual Type AssetType => typeof(Asset);

        protected virtual Asset LoadAssetFromSource(UnityEngine.Object source) =>
            CreateAssetStorage().Load(source, AssetType);

        protected virtual void SaveCurrentAsset() => CreateAssetStorage().Save(asset, assetSource);

        protected virtual IAssetStorage CreateAssetStorage() => AssetStorage.Default;

        private bool FlushEdits()
        {
            CommitEdits(force: true);
            return !assetDirty || TrySaveCurrentAsset();
        }

        private bool TrySaveCurrentAsset()
        {
            try
            {
                SaveCurrentAsset();
                assetDirty = false;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return false;
            }
        }

        private void BeginDocument()
        {
            Undo.ClearUndo(inspectorSelection);
            appliedSnapshot = asset == null ? null : TimelineJson.Serialize(asset);
            inspectorSelection.Snapshot = appliedSnapshot;
            editPending = false;
            assetDirty = false;
            RebuildPlayer();
        }

        private void CommitEdits(bool force = false)
        {
            if (!editPending || (!force && (itemDragger.IsDragging || isMovingEndCarret)))
            {
                return;
            }

            var snapshot = TimelineJson.Serialize(asset);
            editPending = false;
            if (snapshot == appliedSnapshot)
            {
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.RegisterCompleteObjectUndo(inspectorSelection, "Edit Timeline");
            inspectorSelection.Snapshot = appliedSnapshot = snapshot;
            EditorUtility.SetDirty(inspectorSelection);
            assetDirty = true;
            nextAutoSave = EditorApplication.timeSinceStartup + Prefs.autoSaveSeconds;
        }

        private void RestoreUndoSnapshot()
        {
            var snapshot = inspectorSelection.Snapshot;
            if (asset == null || snapshot == appliedSnapshot || string.IsNullOrEmpty(snapshot))
            {
                return;
            }

            var time = player.CurrentTime;
            var hadPreview = previewRequested;
            asset = TimelineJson.Deserialize(snapshot, AssetType);
            appliedSnapshot = snapshot;
            assetDirty = true;
            editPending = false;
            nextAutoSave = EditorApplication.timeSinceStartup + Prefs.autoSaveSeconds;
            RebuildPlayer();
            if (hadPreview)
            {
                Seek(time);
            }
        }

        protected virtual string CreateWindowTitle()
        {
            return Lan.Title;
        }

        private void ApplyWindowTitle()
        {
            titleContent = new GUIContent(CreateWindowTitle(), Styles.cutsceneIconOpen);
        }

        private void RestoreEditorState()
        {
            if (asset == null && assetSource != null)
            {
                try
                {
                    asset = LoadAssetFromSource(assetSource);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }

            if (appliedSnapshot == null)
            {
                BeginDocument();
            }
            else
            {
                RebuildPlayer();
            }
        }

        protected virtual GameObject FindPreviewTarget() => null;

        protected virtual AnimationClip ResolvePreviewAnimation(string path) =>
            AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

        protected virtual void OnPreviewSignal(Clip signal, bool reverse) { }

        private void RebuildPlayer()
        {
            Stop(true);
            player = new AssetPlayer(asset, new PreviewContext(
                () => previewTarget != null ? previewTarget : FindPreviewTarget(),
                ResolvePreviewAnimation));
            player.SignalCrossed += OnPreviewSignal;

            SetTimelineSelection(asset);
            itemDragger.Reset();
            multiSelectStartPos = null;
            pickedGroup = null;
            clipWrappers.Clear();
            durationSnapTimes = null;
            view.ScrollPos = Vector2.zero;
            sortClips = true;

            if (asset != null && (assetSource == null || !viewTimeByAsset.TryGetValue(assetSource, out viewTimeRange)))
            {
                SetViewTimeRange(0, Mathf.Max(5, asset.Length));
            }
        }

        private void OnInspectorSelectionEdited(InspectorPreviewAsset selection)
        {
            if (selection == inspectorSelection)
            {
                NotifyTimelineChanged(rebuildPreview: true);
            }
        }

        internal void NotifyTimelineChanged(bool rebuildPreview = false)
        {
            editPending = true;
            sortClips = true;
            if (rebuildPreview)
            {
                PruneClipWrappers();
            }

            player.InvalidatePreview(rebuildPreview);
            inspectorSelection.RequestRepaint();
            WillRepaint = true;
        }

        #endregion

        #region Init

        public void InitializeAll() => TryOpenAsset(GetSelectedAssetSource());

        #endregion

        #region 快捷键

        /// <summary>
        /// 监听执行鼠标快捷键
        /// </summary>
        void DoKeyboardShortcuts()
        {
            var e = Event.current;
            var isCtrl = e.control;
            var isShift = e.shift;
            var isAlt = e.alt;
            if (e.type == EventType.KeyDown && GUIUtility.keyboardControl == 0)
            {
                //Ctrl +
                if (isCtrl)
                {
                    if (e.keyCode == KeyCode.S && !itemDragger.IsDragging)
                    {
                        FlushEdits();
                        e.Use();
                    }
                }

                //play
                if (e.keyCode == KeyCode.Space && !e.shift)
                {
                    TogglePlayback();
                    e.Use();
                }

                if (e.keyCode == KeyCode.Escape)
                {
                    if (itemDragger.IsDragging)
                    {
                        if (itemDragger.Cancel()) NotifyTimelineChanged();
                        ReleaseItemDragControl();
                    }
                    else if (multiSelectStartPos.HasValue)
                    {
                        multiSelectStartPos = null;
                        ReleaseItemDragControl();
                    }
                    else
                    {
                        Stop(true);
                    }

                    e.Use();
                }

                //下一帧
                if (e.keyCode == KeyCode.Period || e.keyCode == KeyCode.RightArrow)
                {
                    StepForward();
                    e.Use();
                }

                //上一帧
                if (e.keyCode == KeyCode.Comma || e.keyCode == KeyCode.LeftArrow)
                {
                    StepBackward();
                    e.Use();
                }
            }
        }

        #endregion

        #region 时间轴

        private static bool isProSkin => EditorGUIUtility.isProSkin;

        private Action postWindowsGUI;

        private readonly G view = new G();
        private readonly Dictionary<UnityEngine.Object, Vector2> viewTimeByAsset = new Dictionary<UnityEngine.Object, Vector2>();
        private readonly Dictionary<Group, bool> collapsedGroups = new Dictionary<Group, bool>();
        private Vector2 viewTimeRange;
        private float ViewTimeMin => viewTimeRange.x;
        private float ViewTimeMax => viewTimeRange.y;
        private float ViewTime => ViewTimeMax - ViewTimeMin;

        private float TimeToPos(float time) => view.CenterRect.TimeToPos(viewTimeRange, time);
        private float PosToTime(float pos) => view.CenterRect.PosToTime(viewTimeRange, pos);
        private bool IsGroupCollapsed(Group group) =>
            collapsedGroups.TryGetValue(group, out var collapsed) ? collapsed : group.IsCollapsed;

        private bool sortClips = true;

        internal Vector2 mousePosition;

        private readonly Dictionary<Clip, ActionClipWrapper> clipWrappers = new Dictionary<Clip, ActionClipWrapper>();

        private static readonly int ItemDragControlHint = "TrackEditorItemDrag".GetHashCode();
        private readonly ItemDragger itemDragger = new ItemDragger();
        private readonly List<Clip> selectedClips = new List<Clip>();
        private int itemDragControl;
        private Group trackDropGroup;
        private int trackDropIndex;
        private Vector2? multiSelectStartPos;
        private readonly List<Clip> marqueeHits = new List<Clip>();

        internal List<GuideLine> pendingGuides;
        private float[] durationSnapTimes;
        private float MagnetTolerance => ViewTime * 10 / CenterRect.width;

        private Section draggedSection;

        private bool isMovingEndCarret;
        private bool isMouseButton2Down;

        private bool wheelZoomActive;
        private float wheelZoomFocalTime;
        private float wheelZoomMouseX;

        private Rect CenterRect;
        private GUIStyle timelineVerticalScrollbar;
        private float TimelineBottom => view.ScreenHeight - Styles.BOTTOM_HEIGHT;
        private Rect TimelineViewport => Rect.MinMaxRect(CenterRect.xMin, CenterRect.yMin, CenterRect.xMax, TimelineBottom);

        public void DrawTimelineInfo()
        {
            var e = Event.current;

            if (e.button == 2 && e.type == EventType.MouseDown)
            {
                isMouseButton2Down = true;
            }

            if (e.button == 2 && e.rawType == EventType.MouseUp)
            {
                isMouseButton2Down = false;
            }

            mousePosition = e.mousePosition;
            CenterRect = view.CenterRect;

            itemDragControl = GUIUtility.GetControlID(ItemDragControlHint, FocusType.Passive);
            trackDropGroup = null;
            if (itemDragger.IsDragging)
            {
                UpdateItemDrag(e);
            }
            else
            {
                DoScrubControls();
                DoZoomAndPan();
            }

            var scrollView = Rect.MinMaxRect(0, CenterRect.yMin, view.ScreenWidth, TimelineBottom);
            var contentRect = new Rect(0, scrollView.y, CenterRect.xMax, view.ContentHeight);
            if (timelineVerticalScrollbar == null)
            {
                timelineVerticalScrollbar = new GUIStyle(GUI.skin.verticalScrollbar)
                {
                    fixedWidth = Styles.VERTICAL_SCROLLBAR_WIDTH,
                    margin = new RectOffset()
                };
            }

            view.ScrollPos = GUI.BeginScrollView(scrollView, view.ScrollPos, contentRect, GUIStyle.none, timelineVerticalScrollbar);
            CenterRect.height = scrollView.height + view.ScrollPos.y;

            DrawGroupAndTrackListGUI();

            ShowTimeLines(CenterRect);
            GUI.EndScrollView();
            FinishItemDrag(e);

            if (e.type == EventType.MouseDown && e.button == 0 && GUIUtility.hotControl == 0)
            {
                if (TimelineViewport.Contains(mousePosition))
                {
                    SelectedObject = null;
                }

                GUIUtility.keyboardControl = 0;
            }

            if (e.type == EventType.MouseDrag || e.type == EventType.MouseUp || GUI.changed)
            {
                WillRepaint = true;
            }

            Handles.color = Color.black;
            Handles.DrawLine(new Vector2(0, CenterRect.y + 1), new Vector2(CenterRect.xMax, CenterRect.y + 1));
            Handles.color = Color.white;
            DrawGuides();

            GUI.color = Color.white;
            GUI.backgroundColor = Color.white;
            GUI.skin = null;
        }

        private void UpdateItemDrag(Event e)
        {
            if (!itemDragger.IsClipDrag || e.type != EventType.MouseDrag || GUIUtility.hotControl != itemDragControl)
            {
                return;
            }

            var changed = itemDragger.UpdateClips(
                PosToTime(mousePosition.x), !e.control, e.shift, Prefs.magnetSnapping && !e.control, MagnetTolerance);
            if (changed) NotifyTimelineChanged();
            e.Use();
        }

        private void FinishItemDrag(Event e)
        {
            if (!itemDragger.IsDragging || e.rawType != EventType.MouseUp || e.button != 0)
            {
                return;
            }

            if (trackDropGroup != null && itemDragger.DropTrack(trackDropGroup, trackDropIndex))
            {
                collapsedGroups[trackDropGroup] = false;
                NotifyTimelineChanged(rebuildPreview: true);
            }

            itemDragger.Reset();
            ReleaseItemDragControl();
            if (e.type != EventType.Used) e.Use();
        }

        private void CaptureItemDragControl()
        {
            GUIUtility.keyboardControl = 0;
            GUIUtility.hotControl = itemDragControl;
        }

        private void ReleaseItemDragControl()
        {
            if (GUIUtility.hotControl == itemDragControl) GUIUtility.hotControl = 0;
            WillRepaint = true;
        }

        private void BeginClipDrag(ItemDragType type, Clip clip)
        {
            IEnumerable<Clip> items =
                type == ItemDragType.Position || type == ItemDragType.ScaleStart || type == ItemDragType.ScaleEnd
                    ? selectedClips : new[] { clip };
            itemDragger.BeginClips(type, clip, items, asset, PosToTime(mousePosition.x), player.CurrentTime);
            CaptureItemDragControl();
        }

        private void SelectClip(Clip clip, bool toggle)
        {
            if (toggle && selectedClips.Remove(clip))
            {
                SelectedObject = selectedClips.LastOrDefault();
                return;
            }

            if (toggle) selectedClips.Add(clip);
            SelectedObject = clip;
        }

        private void DoMultiSelection(Event e)
        {
            if (multiSelectStartPos.HasValue)
            {
                var start = multiSelectStartPos.Value;
                if ((start - e.mousePosition).sqrMagnitude > 100)
                {
                    var area = Rect.MinMaxRect(Mathf.Min(start.x, e.mousePosition.x), Mathf.Min(start.y, e.mousePosition.y),
                        Mathf.Max(start.x, e.mousePosition.x), Mathf.Max(start.y, e.mousePosition.y));
                    marqueeHits.Clear();
                    foreach (var pair in clipWrappers)
                    {
                        if (!pair.Key.IsLocked && pair.Value.rect.width > 0 && area.Overlaps(pair.Value.rect))
                        {
                            marqueeHits.Add(pair.Key);
                            if (e.type == EventType.Repaint) DrawClipSelection(pair.Value.rect);
                        }
                    }

                    if (e.type == EventType.Repaint)
                    {
                        GUI.Box(area, string.Empty, Styles.hollowFrameStyle);
                        EditorGUI.DrawRect(area, Color.white.WithAlpha(0.05f));
                    }

                    if (e.rawType == EventType.MouseUp && e.button == 0)
                    {
                        selectedClips.Clear();
                        selectedClips.AddRange(marqueeHits);
                        SelectedObject = selectedClips.LastOrDefault();
                    }
                }

                if (e.rawType == EventType.MouseUp && e.button == 0)
                {
                    multiSelectStartPos = null;
                    ReleaseItemDragControl();
                    if (e.type != EventType.Used) e.Use();
                }

                return;
            }

            if (e.type == EventType.MouseDown && e.button == 0 && GUIUtility.hotControl == 0 && TimelineViewport.Contains(mousePosition))
            {
                SelectedObject = null;
                multiSelectStartPos = e.mousePosition;
                CaptureItemDragControl();
                e.Use();
            }
        }

        private void DrawSelectionHandles(Event e)
        {
            if (selectedClips.Count < 2)
            {
                return;
            }

            var rects = selectedClips.Where(clip => !clip.IsLocked && clipWrappers.ContainsKey(clip))
                .Select(clip => clipWrappers[clip].rect).Where(rect => rect.width > 0).ToArray();
            if (rects.Length < 2)
            {
                return;
            }

            var bounds = RectUtility.GetBoundRect(rects).ExpandBy(4);
            var left = new Rect(bounds.xMin - 6, bounds.yMin, 4, bounds.height);
            var right = new Rect(bounds.xMax + 2, bounds.yMin, 4, bounds.height);
            AddCursorRect(left, MouseCursor.ResizeHorizontal);
            AddCursorRect(right, MouseCursor.ResizeHorizontal);
            var color = isProSkin ? new Color(0.7f, 0.7f, 0.7f) : Color.grey;
            EditorGUI.DrawRect(left, color);
            EditorGUI.DrawRect(right, color);
            if (e.type == EventType.MouseDown && e.button == 0 && GUIUtility.hotControl == 0
                && (left.Contains(e.mousePosition) || right.Contains(e.mousePosition)))
            {
                BeginClipDrag(left.Contains(e.mousePosition) ? ItemDragType.ScaleStart : ItemDragType.ScaleEnd,
                    selectedClips.First(clip => !clip.IsLocked && clip.Parent != null));
                e.Use();
            }
        }

        void DrawGuides()
        {
            var height = TimelineViewport.height;
            //区间开始时间参考线
            DrawGuideLine(0, isProSkin ? Color.gray : Color.black, height);

            //区间结束时间参考线
            DrawGuideLine(asset.Length, Styles.DurationMarker, height);

            // 开始和结束时间标线
            var activeClip = itemDragger.ActiveClip;
            if (activeClip != null)
            {
                var color = Color.yellow.WithAlpha(0.2f);
                if (itemDragger.Type == ItemDragType.Position || itemDragger.Type == ItemDragType.TrimStart)
                    DrawGuideLine(activeClip.StartTime, color, height);
                if (itemDragger.Type == ItemDragType.Position || itemDragger.Type == ItemDragType.TrimEnd)
                    DrawGuideLine(activeClip.EndTime, color, height);
            }

            if (itemDragger.SnappedTime.HasValue)
            {
                DrawGuideLine(itemDragger.SnappedTime.Value, Color.white, height);
            }

            if (draggedSection != null)
            {
                DrawGuideLine(draggedSection.time, draggedSection.color, height);
            }

            for (var i = 0; i < pendingGuides.Count; i++)
            {
                DrawGuideLine(pendingGuides[i].time, pendingGuides[i].color, height);
            }

            pendingGuides.Clear();
            DrawGuideLine(player.CurrentTime, player.GetScriberColor(), height);
        }

        /// <summary>
        /// 绘制垂直参考线
        /// </summary>
        /// <param name="time"></param>
        /// <param name="color"></param>
        void DrawGuideLine(float time, Color color, float height)
        {
            if (IsTimeVisible(time))
            {
                EditorGUI.DrawRect(new Rect(CenterRect.x + TimeToGuideX(time), CenterRect.y, 1, height), color);
            }
        }

        /// <summary>
        /// 添加拖动游标
        /// </summary>
        internal void AddCursorRect(Rect rect, MouseCursor type)
        {
            EditorGUIUtility.AddCursorRect(rect, type);
            WillRepaint = true;
        }

        #endregion

        #region 刻度控制

        /// <summary>
        /// 时间轴刻度控制
        /// </summary>
        void DoScrubControls()
        {
            if (Application.isPlaying)
            {
                return;
            }

            var e = Event.current;
            var ruler = view.TopMiddleRect;
            var control = GUIUtility.GetControlID(FocusType.Passive, ruler);
            var overPlayhead = IsTimeVisible(player.CurrentTime) && PlayheadRect(ruler).Contains(mousePosition);
            var overEndHandle = IsTimeVisible(asset.Length) && !overPlayhead && DurationHandleRect(ruler).ExpandBy(4, 0).Contains(mousePosition);
            var movesEnd = GUIUtility.hotControl == control ? isMovingEndCarret : overEndHandle;
            EditorGUIUtility.AddCursorRect(ruler, movesEnd ? MouseCursor.MoveArrow : MouseCursor.Arrow);

            if (e.type == EventType.MouseDown && ruler.Contains(mousePosition))
            {
                if (e.button == 0 && GUIUtility.hotControl == 0)
                {
                    GUIUtility.hotControl = control;
                    GUIUtility.keyboardControl = 0;
                    isMovingEndCarret = overEndHandle;
                    if (isMovingEndCarret)
                    {
                        Pause();
                        durationSnapTimes = ItemDragger.GetSnapTimes(asset, player.CurrentTime);
                    }
                }
                else if (e.button == 1 && overEndHandle)
                {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Set To Last Clip Time"), false, () =>
                    {
                        var clips = asset.groups.SelectMany(group => group.Tracks).SelectMany(track => track.Clips);
                        SetSequenceLength(clips.Select(clip => clip.EndTime).DefaultIfEmpty(asset.Length).Max());
                    });
                    menu.ShowAsContext();
                    e.Use();
                }
            }

            if (GUIUtility.hotControl != control)
            {
                return;
            }

            if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
            {
                var time = Mathf.Clamp(G.SnapTime(PosToTime(mousePosition.x)), ViewTimeMin, ViewTimeMax);
                if (isMovingEndCarret)
                {
                    if (e.type == EventType.MouseDrag)
                    {
                        var snapped = Prefs.magnetSnapping && !e.control ? ItemDragger.FindSnap(time, durationSnapTimes, MagnetTolerance) : null;
                        SetSequenceLength(snapped ?? time);
                    }
                }
                else
                {
                    Seek(time, reveal: false);
                }

                e.Use();
            }
            else if (e.rawType == EventType.MouseUp)
            {
                GUIUtility.hotControl = 0;
                isMovingEndCarret = false;
                e.Use();
            }
        }

        private void SetSequenceLength(float length)
        {
            asset.Length = length;
            NotifyTimelineChanged();
        }

        /// <summary>
        /// 时间轴缩放和平移操作
        /// </summary>
        void DoZoomAndPan()
        {
            if (!TimelineViewport.Contains(mousePosition))
            {
                wheelZoomActive = false;
                return;
            }

            var e = Event.current;

            if (e.type == EventType.ScrollWheel && Prefs.scrollWheelZooms)
            {
                if (Mathf.Abs(e.delta.y) < 0.00001f)
                {
                    wheelZoomActive = false;
                    return;
                }

                if (!wheelZoomActive || Mathf.Abs(wheelZoomMouseX - mousePosition.x) > 1f)
                {
                    wheelZoomMouseX = mousePosition.x;
                    wheelZoomFocalTime = PosToTime(mousePosition.x);
                }

                var zoomFactor = 1f - e.delta.y * 0.02f;
                ZoomTimeRange(zoomFactor, wheelZoomFocalTime);

                wheelZoomActive = true;
                WillRepaint = true;
                e.Use();
                return;
            }

            if (e.isMouse && e.type != EventType.Repaint)
            {
                wheelZoomActive = false;
            }

            // Alt + 鼠标右键拖动缩放
            if (e.alt && !e.shift && e.button == 1)
            {
                AddCursorRect(CenterRect, MouseCursor.Zoom);
                if (
                    e.type == EventType.MouseDrag
                    || e.type == EventType.MouseDown
                    || e.type == EventType.MouseUp
                )
                {
                    var focalTime = PosToTime(mousePosition.x);
                    ZoomTimeRange(1f - e.delta.x * 0.01f, focalTime);
                    e.Use();
                }
            }

            //左/右，上/下
            if (isMouseButton2Down || (e.alt && !e.shift && e.button == 0))
            {
                AddCursorRect(CenterRect, MouseCursor.Pan);
                if (e.type == EventType.MouseDrag || e.type == EventType.MouseDown || e.type == EventType.MouseUp)
                {
                    var offset = Mathf.Max(-ViewTimeMin, -e.delta.x / CenterRect.width * ViewTime);
                    SetViewTimeRange(ViewTimeMin + offset, ViewTimeMax + offset);
                    view.ScrollPos.y -= e.delta.y;
                    e.Use();
                }
            }
        }

        private void ZoomTimeRange(float zoomFactor, float focalTime)
        {
            const float minViewTime = 0.25f;

            if (zoomFactor <= 0f)
            {
                return;
            }

            var timeMin = ViewTimeMin;
            var timeMax = ViewTimeMax;
            var focal = Mathf.Max(focalTime, timeMin);
            var zoomedTimeMin = (timeMin + focal * (zoomFactor - 1f)) / zoomFactor;
            var zoomedTimeMax = (timeMax + focal * (zoomFactor - 1f)) / zoomFactor;

            if (zoomedTimeMax - zoomedTimeMin < minViewTime)
            {
                return;
            }

            var offset = Mathf.Max(0, -zoomedTimeMin);
            SetViewTimeRange(zoomedTimeMin + offset, zoomedTimeMax + offset);
        }

        #endregion

        #region 时间轴

        //初始化动作剪辑包装器
        private void PruneClipWrappers()
        {
            var clips = new HashSet<Clip>(asset.groups.SelectMany(group => group.Tracks).SelectMany(track => track.Clips));
            foreach (var removed in clipWrappers.Keys.Where(clip => !clips.Contains(clip)).ToArray())
            {
                clipWrappers.Remove(removed);
            }

            selectedClips.RemoveAll(clip => !clips.Contains(clip));

            if (SelectedObject is Clip clip && !clips.Contains(clip)
                || SelectedObject is Track track && (track.Parent == null || !asset.groups.Contains(track.Parent) || !track.Parent.Tracks.Contains(track))
                || SelectedObject is Group group && !asset.groups.Contains(group))
            {
                SetTimelineSelection(asset);
            }
        }

        private void SortClips()
        {
            foreach (var group in asset.groups)
            {
                foreach (var track in group.Tracks)
                {
                    track.SortClips();
                }
            }

            sortClips = false;
        }

        /// <summary>
        /// 中间区域-时间轴显示区域
        /// </summary>
        /// <param name="centerRect"></param>
        void ShowTimeLines(Rect centerRect)
        {
            var e = Event.current;
            var bgRect = Rect.MinMaxRect(
                centerRect.xMin,
                Styles.TOP_MARGIN + Styles.TOOLBAR_HEIGHT + view.ScrollPos.y,
                centerRect.xMax,
                view.ScreenHeight - Styles.TOOLBAR_HEIGHT + view.ScrollPos.y
            );
            GUI.color = Color.black.WithAlpha(0.1f);
            GUI.DrawTexture(bgRect, Styles.whiteTexture);
            GUI.color = Color.black.WithAlpha(0.03f);
            GUI.DrawTextureWithTexCoords(bgRect, Styles.stripes, new Rect(0, 0, bgRect.width / -7, bgRect.height / -7));
            GUI.color = Color.white;

            // 绘制时间参考线
            for (var tick = view.FirstTick; tick <= view.LastTick; tick++)
            {
                DrawGuideLine(tick * view.TickInterval, Color.black.WithAlpha(0.05f), CenterRect.height);
            }

            GUI.BeginGroup(centerRect);
            if (e.type == EventType.MouseDown) DrawSelectionHandles(e);

            var nextYPos = Styles.FIRST_GROUP_TOP_MARGIN;

            BeginWindows();

            for (int g = 0; g < asset.groups.Count; g++)
            {
                var group = asset.groups[g];
                ShowGroupArea(group, e, ref nextYPos);
            }

            EndWindows();

            if (postWindowsGUI != null)
            {
                postWindowsGUI();
                postWindowsGUI = null;
            }

            //片段多选
            if (e.type == EventType.Repaint) DrawSelectionHandles(e);
            DoMultiSelection(e);

            GUI.EndGroup();

            GUI.color = Color.white.WithAlpha(0.2f);
            GUI.Box(bgRect, string.Empty, Styles.shadowBorderStyle);
            GUI.color = Color.white;

            //超出范围的变暗
            if (ViewTimeMax > asset.Length)
            {
                var endPos = Mathf.Max(TimeToPos(asset.Length) + view.LeftRect.width, centerRect.xMin);
                var darkRect = Rect.MinMaxRect(endPos, centerRect.yMin, centerRect.xMax, centerRect.yMax);
                EditorGUI.DrawRect(darkRect, Color.black.WithAlpha(isProSkin ? 0.22f : 0.12f));
            }
            //超出范围的变暗
            if (ViewTimeMin < 0)
            {
                var startPos = Mathf.Min(TimeToPos(0) + view.LeftRect.width, centerRect.xMax);
                var darkRect = Rect.MinMaxRect(centerRect.xMin, centerRect.yMin, startPos, centerRect.yMax);
                GUI.color = Color.black.WithAlpha(0.3f);
                GUI.Box(darkRect, string.Empty, (GUIStyle)"TextField");
                GUI.color = Color.white;
            }

        }

        void ShowGroupArea(Group group, Event e, ref float nextYPos)
        {
            if (view.IsFilteredOutBySearch(group))
            {
                return;
            }

            var groupRect = Rect.MinMaxRect(
                Mathf.Max(TimeToPos(ViewTimeMin), TimeToPos(0)),
                nextYPos,
                TimeToPos(ViewTimeMax),
                nextYPos + Styles.GROUP_HEIGHT
            );
            nextYPos += Styles.GROUP_HEIGHT;

            if (IsGroupCollapsed(group))
            {
                GUI.color = Color.black.WithAlpha(0.15f);
                var collapseRect = Rect.MinMaxRect(groupRect.xMin + 2, groupRect.yMin + 2, groupRect.xMax, groupRect.yMax - 4);
                GUI.DrawTexture(collapseRect, Styles.whiteTexture);
                GUI.color = Color.grey.WithAlpha(0.5f);
                foreach (var track in group.Tracks)
                {
                    foreach (var clip in track.Clips)
                    {
                        var start = TimeToPos(clip.StartTime);
                        var end = TimeToPos(clip.EndTime);
                        GUI.DrawTexture(
                            Rect.MinMaxRect(start + 0.5f, collapseRect.y + 2, end - 0.5f, collapseRect.yMax - 2),
                            Styles.whiteTexture
                        );
                    }
                }

                GUI.color = Color.white;
                return;
            }

            for (int t = 0; t < group.Tracks.Count; t++)
            {
                var track = group.Tracks[t];
                ShowTrackArea(track, e, ref nextYPos);
            }

            if (ReferenceEquals(SelectedObject, group))
            {
                var r = Rect.MinMaxRect(groupRect.xMin, groupRect.yMin, groupRect.xMax, nextYPos);
                GUI.color = Color.grey;
                GUI.Box(r, string.Empty, Styles.hollowFrameHorizontalStyle);
                GUI.color = Color.white;
            }
        }

        private bool IsTrackSelected(Track track) => ReferenceEquals(SelectedObject, track) || track == itemDragger.ActiveTrack;

        void ShowTrackArea(Track track, Event e, ref float nextYPos)
        {
            var yPos = nextYPos;

            var trackPosRect = Rect.MinMaxRect(
                Mathf.Max(TimeToPos(ViewTimeMin), TimeToPos(track.StartTime)),
                yPos,
                TimeToPos(ViewTimeMax),
                yPos + track.ShowHeight
            );

            nextYPos += track.ShowHeight + Styles.TRACK_MARGINS;

            var trackColor = IsTrackSelected(track) ? Styles.TrackLaneSelection : Color.black.WithAlpha(isProSkin ? 0.06f : 0.1f);
            EditorGUI.DrawRect(trackPosRect, trackColor);
            Handles.color = ColorUtility.Grey(isProSkin ? 0.15f : 0.4f);
            Handles.DrawLine(
                new Vector2(TimeToPos(ViewTimeMin), trackPosRect.y + 1),
                new Vector2(trackPosRect.xMax, trackPosRect.y + 1)
            );
            Handles.DrawLine(
                new Vector2(TimeToPos(ViewTimeMin), trackPosRect.yMax),
                new Vector2(trackPosRect.xMax, trackPosRect.yMax)
            );

            Handles.color = Color.white;
            if (ViewTimeMin < 0)
            {
                //just visual clarity
                GUI.Box(
                    Rect.MinMaxRect(TimeToPos(ViewTimeMin), trackPosRect.yMin, TimeToPos(0), trackPosRect.yMax),
                    string.Empty
                );
            }

            // Debug.Log($"track.parent={track.parent}");
            if (track.StartTime > track.Parent.StartTime || track.EndTime < track.Parent.EndTime)
            {
                Handles.color = Color.white;
                GUI.color = Color.black.WithAlpha(0.2f);
                if (track.StartTime > track.Parent.StartTime)
                {
                    var tStart = TimeToPos(track.StartTime);
                    var r = Rect.MinMaxRect(TimeToPos(0), yPos, tStart, yPos + track.ShowHeight);
                    GUI.DrawTexture(r, Styles.whiteTexture);
                    GUI.DrawTextureWithTexCoords(r, Styles.stripes, new Rect(0, 0, r.width / 7, r.height / 7));
                    var a = new Vector2(tStart, trackPosRect.yMin);
                    var b = new Vector2(a.x, trackPosRect.yMax);
                    Handles.DrawLine(a, b);
                }

                if (track.EndTime < track.Parent.EndTime)
                {
                    var tEnd = TimeToPos(track.EndTime);
                    var r = Rect.MinMaxRect(tEnd, yPos, TimeToPos(asset.Length), yPos + track.ShowHeight);
                    GUI.DrawTexture(r, Styles.whiteTexture);
                    GUI.DrawTextureWithTexCoords(r, Styles.stripes, new Rect(0, 0, r.width / 7, r.height / 7));
                    var a = new Vector2(tEnd, trackPosRect.yMin);
                    var b = new Vector2(a.x, trackPosRect.yMax);
                    Handles.DrawLine(a, b);
                }

                GUI.color = Color.white;
                Handles.color = Color.white;
            }

            GUI.backgroundColor = Color.white;

            if (track.IsLocked)
            {
                if (e.isMouse && trackPosRect.Contains(e.mousePosition))
                {
                    e.Use();
                }
            }

            if (!track.IsActive || track.IsLocked)
            {
                postWindowsGUI += () =>
                {
                    //overlay dark stripes for disabled tracks
                    if (!track.IsActive)
                    {
                        GUI.color = Color.black.WithAlpha(0.2f);
                        GUI.DrawTexture(trackPosRect, Styles.whiteTexture);
                        GUI.DrawTextureWithTexCoords(
                            trackPosRect,
                            Styles.stripes,
                            new Rect(0, 0, (trackPosRect.width / 5), (trackPosRect.height / 5))
                        );
                        GUI.color = Color.white;
                    }

                    //overlay light stripes for locked tracks
                    if (track.IsLocked)
                    {
                        GUI.color = Color.black.WithAlpha(0.15f);
                        GUI.DrawTextureWithTexCoords(
                            trackPosRect,
                            Styles.stripes,
                            new Rect(0, 0, trackPosRect.width / 20, trackPosRect.height / 20)
                        );
                        GUI.color = Color.white;
                    }

                    if (isProSkin)
                    {
                        string overlayLabel = null;
                        if (!track.IsActive && track.IsLocked)
                        {
                            overlayLabel = $"{Lan.Disable} & {Lan.Locked}";
                        }
                        else
                        {
                            if (!track.IsActive)
                            {
                                overlayLabel = Lan.Disable;
                            }

                            if (track.IsLocked)
                            {
                                overlayLabel = Lan.Locked;
                            }
                        }

                        var size = Styles.centerLabel.CalcSize(new GUIContent(overlayLabel));
                        var bgLabelRect = new Rect(0, 0, size.x, size.y);
                        bgLabelRect.center = trackPosRect.center;
                        GUI.Label(trackPosRect, $"<b>{overlayLabel}</b>", Styles.centerLabel);
                        GUI.color = Color.white;
                    }
                };
            }

            //绘制轨道片段
            for (int a = 0; a < track.Clips.Count; a++)
            {
                ShowClipArea(track, yPos, a, e);
            }

            var cursorTime = G.SnapTime(PosToTime(mousePosition.x));

            TrackDraw.DrawTrackContextMenu(this, track, e, trackPosRect, cursorTime);
        }

        void ShowClipArea(Track track, float nextYPos, int clipIndex, Event e)
        {
            var action = track.Clips[clipIndex];
            if (!clipWrappers.TryGetValue(action, out var clipWrapper))
            {
                clipWrapper = new ActionClipWrapper(this, action);
                clipWrappers.Add(action, clipWrapper);
            }

            var clipRect = clipWrapper.rect;

            clipRect.y = nextYPos;
            clipRect.width = Mathf.Max(action.Length / ViewTime * CenterRect.width, 6);
            clipRect.height = track.ShowHeight;

            clipRect.x = TimeToPos(action.StartTime);
            var isSelected = selectedClips.Contains(action);
            var isVisible = Rect.MinMaxRect(0, view.ScrollPos.y, CenterRect.width, CenterRect.height).Overlaps(clipRect);
            if (!isSelected && !isVisible)
            {
                //we basicaly "nullify" the rect. Too much trouble to work with nullable rect.
                clipWrapper.rect = default(Rect);
                return;
            }

            if (isSelected && e.type == EventType.Repaint)
            {
                postWindowsGUI += () => DrawClipSelection(clipRect);
            }

            //determine color and draw clip
            var color = Color.white;
            color = action.isValid ? color : new Color(1, 0.3f, 0.3f);
            color = track.IsActive ? color : Color.grey;
            GUI.color = color;
            GUI.Box(clipRect, string.Empty, Styles.clipBoxHorizontalStyle);
            clipWrapper.rect = clipRect;
            GUI.BeginClip(clipRect);
            var start = action.StartTime;
            var end = action.EndTime;
            clipWrapper.OnClipGUI();
            GUI.EndClip();
            if (start != action.StartTime || end != action.EndTime)
            {
                NotifyTimelineChanged();
            }

            //右键菜单
            if (e.type == EventType.ContextClick && clipRect.Contains(e.mousePosition))
            {
                var menu = new GenericMenu();
                if (selectedClips.Count > 1 && isSelected)
                {
                    menu.AddItem(
                        new GUIContent(Lan.ClipDelete),
                        false,
                        () =>
                        {
                            foreach (var selected in selectedClips.ToArray())
                            {
                                if (!selected.IsLocked) selected.Parent.DeleteAction(selected);
                            }

                            NotifyTimelineChanged(rebuildPreview: true);
                        }
                    );

                    menu.ShowAsContext();
                    e.Use();
                    return;
                }

                menu.AddItem(
                    new GUIContent(Lan.ClipCopy),
                    false,
                    () =>
                    {
                        CopiedClip = TimelineJson.Clone(action);
                    }
                );
                menu.AddItem(
                    new GUIContent(Lan.ClipCut),
                    false,
                    () =>
                    {
                        CopiedClip = TimelineJson.Clone(action);
                        action.Parent.DeleteAction(action);
                        NotifyTimelineChanged(rebuildPreview: true);
                    }
                );

                if (action is ISubClipContainable subContainable && subContainable.SubClipLength > 0)
                {
                    menu.AddSeparator("");
                    menu.AddItem(
                        new GUIContent(Lan.MatchPreviousLoop),
                        false,
                        () =>
                        {
                            action.TryMatchPreviousSubClipLoop();
                            NotifyTimelineChanged();
                        }
                    );
                    menu.AddItem(
                        new GUIContent(Lan.MatchClipLength),
                        false,
                        () =>
                        {
                            action.TryMatchSubClipLength();
                            NotifyTimelineChanged();
                        }
                    );
                    menu.AddItem(
                        new GUIContent(Lan.MatchNextLoop),
                        false,
                        () =>
                        {
                            action.TryMatchNexSubClipLoop();
                            NotifyTimelineChanged();
                        }
                    );
                }

                menu.AddSeparator("/");

                menu.AddItem(
                    new GUIContent(Lan.ClipDelete),
                    false,
                    () =>
                    {
                        action.Parent.DeleteAction(action);
                        NotifyTimelineChanged(rebuildPreview: true);
                    }
                );

                menu.ShowAsContext();
                e.Use();
            }

            if (!isProSkin)
            {
                GUI.color = Color.white.WithAlpha(0.5f);
                GUI.Box(clipRect, string.Empty);
                GUI.color = Color.white;
            }

            GUI.color = Color.white;

            var nextClip = clipIndex < track.Clips.Count - 1 ? track.Clips[clipIndex + 1] : null;
            var previousClip = clipIndex > 0 ? track.Clips[clipIndex - 1] : null;
            var nextPosX = TimeToPos(nextClip != null ? nextClip.StartTime : ViewTimeMax);
            var prevPosX = TimeToPos(previousClip != null ? previousClip.EndTime : ViewTimeMin);
            var extRectLeft = Rect.MinMaxRect(prevPosX, clipRect.yMin, clipRect.xMin, clipRect.yMax);
            var extRectRight = Rect.MinMaxRect(clipRect.xMax, clipRect.yMin, nextPosX, clipRect.yMax);
            action.ShowClipGUIExternal(extRectLeft, extRectRight);

            //如果剪辑太小，则把说明信息绘制在外部
            if (clipRect.width <= 20)
            {
                GUI.Label(extRectRight, $"<size=9>{action.DisplayName}</size>");
            }
        }

        private static void DrawClipSelection(Rect rect)
        {
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, rect.width, 1), Color.white);
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMax - 1, rect.width, 1), Color.white);
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, 1, rect.height), Color.white);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1, rect.yMin, 1, rect.height), Color.white);
        }

        /// <summary>
        /// 轨道片段包装器
        /// </summary>
        sealed class ActionClipWrapper
        {
            const float CLIP_BLOCK_COLOR_HEIGHT = 4f;
            const float SCALE_RECT_WIDTH = 5;

            public readonly Clip action;
            private readonly TrackEditorWindow editor;

            private Rect _rect;

            public Rect rect
            {
                get => editor.IsGroupCollapsed(action.Parent.Parent) || editor.view.IsFilteredOutBySearch(action.Parent.Parent) ? default : _rect;
                set => _rect = value;
            }

            public ActionClipWrapper(TrackEditorWindow editor, Clip action)
            {
                this.editor = editor;
                this.action = action;
            }

            public void OnClipGUI()
            {
                var e = Event.current;

                var wholeRect = new Rect(0, 0, rect.width, rect.height);
                var canTrim = !e.control && action.CanScale() && action.Length > 0 && rect.width > SCALE_RECT_WIDTH * 2;
                var left = new Rect(0, 0, SCALE_RECT_WIDTH, rect.height);
                var right = new Rect(rect.width - SCALE_RECT_WIDTH, 0, SCALE_RECT_WIDTH, rect.height);

                editor.AddCursorRect(wholeRect, MouseCursor.Link);
                if (canTrim)
                {
                    editor.AddCursorRect(left, MouseCursor.ResizeHorizontal);
                    editor.AddCursorRect(right, MouseCursor.ResizeHorizontal);
                }

                if (action.IsLocked && e.isMouse && wholeRect.Contains(e.mousePosition))
                {
                    e.Use();
                }

                action.ShowClipGUI(wholeRect);
                if (action is ISubClipContainable subClip)
                {
                    DrawTools.DrawLoopedLines(
                        wholeRect,
                        subClip.SubClipLength / subClip.SubClipSpeed,
                        action.Length,
                        subClip.SubClipOffset
                    );
                }

                ShowClipBlockColor(wholeRect);

                if (canTrim && e.type == EventType.Repaint && !action.IsLocked && !editor.itemDragger.IsDragging)
                {
                    var hover = Color.white.WithAlpha(0.5f);
                    if (left.Contains(e.mousePosition))
                        EditorGUI.DrawRect(left, hover);
                    if (right.Contains(e.mousePosition))
                        EditorGUI.DrawRect(right, hover);
                }

                if (e.type == EventType.MouseDown && e.button == 0 && GUIUtility.hotControl == 0
                    && wholeRect.Contains(e.mousePosition))
                {
                    var inEdge = left.Contains(e.mousePosition);
                    var outEdge = right.Contains(e.mousePosition);
                    var type = ItemDragType.Position;
                    if (canTrim && inEdge) type = ItemDragType.TrimStart;
                    else if (canTrim && outEdge) type = ItemDragType.TrimEnd;

                    var toggle = e.control && type == ItemDragType.Position;
                    editor.SelectClip(action, toggle);
                    if (!toggle || editor.selectedClips.Contains(action))
                    {
                        editor.BeginClipDrag(type, action);
                    }

                    e.Use();
                }

                if (rect.width > 20)
                {
                    var label = $"<size=10>{action.DisplayName}</size>";
                    GUI.color = Color.black;
                    GUI.Label(wholeRect, label);
                    GUI.color = Color.white;
                }
            }

            /// <summary>
            /// 显示剪辑的底部色块
            /// </summary>
            /// <param name="rect"></param>
            void ShowClipBlockColor(Rect rect)
            {
                var colorAttribute = action.GetType().GetCustomAttribute<ColorAttribute>();
                if (colorAttribute != null)
                {
                    var dopeRect = new Rect(0, rect.height - CLIP_BLOCK_COLOR_HEIGHT, rect.width, CLIP_BLOCK_COLOR_HEIGHT);
                    GUI.color = colorAttribute.Color;
                    GUI.Box(dopeRect, string.Empty, Styles.headerBoxStyle);
                    GUI.color = Color.white;
                }
            }
        }

        #endregion

        #region 播放相关

        public AssetPlayer player;

        public bool IsStop => !IsPlay;
        public bool IsPlay => EditorPlaybackState != EditorPlaybackState.Stoped;

        internal EditorPlaybackState EditorPlaybackState = EditorPlaybackState.Stoped;

        public WrapMode EditorPlaybackWrapMode = WrapMode.Loop;

        public void Play(Action callback = null)
        {
            if (asset == null || Application.isPlaying)
            {
                return;
            }

            if (player.CurrentTime >= player.Length)
            {
                Seek(0);
            }

            EditorPlaybackState = EditorPlaybackState.PlayingForwards;
            previewRequested = true;
            player.IsActive = true;
            OnPlay();
            WillRepaint = true;
        }

        public void Pause()
        {
            EditorPlaybackState = EditorPlaybackState.Stoped;
            if (player != null)
            {
                player.IsActive = false;
            }

            OnStop();
        }

        public void Stop(bool forceRewind)
        {
            Pause();
            if (forceRewind && player != null)
            {
                previewRequested = false;
                player.RevertPreview();
                player.CurrentTime = 0;
            }
        }

        private void TogglePlayback()
        {
            if (IsPlay)
            {
                Pause();
            }
            else
            {
                Play();
            }
        }

        private void Seek(float time, bool reveal = true)
        {
            Pause();
            previewRequested = true;
            player.CurrentTime = time;
            if (reveal && (player.CurrentTime < ViewTimeMin || player.CurrentTime > ViewTimeMax))
            {
                var start = Mathf.Max(0, player.CurrentTime - ViewTime * 0.5f);
                SetViewTimeRange(start, start + ViewTime);
            }
        }

        public void StepForward() => Step(1);

        public void StepBackward() => Step(-1);

        private void Step(int direction)
        {
            Seek(Prefs.StepTime(player.CurrentTime, direction));
        }

        #endregion
    }
}
