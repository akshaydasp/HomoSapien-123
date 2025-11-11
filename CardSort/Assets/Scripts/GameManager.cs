using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Prefabs & assets")]
    public Card cardPrefab;
    public RectTransform boardArea; 
    public Sprite cardBackSprite;
    public List<Sprite> cardFaceSprites; 

    [Header("Game settings")]
    public int cols = 4;
    public int rows = 3;
    public float spacing = 6f;
    public float flipDuration = 0.25f;
    public float mismatchDelay = 0.8f;
    public bool shuffle = true;

    [Header("Layouts")]
    public Vector2Int[] allowedLayouts = new Vector2Int[]
{
    new Vector2Int(2,2),
    new Vector2Int(2,3),
    new Vector2Int(3,4),
    new Vector2Int(4,4),
    new Vector2Int(5,4),
    new Vector2Int(5,6),
};

    // runtime
    List<Card> allCards = new List<Card>();
    Queue<Card> waitingQueue = new Queue<Card>(); 
    int pairsCount => (cols * rows) / 2;

    [Header("Scoring")]
    public ScoreManager scoreManager;

    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        NewRandomGame();
    }
    // optional function for manual layout
    public void SetupBoard(int c, int r)
    {
        cols = c; rows = r;
        ClearBoard();
        var positions = GridLayoutController.GenerateGridPositions(boardArea.rect.size, cols, rows, spacing);
        // prepare card ids (pairs)
        List<int> ids = new List<int>();
        for (int i = 0; i < pairsCount; i++)
        {
            ids.Add(i);
            ids.Add(i);
        }
        if (shuffle)
        {
            System.Random rnd = new System.Random();
            ids = ids.OrderBy(x => rnd.Next()).ToList();
        }
        // ensure we have enough sprites
        if (cardFaceSprites.Count < pairsCount)
            Debug.LogWarning("Not enough card face sprites; sprites will repeat.");

        for (int i = 0; i < ids.Count; i++)
        {
            var pos = positions[i];
            var cobj = Instantiate(cardPrefab, boardArea);
            cobj.transform.localPosition = pos;
            int id = ids[i];
            Sprite face = cardFaceSprites[id % cardFaceSprites.Count];
            cobj.flipDuration = flipDuration;
            cobj.Setup(id, face, cardBackSprite);
            allCards.Add(cobj);
        }
        LayoutScaleCards();
    }

    void ClearBoard()
    {
        foreach (var c in allCards) if (c) Destroy(c.gameObject);
        allCards.Clear();
        waitingQueue.Clear();
        if (scoreManager) scoreManager.ResetScore();
    }

    void LayoutScaleCards()
    {
        // compute card size to fit boardArea given rows/cols
        if (allCards.Count == 0) return;
        // get a card rect size defaults (we'll compute scale)
        RectTransform sample = allCards[0].GetComponent<RectTransform>();
        Vector2 boardSz = boardArea.rect.size;
        float totalSpacingX = spacing * (cols + 1);
        float totalSpacingY = spacing * (rows + 1);

        float availableW = boardSz.x - totalSpacingX;
        float availableH = boardSz.y - totalSpacingY;

        float cardW = availableW / cols;
        float cardH = availableH / rows;

        // preserve sample aspect ratio:
        float sampleAR = sample.rect.width / sample.rect.height;
        float useH = Mathf.Min(cardH, cardW / sampleAR);
        float useW = useH * sampleAR;

        foreach (var c in allCards)
        {
            RectTransform rt = c.GetComponent<RectTransform>();
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, useW);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, useH);
        }
    }

    // Called by Card when clicked (via OnPointerDown)
    public void OnCardSelected(Card card)
    {
        if (card.IsLocked || card.IsFaceUp || card.IsMatched) return;

        // play flip SFX
        AudioManager.Instance.PlayFlip();

        // flip immediately
        card.Flip(true, this);

        // enqueue and handle pair checking
        waitingQueue.Enqueue(card);
        // if a pair exists (at least two waiting), process oldest pair
        if (waitingQueue.Count >= 2)
        {
            Card a = waitingQueue.Dequeue();
            Card b = waitingQueue.Dequeue();
            StartCoroutine(ProcessPair(a, b));
        }
    }

    IEnumerator ProcessPair(Card a, Card b)
    {
        // ensure they're not null / already matched
        if (a == null || b == null) yield break;
        if (a.IsMatched || b.IsMatched) yield break;

        // small delay to allow flip animation to complete visually
        yield return new WaitForSeconds(Mathf.Max(0.0f, Mathf.Min(flipDuration, 0.12f)));

        if (a.cardId == b.cardId)
        {
            // match!
            a.SetMatched();
            b.SetMatched();
            scoreManager?.OnMatch();
            AudioManager.Instance.PlayMatch();
            // check game over
            if (allCards.All(c => c.IsMatched))
            {
                OnGameOver();
            }
        }
        else
        {
            // mismatch: play sound and flip back after mismatchDelay
            AudioManager.Instance.PlayMismatch();
            // lock both until flipback begins
            a.IsLocked = true;
            b.IsLocked = true;
            yield return new WaitForSeconds(mismatchDelay);
            a.Flip(false, this);
            b.Flip(false, this);
            // unlock after flip animation duration
            yield return new WaitForSeconds(flipDuration + 0.01f);
            a.IsLocked = false;
            b.IsLocked = false;
            scoreManager?.OnMismatch();
        }
    }

    void OnGameOver()
    {
        AudioManager.Instance.PlayGameOver();
        scoreManager?.OnGameOver();
        Debug.Log("Game Over! Score: " + (scoreManager ? scoreManager.CurrentScore : 0));
    }

    #region Save/Load hooks
    public void SaveGame(string filename = "save.json")
    {
        SaveSystem.Save(this, filename);
    }

    public void LoadGame(string filename = "save.json")
    {
        var state = SaveSystem.Load(filename);
        if (state == null) return;
        // restore board using state
        cols = state.cols;
        rows = state.rows;
       
        ClearBoard();
        var positions = GridLayoutController.GenerateGridPositions(boardArea.rect.size, cols, rows, spacing);
        var ids = state.cardOrder;
        for (int i = 0; i < ids.Length; i++)
        {
            var pos = positions[i];
            var cobj = Instantiate(cardPrefab, boardArea);
            cobj.transform.localPosition = pos;
            int id = ids[i];
            Sprite face = cardFaceSprites[id % cardFaceSprites.Count];
            cobj.flipDuration = flipDuration;
            cobj.Setup(id, face, cardBackSprite);
            allCards.Add(cobj);
            // restore matched state
            if (state.matchedIndices.Contains(i))
            {
                cobj.ForceFlipImmediate(true);
                cobj.SetMatchedImmediate(); 
            }
            else if (state.faceUpIndices.Contains(i))
            {
                cobj.ForceFlipImmediate(true);
            }
        }
        LayoutScaleCards();
        if (scoreManager) scoreManager.LoadState(state);
    }
    void OnApplicationPause(bool pause)
    {
        if (pause) SaveGame();
    }
    void OnApplicationQuit()
    {
        SaveGame();
    }

    #endregion

    public List<int> GetCardOrder()
    {
        return allCards.Select(c => c.cardId).ToList();
    }

    public List<int> GetMatchedIndices()
    {
        var list = new List<int>();
        for (int i = 0; i < allCards.Count; i++)
            if (allCards[i].IsMatched) list.Add(i);
        return list;
    }

    public List<int> GetFaceUpIndices()
    {
        var list = new List<int>();
        for (int i = 0; i < allCards.Count; i++)
            if (allCards[i].IsFaceUp && !allCards[i].IsMatched) list.Add(i);
        return list;
    }

    #region Randomly calling Layouts

    System.Random _layoutRng = new System.Random();
    Vector2Int _lastLayout;  


    public void NewRandomGame()
    {
        var pick = PickRandomLayoutDifferent();
        SetupBoard(pick.x, pick.y);
        _lastLayout = pick;
    }

    // Ensures we always pick an even cell count, and try not to repeat the last layout
    Vector2Int PickRandomLayoutDifferent()
    {
        if (allowedLayouts == null || allowedLayouts.Length == 0)
        {
            // Fallback: keep current cols/rows if none configured
            return new Vector2Int(cols, rows);
        }

        // Filter to even-cell layouts
        var valid = new System.Collections.Generic.List<Vector2Int>();
        foreach (var l in allowedLayouts)
        {
            if (((l.x * l.y) % 2) == 0)
                valid.Add(l);
        }
        if (valid.Count == 0) valid.Add(new Vector2Int(cols, rows));

        // Choose random, avoiding immediate repeat if possible
        Vector2Int chosen = valid[_layoutRng.Next(valid.Count)];
        if (valid.Count > 1 && chosen == _lastLayout)
        {
            // pick another
            chosen = valid[_layoutRng.Next(valid.Count)];
        }
        return chosen;
    }
    #endregion
}
