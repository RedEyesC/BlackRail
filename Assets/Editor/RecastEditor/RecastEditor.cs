
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace GameEditor

{

    public enum AREATYPE
    {
        None = 0,
        Walke = 1,
    }


    public class RecastEditor
    {
        static readonly float PI = 3.1415926f;

        static readonly string MapResPath = "Assets/Resources/map/";
        static readonly string MapElement = "MapElement";

        //构建寻路网格的参数
        static readonly int MAX_HEIGHT = 25;
        static readonly int RC_NOT_CONNECTED = 0x3f;//空心高度场，相邻不可行走标志，RC_NOT_CONNECTED-1 为最大层级
        static readonly int MAX_LAYERS = RC_NOT_CONNECTED - 1;

        static readonly float AgentMaxSlope = 45;
        static readonly float AgentMaxClimb = 3f;
        static readonly float AgentHeight = 2.0f;
        static readonly float AgentRadius = 1f;
        static readonly float CellSize = 1f; //xz平面的尺寸
        static readonly float CellHeight = 1f; // y轴的尺寸

        static readonly float MinRegionArea = 10; //小于此则被剔除
        static readonly float MergeRegionArea = 20; //小于此则与周围区域合并

        static readonly bool FilterLowHangingObstacles = false;//过滤悬空可走的span
        static readonly bool FilterLedgeSpans = false;//过滤高度差过大的span
        static readonly bool FilterWalkableLowHeightSpans = false;//过滤不可通过的高度的span



        [MenuItem("Assets/GameEditor/导出地图navmesh", false, 900)]
        static void ExportRecast()
        {
            foreach (var obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                ExportRecastInfo(path);
            }
        }

        [MenuItem("Assets/GameEditor/导出地图navmesh", true)]
        static bool ValidExportRecast()
        {
            foreach (var obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (path.Contains(MapResPath) && AssetDatabase.IsValidFolder(path))
                {
                    continue;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        static void ExportRecastInfo(string path)
        {

            DirectoryInfo dirInfo = new DirectoryInfo(path);
            foreach (var file in dirInfo.GetFiles())
            {
                if (file.Extension.ToLower() == ".unity")
                {
                    Scene tempScene = EditorSceneManager.OpenScene(path + "/" + file.Name); ;

                    break;
                }
            }

            Scene activeScene = SceneManager.GetActiveScene();
            string activeSceneName = activeScene.name;

            GameObject root = GameObject.Find("/" + activeSceneName);

            Transform navRoot = root.transform.Find(MapElement);

            Mesh mesh = CombineMesh(navRoot);

            Heightfield hf = new Heightfield(mesh, AgentMaxSlope, AgentMaxClimb, AgentHeight, AgentRadius, CellSize, CellHeight);

            //判断三角形是否可行走
            AREATYPE[] areas = RcMarkWalkableTriangles(hf.WalkableSlopeAngle, mesh.vertices, mesh.triangles);

            //体素化三角形，构建高度场
            RcRasterizeTriangles(mesh.vertices, mesh.triangles, areas, hf);


            //过滤悬空的可走障碍物
            if (FilterLowHangingObstacles)
            {
                RcFilterLowHangingWalkableObstacles(hf);
            }

            //过滤高度差过大的span
            if (FilterLedgeSpans)
            {
                RcFilterLedgeSpans(hf);
            }

            //过滤不可通过高度span
            if (FilterWalkableLowHeightSpans)
            {
                RcFilterWalkableLowHeightSpans(hf);
            }

            //构建空心高度场
            CompactHeightfield chf = RcBuildCompactHeightfield(hf);


            //设置边缘不可行走
            RcErodeWalkableArea(chf);


            //设置特殊地形标识，用于设置的标记区域的多边形是y值恒定的多边形
            //RcMarkConvexPolyArea(chf, vertices,AREATYPE.None);

            //构建距离场
            RcBuildDistanceField(chf);

            //分水岭算法构建区域
            RcBuildRegions(chf);

        }

        static Mesh CombineMesh(Transform navRoot)
        {
            //合并mesh
            MeshFilter[] meshFilters = navRoot.GetComponentsInChildren<MeshFilter>();
            CombineInstance[] combines = new CombineInstance[meshFilters.Length];

            for (int i = 0; i < meshFilters.Length; i++)
            {
                combines[i].mesh = meshFilters[i].sharedMesh;
                combines[i].transform = navRoot.transform.worldToLocalMatrix * meshFilters[i].transform.localToWorldMatrix;
            }

            Mesh mesh = new Mesh();

            mesh.CombineMeshes(combines, true);

            return mesh;
        }


        static AREATYPE[] RcMarkWalkableTriangles(float walkableSlopeAngle, Vector3[] verts, int[] tris)
        {

            float walkableThr = Mathf.Cos(walkableSlopeAngle / 180.0f * PI);

            int numTris = tris.Length / 3;
            AREATYPE[] areas = new AREATYPE[numTris];

            for (int i = 0; i < numTris; i++)
            {


                //计算三角形法向量
                Vector3 norm = RecastUtility.CalcTriNormal(verts[tris[i * 3]], verts[tris[i * 3 + 1]], verts[tris[i * 3 + 2]]);

                //法向量在y轴的投影越大，倾斜角度越小
                if (norm[1] > walkableThr)
                {
                    areas[i] = AREATYPE.Walke;
                }
                else
                {
                    areas[i] = AREATYPE.None;
                }

            }

            return areas;
        }


        static void RcRasterizeTriangles(Vector3[] verts, int[] tris, AREATYPE[] areas, Heightfield hf)
        {

            int numTris = tris.Length / 3;
            for (int i = 0; i < numTris; i++)
            {
                Vector3 vert1 = verts[tris[i * 3]];
                Vector3 vert2 = verts[tris[i * 3 + 1]];
                Vector3 vert3 = verts[tris[i * 3 + 2]];

                if (!RasterizeTri(vert1, vert2, vert3, areas[i], hf))
                {
                    Debug.LogError("rcRasterizeTriangles: Out of memory.");
                }

            }

        }

        static bool RasterizeTri(Vector3 v0, Vector3 v1, Vector3 v2, AREATYPE areaType, Heightfield hf)
        {
            Vector3 triBBMin = v0;
            triBBMin = Vector3.Min(triBBMin, v1);
            triBBMin = Vector3.Min(triBBMin, v2);

            Vector3 triBBMax = v0;
            triBBMax = Vector3.Max(triBBMax, v1);
            triBBMax = Vector3.Max(triBBMax, v2);

            Vector3 hfBBMin = hf.MinBounds;
            Vector3 hfBBMax = hf.MaxBounds;

            float cellSize = hf.CellSize;
            float inverseCellSize = 1 / hf.CellSize;
            float inverseCellHeight = 1 / hf.CellHeight;



            float by = hfBBMax[1] - hfBBMin[1];

            // 三角形的包围盒不在目标包围盒范围内就放弃这个三角形
            if (!RecastUtility.OverlapBounds(triBBMin, triBBMax, hfBBMin, hfBBMax))
            {
                return true;
            }

            //在xz平面上，以体素为单位的长度，z轴的最大值和最小值。以包围盒的z为0点,
            int z0 = ((int)((triBBMin[2] - hfBBMin[2]) * inverseCellSize));
            int z1 = ((int)((triBBMax[2] - hfBBMin[2]) * inverseCellSize));

            // 案例里写着·使用-1比0 更好的平铺三角形？？，为什么呢
            z0 = Mathf.Clamp(z0, -1, hf.Height - 1);
            z1 = Mathf.Clamp(z1, 0, hf.Height - 1);


            //三角形被正方形切割最多切割出7边形，存放四组多边形的数据
            Vector3[] nvInList = new Vector3[7];
            Vector3[] nvRowList = new Vector3[7];
            Vector3[] p1InList = new Vector3[7];

            nvInList[0] = v0;
            nvInList[1] = v1;
            nvInList[2] = v2;

            int nvRow = 0;
            int nvIn = 3;
            //按z轴开始切割
            for (int z = z0; z <= z1; ++z)
            {

                float cellZ = hfBBMin[2] + (float)z * cellSize;
                //切割三角形，nvRowList 为切割线下半部分的顶点 ,输出的nvInList 为切割线上半部分顶点
                RecastUtility.DividePoly(nvInList, nvIn, cellZ + cellSize, RcAxis.AXIS_Z, out nvRow, out nvIn, out nvRowList, out nvInList);

                //没切割到东西，顶点小于3构不成多边形
                if (nvRow < 3)
                {
                    continue;
                }

                if (z < 0)
                {
                    continue;
                }


                //重复之前的步骤，按x轴切割
                float minX = nvRowList[0][0];
                float maxX = nvRowList[0][0];

                for (int vert = 1; vert < nvRow; ++vert)
                {

                    float x = nvRowList[vert][0];

                    minX = Mathf.Min(minX, x);
                    maxX = Mathf.Max(maxX, x);

                }

                int x0 = ((int)((minX - hfBBMin[0]) * inverseCellSize));
                int x1 = ((int)((maxX - hfBBMin[0]) * inverseCellSize));

                //recastnavigation 里x0 >= hf.Width ,但是本地测试单独一个正方体体素化的时候，假如多了=，会缺失一面体素
                if (x1 < 0 || x0 > hf.Width)
                {
                    continue;
                }
                x0 = Mathf.Clamp(x0, -1, hf.Width - 1);
                x1 = Mathf.Clamp(x1, 0, hf.Width - 1);


                int nvRow2 = 0;
                int nvIn2 = nvRow;

                for (int x = x0; x <= x1; ++x)
                {
                    float cellX = hfBBMin[0] + (float)x * cellSize;
                    RecastUtility.DividePoly(nvRowList, nvIn2, cellX + cellSize, RcAxis.AXIS_X, out nvRow2, out nvIn2, out p1InList, out nvRowList);

                    if (nvRow2 < 3)
                    {
                        continue;
                    }

                    if (x < 0)
                    {
                        continue;
                    }

                    float spanMin = p1InList[0][1];
                    float spanMax = p1InList[0][1];

                    for (int vert = 1; vert < nvRow2; ++vert)
                    {

                        float y = p1InList[vert][1];

                        spanMin = Mathf.Min(spanMin, y);
                        spanMax = Mathf.Max(spanMax, y);

                    }

                    //坐标Y 平移到最小包围盒底部
                    spanMin -= hfBBMin[1];
                    spanMax -= hfBBMin[1];


                    // 假如在包围盒外，舍弃
                    if (spanMax < 0.0f)
                    {
                        continue;
                    }
                    //recastnavigation 里 spanMin > by ,但是本地测试单独一个正方体体素化的时候，
                    //假如少了 = ，可能会出现一个片面刚刚和包围盒最上方重合,这时候体素会对多一层，因为spanMaxCellIndex必须至少比spanMinCellIndex大1
                    if (spanMin >= by)
                    {
                        continue;
                    }

                    // 截取包围盒内的部分
                    if (spanMin < 0.0f)
                    {
                        spanMin = 0;
                    }
                    if (spanMax > by)
                    {
                        spanMax = by;
                    }

                    var a = spanMin * inverseCellHeight;
                    int spanMinCellIndex = (int)Mathf.Max(Mathf.Floor(spanMin * inverseCellHeight), 0);
                    int spanMaxCellIndex = (int)Mathf.Max(Mathf.Ceil(spanMax * inverseCellHeight), spanMinCellIndex + 1);

                    // 加入高度场
                    if (!AddSpan(hf, x, z, spanMinCellIndex, spanMaxCellIndex, areaType))
                    {
                        return false;
                    }
                }



            }
            return true;
        }


        static bool AddSpan(Heightfield hf, int x, int z, int min, int max, AREATYPE areaType)
        {
            Span newSpan = new Span(min, max, areaType);

            int columnIndex = x + z * hf.Width;

            //缓存比newSpan小的体素的列表
            Span previousSpan = null;
            //进行比较的体素
            Span currentSpan = hf.SpanList[columnIndex];

            while (currentSpan != null)
            {
                //newSpan位置在currentSpan之下
                if (currentSpan.Min > newSpan.Max)
                {
                    break;
                }

                //newSpan位置在currentSpan之上
                if (currentSpan.Max < newSpan.Min)
                {
                    //切换到链表的下一个体素
                    previousSpan = currentSpan;
                    currentSpan = currentSpan.Next;
                }
                //newSpan位置与currentSpan重叠
                else
                {
                    if (currentSpan.Min < newSpan.Min)
                    {
                        newSpan.Min = currentSpan.Min;
                    }
                    if (currentSpan.Max > newSpan.Max)
                    {
                        newSpan.Max = currentSpan.Max;
                    }

                    if (Mathf.Abs(newSpan.Max - currentSpan.Max) < hf.WalkableClimb)
                    {
                        newSpan.AreaID = (AREATYPE)Mathf.Max((int)newSpan.AreaID, (int)currentSpan.AreaID);
                    }

                    //从链表中释放currentSpan
                    Span next = currentSpan.Next;
                    if (previousSpan != null)
                    {
                        previousSpan.Next = next;
                    }
                    else
                    {
                        hf.SpanList[columnIndex] = next;
                    }

                    //切换currentSpan 进行下一轮比较
                    currentSpan = next;
                }

            }

            if (previousSpan != null)
            {
                //将newSpan插入 previousSpan 的后面
                newSpan.Next = previousSpan.Next;
                previousSpan.Next = newSpan;
            }
            else
            {
                newSpan.Next = hf.SpanList[columnIndex];
                hf.SpanList[columnIndex] = newSpan;
            }

            return true;
        }


        public static void RcFilterLowHangingWalkableObstacles(Heightfield hf)
        {
            int xSize = hf.Width;
            int zSize = hf.Height;

            for (int z = 0; z < zSize; ++z)
            {
                for (int x = 0; x < xSize; ++x)
                {
                    Span previousSpan = null;
                    bool previousWasWalkable = false;
                    AREATYPE previousArea = 0;

                    //上下两个span，下span可走，上span不可走，并且上下span的上表面相差不超过walkClimb，则把上span也改为可走
                    for (Span span = hf.SpanList[x + z * xSize]; span != null; previousSpan = span, span = span.Next)
                    {
                        bool walkable = span.AreaID == AREATYPE.Walke;


                        if (!walkable && previousWasWalkable)
                        {
                            if (Mathf.Abs(span.Max - previousSpan.Max) <= hf.WalkableClimb)
                            {
                                span.AreaID = previousArea;
                            }
                        }

                        previousWasWalkable = walkable;
                        previousArea = span.AreaID;
                    }
                }
            }
        }

        public static void RcFilterLedgeSpans(Heightfield hf)
        {
            int xSize = hf.Width;
            int zSize = hf.Height;



            for (int z = 0; z < zSize; ++z)
            {
                for (int x = 0; x < xSize; ++x)
                {
                    for (Span span = hf.SpanList[x + z * xSize]; span != null; span = span.Next)
                    {
                        //跳过不可行走区域
                        if (span.AreaID == AREATYPE.None)
                        {
                            continue;
                        }

                        int bot = span.Max;
                        int top = span.Next != null ? span.Next.Min : MAX_HEIGHT;


                        float minNeighborHeight = MAX_HEIGHT;

                        //相邻span的上表面的最大值与最小值
                        int accessibleNeighborMinHeight = span.Max;
                        int accessibleNeighborMaxHeight = span.Max;

                        for (int direction = 0; direction < 4; ++direction)
                        {
                            int dx = x + RecastUtility.RcGetDirOffsetX(direction);
                            int dy = z + RecastUtility.RcGetDirOffsetY(direction);


                            if (dx < 0 || dy < 0 || dx >= xSize || dy >= zSize)
                            {
                                //边缘情况认为，minNeighborHeight 一定小于 -hf.WalkableClimb而已
                                minNeighborHeight = Mathf.Min(minNeighborHeight, -hf.WalkableClimb - bot);
                                continue;
                            }


                            Span neighborSpan = hf.SpanList[dx + dy * xSize];
                            int neighborBot = -hf.WalkableClimb;
                            int neighborTop = neighborSpan != null ? neighborSpan.Min : MAX_HEIGHT;

                            // 只处理上下表面的距离大于WalkableHeight的部分，先默认处理一次？？？？
                            if (Mathf.Min(top, neighborTop) - Mathf.Max(bot, neighborBot) > hf.WalkableHeight)
                            {
                                minNeighborHeight = Mathf.Min(minNeighborHeight, neighborBot - bot);
                            }

                            for (neighborSpan = hf.SpanList[dx + dy * xSize]; neighborSpan != null; neighborSpan = neighborSpan.Next)
                            {
                                neighborBot = neighborSpan.Max;
                                neighborTop = neighborSpan.Next != null ? neighborSpan.Next.Min : MAX_HEIGHT;

                                // 只处理上下表面的距离大于WalkableHeight的部分
                                if (Mathf.Min(top, neighborTop) - Mathf.Max(bot, neighborBot) > hf.WalkableHeight)
                                {

                                    minNeighborHeight = Mathf.Min(minNeighborHeight, neighborBot - bot);

                                    //寻找相邻的span与自身高度差小于WalkableClimb
                                    if (Mathf.Abs(neighborBot - bot) <= hf.WalkableClimb)
                                    {
                                        if (neighborBot < accessibleNeighborMinHeight) accessibleNeighborMinHeight = neighborBot;
                                        if (neighborBot > accessibleNeighborMaxHeight) accessibleNeighborMaxHeight = neighborBot;
                                    }

                                }
                            }
                        }

                        //相邻span与自身的高度差，说明span高于相邻span 大于WalkableClimb
                        if (minNeighborHeight < -hf.WalkableClimb)
                        {
                            span.AreaID = AREATYPE.None;
                        }
                        //邻居span之间的上表面高度差超过walkClimb，说明span处于比较陡峭的地方，则把span标记为不可行走。
                        else if ((accessibleNeighborMaxHeight - accessibleNeighborMinHeight) > hf.WalkableClimb)
                        {
                            span.AreaID = AREATYPE.None;
                        }
                    }
                }
            }
        }


        public static void RcFilterWalkableLowHeightSpans(Heightfield hf)
        {
            int xSize = hf.Width;
            int zSize = hf.Height;

            //如果上下两个span之间的空隙小于等于walkHeight，则把下span标记为不可行走。
            for (int z = 0; z < zSize; ++z)
            {
                for (int x = 0; x < xSize; ++x)
                {
                    for (Span span = hf.SpanList[x + z * xSize]; span != null; span = span.Next)
                    {
                        int bot = span.Max;
                        int top = span.Next != null ? span.Next.Min : MAX_HEIGHT;
                        if ((top - bot) < hf.WalkableHeight)
                        {
                            span.AreaID = AREATYPE.None;
                        }
                    }
                }
            }
        }

        public static CompactHeightfield RcBuildCompactHeightfield(Heightfield heightfield)
        {
            CompactHeightfield compactHeightfield = new CompactHeightfield(heightfield);

            int currentCellIndex = 0;
            int numColumns = heightfield.Width * heightfield.Height;

            for (int columnIndex = 0; columnIndex < numColumns; ++columnIndex)
            {
                Span span = heightfield.SpanList[columnIndex];

                CompactCell cell = new CompactCell(0, 0);

                compactHeightfield.CellList[columnIndex] = cell;

                if (span == null)
                {
                    continue;
                }

                cell.Index = currentCellIndex;

                for (; span != null; span = span.Next)
                {
                    if (span.AreaID != AREATYPE.None)
                    {
                        int bot = span.Max;
                        int top = span.Next != null ? span.Next.Min : MAX_HEIGHT;

                        compactHeightfield.SpanList[currentCellIndex] = new CompactSpan(Mathf.Clamp(bot, 0, MAX_HEIGHT), Mathf.Clamp(top - bot, 0, MAX_HEIGHT), span.AreaID);
                        compactHeightfield.AreaList[currentCellIndex] = span.AreaID;

                        currentCellIndex++;
                        cell.Count++;
                    }
                }
            }


            int zSize = heightfield.Height;
            int xSize = heightfield.Width;
            int maxLayerIndex = 0;
            int zStride = xSize;
            for (int z = 0; z < zSize; ++z)
            {
                for (int x = 0; x < xSize; ++x)
                {
                    CompactCell cell = compactHeightfield.CellList[x + z * zStride];
                    for (int i = cell.Index, ni = (cell.Index + cell.Count); i < ni; ++i)
                    {
                        CompactSpan span = compactHeightfield.SpanList[i];

                        for (int dir = 0; dir < 4; ++dir)
                        {

                            RecastUtility.RcSetCon(span, dir, RC_NOT_CONNECTED);
                            int neighborX = x + RecastUtility.RcGetDirOffsetX(dir);
                            int neighborZ = z + RecastUtility.RcGetDirOffsetY(dir);

                            if (neighborX < 0 || neighborZ < 0 || neighborX >= xSize || neighborZ >= zSize)
                            {
                                continue;
                            }


                            CompactCell neighborCell = compactHeightfield.CellList[neighborX + neighborZ * zStride];

                            for (int k = neighborCell.Index, nk = neighborCell.Index + neighborCell.Count; k < nk; ++k)
                            {
                                CompactSpan neighborSpan = compactHeightfield.SpanList[k];
                                int bot = Mathf.Max(span.Y, neighborSpan.Y);
                                int top = Mathf.Min(span.Y + span.H, neighborSpan.Y + neighborSpan.H);


                                //与相邻的空心体素的之间的高度大于行走高度，之间的落差小于可攀爬高度
                                if (((top - bot) >= compactHeightfield.WalkableHeight) && (Mathf.Abs(neighborSpan.Y - span.Y) <= compactHeightfield.WalkableClimb))
                                {
                                    // Mark direction as walkable.
                                    int layerIndex = k - neighborCell.Index;
                                    if (layerIndex < 0 || layerIndex > MAX_LAYERS)
                                    {
                                        maxLayerIndex = Mathf.Max(maxLayerIndex, layerIndex);
                                        continue;
                                    }

                                    RecastUtility.RcSetCon(span, dir, layerIndex);
                                    break;
                                }

                            }
                        }

                    }
                }
            }
            if (maxLayerIndex > MAX_LAYERS)
            {
                Debug.LogWarning(string.Format("rcBuildCompactHeightfield: Heightfield has too many layers %d (max: %d)", maxLayerIndex, MAX_LAYERS));
            }

            return compactHeightfield;
        }


        public static void RcErodeWalkableArea(CompactHeightfield compactHeightfield)
        {
            int xSize = compactHeightfield.Width;
            int zSize = compactHeightfield.Height;
            int zStride = xSize;


            int[] distanceToBoundary = new int[compactHeightfield.SpanCount];


            for (int z = 0; z < zSize; ++z)
            {
                for (int x = 0; x < xSize; ++x)
                {
                    CompactCell cell = compactHeightfield.CellList[x + z * zStride];
                    for (int spanIndex = cell.Index, maxSpanIndex = (cell.Index + cell.Count); spanIndex < maxSpanIndex; ++spanIndex)
                    {

                        //设置默认值
                        distanceToBoundary[spanIndex] = 255;

                        //不可行走设置为边缘
                        if (compactHeightfield.AreaList[spanIndex] == AREATYPE.None)
                        {
                            distanceToBoundary[spanIndex] = 0;
                            continue;
                        }

                        CompactSpan span = compactHeightfield.SpanList[spanIndex];

                        int neighborCount = 0;
                        for (int direction = 0; direction < 4; ++direction)
                        {
                            int neighborConnection = RecastUtility.RcGetCon(span, direction);
                            if (neighborConnection == RC_NOT_CONNECTED)
                            {
                                break;
                            }

                            int neighborX = x + RecastUtility.RcGetDirOffsetX(direction);
                            int neighborZ = z + RecastUtility.RcGetDirOffsetY(direction);
                            int neighborSpanIndex = compactHeightfield.CellList[neighborX + neighborZ * zStride].Index + neighborConnection;

                            if (compactHeightfield.AreaList[neighborSpanIndex] == AREATYPE.None)
                            {
                                break;
                            }
                            neighborCount++;
                        }

                        //不是四个位置都存在相邻的可行走，则视为边缘
                        if (neighborCount != 4)
                        {
                            distanceToBoundary[spanIndex] = 0;
                        }
                    }
                }
            }

            int newDistance;

            //第一次从左下到右上遍历，计算出障碍物右上方span的距离，对于CompactSpan本身是左下侧，，，左下最边缘是障碍物,一层层向右上方感染

            for (int z = 0; z < zSize; ++z)
            {
                for (int x = 0; x < xSize; ++x)
                {
                    CompactCell cell = compactHeightfield.CellList[x + z * zStride];
                    int maxSpanIndex = (cell.Index + cell.Count);
                    for (int spanIndex = cell.Index; spanIndex < maxSpanIndex; ++spanIndex)
                    {
                        CompactSpan span = compactHeightfield.SpanList[spanIndex];

                        if (RecastUtility.RcGetCon(span, 0) != RC_NOT_CONNECTED)
                        {
                            // (-1,0) 左侧
                            int aX = x + RecastUtility.RcGetDirOffsetX(0);
                            int aY = z + RecastUtility.RcGetDirOffsetY(0);
                            int aIndex = compactHeightfield.CellList[aX + aY * xSize].Index + RecastUtility.RcGetCon(span, 0);
                            CompactSpan aSpan = compactHeightfield.SpanList[aIndex];
                            // 正交的邻居距离+2
                            newDistance = Mathf.Min(distanceToBoundary[aIndex] + 2, 255);
                            if (newDistance < distanceToBoundary[spanIndex])
                            {
                                distanceToBoundary[spanIndex] = newDistance;
                            }

                            // (-1,-1) 左下
                            if (RecastUtility.RcGetCon(aSpan, 3) != RC_NOT_CONNECTED)
                            {
                                int bX = aX + RecastUtility.RcGetDirOffsetX(3);
                                int bY = aY + RecastUtility.RcGetDirOffsetY(3);
                                int bIndex = compactHeightfield.CellList[bX + bY * xSize].Index + RecastUtility.RcGetCon(aSpan, 3);
                                // 斜方向的邻居距离+3
                                newDistance = Mathf.Min(distanceToBoundary[bIndex] + 3, 255);
                                if (newDistance < distanceToBoundary[spanIndex])
                                {
                                    distanceToBoundary[spanIndex] = newDistance;
                                }
                            }
                        }
                        if (RecastUtility.RcGetCon(span, 3) != RC_NOT_CONNECTED)
                        {
                            // (0,-1) 下侧
                            int aX = x + RecastUtility.RcGetDirOffsetX(3);
                            int aY = z + RecastUtility.RcGetDirOffsetY(3);
                            int aIndex = compactHeightfield.CellList[aX + aY * xSize].Index + RecastUtility.RcGetCon(span, 3);
                            CompactSpan aSpan = compactHeightfield.SpanList[aIndex];
                            newDistance = Mathf.Min(distanceToBoundary[aIndex] + 2, 255);
                            if (newDistance < distanceToBoundary[spanIndex])
                            {
                                distanceToBoundary[spanIndex] = newDistance;
                            }

                            // (1,-1) 下右
                            if (RecastUtility.RcGetCon(aSpan, 2) != RC_NOT_CONNECTED)
                            {
                                int bX = aX + RecastUtility.RcGetDirOffsetX(2);
                                int bY = aY + RecastUtility.RcGetDirOffsetY(2);
                                int bIndex = compactHeightfield.CellList[bX + bY * xSize].Index + RecastUtility.RcGetCon(aSpan, 2);
                                newDistance = Mathf.Min(distanceToBoundary[bIndex] + 3, 255);
                                if (newDistance < distanceToBoundary[spanIndex])
                                {
                                    distanceToBoundary[spanIndex] = newDistance;
                                }
                            }
                        }
                    }
                }
            }

            //第二次从右上到最下遍历，计算出障碍物左下方span的距离
            for (int z = zSize - 1; z > 0; --z)
            {
                for (int x = xSize - 1; x > 0; --x)
                {
                    CompactCell cell = compactHeightfield.CellList[x + z * zStride];
                    int maxSpanIndex = (cell.Index + cell.Count);
                    for (int spanIndex = cell.Index; spanIndex < maxSpanIndex; ++spanIndex)
                    {
                        CompactSpan span = compactHeightfield.SpanList[spanIndex];

                        if (RecastUtility.RcGetCon(span, 2) != RC_NOT_CONNECTED)
                        {
                            // (1,0) 右侧
                            int aX = x + RecastUtility.RcGetDirOffsetX(2);
                            int aY = z + RecastUtility.RcGetDirOffsetY(2);
                            int aIndex = compactHeightfield.CellList[aX + aY * xSize].Index + RecastUtility.RcGetCon(span, 2);
                            CompactSpan aSpan = compactHeightfield.SpanList[aIndex];
                            // 正交的邻居距离+2
                            newDistance = Mathf.Min(distanceToBoundary[aIndex] + 2, 255);
                            if (newDistance < distanceToBoundary[spanIndex])
                            {
                                distanceToBoundary[spanIndex] = newDistance;
                            }

                            // (1,1) 右上
                            if (RecastUtility.RcGetCon(aSpan, 1) != RC_NOT_CONNECTED)
                            {
                                int bX = aX + RecastUtility.RcGetDirOffsetX(1);
                                int bY = aY + RecastUtility.RcGetDirOffsetY(1);
                                int bIndex = compactHeightfield.CellList[bX + bY * xSize].Index + RecastUtility.RcGetCon(aSpan, 1);
                                // 斜方向的邻居距离+3
                                newDistance = Mathf.Min(distanceToBoundary[bIndex] + 3, 255);
                                if (newDistance < distanceToBoundary[spanIndex])
                                {
                                    distanceToBoundary[spanIndex] = newDistance;
                                }
                            }
                        }
                        if (RecastUtility.RcGetCon(span, 1) != RC_NOT_CONNECTED)
                        {
                            // (0,1) 上侧
                            int aX = x + RecastUtility.RcGetDirOffsetX(1);
                            int aY = z + RecastUtility.RcGetDirOffsetY(1);
                            int aIndex = compactHeightfield.CellList[aX + aY * xSize].Index + RecastUtility.RcGetCon(span, 1);
                            CompactSpan aSpan = compactHeightfield.SpanList[aIndex];
                            newDistance = Mathf.Min(distanceToBoundary[aIndex] + 2, 255);
                            if (newDistance < distanceToBoundary[spanIndex])
                            {
                                distanceToBoundary[spanIndex] = newDistance;
                            }

                            // (-1,1) 上左
                            if (RecastUtility.RcGetCon(aSpan, 0) != RC_NOT_CONNECTED)
                            {
                                int bX = aX + RecastUtility.RcGetDirOffsetX(0);
                                int bY = aY + RecastUtility.RcGetDirOffsetY(0);
                                int bIndex = compactHeightfield.CellList[bX + bY * xSize].Index + RecastUtility.RcGetCon(aSpan, 0);
                                newDistance = Mathf.Min(distanceToBoundary[bIndex] + 3, 255);
                                if (newDistance < distanceToBoundary[spanIndex])
                                {
                                    distanceToBoundary[spanIndex] = newDistance;
                                }
                            }
                        }
                    }
                }
            }



            //大于半径x2 ，，因为算距离的时候本身就是x2
            int minBoundaryDistance = (compactHeightfield.WalkableRadius * 2);
            for (int spanIndex = 0; spanIndex < compactHeightfield.SpanCount; ++spanIndex)
            {
                if (distanceToBoundary[spanIndex] < minBoundaryDistance)
                {
                    compactHeightfield.AreaList[spanIndex] = AREATYPE.None;
                }
            }
        }


        public static void RcMarkConvexPolyArea(CompactHeightfield compactHeightfield, Vector3[] vertices, AREATYPE areaId)
        {
            int xSize = compactHeightfield.Width;
            int zSize = compactHeightfield.Height;
            int zStride = xSize;

            Vector3 MinBounds;
            Vector3 MaxBounds;

            RecastUtility.CalcBounds(vertices, out MinBounds, out MaxBounds);


            //计算多边形在高度场内的坐标范围
            int minx = (int)((MinBounds[0] - compactHeightfield.MinBounds[0]) / compactHeightfield.CellSize);
            int miny = (int)((MinBounds[1] - compactHeightfield.MinBounds[1]) / compactHeightfield.CellHeight);
            int minz = (int)((MinBounds[2] - compactHeightfield.MinBounds[2]) / compactHeightfield.CellSize);
            int maxx = (int)((MaxBounds[0] - compactHeightfield.MinBounds[0]) / compactHeightfield.CellSize);
            int maxy = (int)((MaxBounds[1] - compactHeightfield.MinBounds[1]) / compactHeightfield.CellHeight);
            int maxz = (int)((MaxBounds[2] - compactHeightfield.MinBounds[2]) / compactHeightfield.CellSize);


            if (maxx < 0) { return; }
            if (minx >= xSize) { return; }
            if (maxz < 0) { return; }
            if (minz >= zSize) { return; }


            if (minx < 0) { minx = 0; }
            if (maxx >= xSize) { maxx = xSize - 1; }
            if (minz < 0) { minz = 0; }
            if (maxz >= zSize) { maxz = zSize - 1; }


            for (int z = minz; z <= maxz; ++z)
            {
                for (int x = minx; x <= maxx; ++x)
                {
                    CompactCell cell = compactHeightfield.CellList[x + z * zStride];
                    int maxSpanIndex = cell.Index + cell.Count;

                    for (int spanIndex = (int)cell.Index; spanIndex < maxSpanIndex; ++spanIndex)
                    {
                        CompactSpan span = compactHeightfield.SpanList[spanIndex];

                        if (compactHeightfield.AreaList[spanIndex] == AREATYPE.None)
                        {
                            continue;
                        }

                        if (span.Y < miny || span.Y > maxy)
                        {
                            continue;
                        }

                        //因为之后是投影至2d平面的运算所以 point[1] = 0
                        float[] point = new float[3];
                        point[0] = compactHeightfield.MinBounds[0] + (x + 0.5f) * compactHeightfield.CellSize;
                        point[1] = 0;
                        point[2] = compactHeightfield.MinBounds[2] + (z + 0.5f) * compactHeightfield.CellSize;

                        //投影到2d平面进行运算
                        if (pointInPoly(vertices, point))
                        {
                            compactHeightfield.AreaList[spanIndex] = areaId;
                        }
                    }
                }
            }


        }


        public static bool pointInPoly(Vector3[] verts, float[] point)
        {
            bool inPoly = false;
            int nvert = verts.Length;


            for (int i = 0, j = nvert - 1; i < nvert; j = i++)
            {
                Vector3 vi = verts[i];
                Vector3 vj = verts[j];

                if ((vi[2] > point[2]) == (vj[2] > point[2]))
                {
                    continue;
                }

                if (point[0] >= (vj[0] - vi[0]) * (point[2] - vi[2]) / (vj[2] - vi[2]) + vi[0])
                {
                    continue;
                }
                inPoly = !inPoly;
            }
            return inPoly;
        }

        public static void RcBuildDistanceField(CompactHeightfield compactHeightfield)
        {
            //计算距离场
            CalculateDistanceField(compactHeightfield);

            //平滑
            boxBlur(compactHeightfield, 1);

        }


        public static void CalculateDistanceField(CompactHeightfield compactHeightfield)
        {
            int xSize = compactHeightfield.Width;
            int zSize = compactHeightfield.Height;
            int zStride = xSize;


            int[] distanceToBoundary = new int[compactHeightfield.SpanCount];


            for (int z = 0; z < zSize; ++z)
            {
                for (int x = 0; x < xSize; ++x)
                {
                    CompactCell cell = compactHeightfield.CellList[x + z * zStride];
                    for (int spanIndex = cell.Index, maxSpanIndex = (cell.Index + cell.Count); spanIndex < maxSpanIndex; ++spanIndex)
                    {

                        //设置默认值
                        distanceToBoundary[spanIndex] = 0xffff;

                        CompactSpan span = compactHeightfield.SpanList[spanIndex];

                        int neighborCount = 0;
                        for (int direction = 0; direction < 4; ++direction)
                        {
                            int neighborConnection = RecastUtility.RcGetCon(span, direction);
                            if (neighborConnection == RC_NOT_CONNECTED)
                            {
                                break;
                            }

                            int neighborX = x + RecastUtility.RcGetDirOffsetX(direction);
                            int neighborZ = z + RecastUtility.RcGetDirOffsetY(direction);
                            int neighborSpanIndex = compactHeightfield.CellList[neighborX + neighborZ * zStride].Index + neighborConnection;

                            if (compactHeightfield.AreaList[neighborSpanIndex] != compactHeightfield.AreaList[spanIndex])
                            {
                                break;
                            }
                            neighborCount++;
                        }

                        //不是四个位置都存在同类型区域，则视为边缘
                        if (neighborCount != 4)
                        {
                            distanceToBoundary[spanIndex] = 0;
                        }
                    }
                }
            }


            int newDistance;

            //第一次从左下到右上遍历，计算出障碍物右上方span的距离，对于CompactSpan本身是左下侧，，，左下最边缘是障碍物,一层层向右上方感染

            for (int z = 0; z < zSize; ++z)
            {
                for (int x = 0; x < xSize; ++x)
                {
                    CompactCell cell = compactHeightfield.CellList[x + z * zStride];
                    int maxSpanIndex = (cell.Index + cell.Count);
                    for (int spanIndex = cell.Index; spanIndex < maxSpanIndex; ++spanIndex)
                    {
                        CompactSpan span = compactHeightfield.SpanList[spanIndex];

                        if (RecastUtility.RcGetCon(span, 0) != RC_NOT_CONNECTED)
                        {
                            // (-1,0) 左侧
                            int aX = x + RecastUtility.RcGetDirOffsetX(0);
                            int aY = z + RecastUtility.RcGetDirOffsetY(0);
                            int aIndex = compactHeightfield.CellList[aX + aY * xSize].Index + RecastUtility.RcGetCon(span, 0);
                            CompactSpan aSpan = compactHeightfield.SpanList[aIndex];
                            // 正交的邻居距离+2
                            newDistance = distanceToBoundary[aIndex] + 2;
                            if (newDistance < distanceToBoundary[spanIndex])
                            {
                                distanceToBoundary[spanIndex] = newDistance;
                            }

                            // (-1,-1) 左下
                            if (RecastUtility.RcGetCon(aSpan, 3) != RC_NOT_CONNECTED)
                            {
                                int bX = aX + RecastUtility.RcGetDirOffsetX(3);
                                int bY = aY + RecastUtility.RcGetDirOffsetY(3);
                                int bIndex = compactHeightfield.CellList[bX + bY * xSize].Index + RecastUtility.RcGetCon(aSpan, 3);
                                // 斜方向的邻居距离+3
                                newDistance = distanceToBoundary[bIndex] + 3;
                                if (newDistance < distanceToBoundary[spanIndex])
                                {
                                    distanceToBoundary[spanIndex] = newDistance;
                                }
                            }
                        }
                        if (RecastUtility.RcGetCon(span, 3) != RC_NOT_CONNECTED)
                        {
                            // (0,-1) 下侧
                            int aX = x + RecastUtility.RcGetDirOffsetX(3);
                            int aY = z + RecastUtility.RcGetDirOffsetY(3);
                            int aIndex = compactHeightfield.CellList[aX + aY * xSize].Index + RecastUtility.RcGetCon(span, 3);
                            CompactSpan aSpan = compactHeightfield.SpanList[aIndex];
                            newDistance = distanceToBoundary[aIndex] + 2;
                            if (newDistance < distanceToBoundary[spanIndex])
                            {
                                distanceToBoundary[spanIndex] = newDistance;
                            }

                            // (1,-1) 下右
                            if (RecastUtility.RcGetCon(aSpan, 2) != RC_NOT_CONNECTED)
                            {
                                int bX = aX + RecastUtility.RcGetDirOffsetX(2);
                                int bY = aY + RecastUtility.RcGetDirOffsetY(2);
                                int bIndex = compactHeightfield.CellList[bX + bY * xSize].Index + RecastUtility.RcGetCon(aSpan, 2);
                                newDistance = distanceToBoundary[bIndex] + 3;
                                if (newDistance < distanceToBoundary[spanIndex])
                                {
                                    distanceToBoundary[spanIndex] = newDistance;
                                }
                            }
                        }
                    }
                }
            }

            //第二次从右上到最下遍历，计算出障碍物左下方span的距离
            for (int z = zSize - 1; z > 0; --z)
            {
                for (int x = xSize - 1; x > 0; --x)
                {
                    CompactCell cell = compactHeightfield.CellList[x + z * zStride];
                    int maxSpanIndex = (cell.Index + cell.Count);
                    for (int spanIndex = cell.Index; spanIndex < maxSpanIndex; ++spanIndex)
                    {
                        CompactSpan span = compactHeightfield.SpanList[spanIndex];

                        if (RecastUtility.RcGetCon(span, 2) != RC_NOT_CONNECTED)
                        {
                            // (1,0) 右侧
                            int aX = x + RecastUtility.RcGetDirOffsetX(2);
                            int aY = z + RecastUtility.RcGetDirOffsetY(2);
                            int aIndex = compactHeightfield.CellList[aX + aY * xSize].Index + RecastUtility.RcGetCon(span, 2);
                            CompactSpan aSpan = compactHeightfield.SpanList[aIndex];
                            // 正交的邻居距离+2
                            newDistance = distanceToBoundary[aIndex] + 2;
                            if (newDistance < distanceToBoundary[spanIndex])
                            {
                                distanceToBoundary[spanIndex] = newDistance;
                            }

                            // (1,1) 右上
                            if (RecastUtility.RcGetCon(aSpan, 1) != RC_NOT_CONNECTED)
                            {
                                int bX = aX + RecastUtility.RcGetDirOffsetX(1);
                                int bY = aY + RecastUtility.RcGetDirOffsetY(1);
                                int bIndex = compactHeightfield.CellList[bX + bY * xSize].Index + RecastUtility.RcGetCon(aSpan, 1);
                                // 斜方向的邻居距离+3
                                newDistance = distanceToBoundary[bIndex] + 3;
                                if (newDistance < distanceToBoundary[spanIndex])
                                {
                                    distanceToBoundary[spanIndex] = newDistance;
                                }
                            }
                        }
                        if (RecastUtility.RcGetCon(span, 1) != RC_NOT_CONNECTED)
                        {
                            // (0,1) 上侧
                            int aX = x + RecastUtility.RcGetDirOffsetX(1);
                            int aY = z + RecastUtility.RcGetDirOffsetY(1);
                            int aIndex = compactHeightfield.CellList[aX + aY * xSize].Index + RecastUtility.RcGetCon(span, 1);
                            CompactSpan aSpan = compactHeightfield.SpanList[aIndex];
                            newDistance = distanceToBoundary[aIndex] + 2;
                            if (newDistance < distanceToBoundary[spanIndex])
                            {
                                distanceToBoundary[spanIndex] = newDistance;
                            }

                            // (-1,1) 上左
                            if (RecastUtility.RcGetCon(aSpan, 0) != RC_NOT_CONNECTED)
                            {
                                int bX = aX + RecastUtility.RcGetDirOffsetX(0);
                                int bY = aY + RecastUtility.RcGetDirOffsetY(0);
                                int bIndex = compactHeightfield.CellList[bX + bY * xSize].Index + RecastUtility.RcGetCon(aSpan, 0);
                                newDistance = distanceToBoundary[bIndex] + 3;
                                if (newDistance < distanceToBoundary[spanIndex])
                                {
                                    distanceToBoundary[spanIndex] = newDistance;
                                }
                            }
                        }
                    }
                }
            }


            int maxDist = 0;
            for (int i = 0; i < distanceToBoundary.Length; i++)
            {
                maxDist = Mathf.Max(maxDist, distanceToBoundary[i]);
            }

            compactHeightfield.MaxDistance = maxDist;
            compactHeightfield.DistanceToBoundary = distanceToBoundary;
        }


        public static void boxBlur(CompactHeightfield compactHeightfield, int thr)
        {
            int xSize = compactHeightfield.Width;
            int zSize = compactHeightfield.Height;
            int zStride = xSize;

            thr = thr * 2;

            int[] distanceToBoundary = new int[compactHeightfield.SpanCount];


            for (int z = 0; z < zSize; ++z)
            {
                for (int x = 0; x < xSize; ++x)
                {
                    CompactCell cell = compactHeightfield.CellList[x + z * zStride];
                    for (int spanIndex = cell.Index, maxSpanIndex = (cell.Index + cell.Count); spanIndex < maxSpanIndex; ++spanIndex)
                    {

                        int cd = compactHeightfield.DistanceToBoundary[spanIndex];

                        if (cd <= thr)
                        {
                            distanceToBoundary[spanIndex] = cd;
                            continue;
                        }

                        CompactSpan span = compactHeightfield.SpanList[spanIndex];

                        int d = cd;

                        for (int direction = 0; direction < 4; ++direction)
                        {
                            int neighborConnection = RecastUtility.RcGetCon(span, direction);
                            if (neighborConnection != RC_NOT_CONNECTED)
                            {
                                int neighborX = x + RecastUtility.RcGetDirOffsetX(direction);
                                int neighborZ = z + RecastUtility.RcGetDirOffsetY(direction);
                                int neighborSpanIndex = compactHeightfield.CellList[neighborX + neighborZ * zStride].Index + neighborConnection;

                                d = d + compactHeightfield.DistanceToBoundary[neighborSpanIndex];

                                CompactSpan neighborSpan = compactHeightfield.SpanList[neighborSpanIndex];

                                int direction2 = (direction + 1) & 0x3;
                                int neighbor2Connection = RecastUtility.RcGetCon(neighborSpan, direction2);
                                if (neighbor2Connection != RC_NOT_CONNECTED)
                                {
                                    int neighbor2X = x + RecastUtility.RcGetDirOffsetX(direction2);
                                    int neighbor2Z = z + RecastUtility.RcGetDirOffsetY(direction2);
                                    int neighbor2SpanIndex = compactHeightfield.CellList[neighbor2X + neighbor2Z * zStride].Index + neighbor2Connection;
                                    d = d + compactHeightfield.DistanceToBoundary[neighbor2SpanIndex];
                                }
                                else
                                {
                                    d = d + cd;
                                }
                            }
                            else
                            {
                                //cd*2是包括了斜边的部分
                                d = d + cd * 2;
                            }

                            distanceToBoundary[spanIndex] = ((d + 5) / 9);

                        }

                    }

                }
            }

            compactHeightfield.DistanceToBoundary = distanceToBoundary;
        }


        public static void RcBuildRegions(CompactHeightfield compactHeightfield)
        {

            const int expandIters = 8;
            const int loglevelsPerStack = 1;//决定了每组的距离
            const int LOG_NB_STACKS = 3;
            const int NB_STACKS = 1 << LOG_NB_STACKS; //决定了每批有8组

            int regionId = 1;

            //使最低一位为0，使得level为2的倍数
            int level = (compactHeightfield.MaxDistance + 1) & (~1);

            int sId = -1;

            Stack<LevelStackEntry>[] lvlStacks = new Stack<LevelStackEntry>[NB_STACKS];
            Stack<LevelStackEntry> stack = new Stack<LevelStackEntry>();

            for (int i = 0; i < NB_STACKS; i++)
            {
                lvlStacks[i] = new Stack<LevelStackEntry>();
            }

            int[] srcReg = new int[compactHeightfield.SpanCount];
            int[] srcDist = new int[compactHeightfield.SpanCount];

            for (int i = 0; i < compactHeightfield.SpanCount; i++)
            {
                srcReg[i] = 0;
                srcDist[i] = 0;
            }

            while (level > 0)
            {
                level = level >= 2 ? level - 2 : 0;
                sId = (sId + 1) & (NB_STACKS - 1);

                //分批划分level层级，ps话说我感觉可以直接一次性划分完啊，为什么要分批去划分
                if (sId == 0)
                    SortCellsByLevel(level, compactHeightfield, srcReg, NB_STACKS, lvlStacks, loglevelsPerStack);
                else
                    AppendStacks(lvlStacks[sId - 1], lvlStacks[sId], srcReg);

                //从水源进行蔓延 
                ExpandRegions(expandIters, level, compactHeightfield, srcReg, srcDist, lvlStacks[sId], false);

                //寻找水源
                foreach (LevelStackEntry lvlStackEntry in lvlStacks[sId])
                {
                    int x = lvlStackEntry.X;
                    int y = lvlStackEntry.Y;
                    int i = lvlStackEntry.Index;
                    if (i >= 0 && srcReg[i] == 0)
                    {
                        if (FloodRegion(x, y, i, level, regionId, compactHeightfield, srcReg, srcDist, stack))
                        {
                            if (regionId == 0xFFFF)
                            {
                                Debug.LogError("rcBuildRegions: Region ID overflow");
                            }

                            regionId++;
                        }
                    }

                }

            }

            ExpandRegions(expandIters * 8, 0, compactHeightfield, srcReg, srcDist, stack, false);

            //合并区域
            compactHeightfield.MaxRegions = regionId;

            MergeAndFilterRegions(compactHeightfield, srcReg);

            for (int i = 0; i < compactHeightfield.SpanCount; ++i)
                compactHeightfield.SpanList[i].Reg = srcReg[i];

            BuildCompactHeightfield(compactHeightfield, 3);

        }

        //loglevelsPerStack 决定距离多少为一个level
        public static void SortCellsByLevel(int startLevel, CompactHeightfield compactHeightfield, int[] srcReg, int nbStacks, Stack<LevelStackEntry>[] stacks, int loglevelsPerStack)
        {
            int w = compactHeightfield.Width;
            int h = compactHeightfield.Height;
            startLevel = startLevel >> loglevelsPerStack;

            for (int i = 0; i < nbStacks; i++)
            {
                stacks[i].Clear();
            }

            for (int y = 0; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    CompactCell c = compactHeightfield.CellList[x + y * w];
                    for (int i = c.Index, ni = (c.Index + c.Count); i < ni; ++i)
                    {
                        if (compactHeightfield.AreaList[i] == AREATYPE.None || srcReg[i] != 0)
                            continue;

                        int level = compactHeightfield.DistanceToBoundary[i] >> loglevelsPerStack;
                        int sId = startLevel - level;
                        if (sId >= nbStacks)
                            continue;
                        if (sId < 0)
                            sId = 0;

                        stacks[sId].Push(new LevelStackEntry(x, y, i));
                    }
                }
            }
        }

        public static void AppendStacks(Stack<LevelStackEntry> srcStack, Stack<LevelStackEntry> dstStack, int[] srcReg)
        {

            foreach (LevelStackEntry srcEntry in srcStack)
            {
                int i = srcEntry.Index;
                if ((i < 0) || (srcReg[i] != 0))
                    continue;
                dstStack.Push(srcEntry);
            }
        }

        public static void ExpandRegions(int maxIter, int level, CompactHeightfield compactHeightfield, int[] srcReg, int[] srcDist, Stack<LevelStackEntry> stack, bool fillStack)
        {
            int w = compactHeightfield.Width;
            int h = compactHeightfield.Height;

            if (fillStack)
            {
                stack.Clear();

                for (int y = 0; y < h; ++y)
                {
                    for (int x = 0; x < w; ++x)
                    {
                        CompactCell c = compactHeightfield.CellList[x + y * w];
                        for (int i = c.Index, ni = (c.Index + c.Count); i < ni; ++i)
                        {
                            if (compactHeightfield.DistanceToBoundary[i] >= level && srcReg[i] == 0 && compactHeightfield.AreaList[i] != AREATYPE.None)
                            {
                                stack.Push(new LevelStackEntry(x, y, i));
                            }
                        }
                    }
                }
            }
            else
            {
                //排除那些已经标记了区域的cell
                foreach (LevelStackEntry entry in stack)
                {
                    int i = entry.Index;
                    if (srcReg[i] != 0)
                        entry.Index = -1;
                }

            }


            Stack<DirtyEntry> dirtyEntries = new Stack<DirtyEntry>();
            int iter = 0;
            while (stack.Count > 0)
            {

                int failed = 0;
                dirtyEntries.Clear();

                //遍历假如相邻的span已经属于某个regionId，则把自身设置为同个regionId
                foreach (LevelStackEntry entry in stack)
                {
                    int x = entry.X;
                    int z = entry.Y;
                    int spanIndex = entry.Index;
                    if (spanIndex < 0)
                    {
                        failed++;
                        continue;
                    }

                    int r = srcReg[spanIndex];
                    int d2 = 0xffff;
                    AREATYPE area = compactHeightfield.AreaList[spanIndex];
                    CompactSpan span = compactHeightfield.SpanList[spanIndex];

                    for (int direction = 0; direction < 4; ++direction)
                    {
                        int neighborConnection = RecastUtility.RcGetCon(span, direction);
                        int neighborX = x + RecastUtility.RcGetDirOffsetX(direction);
                        int neighborZ = z + RecastUtility.RcGetDirOffsetY(direction);
                        int neighborSpanIndex = compactHeightfield.CellList[neighborX + neighborZ * w].Index + neighborConnection;

                        if (compactHeightfield.AreaList[neighborSpanIndex] != compactHeightfield.AreaList[spanIndex])
                        {
                            continue;
                        }

                        // 暂时去掉了关于边缘的这部分
                        // & RC_BORDER_REG == 0 来判断是否不是边缘，有点奇怪，因为如果首位和RC_BORDER_REG一致，即使不是RC_BORDER_REG ，也会等于0,

                        if (srcReg[neighborSpanIndex] > 0)
                        {
                            if (srcDist[neighborSpanIndex] + 2 < d2)
                            {
                                r = srcReg[neighborSpanIndex];
                                d2 = srcDist[neighborSpanIndex] + 2;
                            }
                        }
                    }

                    if (r != 0)
                    {
                        entry.Index = -1; //标记已经使用了
                        dirtyEntries.Push(new DirtyEntry(spanIndex, r, d2));
                    }
                    else
                    {
                        failed++;
                    }

                }

                foreach (DirtyEntry dirtyEntery in dirtyEntries)
                {
                    int idx = dirtyEntery.Index;
                    srcReg[idx] = dirtyEntery.Region;
                    srcDist[idx] = dirtyEntery.Distance2;

                }

                if (failed == stack.Count)
                    break;

                if (level > 0)
                {
                    ++iter;
                    if (iter >= maxIter)
                        break;
                }

            }

        }

        public static bool FloodRegion(int x, int y, int i, int level, int r, CompactHeightfield compactHeightfield, int[] srcReg, int[] srcDist, Stack<LevelStackEntry> stack)
        {
            int w = compactHeightfield.Width;
            AREATYPE area = compactHeightfield.AreaList[i];

            stack.Clear();
            stack.Push(new LevelStackEntry(x, y, i));
            //标记为当前的regionId
            srcReg[i] = r;
            srcDist[i] = 0;

            int lev = level >= 2 ? level - 2 : 0;
            int count = 0;

            while (stack.Count > 0)
            {
                LevelStackEntry back = stack.Pop();
                int cx = back.X;
                int cy = back.Y;
                int ci = back.Index;

                CompactSpan span = compactHeightfield.SpanList[ci];
                int ar = 0;

                //8向遍历相邻span 如果相邻span属于其他region，则自身region标记为0
                for (int direction = 0; direction < 4; ++direction)
                {

                    int neighborConnection = RecastUtility.RcGetCon(span, direction);
                    if (neighborConnection != RC_NOT_CONNECTED)
                    {
                        int neighborX = cx + RecastUtility.RcGetDirOffsetX(direction);
                        int neighborZ = cy + RecastUtility.RcGetDirOffsetY(direction);
                        int neighborSpanIndex = compactHeightfield.CellList[neighborX + neighborZ * w].Index + neighborConnection;

                        //不在同一个area
                        if (compactHeightfield.AreaList[neighborSpanIndex] != area)
                            continue;

                        int nr = srcReg[ci];

                        //属于已标记region，且不是当前的regionId
                        if (nr != 0 && nr != r)
                        {
                            ar = nr;
                            break;
                        }


                        int direction2 = (direction + 1) & 0x3;
                        CompactSpan neighborSpan = compactHeightfield.SpanList[neighborSpanIndex];
                        int neighbor2Connection = RecastUtility.RcGetCon(neighborSpan, direction2);
                        if (neighbor2Connection != RC_NOT_CONNECTED)
                        {
                            int neighbor2X = neighborX + RecastUtility.RcGetDirOffsetX(direction2);
                            int neighbor2Z = neighborZ + RecastUtility.RcGetDirOffsetY(direction2);
                            int neighbor2SpanIndex = compactHeightfield.CellList[neighbor2X + neighbor2Z * w].Index + neighbor2Connection;

                            if (compactHeightfield.AreaList[neighbor2SpanIndex] != area)
                                continue;

                            int nr2 = srcReg[neighbor2SpanIndex];

                            if (nr2 != 0 && nr2 != r)
                            {
                                ar = nr2;
                                break;
                            }
                        }

                    }

                }

                if (ar != 0)
                {
                    srcReg[ci] = 0;
                    continue;
                }

                count++;


                //假如相邻的span未标记且距离场大于leve ，则相邻的span标记为当前的regionId
                for (int direction = 0; direction < 4; ++direction)
                {

                    int neighborConnection = RecastUtility.RcGetCon(span, direction);
                    if (neighborConnection != RC_NOT_CONNECTED)
                    {
                        int neighborX = cx + RecastUtility.RcGetDirOffsetX(direction);
                        int neighborZ = cy + RecastUtility.RcGetDirOffsetY(direction);
                        int neighborSpanIndex = compactHeightfield.CellList[neighborX + neighborZ * w].Index + neighborConnection;

                        if (compactHeightfield.AreaList[neighborSpanIndex] != area)
                            continue;

                        if (compactHeightfield.DistanceToBoundary[neighborSpanIndex] > lev && srcReg[neighborSpanIndex] == 0)
                        {
                            srcReg[neighborSpanIndex] = r;
                            srcDist[neighborSpanIndex] = 0;
                            stack.Push(new LevelStackEntry(neighborX, neighborZ, neighborSpanIndex));
                        }

                    }
                }

            }
            return count > 0;
        }

        public static void MergeAndFilterRegions(CompactHeightfield compactHeightfield, int[] srcReg)
        {
            int w = compactHeightfield.Width;
            int h = compactHeightfield.Height;

            int nreg = compactHeightfield.MaxRegions + 1;

            RcRegion[] regions = new RcRegion[compactHeightfield.MaxRegions + 1];
            for (int i = 0; i < nreg; ++i)
            {
                regions[i] = new RcRegion(i);
            }

            for (int y = 0; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    CompactCell c = compactHeightfield.CellList[x + y * w];
                    for (int i = c.Index, ni = (c.Index + c.Count); i < ni; ++i)
                    {
                        int r = srcReg[i];
                        if (r == 0 || r >= nreg)
                            continue;

                        RcRegion reg = regions[r];
                        reg.SpanCount++;

                        // 寻找相同xz平面位置的上方的span的区域
                        for (int j = c.Index; j < ni; ++j)
                        {
                            if (i == j) continue;
                            int floorId = srcReg[j];
                            if (floorId == 0 || floorId >= nreg)
                                continue;
                            if (floorId == r)
                                reg.Overlap = true;
                            AddUniqueFloorRegion(reg, floorId);
                        }

                        if (reg.Connections.Count > 0)
                            continue;

                        reg.AreaType = compactHeightfield.AreaList[i];


                        int ndir = -1;
                        for (int dir = 0; dir < 4; ++dir)
                        {
                            if (IsSolidEdge(compactHeightfield, srcReg, x, y, i, dir))
                            {
                                ndir = dir;
                                break;
                            }
                        }

                        if (ndir != -1)
                        {
                            WalkContour(compactHeightfield, srcReg, x, y, i, ndir, reg.Connections);
                        }
                    }
                }
            }

            Stack<int> stack = new Stack<int>();
            List<int> trace = new List<int>();

            for (int i = 0; i < nreg; ++i)
            {
                RcRegion reg = regions[i];
                if (reg.Id == 0)
                    continue;
                if (reg.SpanCount == 0)
                    continue;
                if (reg.Visited)
                    continue;


                int spanCount = 0;

                stack.Clear();
                trace.Clear();

                reg.Visited = true;
                stack.Push(i);

                while (stack.Count > 0)
                {
                    int ri = stack.Pop();

                    RcRegion creg = regions[ri];

                    spanCount += creg.SpanCount;
                    trace.Add(ri);

                    for (int j = 0; j < creg.Connections.Count; ++j)
                    {

                        RcRegion neireg = regions[creg.Connections[j]];
                        if (neireg.Visited)
                            continue;
                        if (neireg.Id == 0)
                            continue;

                        //将相邻区域压入stack ，用于计算spanCount
                        stack.Push(neireg.Id);
                        neireg.Visited = true;
                    }
                }

                //去掉过于小的区域
                if (spanCount < MinRegionArea)
                {
                    for (int j = 0; j < trace.Count; ++j)
                    {
                        regions[trace[j]].SpanCount = 0;
                        regions[trace[j]].Id = 0;
                    }
                }
            }

            int mergeCount = 0;


            for (int i = 0; i < nreg; ++i)
            {
                RcRegion reg = regions[i];
                if (reg.Id == 0)
                    continue;
                if (reg.Overlap)
                    continue;
                if (reg.SpanCount == 0)
                    continue;


                if (reg.SpanCount > MergeRegionArea)
                    continue;

                int smallest = 0xfffffff;
                int mergeId = reg.Id;
                for (int j = 0; j < reg.Connections.Count; ++j)
                {

                    RcRegion mreg = regions[reg.Connections[j]];
                    if (mreg.Id == 0 || mreg.Overlap) continue;
                    if (mreg.SpanCount < smallest &&
                        CanMergeWithRegion(reg, mreg) &&
                        CanMergeWithRegion(mreg, reg))
                    {
                        smallest = mreg.SpanCount;
                        mergeId = mreg.Id;
                    }
                }

                //存在可以合并
                if (mergeId != reg.Id)
                {
                    int oldId = reg.Id;
                    RcRegion target = regions[mergeId];


                    if (MergeRegions(target, reg))
                    {

                        for (int j = 0; j < nreg; ++j)
                        {
                            if (regions[j].Id == 0) continue;

                            if (regions[j].Id == oldId)
                                regions[j].Id = mergeId;

                            ReplaceNeighbour(regions[j], oldId, mergeId);
                        }
                        mergeCount++;
                    }
                }
            }

            for (int i = 0; i < nreg; ++i)
            {
                regions[i].Remap = false;
                if (regions[i].Id == 0)
                {
                    continue;
                }

                regions[i].Remap = true;
            }

            int regIdGen = 0;
            for (int i = 0; i < nreg; ++i)
            {
                if (!regions[i].Remap)
                    continue;
                int oldId = regions[i].Id;
                int newId = ++regIdGen;
                for (int j = i; j < nreg; ++j)
                {
                    if (regions[j].Id == oldId)
                    {
                        regions[j].Id = newId;
                        regions[j].Remap = false;
                    }
                }
            }
            compactHeightfield.MaxRegions = regIdGen;

            //重新命名区域
            for (int i = 0; i < compactHeightfield.SpanCount; ++i)
            {
                if (srcReg[i] > 0)
                {
                    srcReg[i] = regions[srcReg[i]].Id;
                }

            }

            // Return regions that we found to be overlapping.
            //for (int i = 0; i < nreg; ++i)
            //    if (regions[i].Overlap)
            //        overlaps.push(regions[i].id);
        }

        static void AddUniqueFloorRegion(RcRegion reg, int n)
        {
            for (int i = 0; i < reg.Floors.Count; ++i)
                if (reg.Floors[i] == n)
                    return;
            reg.Floors.Add(n);
        }

        public static bool IsSolidEdge(CompactHeightfield chf, int[] srcReg, int x, int y, int i, int dir)
        {
            //相邻的span与自身是不同区域的，则视为边缘
            CompactSpan s = chf.SpanList[i];
            int r = 0;
            if (RecastUtility.RcGetCon(s, dir) != RC_NOT_CONNECTED)
            {
                int ax = x + RecastUtility.RcGetDirOffsetX(dir);
                int ay = y + RecastUtility.RcGetDirOffsetY(dir);
                int ai = chf.CellList[ax + ay * chf.Width].Index + RecastUtility.RcGetCon(s, dir);
                r = srcReg[ai];
            }
            if (r == srcReg[i])
                return false;
            return true;
        }

        public static void WalkContour(CompactHeightfield chf, int[] srcReg, int x, int y, int i, int dir, List<int> cont)
        {
            int startDir = dir;
            int starti = i;

            CompactSpan ss = chf.SpanList[i];
            int curReg = 0;
            if (RecastUtility.RcGetCon(ss, dir) != RC_NOT_CONNECTED)
            {
                int ax = x + RecastUtility.RcGetDirOffsetX(dir);
                int ay = y + RecastUtility.RcGetDirOffsetY(dir);
                int ai = chf.CellList[ax + ay * chf.Width].Index + RecastUtility.RcGetCon(ss, dir);
                curReg = srcReg[ai];
            }
            cont.Add(curReg);

            int iter = 0;

            //从如果dir方向遇到边界就尝试顺时针旋转dir继续走，dir方向可走就走一格，然后逆时针旋转dir继续尝试……，
            while (++iter < 40000)
            {
                CompactSpan s = chf.SpanList[i];

                if (IsSolidEdge(chf, srcReg, x, y, i, dir))
                {

                    int r = 0;
                    if (RecastUtility.RcGetCon(s, dir) != RC_NOT_CONNECTED)
                    {
                        int ax = x + RecastUtility.RcGetDirOffsetX(dir);
                        int ay = y + RecastUtility.RcGetDirOffsetY(dir);
                        int ai = chf.CellList[ax + ay * chf.Width].Index + RecastUtility.RcGetCon(s, dir);
                        r = srcReg[ai];
                    }
                    if (r != curReg)
                    {
                        curReg = r;
                        cont.Add(curReg);
                    }

                    dir = (dir + 1) & 0x3;  //顺指针旋转
                }
                else
                {
                    int ni = -1;
                    int nx = x + RecastUtility.RcGetDirOffsetX(dir);
                    int ny = y + RecastUtility.RcGetDirOffsetY(dir);
                    if (RecastUtility.RcGetCon(s, dir) != RC_NOT_CONNECTED)
                    {
                        CompactCell nc = chf.CellList[nx + ny * chf.Width];
                        ni = nc.Index + RecastUtility.RcGetCon(s, dir);
                    }
                    if (ni == -1)
                    {
                        return;
                    }
                    x = nx;
                    y = ny;
                    i = ni; //移动一格
                    dir = (dir + 3) & 0x3;  // 逆时针旋转
                }

                if (starti == i && startDir == dir)
                {
                    break;
                }
            }

            //移除相邻的重复项
            if (cont.Count > 1)
            {
                for (int j = 0; j < cont.Count;)
                {
                    int nj = (j + 1) % cont.Count;
                    if (cont[j] == cont[nj])
                    {
                        for (int k = j; k < cont.Count - 1; ++k)
                            cont[k] = cont[k + 1];
                        cont.RemoveAt(cont.Count - 1);
                    }
                    else
                        ++j;
                }
            }
        }

        public static bool CanMergeWithRegion(RcRegion rega, RcRegion regb)
        {

            if (rega.AreaType != regb.AreaType)
            {
                return false;
            }

            //两个region之间无多处连接
            int n = 0;
            for (int i = 0; i < rega.Connections.Count; ++i)
            {
                if (rega.Connections[i] == regb.Id)
                    n++;
            }
            if (n > 1)
            {
                return false;
            }

            for (int i = 0; i < rega.Floors.Count; ++i)
            {
                if (rega.Floors[i] == regb.Id)
                    return false;
            }
            return true;
        }

        public static bool MergeRegions(RcRegion rega, RcRegion regb)
        {

            int aid = rega.Id;
            int bid = regb.Id;

            List<int> acon = new List<int>(rega.Connections.Count);
            for (int i = 0; i < rega.Connections.Count; ++i)
                acon.Insert(i,rega.Connections[i]);

            List<int> bcon = regb.Connections;

            //发现a的插入点
            int insa = -1;
            for (int i = 0; i < acon.Count; ++i)
            {
                if (acon[i] == bid)
                {
                    insa = i;
                    break;
                }
            }
            if (insa == -1)
                return false;

            // 发现b的插入点
            int insb = -1;
            for (int i = 0; i < bcon.Count; ++i)
            {
                if (bcon[i] == aid)
                {
                    insb = i;
                    break;
                }
            }
            if (insb == -1)
                return false;


            rega.Connections.Clear();

            for (int i = 0, ni = acon.Count; i < ni - 1; ++i)
                rega.Connections.Add(acon[(insa + 1 + i) % ni]);

            for (int i = 0, ni = bcon.Count; i < ni - 1; ++i)
                rega.Connections.Add(bcon[(insb + 1 + i) % ni]);

            RemoveAdjacentNeighbours(rega);

            for (int j = 0; j < regb.Floors.Count; ++j)
                AddUniqueFloorRegion(rega, regb.Floors[j]);

            rega.SpanCount += regb.SpanCount;
            regb.SpanCount = 0;
            regb.Connections.Clear();

            return true;

        }

        private static void ReplaceNeighbour(RcRegion reg, int oldId, int newId)
        {
            bool neiChanged = false;
            for (int i = 0; i < reg.Connections.Count; ++i)
            {
                if (reg.Connections[i] == oldId)
                {
                    reg.Connections[i] = newId;
                    neiChanged = true;
                }
            }
            for (int i = 0; i < reg.Floors.Count; ++i)
            {
                if (reg.Floors[i] == oldId)
                    reg.Floors[i] = newId;
            }
            if (neiChanged)
                RemoveAdjacentNeighbours(reg);
        }

        private static void RemoveAdjacentNeighbours(RcRegion reg)
        {
            //移除相邻的重复项
            for (int i = 0; i < reg.Connections.Count && reg.Connections.Count > 1;)
            {
                int ni = (i + 1) % reg.Connections.Count;
                if (reg.Connections[i] == reg.Connections[ni])
                {

                    for (int j = i; j < reg.Connections.Count - 1; ++j)
                        reg.Connections[j] = reg.Connections[j + 1];
                    reg.Connections.RemoveAt(reg.Connections.Count - 1);
                }
                else
                    ++i;
            }
        }


        //用于绘制计算出来的高度场,并标记可行走区域
        public static void BuildHeightfield(Heightfield hf, bool showWalk = false)
        {

            GameObject root = GameObject.Find("EditorRoot");
            if (root)
            {
                GameObject.DestroyImmediate(root);
            }

            root = new GameObject("EditorRoot");

            int total = 0;
            int walkable = 0;

            Vector3 hfBBMin = hf.MinBounds;
            float cellSize = hf.CellSize;
            float cellHeight = hf.CellHeight;

            Material newMat = null;

            for (int i = 0; i < hf.SpanList.Length; i++)
            {
                int x = i % hf.Width;
                int z = i / hf.Width;

                Span currentSpan = hf.SpanList[i];
                while (currentSpan != null)
                {
                    for (int y = currentSpan.Min; y < currentSpan.Max; y++)
                    {
                        float cellX = hfBBMin[0] + (float)x * cellSize + cellSize / 2;
                        float cellZ = hfBBMin[2] + (float)z * cellSize + cellSize / 2;
                        float cellY = hfBBMin[1] + (float)y * cellHeight + cellHeight / 2;

                        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        total++;
                        cube.transform.localScale = new Vector3(cellSize, cellHeight, cellSize);
                        cube.transform.position = new Vector3(cellX, cellY, cellZ);
                        cube.transform.SetParent(root.transform);

                        if (showWalk && currentSpan.AreaID == AREATYPE.Walke && y == currentSpan.Max - 1)
                        {

                            if (newMat == null)
                            {
                                Material mat = cube.GetComponent<MeshRenderer>().sharedMaterial;
                                newMat = UnityEngine.Material.Instantiate(mat);
                                newMat.color = UnityEngine.Color.red;
                            }

                            cube.GetComponent<MeshRenderer>().sharedMaterial = newMat;
                            walkable++;
                        }

                    }
                    currentSpan = currentSpan.Next;
                }
            }

            Debug.Log("Total Voxels:" + total + ",Walkable Voxels:" + walkable);
        }


        //用于绘制计算出来的空心高度场，绘制距离场参数,并标记区域
        public static void BuildCompactHeightfield(CompactHeightfield chf, int type = 1)
        {

            GameObject root = GameObject.Find("EditorRoot");
            if (root)
            {
                GameObject.DestroyImmediate(root);
            }

            root = new GameObject("EditorRoot");

            Vector3 hfBBMin = chf.MinBounds;
            float cellSize = chf.CellSize;
            float cellHeight = chf.CellHeight;


            int w = chf.Width;
            int h = chf.Height;

            int maxDistance = 0;

            if (chf.DistanceToBoundary != null)
            {
                foreach (int i in chf.DistanceToBoundary)
                {
                    maxDistance = Mathf.Max(maxDistance, i);
                }
            }

            Color[] colorMap = { Color.red, Color.green, Color.blue, Color.white, Color.black,
                Color.yellow, Color.cyan, Color.magenta, Color.gray };


            Material newMat = null;
            Dictionary<int, Material> matMap = new Dictionary<int, Material>();

            for (int z = 0; z < h; ++z)
            {
                for (int x = 0; x < w; ++x)
                {
                    CompactCell c = chf.CellList[x + z * w];
                    for (int i = c.Index, ni = (c.Index + c.Count); i < ni; ++i)
                    {

                        CompactSpan span = chf.SpanList[i];


                        //为了方便观察，CompactSpan只构建一个cellHeight，实际不止
                        float cellX = hfBBMin[0] + (float)x * cellSize + cellSize / 2;
                        float cellZ = hfBBMin[2] + (float)z * cellSize + cellSize / 2;
                        float cellY = hfBBMin[1] + (float)span.Y * cellHeight + cellHeight / 2;

                        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        cube.transform.localScale = new Vector3(cellSize, cellHeight, cellSize);
                        cube.transform.position = new Vector3(cellX, cellY, cellZ);
                        cube.transform.SetParent(root.transform);

                        if (chf.DistanceToBoundary != null && type == 1)
                        {

                            if (!matMap.ContainsKey(chf.DistanceToBoundary[i]))
                            {
                                Material mat = cube.GetComponent<MeshRenderer>().sharedMaterial;
                                Material cloneMat = UnityEngine.Material.Instantiate(mat);
                                float color = (float)chf.DistanceToBoundary[i] / (float)maxDistance;
                                cloneMat.color = new UnityEngine.Color(color, color, color);

                                matMap.Add(chf.DistanceToBoundary[i], cloneMat);
                            }

                            cube.GetComponent<MeshRenderer>().sharedMaterial = matMap[chf.DistanceToBoundary[i]];
                            continue;
                        }

                        if (chf.AreaList[i] == AREATYPE.Walke && type == 2)
                        {

                            if (newMat == null)
                            {
                                Material mat = cube.GetComponent<MeshRenderer>().sharedMaterial;
                                newMat = UnityEngine.Material.Instantiate(mat);
                                newMat.color = UnityEngine.Color.red;
                            }
                            cube.GetComponent<MeshRenderer>().sharedMaterial = newMat;
                            continue;
                        }

                        if (type == 3)
                        {

                            if (!matMap.ContainsKey(span.Reg))
                            {
                                Material mat = cube.GetComponent<MeshRenderer>().sharedMaterial;
                                Material cloneMat = UnityEngine.Material.Instantiate(mat);
                                cloneMat.color = colorMap[span.Reg % 8];
                                matMap.Add(span.Reg, cloneMat);
                            }

                            cube.GetComponent<MeshRenderer>().sharedMaterial = matMap[span.Reg];
                            continue;
                        }
                    }

                }
            }

        }
    }

}

