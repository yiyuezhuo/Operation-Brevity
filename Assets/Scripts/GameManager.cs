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
using YYZ.Unity;
using YYZ.PathFinding;

public enum CellLabelMode
{
    None,
    XY,
    Terrain,
    Side0StrengthMap,
    Side1StrengthMap,
    ControlMap
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

    public PathLineController pathLineController;

    public AudioSource infantryFiringAudioSource;
    public AudioSource gunFiringAudioSource;
    public AudioSource vehicleFiringAudioSource;

    LayerMask unitLayerMask;
    LayerMask mapLayerMask;
    // LayerMask counterLayerMask;

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

        // if(startupConfig.mode == StartupConfig.Mode.ScenPath)
        // {
        //     StartCoroutine(
        //         StreamingAssetManager.Instance.FetchText(Application.streamingAssetsPath + "/" + startupConfig.scenSubPath, xml =>
        //         {
        //             var fullState = XmlUtils.FromXML<FullState>(xml);
        //             LoadFullState(fullState);
        //         })
        //     );
        // }
        // else if(startupConfig.mode == StartupConfig.Mode.FullState)
        // {
        //     LoadFullState(startupConfig.fullState);
        // }

        unitLayerMask = LayerMask.GetMask("Unit");
        mapLayerMask = LayerMask.GetMask("Map");
        // counterLayerMask = LayerMask.GetMask("Counter");

        SetupAsync();
    }

    public async void SetupAsync()
    {
        var unitParameterCsvText = await StreamingAssetManager.Instance.FetchTextAsync(Application.streamingAssetsPath + "/Data/UnitParameter.csv");
        var unitParameterRecords = UnitParameter.ParseUnits(unitParameterCsvText);
        Unit.unitParameterMap = unitParameterRecords.ToDictionary(p => (p.Country, p.UnitType), p => p);

        if(startupConfig.mode == StartupConfig.Mode.ScenPath)
        {
            var scenarioXml = await StreamingAssetManager.Instance.FetchTextAsync(Application.streamingAssetsPath + "/" + startupConfig.scenSubPath);
            var fullState = XmlUtils.FromXML<FullState>(scenarioXml);
            LoadFullState(fullState);
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

        if(!GameState.Instance.firstLoaded)
        {
            GameState.Instance.firstLoaded = true;
            GameState.Instance.ResetStrength();
        }

        SetAllDirty();

        Debug.Log("Fully Initialized");
    }

    void TempFix()
    {
        // Enforce escarpment symmetric

        // var gameState = GameState.Instance;
        // foreach(var kv in gameState.edgeFeatureMap.ToList())
        // {
        //     var (x1, y1, x2, y2) = kv.Key;
        //     var edgeFeature = kv.Value;

        //     if(edgeFeature.escarpment)
        //     {
        //         var cellSrc = gameState.cells[x1, y1];
        //         var cellDst = gameState.cells[x2, y2];
        //         gameState.SetEdgeFeature(cellSrc, cellDst, EdgeFeatureType.Escarpment, true);
        //     }
        // }
    }

    // List<Unit> stackSelecting = new();

    // Update is called once per frame
    void Update()
    {
        HandleInput();
        RefreshDirty();
        RunSimulation();
        UpdateView();
        MaintainSounds();
    }

    void MaintainSounds()
    {
        if(playing)
        {
            var gameState = GameState.Instance;
            if(gameState.anyInfantryFiredInAdvancement && !infantryFiringAudioSource.isPlaying)
            {
                infantryFiringAudioSource.Play();
            }
            if(gameState.anyGunFiredInAdvancement && !gunFiringAudioSource.isPlaying)
            {
                gunFiringAudioSource.Play();
            }
            if(gameState.anyVehicleFiredInAdvancement && !vehicleFiringAudioSource.isPlaying)
            {
                vehicleFiringAudioSource.Play();
            }
        }
    }

    static Vector3[] emptyVector3Arr = new Vector3[0];

    void UpdateView()
    {
        if(selectingUnit != null)
        {
            var positions = selectingUnit.waypoints.Select(xy => GetCellCenterWorld(xy.x, xy.y)).ToArray();
            var p = selectingUnit.movementProgressionKm / ModelUtils.hexDistanceKm;
            pathLineController.Sync(positions, p);
        }
        else
        {
            pathLineController.Sync(emptyVector3Arr, 0);
        }
    }

    float unresolvedSeconds = 0;
    float pulseLengthSeconds = 60; // 60s

    void RunSimulation()
    {
        if(playing)
        {
            unresolvedSeconds += Time.deltaTime * GetTimeRatio();
            while(unresolvedSeconds > pulseLengthSeconds)
            {
                unresolvedSeconds -= pulseLengthSeconds;
                GameState.Instance.AdvanceTime(pulseLengthSeconds);
            }
        }
    }

    // Vector2 rightClickingPosition; // if up/down click is very close, a path direct command is issued (CMO-style)
    Vector3 rightClickingPosition;
    static float rightClickDistThreshold = 1;

    void HandleInput()
    {
        if(IsHotKeyEnabled())
        {
            var leftClicking = Input.GetMouseButtonDown(0);

            if(leftClicking)
            {
                Vector2 mousePosition = PlaneCameraController.Instance.cam.ScreenToWorldPoint(Input.mousePosition);

                RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero, Mathf.Infinity, unitLayerMask);
                // RaycastHit2D hit2 = Physics2D.Raycast(mousePosition, Vector2.zero, 0, mapLayerMask);
                // var hits = Physics2D.RaycastAll(mousePosition, Vector2.zero);

                if(hit.collider != null)
                {
                    if(hit.collider.CompareTag("Unit"))
                    {
                        var counterController = hit.collider.GetComponent<CounterController>();
                        var unit = counterController?.unit;

                        // Debug.Log($"Unit clicked: {counterController}, {unit}");

                        var cell = unit.GetCell();
                        var stack = cell.UnitRefs.Select(r => r.Get() as Unit).ToList();
                        stack = cell.UnitRefs.Select(r => r.Get() as Unit).ToList();
                        stack.Sort(Unit.StackPriorityCompareTo);
                        var oldTopOne = stack[^1];

                        HandleUnitClicked(oldTopOne, stack);
                        HandleCellClicked(cell); // Or use a trimmed version?
                    }
                }
                else // grid cell raycast
                {
                    hit = Physics2D.Raycast(mousePosition, Vector2.zero);

                    var cellPos = WorldToVector3Int(mousePosition);
                    // Debug.Log($"hit.collider={hit.collider}, cellPos={cellPos}");

                    // var x = cellPos.x;
                    // var y = cellPos.y;
                    // var gameState = GameState.Instance;
                    // if(x >= 0 && x < gameState.cells.GetLength(0) && y >= 0 && y < gameState.cells.GetLength(1))
                    // {
                    //     var cell = gameState.cells[x, y];
                    //     HandleCellClicked(cell);
                    // }

                    var cell = Vector3IntToCell(cellPos);
                    HandleCellClicked(cell);
                }
            }

            // Right clicking path plan
            var isRightMouseButtonDown = Input.GetMouseButtonDown(1);
            if(isRightMouseButtonDown)
            {
                // Vector2 mousePosition = PlaneCameraController.Instance.cam.ScreenToWorldPoint(Input.mousePosition);
                // var hit = Physics2D.Raycast(mousePosition, Vector2.zero);
                // if(hit.collider != null) // map
                // {
                //     // rightClickingPosition = hit.point;
                //     rightClickingPosition = Input.mousePosition;
                // }

                rightClickingPosition = Input.mousePosition;
            }

            var isRightMouseButtonUp = Input.GetMouseButtonUp(1);
            if(isRightMouseButtonUp)
            {
                var dist = Vector3.Distance(rightClickingPosition, Input.mousePosition);
                if(dist <= rightClickDistThreshold)
                {
                    Vector2 mousePosition = PlaneCameraController.Instance.cam.ScreenToWorldPoint(Input.mousePosition);
                    
                    var hit = Physics2D.Raycast(mousePosition, Vector2.zero);
                    if(hit.collider != null) // map
                    {
                        var cellPos = WorldToVector3Int(mousePosition);
                        var cell = Vector3IntToCell(cellPos);
                        HandleCellRightClickedWithoutDragging(cell);
                    }
                }
            }

            if(Input.GetKeyDown(KeyCode.Escape))
            {
                selectingUnit = null;
                selectingCell = null;
                oneshotCellClickedCallback = null;

                SetAllDirty();

                Overlay.Instance.RefreshStackContainer(new());
            }

            if(Input.GetKeyDown(KeyCode.Space))
            {
                playing = !playing;
            }
        }
    }

    public void HandleCellRightClickedWithoutDragging(Cell cell)
    {
        if(selectingUnit != null)
        {
            Debug.Log($"Plan path: {selectingUnit} to {cell}");

            // var graph = new DynamicCellGraphArmy();
            var graph = DynamicCellGraphArmy.Instance;
            var srcCell = selectingUnit.GetCell();
            if(srcCell != null)
            {
                var cost = PathFinding<Cell>.AStar(graph, srcCell, cell, out var path);
                // var path = PathFinding<Cell>.AStar3(graph, srcCell, cell);
                // if(path.Path.Count >= 2)
                // {
                //     selectingUnit.SetWaypoints(path.Path);
                // }
                selectingUnit.SetWaypoints(path);
            }
        }
    }

    void RefreshDirty()
    {
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
            // units.Sort((u1, u2) => u1.stackPriority.CompareTo(u2.stackPriority));
            units.Sort(Unit.StackPriorityCompareTo);

            // LayoutStackTransform(
            //     units.Select(u => u.view.transform).ToList(),
            //     GetCellCenterWorld(cell),
            //     // 0.05f
            //     0.25f
            // );

            LayoutStackTransform2(
                units.Select(u => u.view).ToList(),
                GetCellCenterWorld(cell),
                // 0.05f
                0.25f
            );
        }
    }

    // public static void LayoutStackTransform(List<Transform> transforms, Vector3 basePos, float stackSpace)
    // {
    //     var count = transforms.Count;
    //     if (count == 1)
    //     {
    //         transforms[0].position = basePos;
    //         return;
    //     }
    //     var step = stackSpace / (count - 1);
    //     for (int i = 0; i < count; i++)
    //     {
    //         var delta = -stackSpace / 2 + i * step;
    //         transforms[i].position = basePos + new Vector3(delta, delta, 0);
    //     }
    // }

    public static void LayoutStackTransform2(List<CounterController> controllers, Vector3 basePos, float stackSpace)
    {
        var count = controllers.Count;
        if (count == 1)
        {
            controllers[0].transform.position = basePos;
            return;
        }
        var step = stackSpace / (count - 1);
        for (int i = 0; i < count; i++)
        {
            var delta = -stackSpace / 2 + i * step;
            controllers[i].transform.position = basePos + new Vector3(delta, delta, 0);
            controllers[i].sortingGroup.sortingOrder = i;
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

    public Unit selectingUnit;

    [CreateProperty]
    public bool selectingUnitValid => selectingUnit != null;

    void HandleUnitClicked(Unit unit, List<Unit> stack)
    {
        if(unit != null)
        {
            Debug.Log($"HandleUnitClicked: {unit}");

            if(selectingUnit != unit)
            {
                selectingUnit = unit; // re-select
            }
            else if(stack.Count > 1) // toggle stack
            {
                stack.RemoveAt(stack.Count - 1);
                stack.Insert(0, unit);
                for(int i = 0; i < stack.Count; i++)
                {
                    stack[i].stackPriority = ((float)i) / stack.Count;
                }

                EventBus.Publish(Unit.stacksChanged);

                selectingUnit = stack[^1];
            }

            // stackSelecting = stack;
            // TODO: Refresh Stack Selecting
            Overlay.Instance.RefreshStackContainer(stack);

            // TODO: Select Cell here
        }
    }

    public Cell selectingCell;

    [CreateProperty]
    public bool selectingCellValid => selectingCell != null;

    void HandleCellClicked(Cell cell)
    {
        Debug.Log(cell);

        if(cell == null)
        {
            return;
        }

        // Test Neighbor and MoveCost
        var neiStr = string.Join(",", cell.GetNeighbors().Select(c => $"[{c}, {cell.GetMovementCoef(c)}]"));
        Debug.Log($"{cell} => nei={neiStr}");

        if(mapEditEnabled)
        {
            if(mapEditMode == MapEditMode.PaintTerrain)
            {
                cell.terrain = mapEditTerrain;
            }
            else if(mapEditMode == MapEditMode.PaintEdgeFeature)
            {
                if(selectingCell != null)
                {
                    Debug.Log($"Toggle: {selectingCell}, {cell}, {mapEditEdgeFeatureType}");
                    GameState.Instance.ToggleEdgeFeature(selectingCell, cell, mapEditEdgeFeatureType);

                    selectingCell = null;
                }
                else
                {
                    selectingCell = cell;
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
    // bool stackSelectingDirty = false;

    public void SetAllDirty() // Invoke a full refresh
    {
        edgeFeatureDirty = true;
        mapUnitsDirty = true;
        stacksDirty = true;
        // stackSelectingDirty = true;
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

    void OnOrderOfBattleChanged(Unit.OrderOfBattleChanged evt)
    {
        foreach(var unit in GameState.Instance.units)
        {
            unit.SetAllDirty();
        }
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
        EventBus.Subscribe<Unit.OrderOfBattleChanged>(OnOrderOfBattleChanged);
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
        EventBus.Unsubscribe<Unit.OrderOfBattleChanged>(OnOrderOfBattleChanged);
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
            CellLabelMode.Side0StrengthMap => GameState.Instance.side0StrengthMap?.matrix[cell.x, cell.y].ToString("#"),
            CellLabelMode.Side1StrengthMap => GameState.Instance.side1StrengthMap?.matrix[cell.x, cell.y].ToString("#"),
            CellLabelMode.ControlMap => GameState.Instance.controlMap?.matrix[cell.x, cell.y].ToString("#"),
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
        else if(cellLabelMode == CellLabelMode.ControlMap)
        {
            label.color = GameState.Instance.controlMap?.matrix[cell.x, cell.y] switch
            {
                >= 0 => Color.blue,
                <0 => Color.red,
                float.NaN => Color.black
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

    public Cell Vector3IntToCell(Vector3Int cellPos)
    {
        var x = cellPos.x;
        var y = cellPos.y;
        var gameState = GameState.Instance;
        if(x >= 0 && x < gameState.cells.GetLength(0) && y >= 0 && y < gameState.cells.GetLength(1))
        {
            var cell = gameState.cells[x, y];
            return cell;
        }
        return null;
    }

    public Vector3Int WorldToVector3Int(Vector3 worldPosition)
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

    [CreateProperty]
    public GameState currentGameState => GameState.Instance;

    [HideInInspector]
    public bool playing;
    public enum TimeRatioLevel
    {
        x60, // 1s real time=> 1min game time
        x300, // 1s real time => 5min game time.
        x1500
    }

    [HideInInspector]
    public TimeRatioLevel timeRatioLevel = TimeRatioLevel.x300;

    public float GetTimeRatio()
    {
        return timeRatioLevel switch
        {
            TimeRatioLevel.x60 => 60f,
            TimeRatioLevel.x300 => 300f,
            TimeRatioLevel.x1500 => 1500f,
            _ => 0
        };
    }
}
