using System;

namespace GameFramework.Common
{

    public abstract class BTreeNode
    {
        public int idx = 0;

        //创建节点(父节点)
        public abstract void CreatNode(BTreeNode[] nodes, int imin, int imax);

        //排序节点
        public abstract void SortNode(BTreeNode[] nodes, int imin, int imax);

        //判断节点是否包含
        public virtual bool Contain(params object[] paramList)
        {
            return false;
        }

        //判断节点是否更优
        public virtual bool CompareNode(BTreeNode node, params object[] paramList)
        {
            return false;
        }

    }


    public class BTree<T> where T : BTreeNode, new()
    {
        private T[] _treeNodes;
        private int _nodesNum = 0;

        public T[] treeNodes
        {
            get { return _treeNodes; }
        }

        public int nodesNum
        {
            get { return _nodesNum; }
        }


        public void BuildTree(T[] nodes)
        {

            _treeNodes = new T[nodes.Length * 2];
            for (int i = 0; i < _treeNodes.Length; i++)
            {
                _treeNodes[i] = new T();
            }

            _nodesNum = SubDivide(nodes, 0, nodes.Length, _treeNodes, 0);
        }

        //递归构建二叉树
        private int SubDivide(T[] nodes, int imin, int imax, T[] outNodes, int curNode)
        {
            int inum = imax - imin;
            int icur = curNode;

            T node = outNodes[curNode++];
            if (inum == 1)
            {
                node = nodes[imin];
                node.idx = icur;
            }
            else
            {

                node.CreatNode(nodes, imin, imax);

                node.SortNode(nodes, imin, imax);

                int isplit = imin + (int)Math.Floor((double)(inum / 2));

                // 左侧
                curNode = SubDivide(nodes, imin, isplit, outNodes, curNode);
                // 右侧
                curNode = SubDivide(nodes, isplit, imax, outNodes, curNode);

                int iescape = curNode - icur;
                node.idx = -iescape;
            }

            return curNode;
        }

        public T Search(params object[] paramList)
        {
            int cur = 0;
            int end = _nodesNum;

            int bestIdx = -1;

            while (cur < end)
            {
                T node = _treeNodes[cur];
                bool isLeaf = node.idx >= 0;

                bool overlap = node.Contain(paramList);

                if (isLeaf && overlap)
                {
                    if (node.CompareNode(_treeNodes[bestIdx],paramList))
                    {
                        bestIdx = cur;
                    }
                }

                if (overlap || isLeaf)
                {
                    ++cur;
                }
                else
                {
                    cur -= node.idx;
                }
            }


            if(bestIdx < 0)
            {
                return null;
            }
            else
            {
                return _treeNodes[bestIdx];
            }

        }


    }
}
