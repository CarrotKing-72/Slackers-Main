using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Unity.VisualScripting;

public class SimonSaysMinigame : MonoBehaviour
{
    public Button[] buttons;        // 9 UI buttons
    public Image[] buttonImages;    // Their Image components
    public Color normalColor = Color.white;
    public Color flashColor = Color.yellow;
    public Color wrongColor = Color.red;
    public GameManager manager;

    private List<int> sequence = new List<int>();
    private int playerIndex = 0;
    private bool inputEnabled = false;
    [SerializeField] PlayerController playerController;
    void OnEnable()
    {
        StartCoroutine(StartGame());
    }

    public void Update()
    {
        ExitPanel();
    }

    IEnumerator StartGame()
    {
        sequence.Clear();
        playerIndex = 0;
        yield return new WaitForSeconds(1f);

        AddStep();
        yield return ShowSequence();
        inputEnabled = true;
    }

    void AddStep()
    {
        int randomIndex = Random.Range(0, 9); // 0–8
        sequence.Add(randomIndex);
    }

    IEnumerator ShowSequence()
    {
        inputEnabled = false;

        foreach (int id in sequence)
        {
            yield return FlashButton(id);
        }

        inputEnabled = true;
        playerIndex = 0; // reset for player input
    }

    IEnumerator FlashButton(int id)
    {
        buttonImages[id].color = flashColor;
        yield return new WaitForSeconds(0.4f);
        buttonImages[id].color = normalColor;
        yield return new WaitForSeconds(0.2f);
    }

    public void PlayerPress(int id)
    {
        if (!inputEnabled) return;

        StartCoroutine(PressEffect(id));

        // Correct
        if (id == sequence[playerIndex])
        {
            playerIndex++;

            // Completed the whole round?
            if (playerIndex >= sequence.Count)
            {
                // Finished all 5 turns?
                if (sequence.Count >= 5)
                {
                    StartCoroutine(GameComplete());
                    return;
                }

                // Continue to next round
                StartCoroutine(NextRound());
            }
        }
        else
        {
            // Wrong input → restart
            StartCoroutine(WrongAnswer());
        }
    }

IEnumerator PressEffect(int id)
    {
        buttonImages[id].color = flashColor;
        yield return new WaitForSeconds(0.15f);
        buttonImages[id].color = normalColor;
    }

    IEnumerator NextRound()
    {
        inputEnabled = false;
        yield return new WaitForSeconds(0.5f);

        AddStep();
        yield return ShowSequence();
    }

    IEnumerator WrongAnswer()
    {
        inputEnabled = false;

        // Flash all red
        foreach (var img in buttonImages)
            img.color = wrongColor;

        yield return new WaitForSeconds(0.4f);

        // Reset
        foreach (var img in buttonImages)
            img.color = normalColor;

        StartCoroutine(StartGame());
    }

    IEnumerator GameComplete()
    {
        inputEnabled = false;

        // Flash all green or yellow
        foreach (var img in buttonImages)
            img.color = Color.green;

        Vector3 playerPos = FindFirstObjectByType<PlayerController>().transform.position;
        OfficeNPCController.AlertNPCs(playerPos);

        yield return new WaitForSeconds(1f);

        ClosePanel();
        manager.AddScore(75);
        manager.AddTime(20);

        // Reset colors
        foreach (var img in buttonImages)
            img.color = normalColor;
    }

    public void ExitPanel()
    {
        if (Input.GetKeyDown(playerController.exitMenu))
        {
            ClosePanel();
        }
    }

    public void ClosePanel()
    {
        if (gameObject != null)
        {
            gameObject.SetActive(false);
        }
    }
}