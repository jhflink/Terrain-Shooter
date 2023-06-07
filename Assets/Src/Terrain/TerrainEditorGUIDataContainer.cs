using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Container for various gui data
/// </summary>
public class TerrainEditorGUIDataContainer {
    // index to indicate what tool is selected from toolbar
    public int ToolbarIndex = 1;
    public string[] ToolbarStrings = { "Select", "Sculpt", };

    // size of sculpt tool
    public int SculptSize = 2;
    public int GridSculptSize => SculptSize * 2 - 1;

    // index for selection button to either increase or decrease height
    public int ToolbarIncreaseInt = 0;
    public string[] ToolbarIncreaseStrings = { "Increase", "Decrease" };

    // string for custom height input box
    public string MarkedCellHeightString = "0";

    // callback for fetching gui rects
    public System.Func<string, float, float, float, float, Rect> FetchGuiRect = null;

    // callback to refresh gui rects
    public System.Action RefreshGUIRects = null;

    // the hoovering cell
    public TerrainInstanceCellDataContainer HooverCell;

    // the selected cell
    public TerrainInstanceCellDataContainer SelectedCell;

    // the selected cells
    public List<(int, int)> SelectedCells;

    // size of selected cells
    public Vector2Int SelectedCellsSize;

    // custom height input
    public float CustomHeight = 0.0f;

    // callbacks bound to GUI IDs
    public Dictionary<string, System.Action> callbacks = new Dictionary<string, System.Action>();
}
