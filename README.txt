This is a preview of some code I wrote for Cosmews (Trailer: https://www.youtube.com/watch?v=7oSJRtBZEF0), an ARKit game made in Unity about feeding hungry space cats. 
I included the entire Throw class that contains the main gameplay mechanic as well as some system level classes/scripts I wrote for the level/environment generation and individual level creation tool.
These systems all work with other larger and smaller systems, which I would be happy to elaborate on at request so feel free to ask me any questions.

Throw.cs
- The swipe-to-throw main mechanic of the game that probably has had the most iterations out of any script in the project
- Developed/iterated over months, including a full rework that involved a combination of clamping user input,rounding, then scaling/clamping further when translating to physical 3D forces using rounding to ensure intentional, accessible play.
- ThrowMochiRework() method contains most of the actual finished work/mechanic
- Screenshot0 demonstrating design tooling/iteration available in editor: https://i.imgur.com/NCuuz2Y.png

GrowGoalManager.cs
- Each level prefab contains this manager that will spawn in the level's goal nodes/suns that the player must sequentially make the cat grow toward in order to beat the level.
- Allows user to drag and drop goal prefab objects to use as template for that level's sun(s).
- Allows user to easily decide the location goal(s) will appear for a given level in game world's grid space.
- Screenshot1: https://i.imgur.com/r9r3Qy9.png

LevelManager.cs
- Singleton manager that interacts with the game manager states to control game flow
- Decides activation/deactivation and placement of levels, cat, and the player's throw ability

ARGrid.cs
- Uses a three dimensional array to generate a uniform grid in real 3D space.
- Creates galaxy environment of planets for cat and levels to inhabit.
- Allows for user to decide different length, width, height in real world metersas well as planetary grid density, allowing faster testing.
- ScreenCapture2 (GIF): https://i.imgur.com/PWyjJXz.gifv



