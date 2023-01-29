using System;
using System.Diagnostics;

public partial class Detour
{
    // From Thomas Wang, https://gist.github.com/badboy/6267743
    public static uint dtHashRef(ulong a)
    {
        a = (~a) + (a << 18); // a = (a << 18) - a - 1;
        a = a ^ (a >> 31);
        a = a * 21; // a = (a + (a << 2)) + (a << 4);
        a = a ^ (a >> 11);
        a = a + (a << 6);
        a = a ^ (a >> 22);

        return (uint)a;
    }
}

public partial class Detour
{
    public class dtNode
    {
        public float cost;                 //< Cost from previous node to current node.
        public byte flags;                 // : 3;		//< Node flags 0/open/closed.
        public ulong id;                   //< Polygon ref the node corresponds to.
        public uint pidx;                  // : 24;		//< Index to parent node.
        public float[] pos = new float[3]; //< Position of the node.
        public uint state;                 // : 2;	///< extra state information. A polyRef can have multiple nodes with different extra info. see DT_MAX_STATES_PER_NODE
        public float total;                //< Cost up to the node.

        ///
        public static int getSizeOf()
        {
            //C# can't guess the sizeof of the float array, let's pretend
            return sizeof(float) * (3 + 1 + 1) + sizeof(uint) + sizeof(byte) + sizeof(ulong);
        }

        public void dtcsClearFlag(dtNodeFlags flag)
        {
            unchecked
            {
                flags &= (byte)(~flag);
            }
        }

        public void dtcsSetFlag(dtNodeFlags flag)
        {
            flags |= (byte)flag;
        }

        public bool dtcsTestFlag(dtNodeFlags flag)
        {
            return (flags & (byte)flag) != 0;
        }
    };


    public class dtNodePool
    {
        private readonly ushort[] _first;
        private readonly int _hashSize;
        private readonly int _maxNodes;
        private readonly ushort[] _next;
        private readonly dtNode[] _nodes;
        private int _nodeCount;

        //////////////////////////////////////////////////////////////////////////////////////////
        public dtNodePool(int maxNodes, int hashSize)
        {
            _maxNodes = maxNodes;
            _hashSize = hashSize;

            Debug.Assert(dtNextPow2((uint)_hashSize) == (uint)_hashSize);
            Debug.Assert(_maxNodes > 0);

            _nodes = new dtNode[_maxNodes];
            dtcsArrayItemsCreate(_nodes);
            _next = new ushort[_maxNodes];
            _first = new ushort[hashSize];

            Debug.Assert(_nodes != null);
            Debug.Assert(_next != null);
            Debug.Assert(_first != null);

            for (int i = 0; i < hashSize; ++i)
                _first[i] = DT_NULL_IDX;

            for (int i = 0; i < _maxNodes; ++i)
                _next[i] = DT_NULL_IDX;
        }

        public void clear()
        {
            for (int i = 0; i < _hashSize; ++i)
                _first[i] = DT_NULL_IDX;

            _nodeCount = 0;
        }

        public uint getNodeIdx(dtNode node)
        {
            if (node == null)
                return 0;

            return (uint)(Array.IndexOf(_nodes, node)) + 1;
        }

        public dtNode getNodeAtIdx(uint idx)
        {
            if (idx == 0)
                return null;

            return _nodes[idx - 1];
        }

        public int getMemUsed()
        {
            return
                sizeof(int) * 3 +
                dtNode.getSizeOf() * _maxNodes +
                sizeof(ushort) * _maxNodes +
                sizeof(ushort) * _hashSize;
        }

        public int getMaxNodes()
        {
            return _maxNodes;
        }

        public int getHashSize()
        {
            return _hashSize;
        }

        public ushort getFirst(int bucket)
        {
            return _first[bucket];
        }

        public ushort getNext(int i)
        {
            return _next[i];
        }

        public dtNode findNode(ulong id)
        {
            uint bucket = (uint)(dtHashRef(id) & (_hashSize - 1));
            ushort i = _first[bucket];

            while (i != DT_NULL_IDX)
            {
                if (_nodes[i].id == id)
                    return _nodes[i];

                i = _next[i];
            }

            return null;
        }

        public dtNode getNode(ulong id, byte state = 0)
        {
            uint bucket = (uint)(dtHashRef(id) & (_hashSize - 1));
            ushort i = _first[bucket];
            dtNode node = null;

            while (i != DT_NULL_IDX)
            {
                if (_nodes[i].id == id &&
                    _nodes[i].state == state)
                    return _nodes[i];

                i = _next[i];
            }

            if (_nodeCount >= _maxNodes)
                return null;

            i = (ushort)_nodeCount;
            _nodeCount++;

            // Init node
            node = _nodes[i];
            node.pidx = 0;
            node.cost = 0;
            node.total = 0;
            node.id = id;
            node.state = state;
            node.flags = 0;

            _next[i] = _first[bucket];
            _first[bucket] = i;

            return node;
        }
    }


    //////////////////////////////////////////////////////////////////////////////////////////
    public class dtNodeQueue
    {
        private readonly int _capacity;
        private readonly dtNode[] _heap;
        private int _size;

        public dtNodeQueue(int n)
        {
            _capacity = n;
            Debug.Assert(_capacity > 0);

            _heap = new dtNode[_capacity + 1]; //(dtNode**)dtAlloc(sizeof(dtNode*)*(_capacity+1), DT_ALLOC_PERM);
            Debug.Assert(_heap != null);
        }

        public void clear()
        {
            _size = 0;
        }

        public dtNode top()
        {
            return _heap[0];
        }

        public dtNode pop()
        {
            dtNode result = _heap[0];
            _size--;
            trickleDown(0, _heap[_size]);

            return result;
        }

        public void push(dtNode node)
        {
            _size++;
            bubbleUp(_size - 1, node);
        }

        public void modify(dtNode node)
        {
            for (int i = 0; i < _size; ++i)
                if (_heap[i] == node)
                {
                    bubbleUp(i, node);

                    return;
                }
        }

        public bool empty()
        {
            return _size == 0;
        }

        public int getMemUsed()
        {
            return sizeof(int) * 2 +
                   dtNode.getSizeOf() * (_capacity + 1);
        }

        public int getCapacity()
        {
            return _capacity;
        }


        public void bubbleUp(int i, dtNode node)
        {
            int parent = (i - 1) / 2;

            // note: (index > 0) means there is a parent
            while ((i > 0) && (_heap[parent].total > node.total))
            {
                _heap[i] = _heap[parent];
                i = parent;
                parent = (i - 1) / 2;
            }

            _heap[i] = node;
        }

        public void trickleDown(int i, dtNode node)
        {
            int child = (i * 2) + 1;

            while (child < _size)
            {
                if (((child + 1) < _size) &&
                    (_heap[child].total > _heap[child + 1].total))
                    child++;

                _heap[i] = _heap[child];
                i = child;
                child = (i * 2) + 1;
            }

            bubbleUp(i, node);
        }
    }

    public enum dtNodeFlags
    {
        DT_NODE_OPEN = 0x01,
        DT_NODE_CLOSED = 0x02,
        DT_NODE_PARENT_DETACHED = 0x04 // parent of the node is not adjacent. Found using raycast.
    };

    public const ushort DT_NULL_IDX = ushort.MaxValue; //(dtNodeIndex)~0;
}