using System;
using GameModel;
using UnityEngine.UIElements;

public class DialogRoot : SingletonDocument<DialogRoot>
{
    public VisualTreeAsset mapEditDialogDocument;
    public VisualTreeAsset orderOfBattleDialogDocument;

    public void PopupOrderOfBattleDialog()
    {
        var tempDialog = new TempDialog
        {
            root = root,
            template = orderOfBattleDialogDocument,
            templateDataSource = null,
            // positionMode = TempDialog.PositionMode.Left
        };

        tempDialog.onCreated += (sender, el) =>
        {
            var treeView = el.Q<TreeView>();

            // var tree = new FullGroupTreeNameLink();
            // var treeViewerBuilder = new UITKTreeViewBuilder<IStrategicGroupMemberReferenceable, IStrategicGroupMemberReferenceable>()
            // {
            //     tree=tree
            // };
            // var rootItems = treeViewerBuilder.CreateTreeViewRootItems(viewableGroups);
            // oobTreeView.SetRootItems(rootItems);

            // tree.BindMakeItemBindItem(oobTreeView);

            treeView.Rebuild();
            // oobTreeView.ExpandAll();
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
}