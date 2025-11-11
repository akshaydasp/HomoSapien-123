using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class Card : MonoBehaviour, IPointerDownHandler
{
    public int cardId; 
    public Image frontImage;
    public Image backImage;
    public float flipDuration = 0.25f;
    public bool IsMatched;
    public bool IsFaceUp;
    public bool IsLocked;  

    GameManager gm;
    CanvasGroup canvasGroup;

    void Awake()
    {
        gm = GameManager.instance;
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void Setup(int id, Sprite frontSprite, Sprite backSprite)
    {
        cardId = id;
        if (frontImage) frontImage.sprite = frontSprite;
        if (backImage) backImage.sprite = backSprite;
        IsMatched = false;
        IsFaceUp = false;
        IsLocked = false;
        transform.localScale = Vector3.one;
        frontImage.gameObject.SetActive(false);
        backImage.gameObject.SetActive(true);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (IsLocked || IsMatched || IsFaceUp) return;
        gm.OnCardSelected(this);
    }

    public void ForceFlipImmediate(bool faceUp)
    {
        IsFaceUp = faceUp;
        frontImage.gameObject.SetActive(faceUp);
        backImage.gameObject.SetActive(!faceUp);
    }

    public void SetMatched()
    {
        IsMatched = true;
        // optionally, animate matched removal
        StartCoroutine(MatchPulseAndDisable());
    }

    IEnumerator MatchPulseAndDisable()
    {
        // simple scale pulse
        float t = 0f;
        Vector3 start = transform.localScale;
        Vector3 end = start * 1.12f;
        while (t < 0.12f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(start, end, t / 0.12f);
            yield return null;
        }
        yield return new WaitForSeconds(0.12f);
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        float fadeDur = 0.2f;
        float f = 0f;
        while (f < fadeDur)
        {
            f += Time.deltaTime;
            canvasGroup.alpha = 1 - (f / fadeDur);
            yield return null;
        }
        gameObject.SetActive(false);
    }

    // Flip animation coroutine
    public IEnumerator FlipCoroutine(bool faceUp)
    {
        IsLocked = true;
        float half = flipDuration / 2f;
        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float v = t / half;
            float angle = Mathf.Lerp(0f, 90f, v);
            transform.localEulerAngles = new Vector3(0, angle, 0);
            yield return null;
        }
        frontImage.gameObject.SetActive(faceUp);
        backImage.gameObject.SetActive(!faceUp);

        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float v = t / half;
            float angle = Mathf.Lerp(90f, 0f, v);
            transform.localEulerAngles = new Vector3(0, angle, 0);
            yield return null;
        }
        IsFaceUp = faceUp;
        IsLocked = false;
        yield break;
    }

    public void Flip(bool show, MonoBehaviour owner)
    {
        owner.StartCoroutine(FlipCoroutine(show));
    }

    public void SetMatchedImmediate()
    {
        IsMatched = true;
        var cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
        gameObject.SetActive(false);
    }
}
