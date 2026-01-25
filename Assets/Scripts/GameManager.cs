using System;
using GameModel;
using TMPro;
using UnityEngine;
using Unity.Properties;
using UnityEngine.UIElements;
using Unity.VisualScripting;
using UnityEditor;
using YYZ;

public enum CellLabelMode
{
    None,
    XY,
    Terrain
}

public enum MapEditMode
{
    PaintTerrain,
    PaintPrimaryRoad,
    PaintSecondaryRoad,
    PaintBlockEdge
}


public class GameManager : SingletonMonoBehaviour<GameManager>
{
    public Grid grid;

    public GameObject cellLabelPrefab;

    CellLabelMode _cellLabelMode;

    [CreateProperty]
    public CellLabelMode cellLabelMode
    {
        get => _cellLabelMode;
        set
        {
            if (_cellLabelMode == value)
                return;

            _cellLabelMode = value;

            RefreshCellLabels();
        }
    }

    public bool mapEditEnabled; // will override some behaviour
    public MapEditMode mapEditMode;
    public TerrainType mapEditTerrain;

    Transform cellLabelsTransform;

    // Label[,] cellLabels;
    TMP_Text[,] cellLabels;

    public class StartupConfig
    {
        public enum Mode
        {
            ScenPath,
            FullState
        }

        public Mode mode = Mode.ScenPath;
        public string scenSubPath = "Scenarios/scenario.xml";
        public FullState fullState = null;
    }

    public static StartupConfig startupConfig = new();


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cellLabelsTransform = Utils.CreateDynamicTransform(transform, "CellLabels");

        if(startupConfig.mode == StartupConfig.Mode.ScenPath)
        {
            StartCoroutine(
                StreamingTextAssetManager.Instance.FetchText(Application.streamingAssetsPath + "/" + startupConfig.scenSubPath, xml =>
                {
                    var fullState = XmlUtils.FromXML<FullState>(xml);
                    LoadFullState(fullState);
                })
            );
        }
        else if(startupConfig.mode == StartupConfig.Mode.FullState)
        {
            LoadFullState(startupConfig.fullState);
        }
    }

    public ViewState CaptureViewState()
    {
        var cam = PlaneCameraController.Instance.cam;

        return new()
        {
            xPosition = cam.transform.position.x,
            yPosition = cam.transform.position.y,
            orthographicSize = cam.orthographicSize,
        };
    }

    public FullState CaptureFullState()
    {
        var fullState = new FullState
        {
            gameState = GameState.Instance,
            viewState = CaptureViewState(),
        };
        return fullState;
    }

    public void LoadFullState(FullState fullState)
    {
        GameState.UpdateInstance(fullState.gameState);
        var viewState = fullState.viewState;
        
        if(viewState != null)
        {
            var cam = PlaneCameraController.Instance.cam;
            cam.transform.position = new Vector3(
                viewState.xPosition,
                viewState.yPosition,
                cam.transform.position.z
            );
            cam.orthographicSize = viewState.orthographicSize;
        }

        // Setup
        GameState.gameStateReplaced += OnGameStateReplaced;

        RegisterGameState();

        RebuildCellLabels();

        Debug.Log("Fully Initialized");
    }

    // Update is called once per frame
    void Update()
    {
        var leftClicking = Input.GetMouseButtonDown(0);

        if(leftClicking)
        {
            Vector2 mousePosition = PlaneCameraController.Instance.cam.ScreenToWorldPoint(Input.mousePosition);

            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);
            if(hit.collider != null)
            {
                var cellPos = WorldToCell(mousePosition);
                Debug.Log($"hit.collider={hit.collider}, cellPos={cellPos}");

                var x = cellPos.x;
                var y = cellPos.y;
                var gameState = GameState.Instance;
                if(x >= 0 && x < gameState.cells.GetLength(0) && y >= 0 && y < gameState.cells.GetLength(1))
                {
                    var cell = gameState.cells[x, y];
                    HandleCellClicked(cell);
                }
            }
        }
    }

    void HandleCellClicked(Cell cell)
    {
        Debug.Log(cell);

        if(mapEditEnabled)
        {
            cell.terrain = mapEditTerrain;
        }
    }

    // void OnEnable()
    // {
    //     GameState.gameStateReplaced += OnGameStateReplaced;

    //     RegisterGameState();
    // }

    // void OnDisable()
    // {
    //     GameState.gameStateReplaced -= OnGameStateReplaced;

    //     UnregisterGameState();
    // }

    public override void OnDestroy()
    {
        GameState.gameStateReplaced -= OnGameStateReplaced;

        UnregisterGameState();
    }

    void OnGameStateReplaced(object sender, EventArgs args)
    {
        UnregisterGameState();
        RegisterGameState();
    }

    void RegisterGameState()
    {
        GameState.Instance.cellChanged += OnGameStateCellChanged;
        GameState.Instance.cellsChanged += OnGameStateCellsChanged;
    }

    void UnregisterGameState()
    {
        GameState.Instance.cellChanged -= OnGameStateCellChanged;
        GameState.Instance.cellsChanged -= OnGameStateCellsChanged;
    }

    void OnGameStateCellChanged(object sender, Cell cell)
    {
        RefreshCellLabel(cell);
    }

    void OnGameStateCellsChanged(object sender, EventArgs args)
    {
        RebuildCellLabels();
    }

    public void RefreshCellLabel(Cell cell)
    {
        var x = cell.x;
        var y = cell.y;

        var label = cellLabels[x, y];

        label.text = cellLabelMode switch
        {
            CellLabelMode.XY => $"({x}, {y})",
            CellLabelMode.Terrain => cell.terrain.ToString(),
            _ => ""
        };

        if(cellLabelMode == CellLabelMode.Terrain)
        {
            label.color = cell.terrain switch
            {
                TerrainType.Desert => Color.yellow,
                TerrainType.Water => Color.blue,
                _ => Color.black
            };
        }
        else
        {
            label.color = Color.black;
        }
    }

    public void RefreshCellLabels()
    {
        var cells = GameState.Instance.cells;

        for (int x = 0; x < cellLabels.GetLength(0); x++)
        {
            for (int y = 0; y < cellLabels.GetLength(1); y++)
            {
                var cell = cells[x, y];
                RefreshCellLabel(cell);
            }
        }
    }

    public void RebuildCellLabels()
    {
        // Utils.DestroyChildrensFor(cellLabelsTransform);
        var cells = GameState.Instance.cells;
        if(cells == null)
            return;

        Utils.SyncTransformViewerLength(cellLabelsTransform, cells.Length, cellLabelPrefab);
        
        var texts = cellLabelsTransform.GetComponentsInChildren<TMP_Text>();
        cellLabels = new TMP_Text[cells.GetLength(0), cells.GetLength(1)];

        // var docs = cellLabelsTransform.GetComponentsInChildren<UIDocument>();
        // cellLabels = new Label[cells.GetLength(0), cells.GetLength(1)];
        int i = 0;
        for (int x = 0; x < cells.GetLength(0); x++)
        {
            for (int y = 0; y < cells.GetLength(1); y++)
            {
                var text = texts[i];
                // var doc = docs[i];
                
                i++;

                // doc.name = $"Cell_{x}_{y}";
                // cellLabels[x, y] = doc.rootVisualElement.Q<Label>();
                text.name = $"Cell_{x}_{y}";
                cellLabels[x, y] = text;

                var worldPos = GetCellCenterWorld(x, y);
                // doc.transform.position = worldPos;
                text.transform.position = worldPos;

                // TODO: Position the label

            }
        }

        RefreshCellLabels();
    }

    public Vector3Int WorldToCell(Vector3 worldPosition)
    {
        return grid.WorldToCell(worldPosition);
    }
    
    public Vector3 GetCellCenterWorld(Vector3Int cellPosition)
    {
        return grid.GetCellCenterWorld(cellPosition);
    }

    public Vector3 GetCellCenterWorld(int x, int y)
    {
        return grid.GetCellCenterWorld(new Vector3Int(x, y, 0));
    }

    [CreateProperty]
    public string bottomDescription
    {
        get
        {
            return $"Map Edit={mapEditEnabled} ";
        }
    }

    [CreateProperty]
    public int currentWidth => GameState.Instance.cells.GetLength(0);

    [CreateProperty]
    public int currentHeight => GameState.Instance.cells.GetLength(1);

    [HideInInspector]
    public int targetWidth = 37;

    [HideInInspector]
    public int targetHeight = 40;
}
