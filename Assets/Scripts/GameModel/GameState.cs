using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Hashing;
using System.Linq;
using System.Xml.Serialization;

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

    }

    public class GameState
    {
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
        public Dictionary<(int, int, int, int), EdgeFeature> edgeFeatureMap = new();

        public List<EdgeFeature> seralizedEdgeFeatureMap
        {
            get
            {
                var features = edgeFeatureMap.Values.ToList();
                features.Sort(EdgeFeature.CompareTo);
                return features;
            }
            set
            {
                edgeFeatureMap = value.ToDictionary(
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
            if(featureType == EdgeFeatureType.PrimaryRoad || featureType == EdgeFeatureType.SecondaryRoad)
            {
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

                if(edgeFeatureDst.IsDefault())
                {
                    edgeFeatureMap.Remove(dsKey);
                }
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