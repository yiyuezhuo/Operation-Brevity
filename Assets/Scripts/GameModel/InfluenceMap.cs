using System;
using System.Collections.Generic;

namespace GameModel
{
    public class InfluenceMap
    {
        public float[,] matrix;
        public static float decayCoef = 0.2f;
        public float threshold = 1;

        public InfluenceMap(int width, int height)
        {
            matrix = new float[width, height];
        }

        public void AddSource(Cell srcCell, float influence)
        {
            var graph = DynamicCellGraphArmy.Instance;
            var activeSet = new List<Cell>(){srcCell};
            var newActiveSet = new List<Cell>();

            var influenceMap = new Dictionary<Cell, float>()
            {
                [srcCell] = influence
            };

            while(activeSet.Count > 0)
            {
                foreach(var activeCell in activeSet)
                {
                    var activeCellValue = influenceMap[activeCell];
                    if(activeCellValue < threshold)
                        continue;

                    foreach(var neiCell in graph.Neighbors(activeCell))
                    {
                        if(!influenceMap.TryGetValue(neiCell, out var prevValue))
                        {
                            influenceMap[neiCell] = prevValue = 0;
                        }
                        var newValue = activeCellValue * MathF.Exp(-decayCoef * graph.MoveCost(activeCell, neiCell));
                        if(newValue > prevValue)
                        {
                            influenceMap[neiCell] = newValue;
                            newActiveSet.Add(neiCell);
                        }
                    }
                }

                (newActiveSet, activeSet) = (activeSet, newActiveSet);
                newActiveSet.Clear();
            }

            foreach(var(cell, value) in influenceMap)
            {
                matrix[cell.x, cell.y] += value;
            }
        }

        public void Plus(InfluenceMap other)
        {
            var xl = matrix.GetLength(0);
            var yl = matrix.GetLength(1);
            for(int x=0; x<xl; x++)
            {
                for(int y=0; y<yl; y++)
                {
                    matrix[x, y] += other.matrix[x, y];
                }
            }
        }

        public void Subtract(InfluenceMap other)
        {
            var xl = matrix.GetLength(0);
            var yl = matrix.GetLength(1);
            for(int x=0; x<xl; x++)
            {
                for(int y=0; y<yl; y++)
                {
                    matrix[x, y] -= other.matrix[x, y];
                }
            }
        }
    }
}