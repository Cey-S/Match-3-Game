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
    private static bool isSwapping = false;

    private SpriteRenderer render;
    private bool isSelected = false;
    private bool matchFound = false;

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
        if (isSwapping || BoardManager.instance.IsShifting || render.sprite == null)
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
                    await Swap(previousSelected, this);
                    
                    previousSelected.ClearAllMatches();
                    previousSelected.Deselect();
                    ClearAllMatches();
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
        isSwapping = true;

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

        isSwapping = false;
    }

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

    public void ClearAllMatches()
    {
        if (render.sprite == null)
        {
            return;
        }

        ClearMatch(new Vector2[2] { Vector2.left, Vector2.right });
        ClearMatch(new Vector2[2] { Vector2.up, Vector2.down });
        if (matchFound)
        {
            render.sprite = null;
            matchFound = false;
        }
    }

    private void ClearMatch(Vector2[] paths)
    {
        List<GameObject> matchingTiles = new List<GameObject>();
        for (int i = 0; i < paths.Length; i++)
        {
            matchingTiles.AddRange(FindMatch(paths[i]));
        }

        if (matchingTiles.Count >= 2)
        {
            for (int i = 0; i < matchingTiles.Count; i++)
            {
                matchingTiles[i].GetComponent<SpriteRenderer>().sprite = null;
            }

            matchFound = true;
        }
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
