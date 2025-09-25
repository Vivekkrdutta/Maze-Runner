using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class MazeGenerator : MonoBehaviour
{
    [SerializeField] private Transform mazeTransform;
    [SerializeField] private Cell cellPrefab;

    [SerializeField] private float wallBreakVisualizationTime = 0.05f;

    [SerializeField]
    [Range(0f, 1f)]
    private float horizontalBiasing = 0.5f;

    [Tooltip("Determines the Number of cells in the X and the Y axis")]
    [SerializeField] private Vector2Int dimension;

    private float cellWidth, cellHeight;
    private Cell[,] cellsArray;

    public void GenerateGrid()
    {
        ClearMaze();

        cellWidth = mazeTransform.localScale.x * 10f / dimension.x;
        cellHeight = mazeTransform.localScale.z * 10f / dimension.y;

        float biasX = -1 * dimension.x / 2 * cellWidth;
        float biasZ = -1 * dimension.y / 2 * cellHeight;

        cellsArray = new Cell[dimension.x, dimension.y];

        for(int x = 0; x < dimension.x; x++)
        {
            for(int z = 0; z < dimension.y; z++)
            {
                var position = new Vector3(x * cellWidth + biasX,0.01f, z * cellHeight +  biasZ);
                var cell = Instantiate(cellPrefab);
                cell.transform.localScale = new Vector3(cellWidth, 1f, cellHeight);
                cell.transform.position = position + mazeTransform.position;
                cell.SetIndex((x,z));
                cellsArray[x,z] = cell;
                cell.transform.SetParent(mazeTransform);
            }
        }

        // Setup the neighbours of each of the cells
        for(int x = 0; x < dimension.x; x++)
        {
            for(int z = 0;z < dimension.y; z++)
            {
                if (x - 1 >= 0) cellsArray[x, z].WestNeighbour = cellsArray[x - 1, z];
                if (x+1 < dimension.x) cellsArray[x,z].EastNeighbour = cellsArray[x+1, z];
                if (z - 1 >= 0) cellsArray[x, z].SouthNeighbour = cellsArray[x, z - 1];
                if(z+1 < dimension.y) cellsArray[x,z].NorthNeighbour = cellsArray[x, z + 1];
            }
        }
    }

    public IEnumerator CreateMazePrims(bool visualize = false)
    {
        if (cellsArray == null || cellsArray.Length == 0) yield break;

        // for the walls.
        int randX = Random.Range(0, dimension.x);
        int randY = Random.Range(0, dimension.y);

        var startCell = cellsArray[randX, randY];
        startCell.Visited = true;
        Stack<Cell> stack = new();
        stack.Push(startCell);

        while (stack.Count > 0)
        {
            var currentCell = stack.Pop();

            Cell chosenNeighbour = null;

            List<Cell> unvisited = new List<Cell>();
            if (currentCell.NorthNeighbour && !currentCell.NorthNeighbour.Visited) unvisited.Add(currentCell.NorthNeighbour);
            if (currentCell.SouthNeighbour && !currentCell.SouthNeighbour.Visited) unvisited.Add(currentCell.SouthNeighbour);
            if (currentCell.EastNeighbour && !currentCell.EastNeighbour.Visited) unvisited.Add(currentCell.EastNeighbour);
            if (currentCell.WestNeighbour && !currentCell.WestNeighbour.Visited) unvisited.Add(currentCell.WestNeighbour);

            if (unvisited.Count > 0)
            {
                stack.Push(currentCell);

                // check biasing
                if (Random.Range(0f, 1f) <= horizontalBiasing)
                {
                    // chose the horizontal neighbour : east or west
                    bool choseWest = Random.Range(0f, 1f) <= 0.5f;
                    if (choseWest && currentCell.WestNeighbour && !currentCell.WestNeighbour.Visited)
                    {
                        chosenNeighbour = currentCell.WestNeighbour;
                    }
                    else if (currentCell.EastNeighbour && !currentCell.EastNeighbour.Visited)
                    {
                        chosenNeighbour = currentCell.EastNeighbour;
                    }
                }
                else
                {
                    bool choseNorth = Random.Range(0f, 1f) <= 0.5f;
                    if (choseNorth && currentCell.NorthNeighbour && !currentCell.NorthNeighbour.Visited)
                    {
                        chosenNeighbour = currentCell.NorthNeighbour;
                    }
                    else if (currentCell.SouthNeighbour && !currentCell.SouthNeighbour.Visited)
                    {
                        chosenNeighbour = currentCell.SouthNeighbour;
                    }
                }

                if (chosenNeighbour == null) chosenNeighbour = unvisited[Random.Range(0, unvisited.Count)];

                chosenNeighbour.Visited = true;
                stack.Push(chosenNeighbour);

                // break the wall.
                if (chosenNeighbour == currentCell.NorthNeighbour) currentCell.NorthWallActive = false;
                else if (chosenNeighbour == currentCell.SouthNeighbour) chosenNeighbour.NorthWallActive = false;
                else if (chosenNeighbour == currentCell.EastNeighbour) currentCell.EastWallActive = false;
                else chosenNeighbour.EastWallActive = false;

                if(visualize)
                    yield return new WaitForSecondsRealtime(wallBreakVisualizationTime);
            }
        }
        yield return null;
    }

    public IEnumerator CreateMazeKrushkals(bool visualize)
    {
        if(cellsArray == null || cellsArray.Length == 0) yield break;

        // create a list of walls, keeping record of the cells that it devides.
        Dictionary<Transform, (Cell cell1, Cell cell2)> walls = new();
        
        foreach (var cell in cellsArray)
        {
            if (cell.NorthNeighbour) walls.Add(cell.Walls.North, (cell, cell.NorthNeighbour));
            if(cell.EastNeighbour) walls.Add(cell.Walls.East, (cell, cell.EastNeighbour));

            // everyone parent of itself primarily
            cell.Parent = cell;
        }

        int n = walls.Keys.Count;
        int[] randNums = Enumerable.Range(0, n).ToArray();
        for (int i = n - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = randNums[i];
            randNums[i] = randNums[j];
            randNums[j] = temp;
        }


        foreach (int i in randNums)
        {
            var wall = walls.Keys.ElementAt(i);
            var fCell = walls[wall].cell1;
            var sCell = walls[wall].cell2;

            if(fCell.Parent != sCell.Parent) // under different sets // This comparision runs deep down to the root parent.
            {
                // break the wall.
                if (fCell.NorthNeighbour == sCell) fCell.NorthWallActive = false;
                else if (fCell.EastNeighbour == sCell) fCell.EastWallActive = false;
                else if (sCell.NorthNeighbour == fCell) sCell.NorthWallActive = false;
                else if (sCell.EastNeighbour == fCell) sCell.EastWallActive= false;

                // join under same hood.
                Cell root1 = fCell.Parent;
                Cell root2 = sCell.Parent;
                root2.Parent = root1;

                if (visualize) yield return new WaitForSecondsRealtime(wallBreakVisualizationTime);
            }
;       }

        yield return null;
    }

    public void ClearMaze()
    {
        for (int i = mazeTransform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(mazeTransform.GetChild(i).gameObject);
        }

        cellsArray = null;
    }

}
