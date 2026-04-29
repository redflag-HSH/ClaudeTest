using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button loadGameButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button quitGameButton;

    private void Start()
    {
        if (loadGameButton != null)
            loadGameButton.interactable = SaveManager.Instance != null && SaveManager.Instance.HasSave();
        if (newGameButton != null)
            newGameButton.interactable = true;
        if (quitGameButton != null)
            quitGameButton.interactable = true;
    }

    public void NewGame()
    {
        GameManager.Instance.StartNewGame();
    }

    public void LoadGame()
    {
        if (SaveManager.Instance == null || !SaveManager.Instance.HasSave()) return;
        SaveManager.Instance.LoadGame();
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
