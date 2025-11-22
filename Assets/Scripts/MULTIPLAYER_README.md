# Multiplayer Implementation Documentation

## Overview
This document describes the multiplayer implementation for Friendslop using Unity Netcode for GameObjects.

## Architecture

### Network Model
- **Type**: Authoritative Server (Host acts as server)
- **Maximum Players**: 4
- **Connection Type**: LAN (local area network) initially, expandable to WAN
- **Framework**: Unity Netcode for GameObjects (com.unity.netcode.gameobjects)

## Components

### 1. MultiplayerManager (`MultiplayerManager.cs`)
Main manager for multiplayer functionality.

**Responsibilities:**
- Starting/stopping host and client
- Managing player connections (up to 4 players)
- Spawning players at designated spawn points
- Tracking connected players

**Key Methods:**
- `StartHost()` - Start game as host (server + client)
- `StartClient()` - Join game as client
- `Shutdown()` - Disconnect from network session
- `OnClientConnected()` - Handle new player connections
- `OnClientDisconnected()` - Handle player disconnections

**Properties:**
- `MaxPlayers` - Maximum number of players (4)
- `ConnectedPlayerCount` - Current number of connected players
- `IsLobbyFull` - Whether the lobby has reached max capacity

### 2. NetworkPlayer (`NetworkPlayer.cs`)
Network-synchronized player controller.

**Features:**
- Network-synchronized position and rotation
- Player identification via unique IDs
- Shooting synchronization across all clients
- Movement with physics-based controls
- Jump functionality
- Ground detection

**Network Variables:**
- `playerId` - Unique identifier for each player
- `networkPosition` - Synchronized position
- `networkRotation` - Synchronized rotation

**RPCs:**
- `RequestPlayerIdServerRpc()` - Request player ID from server
- `UpdatePositionServerRpc()` - Update position on server
- `ShootServerRpc()` - Notify server of shooting action
- `ShootClientRpc()` - Broadcast shooting to all clients

### 3. Gun (`Gun.cs`)
Basic shooting mechanics.

**Features:**
- Configurable fire rate
- Raycast-based shooting
- Hit detection
- Visual effects support (muzzle flash, bullet impacts)
- Audio support

**Key Settings:**
- `fireRate` - Time between shots
- `range` - Maximum shooting range
- `damage` - Damage per shot

### 4. MultiplayerUIManager (`MultiplayerUIManager.cs`)
UI for multiplayer lobby and connection management.

**Features:**
- Host game button
- Join game button
- Disconnect button
- Status text display
- Player count display
- Current player ID display

## Setup Instructions

### 1. Package Requirements
Add to `Packages/manifest.json`:
```json
"com.unity.netcode.gameobjects": "2.2.1"
```

### 2. Scene Setup

#### Create NetworkManager GameObject
1. Create empty GameObject named "NetworkManager"
2. Add `NetworkManager` component (from Unity Netcode)
3. Configure NetworkManager:
   - Set Transport to `UnityTransport`
   - Configure connection settings (default: localhost, port 7777)

#### Create MultiplayerManager GameObject
1. Create empty GameObject named "MultiplayerManager"
2. Add `MultiplayerManager` component
3. Assign player prefab (see below)
4. Configure spawn points or let it auto-generate

#### Create Player Prefab
1. Create player GameObject with:
   - Rigidbody (kinematic = false)
   - NetworkObject component
   - NetworkPlayer component
   - Collider (e.g., CapsuleCollider)
   - Visual mesh (e.g., Capsule)
2. Add Gun as child GameObject
3. Configure NetworkPlayer settings
4. Save as prefab in Assets/Prefabs/
5. Assign to NetworkManager's player prefab list

#### Setup UI
1. Create Canvas with MultiplayerUIManager
2. Create UI elements:
   - Lobby Panel with Host/Join buttons
   - Gameplay Panel with player info
   - Status text and player count displays
3. Assign UI references in MultiplayerUIManager

### 3. Network Configuration

#### NetworkManager Settings
- **Transport**: UnityTransport
- **Player Prefabs**: Add NetworkPlayer prefab
- **Network Prefabs**: Add any networkable prefabs (projectiles, effects, etc.)

#### Transport Settings (UnityTransport)
- **Protocol Type**: UDP (default)
- **Address**: 127.0.0.1 (localhost for testing)
- **Port**: 7777 (default)

## Usage

### Starting a Game

**As Host:**
1. Click "Host" button in lobby
2. Wait for clients to connect
3. Game starts automatically

**As Client:**
1. Ensure host IP is configured (default: localhost)
2. Click "Join" button in lobby
3. Connect to host

### Controls

**Movement:**
- WASD or Arrow Keys - Move
- Space - Jump

**Actions:**
- Left Mouse Button / Fire1 - Shoot

### Testing Multiplayer Locally

#### Option 1: ParrelSync (Recommended)
1. Install ParrelSync from Package Manager
2. Create clone project
3. Run host in main editor
4. Run client in cloned editor

#### Option 2: Build and Editor
1. Build standalone executable
2. Run host in editor
3. Run client(s) in built executable(s)

## Network Synchronization

### Player Movement
- Owner client updates position locally
- Position sent to server via ServerRpc
- Server broadcasts to all clients via NetworkVariable
- Non-owner clients interpolate to network position

### Shooting
1. Owner client detects shoot input
2. Owner performs local shoot immediately
3. Owner sends ShootServerRpc to server
4. Server broadcasts ShootClientRpc to all clients
5. Non-owner clients play shoot effects

### Player Identification
- Each player receives unique ID based on ClientId
- Player materials can be customized per player
- Player ID displayed in UI

## Future Enhancements

### Planned Features
- [ ] Player health system
- [ ] Damage synchronization
- [ ] Different weapon types
- [ ] Power-ups
- [ ] Score tracking and leaderboard
- [ ] Player respawning
- [ ] Match timer and win conditions
- [ ] Lobby system with player names
- [ ] Internet connectivity (relay servers)
- [ ] Dedicated server support

### Network Optimization Ideas
- [ ] Client-side prediction for smoother movement
- [ ] Lag compensation for shooting
- [ ] State synchronization optimization
- [ ] Bandwidth usage optimization

## Troubleshooting

### Common Issues

**Players not spawning:**
- Ensure player prefab has NetworkObject component
- Check player prefab is added to NetworkManager
- Verify spawn points are configured

**Connection failed:**
- Check firewall settings
- Verify IP address and port
- Ensure NetworkManager is in scene
- Check transport settings

**Shooting not synchronized:**
- Verify Gun component is attached
- Check ServerRpc/ClientRpc methods are called
- Ensure NetworkObject is spawned

**Position desync:**
- Increase interpolation speed in NetworkPlayer
- Check network conditions
- Verify UpdatePositionServerRpc is called in FixedUpdate

## Technical Notes

### Network Variables
- Automatically synchronized by Netcode
- Updated on value change
- Can trigger callbacks (OnValueChanged)

### RPCs (Remote Procedure Calls)
- **ServerRpc**: Client → Server
- **ClientRpc**: Server → All Clients
- Used for events and commands

### Authority
- Server has authority over game state
- Clients send inputs, server validates
- Reduces cheating opportunities

## References
- [Unity Netcode for GameObjects Documentation](https://docs-multiplayer.unity3d.com/netcode/current/about/)
- [Unity Multiplayer Best Practices](https://docs-multiplayer.unity3d.com/netcode/current/learn/bossroom/)
