using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using System.Xml;


namespace GameModel
{
    public enum InfluenceMapType
    {
        Strength,
        Control,
        Frontline,
        Threat
    }


    public class InfluenceMapSet
    {
        public InfluenceMap strength;
        public InfluenceMap control;
        public FrontlineMap frontline;
        public InfluenceMap threat;

        public InfluenceMap Get(InfluenceMapType type)
        {
            return type switch
            {
                InfluenceMapType.Strength => strength,
                InfluenceMapType.Control => control,
                InfluenceMapType.Frontline => frontline,
                InfluenceMapType.Threat => threat,
                _ => null
            };
        }
    }

    public partial class Side : IObjectIdLabeled, IOrderOfBattleNode, IHasObjRef
    {
        public string objectId{get; set;}

        // public string name{get; set;}
        public string name;

        public IEnumerable<IObjectIdLabeled> GetSubObjects()
        {
            yield break;
        }

        public List<ObjRef> oobChildrenRefs = new();

        public IOrderOfBattleNode parent => null;
        public IEnumerable<IOrderOfBattleNode> children => oobChildrenRefs.Select(x => x.Get() as IOrderOfBattleNode);

        [XmlIgnore]
        public InfluenceMapSet influenceMapSet = new();

        public void CalcualteStrengthMap()
        {
            var gameState = GameState.Instance;
            var cells = gameState.cells;

            // var strengthMap = influenceMapSet.strength = new(cells.GetLength(0), cells.GetLength(1));

            var strengthMap = influenceMapSet.strength = new();

            foreach(var g in gameState.units.Where(u => u.deployState == DeployState.Deployed && u.side == this).GroupBy(u => u.GetCell()))
            {
                var cell = g.Key;
                var strength = g.Sum(u => u.GetPower());
                strengthMap.AddSource(cell, strength);
            }
        }

        public Side GetOtherSide() => GameState.Instance.sides.FirstOrDefault(s => s != this);

        public void CalcualteControlMap()
        {
            // var cells = GameState.Instance.cells;

            var otherSide = GetOtherSide();
            if(otherSide != null)
            {
                // var controlMap = influenceMapSet.control = new(cells.GetLength(0), cells.GetLength(1));
                var controlMap = influenceMapSet.control = new();
                controlMap.Plus(influenceMapSet.strength);
                controlMap.Subtract(otherSide.influenceMapSet.strength);
            }
        }

        public void CalculateFrontlineMap()
        {
            var frontlineMap = influenceMapSet.frontline = new();
            var xl = frontlineMap.matrix.GetLength(0);
            var yl = frontlineMap.matrix.GetLength(1);
            
            var cells = GameState.Instance.cells;
            var graph = DynamicCellGraphArmy.Instance;
            var controlMatrix = influenceMapSet.control.matrix;

            // First Scan
            var dist0cells = frontlineMap.distanceToCells[0] = new();
            for(int x=0; x<xl; x++)
            {
                for(int y=0; y<yl; y++)
                {
                    var cell = cells[x, y];
                    if(!cell.IsArmyPassable())
                    {
                        frontlineMap.matrix[x, y] = float.NaN;
                        continue;
                    }

                    if(controlMatrix[x, y] <= 0)
                    {
                        frontlineMap.matrix[x, y] = -9999;
                        continue;
                    }
                    
                    // So controlMatrix[x, y] > 0
                    if(graph.Neighbors(cell).Any(nei => controlMatrix[nei.x, nei.y] < 0))
                    {
                        frontlineMap.matrix[x, y] = 0;
                        dist0cells.Add(cell);
                    }
                    else
                    {
                        frontlineMap.matrix[x, y] = 9999;
                    }
                }
            }

            var closeSet = dist0cells.ToHashSet();
            var activeSet = dist0cells.ToList();
            
            var toAbsLayer = 1;
            var positiveCells = frontlineMap.distanceToCells[toAbsLayer] = new();
            var negativeCells = frontlineMap.distanceToCells[-toAbsLayer] = new();
            
            while(activeSet.Count > 0)
            {
                var newActiveSet = new List<Cell>();

                foreach(var activeCell in activeSet)
                {
                    foreach(var neiCell in graph.Neighbors(activeCell))
                    {
                        if(!closeSet.Contains(neiCell))
                        {
                            var frontlineOldValue = frontlineMap.matrix[neiCell.x, neiCell.y];
                            if(frontlineOldValue < 0) // inner broadcast
                            {
                                frontlineMap.matrix[neiCell.x, neiCell.y] = -toAbsLayer;
                                negativeCells.Add(neiCell);
                            }
                            else if(frontlineOldValue > 0) // outer broadcast
                            {
                                frontlineMap.matrix[neiCell.x, neiCell.y] = +toAbsLayer;
                                positiveCells.Add(neiCell);
                            }
                            closeSet.Add(neiCell);
                            newActiveSet.Add(neiCell);
                        }
                    }
                }

                activeSet = newActiveSet;
                newActiveSet = new();
                toAbsLayer += 1;
                positiveCells = frontlineMap.distanceToCells[toAbsLayer] = new();
                negativeCells = frontlineMap.distanceToCells[-toAbsLayer] = new();
            }
        }

        public override string ToString()
        {
            return $"Side({name})";
        }

        public IEnumerable<ObjRef> IterateObjRefs()
        {
            foreach(var objRef in oobChildrenRefs)
                yield return objRef;
        }
    }
}