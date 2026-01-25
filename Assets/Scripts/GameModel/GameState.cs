using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;


namespace GameModel
{
    public enum TerrainType
    {
        Desert,
        Water,

    }

    public class EdgeState
    {
        public bool hasPrimaryRoad;
        public bool hasSecondaryRoad;
        public bool moveBlocked; // Escarpment
        public bool isFort; //  Fort is unidirectional
    }


    public class Cell
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
                    GameState.Instance.NotifyCellChanged(this);
                }
            }
        }

        public override string ToString()
        {
            return $"Cell({x}, {y}, {terrain})";
        }
    }

    public class SerializedCells
    {
        public int width;
        public int height;
        public List<Cell> records;
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
                cellsChanged?.Invoke(this, EventArgs.Empty);
            }
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

        public event EventHandler<Cell> cellChanged;
        public event EventHandler cellsChanged;
        public void NotifyCellChanged(Cell cell)
        {
            cellChanged?.Invoke(this, cell);
        }

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
            cellsChanged?.Invoke(this, EventArgs.Empty);
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
        }
    }
}