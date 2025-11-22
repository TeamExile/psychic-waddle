# Friendslop - Unity Game Project

A multiplayer "friendslop" game built with Unity using modern best practices and the latest Unity conventions.

## Project Information

- **Unity Version**: 2022.3.10f1 (LTS)
- **Game Name**: Friendslop
- **Developer**: TeamExile

## Features

- Modern Unity architecture using:
  - ScriptableObjects for configuration
  - Singleton patterns for managers
  - Component-based design
  - Ready for New Input System integration
  - TextMeshPro for UI
  
## Project Structure

```
Assets/
├── Scenes/          # Game scenes
│   └── MainScene.unity
├── Scripts/         # C# game scripts
│   ├── GameManager.cs        # Main game controller
│   ├── GameSettings.cs       # ScriptableObject for settings
│   ├── PlayerController.cs   # Player movement and controls
│   └── UIManager.cs          # UI management
├── Prefabs/         # Reusable game objects
└── Materials/       # Materials and shaders

Packages/            # Unity package dependencies
ProjectSettings/     # Unity project configuration
```

## Getting Started

1. Open this project in Unity 2022.3 LTS or later
2. Open the MainScene in Assets/Scenes/
3. Press Play to test the game

## Game Components

### GameManager
Central game controller that manages game states (MainMenu, Playing, Paused, GameOver).

### GameSettings
ScriptableObject containing configurable game parameters:
- Player movement settings
- Game rules
- Visual settings

### PlayerController
Handles player movement with:
- WASD/Arrow key movement
- Jump mechanics
- Ground detection
- Rigidbody-based physics

### UIManager
Manages all UI elements and game screens using TextMeshPro.

## Development Notes

This project follows Unity best practices:
- Uses namespaces to organize code
- Component-based architecture
- ScriptableObjects for data-driven design
- Ready for multiplayer expansion
- Compatible with Unity's new Input System

## License

MIT License