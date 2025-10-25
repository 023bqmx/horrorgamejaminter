using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class KeypadPuzzle : MonoBehaviour
{
    [Header("UI References")]
    public GameObject puzzleUI;
    public TextMeshProUGUI inputDisplay;
    public Image outlineImage;

    [Header("Result Pop-Ups")]
    public GameObject correctUI;
    public GameObject wrongUI;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip correctSound;
    public AudioClip wrongSound;

    [Header("Game Data Reference")]
    public GameData gameData;

    [Header("Settings")]
    public float popScaleDuration = 0.25f;
    public float redFlashDuration = 0.4f;
    public Color normalOutlineColor = Color.white;
    public Color wrongOutlineColor = Color.red;

    private string playerInput = "";
    private string correctCode;
    private bool isChecking = false;

    void Start()
    {
        if (!gameData)
            gameData = FindObjectOfType<GameData>();

        correctCode = $"{gameData.Digit1}{gameData.Digit2}{gameData.Digit3}";
        Debug.Log("[KeypadPuzzle] Correct code: " + correctCode);

        if (inputDisplay) inputDisplay.text = "";
        if (outlineImage) outlineImage.color = normalOutlineColor;

        if (correctUI) correctUI.SetActive(false);
        if (wrongUI) wrongUI.SetActive(false);
    }

    void Update()
    {
        if (isChecking) return;

        foreach (char c in Input.inputString)
        {
            if (char.IsDigit(c))
                AddDigit(c);
            else if (c == '\b')
                RemoveLastDigit();
            else if (c == '\n' || c == '\r')
                CheckCode();
        }
    }

    private void AddDigit(char c)
    {
        if (playerInput.Length >= 3) return;

        playerInput += c;
        UpdateDisplay();

        if (playerInput.Length == 3)
            CheckCode();
    }

    private void RemoveLastDigit()
    {
        if (playerInput.Length > 0)
        {
            playerInput = playerInput.Substring(0, playerInput.Length - 1);
            UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        if (inputDisplay)
            inputDisplay.text = playerInput;
    }

    private void CheckCode()
    {
        isChecking = true;

        if (playerInput == correctCode)
        {
            Debug.Log("✅ Correct code entered!");
            StartCoroutine(HandleCorrect());
        }
        else
        {
            Debug.Log("❌ Wrong code!");
            Debug.Log(gameData.Digit1);
            Debug.Log(gameData.Digit2);
            Debug.Log(gameData.Digit3);
            StartCoroutine(HandleWrong());
        }
    }

    private IEnumerator HandleCorrect()
    {
        if (audioSource && correctSound)
            audioSource.PlayOneShot(correctSound);

        if (correctUI)
        {
            yield return StartCoroutine(PopUI(correctUI));
        }

        yield return new WaitForSeconds(0.3f);

        if (puzzleUI)
            puzzleUI.SetActive(false);

        isChecking = false;
    }

    private IEnumerator HandleWrong()
    {
        if (audioSource && wrongSound)
            audioSource.PlayOneShot(wrongSound);

        if (outlineImage)
        {
            outlineImage.color = wrongOutlineColor;
            yield return new WaitForSeconds(redFlashDuration);
            outlineImage.color = normalOutlineColor;
        }

        if (wrongUI)
        {
            yield return StartCoroutine(PopUI(wrongUI));
        }

        playerInput = "";
        UpdateDisplay();

        isChecking = false;
    }

    private IEnumerator PopUI(GameObject ui)
    {
        ui.SetActive(true);
        Transform t = ui.transform;
        t.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < popScaleDuration)
        {
            elapsed += Time.deltaTime;
            float k = elapsed / popScaleDuration;
            t.localScale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one, k);
            yield return null;
        }

        t.localScale = Vector3.one;

        yield return new WaitForSeconds(1f);
        ui.SetActive(false);
    }
}
