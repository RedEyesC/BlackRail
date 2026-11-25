using System.Collections.Generic;
using System.Linq;
using TrackEditor.Draws;
using UnityEditor;
using UnityEngine;

namespace TrackEditor
{
    public partial class TrackEditorWindow
    {
        private const float AddGroupButtonGap = 20;
        private const float AddGroupButtonHeight = 20;

        private static readonly Color GROUP_COLOR = new Color(0f, 0f, 0f, 0.25f);

        private Rect leftRect;
        private bool isResizingLeftMargin;
        private Group pickedGroup;
        private Track _copyTrack;

        /// <summary>
        /// 轨道/轨道组 列表
        /// </summary>
        public void DrawGroupAndTrackListGUI()
        {
            leftRect = view.LeftRect;
            var e = Event.current;

            var scaleRect = new Rect(leftRect.xMax - 4, leftRect.yMin, 4, leftRect.height);
            AddCursorRect(scaleRect, MouseCursor.ResizeHorizontal);
            if (e.type == EventType.MouseDown && e.button == 0 && scaleRect.Contains(e.mousePosition))
            {
                isResizingLeftMargin = true;
                e.Use();
            }

            if (isResizingLeftMargin)
            {
                view.LeftMargin = Mathf.Clamp(e.mousePosition.x + 2, 230, 400);
                WillRepaint = true;
            }

            if (e.rawType == EventType.MouseUp)
            {
                isResizingLeftMargin = false;
            }

            GUI.enabled = !IsPlay;

            var nextYPos = Styles.FIRST_GROUP_TOP_MARGIN;
            var wasEnabled = GUI.enabled;
            GUI.enabled = true;
            var collapseAllRect = Rect.MinMaxRect(leftRect.x + 5, leftRect.y + 4, 20, leftRect.y + 20 - 1);
            var searchRect = Rect.MinMaxRect(leftRect.x + 20, leftRect.y + 4, leftRect.xMax - 18, leftRect.y + 20 - 1);
            var searchCancelRect = Rect.MinMaxRect(searchRect.xMax, searchRect.y, leftRect.xMax - 4, searchRect.yMax);
            var anyExpanded = asset.groups.Any(g => !IsGroupCollapsed(g));
            AddCursorRect(collapseAllRect, MouseCursor.Link);
            GUI.color = Color.white.WithAlpha(0.5f);
            if (GUI.Button(collapseAllRect, anyExpanded ? "▼" : "►", (GUIStyle)"label"))
            {
                foreach (var group in asset.groups)
                {
                    collapsedGroups[group] = anyExpanded;
                }
            }

            GUI.color = Color.white;
            view.SearchString = EditorGUI.TextField(searchRect, view.SearchString, (GUIStyle)"ToolbarSeachTextField");
            if (GUI.Button(searchCancelRect, string.Empty, "ToolbarSeachCancelButton"))
            {
                view.SearchString = string.Empty;
                GUIUtility.keyboardControl = 0;
            }

            GUI.enabled = wasEnabled;

            GUI.BeginGroup(leftRect);
            ShowListGroups(e, ref nextYPos);
            GUI.EndGroup();

            var addButtonY = leftRect.y + nextYPos + AddGroupButtonGap;
            var addRect = Rect.MinMaxRect(leftRect.xMin + 10, addButtonY, leftRect.xMax - 10, addButtonY + AddGroupButtonHeight);
            GUI.color = Color.white.WithAlpha(0.5f);
            if (GUI.Button(addRect, Lan.GroupAdd))
            {
                DrawGroupContextMenu();
            }

            //clear picks
            if (e.rawType == EventType.MouseUp)
            {
                pickedGroup = null;
            }

            GUI.enabled = true;
            GUI.color = Color.white;
        }

        private float GetTrackContentHeight()
        {
            var height = Styles.FIRST_GROUP_TOP_MARGIN + AddGroupButtonGap + AddGroupButtonHeight;
            foreach (var group in asset.groups)
            {
                if (view.IsFilteredOutBySearch(group))
                {
                    continue;
                }

                height += Styles.GROUP_HEIGHT;
                if (!IsGroupCollapsed(group))
                {
                    foreach (var track in group.Tracks)
                    {
                        height += track.ShowHeight + Styles.TRACK_MARGINS;
                    }
                }
            }

            return height;
        }

        private void DrawGroupContextMenu()
        {
            var attachableTypeInfos = new List<EditorTools.TypeMetaInfo>();

            foreach (var group in EditorTools.GetTypeMetaDerivedFrom(typeof(Group)))
            {
                if (!group.attachableTypes.Any(type => type.IsAssignableFrom(asset.GetType())))
                {
                    continue;
                }

                attachableTypeInfos.Add(group);
            }

            if (attachableTypeInfos.Count > 1)
            {
                var menu = new GenericMenu();
                foreach (var _info in attachableTypeInfos)
                {
                    var info = _info;
                    var category = string.IsNullOrEmpty(info.category) ? string.Empty : (info.category + "/");
                    var tName = info.name;
                    menu.AddItem(
                        new GUIContent(category + tName),
                        false,
                        () =>
                        {
                            SelectedObject = asset.AddGroup(info.type);
                            NotifyTimelineChanged(rebuildPreview: true);
                        }
                    );
                }

                menu.ShowAsContext();
            }
            else if (attachableTypeInfos.Count > 0)
            {
                var info = attachableTypeInfos[0];
                SelectedObject = asset.AddGroup(info.type);
                NotifyTimelineChanged(rebuildPreview: true);
            }
        }

        private void ShowListGroups(Event e, ref float nextYPos)
        {
            for (int g = 0; g < asset.groups.Count; g++)
            {
                var group = asset.groups[g];

                if (view.IsFilteredOutBySearch(group))
                {
                    continue;
                }

                var groupRect = new Rect(4, nextYPos, leftRect.width - Styles.GROUP_RIGHT_MARGIN - 4, Styles.GROUP_HEIGHT - 3);
                AddCursorRect(groupRect, pickedGroup == null ? MouseCursor.Link : MouseCursor.MoveArrow);
                nextYPos += Styles.GROUP_HEIGHT;

                var groupSelected = (ReferenceEquals(group, SelectedObject) || group == pickedGroup);
                GUI.color = groupSelected ? Styles.TrackHeaderSelection : GROUP_COLOR;
                GUI.Box(groupRect, string.Empty, Styles.headerBoxStyle);
                GUI.color = Color.white;

                var plusClicked = false;
                GUI.color = EditorGUIUtility.isProSkin ? Color.white.WithAlpha(0.5f) : new Color(0.2f, 0.2f, 0.2f);
                var plusRect = new Rect(groupRect.xMax - 20, groupRect.y + 6, 16, 16);

                if (GUI.Button(plusRect, Styles.menuIcon, GUIStyle.none))
                {
                    plusClicked = true;
                }

                if (!group.IsActive)
                {
                    var disableIconRect = new Rect(plusRect.xMin - 20, groupRect.y + 6, 16, 16);
                    if (GUI.Button(disableIconRect, Styles.hiddenIcon, GUIStyle.none))
                    {
                        group.IsActive = true;
                        NotifyTimelineChanged(rebuildPreview: true);
                    }
                }

                if (group.IsLocked)
                {
                    var lockIconRect = new Rect(plusRect.xMin - (group.IsActive ? 20 : 36), groupRect.y + 6, 16, 16);
                    if (GUI.Button(lockIconRect, Styles.lockIcon, GUIStyle.none))
                    {
                        group.IsLocked = false;
                        NotifyTimelineChanged();
                    }
                }

                GUI.color = EditorGUIUtility.isProSkin ? Color.yellow : Color.white;
                GUI.color = group.IsActive ? GUI.color : Color.grey;
                var foldRect = new Rect(groupRect.x + 2, groupRect.y + 1, 20, groupRect.height);
                collapsedGroups[group] = !EditorGUI.Foldout(foldRect, !IsGroupCollapsed(group), $"<b>{@group.Name}</b>");
                GUI.color = Color.white;

                //右键菜单
                if ((e.type == EventType.ContextClick && groupRect.Contains(e.mousePosition)) || plusClicked)
                {
                    var menu = new GenericMenu();
                    foreach (var _info in EditorTools.GetTypeMetaDerivedFrom(typeof(Track)))
                    {
                        var info = _info;
                        if (info.attachableTypes == null || !info.attachableTypes.Contains(group.GetType()))
                        {
                            continue;
                        }

                        var canAdd = !info.isUnique || (group.Tracks.Find(track => track.GetType() == info.type) == null);
                        if (group.IsLocked)
                        {
                            canAdd = false;
                        }

                        var finalPath = string.IsNullOrEmpty(info.category) ? info.name : info.category + "/" + info.name;
                        if (canAdd)
                        {
                            menu.AddItem(
                                new GUIContent($"{Lan.MenuAddTrack}/" + finalPath),
                                false,
                                () =>
                                {
                                    SelectedObject = group.AddTrack(info.type);
                                    NotifyTimelineChanged(rebuildPreview: true);
                                }
                            );
                        }
                        else
                        {
                            menu.AddDisabledItem(new GUIContent($"{Lan.MenuAddTrack}/" + finalPath));
                        }
                    }

                    menu.AddSeparator("");
                    if (group.CanAddTrack(_copyTrack))
                    {
                        menu.AddItem(
                            new GUIContent(Lan.MenuPasteTrack),
                            false,
                            () =>
                            {
                                var t = group.PasteTrack(_copyTrack);
                                SelectedObject = t;
                                NotifyTimelineChanged(rebuildPreview: true);
                            }
                        );
                    }
                    else
                    {
                        menu.AddDisabledItem(new GUIContent(Lan.MenuPasteTrack));
                    }

                    menu.AddItem(
                        new GUIContent(Lan.GroupDisable),
                        !group.IsActive,
                        () =>
                        {
                            group.IsActive = !group.IsActive;
                            NotifyTimelineChanged(rebuildPreview: true);
                        }
                    );
                    menu.AddItem(
                        new GUIContent(Lan.GroupLocked),
                        group.IsLocked,
                        () =>
                        {
                            group.IsLocked = !group.IsLocked;
                            NotifyTimelineChanged();
                        }
                    );

                    menu.AddSeparator("");

                    menu.AddItem(
                        new GUIContent(Lan.GroupReplica),
                        false,
                        () =>
                        {
                            asset.PasteGroup(group);
                            NotifyTimelineChanged(rebuildPreview: true);
                        }
                    );

                    menu.AddSeparator("");
                    if (group.IsLocked)
                    {
                        menu.AddDisabledItem(new GUIContent(Lan.GroupDelete));
                    }
                    else
                    {
                        menu.AddItem(
                            new GUIContent(Lan.GroupDelete),
                            false,
                            () =>
                            {
                                if (EditorUtility.DisplayDialog(Lan.GroupDelete, Lan.GroupDeleteTips, Lan.TipsConfirm, Lan.TipsCancel))
                                {
                                    asset.DeleteGroup(group);
                                    NotifyTimelineChanged(rebuildPreview: true);
                                }
                            }
                        );
                    }

                    menu.ShowAsContext();
                    e.Use();
                }

                if (e.type == EventType.MouseDown && e.button == 0 && groupRect.Contains(e.mousePosition))
                {
                    SelectedObject = group;

                    pickedGroup = group;

                    e.Use();
                }

                if (pickedGroup != null && pickedGroup != group) // && !(group is DirectorGroup))
                {
                    if (groupRect.Contains(e.mousePosition))
                    {
                        var markRect = new Rect(
                            groupRect.x,
                            (asset.groups.IndexOf(pickedGroup) < g) ? groupRect.yMax - 2 : groupRect.y,
                            groupRect.width,
                            2
                        );
                        GUI.color = Color.grey;
                        GUI.DrawTexture(markRect, Styles.whiteTexture);
                        GUI.color = Color.white;
                    }

                    if (e.rawType == EventType.MouseUp && e.button == 0 && groupRect.Contains(e.mousePosition))
                    {
                        asset.groups.Remove(pickedGroup);
                        asset.groups.Insert(g, pickedGroup);
                        NotifyTimelineChanged(rebuildPreview: true);
                        pickedGroup = null;
                        e.Use();
                    }
                }

                ConsiderTrackDrop(group, 0, groupRect, groupRect.yMax - 2);
                if (!IsGroupCollapsed(group))
                {
                    ShowListTracks(e, group, ref nextYPos);
                    GUI.color = groupSelected ? Styles.TrackHeaderSelection : GROUP_COLOR;
                    var verticalRect = Rect.MinMaxRect(groupRect.x, groupRect.yMax, groupRect.x + 3, nextYPos - 2);
                    GUI.DrawTexture(verticalRect, Styles.whiteTexture);
                    GUI.color = Color.white;
                }
            }
        }

        /// <summary>
        /// 显示轨道列表
        /// </summary>
        void ShowListTracks(Event e, Group group, ref float nextYPos)
        {
            for (int t = 0; t < group.Tracks.Count; t++)
            {
                var track = group.Tracks[t];
                var yPos = nextYPos;

                var trackRect = new Rect(10, yPos, leftRect.width - Styles.TRACK_RIGHT_MARGIN - 10, track.ShowHeight);
                nextYPos += track.ShowHeight + Styles.TRACK_MARGINS;

                GUI.color = ColorUtility.Grey(
                    EditorGUIUtility.isProSkin ? (track.IsActive ? 0.25f : 0.2f) : (track.IsActive ? 0.9f : 0.8f)
                );
                GUI.DrawTexture(trackRect, Styles.whiteTexture);
                GUI.color = Color.white.WithAlpha(0.25f);
                GUI.Box(trackRect, string.Empty, (GUIStyle)"flow node 0");
                if (IsTrackSelected(track))
                {
                    EditorGUI.DrawRect(trackRect, Styles.TrackHeaderSelection);
                }

                if (track.IsActive && track.Color != Color.white && track.Color.a > 0.2f)
                {
                    GUI.color = track.Color;
                    var colorRect = new Rect(trackRect.xMax + 1, trackRect.yMin, 2, track.ShowHeight);
                    GUI.DrawTexture(colorRect, Styles.whiteTexture);
                }

                GUI.color = Color.white;

                GUI.BeginGroup(trackRect);
                TrackDraw.Draw(this, track, trackRect);
                GUI.EndGroup();

                AddCursorRect(trackRect, itemDragger.ActiveTrack == null ? MouseCursor.Link : MouseCursor.MoveArrow);

                //右键菜单
                if (e.type == EventType.ContextClick && trackRect.Contains(e.mousePosition))
                {
                    var menu = new GenericMenu();
                    menu.AddItem(
                        new GUIContent(Lan.TrackDisable),
                        !track.IsActive,
                        () =>
                        {
                            track.IsActive = !track.IsActive;
                            NotifyTimelineChanged(rebuildPreview: true);
                        }
                    );
                    menu.AddItem(
                        new GUIContent(Lan.TrackLocked),
                        track.IsLocked,
                        () =>
                        {
                            track.IsLocked = !track.IsLocked;
                            NotifyTimelineChanged();
                        }
                    );

                    menu.AddSeparator("");

                    menu.AddItem(
                        new GUIContent(Lan.TrackCopy),
                        false,
                        () =>
                        {
                            _copyTrack = TimelineJson.Clone(track);
                        }
                    );

                    if (track.GetType().RTGetAttribute<UniqueAttribute>(true) == null)
                    {
                        menu.AddItem(
                            new GUIContent(Lan.TrackReplica),
                            false,
                            () =>
                            {
                                var t1 = group.PasteTrack(track);
                                NotifyTimelineChanged(rebuildPreview: true);
                                SelectedObject = t1;
                            }
                        );
                    }
                    else
                    {
                        menu.AddDisabledItem(new GUIContent(Lan.TrackReplica));
                    }

                    menu.AddSeparator("/");
                    if (track.IsLocked)
                    {
                        menu.AddDisabledItem(new GUIContent(Lan.TrackDelete));
                    }
                    else
                    {
                        menu.AddItem(
                            new GUIContent(Lan.TrackDelete),
                            false,
                            () =>
                            {
                                if (EditorUtility.DisplayDialog(Lan.TrackDelete, Lan.TrackDeleteTips, Lan.TipsConfirm, Lan.TipsCancel))
                                {
                                    group.DeleteTrack(track);
                                    NotifyTimelineChanged(rebuildPreview: true);
                                }
                            }
                        );
                    }

                    menu.ShowAsContext();
                    e.Use();
                }

                //选中
                if (e.type == EventType.MouseDown && e.button == 0 && GUIUtility.hotControl == 0 && trackRect.Contains(e.mousePosition))
                {
                    SelectedObject = track;
                    if (!track.IsLocked)
                    {
                        itemDragger.BeginTrack(track);
                        CaptureItemDragControl();
                    }

                    e.Use();
                }

                var after = e.mousePosition.y >= trackRect.center.y;
                ConsiderTrackDrop(group, after ? t + 1 : t, trackRect, after ? trackRect.yMax - 2 : trackRect.y);
            }
        }
        private void ConsiderTrackDrop(Group group, int index, Rect area, float markerY)
        {
            var e = Event.current;
            if (mousePosition.y < CenterRect.yMin || mousePosition.y >= TimelineBottom
                || !area.Contains(e.mousePosition) || !itemDragger.CanDropTrack(group, index))
            {
                return;
            }

            EditorGUI.DrawRect(new Rect(area.xMin, markerY, area.width, 2), Color.grey);
            if (e.rawType == EventType.MouseUp && e.button == 0)
            {
                trackDropGroup = group;
                trackDropIndex = index;
            }
        }
    }
}
