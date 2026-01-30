
using YYZ.PathFinding;
using YYZ;
using System;
using System.Collections.Generic;

namespace GameModel
{
    public class DynamicCellGraphArmy : IGraphEnumerable<Cell>
    {
        static DynamicCellGraphArmy instance = new();
        public static DynamicCellGraphArmy Instance => instance;

        public IEnumerable<Cell> Neighbors(Cell pos)
        {
            foreach (var nei in pos.GetNeighbors())
            {
                // if (nei.IsArmyPassable())
                //     yield return nei;
                if (nei.IsArmyPassable() && !GameState.Instance.GetEdgeFeature(pos, nei, EdgeFeatureType.Escarpment))
                {
                    yield return nei;
                }
            }
        }

        public float EstimateCost(Cell src, Cell dst)
        {
            // return Math.Abs(src.x - dst.x) + Math.Abs(src.y - dst.y);
            return (Math.Abs(src.x - dst.x) + Math.Abs(src.y - dst.y)) / 2f;
        }

        // public float MoveCost(Cell src, Cell dst) => 1;
        public float MoveCost(Cell src, Cell dst) => src.GetMovementCoef(dst);

        public IEnumerable<Cell> Nodes()
        {
            foreach (var cell in GameState.Instance.IterateCells())
                yield return cell;
        }
    }
}