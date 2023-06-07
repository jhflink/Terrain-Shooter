using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data continer for necessary editor gizmo functionality
/// should be passed to the gizmo constructor
/// </summary>
public class TerrainEditorGizmoData {
    // terrain layers
    public TerrainInstance terrainInstanceFeedback = null;
    public TerrainInstance terrainInstanceMain = null;
    public TerrainInstance terrainInstanceSelected = null;

    // camera
    public TerrainEditorCamera editorCamera = null;

    public TerrainEditorGUIDataContainer guiDataContainer = null;

    public TerrainEditorGizmoData() { }
}

/// <summary>
/// Class to handle all logic and input for the various terrain editiing tools
/// </summary>
public class TerrainEditorGizmo {
    // editor tools enum
    public enum EditorTools {
        Select,
        Sculpt,
    }

    // editor tools enum and callbacks
    public EditorTools editorTool = EditorTools.Sculpt;
    private Dictionary<EditorTools, System.Action<TerrainInstanceCellDataContainer>> _editorToolsCallbacks = new Dictionary<EditorTools, System.Action<TerrainInstanceCellDataContainer>>();

    // active cell
    public TerrainInstanceCellDataContainer ActiveCell => cell;
    private TerrainInstanceCellDataContainer cell = null;

    // max raycast distance towards terrain instance collider
    private const int _raycastDistance = 500;

    // raycast out hit data
    private RaycastHit _raycastHit = new RaycastHit();

    // selected cells data
    public TerrainInstanceCellDataContainer SelectedCell => _selectedCell;
    private TerrainInstanceCellDataContainer _selectedCell = null;

    public List<(int StartIndexX, int StartIndexY)> SelectedCells => _selectedCells;
    private List<(int StartIndexX, int StartIndexY)> _selectedCells = null;

    public Vector2Int SelectedCellsSize;

    // edit cool down variables
    private float _editCooldown = 0.2f;
    private float _editCooldownCounter = -1.0f;

    // how much to edit the terrain per edit
    public static float editHeight = 0.2f;

    // height offset from the main terrain instance
    private float _feedbackLayerOffset = 0.05f;

    // mouse delta
    private Vector3 _mouseDelta = new Vector3();
    private Vector3 _lastMousePosition = Vector3.zero;

    // gizmo data
    private TerrainEditorGizmoData _gizmoData = null;

    public TerrainEditorGizmo(TerrainEditorGizmoData gizmoData) {

        _gizmoData = gizmoData;

        // set up editor tool update callbacks
        _editorToolsCallbacks.Add(EditorTools.Select, EditorToolSelectCallback);
        _editorToolsCallbacks.Add(EditorTools.Sculpt, EditorToolSculptCallback);

        // set last mouse positions
        _lastMousePosition = Input.mousePosition;
    }

    public void InputStateEditExit() {
        // hide the select visuals if we change state
        _gizmoData.terrainInstanceFeedback.Hide = true;
    }

    public void InputStateEditEnter() {
        // unhide feedback visuals
        _gizmoData.terrainInstanceFeedback.Hide = false;
    }

    /// <summary>
    /// Update the gizmos various input states
    /// </summary>
    /// <param name="currentState"></param>
    /// <returns>same or new state that should be active</returns>
    public TerrainEditorMain.EditorInputState InputStateEditUpdate(TerrainEditorMain.EditorInputState currentState) {

        // check if we're truing use any of the cameras input, if we do change to the camera state
        if (_gizmoData.editorCamera.isAnyCameraInputActive())
            return TerrainEditorMain.EditorInputState.Camera;

        // update mouse delta
        _mouseDelta = Input.mousePosition - _lastMousePosition;

        // create a ray from the camera through the mouse position
        Ray ray = _gizmoData.editorCamera.CameraRaw.ScreenPointToRay(Input.mousePosition);

        // raycast against the terrain collider
        bool raycastResult = _gizmoData.terrainInstanceMain.RaycastMeshCollider(ray, out _raycastHit, _raycastDistance);

        // if the raycast hit the collider, pick out the cell based on the local hit position
        cell = raycastResult ? _gizmoData.terrainInstanceMain.ClosestCellToPoint(_raycastHit.point) : null;

        // update the logic for current choosen editor tool
        _editorToolsCallbacks[editorTool](cell);

        // hide feedback cell if we dont have a cell
        //_gizmoData.terrainInstanceFeedback.Hide = cell == null;

        // update last mouse position
        _lastMousePosition = Input.mousePosition;

        return currentState;
    }

    /// <summary>
    /// Update logic for select tool
    /// </summary>
    /// <param name="newCell"></param>
    private void EditorToolSelectCallback(TerrainInstanceCellDataContainer newCell) {
        // if we're not hoovering any cell just return
        if (newCell == null) {
            // reset select visuals if we click outside the meshcollider 
            if (Input.GetMouseButtonDown(0)) {
                _selectedCell = null;
                _gizmoData.terrainInstanceSelected.Hide = true;
            }

            return;
        }

        // set position and assign cell height to feedback visuals
        _gizmoData.terrainInstanceFeedback.SetXZPosition(TerrainInstanceCellDataContainer.CalculateCellWorldPosition(newCell));
        _gizmoData.terrainInstanceMain.AssignCellDataToCell(_gizmoData.terrainInstanceFeedback, _gizmoData.terrainInstanceFeedback.CellFromIndex(0, 0), 0.01f, newCell);

        // mark first tile
        if (Input.GetMouseButtonDown(0) || (_selectedCell == null && Input.GetMouseButton(0))) {

            // reset gui marked cell height text
            _gizmoData.guiDataContainer.MarkedCellHeightString = "";

            // assign new cell
            _selectedCell = newCell;

            // unhide selected terrain visuals
            _gizmoData.terrainInstanceSelected.Hide = false;

            // set position to follow mouse pointer through the hoovered cell
            _gizmoData.terrainInstanceSelected.SetXZPosition(TerrainInstanceCellDataContainer.CalculateCellWorldPosition(newCell));

            // copy cell data from main terrain to selected terrain
            _gizmoData.terrainInstanceMain.AssignCellDataToCell(_gizmoData.terrainInstanceSelected, _gizmoData.terrainInstanceSelected.CellFromIndex(0, 0), 0.02f, newCell);

            // add cell to selected cells and set size
            _selectedCells = new List<(int StartIndexX, int StartIndexY)>(1) { (_selectedCell.Index.x, _selectedCell.Index.y) };
            SelectedCellsSize.Set(0, 0);
        }
        // keep marking multiple tiles 
        else if (Input.GetMouseButton(0) && newCell.Index != _selectedCell.Index) {

            // locate the lowest x,y indexes
            Vector2Int lowestIndex = new Vector2Int(newCell.Index.x < _selectedCell.Index.x ? newCell.Index.x : _selectedCell.Index.x, newCell.Index.y < _selectedCell.Index.y ? newCell.Index.y : _selectedCell.Index.y);

            // set new size and assign main terrain data to the select visuals
            SelectedCellsSize.Set(Mathf.Abs(newCell.Index.x - _selectedCell.Index.x), Mathf.Abs(newCell.Index.y - _selectedCell.Index.y));
            _gizmoData.terrainInstanceMain.AssignCellDataToCell(_gizmoData.terrainInstanceSelected, new Vector2Int(0, 0), 0.02f, lowestIndex, SelectedCellsSize, out _selectedCells);

            // set new positon to select grid to lowest cell index
            _gizmoData.terrainInstanceSelected.SetXZPosition(TerrainInstanceCellDataContainer.CalculateCellWorldPosition(_gizmoData.terrainInstanceMain.CellFromIndex(lowestIndex.x, lowestIndex.y)));
        }
    }

    /// <summary>
    /// Update sculpting tool logic
    /// </summary>
    /// <param name="newCell">Cell that is currently marked, can be null</param>
    private void EditorToolSculptCallback(TerrainInstanceCellDataContainer newCell) {

        void getSelectedCells(TerrainInstanceCellDataContainer centerCell) {
            // special logic for handling 1x1 grid size
            if (_gizmoData.guiDataContainer.SculptSize == 1) {
                // set position to match hoovered cell
                _gizmoData.terrainInstanceFeedback.SetXZPosition(TerrainInstanceCellDataContainer.CalculateCellWorldPosition(centerCell));

                // copy cell data from main terrain
                _gizmoData.terrainInstanceMain.AssignCellDataToCell(_gizmoData.terrainInstanceFeedback, _gizmoData.terrainInstanceFeedback.CellFromIndex(0, 0), 0.02f, centerCell);

                _selectedCells = new List<(int StartIndexX, int StartIndexY)>() { centerCell.Index };
            }
            // handle bigger grid sizes
            else {
                // reset terrain position
                _gizmoData.terrainInstanceFeedback.SetXZPosition(new Vector3(0.0f, 0.0f, 0.0f));

                // set start index for marking multiple cells
                Vector2Int startIndex = new Vector2Int(centerCell.Index.x - _gizmoData.guiDataContainer.SculptSize, centerCell.Index.y - _gizmoData.guiDataContainer.SculptSize);

                // copy cell data by giving a start index and then looping the x,y size
                _gizmoData.terrainInstanceMain.AssignCellDataToCell(_gizmoData.terrainInstanceFeedback, // copy to this terrain layer
                                                          startIndex,   // start index on feedback terrain
                                                          _feedbackLayerOffset,        // increased height
                                                          startIndex,   // start index on main terrain
                                                          new Vector2Int(_gizmoData.guiDataContainer.GridSculptSize, _gizmoData.guiDataContainer.GridSculptSize), // size of marked area
                                                          out _selectedCells); // collect the marked cells
            }
        }

        _gizmoData.terrainInstanceFeedback.Hide = true;

        // handle input cooldown
        if (_editCooldownCounter >= 0.0f) {
            _editCooldownCounter += Time.deltaTime;

            if (_editCooldownCounter >= _editCooldown) {
                _editCooldownCounter = -1.0f;
            }
        }

        // reset selected sell data if active
        if (_selectedCell != null) {
            _selectedCell = null;
            _selectedCells = null;
            _gizmoData.terrainInstanceSelected.Hide = true;
        }

        // stop logic if we're not hoovering any cell
        if (newCell == null)
            return;

        // get new cell if it's only one
        if ((_selectedCells == null) || _mouseDelta.magnitude > 0.0f)
            getSelectedCells(newCell);
        // get grid of cells
        else {
           (int x, int y) centerIndex = _selectedCells.Count == 1 ? _selectedCells[0] : (_selectedCells[0].StartIndexX + _gizmoData.guiDataContainer.SculptSize, _selectedCells[0].StartIndexY + _gizmoData.guiDataContainer.SculptSize);
            getSelectedCells(_gizmoData.terrainInstanceMain.CellFromIndex(centerIndex.x , centerIndex.y));
        }

        // apply height modification on mouse down or hold
        if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0))) {

            bool threasholdReached = false;
            float modscale = Time.deltaTime * 3f;
            bool increaseTerrain = Input.GetMouseButtonDown(0) ? ITerrainEditorGizmoRepresentation.CurrentRepresentation.ShouldIncrease() : _gizmoData.guiDataContainer.ToolbarIncreaseInt == 1;
            if (!increaseTerrain)
            {
                _gizmoData.guiDataContainer.ToolbarIncreaseInt = 0;
                ITerrainEditorGizmoRepresentation.CurrentRepresentation.Decrease(modscale, ref threasholdReached);
                
            }
            else
            {
                _gizmoData.guiDataContainer.ToolbarIncreaseInt = 1;
                ITerrainEditorGizmoRepresentation.CurrentRepresentation.Increase(modscale, ref threasholdReached);
            }

            //if (_editCooldownCounter < 0.0f)
            if(!threasholdReached)
            {
                List<(int, double)> newTerrainState = new List<(int, double)>();

                modscale *= 0.35f;

                // increase height of cells
                _gizmoData.terrainInstanceMain.IncreaseHeightAndUpdate(_selectedCells, _gizmoData.guiDataContainer.ToolbarIncreaseInt == 0 ? modscale : -modscale, in newTerrainState, null);

                _editCooldownCounter = 0.0f;
            }
        }
        // reset cooldown on releasing mouse button
        else if (Input.GetMouseButtonUp(0)) {
            _editCooldownCounter = 0.0f;
            ITerrainEditorGizmoRepresentation.CurrentRepresentation.ResetCooldown();
        }
    }
}
