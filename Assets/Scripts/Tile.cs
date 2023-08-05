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

    private void OnMouseDown()
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
                SwapTiles(previousSelected, this);
                previousSelected.Deselect();
            }
        }
    }

    public async void SwapTiles(Tile tile1, Tile tile2)
    {
        isSwapping = true;

        await Swap(tile1, tile2);

        isSwapping = false;
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
}
