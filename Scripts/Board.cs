using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // UI için gerekli

public class Board : MonoBehaviour
{
    public int width = 8;
    public int height = 8;
    public GameObject gemPrefab;
    public GemType[] gemTypes;

    [SerializeField] private float spacing = 0.9f;
    public static bool isSwapping = false;

    // UI elemanlarý
    public Text timeText;
    public Text scoreText;
    public Button startButton;
    public Text gameOverText;

    // Oyun süresi ve puan
    private float gameTime = 60f;
    private int score = 0;
    private bool isGameActive = false;
    private Coroutine gameTimerCoroutine;

    private void Start()
    {
        if (startButton != null)
            startButton.onClick.AddListener(StartGame);

        UpdateTimeUI();
        UpdateScoreUI();
    }

    public void StartGame()
    {
        // Önce eski gemleri temizle
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        SetupBoard();
        StartCoroutine(CheckInitialMatches());

        score = 0;
        gameTime = 60f;
        isGameActive = true;

        UpdateScoreUI();
        UpdateTimeUI();

        if (gameOverText != null)
            gameOverText.gameObject.SetActive(false); // oyun baþlarken gizle

        // Coroutine zaten çalýþýyorsa durdur
        if (gameTimerCoroutine != null)
            StopCoroutine(gameTimerCoroutine);

        gameTimerCoroutine = StartCoroutine(GameTimer());
    }


    private IEnumerator GameTimer()
    {
        while (gameTime > 0)
        {
            yield return new WaitForSeconds(1f);
            gameTime -= 1f;
            if (gameTime < 0) gameTime = 0;
            UpdateTimeUI();
        }

        isGameActive = false;

        // Board'u gizle
        foreach (Transform child in transform)
            child.gameObject.SetActive(false);

        // GameOver text göster
        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(true);
            gameOverText.text = "Skorunuz: " + score;
        }

        Debug.Log("Oyun bitti! Puanýnýz: " + score);
    }



    void UpdateTimeUI()
    {
        if (timeText != null)
            timeText.text = Mathf.CeilToInt(gameTime).ToString();
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = score.ToString();
    }

    void SetupBoard()
    {
        float offsetX = -width / 2f + 0.5f;
        float offsetY = -height / 2f + 0.5f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2 position = new Vector2((x + offsetX) * spacing, (y + offsetY) * spacing);
                GameObject gemObj = Instantiate(gemPrefab, position, Quaternion.identity, transform);

                int randomIndex = Random.Range(0, gemTypes.Length);
                GemType chosenType = gemTypes[randomIndex];

                Gem gem = gemObj.GetComponent<Gem>();
                gem.Init(chosenType, x, y);
            }
        }
    }

    private IEnumerator CheckInitialMatches()
    {
        yield return new WaitForEndOfFrame();

        List<Gem> matches = GetMatches();
        while (matches.Count > 0)
        {
            DestroyMatches(matches);
            AddScore(matches.Count);
            yield return new WaitForSeconds(0.25f);
            yield return StartCoroutine(DropGems());
            matches = GetMatches();
        }
    }

    public void TrySwap(Gem first, Gem second)
    {
        if (!isGameActive) return; // Oyun baþlamadýysa swap yok

        int dx = Mathf.Abs(first.gridX - second.gridX);
        int dy = Mathf.Abs(first.gridY - second.gridY);
        if ((dx + dy) != 1) return;

        StartCoroutine(SwapCoroutine(first, second));
    }

    private IEnumerator SwapCoroutine(Gem first, Gem second)
    {
        isSwapping = true;

        Vector3 posA = first.transform.position;
        Vector3 posB = second.transform.position;

        float t = 0f;
        float duration = 0.2f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float factor = t / duration;
            first.transform.position = Vector3.Lerp(posA, posB, factor);
            second.transform.position = Vector3.Lerp(posB, posA, factor);
            yield return null;
        }

        // Grid koordinatlarýný swap et
        int tempX = first.gridX;
        int tempY = first.gridY;
        first.gridX = second.gridX;
        first.gridY = second.gridY;
        second.gridX = tempX;
        second.gridY = tempY;

        first.transform.position = posB;
        second.transform.position = posA;

        // Zincirleme eþleþmeleri kontrol et
        yield return StartCoroutine(CheckMatchesLoop());

        // Hamle yoksa shuffle
        if (!HasPossibleMoves())
        {
            yield return StartCoroutine(ShuffleBoard());
        }

        isSwapping = false;
    }

    private IEnumerator CheckMatchesLoop()
    {
        List<Gem> matches = GetMatches();
        while (matches.Count > 0)
        {
            DestroyMatches(matches);
            AddScore(matches.Count);
            yield return new WaitForSeconds(0.2f);
            yield return StartCoroutine(DropGems());
            matches = GetMatches();
        }
    }

    private Gem GetGemAt(int x, int y)
    {
        foreach (Gem gem in GetComponentsInChildren<Gem>())
        {
            if (gem.gridX == x && gem.gridY == y) return gem;
        }
        return null;
    }

    public List<Gem> GetMatches()
    {
        List<Gem> matches = new List<Gem>();

        // Yatay kontrol
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width - 2; x++)
            {
                Gem g1 = GetGemAt(x, y);
                Gem g2 = GetGemAt(x + 1, y);
                Gem g3 = GetGemAt(x + 2, y);

                if (g1 != null && g2 != null && g3 != null)
                {
                    if (g1.type == g2.type && g2.type == g3.type)
                    {
                        if (!matches.Contains(g1)) matches.Add(g1);
                        if (!matches.Contains(g2)) matches.Add(g2);
                        if (!matches.Contains(g3)) matches.Add(g3);
                    }
                }
            }
        }

        // Dikey kontrol
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height - 2; y++)
            {
                Gem g1 = GetGemAt(x, y);
                Gem g2 = GetGemAt(x, y + 1);
                Gem g3 = GetGemAt(x, y + 2);

                if (g1 != null && g2 != null && g3 != null)
                {
                    if (g1.type == g2.type && g2.type == g3.type)
                    {
                        if (!matches.Contains(g1)) matches.Add(g1);
                        if (!matches.Contains(g2)) matches.Add(g2);
                        if (!matches.Contains(g3)) matches.Add(g3);
                    }
                }
            }
        }

        return matches;
    }

    public void DestroyMatches(List<Gem> matches)
    {
        foreach (Gem gem in matches)
        {
            Destroy(gem.gameObject);
        }
    }

    private IEnumerator DropGems()
    {
        for (int x = 0; x < width; x++)
        {
            int emptyCount = 0;
            for (int y = 0; y < height; y++)
            {
                Gem gem = GetGemAt(x, y);
                if (gem == null)
                {
                    emptyCount++;
                }
                else if (emptyCount > 0)
                {
                    Vector3 targetPos = new Vector3((x - width / 2f + 0.5f) * spacing,
                                                    (y - emptyCount - height / 2f + 0.5f) * spacing,
                                                    0);
                    gem.gridY -= emptyCount;
                    StartCoroutine(FallGem(gem, targetPos));
                }
            }

            for (int i = 0; i < emptyCount; i++)
            {
                int y = height - emptyCount + i;
                Vector3 spawnPos = new Vector3((x - width / 2f + 0.5f) * spacing,
                                               (y - height / 2f + 0.5f) * spacing + 2f,
                                               0);
                GameObject gemObj = Instantiate(gemPrefab, spawnPos, Quaternion.identity, transform);
                int randomIndex = Random.Range(0, gemTypes.Length);
                GemType chosenType = gemTypes[randomIndex];
                Gem newGem = gemObj.GetComponent<Gem>();
                newGem.Init(chosenType, x, y);

                Vector3 targetPos = new Vector3((x - width / 2f + 0.5f) * spacing,
                                                (y - height / 2f + 0.5f) * spacing,
                                                0);
                StartCoroutine(FallGem(newGem, targetPos));
            }
        }

        yield return new WaitForSeconds(0.25f);
    }

    private IEnumerator FallGem(Gem gem, Vector3 targetPos)
    {
        float t = 0f;
        float duration = 0.2f;
        Vector3 startPos = gem.transform.position;

        while (t < duration)
        {
            t += Time.deltaTime;
            float factor = t / duration;
            gem.transform.position = Vector3.Lerp(startPos, targetPos, factor);
            yield return null;
        }

        gem.transform.position = targetPos;
    }

    private bool HasPossibleMoves()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Gem gem = GetGemAt(x, y);
                if (gem == null) continue;

                if (x < width - 1)
                {
                    SwapGemTypes(gem, GetGemAt(x + 1, y));
                    if (GetMatches().Count > 0) { SwapGemTypes(gem, GetGemAt(x + 1, y)); return true; }
                    SwapGemTypes(gem, GetGemAt(x + 1, y));
                }
                if (y < height - 1)
                {
                    SwapGemTypes(gem, GetGemAt(x, y + 1));
                    if (GetMatches().Count > 0) { SwapGemTypes(gem, GetGemAt(x, y + 1)); return true; }
                    SwapGemTypes(gem, GetGemAt(x, y + 1));
                }
            }
        }
        return false;
    }

    private void SwapGemTypes(Gem a, Gem b)
    {
        if (a == null || b == null) return;
        GemType temp = a.type;
        a.type = b.type;
        b.type = temp;
    }

    private IEnumerator ShuffleBoard()
    {
        isSwapping = true;

        List<Gem> allGems = new List<Gem>(GetComponentsInChildren<Gem>());
        List<GemType> types = new List<GemType>();
        foreach (Gem gem in allGems)
            types.Add(gem.type);

        for (int i = 0; i < types.Count; i++)
        {
            int randIndex = Random.Range(0, types.Count);
            GemType temp = types[i];
            types[i] = types[randIndex];
            types[randIndex] = temp;
        }

        float duration = 0.5f;
        float t = 0f;

        List<Vector3> positions = new List<Vector3>();
        foreach (Gem gem in allGems)
            positions.Add(gem.transform.position);

        List<Vector3> targetPositions = new List<Vector3>(positions);
        for (int i = 0; i < targetPositions.Count; i++)
        {
            int randIndex = Random.Range(0, targetPositions.Count);
            Vector3 tempPos = targetPositions[i];
            targetPositions[i] = targetPositions[randIndex];
            targetPositions[randIndex] = tempPos;
        }

        while (t < duration)
        {
            t += Time.deltaTime;
            float factor = t / duration;
            for (int i = 0; i < allGems.Count; i++)
            {
                allGems[i].transform.position = Vector3.Lerp(positions[i], targetPositions[i], factor);
            }
            yield return null;
        }

        for (int i = 0; i < allGems.Count; i++)
        {
            allGems[i].transform.position = targetPositions[i];
            allGems[i].type = types[i];
            allGems[i].GetComponent<SpriteRenderer>().sprite = types[i].sprite;

            int newX = Mathf.RoundToInt((targetPositions[i].x / spacing) + width / 2f - 0.5f);
            int newY = Mathf.RoundToInt((targetPositions[i].y / spacing) + height / 2f - 0.5f);
            allGems[i].gridX = newX;
            allGems[i].gridY = newY;
        }

        isSwapping = false;

        yield return StartCoroutine(CheckMatchesLoop());

        if (!HasPossibleMoves())
        {
            yield return StartCoroutine(ShuffleBoard());
        }
    }

    private void AddScore(int matchedCount)
    {
        if (!isGameActive) return;
        score += matchedCount * 10;
        UpdateScoreUI();
    }
}
