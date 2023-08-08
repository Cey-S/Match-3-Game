using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public static BoardManager instance;

    public RectTransform board;
    public List<Sprite> characters = new List<Sprite>();
    public List<Sprite> gridColors = new List<Sprite>();
    public GameObject grid;
    public GameObject tile; // tile prefab

    private Vector3[] boardCorners = new Vector3[4]; // board corners on world space, clockwise, 0 is bottom left
    private int _rowSize;
    private int _columnSize;
    private Vector2 tileSize;
    private GameObject[,] tiles;

    public bool IsShifting { get; private set; }

    private void Start()
    {
        instance = GetComponent<BoardManager>();

        Vector3[] corners = new Vector3[4];
        board.GetWorldCorners(corners); // board corners on canvas

        for (int i = 0; i < corners.Length; i++)
        {
            boardCorners[i] = Camera.main.ScreenToWorldPoint(corners[i]);
        }

        float boardWidth = boardCorners[3].x - boardCorners[0].x;
        float boardHeight = boardCorners[1].y - boardCorners[0].y;

        tileSize = tile.GetComponent<SpriteRenderer>().bounds.size;

        _rowSize = Mathf.FloorToInt(boardWidth / tileSize.x);
        _columnSize = Mathf.FloorToInt(boardHeight / tileSize.y);

        // for centering the board
        float offset = (boardWidth - (tileSize.x * _rowSize)) * 0.5f;

        CreateBoard(_rowSize, _columnSize, offset);
    }

    private void CreateBoard(int rowSize, int columnSize, float offset)
    {
        tiles = new GameObject[rowSize, columnSize];

        float startX = boardCorners[0].x + tileSize.x * 0.5f + offset;
        float startY = boardCorners[0].y + tileSize.y * 0.5f;

        Sprite[] previousLeft = new Sprite[columnSize];
        Sprite previousBelow = null;

        for (int x = 0; x < rowSize; x++)
        {
            for (int y = 0; y < columnSize; y++)
            {
                Vector3 tilePos = new Vector3(startX + (x * tileSize.x), startY + (y * tileSize.y), 0);
                GameObject newGrid = Instantiate(grid, tilePos, grid.transform.rotation);
                GameObject newTile = Instantiate(tile, tilePos, tile.transform.rotation);

                newTile.GetComponent<Tile>().GridPos = new Tile.GridPosition(x, y, true);

                newGrid.transform.parent = transform;
                newTile.transform.parent = newGrid.transform;

                Sprite gridColor = ((x + y) % 2 == 0) ? gridColors[0] : gridColors[1]; // Checkered pattern
                newGrid.GetComponent<SpriteRenderer>().sprite = gridColor;

                List<Sprite> possibleCharacters = new List<Sprite>();
                possibleCharacters.AddRange(characters);
                possibleCharacters.Remove(previousLeft[y]);
                possibleCharacters.Remove(previousBelow);

                Sprite randomSprite = possibleCharacters[Random.Range(0, possibleCharacters.Count)];
                newTile.GetComponent<SpriteRenderer>().sprite = randomSprite;

                previousLeft[y] = randomSprite;
                previousBelow = randomSprite;

                tiles[x, y] = newTile;
            }
        }
    }

    public async void FindEmptyTiles()
    {
        IsShifting = true;

        var shiftTasks = new List<Task>();

        for (int x = 0; x < _rowSize; x++)
        {
            for (int y = 0; y < _columnSize; y++)
            {
                if (!tiles[x, y].GetComponent<Tile>().GridPos.IsFilled)
                {
                    shiftTasks.Add(ShiftTilesDownFrom(x, y));
                    break;
                }
            }
        }

        if (shiftTasks.Count != 0)
        {
            await Task.WhenAll(shiftTasks);

            Combos();
        }

        IsShifting = false;
    }

    private async void Combos()
    {
        for (int x = 0; x < _rowSize; x++)
        {
            for (int y = 0; y < _columnSize; y++)
            {
                await tiles[x, y].GetComponent<Tile>().ClearAllMatches();
            }
        }

        FindEmptyTiles();
    }

    private async Task ShiftTilesDownFrom(int x, int y)
    {
        int yCurrent = y;
        int shiftingTiles = _columnSize - yCurrent;
        Vector3[] originalPos = new Vector3[shiftingTiles];
        List<Tile> verticalMatch = new List<Tile>();
        List<Tile> firstShift = new List<Tile>();

        for (int i = 0; i < shiftingTiles; i++)
        {
            Tile currentTile = tiles[x, y].GetComponent<Tile>();
            originalPos[i] = currentTile.transform.position;

            if (!currentTile.GridPos.IsFilled)
            {
                verticalMatch.Add(currentTile);
            }
            else
            {
                firstShift.Add(currentTile);
            }

            y++;
        }

        int newSpawn = verticalMatch.Count;

        for (int i = 0; i < newSpawn; i++)
        {
            Vector3 offset = new Vector3(0, tileSize.y + (i * tileSize.y), 0);
            verticalMatch[i].transform.position = tiles[x, _columnSize - 1].transform.position + offset;

            Sprite randomSprite = characters[Random.Range(0, characters.Count)];
            verticalMatch[i].GetComponent<SpriteRenderer>().sprite = randomSprite;
        }

        List<Tile> shiftOrder = new List<Tile>();
        shiftOrder.AddRange(firstShift);
        shiftOrder.AddRange(verticalMatch);

        var sequence = DOTween.Sequence();

        for (int i = 0; i < shiftingTiles; i++)
        {
            sequence.Join(shiftOrder[i].transform.DOMove(originalPos[i], 0.25f));
            
            Tile.GridPosition currentGrid = new Tile.GridPosition(x, yCurrent, true);
            shiftOrder[i].GridPos = currentGrid;
            RefreshBoard(currentGrid, shiftOrder[i].gameObject);
            
            yCurrent++;
        }

        await sequence.Play().AsyncWaitForCompletion();
    }    

    public void RefreshBoard(Tile.GridPosition grid, GameObject tile)
    {
        tiles[grid.X, grid.Y] = tile;
    }
}
