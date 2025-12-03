using UnityEngine;
using UnityEngine.UI; 
using System.Collections; 

public class GameManager : MonoBehaviour
{
  
    public Text scoreText;
    public Text timerText;
    public float timeLimit = 120.0f; 
    public int scoreIncreaseAmount = 10;
    public GameObject gameOverUI;
    public GameObject gameWonUI;

   
    private int currentScore = 0;
    private float currentTime;
    private bool isGameOver = false;

   
    void Start()
    {
        currentTime = timeLimit;
        UpdateScoreUI();
        StartCoroutine(CountdownTimer());
    }

    private void Update()
    {
        if (currentScore >= 5)
        {
            GameWon();
        }
    }

    public void IncreaseScore()
    {
        if (isGameOver) return; 

        currentScore += scoreIncreaseAmount;
        UpdateScoreUI();
        
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + currentScore.ToString();
        }
    }

    

    IEnumerator CountdownTimer()
    {
        while (currentTime > 0)
        {
            
            currentTime -= Time.deltaTime;
            UpdateTimerUI();

           
            yield return null;
        }

       
        currentTime = 0;
        UpdateTimerUI();
        GameOver();
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
          
            int minutes = Mathf.FloorToInt(currentTime / 60);
            int seconds = Mathf.FloorToInt(currentTime % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

  public void GameOver()
    {

        isGameOver = true;
        gameOverUI.SetActive(true);
        Debug.Log("Game Over! Final Score: " + currentScore);
        
    }

   public void GameWon()
    {
        
        gameWonUI.SetActive(true);
    }
}