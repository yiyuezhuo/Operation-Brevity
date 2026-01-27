using GameModel;
using UnityEngine;
using UnityEngine.UIElements;
using YYZ;
using UnityEngine.SceneManagement;

public class Overlay : SingletonDocument<Overlay> // So don't specify binding for the TopTabs or other "subordinate" elements
{
    protected override void Awake()
    {
        base.Awake();

        root.dataSource = GameManager.Instance;

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
            var fullState = GameManager.Instance.CaptureFullState();
            var xml = XmlUtils.ToXML(fullState);
            IOManager.Instance.SaveTextFile(xml, "scenario", "xml");
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
    }
}