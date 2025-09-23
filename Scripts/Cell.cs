using UnityEngine;

public class Cell : MonoBehaviour
{
    [SerializeField] private Transform eastWall;
    [SerializeField] private Transform northWall;

    private Cell northNeighbour;
    private Cell southNeighbour;
    private Cell eastNeighbour;
    private Cell westNeighbour;
    private Cell parent;

    public Cell NorthNeighbour { get => northNeighbour; set=> northNeighbour = value; }
    public Cell SouthNeighbour { get=> southNeighbour; set=> southNeighbour = value; }
    public Cell EastNeighbour { get => eastNeighbour; set=> eastNeighbour = value; }
    public Cell WestNeighbour { get => westNeighbour; set => westNeighbour = value; }

    /// <summary>
    /// The parent of this particular cell, used in Krushkal's Algorithm
    /// </summary>
    public Cell Parent
    {
        get => FindParent(this);
        set
        {
            parent = value;
        }
    }

    public bool NorthWallActive {  set => northWall.gameObject.SetActive(value); }
    public bool EastWallActive {  set => eastWall.gameObject.SetActive(value); }

    private bool isVisited = false;
    public bool Visited { get=> isVisited; set => isVisited = value; }

    (int x, int z) indices;
    public void SetIndex((int x,int z) indices)=> this.indices = indices;
    public (int x, int z) GetIndices() => indices;
    public (Transform North, Transform East) Walls { get => (northWall, eastWall); }

    private static Cell FindParent(Cell cell)
    {
        if (cell.parent == null) return null;

        if (cell == cell.parent) return cell;

        var parent = FindParent(cell.parent);
        cell.parent = parent;
        return parent;
    }
}
