# VR Supermarket

A small Unity prototype for a first-person supermarket experience. The project includes a walkable store scene, item pickup interactions, a checkout flow, and editor tools to generate all the product data and build the environment automatically.

## Features

- First-person player movement with walking, sprinting, jumping, and mouse look
- Raycast-based interaction for picking up items from shelves
- Shopping cart and checkout trigger flow
- Editor menu tools to create product assets and build a supermarket scene
- Basic desktop-mode setup with a VR adapter stub for future XR support

## Project structure

- `PlayerController.cs` — handles movement, camera look, gravity, and input locking
- `PlayerInteraction.cs` — detects products and checkout areas and triggers interactions
- `ProductAssetCreator.cs` — creates ScriptableObject product assets for the store
- `SupermarketSceneBuilder.cs` — builds the full supermarket environment, lighting, shelves, UI, and player rig
- `VRPlayerAdapter.cs` — toggles between desktop and VR rig modes

## Getting started

1. Open the project in Unity.
2. Go to `Edit > Project Settings > Player`.
3. Set `Active Input Handling` to `Both`.
4. Run the editor menu item: `VR Supermarket > Step 1 - Create Product Assets`.
5. Run: `VR Supermarket > Step 2 - Build Supermarket Scene`.
6. Press Play to test the supermarket.

## Notes

- This project is a prototype and is designed for experimentation and learning.
- Desktop mode is the active default workflow.
- VR support is prepared for future expansion through the XR Interaction Toolkit.
