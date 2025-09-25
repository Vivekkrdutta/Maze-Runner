using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HexMazeGenerator))]
public class HexMazeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (target is not HexMazeGenerator hexMazeGenerator) return;

        if (GUILayout.Button("Hex Cell"))
            hexMazeGenerator.CreateCellGameObject(new HexMazeGenerator.HexCell(0,0));

        if (GUILayout.Button("Grid"))
        {
            hexMazeGenerator.CreateGrid();
        }

        if(GUILayout.Button("Maze with Prims"))
        {
            hexMazeGenerator.GenerateWithPrims(visualize: EditorApplication.isPlaying);
        }

        if (GUILayout.Button("Clear"))
        {
            hexMazeGenerator.ClearGrid();
        }
    }
}
