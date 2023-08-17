using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameUIHandler : MonoBehaviour
{
    public static GameUIHandler Instance;

    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Text levelScoreTxt;
    [SerializeField] private Text highScoreTxt;

    [SerializeField] private Text scoreTxt;
    [SerializeField] private Text moveCounterTxt;

    private int score;
    private int moveCounter;

    public int Score
    {
        get
        {
            return score;
        }

        set
        {
            score = value;
            scoreTxt.text = score.ToString();
        }
    }

    public int MoveCounter
    {
        get
        {
            return moveCounter;
        }

        set
        {
            moveCounter = value;
            moveCounterTxt.text = moveCounter.ToString();
            if (moveCounter <= 0)
            {
                moveCounter = 0;
                StartCoroutine(WaitForCombos());
            }
        }
    }

    private void Awake()
    {
        Instance = GetComponent<GameUIHandler>();

        score = 0;
        scoreTxt.text = score.ToString();

        moveCounter = 30;
        moveCounterTxt.text = moveCounter.ToString();
    }

    // Show the game over panel
    public void GameOver()
    {
        GameManager.Instance.GameOver = true;

        gameOverPanel.SetActive(true);

        if (score > PlayerPrefs.GetInt("HighScore"))
        {
            PlayerPrefs.SetInt("HighScore", score);
            highScoreTxt.text = "New Best: " + PlayerPrefs.GetInt("HighScore").ToString();
        }
        else
        {
            highScoreTxt.text = "Best: " + PlayerPrefs.GetInt("HighScore").ToString();
        }

        levelScoreTxt.text = score.ToString();
    }

    private IEnumerator WaitForCombos()
    {
        yield return new WaitUntil(() => !BoardManager.Instance.IsShifting && !Tile.LockTiles);
        yield return new WaitForSeconds(.25f);
        GameOver();
    }
}
