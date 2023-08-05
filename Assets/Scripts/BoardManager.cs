using System.Collections;
using System.Collections.Generic;
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

        Vector2 tileSize = tile.GetComponent<SpriteRenderer>().bounds.size;

        int tilesInRow = Mathf.FloorToInt(boardWidth / tileSize.x);
        int tilesInColumn = Mathf.FloorToInt(boardHeight / tileSize.y);

        // for centering the board
        float offset = (boardWidth - (tileSize.x * tilesInRow)) * 0.5f;

        CreateBoard(tilesInRow, tilesInColumn, tileSize, offset);
    }

    private void CreateBoard(int rowSize, int columnSize, Vector2 tileSize, float offset)
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

                newTile.GetComponent<Tile>().Position = tilePos;

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
}
