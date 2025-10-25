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

    string playerInput = "";
    bool isChecking = false;

    void Start()
    {
        if (!gameData) gameData = FindObjectOfType<GameData>();

        if (inputDisplay) inputDisplay.text = "";
        if (outlineImage) outlineImage.color = normalOutlineColor;
        if (correctUI) correctUI.SetActive(false);
        if (wrongUI) wrongUI.SetActive(false);

        // debug โค้ดปัจจุบัน (คำนวณสด)
        Debug.Log("[KeypadPuzzle] Current correct code: " + BuildCorrectCode());
    }

    void Update()
    {
        if (isChecking) return;

        // รองรับพิมพ์จากคีย์บอร์ด (ถ้าใช้ปุ่ม UI ให้เรียก PressDigit/PressBackspace/PressEnter)
        foreach (char c in Input.inputString)
        {
            if (char.IsDigit(c)) PressDigit(c - '0');
            else if (c == '\b') PressBackspace();
            else if (c == '\n' || c == '\r') PressEnter();
        }
    }

    // ===== Public hooks for UI buttons =====
    public void PressDigit(int d)
    {
        if (isChecking) return;
        d = Mathf.Clamp(d, 0, 9);
        if (playerInput.Length >= 3) return;

        playerInput += d.ToString();
        UpdateDisplay();

        if (playerInput.Length == 3) CheckCode();
    }

    public void PressBackspace()
    {
        if (isChecking) return;
        if (playerInput.Length > 0)
        {
            playerInput = playerInput.Substring(0, playerInput.Length - 1);
            UpdateDisplay();
        }
    }

    public void PressEnter()
    {
        if (isChecking) return;
        if (playerInput.Length == 3) CheckCode();
    }

    // ===== core =====
    string BuildCorrectCode()
    {
        if (!gameData) return "";
        // กันค่าหลุดช่วง/ติดลบ และคง leading zero
        int d1 = Mathf.Clamp(gameData.Digit1, 0, 9);
        int d2 = Mathf.Clamp(gameData.Digit2, 0, 9);
        int d3 = Mathf.Clamp(gameData.Digit3, 0, 9);
        return $"{d1}{d2}{d3}";
    }

    void UpdateDisplay()
    {
        if (inputDisplay) inputDisplay.text = playerInput;
    }

    void CheckCode()
    {
        isChecking = true;

        string correct = BuildCorrectCode();            // <<< คำนวณสดทุกครั้ง
        bool ok = string.Equals(playerInput, correct, System.StringComparison.Ordinal);

        // debug ช่วยไล่ปัญหา
        Debug.Log($"[KeypadPuzzle] Input={playerInput}  Correct={correct}  Match={ok}");

        if (ok) StartCoroutine(HandleCorrect());
        else StartCoroutine(HandleWrong());
    }

    IEnumerator HandleCorrect()
    {
        if (audioSource && correctSound) audioSource.PlayOneShot(correctSound);

        if (correctUI) yield return StartCoroutine(PopUI(correctUI));

        yield return new WaitForSeconds(0.3f);
        if (puzzleUI) puzzleUI.SetActive(false);

        isChecking = false;
    }

    IEnumerator HandleWrong()
    {
        if (audioSource && wrongSound) audioSource.PlayOneShot(wrongSound);

        if (outlineImage)
        {
            outlineImage.color = wrongOutlineColor;
            yield return new WaitForSeconds(redFlashDuration);
            outlineImage.color = normalOutlineColor;
        }

        if (wrongUI) yield return StartCoroutine(PopUI(wrongUI));

        playerInput = "";
        UpdateDisplay();
        isChecking = false;
    }

    IEnumerator PopUI(GameObject ui)
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
