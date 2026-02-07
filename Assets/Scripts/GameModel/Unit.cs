using System.Collections.Generic;
using System.Linq;
using YYZ;
using System.Xml.Serialization;
using System;
using YYZ.PathFinding;


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
        Infantry, // Men
        Tank, // Vehicles
        Artillery, // Guns
        AntiTank, // Guns
        AntiAir, // Vehicles
        Cavalry, // Vehicles, Armored Cavalry
        HeadQuarters, // Men
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

    public enum UnitQuality
    {
        A,
        B,
        C,
        D,
        E,
        F
    }

    public class XY
    {
        [XmlAttribute]
        public int x;

        [XmlAttribute]
        public int y;

        public Cell Get() => GameState.Instance.cells[x, y];
    }


    public partial class Unit : IObjectIdLabeled, IOrderOfBattleNode, IHasObjRef
    {
        public string objectId{get; set;}

        // public string name{get; set;}
        public string name = "Unnamed";
        public UnitType unitType;

        Country _country;
        public Country country
        {
            get => _country;
            set
            {
                if(value != _country)
                {
                    _country = value;
                    parameterCached = null;
                }
            }
        }

        // [InvalidateCache(nameof(parameterCached))]
        // Country country{get;set;}

        // public UnitSize unitSize;

        UnitSize _unitSize;
        public UnitSize unitSize
        {
            get => _unitSize;
            set
            {
                if(value != _unitSize)
                {
                    _unitSize = value;
                    parameterCached = null;
                }
            }
        }

        public DeployState deployState;
        public int x;
        public int y;

        // public float hardAttack;
        // public float softAttack;
        // public float defence;
        public int strength; // men, vehicle or guns, following PZC representation style (so vehicle/guns's "men" is ignored)
        public float readiness = 1; // 0~1.0 (0%~100%)
        public float initialStrengthPercent = 1;
        public float initialReadiness = 1;

        public List<XY> waypoints = new();
        public float movementProgressionKm = 0;

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

        public XY missionTargetXY;
        public float missionDepth = 2000; // "Soft" stack limit for strength asset allocation

        public class OrderOfBattleChanged : IEvent{}
        public static OrderOfBattleChanged orderOfBattleChanged = new OrderOfBattleChanged();

        public static Dictionary<(Country, UnitType), UnitParameter> unitParameterMap = new();
        static UnitParameter defaultUnitParameter = new UnitParameter();

        UnitParameter parameterCached;

        public UnitParameter parameter
        {
            get
            {
                if(parameterCached == null)
                {
                    if(unitParameterMap.TryGetValue((country, unitType), out var _parameter))
                    {
                        parameterCached = _parameter;
                    }
                    else
                    {
                        YDebug.LogWarning($"Parameter for {country}, {unitType} is not specified, placeholder fallback is used.");
                        parameterCached = defaultUnitParameter;
                    }
                }
                return parameterCached;
            }
        } 

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

        public void AdvanceTimeMovement(float seconds)
        {
            if(waypoints.Count >= 2)
            {
                var currentCell = GetCell();
                var nextCell = waypoints[1].Get();
                if(currentCell != null && nextCell != null)
                {
                    // TODO: Check neighbor relationship is valid
                    var coef = currentCell.GetMovementCoef(nextCell);
                    var speedCoef = 1 / coef;
                    var moveCapKm = parameter.Speed * speedCoef * seconds / 3600;

                    if(retreating)
                    {
                        moveCapKm *= 1.25f; // +25% speed for retreating unit
                    }

                    // var enemyBlocked = nextCell.UnitRefs.Any(r => (r.Get() as Unit).side != side);
                    var enemyBlocked = nextCell.HasResistTo(side);

                    if(enemyBlocked && retreating)
                    {
                        var fallbackCell = SelectRetreatCell(currentCell);
                        if(fallbackCell == null)
                        {
                            MoveTo(null, true); // surrender
                        }
                        else
                        {
                            SetWaypoints(new(){currentCell, fallbackCell});
                            movementProgressionKm = 0;
                        }
                        return;
                    }

                    if(!enemyBlocked && moveCapKm + movementProgressionKm > ModelUtils.hexDistanceKm)
                    {
                        movementProgressionKm = 0;
                        MoveTo(nextCell);
                        waypoints.RemoveAt(0);
                        if(waypoints.Count < 2)
                        {
                            waypoints.Clear();
                        }

                        if(retreating)
                        {
                            retreating = false;
                        }
                    }
                    else
                    {
                        movementProgressionKm = Math.Min(moveCapKm + movementProgressionKm, ModelUtils.hexDistanceKm);
                    }
                }
            }
        }

        public void SetWaypoints(List<Cell> cells)
        {
            // TODO: If first cell is identical to previous path, move progression is not reset.
            // TODO: Reset movement progression

            waypoints = cells.Select(c => c.ToXY()).ToList();
        }

        Side sideCached;
        bool sideDirty = true;
        public Side side
        {
            get
            {
                if(sideDirty)
                {
                    sideDirty = false;
                    IOrderOfBattleNode pt = this;
                    while(pt != null)
                    {
                        if(pt is Side side)
                        {
                            sideCached = side;
                            break;
                        }
                        pt = pt.parent;
                    }
                }
                return sideCached;
            }
        }

        List<IOrderOfBattleNode> parentsAndMeCached;
        bool parentsAndMeDirty = true;

        [XmlIgnore]
        public List<IOrderOfBattleNode> parentsAndMe
        {
            get
            {
                if(parentsAndMeDirty)
                {
                    parentsAndMeDirty = false;

                    var list = new List<IOrderOfBattleNode>();
                    IOrderOfBattleNode pt = this;
                    while(pt != null)
                    {
                        list.Add(pt);
                        pt = pt.parent;
                    }
                    list.Reverse();
                    parentsAndMeCached = list;
                }
                return parentsAndMeCached;
            }
        }

        public void PlanFormationMovement()
        {
            var missionTarget = missionTargetXY?.Get();
            if(missionTarget == null)
                return;
            
            var commandedUnits = CollectSubordinateNotIncludeDetached(this).ToList();
            var mapSet = side.influenceMapSet;

            // mapSet.frontline.distanceToCells.GetValueOrDefault(-1);
            var kv = mapSet.frontline.distanceToCells.FirstOrDefault(kv => kv.Value.Contains(missionTarget));
            var frontlineDist = kv.Key;
            var frolineCells = kv.Value;
            if(frolineCells == null) // default(KeyValuePair<TKey, TValue>) = KeyValuePair(default(TKey), default(TValue))
                return;

            var unitsUnderControl = commandedUnits.ToList();

            var grouping = unitsUnderControl.GroupBy(u => u.parameter.IsLineUnit()).ToDictionary(g => g.Key, g=>g.ToList());
            if(grouping.TryGetValue(true, out var lineUnits))
            {
                PlanFormationMovement(missionTarget, frolineCells, lineUnits);
            }
            if(grouping.TryGetValue(false, out var supportUnits))
            {
                var frolineCells2 = mapSet.frontline.distanceToCells.GetValueOrDefault(frontlineDist + 1, frolineCells);
                var missionTarget2 = DynamicCellGraphArmy.Instance.Neighbors(missionTarget).FirstOrDefault(nei => frolineCells2.Contains(nei)) ?? missionTarget;
                PlanFormationMovement(missionTarget2, frolineCells2, supportUnits);
            }
        }

        void PlanFormationMovement(Cell missionTarget, HashSet<Cell> frolineCells, List<Unit> notAllocatedUnits)
        {
            HashSet<Cell> closeSet = new(){missionTarget};
            Dictionary<Cell, float> allocatedPowerMap = new()
            {
                [missionTarget] = 0
            };
            List<Cell> activeSet = new(){missionTarget};

            while(activeSet.Count > 0 && notAllocatedUnits.Count > 0)
            {
                var currentUnit = notAllocatedUnits[0];
                var currentCell = activeSet.First();
                currentUnit.TryPlanPathTo(currentCell);
                allocatedPowerMap[currentCell] += currentUnit.GetPower();
                
                if(allocatedPowerMap[currentCell] >= missionDepth)
                {
                    activeSet.Remove(currentCell);

                    // Try to expand across frontline
                    foreach(var nei in DynamicCellGraphArmy.Instance.Neighbors(currentCell))
                    {
                        if(!closeSet.Contains(nei) && frolineCells.Contains(nei))
                        {
                            activeSet.Add(nei);
                            allocatedPowerMap[nei] = 0;
                        }
                    }

                }
                notAllocatedUnits.RemoveAt(0);
            }

            // Placeholder/Fallback all-movement behaviour 
            foreach(var unit in notAllocatedUnits)
            {
                // unit.SetWaypoints()
                unit.TryPlanPathTo(missionTarget);
            }
        }

        public static IEnumerable<Unit> CollectSubordinateNotIncludeDetached(Unit unit)
        {
            yield return unit;

            foreach(var obj in unit.children)
            {
                var subUnit = obj as Unit;
                if(subUnit != null && subUnit.deployState == DeployState.Deployed && subUnit.missionTargetXY == null)
                {
                    foreach(var _subUnit in CollectSubordinateNotIncludeDetached(subUnit))
                    {
                        yield return _subUnit;
                    }
                }
            }
        }

        public void SetAllDirty()
        {
            sideDirty = true;
            parentsAndMeDirty = true;
        }

        public override string ToString()
        {
            return $"Unit({name})";
        }

        public float GetPower()
        {
            // return parameter.GetPower();
            return GetAssaultValue();
        }

        public string GetStrengthWord() => parameter.GetStrengthWord(strength);

        public bool retreating = false;

        public bool IsOperational() => deployState == DeployState.Deployed && !retreating;

        public float GetHitWeight() => strength * parameter.category.strengthCoef;
        public float GetAssaultValue() => strength * parameter.Assault * parameter.category.strengthCoef * readiness;

        public void ForceRetreat()
        {
            var currentCell = GetCell();
            retreating = true;

            // var retreatToCells = currentCell.GetNeighbors().Where(cell => !cell.HasResistTo(side)).ToList();
            var retreatToCell = SelectRetreatCell(currentCell);
            if(retreatToCell == null)
            {
                MoveTo(null, true); // surrender
            }
            else
            {
                SetWaypoints(new(){currentCell, retreatToCell});
            }
        }

        Cell SelectRetreatCell(Cell currentCell)
        {
            if(currentCell == null)
                return null;

            var retreatToCells = DynamicCellGraphArmy.Instance.Neighbors(currentCell)
                .Where(cell => !cell.HasResistTo(side))
                .ToList();
            if(retreatToCells.Count == 0)
                return null;

            // TODO: Consider priority
            var retreatToCellsHostileNeighbors = retreatToCells
                .Select(c => c.GetNeighbors().Count(nei => nei.HasResistTo(side)))
                .ToList();
            var minHostileCount = retreatToCellsHostileNeighbors.Min();

            var minSet = new List<Cell>();
            for(int i=0; i<retreatToCellsHostileNeighbors.Count; i++)
            {
                if(retreatToCellsHostileNeighbors[i] == minHostileCount)
                {
                    minSet.Add(retreatToCells[i]);
                }
            }

            return RandomUtils.Sample(minSet);
        }

        public void TryPlanPathTo(Cell dstCell)
        {
            // var graph = new DynamicCellGraphArmy();
            var graph = DynamicCellGraphArmy.Instance;
            var srcCell = GetCell();
            if(srcCell != null)
            {
                var cost = PathFinding<Cell>.AStar(graph, srcCell, dstCell, out var path);
                SetWaypoints(path);
            }
        }

        public void AdvanceTimeRestore(float seconds)
        {
            var readinessRestored = seconds / 3600 / 24;
            readiness = Math.Min(1, readiness + readinessRestored);
        }

        public void ReattachToSuperior()
        {
            missionTargetXY = null;
        }

        public bool IsDetached() => missionTargetXY != null;

        public void ReattachAllSubordinates()
        {
            foreach(var obj in children)
            {
                var unit = obj as Unit;
                if(unit != null && unit.IsDetached())
                {
                    unit.ReattachToSuperior();
                }
                unit.ReattachAllSubordinates();
            }
        }

        public void AllSuboridnateStop()
        {
            SetWaypoints(new());
            foreach(var obj in children)
            {
                var unit = obj as Unit;
                if(unit != null && !unit.IsDetached())
                {
                    // unit.ReattachToSuperior();
                    unit.AllSuboridnateStop();
                }
            }
        }
    }
}
