
using UnityEngine;

namespace GameEditor.RecastEditor
{
    internal class RecastHeightField
    {
        public static void RcRasterizeTriangles(Vector3[] verts, int[] tris, AREATYPE[] areas, Heightfield hf)
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

        private static bool RasterizeTri(Vector3 v0, Vector3 v1, Vector3 v2, AREATYPE areaType, Heightfield hf)
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
            Vector3[] nvRowList;
            Vector3[] p1InList;

            nvInList[0] = v0;
            nvInList[1] = v1;
            nvInList[2] = v2;

            int nvRow;
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


                int nvRow2;
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


        private static bool AddSpan(Heightfield hf, int x, int z, int min, int max, AREATYPE areaType)
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
                        int top = span.Next != null ? span.Next.Min : RecastConfig.MAX_HEIGHT;


                        float minNeighborHeight = RecastConfig.MAX_HEIGHT;

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
                            int neighborTop = neighborSpan != null ? neighborSpan.Min : RecastConfig.MAX_HEIGHT;

                            // 只处理上下表面的距离大于WalkableHeight的部分，先默认处理一次？？？？
                            if (Mathf.Min(top, neighborTop) - Mathf.Max(bot, neighborBot) > hf.WalkableHeight)
                            {
                                minNeighborHeight = Mathf.Min(minNeighborHeight, neighborBot - bot);
                            }

                            for (neighborSpan = hf.SpanList[dx + dy * xSize]; neighborSpan != null; neighborSpan = neighborSpan.Next)
                            {
                                neighborBot = neighborSpan.Max;
                                neighborTop = neighborSpan.Next != null ? neighborSpan.Next.Min : RecastConfig.MAX_HEIGHT;

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
                        int top = span.Next != null ? span.Next.Min : RecastConfig.MAX_HEIGHT;
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
                        int top = span.Next != null ? span.Next.Min : RecastConfig.MAX_HEIGHT;

                        compactHeightfield.SpanList[currentCellIndex] = new CompactSpan(Mathf.Clamp(bot, 0, RecastConfig.MAX_HEIGHT), Mathf.Clamp(top - bot, 0, RecastConfig.MAX_HEIGHT), span.AreaID);
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

                            RecastUtility.RcSetCon(span, dir, RecastConfig.RC_NOT_CONNECTED);
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
                                    if (layerIndex < 0 || layerIndex > RecastConfig.MAX_LAYERS)
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
            if (maxLayerIndex > RecastConfig.MAX_LAYERS)
            {
                Debug.LogWarning(string.Format("rcBuildCompactHeightfield: Heightfield has too many layers %d (max: %d)", maxLayerIndex, RecastConfig.MAX_LAYERS));
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
                            if (neighborConnection == RecastConfig.RC_NOT_CONNECTED)
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

                        if (RecastUtility.RcGetCon(span, 0) != RecastConfig.RC_NOT_CONNECTED)
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
                            if (RecastUtility.RcGetCon(aSpan, 3) != RecastConfig.RC_NOT_CONNECTED)
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
                        if (RecastUtility.RcGetCon(span, 3) != RecastConfig.RC_NOT_CONNECTED)
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
                            if (RecastUtility.RcGetCon(aSpan, 2) != RecastConfig.RC_NOT_CONNECTED)
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

                        if (RecastUtility.RcGetCon(span, 2) != RecastConfig.RC_NOT_CONNECTED)
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
                            if (RecastUtility.RcGetCon(aSpan, 1) != RecastConfig.RC_NOT_CONNECTED)
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
                        if (RecastUtility.RcGetCon(span, 1) != RecastConfig.RC_NOT_CONNECTED)
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
                            if (RecastUtility.RcGetCon(aSpan, 0) != RecastConfig.RC_NOT_CONNECTED)
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
                        if (PointInPoly(vertices, point))
                        {
                            compactHeightfield.AreaList[spanIndex] = areaId;
                        }
                    }
                }
            }


        }


        private static bool PointInPoly(Vector3[] verts, float[] point)
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
            BoxBlur(compactHeightfield, 1);

        }


        private static void CalculateDistanceField(CompactHeightfield compactHeightfield)
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
                            if (neighborConnection == RecastConfig.RC_NOT_CONNECTED)
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

                        if (RecastUtility.RcGetCon(span, 0) != RecastConfig.RC_NOT_CONNECTED)
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
                            if (RecastUtility.RcGetCon(aSpan, 3) != RecastConfig.RC_NOT_CONNECTED)
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
                        if (RecastUtility.RcGetCon(span, 3) != RecastConfig.RC_NOT_CONNECTED)
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
                            if (RecastUtility.RcGetCon(aSpan, 2) != RecastConfig.RC_NOT_CONNECTED)
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

                        if (RecastUtility.RcGetCon(span, 2) != RecastConfig.RC_NOT_CONNECTED)
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
                            if (RecastUtility.RcGetCon(aSpan, 1) != RecastConfig.RC_NOT_CONNECTED)
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
                        if (RecastUtility.RcGetCon(span, 1) != RecastConfig.RC_NOT_CONNECTED)
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
                            if (RecastUtility.RcGetCon(aSpan, 0) != RecastConfig.RC_NOT_CONNECTED)
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


        private static void BoxBlur(CompactHeightfield compactHeightfield, int thr)
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
                            if (neighborConnection != RecastConfig.RC_NOT_CONNECTED)
                            {
                                int neighborX = x + RecastUtility.RcGetDirOffsetX(direction);
                                int neighborZ = z + RecastUtility.RcGetDirOffsetY(direction);
                                int neighborSpanIndex = compactHeightfield.CellList[neighborX + neighborZ * zStride].Index + neighborConnection;

                                d = d + compactHeightfield.DistanceToBoundary[neighborSpanIndex];

                                CompactSpan neighborSpan = compactHeightfield.SpanList[neighborSpanIndex];

                                int direction2 = (direction + 1) & 0x3;
                                int neighbor2Connection = RecastUtility.RcGetCon(neighborSpan, direction2);
                                if (neighbor2Connection != RecastConfig.RC_NOT_CONNECTED)
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

    }
}
