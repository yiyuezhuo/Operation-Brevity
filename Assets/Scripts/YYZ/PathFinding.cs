using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Data;

namespace YYZ.PathFinding
{

    public interface INodeEnumerable<IndexT>
    {
        IEnumerable<IndexT> Nodes();
    }

    public interface IGeneralGraph<IndexT>
    {
        /// <summary>
        /// src and dst are expected to be neighbor.
        /// </summary>
        float MoveCost(IndexT src, IndexT dst);

        IEnumerable<IndexT> Neighbors(IndexT pos);
    }

    public interface IGraph<IndexT> : IGeneralGraph<IndexT>
    {
        /// <summary>
        /// A heuristic comes from Euclidean space or something like that.
        /// </summary>
        float EstimateCost(IndexT src, IndexT dst);
    }

    public interface IGeneralGraphEnumerable<IndexT> : IGeneralGraph<IndexT>, INodeEnumerable<IndexT>
    {
    }

    public interface IGraphEnumerable<IndexT> : IGraph<IndexT>, INodeEnumerable<IndexT>, IGeneralGraphEnumerable<IndexT>
    {
    }

    public sealed class MinHeap<T>
    {
        private readonly List<(T item, float priority)> _data
            = new List<(T, float)>();

        public int Count => _data.Count;

        public void Clear() => _data.Clear();

        public void Push(T item, float priority)
        {
            _data.Add((item, priority));
            HeapifyUp(_data.Count - 1);
        }

        public T Pop()
        {
            var root = _data[0].item;

            var last = _data[_data.Count - 1];
            _data.RemoveAt(_data.Count - 1);

            if (_data.Count > 0)
            {
                _data[0] = last;
                HeapifyDown(0);
            }

            return root;
        }

        private void HeapifyUp(int i)
        {
            while (i > 0)
            {
                int parent = (i - 1) >> 1;
                if (_data[parent].priority <= _data[i].priority)
                    break;

                (_data[parent], _data[i]) = (_data[i], _data[parent]);
                i = parent;
            }
        }

        private void HeapifyDown(int i)
        {
            int count = _data.Count;
            while (true)
            {
                int left = (i << 1) + 1;
                if (left >= count) break;

                int right = left + 1;
                int smallest = left;

                if (right < count &&
                    _data[right].priority < _data[left].priority)
                {
                    smallest = right;
                }

                if (_data[i].priority <= _data[smallest].priority)
                    break;

                (_data[i], _data[smallest]) = (_data[smallest], _data[i]);
                i = smallest;
            }
        }
    }

    public static class PathFinding<IndexT>
    {
        public static float AStar(
            IGraph<IndexT> graph,
            IndexT src,
            IndexT dst,
            out List<IndexT> path)
        {
            var openSet = new MinHeap<IndexT>();
            var gScore = new Dictionary<IndexT, float>();
            var cameFrom = new Dictionary<IndexT, IndexT>();

            gScore[src] = 0f;
            openSet.Push(src, graph.EstimateCost(src, dst));

            while (openSet.Count > 0)
            {
                var current = openSet.Pop();

                if (EqualityComparer<IndexT>.Default.Equals(current, dst))
                {
                    path = ReconstructPath(cameFrom, current);
                    return gScore[current];
                }

                foreach (var neighbor in graph.Neighbors(current))
                {
                    float tentativeG =
                        gScore[current] + graph.MoveCost(current, neighbor);

                    if (!gScore.TryGetValue(neighbor, out float oldG) ||
                        tentativeG < oldG)
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeG;

                        float f =
                            tentativeG + graph.EstimateCost(neighbor, dst);

                        openSet.Push(neighbor, f);
                    }
                }
            }

            path = new List<IndexT>();
            return float.PositiveInfinity;
        }

        private static List<IndexT> ReconstructPath(
            Dictionary<IndexT, IndexT> cameFrom,
            IndexT current)
        {
            var path = new List<IndexT> { current };

            while (cameFrom.TryGetValue(current, out var prev))
            {
                current = prev;
                path.Add(current);
            }

            path.Reverse();
            return path;
        }
    }
}