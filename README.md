# Simple Unity Object History

This is a very simple Object History Window for Unity. The window records previously selected objects, which can be used for quick access.

## Installation

Option A: Install as Unity Package
1. Download or clone the repository.
2. Install the folder or .zip as a [Unity Local Package](https://docs.unity3d.com/Manual/upm-ui-local.html).

Option B: Manual
1. Download or clone the repository.
2. Grab the script in the `Editor` folder, and place it in any folder in the Editor assembly in Unity (e.g. a folder named `Editor`).

## Usage

In Unity, open the window at: **Window > General > Simple Object History**. This will begin recording of the object history.

While opened, any selection of a Unity Object (Folders, ScriptableObject, GameObject etc) will record it into the window.
Click on an entry in the window to highlight the previous selected Object, or Double Click on the object entry to open it.
The "Select Object" button selects the object, while the "Open Inspector" button opens an inspector window for the object.

## Known Issues / Future Work:

1. Scene objects and objects in Prefabs are currently recorded, but will be removed from the History once the Scene or Prefab has been closed.
2. Currently, the maximum size of the History is 20, which is adjustable in the code. Future work will be to move this into a configurable plugin preference.