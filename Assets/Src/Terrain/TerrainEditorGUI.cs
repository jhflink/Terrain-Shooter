using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// All the shared gui IDs, should be loaded from some data in the future
/// </summary>
public static class TerrainEditorGUI_IDS {
    public const string TopBar = "TopBar";
    public const string TopBarLable = "TopBarLable";
    public const string SaveButton = "Save";
    public const string LoadButton = "Load";
    public const string RedoButton = "Redo";
    public const string UndoButton = "Undo";

    public const string GenerateButton = "Generate";
    public const string FlattenButton = "Flatten";
    public const string GenerateButtonDesc = "Generate Terrain";
    public const string FlattenButtonDesc = "Flatten Terrain";

    public const string ToolsList = "Tools";
    public const string MarkedBox = "MarkedBox";

    public const string MarkedCellsLabel = "Marked Cells";
    public const string MarkedCellsSizeLabel = "Marked Cells Size";
    public const string MarkedCellIndexLabel = "Marked Cell Index";
    public const string MarkedCellHeightLabel = "Marked Cell Height";

    public const string MarkedCellsLabelDesc = "Marked Cells: ";
    public const string MarkedCellsSizeLabelDesc = "Size: ";
    public const string MarkedCellIndexLabelDesc = "Cell Index: ";
    public const string MarkedCellHeightLabelDesc = "Cell Height: ";

    public const string EnterHeightLabel = "Enter Height";
    public const string EnterHeightLabelDesc = "Custom Height: ";
    public const string EnterHeightInput = "Marked Cell Height Input";

    public const string RaiseButton = "Raise";
    public const string LowerButton = "Lower";

    public const string FlattenMarkedCellsButton = "FlattenMarkedCells";
    public const string FlattenMarkedCellsButtonDesc = "Flatten";

    public const string SpawnCreatureButton = "SpawnCreature";
    public const string SpawnCreateButtonDesc = "Creature";

    public const string SculptBox = "SculptBox";
    public const string SculptSizeLabel = "Sculpt Size";
    public const string SculptSizeLabelDesc = "Sculpt Size: ";
    public const string SculptSizeCellsLabel = "Sculpt Siz Cells";

    public const string IncreaseSculptSizeButton = "+";
    public const string DecreaseSculptSizeButton = "-";

    public const string IncreaseDecreaseToolbar = "Increase";
}

/// <summary>
/// Class to render our gui
/// </summary>
public class TerrainEditorGUI : MonoBehaviour {

    // top bar height and position
    public Vector2 topBarPosition = new Vector2(-2, -2);
    public Vector2 topBarSize = new Vector2(4, 47.0f);

    // start position of right most button on top bar
    public Vector2 rightButtonsStartPosition = new Vector2(110, 5);

    // marked cell box position and size
    public Vector2 markedBoxPosition = new Vector2(270, 55);
    public Vector2 markedBoxSize = new Vector2(260, 260);

    // sculpt box position and size
    public Vector2 sculptBoxPosition = new Vector2(270, 55);
    public Vector2 sculptBoxSize = new Vector2(260, 210);

    // if this is true we will refresh the gui rects for this frame
    private bool _refreshGuiRects = false;

    private void Start() {
        // update with screen width offset
        topBarSize.Set(Screen.width - topBarSize.x, topBarSize.y);
        rightButtonsStartPosition.Set(Screen.width - rightButtonsStartPosition.x, rightButtonsStartPosition.y);
        markedBoxPosition.Set(Screen.width - markedBoxPosition.x, markedBoxPosition.y);
        sculptBoxPosition.Set(Screen.width - sculptBoxPosition.x, sculptBoxPosition.y);
    }

    private void OnValidate() {
        // if we get a change in the inspector update the gui rects
        // so we can dynamically tweak the positions in the inspector while the game is running
        _refreshGuiRects = true;
    }

    /// <summary>
    /// Render the gui
    /// </summary>
    /// <param name="dataContainer">the data that all the gui is based on</param>
    /// <param name="currentEditorTool">the current editor tool</param>
    /// <returns>what editor tool that should be active</returns>
    public TerrainEditorGizmo.EditorTools Render(TerrainEditorGUIDataContainer dataContainer,
                                                 TerrainEditorGizmo.EditorTools currentEditorTool) {
        // force refresh of rects and positions 
        if (_refreshGuiRects)  {
            dataContainer.RefreshGUIRects?.Invoke();
            _refreshGuiRects = false;
        }

        GUIStyle style = GUI.skin.GetStyle("label");
        style.fontSize = 25;

        // Top Bar
        GUI.Box(dataContainer.FetchGuiRect(TerrainEditorGUI_IDS.TopBar, topBarPosition.x, topBarPosition.y, topBarSize.x, topBarSize.y), "");

        // Label to display hoover cell index and height
        string cellIndexString = dataContainer.HooverCell == null ? "No Cell Selected" : "Cell Index: " + dataContainer.HooverCell.Index.ToString() + " Height: " + TerrainInstanceCellDataContainer.HeightOfCell(dataContainer.HooverCell).ToString("0.000");
        GUI.Label(dataContainer.FetchGuiRect(TerrainEditorGUI_IDS.TopBarLable, topBarPosition.x + 17, topBarPosition.y + 7, 1000, topBarSize.y), cellIndexString);

        GUI.skin.button.fontSize = 20;
        // Button to save terrain data to disk
        if (GUI.Button(dataContainer.FetchGuiRect(TerrainEditorGUI_IDS.SaveButton, rightButtonsStartPosition.x - 110.0f, rightButtonsStartPosition.y, 100, 30), TerrainEditorGUI_IDS.SaveButton))
            dataContainer.callbacks[TerrainEditorGUI_IDS.SaveButton]?.Invoke();

        // Button to load terrain data from disk
        if (GUI.Button(dataContainer.FetchGuiRect(TerrainEditorGUI_IDS.LoadButton, rightButtonsStartPosition.x, rightButtonsStartPosition.y, 100, 30), TerrainEditorGUI_IDS.LoadButton))
                dataContainer.callbacks[TerrainEditorGUI_IDS.LoadButton]?.Invoke();

        // Button to undo last edit to the terrain
        //if (GUI.Button(dataContainer.FetchGuiRect(TerrainEditorGUI_IDS.UndoButton, rightButtonsStartPosition.x - 220.0f, rightButtonsStartPosition.y, 100, 30), TerrainEditorGUI_IDS.UndoButton))
          //  dataContainer.callbacks[TerrainEditorGUI_IDS.UndoButton]?.Invoke();

        // Button to redo previous edit to the terrain
        //if (GUI.Button(dataContainer.FetchGuiRect(TerrainEditorGUI_IDS.RedoButton, rightButtonsStartPosition.x - 330.0f, rightButtonsStartPosition.y, 100, 30), TerrainEditorGUI_IDS.RedoButton))
          //  dataContainer.callbacks[TerrainEditorGUI_IDS.RedoButton]?.Invoke();

        // Button to generate a new terrain
        if (GUI.Button(dataContainer.FetchGuiRect(TerrainEditorGUI_IDS.GenerateButton, Screen.width * 0.5f - 185, 5, 180, 30), TerrainEditorGUI_IDS.GenerateButtonDesc))
            dataContainer.callbacks[TerrainEditorGUI_IDS.GenerateButton]?.Invoke();

        // Button to flatten the terrain
        if (GUI.Button(dataContainer.FetchGuiRect(TerrainEditorGUI_IDS.FlattenButton, Screen.width * 0.5f + 5, 5, 160, 30), TerrainEditorGUI_IDS.FlattenButtonDesc))
            dataContainer.callbacks[TerrainEditorGUI_IDS.FlattenButton]?.Invoke();

        GUI.skin.button.fontSize = 25;

        // Selection grid with button to choose between Selec / Sculpt / ... tools
        currentEditorTool = (TerrainEditorGizmo.EditorTools)(dataContainer.ToolbarIndex = GUI.SelectionGrid(dataContainer.FetchGuiRect(TerrainEditorGUI_IDS.ToolsList, 10, 55, 150, 150), dataContainer.ToolbarIndex, dataContainer.ToolbarStrings, 1));

        // Select Tool
        if (dataContainer.SelectedCell != null && dataContainer.SelectedCells != null && dataContainer.SelectedCells.Count > 0 && currentEditorTool == TerrainEditorGizmo.EditorTools.Select) {

            // Box 
            GUI.Box(dataContainer.FetchGuiRect(TerrainEditorGUI_IDS.MarkedBox, markedBoxPosition.x, markedBoxPosition.y, markedBoxSize.x, markedBoxSize.y), "");

            // Show labels if only one cell is selected
            if (dataContainer.SelectedCells.Count > 1) {
                GUI.Label(dataContainer.FetchGuiRect(TerrainEditorGUI_IDS.MarkedCellsLabel, markedBoxPosition.x + 15.0f, markedBoxPosition.y + 5.0f, 300, 100), TerrainEditorGUI_IDS.MarkedCellsLabelDesc + dataContainer.SelectedCells.Count);
                GUI.Label(dataContainer.FetchGuiRect(TerrainEditorGUI_IDS.MarkedCellsSizeLabel, markedBoxPosition.x + 15.0f, markedBoxPosition.y + 45.0f, 300, 100), TerrainEditorGUI_IDS.MarkedCellsSizeLabelDesc + dataContainer.SelectedCellsSize.x + "x" + dataContainer.SelectedCellsSize.y);
            }
            // Show labels if multiple cells are selected
            else {
                GUI.Label(dataContainer.FetchGuiRect(TerrainEditorGUI_IDS.MarkedCellIndexLabel, markedBoxPosition.x + 15.0f, markedBoxPosition.y + 5.0f, 300, 100), TerrainEditorGUI_IDS.MarkedCellIndexLabelDesc + dataContainer.SelectedCell.Index);
                GUI.Label(dataContainer.FetchGuiRect(TerrainEditorGUI_IDS.MarkedCellHeightLabel, markedBoxPosition.x + 15.0f, markedBoxPosition.y + 45.0f, 300, 100), TerrainEditorGUI_IDS.MarkedCellHeightLabelDesc + TerrainInstanceCellDataContainer.HeightOfCell(dataContainer.SelectedCell).ToString("0.000"));
            }

            // Enter a custom height value to set height on selected cells
            GUI.Label(dataContainer.FetchGuiRect(TerrainEditorGUI_IDS.EnterHeightLabel, markedBoxPosition.x + 15.0f, markedBoxPosition.y + 85.0f, 300, 100), TerrainEditorGUI_IDS.EnterHeightLabelDesc);
            string newCellHeightString = GUI.TextField(dataContainer.FetchGuiRect(TerrainEditorGUI_IDS.EnterHeightInput, markedBoxPosition.x + 195.0f, markedBoxPosition.y + 85.0f, 50, 40), dataContainer.MarkedCellHeightString);
            if (!string.Equals(newCellHeightString, dataContainer.MarkedCellHeightString)) {

                float newHeight = 0.0f;

                if (float.TryParse(newCellHeightString, out newHeight)) {
                    dataContainer.CustomHeight = newHeight;
                    dataContainer.callbacks[TerrainEditorGUI_IDS.EnterHeightInput]?.Invoke();
                }

                dataContainer.MarkedCellHeightString = newCellHeightString;
            }

            // Button to raise selected cells
            if (GUI.Button(dataContainer.FetchGuiRect(TerrainEditorGUI_IDS.RaiseButton, markedBoxPosition.x + 15.0f, markedBoxPosition.y + 145.0f, 110, 40), TerrainEditorGUI_IDS.RaiseButton)) {

                dataContainer.callbacks[TerrainEditorGUI_IDS.RaiseButton]?.Invoke();
            }

            // Button to lower selected cells
            if (GUI.Button(dataContainer.FetchGuiRect(TerrainEditorGUI_IDS.LowerButton, markedBoxPosition.x + 135.0f, markedBoxPosition.y + 145.0f, 110, 40), TerrainEditorGUI_IDS.LowerButton)) {
                dataContainer.callbacks[TerrainEditorGUI_IDS.LowerButton]?.Invoke();
            }

            // Button to flatten selected cells
            if (GUI.Button(dataContainer.FetchGuiRect(TerrainEditorGUI_IDS.FlattenMarkedCellsButton, markedBoxPosition.x + 15.0f, markedBoxPosition.y + 205.0f, 230, 40), TerrainEditorGUI_IDS.FlattenButtonDesc)) {
                dataContainer.callbacks[TerrainEditorGUI_IDS.FlattenMarkedCellsButton]?.Invoke();
            }

            // Button to spawn creatures
            if (GUI.Button(dataContainer.FetchGuiRect(TerrainEditorGUI_IDS.SpawnCreatureButton, markedBoxPosition.x + 15.0f, markedBoxPosition.y + 265.0f, 230, 40), TerrainEditorGUI_IDS.SpawnCreateButtonDesc))
            {
                dataContainer.callbacks[TerrainEditorGUI_IDS.SpawnCreatureButton]?.Invoke();
            }
        }
        // Sculpt Tool
        else if (currentEditorTool == TerrainEditorGizmo.EditorTools.Sculpt) {
            // Box for sculp gui
            GUI.Box(dataContainer.FetchGuiRect(TerrainEditorGUI_IDS.SculptBox, sculptBoxPosition.x, sculptBoxPosition.y, sculptBoxSize.x ,sculptBoxSize.y), "");

            // Label for sculpt size
            GUI.Label(dataContainer.FetchGuiRect(TerrainEditorGUI_IDS.SculptSizeLabel, sculptBoxPosition.x + 15.0f, sculptBoxPosition.y + 5.0f, 300, 100), TerrainEditorGUI_IDS.SculptSizeLabelDesc);

            // Label for setting sculpt size
            GUI.Label(dataContainer.FetchGuiRect(TerrainEditorGUI_IDS.SculptSizeCellsLabel, sculptBoxPosition.x + 160.0f, sculptBoxPosition.y + 5.0f, 300, 100), dataContainer.GridSculptSize + "x" + dataContainer.GridSculptSize);

            // increase sculpt size
            if (GUI.Button(dataContainer.FetchGuiRect(TerrainEditorGUI_IDS.IncreaseSculptSizeButton, sculptBoxPosition.x + 15.0f, sculptBoxPosition.y + 45.0f, 110, 40), TerrainEditorGUI_IDS.IncreaseSculptSizeButton)) {
                dataContainer.SculptSize++;
            }

            // decrease sculpt size
            if (GUI.Button(dataContainer.FetchGuiRect(TerrainEditorGUI_IDS.DecreaseSculptSizeButton, sculptBoxPosition.x + 135.0f, sculptBoxPosition.y + 45.0f, 110, 40), TerrainEditorGUI_IDS.DecreaseSculptSizeButton)) {
                dataContainer.SculptSize--;
            }

            // cap sculpt size
            if (dataContainer.SculptSize < 1)
                dataContainer.SculptSize = 1;

            // selection buttons to either increase or decrease height
            dataContainer.ToolbarIncreaseInt = GUI.SelectionGrid(dataContainer.FetchGuiRect(TerrainEditorGUI_IDS.IncreaseDecreaseToolbar, sculptBoxPosition.x + 15.0f, sculptBoxPosition.y+90.0f, 230, 100), dataContainer.ToolbarIncreaseInt, dataContainer.ToolbarIncreaseStrings, 1);
        }

        // return same or new editor tool to set as active tool
        return currentEditorTool;
    }
}
