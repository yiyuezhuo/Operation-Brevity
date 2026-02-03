using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class MainMenuController : MonoBehaviour
{
    private Button playButton;
    private Button quitButton;

    private void Awake()
    {
        var document = GetComponent<UIDocument>();
        var root = document.rootVisualElement;

        playButton = root.Q<Button>("PlayButton");
        quitButton = root.Q<Button>("QuitButton");

        if (playButton == null)
        {
            Debug.LogError("MainMenuController: Could not find Button 'PlayButton' in the assigned UXML.");
        }

        if (quitButton == null)
        {
            Debug.LogError("MainMenuController: Could not find Button 'QuitButton' in the assigned UXML.");
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
    }

    private static void OnPlayClicked()
    {
        SceneManager.LoadScene(SceneNames.Game, LoadSceneMode.Single);
    }

    private static void OnQuitClicked()
    {
        Application.Quit();
    }
}
