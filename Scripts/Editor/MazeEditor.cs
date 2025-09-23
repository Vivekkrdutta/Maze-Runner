using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MazeGenerator))]
public class MazeEditor : Editor // call function GenerateMaze() from MazeGenerator.
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        // Generate the Maze on a button click
        if(target is MazeGenerator generator)
        {
            if(GUILayout.Button("Generate Grid"))
                generator.GenerateGrid();

            if(GUILayout.Button("Generate Maze with 'Prims' algorithm"))
            {
                generator.StartCoroutine(generator.CreateMazePrims(visualize: EditorApplication.isPlaying));
            }

            if(GUILayout.Button("Generate Maze with 'Krushkal Algorithm'"))
            {
                generator.StartCoroutine(generator.CreateMazeKrushkals(visualize: EditorApplication.isPlaying));
            }

            if (GUILayout.Button("Delete Maze"))
                generator.ClearMaze();
        }
    }
}
