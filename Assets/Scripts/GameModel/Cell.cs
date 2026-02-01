using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using YYZ;

namespace GameModel
{
    public partial class Cell : IHasObjRef
    {
        [XmlAttribute]
        public int x;

        [XmlAttribute]
        public int y;

        TerrainType _terrain;

        [XmlAttribute]
        public TerrainType terrain
        {
            get => _terrain;
            set
            {
                if(_terrain != value)
                {
                    _terrain = value;
                    // GameState.Instance.NotifyCellChanged(this);
                    EventBus.Publish(new CellChanged{cell = this});
                }
            }
        }

        public class CellChanged : IEvent
        {
            public Cell cell;
        }

        public override string ToString()
        {
            return $"Cell({x}, {y}, {terrain})";
        }

        static List<(int, int)> yEvenOffset = new()
        {
            (+1, +0), (0, +1), (-1, +1), (-1, 0), (-1, -1), (0, -1)
        };

        static List<(int, int)> yOddOffset = new()
        {
            (+1, 0), (+1, +1), (0, +1), (-1, 0), (0, -1), (+1, -1)
        };

        IEnumerable<Cell> GetNeighborsCached()
        {
            var isEven = y % 2 == 0;
            var offsets = isEven ? yEvenOffset : yOddOffset;
            var width = GameState.Instance.GetMapWidth();
            var height = GameState.Instance.GetMapHeight();
            foreach (var (dx, dy) in offsets)
            {
                var nx = x + dx;
                var ny = y + dy;
                if (nx >= 0 && nx < width &&
                    ny >= 0 && ny < height)
                {
                    yield return GameState.Instance.cells[nx, ny];
                }
            }
        }

        List<Cell> neighborCellsCached;
        bool neighborCellsCachedDirty = true;
        public List<Cell> GetNeighbors()
        {
            if(neighborCellsCachedDirty)
            {
                neighborCellsCachedDirty = false;
                neighborCellsCached = GetNeighborsCached().ToList();
            }
            return neighborCellsCached;
        }

        public List<ObjRef> UnitRefs = new();

        public bool ShouldSerializeUnitRefs() => UnitRefs.Count > 0;

        public IEnumerable<ObjRef> IterateObjRefs()
        {
            return UnitRefs;
        }

        public IEnumerable<Unit> IterateUnits() => UnitRefs.Select(x => x.Get() as Unit).Where(x => x != null);

        public bool IsArmyPassable() => terrain != TerrainType.Water;

        public float GetMovementCoef(Cell dst)
        {
            var edgeFeature = GameState.Instance.GetEdgeFeature(this, dst);
            var edgeFeatureCoef = 1f;
            if(edgeFeature != null)
            {
                if(edgeFeature.primaryRoad)
                {
                    // edgeFeatureCoef = 0.5f;
                    edgeFeatureCoef = 0.8f; // AI PathFinding workaround, but also consider it as Track Movement
                }
                else if(edgeFeature.secondaryRoad)
                {
                    // edgeFeatureCoef = 0.75f;
                    edgeFeatureCoef = 0.9f;
                }
                else if(edgeFeature.escarpment)
                {
                    edgeFeatureCoef = 100f;
                }
            }
            return edgeFeatureCoef * globalCoef; // Assume that all land terrain is identical.
        }

        public static float globalCoef = 2; 

        public XY ToXY() => new XY{x = x, y = y};

        public bool HasResistTo(Side side) => GetUnitsResistTo(side).FirstOrDefault() != null;
        public IEnumerable<Unit> GetUnitsResistTo(Side side) => IterateUnits().Where(u => u.side != side && u.IsOperational());
    }
}