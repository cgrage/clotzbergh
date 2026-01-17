# Clotzbergh

## Status Quo

![Screenshot of the game](doc/game-screen.png)

## Key Bindings

| Action | Button | Comment |
|--------|--------|---------|
| Movement | W-A-S-D or arrow keys | Input.GetAxis |
| Jump | Space-Bar | Input.GetButton("Jump") |
| Run | Left Shift | |
| Crouch | R | |
| Selection Mode | Mouse Wheel | |
| Toggle Studs | F11 | |
| Toggle Debug Panel | F12 | |

## TODOs

List of TODOs

Documentation:
- ☐ Write some initial documentation

Code:
- ☐ Apply and verify some coding guidelines and style
  - ☐ Casing for names of public fields

Game-play:
- ☑ Add colors to klotzes (2024-Nov-03)
- ☑ Add per klotz color variants (2024-Nov-12)
- ☑ Multiplayer (level 1) (2024-Nov-16)
  - ☑ Client: Update server about real position, often (2024-Nov-15)
  - ☑ Server: Add server status (2024-Nov-15)
  - ☑ Server: Add other players list (2024-Nov-16)
  - ☑ Client: Display other players (2024-Nov-16)
- Server-side features
  - ☑ Saving and loading of the world (2024-Dec-10)
  - ☐ Thread-safe world updates
- ☐ Player interaction with game (level 1)
  - ☐ Multi-klotz collection
    - ☐ Cutout for multi-klotz selection
  - ☐ Placing klotzes

Deployment:
- ☐ How to bundle the game?

Learn:
- https://github.com/mxgmn/WaveFunctionCollapse
