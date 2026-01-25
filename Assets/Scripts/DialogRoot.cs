using System;
using GameModel;
using UnityEngine.UIElements;

public class DialogRoot : SingletonDocument<DialogRoot>
{
    public VisualTreeAsset mapEditDialogDocument;

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