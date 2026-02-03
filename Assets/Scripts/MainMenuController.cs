using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using YYZ;

[RequireComponent(typeof(UIDocument))]
public class MainMenuController : MonoBehaviour
{
    private Button playButton;
    private Button loadGameButton;
    private Button quitButton;

    private void Awake()
    {
        var document = GetComponent<UIDocument>();
        var root = document.rootVisualElement;

        playButton = root.Q<Button>("PlayButton");
        loadGameButton = root.Q<Button>("LoadGameButton");
        quitButton = root.Q<Button>("QuitButton");

        if (playButton == null)
        {
            Debug.LogError("MainMenuController: Could not find Button 'PlayButton' in the assigned UXML.");
        }

        if (quitButton == null)
        {
            Debug.LogError("MainMenuController: Could not find Button 'QuitButton' in the assigned UXML.");
        }

        if (loadGameButton == null)
        {
            Debug.LogError("MainMenuController: Could not find Button 'LoadGameButton' in the assigned UXML.");
        }
    }

    private void OnEnable()
    {
        if (playButton != null)
        {
            playButton.clicked += OnPlayClicked;
        }

        if (quitButton != null)
        {
            quitButton.clicked += OnQuitClicked;
        }

        if (loadGameButton != null)
        {
            loadGameButton.clicked += OnLoadGameClicked;
        }
    }

    private void OnDisable()
    {
        if (playButton != null)
        {
            playButton.clicked -= OnPlayClicked;
        }

        if (quitButton != null)
        {
            quitButton.clicked -= OnQuitClicked;
        }

        if (loadGameButton != null)
        {
            loadGameButton.clicked -= OnLoadGameClicked;
        }
    }

    private static void OnPlayClicked()
    {
        ConfigureStartupForScenario();
        SceneManager.LoadScene(SceneNames.Game, LoadSceneMode.Single);
    }

    private static void OnQuitClicked()
    {
        Application.Quit();
    }

    private void OnLoadGameClicked()
    {
        var ioManager = EnsureIOManager();
        if (ioManager == null)
        {
            Debug.LogError("MainMenuController: Could not initialize IOManager for loading.");
            return;
        }

        ioManager.LoadTextFile(xml =>
        {
            if (string.IsNullOrEmpty(xml))
            {
                Debug.LogWarning("MainMenuController: Load Game cancelled or failed.");
                return;
            }

            try
            {
                var fullState = XmlUtils.FromXML<FullState>(xml);
                if (fullState == null || fullState.gameState == null)
                {
                    Debug.LogError("MainMenuController: Loaded XML does not contain a valid FullState.");
                    return;
                }

                GameManager.startupConfig.mode = GameManager.StartupConfig.Mode.FullState;
                GameManager.startupConfig.fullState = fullState;

                SceneManager.LoadScene(SceneNames.Game, LoadSceneMode.Single);
            }
            catch (Exception ex)
            {
                Debug.LogError($"MainMenuController: Failed to parse/load XML save. {ex}");
            }
        }, "xml");
    }

    private static void ConfigureStartupForScenario()
    {
        GameManager.startupConfig.mode = GameManager.StartupConfig.Mode.ScenPath;
        GameManager.startupConfig.fullState = null;
    }

    private static IOManager EnsureIOManager()
    {
        var ioManager = IOManager.Instance;
        if (ioManager != null)
            return ioManager;

        var ioManagerObject = new GameObject("IOManager");
        return ioManagerObject.AddComponent<IOManager>();
    }
}
