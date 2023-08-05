using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using DG.Tweening;

public class Tile : MonoBehaviour
{
    private Vector3 position;
    public Vector3 Position
    {
        get { return position; }
        set { position = value; }
    }

    private const float TweenDuration = 0.25f;

    private static Color selectedColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
    private static Tile previousSelected = null;
    private static bool lockTiles = false; // prevent tiles from being selected

    private SpriteRenderer render;
    private bool isSelected = false;

    private Vector2[] adjacentDirections = new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

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
        if (lockTiles || BoardManager.instance.IsShifting || render.sprite == null)
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
                    lockTiles = true;

                    await Swap(previousSelected, this);

                    var tasks = new List<Task>();
                    tasks.Add(previousSelected.ClearAllMatches());
                    tasks.Add(ClearAllMatches());
                    
                    await Task.WhenAll(tasks);
                    
                    previousSelected.Deselect();
                    
                    lockTiles = false;
                }
                else
                {
                    previousSelected.GetComponent<Tile>().Deselect();
                    Select();
                }                
            }
        }
    }

    private async Task Swap(Tile tile1, Tile tile2)
    {
        if (tile1.GetComponent<SpriteRenderer>().sprite == tile2.GetComponent<SpriteRenderer>().sprite)
        {
            var yoyoSequence = DOTween.Sequence();

            yoyoSequence.Join(tile1.transform.DOMove(tile2.Position, TweenDuration).SetLoops(2, LoopType.Yoyo))
                .Join(tile2.transform.DOMove(tile1.Position, TweenDuration).SetLoops(2, LoopType.Yoyo));

            await yoyoSequence.Play().AsyncWaitForCompletion();
        }
        else
        {
            var swapSequence = DOTween.Sequence();

            swapSequence.Join(tile1.transform.DOMove(tile2.Position, TweenDuration))
                .Join(tile2.transform.DOMove(tile1.Position, TweenDuration));

            await swapSequence.Play().AsyncWaitForCompletion();

            tile1.Position = tile1.transform.position;
            tile2.Position = tile2.transform.position;

            var tempParent = tile1.transform.parent;
            tile1.transform.SetParent(tile2.transform.parent);
            tile2.transform.SetParent(tempParent);
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
        RaycastHit2D hit = Physics2D.Raycast(Position, castDir);
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
            await Clear(matchingTiles);
        }
    }

    private async Task Clear(List<GameObject> tiles)
    {
        var sequence = DOTween.Sequence();

        tiles.Add(transform.gameObject);

        foreach (GameObject tile in tiles)
        {
            sequence.Join(tile.transform.DOScale(Vector3.zero, 0.5f));
        }

        await sequence.Play().AsyncWaitForCompletion();
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
        RaycastHit2D hit = Physics2D.Raycast(Position, castDir);
        while (hit.collider != null && hit.collider.GetComponent<SpriteRenderer>().sprite == render.sprite)
        {
            matchingTiles.Add(hit.collider.gameObject);
            hit = Physics2D.Raycast(hit.collider.transform.position, castDir);
        }

        return matchingTiles;
    }
}
