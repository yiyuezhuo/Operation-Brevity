using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using YYZ;

namespace GameModel
{
    public enum Country // mainly for color schema
    {
        Britain,
        Germany,
        Italy,
    }

    public enum UnitType // mainly for unit icon
    {
        Infantry,
        Tank,
        Artillery,
        AntiTank,
        AntiAir,
        Cavalry, // Armored Cavalry
        HeadQuarters,
    }

    public enum UnitSize
    {
        NotSpecified,
        Platton,
        Company, // Squadron
        Battalion,
        Regiment,
        Brigade,
        Division
    }

    public enum DeployState
    {
        NotDeployed,
        Deployed,
        Destroyed
    }


    public partial class Unit : IObjectIdLabeled, IOrderOfBattleNode, IHasObjRef
    {
        public string objectId{get; set;}

        // public string name{get; set;}
        public string name = "Unnamed";
        public UnitType unitType;
        public Country country;
        public UnitSize unitSize;
        public DeployState deployState;
        public int x;
        public int y;

        public float hardAttack;
        public float softAttack;
        public float defence;
        public float strength; // men, vehicle or guns

        public ObjRef oobParentRef = new(); // reference to another Unit or Side
        public List<ObjRef> oobChildrenRefs = new();

        public IEnumerable<ObjRef> IterateObjRefs()
        {
            yield return oobParentRef;
            foreach(var objRef in oobChildrenRefs)
                yield return objRef;
        }

        public IOrderOfBattleNode parent => oobParentRef.Get() as IOrderOfBattleNode;
        public IEnumerable<IOrderOfBattleNode> children => oobChildrenRefs.Select(x => x.Get() as IOrderOfBattleNode);

        public IEnumerable<IObjectIdLabeled> GetSubObjects()
        {
            yield break;
        }

        public class OrderOfBattleChanged : IEvent{}
        public static OrderOfBattleChanged orderOfBattleChanged = new OrderOfBattleChanged();

        public void AttachTo(IOrderOfBattleNode newParent)
        {
            if(parent != null)
            {
                if(parent is Side parentSide)
                {
                    parentSide.oobChildrenRefs.RemoveAll(r => r.Get() == this);
                }
                else if(parent is Unit parentUnit)
                {
                    parentUnit.oobChildrenRefs.RemoveAll(r => r.Get() == this);
                }
            }

            if(newParent != null)
            {
                if(newParent is Side newParentSide)
                {
                    newParentSide.oobChildrenRefs.Add(new ObjRef() { objectId = objectId });
                }
                else if(newParent is Unit newParentUnit)
                {
                    newParentUnit.oobChildrenRefs.Add(new ObjRef() { objectId = objectId });
                }
            }

            oobParentRef.Set(newParent as IObjectIdLabeled);

            EventBus.Publish(orderOfBattleChanged);
        }

        public Cell GetCell() => deployState == DeployState.Deployed ? GameState.Instance.cells[x, y] : null;

        public class MapUnitsChanged : IEvent{}
        public static MapUnitsChanged mapUnitsChanged = new();

        public class StacksChanged : IEvent{}
        public static StacksChanged stacksChanged = new();


        public void MoveTo(Cell cell, bool destroyed = false)
        {
            var currentCell = GetCell();
            if(currentCell != null)
            {
                currentCell.UnitRefs.RemoveAll(r => r.objectId == objectId);
            }
            else
            {
                // Add to map event
                EventBus.Publish(mapUnitsChanged);
            }

            if(cell != null)
            {
                deployState = DeployState.Deployed;
                x = cell.x;
                y = cell.y;
                cell.UnitRefs.Add(new ObjRef { objectId = objectId });
            }
            else
            {
                deployState = destroyed ? DeployState.Destroyed : DeployState.NotDeployed;
                // remove from map event
                EventBus.Publish(mapUnitsChanged);
            }

            EventBus.Publish(stacksChanged);
        }

        public override string ToString()
        {
            return $"Unit({name})";
        }
    }
}