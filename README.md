# psychic-waddle

A Unity multiplayer game project (Friendslop) with support for up to 4 players.

## Features

### Multiplayer Support
- **Up to 4 players** can connect and play simultaneously
- **LAN multiplayer** using Unity Netcode for GameObjects
- **Real-time shooting synchronization** - all players see shooting actions
- **Player identification** - each player has a unique ID
- **Authoritative server** architecture for reliable gameplay
- Modular design for future expansion

### Gameplay
- Player movement with physics-based controls (WASD/Arrow Keys)
- Jump mechanics (Space)
- Shooting system (Left Mouse Button / Fire1)
- Ground detection and collision

## Getting Started

### Prerequisites
- Unity 6000.2.13f1 or compatible version
- Unity Netcode for GameObjects package (automatically added via manifest)

### Setup Instructions
1. Clone the repository
2. Open the project in Unity
3. See [MULTIPLAYER_README.md](Assets/Scripts/MULTIPLAYER_README.md) for detailed multiplayer setup

### Quick Start - Testing Multiplayer
1. Open the game scene in Unity
2. Set up NetworkManager and MultiplayerManager in the scene
3. Create a player prefab with NetworkPlayer component
4. Run the game - click "Host" to start a game
5. Build and run a second instance, click "Join" to connect

For detailed setup instructions, see [Assets/Scripts/MULTIPLAYER_README.md](Assets/Scripts/MULTIPLAYER_README.md)

## Project Structure
- `Assets/Scripts/` - Core game scripts
  - `PlayerController.cs` - Basic single-player movement
  - `NetworkPlayer.cs` - Network-enabled multiplayer player
  - `Gun.cs` - Shooting mechanics
  - `MultiplayerManager.cs` - Network session management
  - `MultiplayerUIManager.cs` - Multiplayer lobby UI
  - `GameManager.cs` - Game state management
  - `UIManager.cs` - General UI management
  - `GameSettings.cs` - Game configuration (ScriptableObject)
- `Assets/Scenes/` - Unity scenes
- `Packages/` - Unity package dependencies

## Future Enhancements
- Player health and damage system
- Different weapon types
- Power-ups and collectibles
- Score tracking and leaderboard
- Match timer and win conditions
- Internet connectivity via relay servers
- Dedicated server support

## License
See repository license file for details.