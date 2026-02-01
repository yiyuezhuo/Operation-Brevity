using System;
using GameModel;
using UnityEngine.UIElements;
using UnityEngine;
using YYZ;
using System.Collections.Generic;
using System.Linq;

public class OrderOfBattleNodePlaceholder // : IOrderOfBattleNode // UITK Helper
{
    public string oobDesc;
    public string oobDescLink;
}

public class AISelectionDialog
{
    public enum AISelectionOption
    {
        Axis,
        Allied,
        Both,
        Neither
    }

    public AISelectionOption selectionOption;

    public void Execute()
    {
        var enableAlliedAI = selectionOption == AISelectionOption.Allied || selectionOption == AISelectionOption.Both;
        var enableAxisAI = selectionOption == AISelectionOption.Axis || selectionOption == AISelectionOption.Both;
        if(!enableAlliedAI)
        {
            ClearAIFor(GameState.Instance.sides[0]); // TODO: relax Hardcoded or use another method to set AI options
        }
        if(!enableAxisAI)
        {
            ClearAIFor(GameState.Instance.sides[1]);
        }
    }

    public void ClearAIFor(Side side)
    {
        foreach(var unit in (side as IOrderOfBattleNode).WalkChildren<Unit>())
        {
            unit.SetWaypoints(new());
            unit.missionTargetXY = null;
        }
    }
}

public class DialogRoot : SingletonDocument<DialogRoot>
{
    public VisualTreeAsset mapEditDialogDocument;
    public VisualTreeAsset orderOfBattleDialogDocument;
    public VisualTreeAsset unitDialogDocument;
    public VisualTreeAsset victoryStatusDialogDocument;
    public VisualTreeAsset aiSelectionDialogDocument;

    public void PopupAISelectionDialog()
    {
        var aiSelectionDialog = new AISelectionDialog();

        var tempDialog = new TempDialog
        {
            root = root,
            template = aiSelectionDialogDocument,
            templateDataSource = aiSelectionDialog,
        };

        tempDialog.onConfirmed += (e, a) =>
        {
            aiSelectionDialog.Execute();
        };

        tempDialog.Popup();
    }

    public void PopupVictoryStatusDialog(VictoryStatus victoryStatus)
    {
        var tempDialog = new TempDialog
        {
            root = root,
            template = victoryStatusDialogDocument,
            templateDataSource = victoryStatus,
            // positionMode = TempDialog.PositionMode.Left
        };

        tempDialog.Popup();
    }
    public void PopupVictoryStatusDialog() => PopupVictoryStatusDialog(VictoryStatus.Capture(GameState.Instance));

    public void PopupUnitDialog(Unit unit)
    {
        var tempDialog = new TempDialog
        {
            root = root,
            template = unitDialogDocument,
            templateDataSource = unit,
            // positionMode = TempDialog.PositionMode.Left
        };

        tempDialog.onCreated += (s, e) =>
        {
            var deployButton = e.Q<Button>("DeployButton");
            deployButton.clicked += () =>
            {
                GameManager.Instance.ScheduleOneshotCellClickedCallback(cell =>
                {
                    Debug.Log($"Deploying {unit.name} to {cell.x} {cell.y}");
                    unit.MoveTo(cell);
                });
            };
        };

        tempDialog.Popup();
    }

    public void PopupOrderOfBattleDialog()
    {
        var tempDialog = new TempDialog
        {
            root = root,
            template = orderOfBattleDialogDocument,
            templateDataSource = null,
            positionMode = TempDialog.PositionMode.Left
        };

        // Action refresh = null;
        // Action<Unit.OrderOfBattleChanged> refreshCallback = evt => refresh();
        var dirty = false;
        Action<Unit.OrderOfBattleChanged> oobChangedCallback = evt => dirty = true;

        tempDialog.onCreated += (sender, el) =>
        {
            var treeView = el.Q<TreeView>();

            treeView.makeItem = () =>
            {
                var _el = treeView.itemTemplate.CloneTree();

                return _el;
            };
            treeView.bindItem = (e, i) =>
            {
                var item = treeView.GetItemDataForIndex<IOrderOfBattleNode>(i);

                e.dataSource = item;
                // var label = e.Q<Label>();
                // label.dataSource = item;
            };

            Unit attachingUnit = null;

            treeView.selectionChanged += selectedItems =>
            {
                var selectedUnit = selectedItems.FirstOrDefault() as Unit;
                if(selectedUnit != null)
                {
                    if(attachingUnit == null)
                    {
                        GameManager.Instance.selectingUnit = selectedUnit;
                    }
                    else
                    {
                        attachingUnit.AttachTo(selectedUnit);
                        attachingUnit = null;
                    }
                }
            };

            var tree = new OrderOfBattleTree();

            var editButton = el.Q<Button>("EditButton");
            editButton.clicked += () =>
            {
                Debug.Log($"Edit: {treeView.selectedItem}");
                
                var unit = treeView.selectedItem as Unit;
                if(unit != null)
                {
                    PopupUnitDialog(unit);
                }
            };

            Unit focusedUnit = null;

            var newSubordinateButton = el.Q<Button>("NewSubordinateButton");
            newSubordinateButton.clicked += () =>
            {
                Debug.Log($"New Subordinate: {treeView.selectedItem}");

                if(treeView.selectedItem is IOrderOfBattleNode newParent)
                {
                    var unit = new Unit();
                    EntityManager.Instance.Register(unit, null);
                    GameState.Instance.units.Add(unit);
                    unit.AttachTo(newParent);

                    focusedUnit = unit; // 
                }
            };

            var deleteButton = el.Q<Button>("DeleteButton");
            deleteButton.clicked += () =>
            {
                if(treeView.selectedItem is Unit unit)
                {
                    unit.AttachTo(null);
                    GameState.Instance.units.Remove(unit);

                    // EntityManager.Instance.Unregister(unit);
                    GameState.Instance.ResetAndRegisterAll();

                    // refresh(); // Otherwise state
                }
            };

            

            var attachButton = el.Q<Button>("AttachButton");
            attachButton.clicked += () =>
            {
                attachingUnit = treeView.selectedItem as Unit;
            };

            Action refresh = () =>
            {
                var treeViewerBuilder = new UITKTreeViewBuilder<IOrderOfBattleNode, IOrderOfBattleNode>()
                {
                    tree=tree
                };
                List<IOrderOfBattleNode> topNodes = new();
                topNodes.AddRange(GameState.Instance.sides);
                topNodes.AddRange(GameState.Instance.units.Where(u => u.parent == null));

                var rootItems = treeViewerBuilder.CreateTreeViewRootItems(topNodes);
                treeView.SetRootItems(rootItems);
                treeView.Rebuild();
                // treeView.ExpandAll();

                // focusedUnit
                if(focusedUnit != null)
                {
                    var viewIdx = treeViewerBuilder.indexToTreeViewIdx[focusedUnit];
                    // treeView.ScrollToItemById(viewIdx);
                    // treeView.ScrollToItem(viewIdx);

                    treeView.SetSelectionById(viewIdx);
                    treeView.ScrollToItemById(viewIdx);

                    focusedUnit = null;
                }
                else
                {
                    treeView.ExpandAll();
                }
                
            };

            refresh();

            tempDialog.updateCallback = () =>
            {
                if(dirty)
                {
                    dirty = false;
                    refresh();
                }
            };

            EventBus.Subscribe(oobChangedCallback);
        };

        tempDialog.onClosed += (e, el) =>
        {
            EventBus.Unsubscribe(oobChangedCallback);
        };

        tempDialog.Popup();
    }

    public void PopupMapEditDialog(Action callback)
    {
        var tempDialog = new TempDialog
        {
            root = root,
            template = mapEditDialogDocument,
            templateDataSource = GameManager.Instance,
            positionMode = TempDialog.PositionMode.Left
        };

        tempDialog.onConfirmed += (s, e) => callback();

        tempDialog.onCreated += (s, el) =>
        {
            el.Q<Button>("RebuildButton").clicked += () =>
            {
                var gameState = GameState.Instance;
                gameState.BuildCells(GameManager.Instance.targetWidth, GameManager.Instance.targetHeight);
            };
        };

        tempDialog.Popup();
    }

    public List<TempDialog> activingTempDialogs = new();

    void Update()
    {
        foreach(var dialog in activingTempDialogs)
        {
            if(dialog.updateCallback != null)
            {
                dialog.updateCallback();
            }
        }

    }
}