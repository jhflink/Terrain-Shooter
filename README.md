# TerrainShooter

Game Controls:  
  
WASD to move.  
SPACE to change weapon type.  
LEFT MOUSE BUTTON to shoot.  
  
Flip enemies with the "increase terrain" weapon.  
When flipped use the "decrease terrain" weapon to kill them.  
Be careful to not make them angry.  
  
Editor Controls:  
  
WASD to move.  
Hold right mouse button to look around.  
Left mouse button to edit terrain.  
  
Game.    
Class Index:  
GameManager.cs: Main entry point tying all the game logic together. 
  
ObjectPool.cs:                  Generic ObjectPool class for IPoolableObjects.  
BaseActor.cs                    A base class that every actor that moves around the terrain should inherit from.  
Creature.cs                     Logic for the creature / enemy.  
Bullet.cs                       Logic for shooting bullets, checking if it hits something, etc.  
ParticleEmitter,cs:             A wrapper around the particle system to be pooled and copy particle settings to.  
SpawnPoint.cs                   Lightweight logic for updating a spawner.  
   
Player.cs:                      Player input / logic.  
PlayerCamerea.cs:               Logic for handling the player camera.  
PlayerCameraSettings.cs         Data container for camera settings that can be applied to the player camera.  
PlayerDataContainer.cs:         Data container for some player stats, as well as the GUI code to display them.  

Editor.  
Class Index:  
TerrainEditorMain.cs: Main entry point, tying all the different parts together. 
   
TerrainInstance.cs:                 Contains all important functionality to generate a terrain  
TerrainInstanceCreator.cs           Component to add to a game object to instantiate an terrain.  
TerrainInstanceCellDataContainer.cs The data related to each cell as well as some functionality for manipulation.  
TerrainInstanceMeshData.cs          Contains the actual mesh data and functionality to manipulate it.  
TerrainInstanceDataContainer:       Contains functionality to save and load terrain data.  
   
TerrainEditorCamera.cs:             Handles all camera input.  
TerrainEditorGui.cs:                All the code to render gui (you could call this the gui view).  
TerrainEditorGUIDataContainer.cs    The data container the gui is displaying (you could call this the gui model).  
TerrainEditorGizmo.cs:              Handles the logic for the various terrain editing tools.  
TerrainEditorPerlinNoiseGenerator:  A simple wrapper for a perlin noise generator.  
 
ComputeBufferContainer.cs: A wrapper around the compute buffer with some extra functionality.  
IOUtility.cs: Save/load xml functionality. 
