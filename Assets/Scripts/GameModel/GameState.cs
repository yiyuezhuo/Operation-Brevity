using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Hashing;
using System.Linq;
using System.Xml.Serialization;
using UnityEditor.Animations;
using YYZ;


namespace GameModel
{
    public enum TerrainType
    {
        Desert,
        Water,
    }


    public class SerializedCells
    {
        public int width;
        public int height;
        public List<Cell> records;
    }

    public interface IOrderOfBattleNode
    {
        // public string name{get;}
        public IEnumerable<IOrderOfBattleNode> children{get;}
        public IOrderOfBattleNode parent{get;}

        public IEnumerable<T> WalkChildren<T>()
        {
            foreach(var child in children)
            {
                if(child is T t)
                    yield return t;
                
                foreach(var _child in child.WalkChildren<T>())
                {
                    yield return _child;
                }
            }
        }

    }

    public class GameState
    {
        [XmlIgnore]
        public DateTimeOffset time = new DateTimeOffset(1941, 5, 15, 6, 0, 0, TimeSpan.FromHours(1));

        public DateTime serializedTime
        {
            get => time.UtcDateTime;
            // set => time = new DateTimeOffset(value, TimeSpan.FromHours(1));
            set => time = new DateTimeOffset(value).ToOffset(TimeSpan.FromHours(1));
        }

        public bool firstLoaded = false;

        Cell[,] _cells = new Cell[0, 0];
        
        [XmlIgnore]
        public Cell[,] cells
        {
            get => _cells;
            set
            {
                _cells = value;
                // cellsChanged?.Invoke(this, EventArgs.Empty);
                EventBus.Publish(cellsChanged);
            }
        }

        // public List<Side> sides = new()
        // {
        //     new Side(){name="Allied"},
        //     new Side(){name="Axis"}
        // };
        public List<Side> sides = new();

        public List<Unit> units = new();

        // public Dictionary<(UnitType, Country), UnitParameter> unitTypeCountryParameterType = new();

        public void ResetAndRegisterAll()
        {
            // Clear previous
            EntityManager.Instance.Reset();

            // Re-register
            foreach (var side in sides)
            {
                EntityManager.Instance.Register(side, null);
            }

            foreach (var unit in units)
            {
                EntityManager.Instance.Register(unit, null);
            }

            // ResetObjRefs
            // EntityManager.Instance.ResetObjRefs();
            // var objRefs = new List<ObjRef>();
            // objRefs.Concat(sides.SelectMany(s => s.IterateObjRefs()));
            var objRefs = sides.SelectMany(s => s.IterateObjRefs());
            objRefs.Concat(units.SelectMany(s => s.IterateObjRefs()));
            objRefs.Concat(units.Select(u => u.GetCell()).ToHashSet().SelectMany(c => c.IterateObjRefs()));
            // objRefs.AddRange(units.SelectMany(s => s.IterateObjRefs()));
            foreach(var objRef in objRefs)
            {
                objRef.SetDirty();
            }

            // events
            EventBus.Publish(Unit.orderOfBattleChanged);
        }

        public int GetMapWidth() => cells.GetLength(0);
        public int GetMapHeight() => cells.GetLength(1);

        public SerializedCells serializedCells
        {
            get
            {
                var records = new List<Cell>();
                for (var x = 0; x < GetMapWidth(); x++)
                {
                    for (var y = 0; y < GetMapHeight(); y++)
                    {
                        records.Add(cells[x, y]);
                    }
                }
                return new()
                {
                    width = GetMapWidth(),
                    height = GetMapHeight(),
                    records=records
                };
            }
            set
            {
                cells = new Cell[value.width, value.height];

                foreach (var cell in value.records)
                {
                    cells[cell.x, cell.y] = cell;
                }
            }
        }

        // public List<EdgeFeature> edgeFeatures = new();
        [XmlIgnore]
        public Dictionary<(int, int, int, int), EdgeFeature> edgeFeatureMap;
        // public Dictionary<(int, int, int, int), EdgeFeature> edgeFeatureMap = new();

        // public List<EdgeFeature> seralizedEdgeFeatureMap
        // {
        //     get
        //     {
        //         var features = edgeFeatureMap.Values.ToList();
        //         features.Sort(EdgeFeature.CompareTo);
        //         return features;
        //     }
        //     set
        //     {
        //         edgeFeatureMap = value.ToDictionary(
        //             x => (x.x1, x.y1, x.x2, x.y2),
        //             x => x
        //         );
        //     }
        // }

        public ListWrapper<EdgeFeature> seralizedEdgeFeatureMap
        {
            get
            {
                var features = edgeFeatureMap.Values.ToList();
                features.Sort(EdgeFeature.CompareTo);
                return new(){list=features};
            }
            set
            {
                edgeFeatureMap = value.list.ToDictionary(
                    x => (x.x1, x.y1, x.x2, x.y2),
                    x => x
                );
            }
        }

        public void ToggleEdgeFeature(Cell cellSrc, Cell cellDst, EdgeFeatureType featureType)
        {
            SetEdgeFeature(cellSrc, cellDst, featureType, !GetEdgeFeature(cellSrc, cellDst, featureType));
        }

        public bool GetEdgeFeature(Cell cellSrc, Cell cellDst, EdgeFeatureType featureType)
        {
            var sdKey = (cellSrc.x, cellSrc.y, cellDst.x, cellDst.y);
            if(edgeFeatureMap.TryGetValue(sdKey, out var edgeFeature))
            {
                return edgeFeature.Get(featureType);
            }
            return false;
        }

        public EdgeFeature GetEdgeFeature(Cell cellSrc, Cell cellDst)
        {
            var sdKey = (cellSrc.x, cellSrc.y, cellDst.x, cellDst.y);
            return edgeFeatureMap.GetValueOrDefault(sdKey);
        }

        public void SetEdgeFeature(Cell cellSrc, Cell cellDst, EdgeFeatureType featureType, bool value)
        {
            var sdKey = (cellSrc.x, cellSrc.y, cellDst.x, cellDst.y);
            if(!edgeFeatureMap.TryGetValue(sdKey, out var edgeFeature))
                edgeFeature = edgeFeatureMap[sdKey] = new EdgeFeature(){ x1 = cellSrc.x, y1 = cellSrc.y, x2 = cellDst.x, y2 = cellDst.y};
            
            if(featureType == EdgeFeatureType.PrimaryRoad)
            {
                edgeFeature.primaryRoad = value;
            }
            else if(featureType == EdgeFeatureType.SecondaryRoad)
            {
                edgeFeature.secondaryRoad = value;
            }
            else if(featureType == EdgeFeatureType.Escarpment)
            {
                edgeFeature.escarpment = value;
            }

            if(edgeFeature.IsDefault())
            {
                edgeFeatureMap.Remove(sdKey);
            }

            // Enforce symmetry
            // if(featureType == EdgeFeatureType.PrimaryRoad || featureType == EdgeFeatureType.SecondaryRoad)
            // {
            //     var dsKey = (cellDst.x, cellDst.y, cellSrc.x, cellSrc.y);
            //     if(!edgeFeatureMap.TryGetValue(dsKey, out var edgeFeatureDst))
            //         edgeFeatureDst = edgeFeatureMap[dsKey] = new EdgeFeature(){ x1 = cellDst.x, y1 = cellDst.y, x2 = cellSrc.x, y2 = cellSrc.y};
                
            //     if(featureType == EdgeFeatureType.PrimaryRoad)
            //     {
            //         edgeFeatureDst.primaryRoad = value;
            //     }
            //     else if(featureType == EdgeFeatureType.SecondaryRoad)
            //     {
            //         edgeFeatureDst.secondaryRoad = value;
            //     }

            //     if(edgeFeatureDst.IsDefault())
            //     {
            //         edgeFeatureMap.Remove(dsKey);
            //     }
            // }

            var dsKey = (cellDst.x, cellDst.y, cellSrc.x, cellSrc.y);
            if(!edgeFeatureMap.TryGetValue(dsKey, out var edgeFeatureDst))
                edgeFeatureDst = edgeFeatureMap[dsKey] = new EdgeFeature(){ x1 = cellDst.x, y1 = cellDst.y, x2 = cellSrc.x, y2 = cellSrc.y};
            
            if(featureType == EdgeFeatureType.PrimaryRoad)
            {
                edgeFeatureDst.primaryRoad = value;
            }
            else if(featureType == EdgeFeatureType.SecondaryRoad)
            {
                edgeFeatureDst.secondaryRoad = value;
            }
            else if(featureType == EdgeFeatureType.Escarpment)
            {
                edgeFeatureDst.escarpment = value;
            }

            if(edgeFeatureDst.IsDefault())
            {
                edgeFeatureMap.Remove(dsKey);
            }


            // edgeFeatureChanged?.Invoke(this, EventArgs.Empty);
            EventBus.Publish(edgeFeatureChanged);
        }

        public class EdgeFeatureChanged : IEvent{}
        public static EdgeFeatureChanged edgeFeatureChanged = new(); 

        // public event EventHandler edgeFeatureChanged; // No one edge event is provided
        public class CellsChanged : IEvent{}
        public static CellsChanged cellsChanged = new();

        // public event EventHandler<Cell> cellChanged;
        // public event EventHandler cellsChanged;
        // public void NotifyCellChanged(Cell cell)
        // {
        //     cellChanged?.Invoke(this, cell);
        // }

        public IEnumerable<Cell> IterateCells()
        {
            for (int x = 0; x < cells.GetLength(0); x++)
            {
                for (int y = 0; y < cells.GetLength(1); y++)
                {
                    yield return cells[x, y];
                }
            }
        }

        public void BuildCells(int width, int height)
        {
            var _cells = new Cell[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    _cells[x, y] = new Cell() { x = x, y = y };
                }
            }

            cells = _cells;
            // cellsChanged?.Invoke(this, EventArgs.Empty);
            EventBus.Publish(cellsChanged);
        }

        public void AdvanceTime(float seconds)
        {
            time = time.AddSeconds(seconds);

            var deployedUnits = units.Where(unit => unit.deployState == DeployState.Deployed).ToList();

            foreach(var unit in deployedUnits)
            {
                unit.AdvanceTimeMovement(seconds);
            }

            foreach(var unit in deployedUnits)
            {
                unit.AdvanceTimeRestore(seconds);
            }

            AdvanceTimeCombat(seconds);
        }

        // void AdvanceTimeRestore()
        // {
            
        // }

        public enum CombatType
        {
            Attack,
            Defend
        }

        public class EngagementRecord
        {
            public CombatUnitBundle target;
            public CombatType type;
            // TODO: Add edge / cell derived modifier
            // public float inflictedAssaultValue = 0;
        }

        public class CombatUnitBundle
        {
            public Unit unit;
            public List<EngagementRecord> engagements = new();
            // Precalculated attributes
            public float hitWeight = 0;
            public float assaultValue = 0;
            public float inflictMenStrength = 0;
        }

        // Aux info for SFX
        public bool anyInfantryFiredInAdvancement;
        public bool anyGunFiredInAdvancement;
        public bool anyVehicleFiredInAdvancement;

        void ResetAnyCategoryFiredInAdvancement()
        {
            anyInfantryFiredInAdvancement = anyGunFiredInAdvancement = anyVehicleFiredInAdvancement = false;
        }
        void CollectionAnyCategoryFiredInAdvancement(Unit unit)
        {
            var firerCategory = unit.parameter.category;
            if(firerCategory == UnitParameter.personelCategory)
            {
                anyInfantryFiredInAdvancement = true;
            }
            else if(firerCategory == UnitParameter.gunCategory)
            {
                anyGunFiredInAdvancement = true;
            }
            else if(firerCategory == UnitParameter.vehicleCategory)
            {
                anyVehicleFiredInAdvancement = true;
            }
        }

        public void AdvanceTimeCombat(float seconds)
        {
            ResetAnyCategoryFiredInAdvancement();

            Dictionary<Unit, CombatUnitBundle> bundleMap = new();

            // Stage 1 - Build Bundles
            foreach(var unit in units.Where(u => u.IsOperational()))
            {
                bundleMap[unit] = new CombatUnitBundle()
                {
                    unit = unit,
                    hitWeight = unit.GetHitWeight(),
                    assaultValue = unit.GetAssaultValue()
                };
            }

            // Stage 2 - Collect Attacking / Defending relationships
            foreach(var bundle in bundleMap.Values)
            {
                if(bundle.unit.waypoints.Count >= 2)
                {
                    var nextCell = bundle.unit.waypoints[1].Get();
                    foreach(var attackToUnit in nextCell.GetUnitsResistTo(bundle.unit.side))
                    {
                        var attackToUnitBundle = bundleMap[attackToUnit];
                        bundle.engagements.Add(new()
                        {
                            target=attackToUnitBundle,
                            type=CombatType.Attack
                        });

                        attackToUnitBundle.engagements.Add(new()
                        {
                            target=bundle,
                            type=CombatType.Defend 
                        });
                    }
                }
            }

            // Stage 3 - Distribute Assault Value
            foreach(var bundle in bundleMap.Values)
            {
                if(bundle.engagements.Count >= 1)
                {
                    CollectionAnyCategoryFiredInAdvancement(bundle.unit);

                    var weightSum = Math.Max(1, bundle.engagements.Sum(e => e.target.hitWeight));
                    foreach(var engagement in bundle.engagements)
                    {
                        var p = engagement.target.hitWeight / weightSum;
                        var commitAssaultValue = bundle.assaultValue * p;
                        var combatValue = commitAssaultValue / engagement.target.unit.parameter.Defense;
                        
                        float lowValue, highValue;
                        if(engagement.type == CombatType.Attack)
                        {
                            lowValue = defenderLowValue;
                            highValue = defenderHighValue;
                        }
                        else
                        {
                            lowValue = attackerLowValue;
                            highValue = attackerHighValue;
                        }

                        var assaultStrengthLoss = combatValue / 1000 * (RandomUtils.NextFloat() * (highValue - lowValue) + lowValue); // One PZC Assault loss
                        var timeCoef = seconds / 7200 * 2; // 2 assault 1 turn => x2, 2 hours turn => /2
                        var strengthLoss = assaultStrengthLoss * timeCoef;

                        engagement.target.inflictMenStrength += strengthLoss;
                    }
                }
            }

            // Stage 4 - Resolve Loss
            foreach(var bundle in bundleMap.Values)
            {
                var strengthLossF = bundle.inflictMenStrength / bundle.unit.parameter.category.strengthCoef;
                var strengthLoss = RandomUtils.RandomRoundToInt(strengthLossF);
                var readinessLoss = strengthLossF / bundle.unit.strength * 10;

                bundle.unit.readiness = Math.Max(0, bundle.unit.readiness - readinessLoss);
                bundle.unit.strength = Math.Max(0, bundle.unit.strength - strengthLoss);

                if(bundle.unit.strength == 0)
                {
                    bundle.unit.MoveTo(null, true);
                }
                else if(bundle.unit.readiness < 0.25)
                {
                    bundle.unit.ForceRetreat();
                }
            }
        }

        static float attackerLowValue = 40;
        static float attackerHighValue = 200;
        static float defenderLowValue = 20;
        static float defenderHighValue = 100;

        // Combat Losses (per 1000 combat value):
        //     Fire Low Value: 10	    Fire High Value: 50
        //     Attacker Low Value: 40	    Attacker High Value: 200
        //     Defender Low Value: 20	    Defender High Value: 100

        public void ResetStrength()
        {
            foreach(var unit in units)
            {
                unit.strength = (int)Math.Round(unit.initialStrengthPercent * unit.parameter.Strength);
                unit.readiness = unit.initialReadiness;
            }
        }

        [XmlIgnore]
        public InfluenceMap side0StrengthMap;
        [XmlIgnore]
        public InfluenceMap side1StrengthMap;
        [XmlIgnore]
        public InfluenceMap controlMap;

        public InfluenceMap CalcualteStrengthMap(InfluenceMap strengthMap, Side side)
        {
            foreach(var g in units.Where(u => u.deployState == DeployState.Deployed && u.side == side).GroupBy(u => u.GetCell()))
            {
                var cell = g.Key;
                var strength = g.Sum(u => u.GetPower());
                strengthMap.AddSource(cell, strength);
            }
            return strengthMap;
        }

        public void CalculateInfluenceMap()
        {
            side0StrengthMap = new(cells.GetLength(0), cells.GetLength(1));
            side1StrengthMap = new(cells.GetLength(0), cells.GetLength(1));
            controlMap = new(cells.GetLength(0), cells.GetLength(1));

            if(sides.Count >= 1)
            {
                CalcualteStrengthMap(side0StrengthMap, sides[0]);
            }
            if(sides.Count >= 2)
            {
                CalcualteStrengthMap(side1StrengthMap, sides[1]);
            }

            controlMap.Plus(side0StrengthMap);
            controlMap.Subtract(side1StrengthMap);
        }

        static GameState _instance;
        public static GameState Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new();
                }
                return _instance;
            }
        }

        public static event EventHandler gameStateReplaced;
        public static void UpdateInstance(GameState gameState)
        {
            _instance = gameState;
            gameStateReplaced?.Invoke(gameState, EventArgs.Empty);

            gameState.ResetAndRegisterAll();
        }
    }
}