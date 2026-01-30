using GameModel;
using UnityEngine;
using UnityEngine.UIElements;
using YYZ;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class Overlay : SingletonDocument<Overlay> // So don't specify binding for the TopTabs or other "subordinate" elements
{
    VisualElement stackContainer;

    public VisualTreeAsset unitIconAsset;

    protected override void Awake()
    {
        base.Awake();

        root.dataSource = GameManager.Instance;

        Utils.BindItemsSourceRecursive(root);

        var mapEditButton = root.Q<Button>("MapEditButton");
        mapEditButton.clicked += () =>
        {
            GameManager.Instance.mapEditEnabled = true;
            DialogRoot.Instance.PopupMapEditDialog(() =>
            {
                GameManager.Instance.mapEditEnabled = false;
            });
        };

        var saveButton = root.Q<Button>("SaveButton");
        saveButton.clicked += () =>
        {
            // var fullState = GameManager.Instance.CaptureFullState();
            // var xml = XmlUtils.ToXML(fullState);
            // IOManager.Instance.SaveTextFile(xml, "scenario", "xml");
            DoSave(false);
        };

        var saveEditButton = root.Q<Button>("SaveEditButton");
        saveEditButton.clicked += () =>
        {
            DoSave(true);
        };

        var loadButton = root.Q<Button>("LoadButton");
        loadButton.clicked += () =>
        {
            IOManager.Instance.LoadTextFile(xml =>
            {
                var fullState = XmlUtils.FromXML<FullState>(xml);
                // GameState.UpdateInstance(fullState.gameState);
                GameManager.Instance.LoadFullState(fullState);

            }, "xml");
        };

        var orderOfBattleButton = root.Q<Button>("OrderOfBattleButton");
        orderOfBattleButton.clicked += () =>
        {
            DialogRoot.Instance.PopupOrderOfBattleDialog();
        };

        var exitButton = root.Q<Button>("ExitButton");
        exitButton.clicked += () =>
        {
            Application.Quit();
        };

        var restartButton = root.Q<Button>("RestartButton");
        restartButton.clicked += () =>
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        };

        stackContainer = root.Q<VisualElement>("StackContainer");

        var detailButton = root.Q<Button>("DetailButton");
        detailButton.clicked += () =>
        {
            if(Utils.TryResolveCurrentValueForBinding<Unit>(detailButton, out var unit))
            {
                DialogRoot.Instance.PopupUnitDialog(unit);
            }
        };
    }

    void DoSave(bool editSave)
    {
        var fullState = GameManager.Instance.CaptureFullState();
        if(editSave)
        {
            fullState.gameState.firstLoaded = false;
        }
        var xml = XmlUtils.ToXML(fullState);
        IOManager.Instance.SaveTextFile(xml, "scenario", "xml");
    }

    public void RefreshStackContainer(List<Unit> stack)
    {
        stackContainer.Clear();

        foreach(var unit in stack)
        {
            var el = unitIconAsset.CloneTree();
            el.dataSource = unit;
            stackContainer.Add(el);

            el.RegisterCallback<ClickEvent>(evt =>
            {
                Debug.Log($"Stack unit {unit} clicked");
            });
        }
    }

}