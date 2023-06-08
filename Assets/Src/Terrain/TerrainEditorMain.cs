using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using SimpleFileBrowser;

/// <summary>
/// Container class for Input State callbacks
/// </summary>
public class TerrainEditorInputStateCallbackContainer
{
    // callback to be called on entering the assigned state
    public System.Action onEnter = null;

    /// <summary>
    /// callback to be called on updating the assigned state
    /// Pass the current state
    /// returns a new state or the same to be kept updating
    /// </summary>
    public System.Func<TerrainEditorMain.EditorInputState, TerrainEditorMain.EditorInputState> onUpdate = null;

    // callback to be called on exiting the assigned state
    public System.Action onExit = null;

    public TerrainEditorInputStateCallbackContainer() { }
}

/// <summary>
/// Main entry point for program, handling basic state functionality
/// Class Index:
/// TerrainEditorMain.cs: Main entry point, tying all the different parts together
/// 
/// TerrainInstance.cs:                 Contains all important functionality to generate a terrain
/// TerrainInstanceCreator.cs           Component to add to a game object to instantiate an terrain
/// TerrainInstanceCellDataContainer.cs The data related to each cell as well as some functionality for manipulation
/// TerrainInstanceMeshData.cs          Contains the actual mesh data and functionality to manipulate it
/// TerrainInstanceDataContainer:       Contains functionality to save and load terrain data
/// 
/// TerrainEditorCamera.cs:             Handles all camera input
/// TerrainEditorGui.cs:                All the code to render gui (you could call this the gui view)
/// TerrainEditorGUIDataContainer.cs    The data container the gui is displaying (you could call this the gui model)
/// TerrainEditorGizmo.cs:              Handles the logic for the various terrain editing tools
/// TerrainEditorPerlinNoiseGenerator:  A simple wrapper for a perlin noise generator
///
/// ComputeBufferContainer.cs: A wrapper around the compute buffer with some extra functionality
/// IOUtility.cs: Save/load xml functionality
/// </summary>
public class TerrainEditorMain : MonoBehaviour
{
    // Different states for the editor
    public enum EditorInputState {
        Camera, // Handling free roaming camera input
        Edit,   // Handling editing of terrain
        Io,     // Handling file Input/Output
        Paused, // Pausing all input
    }

    // Current editor state
    private EditorInputState _currentEditorState = EditorInputState.Edit;

    /// <summary>
    /// Container that connects a certain editor state with an callback function
    /// Callback function should take the current state
    /// and return a new, or the same state to run
    /// </summary>
    private Dictionary<EditorInputState, TerrainEditorInputStateCallbackContainer> _editorUpdateStateCallbacks = new System.Collections.Generic.Dictionary<EditorInputState, TerrainEditorInputStateCallbackContainer>();

    // all created terrain instances
    private List<TerrainInstance> _terrainInstances = null;

    // states of the modified terrain for undo/redo purpose
    // and index of current state
    private List<List<(int, double)>> _terrainStates = new List<List<(int, double)>>();
    private int _terrainStateIndex = 0;

    // all terrain instance creator components
    private TerrainInstanceCreator[] _terrainInstanceCreators = null;

    // terrain size
    public Vector2Int terrainSize = new Vector2Int();

    // camera
    public TerrainEditorCamera TerrainEditorCamera = null;

    // should render gui
    public bool RenderGUI = true;

    // main terrain instance and cretor component
    private TerrainInstance _terrainInstanceMain = null;
    public TerrainInstanceCreator mainTerrainCreator = null;

    // feedback geometry and creator component
    public TerrainInstanceCreator feedbackTerrainCreator = null;
    private TerrainInstance _terrainInstanceFeedback = null;

    // selected geometry and creator component
    public TerrainInstanceCreator selectedTerrainCreator = null;
    private TerrainInstance _terrainInstanceSelected = null;

    // gui data container
    private TerrainEditorGUIDataContainer _guiDataContainer;

    // gui component
    public TerrainEditorGUI editorGui = null;

    // editor gizmo
    private TerrainEditorGizmo _editorGizmo = null;

    // base name for naming instances
    private const string _instanceBaseName = "TerrainInstance ";

    // raycast out hit data
    private RaycastHit _raycastHit = new RaycastHit();

    // Start is called before the first frame update
    void Start() {

        // force resolution
        float aspectRatio = TerrainEditorCamera.CameraRaw.aspect;
        int newHeight = 1080;
        Screen.SetResolution((int)(newHeight * aspectRatio), newHeight, Screen.fullScreen);
        _refreshGui = true;

        // get all the terrain creator components
        _terrainInstanceCreators = GetComponents<TerrainInstanceCreator>();
        _terrainInstances = new List<TerrainInstance>(_terrainInstanceCreators.Length);

        // instantiate all our terrain creators
        for (int i = 0; i < _terrainInstanceCreators.Length; i++) {
            _terrainInstances.Add(new TerrainInstance(_terrainInstanceCreators[i],
                                  new GameObject(_instanceBaseName + _terrainInstanceCreators[i].id),
                                  terrainSize));
        }

        // set up main terrain
        _terrainInstanceMain = _terrainInstances.Find(x => x.Id == mainTerrainCreator.id);
        _terrainInstanceMain.ReGenerate((_terrainInstanceMain.DataContainer.Size.x, _terrainInstanceMain.DataContainer.Size.y));

        // set up feedback terrain visuals
        _terrainInstanceFeedback = _terrainInstances.Find(x => x.Id == feedbackTerrainCreator.id);
        _terrainInstanceSelected = _terrainInstances.Find(x => x.Id == selectedTerrainCreator.id);
        _terrainInstanceSelected.SetAllColorAndUpdate(selectedTerrainCreator.gradient.Evaluate(0.0f));
        _terrainInstanceSelected.Hide = true;
        _terrainInstanceFeedback.Hide = true;

        // set up gui
        SetUpGUI();

        // Set-up camera input state callbacks.
        _editorUpdateStateCallbacks.Add(EditorInputState.Camera, new TerrainEditorInputStateCallbackContainer() {
            onUpdate = TerrainEditorCamera.InputStateCameraUpdate,
        });

        // create editor gizmo
        _editorGizmo = new TerrainEditorGizmo(new TerrainEditorGizmoData() {
            terrainInstanceFeedback = _terrainInstanceFeedback,
            terrainInstanceMain = _terrainInstanceMain,
            terrainInstanceSelected = _terrainInstanceSelected,
            guiDataContainer = _guiDataContainer,
            editorCamera = TerrainEditorCamera,
        });

        // Set-up edit input state callbacks.
        _editorUpdateStateCallbacks.Add(EditorInputState.Edit, new TerrainEditorInputStateCallbackContainer() {
            onEnter = _editorGizmo.InputStateEditEnter,
            onUpdate = _editorGizmo.InputStateEditUpdate,
            onExit = _editorGizmo.InputStateEditExit,
        });

        // Set-up io input state callbacks.
        _editorUpdateStateCallbacks.Add(EditorInputState.Io, new TerrainEditorInputStateCallbackContainer() {
            onUpdate = InputStateIOUpdate,
        });

        // set-up puase callbacks
        _editorUpdateStateCallbacks.Add(EditorInputState.Paused, new TerrainEditorInputStateCallbackContainer()
        {
            onUpdate = (EditorInputState currentState) => { return RenderGUI ? EditorInputState.Edit : currentState; }
        });

        // check if editor should be paused or not
        _currentEditorState = RenderGUI ? EditorInputState.Edit : EditorInputState.Paused;

        // enter start state
        _editorUpdateStateCallbacks[_currentEditorState].onEnter?.Invoke();
    }

    /// <summary>
    /// Hide feedback layers
    /// </summary>
    public void HideTerrainFeedbackAndSelection(bool hide)
    {
        _terrainInstanceSelected.Hide = hide;
        _terrainInstanceFeedback.Hide = hide;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public float HeightOnColliderCallback(Vector3 position)
    {
        return _terrainInstanceMain.ClosestPointOnCollider(position).y;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public TerrainInstanceCellDataContainer FetchCellFromWorldPositionCallback(Vector3 position)
    {
        return _terrainInstanceMain.ClosestCellToPoint(_terrainInstanceMain.InvereseTransformPoint(position));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cellIndex"></param>
    /// <returns></returns>
    public TerrainInstanceCellDataContainer FetchCellFromIndexCallback((int x, int y) cellIndex)
    {
        return _terrainInstanceMain.CellFromIndex(cellIndex.x, cellIndex.y);
    }

    /// <summary>
    /// Set up gui callbacks gui data container
    /// </summary>
    private void SetUpGUI() {
        // create gui data container
        _guiDataContainer = new TerrainEditorGUIDataContainer() {
            FetchGuiRect = FetchGUIRect,
            RefreshGUIRects = RefreshGuiRects,
        };

        // raise button callback
        _guiDataContainer.callbacks.Add(TerrainEditorGUI_IDS.RaiseButton, () => {
            IncreaseMainCellHeightAndSaveState(_guiDataContainer.SelectedCells, TerrainEditorGizmo.editHeight);
            _terrainInstanceSelected.IncreaseHeightAndUpdate((0, 0), _guiDataContainer.SelectedCellsSize, TerrainEditorGizmo.editHeight, null, _terrainInstanceMain);
        });

        // lower button callback
        _guiDataContainer.callbacks.Add(TerrainEditorGUI_IDS.LowerButton, () => {
            IncreaseMainCellHeightAndSaveState(_guiDataContainer.SelectedCells, -TerrainEditorGizmo.editHeight);
            _terrainInstanceSelected.IncreaseHeightAndUpdate((0, 0), _guiDataContainer.SelectedCellsSize, -TerrainEditorGizmo.editHeight, null, _terrainInstanceMain);
        });

        // flatten marked cells button
        _guiDataContainer.callbacks.Add(TerrainEditorGUI_IDS.FlattenMarkedCellsButton, () => {
            float flattenHeight = _terrainInstanceMain.Flatten(_guiDataContainer.SelectedCells);
            _terrainInstanceSelected.SetAllHeightAndUpdate(flattenHeight + 0.02f, _terrainInstanceMain);
        });

        // flatten terrain button
        _guiDataContainer.callbacks.Add(TerrainEditorGUI_IDS.FlattenButton, () => {
            _terrainInstanceMain.FlattenAllCellsToHeight(-1.0f);
        });

        // flatten terrain button
        _guiDataContainer.callbacks.Add(TerrainEditorGUI_IDS.SpawnCreatureButton, () => {
            List<TerrainInstanceCellDataContainer> spawnCells = null;
            _terrainInstanceMain.CellsFromIndexes(_guiDataContainer.SelectedCells, out spawnCells);
            Creature.SpawnNext(spawnCells, true);
        });

        // generate terrain button
        _guiDataContainer.callbacks.Add(TerrainEditorGUI_IDS.GenerateButton, () => {
            ReGenerateTerrain();
        });

        // redo button
        _guiDataContainer.callbacks.Add(TerrainEditorGUI_IDS.RedoButton, () => {
            IncrementTerrainState(1, (height) => { return height; });
        });

        // undo button
        _guiDataContainer.callbacks.Add(TerrainEditorGUI_IDS.UndoButton, () => {
            IncrementTerrainState(-1, (height) => { return -height; });
        });

        // load button
        _guiDataContainer.callbacks.Add(TerrainEditorGUI_IDS.LoadButton, () => {
            SimpleFileBrowser.FileBrowser.ShowLoadDialog(new FileBrowser.OnSuccess(LoadTerrainFromFile), null, FileBrowser.PickMode.Files);
        });

        // save button
        _guiDataContainer.callbacks.Add(TerrainEditorGUI_IDS.SaveButton, () => {
            SimpleFileBrowser.FileBrowser.ShowSaveDialog(new FileBrowser.OnSuccess(SaveTerrainToFile), null, FileBrowser.PickMode.Files);
        });

        // enter height input field
        _guiDataContainer.callbacks.Add(TerrainEditorGUI_IDS.EnterHeightInput, () => {
            _terrainInstanceMain.SetHeightAndUpdate(_guiDataContainer.SelectedCells, _guiDataContainer.CustomHeight);
            _terrainInstanceSelected.SetAllHeightAndUpdate(_guiDataContainer.CustomHeight + 0.02f, _terrainInstanceMain);
        });
    }

    // cache rect for all gui so we can use it to figuire out if it's being hoovered or not
    private Dictionary<string, Rect> _guiRects = new Dictionary<string, Rect>();
    private Rect FetchGUIRect(string id, float x, float y, float width, float height) {

        if (!_guiRects.ContainsKey(id))
            _guiRects.Add(id, new Rect(x, y, width, height));

        return _guiRects[id];
    }

    public class MenuItem
    {
        public List<MenuItem> children = new List<MenuItem>();
        public string _id = "";
        public MenuItem(string id)
        {
            _id = id;
        }
    }

    // render our immidiete gui
    private bool _refreshGui = false;
    private void OnGUI() {

        // update variables (should usually be connected through bindings)
        _guiDataContainer.SelectedCell = _editorGizmo.SelectedCell;
        _guiDataContainer.SelectedCells = _editorGizmo.SelectedCells;
        _guiDataContainer.SelectedCellsSize = _editorGizmo.SelectedCellsSize;
        _guiDataContainer.HooverCell = _editorGizmo.ActiveCell;

        // update gizmo visual representation
        ITerrainEditorGizmoRepresentation.CurrentRepresentation.GameObject().SetActive(_guiDataContainer.HooverCell != null);
        if (_guiDataContainer.HooverCell!=null) {
            Vector3 worldPosition = TerrainInstanceCellDataContainer.CalculateCellWorldPosition(_guiDataContainer.HooverCell);
            ITerrainEditorGizmoRepresentation.CurrentRepresentation.GameObject().transform.position = new Vector3(worldPosition.x, TerrainInstanceCellDataContainer.HeightOfCell(_guiDataContainer.HooverCell) + 3.0f, worldPosition.z);  
        }

        // render the actual gui
        if (RenderGUI) {
            _editorGizmo.editorTool = editorGui.Render(_guiDataContainer, _editorGizmo.editorTool);
        }

        // refresh gui rects after changing screen resolution
        if (_refreshGui) {
            RefreshGuiRects();
        }
    }

    /// <summary>
    /// Regenerate the terrain
    /// </summary>
    public void ReGenerateTerrain()
    {
        _terrainInstanceMain.ReGenerate((_terrainInstanceMain.DataContainer.Size.x, _terrainInstanceMain.DataContainer.Size.y));
        ResetTerrainState();
    }

    /// <summary>
    /// Get a cell by shooting a ray from the mouse to the terrain
    /// </summary>
    /// <param name="rayDistance"></param>
    /// <returns></returns>
    public (TerrainInstanceCellDataContainer cell, Vector3 hitPoint) CellFromMousePosition(float rayDistance)
    {
        // create a ray from the camera through the mouse position
        Ray ray = TerrainEditorCamera.CameraRaw.ScreenPointToRay(Input.mousePosition);

        // raycast against the terrain collider
        bool raycastResult = _terrainInstanceMain.RaycastMeshCollider(ray, out _raycastHit, rayDistance);

        // if the raycast hit the collider, pick out the cell based on the local hit position
        return (raycastResult ? _terrainInstanceMain.ClosestCellToPoint(_raycastHit.point) : null, _raycastHit.point);
    }

    /// <summary>
    /// Remove all saved gui rects so they can be rebuilt
    /// </summary>
    public void RefreshGuiRects() {
        _guiRects.Clear();
        _refreshGui = false;
    }

    /// <summary>
    /// Increase height of cells on the main terrain.
    /// </summary>
    /// <param name="cells">the cells to manipulate</param>
    /// <param name="height">vertex height</param>
    public void IncreaseMainCellHeightAndSaveState(List<(int,int)> cells, float height) {

        List<(int, double)> newTerrainState = new List<(int, double)>();

        _terrainInstanceMain.IncreaseHeightAndUpdate(cells, height, in newTerrainState, null);

        SaveNewTerrainState(newTerrainState);
    }

    /// <summary>
    /// Increase height of one cell
    /// </summary>
    /// <param name="cell"></param>
    /// <param name="height"></param>
    public void IncreaseHeightAndUpdate(TerrainInstanceCellDataContainer cell, float height) => _terrainInstanceMain.SetHeightAndUpdate(cell, height);

    /// <summary>
    /// Saves a new terrain state
    /// </summary>
    /// <param name="newTerrainState"></param>
    public void SaveNewTerrainState(List<(int, double)> newTerrainState) {
        // save new state
        for (int i = _terrainStates.Count - 1; i > _terrainStateIndex; i--) {
            _terrainStates.RemoveAt(i);
        }

        _terrainStates.Add(newTerrainState);
        _terrainStateIndex = _terrainStates.Count;
    }

    /// <summary>
    /// Called when program is terminated
    /// </summary>
    private void OnDestroy() {
        // call destroy code on all the terrains
        foreach (TerrainInstance terrain in _terrainInstances)
            terrain.Destroy();
    }

    /// <summary>
    /// Reset terrain state
    /// </summary>
    public void ResetTerrainState() {
        _terrainStateIndex = - 1;
        _terrainStates.Clear();
    }

    /// <summary>
    /// Test if position is within bounds
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public bool WithinMainBounds(Vector3 position)
    {
        return _terrainInstanceMain.WithinBounds(position);
    }

    /// <summary>
    /// Correct the position to closest point on bounds if we're out of bounds
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public Vector3 CorrectPositionToBeWithinBounds(Vector3 position)
    {
        return WithinMainBounds(position) ? position : _terrainInstanceMain.ClosestPointOnBounds(position);
    }

    /// <summary>
    /// Step forward or backward in the saved terrain state
    /// </summary>
    /// <param name="incrementValue">How many steps to move positive or negative</param> 
    /// <param name="undoRedoHeightFunc">A function call that desides the positive or negative value of the height change</param>
    public void IncrementTerrainState(int incrementValue, System.Func<float, float> undoRedoHeightFunc) {
        _terrainStateIndex += incrementValue;

        if (_terrainStateIndex < 0)
            _terrainStateIndex = -1;
        else if (_terrainStateIndex > _terrainStates.Count - 1)
            _terrainStateIndex = _terrainStates.Count;
        else {

            _terrainInstanceMain.UndoRedoTerrainState(_terrainStates[_terrainStateIndex], undoRedoHeightFunc);
            _terrainInstanceSelected.Hide = true;
        }
    }

    /// <summary>
    /// Load a terrain from xml data
    /// </summary>
    /// <param name="paths">Path(s) to file(s) to load</param>
    public void LoadTerrainFromFile(string[] paths) {
        ResetTerrainState();
        TerrainInstanceDataContainer dataContainer = TerrainInstanceDataContainer.LoadFromFile(paths[0]);

        if (dataContainer != null)
            _terrainInstanceMain.AssignDataContainerAndUpdateVertices(dataContainer);
    }

    /// <summary>
    /// Save a terrain to xml file
    /// </summary>
    /// <param name="paths">Path(s) to where to save file(s)</param>
    public void SaveTerrainToFile(string[] paths) {
        _terrainInstanceMain.SaveHeightDataToDataContainer();

        TerrainInstanceDataContainer.SaveToFile(_terrainInstanceMain.DataContainer, paths[0]);
    }

    // Update is called once per frame
    void Update() {

        // kill application
        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.P))
            UnityEditor.EditorApplication.isPaused = true;
#endif

        // get mouse position and flip the Y value since the GUI has inverted vertical coordinates
        Vector2 mouseGuiPosition = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

        // check if we overlap any gui elements
        bool hooverGui = false;
        foreach (Rect rect in _guiRects.Values) {

            if (RenderGUI && rect.Contains(mouseGuiPosition)) {
                hooverGui = true;
                break;
            }
        }

        // update current state if we're not hoovering the gui and have a stte callback
        if (!hooverGui && _editorUpdateStateCallbacks.ContainsKey(_currentEditorState)) {

            // update current state
            EditorInputState resultState = _editorUpdateStateCallbacks[_currentEditorState]!.onUpdate(_currentEditorState);

            // check if we should pause the editing mode
            if (!RenderGUI && _currentEditorState != EditorInputState.Paused)
                resultState = EditorInputState.Paused;

            // check if file browser is open then
            // change to IO state
            if (SimpleFileBrowser.FileBrowser.IsOpen)
                resultState = EditorInputState.Io;

            // handle state change
            if (_currentEditorState != resultState) {

                // exit old state
                _editorUpdateStateCallbacks[_currentEditorState].onExit?.Invoke();

                // set new state
                _currentEditorState = resultState;

                // enter new state
                _editorUpdateStateCallbacks[_currentEditorState].onEnter?.Invoke();

                // update new state
                _editorUpdateStateCallbacks[_currentEditorState].onUpdate?.Invoke(resultState);
            }
        }
    }

    private EditorInputState InputStateIOUpdate(EditorInputState currentState) {

        // if we're in IO state but file browser is closed, return back to Edit state
        return SimpleFileBrowser.FileBrowser.IsOpen ? currentState : EditorInputState.Edit;
    }
}
