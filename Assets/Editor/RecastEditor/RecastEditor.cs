
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        static readonly int MAX_HEIGHT = 9999;
        static readonly int RC_NOT_CONNECTED = 0x3f;

        static readonly string MapResPath = "Assets/Resources/map/";
        static readonly string MapElement = "MapElement";

        //构建寻路网格的参数
        static readonly float AgentMaxSlope = 45;
        static readonly float AgentMaxClimb = 0.9f;
        static readonly float AgentHeight = 2.0f;
        static readonly float CellSize = 1f; //xz平面的尺寸
        static readonly float CellHeight = 1f; // y轴的尺寸

        static readonly bool FilterLowHangingObstacles = true;//过滤悬空可走的span
        static readonly bool FilterLedgeSpans = true;//过滤高度差过大的span
        static readonly bool FilterWalkableLowHeightSpans = true;//过滤不可通过的高度的span

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
            var scenePath = activeScene.path;
            string activeScenePath = scenePath.Remove(scenePath.LastIndexOf("/"));

            GameObject root = GameObject.Find("/" + activeSceneName);

            Transform navRoot = root.transform.Find(MapElement);

            MeshFilter[] meshFilters = navRoot.GetComponentsInChildren<MeshFilter>();
            CombineInstance[] combines = new CombineInstance[meshFilters.Length];

            for (int i = 0; i < meshFilters.Length; i++)
            {
                combines[i].mesh = meshFilters[i].sharedMesh;
                combines[i].transform = navRoot.transform.worldToLocalMatrix * meshFilters[i].transform.localToWorldMatrix;
            }

            Mesh mesh = new Mesh();

            mesh.CombineMeshes(combines, true);


            Heightfield hf = new Heightfield(mesh, AgentMaxSlope, AgentMaxClimb, AgentHeight, CellSize, CellHeight);


            AREATYPE[] areas = RcMarkWalkableTriangles(hf.WalkableSlopeAngle, mesh.vertices, mesh.triangles);

            RcRasterizeTriangles(mesh.vertices, mesh.triangles, areas, hf);


            if (FilterLowHangingObstacles)
            {
                RcFilterLowHangingWalkableObstacles(hf);
            }

            if (FilterLedgeSpans)
            {
                RcFilterLedgeSpans(hf);
            }

            if (FilterWalkableLowHeightSpans)
            {
                RcFilterWalkableLowHeightSpans(hf);
            }


            CompactHeightfield chf = RcBuildCompactHeightfield(hf);
        }

        //判断是否符合可行走角度
        static AREATYPE[] RcMarkWalkableTriangles(float walkableSlopeAngle, Vector3[] verts, int[] tris)
        {

            float walkableThr = Mathf.Cos(walkableSlopeAngle / 180.0f * PI);

            int numTris = tris.Length / 3;
            AREATYPE[] areas = new AREATYPE[numTris];

            for (int i = 0; i < numTris; i++)
            {


                //计算三角形法向量
                Vector3 norm = CommonUtility.CalcTriNormal(verts[tris[i * 3]], verts[tris[i * 3 + 1]], verts[tris[i * 3 + 2]]);

                //法向量在y轴的投影越大，倾斜角度越小
                if (norm[1] > walkableThr)
                {
                    areas[i] = AREATYPE.Walke;
                }

            }

            return areas;
        }

        //体素化三角形，构建高度场
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
            if (!CommonUtility.OverlapBounds(triBBMin, triBBMax, hfBBMin, hfBBMax))
            {
                return true;
            }

            //在xz平面上，以体素为单位的长度，z轴的最大值和最小值。以包围盒的z为0点,
            int z0 = (int)((triBBMin[2] - hfBBMin[2]) * inverseCellSize);
            int z1 = (int)((triBBMax[2] - hfBBMin[2]) * inverseCellSize);

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
                CommonUtility.DividePoly(nvInList, nvIn, cellZ + cellSize, RcAxis.AXIS_Z, out nvRow, out nvIn, out nvRowList, out nvInList);

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

                int x0 = (int)((minX - hfBBMin[0]) * inverseCellSize);
                int x1 = (int)((maxX - hfBBMin[0]) * inverseCellSize);

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
                    CommonUtility.DividePoly(nvRowList, nvIn2, cellX + cellSize, RcAxis.AXIS_X, out nvRow2, out nvIn2, out p1InList, out nvRowList);

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
                        int accessibleNeighborMinHeight = span.Min;
                        int accessibleNeighborMaxHeight = span.Max;

                        for (int direction = 0; direction < 4; ++direction)
                        {
                            int dx = x + CommonUtility.RcGetDirOffsetX(direction);
                            int dy = z + CommonUtility.RcGetDirOffsetY(direction);


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
                        currentCellIndex++;
                        cell.Count++;
                    }
                }
            }


            int MAX_LAYERS = RC_NOT_CONNECTED - 1;
            int zSize = heightfield.Width;
            int xSize = heightfield.Height;
            int maxLayerIndex = 0;
            int zStride = heightfield.Width;
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
                            CommonUtility.RcSetCon(span, dir, RC_NOT_CONNECTED);
                            int neighborX = x + CommonUtility.RcGetDirOffsetX(dir);
                            int neighborZ = z + CommonUtility.RcGetDirOffsetY(dir);

                            if (neighborX < 0 || neighborZ < 0 || neighborX >= xSize || neighborZ >= zSize)
                            {
                                continue;
                            }


                            CompactCell neighborCell = compactHeightfield.CellList[neighborX + neighborZ * zStride];

                            for (int k = neighborCell.Index, nk = (int)(neighborCell.Index + neighborCell.Count); k < nk; ++k)
                            {
                                CompactSpan neighborSpan = compactHeightfield.SpanList[k];
                                int bot = Mathf.Max(span.Y, neighborSpan.Y);
                                int top = Mathf.Min(span.Y + span.H, neighborSpan.Y + neighborSpan.H);

                                //与相邻的空心体素的之间的高度大于行走高度，之间的落差小于可攀爬高度
                                if ((top - bot) >= compactHeightfield.WalkableHeight && Mathf.Abs((int)neighborSpan.Y - (int)span.Y) <= compactHeightfield.WalkableClimb)
                                {
                                    // Mark direction as walkable.
                                    int layerIndex = k - neighborCell.Index;
                                    if (layerIndex < 0 || layerIndex > MAX_LAYERS)
                                    {
                                        maxLayerIndex = Mathf.Max(maxLayerIndex, layerIndex);
                                        continue;
                                    }

                                    CommonUtility.RcSetCon(span, dir, layerIndex);
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

        //用于绘制计算出来的高度场
        public static void BuildHeightfield(Heightfield hf)
        {

            GameObject root = GameObject.Find("EditorRoot");
            if (root)
            {
                GameObject.DestroyImmediate(root);
            }

            root = new GameObject("EditorRoot");

            Vector3 hfBBMin = hf.MinBounds;
            float cellSize = hf.CellSize;
            float cellHeight = hf.CellHeight;

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

                        cube.transform.localScale = new Vector3(cellSize, cellHeight, cellSize);
                        cube.transform.position = new Vector3(cellX, cellY, cellZ);
                        cube.transform.SetParent(root.transform);

                        //Debug.Log(string.Format("x:{0},y:{1},z:{1}", x, y, z));
                    }
                    currentSpan = currentSpan.Next;
                }
            }

        }
    }

}
