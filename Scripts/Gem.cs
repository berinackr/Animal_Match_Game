using UnityEngine;

public class Gem : MonoBehaviour
{
    public GemType type;
    private SpriteRenderer sr;
    public int gridX;
    public int gridY;

    private Vector3 originalScale;
    private static Gem selectedGem = null;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;
    }

    public void Init(GemType newType, int x, int y)
    {
        type = newType;
        sr.sprite = type.sprite;
        gridX = x;
        gridY = y;
    }

    void OnMouseDown()
    {
        if (Board.isSwapping) return;

        if (selectedGem == null)
        {
            selectedGem = this;
            transform.localScale = originalScale * 1.2f;
        }
        else if (selectedGem == this)
        {
            transform.localScale = originalScale;
            selectedGem = null;
        }
        else
        {
            Board board = FindObjectOfType<Board>();
            board.TrySwap(selectedGem, this);

            selectedGem.transform.localScale = selectedGem.originalScale;
            selectedGem = null;
        }
    }
}
