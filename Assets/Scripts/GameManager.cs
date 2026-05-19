using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Score Object References")]
    [SerializeField] Text scoreText;

    [Header("Score Logic References")]
    [SerializeField] public int scoreToPass;
    [SerializeField] public int startingScore;
    [SerializeField] public int currentScore;

    [Header("Timer References")]
    [SerializeField] float timeRemaining;
    [SerializeField] bool timerIsRunning = false;
    [SerializeField] Text timerText;

    [Header("Typewriter Settings")]
    [SerializeField] float typingSpeed = 0.05f;

    private Coroutine typingCoroutine;

    private void Start()
    {
        timerIsRunning = true;
        currentScore = startingScore;
        UpdateScoreTextInstant();
    }

    private void Update()
    {
        if (timerIsRunning)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                DisplayTime(timeRemaining);
            }
            else
            {
                timeRemaining = 0;
                timerIsRunning = false;
                Debug.Log("Time has run out!");
            }
        }
    }

    public void AddScore(int score)
    {
        currentScore += score;
        StartTypingScore();
    }

    public void RemoveScore(int score)
    {
        currentScore -= score;
        StartTypingScore();
    }

    public void AddTime(int time)
    {
        timeRemaining += time;
        DisplayTime(timeRemaining);
    }

    public void RemoveTime(int time)
    {
        timeRemaining -= time;
        DisplayTime(timeRemaining);
    }

    void StartTypingScore()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(EraseAndTypeScore());
    }

    IEnumerator EraseAndTypeScore()
    {
        string newText = "Current Score: " + currentScore.ToString() + " / " + scoreToPass.ToString();

        while (scoreText.text.Length > 0)
        {
            scoreText.text = scoreText.text.Substring(0, scoreText.text.Length - 1);
            yield return new WaitForSeconds(typingSpeed);
        }

        yield return new WaitForSeconds(typingSpeed * 2);

        foreach (char c in newText)
        {
            scoreText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
    }

    void UpdateScoreTextInstant()
    {
        scoreText.text = "Current Score: " + currentScore.ToString() + " / " + scoreToPass.ToString();
    }

    void DisplayTime(float timeToDisplay)
    {
        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);

        timerText.text = "Time Left: " + string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}