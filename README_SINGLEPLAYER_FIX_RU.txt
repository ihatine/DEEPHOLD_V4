OUT OF SYNC — 2.5D SINGLEPLAYER TEST BUILD
==============================================

Unity: 6000.0.59f1
Default mode: SINGLEPLAYER.

WHAT WAS FIXED
--------------
1. GameBootstrap no longer creates NetworkBootstrap automatically.
2. Therefore a NetworkManager cannot become nested under NetworkRuntime.
3. Singleplayer creates one local player without starting Netcode.
4. Player movement/jump works in standalone mode.
5. Mining/placing works in standalone mode.
6. Inventory has local values for offline mode.
7. Combat has a local path for offline mode.
8. HUD no longer calls StartHost/StartClient in the default build.
9. NetworkBootstrap itself was also corrected: if used later, NetworkManager is created at the scene root.
10. The project remains prepared for a later multiplayer pass.

CONTROLS
--------
A/D or Left/Right  - move
Space              - jump
Left Mouse         - mine
Right Mouse        - place block
F                  - attack

IMPORTANT
---------
Do not add a NetworkManager manually to TestScene while testing singleplayer.
The current build intentionally runs with no NetworkManager.

If Unity has an old Library folder from a previous broken version, close Unity
and delete Library before opening this copy. Unity will regenerate it.
