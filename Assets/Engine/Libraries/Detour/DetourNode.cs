
using System;

namespace GameFramework.Detour
{

    public class DtNode
    {
        public float[] pos = new float[3];
        public float cost;
        public float total;
        public int id;
        public int pidx = DetourConfig.DT_NODE_PARENT_BITS;
        public int flags = 3;
    }

    internal class DtNodeQueue
    {
        private DtNode[] _heap;
        private int _capacity;
        private int _size;


        public DtNodeQueue(int n)
        {
            _capacity = n;
            _size = 0;
            _heap = new DtNode[n + 1];
        }

        public int GetCapacity()
        {
            return _capacity;
        }

        public void Clear()
        {
            _size = 0;
        }

        public void Push(DtNode node)
        {
            _size++;
            BubbleUp(_size - 1, node);
        }

        public bool Empty()
        {
            return _size == 0;
        }

        public DtNode Pop()
        {
            DtNode result = _heap[0];
            _size--;
            TrickleDown(0, _heap[_size]);
            return result;
        }

        public void Modify(DtNode node)
        {
            for (int i = 0; i < _size; ++i)
            {
                if (_heap[i] == node)
                {
                    BubbleUp(i, node);
                    return;
                }
            }
        }

        private void BubbleUp(int i, DtNode node)
        {
            int parent = (i - 1) / 2;
            while ((i > 0) && (_heap[parent].total > node.total))
            {
                _heap[i] = _heap[parent];
                i = parent;
                parent = (i - 1) / 2;
            }
            _heap[i] = node;
        }

        private void TrickleDown(int i, DtNode node)
        {
            int child = (i * 2) + 1;
            while (child < _size)
            {
                if (((child + 1) < _size) &&
                    (_heap[child].total > _heap[child + 1].total))
                {
                    child++;
                }
                _heap[i] = _heap[child];
                i = child;
                child = (i * 2) + 1;
            }
            BubbleUp(i, node);
        }
    }


    internal class DtNodePool
    {
        private int _maxNodes;
        private int _hashSize;
        private int _nodeCount;
        private DtNode[] _nodes;
        private int[] _next;
        private int[] _first;
        public DtNodePool(int maxNodes, int hashSize)
        {
            _maxNodes = maxNodes;
            _hashSize = hashSize;

            _nodes = new DtNode[maxNodes];

            for (int i = 0; i < maxNodes; i++)
            {
                _nodes[i] = new DtNode();
            }
            _first = new int[hashSize];
            Array.Fill(_first, DetourConfig.DT_NULL_IDX);
            _next = new int[maxNodes];
            Array.Fill(_next, DetourConfig.DT_NULL_IDX);
        }

        public DtNode GetNode(int id)
        {
            int bucket = DtHashRef(id) & (_hashSize - 1);
            int i = _first[bucket];

            while (i != DetourConfig.DT_NULL_IDX)
            {
                if (_nodes[i].id == id)
                {
                    return _nodes[i];
                }

                i = _next[i];
            }

            if (_nodeCount >= _maxNodes)
                return null;

            i = _nodeCount;
            _nodeCount++;

            DtNode node = _nodes[i];
            node.pidx = 0;
            node.cost = 0;
            node.total = 0;
            node.id = id;
            node.flags = 0;

            _next[i] = _first[bucket];
            _first[bucket] = i;

            return node;
        }


        private int DtHashRef(int a)
        {
            a += ~(a << 15);
            a ^= (a >> 10);
            a += (a << 3);
            a ^= (a >> 6);
            a += ~(a << 11);
            a ^= (a >> 16);
            return (int)a;
        }

        public int GetMaxNodes()
        {
            return _maxNodes;
        }

        public void Clear()
        {
            Array.Fill(_first, DetourConfig.DT_NULL_IDX);
            _nodeCount = 0;
        }

        public int GetNodeIdx(DtNode node)
        {
            if (node == null)
            {
                return 0;
            }
            return Array.IndexOf(_nodes, node) + 1;
        }

        public DtNode GetNodeAtIdx(int idx)
        {
            if (idx == 0)
            {
                return null;
            }
            return _nodes[idx - 1];
        }
    }
}
