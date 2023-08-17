using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using DG.Tweening;

public class Tile : MonoBehaviour
{
    public struct GridPosition
    {
        public int X;
        public int Y;
        public bool IsFilled;

        public GridPosition(int x, int y, bool isFilled)
        {
            X = x;
            Y = y;
            IsFilled = isFilled;
        }

        public override string ToString() => $"({X}, {Y})";
    }

    public GridPosition GridPos;

    public static bool LockTiles { get; private set; } // prevent tiles from being selected

    private static Color selectedColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
    private static Tile previousSelected = null;
    private bool isSelected = false;

    private SpriteRenderer render;
    private Vector2[] adjacentDirections = new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

    private const float TweenDuration = 0.25f;

    private void Awake()
    {
        render = GetComponent<SpriteRenderer>();
    }

    private void Select()
    {
        isSelected = true;
        render.color = selectedColor;
        previousSelected = gameObject.GetComponent<Tile>();
    }

    private void Deselect()
    {
        isSelected = false;
        render.color = Color.white;
        previousSelected = null;
    }

    private async void OnMouseDown()
    {
        if (GameManager.Instance.GameOver || LockTiles || BoardManager.Instance.IsShifting || render.sprite == null)
        {
            return;
        }

        if (isSelected) // same tile
        {
            Deselect();
        }
        else
        {
            if (previousSelected == null) // first tile selection
            {
                Select();
            }
            else
            {
                if (GetAllAdjacentTiles().Contains(previousSelected.gameObject))
                {
                    LockTiles = true;

                    await Swap(previousSelected, this);

                    var tasks = new List<Task>();
                    tasks.Add(previousSelected.ClearAllMatches());
                    tasks.Add(ClearAllMatches());
                    
                    await Task.WhenAll(tasks);
                    
                    previousSelected.Deselect();

                    BoardManager.Instance.FindEmptyTiles();
                    
                    LockTiles = false;
                }
                else
                {
                    previousSelected.GetComponent<Tile>().Deselect();
                    Select();
                }                
            }
        }
    }

    public async Task Swap(Tile tile1, Tile tile2)
    {
        Vector3 tile1Pos = tile1.transform.position;
        Vector3 tile2Pos = tile2.transform.position;

        GridPosition grid1 = tile1.GridPos;
        GridPosition grid2 = tile2.GridPos;


        if (!BoardManager.Instance.IsShifting && tile1.GetComponent<SpriteRenderer>().sprite == tile2.GetComponent<SpriteRenderer>().sprite)
        {
            var yoyoSequence = DOTween.Sequence();

            yoyoSequence.Join(tile1.transform.DOMove(tile2Pos, TweenDuration).SetLoops(2, LoopType.Yoyo))
                .Join(tile2.transform.DOMove(tile1Pos, TweenDuration).SetLoops(2, LoopType.Yoyo));

            await yoyoSequence.Play().AsyncWaitForCompletion();
        }
        else
        {
            var swapSequence = DOTween.Sequence();

            swapSequence.Join(tile1.transform.DOMove(tile2Pos, TweenDuration))
                .Join(tile2.transform.DOMove(tile1Pos, TweenDuration));

            await swapSequence.Play().AsyncWaitForCompletion();
                        
            var tempParent = tile1.transform.parent;
            tile1.transform.SetParent(tile2.transform.parent);
            tile2.transform.SetParent(tempParent);

            tile1.GridPos = grid2;
            tile2.GridPos = grid1;

            BoardManager.Instance.RefreshBoard(grid1, tile2.gameObject);
            BoardManager.Instance.RefreshBoard(grid2, tile1.gameObject);

            GameUIHandler.Instance.MoveCounter--; // Swapping happened, decrease one move
        }
    }

    #region FINDING ADJACENT TILES
    private List<GameObject> GetAllAdjacentTiles()
    {
        List<GameObject> adjacentTiles = new List<GameObject>();
        for (int i = 0; i < adjacentDirections.Length; i++)
        {
            adjacentTiles.Add(GetAdjacent(adjacentDirections[i]));
        }

        return adjacentTiles;
    }

    private GameObject GetAdjacent(Vector2 castDir)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, castDir);
        if (hit.collider != null)
        {
            return hit.collider.gameObject;
        }

        return null;
    }
    #endregion

    public async Task ClearAllMatches()
    {
        if (render.sprite == null)
        {
            return;
        }

        List<GameObject> matchingTiles = MatchToClear();

        if (matchingTiles.Count != 0)
        {
            matchingTiles.Add(transform.gameObject);

            await Clear(matchingTiles);
        }
    }

    private async Task Clear(List<GameObject> tiles)
    {
        Vector3 originalScale = transform.localScale;

        var sequence = DOTween.Sequence();

        foreach (GameObject tile in tiles)
        {
            sequence.Join(tile.transform.DOScale(Vector3.zero, 0.5f));
            tile.GetComponent<Tile>().GridPos.IsFilled = false;
        }

        await sequence.Play().AsyncWaitForCompletion();

        foreach (GameObject tile in tiles)
        {
            GameUIHandler.Instance.Score += 10; // For each tile cleared, add score

            tile.transform.localScale = originalScale;
            tile.GetComponent<SpriteRenderer>().sprite = null;
        }
    }

    private List<GameObject> MatchToClear()
    {
        List<GameObject> matchToClear = new List<GameObject>();

        List<GameObject> matching_Horizontal = new List<GameObject>();
        List<GameObject> matching_Vertical = new List<GameObject>();

        for (int i = 0; i < adjacentDirections.Length; i++)
        {
            if (i < 2)
            {
                matching_Vertical.AddRange(FindMatch(adjacentDirections[i]));
            }
            else
            {
                matching_Horizontal.AddRange(FindMatch(adjacentDirections[i]));
            }
        }

        if (matching_Vertical.Count >= 2)
        {
            matchToClear.AddRange(matching_Vertical);
        }
        
        if (matching_Horizontal.Count >= 2)
        {
            matchToClear.AddRange(matching_Horizontal);
        }

        return matchToClear;
    }    

    private List<GameObject> FindMatch(Vector2 castDir)
    {
        List<GameObject> matchingTiles = new List<GameObject>();
        RaycastHit2D hit = Physics2D.Raycast(transform.position, castDir);
        while (hit.collider != null && hit.collider.GetComponent<SpriteRenderer>().sprite == render.sprite)
        {
            matchingTiles.Add(hit.collider.gameObject);
            hit = Physics2D.Raycast(hit.collider.transform.position, castDir);
        }

        return matchingTiles;
    }
}
