using TMPro;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HexMazeGenerator : MonoBehaviour
{
    // ... (all your existing fields remain the same)
    [Tooltip("A Transform used to define the area of the maze via its scale.")]
    [SerializeField] private Transform hexMazeTransform;
    [Tooltip("The actual container for the generated hex cells. Should not be scaled.")]
    [SerializeField] private Transform mazeContainer;

    [SerializeField] private GridType gridType;
    [SerializeField] private GameObject wall;
    [SerializeField] private TextMeshPro numberTextPrefab;

    [Header("Hex Settings")]
    [SerializeField] private float hexSize = 1f;
    [Tooltip("If true, calculates rows/columns to fit the hexMazeTransform. If false, uses the manual values below.")]
    [SerializeField] private bool fitToTransform = true;

    [Header("Manual Grid Size")]
    [SerializeField] private int rows;
    [SerializeField] private int columns;
    [SerializeField]
    [Range(0f,1f)]
    private float loopsPercent = 0f;

    [Header("Generation Settings")]
    [Tooltip("The delay between steps when visualizing the maze creation.")]
    [SerializeField] private float waitTime = 0.01f;

    private Dictionary<(int, int), HexCell> hexGridDict = new();
    private List<HexCell> hexCells = new();

    private int currentRows = 0;
    private int currentColumns = 0;

    public enum GridType { FlatTop, PointyTop }
    public class HexCell
    {
        public int X;
        public int Z;
        public HexCell[] neighbors = new HexCell[6];
        public Transform[] walls = new Transform[6];
        public bool Visited = false;

        public HexCell(int x, int z)
        {
            X = x;
            Z = z;
        }
    }

    // --- YOUR EXISTING METHODS (CreateGrid, PopulateNeighbours, etc.) ---
    // They are correct and don't need changes. For brevity, I'll place the new
    // methods first and the existing code after.

    /// <summary>
    /// Call this to generate the grid, create the maze, and then add loops.
    /// </summary>
    /// <param name="loopPercentage">A value from 0.0 to 1.0. 0.1 means 10% of remaining walls will be removed.</param>
    /// <param name="visualize">Should the maze generation be animated?</param>
    public void GenerateWithPrims(bool visualize = true)
    {
        StopAllCoroutines();
        CreateGrid();
        StartCoroutine(ProcessMazeGeneration(loopsPercent, visualize));
    }

    private IEnumerator ProcessMazeGeneration(float loopPercentage, bool visualize)
    {
        yield return StartCoroutine(CreateMaze(visualize));
        if(loopPercentage > 0)
        CreateLoops(loopPercentage);
    }

    /// <summary>
    /// Removes a percentage of the remaining walls to create loops in the maze.
    /// </summary>
    /// <param name="percentageToRemove">A value from 0.0 to 1.0.</param>
    public void CreateLoops(float percentageToRemove)
    {
        // 1. Find all remaining walls between cells
        List<(HexCell, HexCell)> remainingWalls = new List<(HexCell, HexCell)>();
        foreach (var cell in hexCells)
        {
            for (int i = 0; i < cell.neighbors.Length; i++)
            {
                var neighbor = cell.neighbors[i];
                // Check if a wall exists and the neighbor is valid
                if (neighbor != null && cell.walls[i] != null)
                {
                    // To avoid adding each wall twice, only add it from the cell with the smaller coordinate.
                    if (cell.X < neighbor.X || (cell.X == neighbor.X && cell.Z < neighbor.Z))
                    {
                        remainingWalls.Add((cell, neighbor));
                    }
                }
            }
        }

        // 2. Shuffle the list of walls for randomness
        for (int i = 0; i < remainingWalls.Count; i++)
        {
            var temp = remainingWalls[i];
            int randomIndex = Random.Range(i, remainingWalls.Count);
            remainingWalls[i] = remainingWalls[randomIndex];
            remainingWalls[randomIndex] = temp;
        }

        // 3. Remove a percentage of the shuffled walls
        int wallsToRemoveCount = Mathf.FloorToInt(remainingWalls.Count * percentageToRemove);
        for (int i = 0; i < wallsToRemoveCount; i++)
        {
            var wallPair = remainingWalls[i];
            RemoveWall(wallPair.Item1, wallPair.Item2);
        }
    }


    // --- THE REST OF YOUR SCRIPT (UNCHANGED BUT INCLUDED FOR COMPLETENESS) ---

    public IEnumerator CreateMaze(bool visualize = false)
    {
        if (hexCells.Count == 0)
        {
            Debug.LogWarning("Grid has not been created yet. Cannot create maze.");
            yield break;
        }

        Stack<HexCell> stack = new();
        var startCell = hexCells[Random.Range(0, hexCells.Count)];
        startCell.Visited = true;
        stack.Push(startCell);

        while (stack.Count > 0)
        {
            var currentCell = stack.Peek();
            List<HexCell> unvisitedNeighbours = new List<HexCell>();
            foreach (var neighbor in currentCell.neighbors)
            {
                if (neighbor != null && !neighbor.Visited)
                {
                    unvisitedNeighbours.Add(neighbor);
                }
            }

            if (unvisitedNeighbours.Count > 0)
            {
                var chosenNeighbour = unvisitedNeighbours[Random.Range(0, unvisitedNeighbours.Count)];
                RemoveWall(currentCell, chosenNeighbour);
                chosenNeighbour.Visited = true;
                stack.Push(chosenNeighbour);
                if (visualize && waitTime > 0)
                    yield return new WaitForSeconds(waitTime);
            }
            else
            {
                stack.Pop();
            }
        }
    }

    private void RemoveWall(HexCell current, HexCell neighbour)
    {
        if (current == null || neighbour == null) return;

        for (int i = 0; i < current.neighbors.Length; i++)
        {
            if (current.neighbors[i] == neighbour)
            {
                int oppositeWallIndex = (i + 3) % 6;

                if (current.walls[i] != null)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying) DestroyImmediate(current.walls[i].gameObject);
                    else Destroy(current.walls[i].gameObject);
#else
                    Destroy(current.walls[i].gameObject);
#endif
                }

                if (neighbour.walls[oppositeWallIndex] != null)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying) DestroyImmediate(neighbour.walls[oppositeWallIndex].gameObject);
                    else Destroy(neighbour.walls[oppositeWallIndex].gameObject);
#else
                    Destroy(neighbour.walls[oppositeWallIndex].gameObject);
#endif
                }
                return;
            }
        }
    }

    public void CreateGrid()
    {
        ClearGrid();
        float xSpacing, zSpacing;
        if (gridType == GridType.FlatTop)
        {
            xSpacing = 1.5f * hexSize;
            zSpacing = Mathf.Sqrt(3) * hexSize;
        }
        else
        { // PointyTop
            xSpacing = Mathf.Sqrt(3) * hexSize;
            zSpacing = 1.5f * hexSize;
        }
        currentColumns = fitToTransform ? Mathf.FloorToInt(hexMazeTransform.lossyScale.x * 10f / xSpacing) : columns;
        currentRows = fitToTransform ? Mathf.FloorToInt(hexMazeTransform.lossyScale.z * 10f / zSpacing) : rows;
        float gridWidth = (currentColumns - 1) * xSpacing;
        float gridHeight = (currentRows - 1) * zSpacing;
        Vector3 startPos = new Vector3(-gridWidth / 2f, 0f, -gridHeight / 2f);

        for (int i = 0; i < currentColumns; i++)
        {
            for (int j = 0; j < currentRows; j++)
            {
                HexCell newHexCell = new HexCell(i, j);
                hexGridDict.Add((i, j), newHexCell);
                hexCells.Add(newHexCell);
                GameObject cellGO = CreateCellGameObject(newHexCell);
                float xOffset, zOffset;
                if (gridType == GridType.FlatTop)
                {
                    xOffset = i * xSpacing;
                    zOffset = j * zSpacing + (i % 2 != 0 ? zSpacing / 2f : 0f);
                }
                else
                { // PointyTop
                    xOffset = i * xSpacing + (j % 2 != 0 ? xSpacing / 2f : 0f);
                    zOffset = j * zSpacing;
                }
                cellGO.transform.localPosition = new Vector3(xOffset, 0f, zOffset) + startPos;
            }
        }
        PopulateNeighbours();
    }

    private void PopulateNeighbours()
    {
        foreach (HexCell cell in hexCells)
        {
            for (int i = 0; i < 6; i++)
            {
                int neighbourX = cell.X;
                int neighbourZ = cell.Z;
                if (gridType == GridType.FlatTop)
                {
                    bool isOddCol = (cell.X % 2 != 0);
                    switch (i)
                    {
                        case 0: neighbourX++; neighbourZ = isOddCol ? cell.Z : cell.Z - 1; break;
                        case 1: neighbourX++; neighbourZ = isOddCol ? cell.Z + 1 : cell.Z; break;
                        case 2: neighbourZ++; break;
                        case 3: neighbourX--; neighbourZ = isOddCol ? cell.Z + 1 : cell.Z; break;
                        case 4: neighbourX--; neighbourZ = isOddCol ? cell.Z : cell.Z - 1; break;
                        case 5: neighbourZ--; break;
                    }
                }
                else // PointyTop
                {
                    bool isOddRow = (cell.Z % 2 != 0);
                    switch (i)
                    {
                        case 0: neighbourX++; break;
                        case 1: neighbourX = isOddRow ? cell.X + 1 : cell.X; neighbourZ++; break;
                        case 2: neighbourX = isOddRow ? cell.X : cell.X - 1; neighbourZ++; break;
                        case 3: neighbourX--; break;
                        case 4: neighbourX = isOddRow ? cell.X : cell.X - 1; neighbourZ--; break;
                        case 5: neighbourX = isOddRow ? cell.X + 1 : cell.X; neighbourZ--; break;
                    }
                }
                if (hexGridDict.TryGetValue((neighbourX, neighbourZ), out HexCell neighbour))
                {
                    cell.neighbors[i] = neighbour;
                }
            }
        }
    }

    public GameObject CreateCellGameObject(HexCell cellData)
    {
        GameObject hexCellGO = new("Hex cell " + cellData.X + "," + cellData.Z);
        hexCellGO.transform.SetParent(mazeContainer);
        hexCellGO.transform.localPosition = Vector3.zero;
        TextMeshPro numberText = Instantiate(numberTextPrefab, hexCellGO.transform);
        numberText.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        numberText.transform.localPosition += Vector3.up * 0.01f;
        numberText.text = cellData.X + "," + cellData.Z;
        float bias = gridType == GridType.FlatTop ? 30f : 0f;
        float radius = hexSize * Mathf.Sqrt(3) / 2f;
        for (int i = 0; i < 6; i++)
        {
            var wallInstance = Instantiate(this.wall, hexCellGO.transform);
            wallInstance.transform.localScale = new Vector3(wallInstance.transform.localScale.x, wallInstance.transform.localScale.y, hexSize);
            float yAngle = (i * 60f - bias) * Mathf.Deg2Rad;
            float x = radius * Mathf.Cos(yAngle);
            float z = radius * Mathf.Sin(yAngle);
            wallInstance.transform.SetLocalPositionAndRotation(new Vector3(x, 0f, z), Quaternion.Euler(0f, -i * 60f + bias, 0f));
            cellData.walls[i] = wallInstance.transform;
        }
        return hexCellGO;
    }

    public void ClearGrid()
    {
        StopAllCoroutines();
        hexGridDict.Clear();
        hexCells.Clear();
        for (int i = mazeContainer.childCount - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(mazeContainer.GetChild(i).gameObject);
            else Destroy(mazeContainer.GetChild(i).gameObject);
#else
            Destroy(mazeContainer.GetChild(i).gameObject);
#endif
        }
    }
}