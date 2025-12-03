using UnityEngine;

public class ScoreTrigger : MonoBehaviour
{
    private GameManager gameManager;

    void Start()
    {
        // Find the GameManager script in the scene
        gameManager = FindObjectOfType<GameManager>();

        if (gameManager == null)
        {
            Debug.LogError("GameManager not found in the scene! Scoring will not work.");
        }
    }

  
    private void OnTriggerEnter(Collider other)
    {
       
        if (other.CompareTag("Player"))
        {
            
            gameManager.IncreaseScore();

         
            Destroy(gameObject);
        }
    }
}