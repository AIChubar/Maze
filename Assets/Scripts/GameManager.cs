using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private float timeLimit = 180f;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private GameObject endPanel;
    [SerializeField] private TextMeshProUGUI endMessageText;

    private float _timeRemaining;
    public bool IsPlaying { get; private set; }

    private void Awake()
    {
        Instance = this;
        _timeRemaining = timeLimit;
        IsPlaying = true;
    }

    private void Update()
    {
        if (!IsPlaying) return;

        _timeRemaining -= Time.deltaTime;
        if (_timeRemaining <= 0f)
        {
            _timeRemaining = 0f;
            Lose();
        }

        int minutes = Mathf.FloorToInt(_timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(_timeRemaining % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    public void Win()
    {
        if (!IsPlaying) return;
        IsPlaying = false;
        ShowEndPanel("You escaped!");
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Lose()
    {
        IsPlaying = false;
        ShowEndPanel("Time's up!");
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ShowEndPanel(string message)
    {
        endMessageText.text = message;
        endPanel.SetActive(true);
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}