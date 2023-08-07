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

    public async Task FindEmptyTiles()
    {
        IsShifting = true;

        var shiftTasks = new List<Task>();

        for (int x = 0; x < _rowSize; x++)
        {
            List<Tile> horizontalMatch = new List<Tile>();
            for (int y = 0; y < _columnSize; y++)
            {
                if (!tiles[x, y].GetComponent<Tile>().GridPos.IsFilled)
                {
                    horizontalMatch.Add(tiles[x, y].GetComponent<Tile>());
                }
            }
            shiftTasks.Add(ShiftTilesDown(horizontalMatch));
        }

        await Task.WhenAll(shiftTasks);

        
        IsShifting = false;
    }    

    private async Task ShiftTilesDown(List<Tile> horizontalMatch)
    {
        int matchCount = horizontalMatch.Count;
        if (matchCount != 0)
        {
            int x = horizontalMatch[0].GridPos.X;

            List<Vector3> originalPos = new List<Vector3>();
            for (int i = 0; i < matchCount; i++)
            {
                originalPos.Add(horizontalMatch[i].transform.position);
                Vector3 offset = new Vector3(0, tileSize.y + (i * tileSize.y), 0);
                horizontalMatch[i].transform.position = tiles[x, _columnSize - 1].transform.position + offset;

                Sprite randomSprite = characters[Random.Range(0, characters.Count)];
                horizontalMatch[i].GetComponent<SpriteRenderer>().sprite = randomSprite;
            }

            int yStart = horizontalMatch[0].GridPos.Y;
            int shiftingTiles = _columnSize - yStart;
            int tilesAbove = shiftingTiles - matchCount;
            int currentIndex = 0;
            int matchIndex = 0;
            var sequence = DOTween.Sequence();

            for (int y = yStart; y < _columnSize; y++)
            {
                if (currentIndex < tilesAbove)
                {
                    originalPos.Add(tiles[x, y + matchCount].transform.position);
                    sequence.Join(tiles[x, y + matchCount].transform.DOMove(originalPos[currentIndex], 0.25f));
                    tiles[x, y + matchCount].GetComponent<Tile>().GridPos = new Tile.GridPosition(x, y, true);
                    RefreshBoard(tiles[x, y + matchCount].GetComponent<Tile>().GridPos, tiles[x, y + matchCount]);
                }
                else
                {
                    sequence.Join(horizontalMatch[matchIndex].transform.DOMove(originalPos[currentIndex], 0.25f));
                    horizontalMatch[matchIndex].GridPos = new Tile.GridPosition(x, y, true);
                    RefreshBoard(horizontalMatch[matchIndex].GridPos, horizontalMatch[matchIndex].gameObject);

                    matchIndex++;
                }

                currentIndex++;
            }

            await sequence.Play().AsyncWaitForCompletion();
        }
    }

    public Tile GetTile(Tile.GridPosition grid)
    {
        return tiles[grid.X, grid.Y].GetComponent<Tile>();
    }

    public void RefreshBoard(Tile.GridPosition grid, GameObject tile)
    {
        tiles[grid.X, grid.Y] = tile;
    }
}
