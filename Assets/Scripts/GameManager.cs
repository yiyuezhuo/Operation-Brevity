using System;
using GameModel;
using TMPro;
using UnityEngine;
using Unity.Properties;
using UnityEngine.UIElements;
using YYZ;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;


public enum CellLabelMode
{
    None,
    XY,
    Terrain
}

public enum MapEditMode
{
    PaintTerrain,
    PaintEdgeFeature,
}



public class GameManager : SingletonMonoBehaviour<GameManager>
{
    public Grid grid;

    public GameObject cellLabelPrefab;
    public GameObject edgeFeaturePrefab;
    public GameObject counterPrefab;

    UIDocument[] allUIDocuments;

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

    bool _mapEditEnabled;
    public bool mapEditEnabled
    {
        get => _mapEditEnabled;
        set
        {
            if(_mapEditEnabled == value)
                return;

            _mapEditEnabled = value;

            RefreshEdgeFeatures();
        }
    }

    MapEditMode _mapEditMode;

    [CreateProperty]
    public MapEditMode mapEditMode
    {
        get => _mapEditMode;
        set
        {
            if(_mapEditMode == value)
                return;

            _mapEditMode = value;

            RefreshEdgeFeatures();
        }
    }

    public TerrainType mapEditTerrain;

    EdgeFeatureType _mapEditEdgeFeatureType;

    [CreateProperty]
    public EdgeFeatureType mapEditEdgeFeatureType
    {
        get => _mapEditEdgeFeatureType;
        set
        {
            if(_mapEditEdgeFeatureType == value)
                return;

            _mapEditEdgeFeatureType = value;

            RefreshEdgeFeatures();
        }
    }

    Transform cellLabelsTransform;
    Transform edgeFeaturesTransform;
    Transform countersTransform;

    // Label[,] cellLabels;
    TMP_Text[,] cellLabels;

    Action<Cell> oneshotCellClickedCallback;

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
        allUIDocuments = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);

        cellLabelsTransform = Utils.CreateDynamicTransform(transform, "CellLabels");
        edgeFeaturesTransform = Utils.CreateDynamicTransform(transform, "EdgeFeatures");
        countersTransform = Utils.CreateDynamicTransform(transform, "Counters");

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

    public bool IsHotKeyEnabled()
    {
        if(EventSystem.current.IsPointerOverGameObject())
            return false;


        if(allUIDocuments != null)
        {
            foreach (var doc in allUIDocuments)
            {
                var root = doc.rootVisualElement;
                if (root == null) continue;

                var focused = root.focusController?.focusedElement;
                if (focused == null) continue;

                return false;
            }
        }

        return true;
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
        // GameState.gameStateReplaced += OnGameStateReplaced;

        RegisterGameState();

        RebuildCellLabels();

        TempFix();

        SetAllDirty();

        Debug.Log("Fully Initialized");
    }

    void TempFix()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(IsHotKeyEnabled())
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

        if(edgeFeatureDirty)
        {
            edgeFeatureDirty = false;
            RefreshEdgeFeatures();
        }

        if(mapUnitsDirty)
        {
            mapUnitsDirty = false;
            RefreshMapUnits();
        }

        if(stacksDirty)
        {
            stacksDirty = false;
            RefreshStacks();
        }
    }

    void RefreshMapUnits()
    {
        var unitsOnMap = GameState.Instance.units.Where(u => u.deployState == DeployState.Deployed).ToList();

        Utils.SyncTransformViewerLength(countersTransform, unitsOnMap.Count, counterPrefab);
        var controllers = countersTransform.GetComponentsInChildren<CounterController>();
        for(int i=0; i<unitsOnMap.Count; i++)
        {
            var unit = unitsOnMap[i];
            var controller = controllers[i];
            // binding
            controller.unit = unit;
            unit.view = controller;
        }

        // RefreshStacks();
        stacksDirty = true;
    }

    void RefreshStacks()
    {
        var unitsOnMap = GameState.Instance.units.Where(u => u.deployState == DeployState.Deployed).ToList();
        var groupings = unitsOnMap.GroupBy(u => u.GetCell()).ToList();
        foreach(var grouping in groupings)
        {
            var cell = grouping.Key;
            var units = grouping.ToList();
            units.Sort((u1, u2) => u1.stackPriority.CompareTo(u2.stackPriority));

            LayoutStackTransform(
                units.Select(u => u.view.transform).ToList(),
                GetCellCenterWorld(cell),
                0.05f
            );
        }
    }

    public static void LayoutStackTransform(List<Transform> transforms, Vector3 basePos, float stackSpace)
    {
        var count = transforms.Count;
        if (count == 1)
        {
            transforms[0].position = basePos;
            return;
        }
        var step = stackSpace / (count - 1);
        for (int i = 0; i < count; i++)
        {
            var delta = -stackSpace / 2 + i * step;
            // transforms[i].position = basePos + new Vector3(delta, delta, 0);
            // var z = -(i * step); // negative z is closer to camera
            transforms[i].position = basePos + new Vector3(delta, delta, 0);
        }
    }


    void RefreshEdgeFeatures()
    {
        // if(!mapEditEnabled)
        // {
            
        // }
        List<EdgeFeature> edges;

        if(mapEditEnabled && mapEditMode == MapEditMode.PaintEdgeFeature)
        {
            edges = GameState.Instance.edgeFeatureMap.Values.Where(e => e.Get(mapEditEdgeFeatureType)).ToList();
        }
        else
        {
            edges = new();
        }

        Utils.SyncTransformViewerLength(edgeFeaturesTransform, edges.Count, edgeFeaturePrefab);
        var lineRenderers = edgeFeaturesTransform.GetComponentsInChildren<LineRenderer>().ToList();
        for(int i = 0; i < edges.Count; i++)
        {
            var edge = edges[i];
            var lineRenderer = lineRenderers[i];

            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, GetCellCenterWorld(edge.x1, edge.y1));
            lineRenderer.SetPosition(1, GetCellCenterWorld(edge.x2, edge.y2));
        }
    }

    Cell cellStart;

    void HandleCellClicked(Cell cell)
    {
        Debug.Log(cell);

        if(mapEditEnabled)
        {
            if(mapEditMode == MapEditMode.PaintTerrain)
            {
                cell.terrain = mapEditTerrain;
            }
            else if(mapEditMode == MapEditMode.PaintEdgeFeature)
            {
                if(cellStart != null)
                {
                    Debug.Log($"Toggle: {cellStart}, {cell}, {mapEditEdgeFeatureType}");
                    GameState.Instance.ToggleEdgeFeature(cellStart, cell, mapEditEdgeFeatureType);

                    cellStart = null;
                }
                else
                {
                    cellStart = cell;
                }
            }
        }
        else if(oneshotCellClickedCallback != null)
        {
            oneshotCellClickedCallback(cell);
            oneshotCellClickedCallback = null;
        }
    }

    public void ScheduleOneshotCellClickedCallback(Action<Cell> callback)
    {
        oneshotCellClickedCallback = callback;
    }

    public override void OnDestroy()
    {
        // GameState.gameStateReplaced -= OnGameStateReplaced;

        UnregisterGameState();
    }

    bool edgeFeatureDirty = false;
    bool mapUnitsDirty = false;
    bool stacksDirty = false;

    public void SetAllDirty() // Invoke a full refresh
    {
        edgeFeatureDirty = true;
        mapUnitsDirty = true;
        stacksDirty = true;
    }

    // public void OnEdgeFeatureChanged(object sender, EventArgs args)
    // {
    //     edgeFeatureDirty = true;
    // }

    void OnEdgeFeatureChanged(GameState.EdgeFeatureChanged evt)
    {
        edgeFeatureDirty = true;
    }

    void OnMapUnitsChanged(Unit.MapUnitsChanged evt)
    {
        mapUnitsDirty = true;
    }

    void OnStacksChanged(Unit.StacksChanged evt)
    {
        stacksDirty = true;
    }


    void RegisterGameState()
    {
        // GameState.Instance.cellChanged += OnGameStateCellChanged;
        // GameState.Instance.cellsChanged += OnGameStateCellsChanged;
        // GameState.Instance.edgeFeatureChanged += OnEdgeFeatureChanged;

        EventBus.Subscribe<Cell.CellChanged>(OnGameStateCellChanged);
        EventBus.Subscribe<GameState.CellsChanged>(OnGameStateCellsChanged);
        EventBus.Subscribe<GameState.EdgeFeatureChanged>(OnEdgeFeatureChanged);
        EventBus.Subscribe<Unit.MapUnitsChanged>(OnMapUnitsChanged);
        EventBus.Subscribe<Unit.StacksChanged>(OnStacksChanged);
    }

    void UnregisterGameState()
    {
        // GameState.Instance.cellChanged -= OnGameStateCellChanged;
        // GameState.Instance.cellsChanged -= OnGameStateCellsChanged;
        // GameState.Instance.edgeFeatureChanged -= OnEdgeFeatureChanged;

        EventBus.Unsubscribe<Cell.CellChanged>(OnGameStateCellChanged);
        EventBus.Unsubscribe<GameState.CellsChanged>(OnGameStateCellsChanged);
        EventBus.Unsubscribe<GameState.EdgeFeatureChanged>(OnEdgeFeatureChanged);
        EventBus.Unsubscribe<Unit.MapUnitsChanged>(OnMapUnitsChanged);
        EventBus.Unsubscribe<Unit.StacksChanged>(OnStacksChanged);
    }

    // void OnGameStateCellChanged(object sender, Cell cell)
    // {
    //     RefreshCellLabel(cell);
    // }

    void OnGameStateCellChanged(Cell.CellChanged evt)
    {
        RefreshCellLabel(evt.cell);
    }


    // void OnGameStateCellsChanged(object sender, EventArgs args)
    // {
    //     RebuildCellLabels();
    // }


    void OnGameStateCellsChanged(GameState.CellsChanged evt)
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

    public Vector3 GetCellCenterWorld(Cell cell)
    {
        return GetCellCenterWorld(cell.x, cell.y);
    }

    [CreateProperty]
    public string bottomDescription
    {
        get
        {
            var mapEditEnabledStr = mapEditEnabled ? "Map Edit Enabled" : "";
            var oneshotCellClickedCallbackStr = oneshotCellClickedCallback != null ? "Oneshot Callback Assigned" : "";
            return $"{mapEditEnabledStr} {oneshotCellClickedCallbackStr}";
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
