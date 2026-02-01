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
        Fronline,
        Threat
    }


    public class InfluenceMapSet
    {
        public InfluenceMap strength;
        public InfluenceMap control;
        public InfluenceMap frontline;
        public InfluenceMap threat;

        public InfluenceMap Get(InfluenceMapType type)
        {
            return type switch
            {
                InfluenceMapType.Strength => strength,
                InfluenceMapType.Control => control,
                InfluenceMapType.Fronline => frontline,
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

            var strengthMap = influenceMapSet.strength = new(cells.GetLength(0), cells.GetLength(1));

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
            var cells = GameState.Instance.cells;

            var otherSide = GetOtherSide();
            if(otherSide != null)
            {
                var controlMap = influenceMapSet.control = new(cells.GetLength(0), cells.GetLength(1));
                controlMap.Plus(influenceMapSet.strength);
                controlMap.Subtract(otherSide.influenceMapSet.strength);
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