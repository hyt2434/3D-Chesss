using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum SpecialMove
{
    Nothing = 0,
    enPassant,
    Castling,
    Promotion
}
public class Chessboard : MonoBehaviour
{
    [Header("Art stuff")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private float yOffset = 0.2f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;
    [SerializeField] private float deadSize = 0.3f;
    [SerializeField] private float deadSpacing = 0.3f;
    [SerializeField] private float dragOffset = 1.5f;
    [SerializeField] private GameObject winningScreen;


    [Header("Prefabs & Materials")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;
    
    [Header("Bot Settings")]
    [SerializeField] private ChessAI chessAI;
    [SerializeField] private float botMoveDelay = 1.0f;
    [SerializeField] private bool showBotThinkingIndicator = true;
    
    // LOGIC
    private ChessPiece[,] chessPieces;
    private ChessPiece currentlyDragging;
    private List<Vector2Int> availableMoves = new List<Vector2Int>();
    private List<ChessPiece> deadWhites = new List<ChessPiece>();
    private List<ChessPiece> deadBlacks = new List<ChessPiece>();
    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;
    private GameObject[,] tiles;
    private Camera currentCamera;
    private Vector2Int currentHover = -Vector2Int.one;
    private Vector3 bounds;
    private SpecialMove specialMoves;
    private bool isItWhiteTurn;
    private List<Vector2Int[]> moveList = new List<Vector2Int[]>();
    public GameTimer timerManager;
    public ScoreManager scoreManager;
    
    // Bot game variables
    private bool isBotGame = false;
    private bool isBotTurn = false;
    private bool isPlayerWhite = true;
    private bool isBotThinking = false;
    private void Awake()
    {
        // Ensure only one Chessboard is active at a time
        // This prevents conflicts when multiple Game scenes might be loaded
        Chessboard[] existingChessboards = FindObjectsOfType<Chessboard>();
        foreach (Chessboard board in existingChessboards)
        {
            if (board != this && board.gameObject.scene != this.gameObject.scene)
            {
                Debug.Log($"Destroying old Chessboard from scene: {board.gameObject.scene.name}");
                Destroy(board.gameObject);
            }
        }

        // Initialize all game state variables to ensure clean start
        InitializeGameState();

        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);
        SpawnAllPieces();
        PositionAllPieces();
    }

    // Initialize all game state variables for a fresh game
    private void InitializeGameState()
    {
        Debug.Log("Initializing fresh game state for Chessboard");
        
        // Reset turn state
        isItWhiteTurn = true;
        
        // Reset dragging state - this is crucial for pickup animation
        currentlyDragging = null;
        currentHover = -Vector2Int.one;
        
        // Clear move lists
        availableMoves.Clear();
        moveList.Clear();
        deadWhites.Clear();
        deadBlacks.Clear();
        
        // Reset special move state
        specialMoves = SpecialMove.Nothing;
        
        // Clear camera reference to force re-detection
        currentCamera = null;
        
        // Initialize bot game settings
        InitializeBotGame();
        
        Debug.Log($"Game state initialized - dragOffset: {dragOffset}, currentlyDragging: {currentlyDragging}");
    }

    private void Update()
    {
        if (!currentCamera)
        {
            Camera[] cameras = FindObjectsOfType<Camera>();
            foreach (Camera cam in cameras)
            {
                if (cam.gameObject.scene == this.gameObject.scene && cam.enabled)
                {
                    currentCamera = cam;
                    Debug.Log($"Found camera for chessboard: {cam.name} in scene {cam.gameObject.scene.name}");
                    break;
                }
            }
            
            // Fallback to Camera.main if no camera found in current scene
            if (!currentCamera)
            {
                currentCamera = Camera.main;
                if (currentCamera)
                {
                    Debug.Log($"Using Camera.main as fallback: {currentCamera.name}");
                }
            }
            
            if (!currentCamera) 
            {
                Debug.LogError("No camera found for chessboard! Pickup animation will not work.");
                return;
            }
        }

        // Check if it's bot's turn
        if (isBotGame && isBotTurn)
        {
            return; // Don't process player input during bot's turn
        }

        // Check if player can make a move
        if (isBotGame && !CanPlayerMove())
        {
            return; // Don't process player input if it's not their turn or bot is thinking
        }

        RaycastHit info;
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight")))
        {
            Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);

            if (currentHover == -Vector2Int.one)
            {
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }

            // If we were already hovering a tile, change the previous one 
            if (currentHover != hitPosition)
            {
                tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }
            // if we press down on the mouse
            if (Input.GetMouseButtonDown(0))
            {
                if (chessPieces[hitPosition.x, hitPosition.y] != null)
                {
                    bool canSelectPiece = false;
                    
                    if (isBotGame)
                    {
                        // In bot games, check if it's the player's turn and the piece belongs to the player
                        canSelectPiece = ((chessPieces[hitPosition.x, hitPosition.y].team == 0 && isItWhiteTurn && isPlayerWhite) || 
                                        (chessPieces[hitPosition.x, hitPosition.y].team == 1 && !isItWhiteTurn && !isPlayerWhite));
                    }
                    else
                    {
                        // In multiplayer games, check if it's the correct player's turn
                        canSelectPiece = ((chessPieces[hitPosition.x, hitPosition.y].team == 0 && isItWhiteTurn) || 
                                        (chessPieces[hitPosition.x, hitPosition.y].team == 1 && !isItWhiteTurn));
                    }
                    
                    if (canSelectPiece)
                    {
                        currentlyDragging = chessPieces[hitPosition.x, hitPosition.y];
                        
                        // Debug log when piece is selected for dragging
                        Debug.Log($"Selected piece for dragging: {currentlyDragging.name} at ({hitPosition.x},{hitPosition.y}), dragOffset={dragOffset}");

                        // get a list of available moves for this piece, highlight tiles as well
                        availableMoves = currentlyDragging.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                        // get a list of special moves
                        specialMoves = currentlyDragging.GetSpecialMoves(ref chessPieces, ref moveList, ref availableMoves);
                        PreventingCheck();
                        HighlightTiles();
                    }
                }
            }
            // if we are releasing the mouse
            if (currentlyDragging != null && Input.GetMouseButtonUp(0))
            {
                Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);

                bool validMove = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y);
                if (!validMove)
                {
                    currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y));
                    currentlyDragging = null;
                }
                currentlyDragging = null;
                RemoveHighlightTiles();
            }
        }
        else
        {
            if (currentHover != -Vector2Int.one)
            {
                tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                currentHover = -Vector2Int.one;
            }
            if (currentlyDragging && Input.GetMouseButtonUp(0))
            {
                // If we are dragging a piece and release the mouse, reset it
                currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY));
                currentlyDragging = null;
                RemoveHighlightTiles();
            }
        }

        // If we are dragging a piece, update its position
        if (currentlyDragging)
        {
            Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * yOffset);
            float distance = 0.0f;
            if (horizontalPlane.Raycast(ray, out distance))
            {
                Vector3 targetPosition = ray.GetPoint(distance) + Vector3.up * dragOffset;
                currentlyDragging.SetPosition(targetPosition);
                
                // Debug log to track pickup animation (only log occasionally to avoid spam)
                if (Time.frameCount % 30 == 0) // Log every 30 frames
                {
                    Debug.Log($"Dragging piece {currentlyDragging.name}: dragOffset={dragOffset}, targetPos={targetPosition}, rayPoint={ray.GetPoint(distance)}");
                }
            }
            else
            {
                Debug.LogWarning($"Failed to raycast for dragging piece {currentlyDragging.name}");
            }
        }
    }


    private void GenerateAllTiles(float tileSize, int tileCountX, int tileCountY)
    {
        yOffset += transform.position.y;
        bounds = new Vector3((tileCountX / 2) * tileSize, 0, (tileCountX / 2) * tileSize) + boardCenter;


        tiles = new GameObject[tileCountX, tileCountY];
        for (int x = 0; x < tileCountX; x++)
            for (int y = 0; y < tileCountY; y++)
                tiles[x, y] = GenerateSingleTile(tileSize, x, y);
    }

    private GameObject GenerateSingleTile(float tileSize, int x, int y)
    {
        GameObject tileObject = new GameObject($"X:{x}, Y:{y}");
        tileObject.transform.parent = transform;

        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x * tileSize, yOffset, y * tileSize) - bounds;
        vertices[1] = new Vector3(x * tileSize, yOffset, (y + 1) * tileSize) - bounds;
        vertices[2] = new Vector3((x + 1) * tileSize, yOffset, y * tileSize) - bounds;
        vertices[3] = new Vector3((x + 1) * tileSize, yOffset, (y + 1) * tileSize) - bounds;

        int[] tris = new int[] { 0, 1, 2, 1, 3, 2 };

        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.RecalculateNormals();

        tileObject.layer = LayerMask.NameToLayer("Tile");
        tileObject.AddComponent<BoxCollider>();

        return tileObject;
    }
    // Spawning the pieces
    private void SpawnAllPieces()
    {
        chessPieces = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];
        int whiteTeam = 0, blackTeam = 1;
        // White team
        chessPieces[0, 0] = SpawnSinglePiece(ChessPieceType.Rock, whiteTeam);
        chessPieces[1, 0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[2, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[3, 0] = SpawnSinglePiece(ChessPieceType.Queen, whiteTeam);
        chessPieces[4, 0] = SpawnSinglePiece(ChessPieceType.King, whiteTeam);
        chessPieces[5, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[6, 0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[7, 0] = SpawnSinglePiece(ChessPieceType.Rock, whiteTeam);
        for (int i = 0; i < TILE_COUNT_X; i++)
        {
            chessPieces[i, 1] = SpawnSinglePiece(ChessPieceType.Pawn, whiteTeam);
        }
        // black team
        chessPieces[0, 7] = SpawnSinglePiece(ChessPieceType.Rock, blackTeam);
        chessPieces[1, 7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[2, 7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[3, 7] = SpawnSinglePiece(ChessPieceType.Queen, blackTeam);
        chessPieces[4, 7] = SpawnSinglePiece(ChessPieceType.King, blackTeam);
        chessPieces[5, 7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[6, 7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[7, 7] = SpawnSinglePiece(ChessPieceType.Rock, blackTeam);
        for (int i = 0; i < TILE_COUNT_X; i++)
        {
            chessPieces[i, 6] = SpawnSinglePiece(ChessPieceType.Pawn, blackTeam);
        }
    }
    // Positioning the pieces
    private void PositionAllPieces()
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (chessPieces[x, y] != null)
                    PositionSinglePiece(x, y, true);
    }
    private void PositionSinglePiece(int x, int y, bool force = false)
    {
        if (chessPieces[x, y] == null)
        {
            Debug.LogError($"No piece at position ({x},{y}) to position!");
            return;
        }
        
        chessPieces[x, y].currentX = x;
        chessPieces[x, y].currentY = y;
        Vector3 targetPosition = GetTileCenter(x, y);
        Debug.Log($"Positioning piece {chessPieces[x, y].name} at ({x},{y}) to world position {targetPosition} in scene {gameObject.scene.name}, force={force}");
        Debug.Log($"Piece current position before SetPosition: {chessPieces[x, y].transform.position}");
        chessPieces[x, y].SetPosition(targetPosition, force);
        Debug.Log($"Piece position after SetPosition: {chessPieces[x, y].transform.position}");
    }
    private Vector3 GetTileCenter(int x, int y)
    {
        return new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
    }
    private ChessPiece SpawnSinglePiece(ChessPieceType type, int team)
    {
        ChessPiece cp = Instantiate(prefabs[(int)type - 1], transform).GetComponent<ChessPiece>();
        cp.type = type;
        cp.team = team;
        cp.GetComponent<MeshRenderer>().material = teamMaterials[team];
        return cp;
    }


    // Highlighting the tiles
    private void HighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
        {
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Highlight");
        }
    }
    private void RemoveHighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
        {
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Tile");
        }
        availableMoves.Clear();
    }

    private void CheckMate(int team)
    {
        DisplayVictory(team);
    }

    private void DisplayVictory(int victoryTeam)
    {
        winningScreen.SetActive(true);
        winningScreen.transform.GetChild(victoryTeam).gameObject.SetActive(true);
    }
    public void OnRestartButton()
    {
        // hide victory UI
        winningScreen.transform.GetChild(2).gameObject.SetActive(false);
        winningScreen.transform.GetChild(0).gameObject.SetActive(false);
        winningScreen.transform.GetChild(1).gameObject.SetActive(false);
        winningScreen.SetActive(false);

        // reset drag & move lists and all game state
        currentlyDragging = null;
        currentHover = -Vector2Int.one;
        availableMoves.Clear();
        moveList.Clear();
        specialMoves = SpecialMove.Nothing;
        
        Debug.Log("Game restarted - all dragging state reset");

        // destroy and clear pieces
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (chessPieces[x, y] != null)
                    Destroy(chessPieces[x, y].gameObject);
                chessPieces[x, y] = null;
            }
        foreach (var w in deadWhites) Destroy(w.gameObject);
        foreach (var b in deadBlacks) Destroy(b.gameObject);
        deadWhites.Clear();
        deadBlacks.Clear();

        // respawn the board
        SpawnAllPieces();
        PositionAllPieces();
        isItWhiteTurn = true;

        // **reset the timers** back to initial values
        if (timerManager != null)
            timerManager.ResetTimers();

        // **reset the scores** back to zero
        if (scoreManager != null)
            scoreManager.ResetScores();

        // Reinitialize bot game state if this is a bot game
        if (isBotGame)
        {
            InitializeBotGame();
            Debug.Log("Bot game restarted - bot state reinitialized");
        }
    }

    public void OnExitButton()
    {
        if (Time.timeScale == 0f) Time.timeScale = 1f;
        // go back to your MainMenu scene
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }

    //Features
    private void BeforeSpecialMove() //This function contains the methods of implementing special moves to the board, each special 
                                     //move has been implemented in said piece, check those scripts in "ChessPiece" folder!
    {
        if (specialMoves == SpecialMove.enPassant)
        {
            var newMove = moveList[moveList.Count - 1];
            ChessPiece myPawn = chessPieces[newMove[1].x, newMove[1].y];
            var targetPawnPosition = moveList[moveList.Count - 2];
            ChessPiece enemyPawn = chessPieces[targetPawnPosition[1].x, targetPawnPosition[1].y];
            if (myPawn.currentX == enemyPawn.currentX)
            {
                if (myPawn.currentY == enemyPawn.currentY - 1 || myPawn.currentY == enemyPawn.currentY + 1)
                {
                    if (enemyPawn.team == 0)
                    {
                        deadWhites.Add(enemyPawn);
                        Vector3 originalScale = enemyPawn.transform.localScale * 2;

                        enemyPawn.SetScale(originalScale * deadSize, true);
                        enemyPawn.SetPosition(new Vector3(8 * tileSize, yOffset - 0.05f, -1 * tileSize) - bounds
                            + new Vector3(tileSize / 2 - 0.17f, 0, tileSize / 2)
                            + (Vector3.forward * deadSpacing * 1.2f) * deadWhites.Count, true);
                    }
                    else
                    {
                        deadBlacks.Add(enemyPawn);
                        Vector3 originalScale = enemyPawn.transform.localScale * 2;

                        enemyPawn.SetScale(originalScale * deadSize, true);
                        enemyPawn.SetPosition(new Vector3(8 * tileSize, yOffset - 0.05f, -1 * tileSize) - bounds
                            + new Vector3(tileSize / 2 - 0.17f, 0, tileSize / 2)
                            + (Vector3.forward * deadSpacing * 1.2f) * deadBlacks.Count, true);
                    }
                    chessPieces[enemyPawn.currentX, enemyPawn.currentY] = null;
                } 
            }
        }

        if (specialMoves == SpecialMove.Promotion)
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1];
            ChessPiece promotedPawn = chessPieces[lastMove[1].x, lastMove[1].y]; 
            if (promotedPawn.type == ChessPieceType.Pawn)
            {
                if (promotedPawn.team == 0 && lastMove[1].y == 7) //Promotion for White
                {
                    ChessPiece pQueen = SpawnSinglePiece(ChessPieceType.Queen, 0);
                    Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                    chessPieces[lastMove[1].x, lastMove[1].y] = pQueen;
                    PositionSinglePiece(lastMove[1].x, lastMove[1].y, true);
                }
                else if (promotedPawn.team == 1 && lastMove[1].y == 0) //Promotion for Black
                {
                    ChessPiece pQueen = SpawnSinglePiece(ChessPieceType.Queen, 1);
                    Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                    chessPieces[lastMove[1].x, lastMove[1].y] = pQueen;
                    PositionSinglePiece(lastMove[1].x, lastMove[1].y, true);
                }
            }
        }

        if (specialMoves == SpecialMove.Castling)
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1];
            if (lastMove[1].x == 2)
            {
                if (lastMove[1].y == 0) //White
                {
                    ChessPiece rook = chessPieces[0, 0];
                    chessPieces[3, 0] = rook;
                    PositionSinglePiece(3, 0);
                    chessPieces[0, 0] = null;
                }
                else if (lastMove[1].y == 7) //Black
                {
                    ChessPiece rook = chessPieces[0, 7];
                    chessPieces[3, 7] = rook;
                    PositionSinglePiece(3, 7);
                    chessPieces[0, 7 ] = null;
                }

            }
            else if (lastMove[1].x == 6)
            {
                if (lastMove[1].y == 0) //White
                {
                    ChessPiece rook = chessPieces[7, 0];
                    chessPieces[5, 0] = rook;
                    PositionSinglePiece(5, 0);
                    chessPieces[7, 0] = null;
                }
                else if (lastMove[1].y == 7) //Black
                {
                    ChessPiece rook = chessPieces[7, 7];
                    chessPieces[5, 7] = rook;
                    PositionSinglePiece(5, 7);
                    chessPieces[7, 7] = null;
                }
            }
        }
    }
    private void PreventingCheck() //This function prevents player from being checked by their own moves
    {
        ChessPiece targetKing = null;
        for (int i = 0; i < TILE_COUNT_X; i++)
            for (int j = 0; j < TILE_COUNT_Y; j++)
                if (chessPieces[i, j] != null)
                    if (chessPieces[i, j].type == ChessPieceType.King)
                        if (chessPieces[i, j].team == currentlyDragging.team)
                            targetKing = chessPieces[i, j];
        
        //We will be deleting moves that are putting us in check since we are sending reference of availableMoves
        SimulateMoveForSinglePiece(currentlyDragging, ref availableMoves, targetKing);
    }
    private void SimulateMoveForSinglePiece(ChessPiece cp, ref List<Vector2Int> moves, ChessPiece targetKing)
    {
        //Save the current values to reset the function call;
        int actualX = cp.currentX;
        int actualY = cp.currentY;
        List<Vector2Int> movesToRemove = new List<Vector2Int>();

        //Going through all the moves and simulate them, see if we are in check;
        for (int i = 0; i < moves.Count; i++)
        {
            int simX = moves[i].x;
            int simY = moves[i].y;
            Vector2Int kingPosInThisSim = new Vector2Int(targetKing.currentX, targetKing.currentY); 
            //Check if we have simulated the king's move or haven't
            if (cp.type == ChessPieceType.King)
                kingPosInThisSim = new Vector2Int(simX, simY);
            //Copy the [,] and not a reference
            ChessPiece[,] simulation = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];
            List<ChessPiece> simAttackingPieces = new List<ChessPiece>();
            for (int x = 0; x < TILE_COUNT_X; x++)
            {

                for (int y = 0; y < TILE_COUNT_Y; y++)
                {
                    if (chessPieces[x, y] != null)
                    {
                        simulation[x, y] = chessPieces[x, y];
                        if(simulation[x, y].team != cp.team)
                        {
                            simAttackingPieces.Add(simulation[x, y]);
                        }
                    }
                }
            }

            //Simulate the move
            simulation[actualX, actualY] = null;
            cp.currentX = simX;
            cp.currentY = simY;
            simulation[simX, simY] = cp;
            //Did one of the piece got  taken down after the simulation
            var deadPiece = simAttackingPieces.Find(c => c.currentX == simX && c.currentY == simY);
            if (deadPiece != null)
                simAttackingPieces.Remove(deadPiece);

            //Get all that simulated attacking pieces moves
            List<Vector2Int> simulatedMoves = new List<Vector2Int>();
            for (int z = 0; z < simAttackingPieces.Count; z++)
            {
                var simPieceMove = simAttackingPieces[z].GetAvailableMoves(ref simulation, TILE_COUNT_X, TILE_COUNT_Y);
                for (int v = 0; v < simPieceMove.Count; v++)
                    simulatedMoves.Add(simPieceMove[v]);
            }

            //Is the king being checked if we made that move? 
            if (ContainsValidMove(ref simulatedMoves, kingPosInThisSim))
            {
                movesToRemove.Add(moves[i]);
            }

            //Restore the actual position
            cp.currentX = actualX;
            cp.currentY = actualY;
        }

        //Removing the current available move that pputs us in check
        for (int i = 0; i < movesToRemove.Count; i++)
        {
            moves.Remove(movesToRemove[i]);
        }
    }
    private int CHECKMATE()
    {
        // Safety check: ensure moveList has moves and chessPieces is initialized
        if (moveList == null || moveList.Count == 0 || chessPieces == null)
        {
            Debug.LogWarning("CHECKMATE: moveList or chessPieces not properly initialized");
            return 0;
        }

        var lastMove = moveList[moveList.Count - 1];
        
        // Safety check: ensure the move coordinates are valid
        if (lastMove == null || lastMove.Length < 2 || 
            lastMove[1].x < 0 || lastMove[1].x >= TILE_COUNT_X || 
            lastMove[1].y < 0 || lastMove[1].y >= TILE_COUNT_Y ||
            chessPieces[lastMove[1].x, lastMove[1].y] == null)
        {
            Debug.LogWarning("CHECKMATE: Invalid last move or piece position");
            return 0;
        }

        int targetTeam = (chessPieces[lastMove[1].x, lastMove[1].y].team == 0) ? 1 : 0;

        List<ChessPiece> attackingPieces = new List<ChessPiece>();
        List<ChessPiece> defendingPieces = new List<ChessPiece>();
        ChessPiece targetKing = null;
        
        for (int i = 0; i < TILE_COUNT_X; i++)
            for (int j = 0; j < TILE_COUNT_Y; j++)
                if (chessPieces[i, j] != null)
                {
                    if (chessPieces[i, j].team == targetTeam)
                    {
                        defendingPieces.Add(chessPieces[i, j]);
                        if (chessPieces[i, j].type == ChessPieceType.King)
                            targetKing = chessPieces[i, j];
                    }
                    else
                    {
                        attackingPieces.Add(chessPieces[i, j]);
                    }
                }

        // Safety check: ensure we found a target king
        if (targetKing == null)
        {
            Debug.LogWarning($"CHECKMATE: No king found for team {targetTeam}");
            return 0;
        }

        //Is the king being attacked? 
        List<Vector2Int> currentAvailableMoves = new List<Vector2Int>();
        for (int i = 0; i < attackingPieces.Count; i++)
        {
            if (attackingPieces[i] != null)
            {
                var pieceMove = attackingPieces[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                for (int b = 0; b < pieceMove.Count; b++)
                    currentAvailableMoves.Add(pieceMove[b]);
            }
        }
        
        //Are we in check? 
        if (ContainsValidMove(ref currentAvailableMoves, new Vector2Int(targetKing.currentX, targetKing.currentY)))
        {
            for (int i = 0; i < defendingPieces.Count; i++)
            {
                if (defendingPieces[i] != null)
                {
                    List<Vector2Int> defendingMoves = defendingPieces[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                    SimulateMoveForSinglePiece(defendingPieces[i], ref defendingMoves, targetKing);

                    if (defendingMoves.Count != 0)
                        return 0;
                }
            }
            return 1; //checkmate
        }
        else
        {
            for (int i = 0; i < defendingPieces.Count; i++)
            {
                if (defendingPieces[i] != null)
                {
                    List<Vector2Int> defendingMoves = defendingPieces[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                    SimulateMoveForSinglePiece(defendingPieces[i], ref defendingMoves, targetKing);
                    if (defendingMoves.Count != 0)
                        return 0;
                }
            }
            return 2; //staleMate Exit
        }
    }

    // Operations

    private bool ContainsValidMove(ref List<Vector2Int> moves, Vector2 pos)
    {
        for (int i = 0; i < moves.Count; i++)
        {
            if (moves[i].x == pos.x && moves[i].y == pos.y)
            {
                return true;
            }
        }
        return false;
    }

    private Vector2Int LookupTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (tiles[x, y] == hitInfo)
                    return new Vector2Int(x, y);

        return -Vector2Int.one; // Invalid
    }

    // Bot game methods
    public void SetBotGameMode(bool enabled, bool playerIsWhite = true)
    {
        isBotGame = enabled;
        isPlayerWhite = playerIsWhite;
        isBotTurn = false;
        isBotThinking = false;
        
        if (enabled)
        {
            Debug.Log($"Bot game mode {(enabled ? "enabled" : "disabled")} - Player is {(playerIsWhite ? "White" : "Black")}");
        }
    }

    public bool IsBotGame() => isBotGame;
    public bool IsBotTurn() => isBotTurn;
    public bool IsPlayerWhite() => isPlayerWhite;
    public bool IsBotThinking() => isBotThinking;
    
    public void ForceBotTurn()
    {
        if (isBotGame && !isBotThinking)
        {
            isBotTurn = true;
            Invoke(nameof(MakeBotMove), botMoveDelay);
            Debug.Log("Bot turn forced manually");
        }
        else
        {
            Debug.LogWarning("Cannot force bot turn - conditions not met");
        }
    }

    public void SetBotMoveDelay(float delay)
    {
        botMoveDelay = Mathf.Max(0.1f, delay); // Minimum 0.1 second delay
        Debug.Log($"Bot move delay set to {botMoveDelay} seconds");
    }

    public float GetBotMoveDelay() => botMoveDelay;

    public void StopBot()
    {
        isBotTurn = false;
        isBotThinking = false;
        CancelInvoke(nameof(MakeBotMove));
        Debug.Log("Bot stopped");
    }

    public void ResumeBot()
    {
        if (isBotGame && !isBotThinking)
        {
            bool shouldBeBotTurn = (isItWhiteTurn && !isPlayerWhite) || (!isItWhiteTurn && isPlayerWhite);
            if (shouldBeBotTurn)
            {
                isBotTurn = true;
                Invoke(nameof(MakeBotMove), botMoveDelay);
                Debug.Log("Bot resumed");
            }
        }
    }

    public string GetGameStatus()
    {
        if (!isBotGame)
            return "Multiplayer Game";
        
        string playerSide = isPlayerWhite ? "White" : "Black";
        string currentTurn = isItWhiteTurn ? "White" : "Black";
        string botStatus = isBotThinking ? " (Bot thinking...)" : "";
        
        return $"Bot Game - You are {playerSide}, {currentTurn}'s turn{botStatus}";
    }

    public bool CanPlayerMove()
    {
        if (!isBotGame)
            return true; // In multiplayer, players can always move on their turn
        
        // Check if it's the player's turn and the bot is not thinking
        bool isPlayerTurn = (isItWhiteTurn && isPlayerWhite) || (!isItWhiteTurn && !isPlayerWhite);
        return isPlayerTurn && !isBotThinking;
    }

    public void SetBotDifficulty(int depth)
    {
        if (chessAI != null)
        {
            // The ChessAI uses depth parameter for difficulty
            // Higher depth = stronger bot but slower moves
            depth = Mathf.Clamp(depth, 1, 5); // Limit depth between 1-5 for reasonable performance
            Debug.Log($"Bot difficulty set to depth {depth}");
        }
        else
        {
            Debug.LogWarning("Cannot set bot difficulty - ChessAI not assigned");
        }
    }

    public int GetBotDifficulty()
    {
        if (chessAI != null)
        {
            // Return the current depth setting (default is 3)
            return 3; // This could be made configurable in the future
        }
        return 0; // No AI available
    }

    public bool IsBotAvailable()
    {
        return chessAI != null;
    }

    public string GetBotInfo()
    {
        string info = $"Bot Game: {isBotGame}\n";
        info += $"Bot Turn: {isBotTurn}\n";
        info += $"Player White: {isPlayerWhite}\n";
        info += $"Bot Thinking: {isBotThinking}\n";
        info += $"Current Turn: {(isItWhiteTurn ? "White" : "Black")}\n";
        info += $"ChessAI Assigned: {chessAI != null}\n";
        info += $"Bot Move Delay: {botMoveDelay}\n";
        info += $"GameManager SinglePlayer: {(GameManager.Instance != null ? GameManager.Instance.isSinglePlayerMode : "null")}\n";
        info += $"GameManager PlayerWhite: {(GameManager.Instance != null ? GameManager.Instance.isPlayerWhite : "null")}";
        
        return info;
    }

    private void InitializeBotGame()
    {
        if (GameManager.Instance != null && GameManager.Instance.isSinglePlayerMode)
        {
            isBotGame = true;
            isPlayerWhite = GameManager.Instance.isPlayerWhite;
            isBotThinking = false;
            
            // Auto-assign ChessAI if not already assigned
            if (chessAI == null)
            {
                AutoAssignChessAI();
            }
            
            // Reset opening strategy for variety
            if (chessAI != null)
            {
                // Force the ChessAI to choose a new opening strategy
                chessAI.ResetOpeningStrategy();
            }
            
            // Cancel any existing bot move invocations to prevent duplicates
            CancelInvoke(nameof(MakeBotMove));
            
            // If player is black, bot goes first
            if (!isPlayerWhite)
            {
                isBotTurn = true;
                Invoke(nameof(MakeBotMove), botMoveDelay);
            }
            
            Debug.Log($"Bot game initialized - Player is {(isPlayerWhite ? "White" : "Black")}, Bot goes {(isPlayerWhite ? "second" : "first")}");
        }
        else
        {
            isBotGame = false;
            isBotTurn = false;
            isBotThinking = false;
            CancelInvoke(nameof(MakeBotMove));
            Debug.Log("Multiplayer game - no bot");
        }
    }

    private void AutoAssignChessAI()
    {
        // First try to find existing ChessAI in the scene
        ChessAI existingAI = FindObjectOfType<ChessAI>();
        if (existingAI != null)
        {
            chessAI = existingAI;
            Debug.Log($"Auto-assigned ChessAI: {existingAI.name}");
            return;
        }

        // If no existing ChessAI, create one on this GameObject
        chessAI = gameObject.AddComponent<ChessAI>();
        Debug.Log("Created new ChessAI component on Chessboard");
        
        // Verify the component was added successfully
        if (chessAI == null)
        {
            Debug.LogError("Failed to create ChessAI component!");
        }
        else
        {
            Debug.Log("ChessAI component successfully created and assigned");
        }
    }

    private void Start()
    {
        // Initialize bot game if this is a single player game
        if (GameManager.Instance != null && GameManager.Instance.isSinglePlayerMode)
        {
            InitializeBotGame();
        }
    }

    public void InitializeBotGameManually()
    {
        Debug.Log("Manually initializing bot game...");
        if (GameManager.Instance != null && GameManager.Instance.isSinglePlayerMode)
        {
            InitializeBotGame();
        }
        else
        {
            Debug.LogWarning("Cannot initialize bot game - GameManager not found or not in single player mode");
        }
    }

    private void MakeBotMove()
    {
        if (!isBotGame || !isBotTurn || isBotThinking)
        {
            Debug.LogWarning($"Cannot make bot move - conditions not met:");
            Debug.LogWarning($"  isBotGame: {isBotGame}");
            Debug.LogWarning($"  isBotTurn: {isBotTurn}");
            Debug.LogWarning($"  isBotThinking: {isBotThinking}");
            return;
        }

        // Auto-assign ChessAI if still null
        if (chessAI == null)
        {
            Debug.LogWarning("ChessAI is null, attempting to auto-assign...");
            AutoAssignChessAI();
            
            // If still null after auto-assignment, give up
            if (chessAI == null)
            {
                Debug.LogError("Failed to assign ChessAI component. Bot cannot make moves.");
                isBotThinking = false;
                return;
            }
        }

        // Pass current move count to ChessAI for opening detection
        chessAI.SetMoveCount(moveList.Count);

        isBotThinking = true;
        Debug.Log("Bot is thinking...");
        
        // Find the best move for the bot
        ChessAI.AIMove bestMove = chessAI.FindBestMove(chessPieces, isItWhiteTurn);
        
        if (bestMove.piece != null)
        {
            Debug.Log($"Bot found move: {bestMove.piece.type} from ({bestMove.piece.currentX},{bestMove.piece.currentY}) to ({bestMove.targetPosition.x},{bestMove.targetPosition.y})");
            ExecuteBotMove(bestMove.piece, bestMove.targetPosition.x, bestMove.targetPosition.y);
        }
        else
        {
            Debug.LogWarning("Bot could not find a valid move!");
            isBotThinking = false;
        }
    }

    private void ExecuteBotMove(ChessPiece piece, int targetX, int targetY)
    {
        // Store the piece's current position
        Vector2Int previousPosition = new Vector2Int(piece.currentX, piece.currentY);
        
        // Execute the move using the existing MoveTo logic but bypass bot turn logic
        bool validMove = ExecuteMoveDirectly(piece, targetX, targetY);
        
        if (validMove)
        {
            // Move was successful, now it's the player's turn
            isBotTurn = false;
            isBotThinking = false;
            Debug.Log("Bot move completed successfully");
        }
        else
        {
            Debug.LogError("Bot move failed - this shouldn't happen!");
            // Reset the piece position if move failed
            piece.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y));
            isBotTurn = false;
            isBotThinking = false;
        }
    }

    private bool ExecuteMoveDirectly(ChessPiece cp, int x, int y)
    {
        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);
        
        // Is there another piece at the target position?
        if (chessPieces[x, y] != null)
        {
            ChessPiece ocp = chessPieces[x, y];
            if (cp.team == ocp.team)
            {
                return false; 
            }
            int value = ocp.type switch
            {
                ChessPieceType.Pawn => 1,
                ChessPieceType.Knight => 3,
                ChessPieceType.Bishop => 3,
                ChessPieceType.Rock => 5,
                ChessPieceType.Queen => 9,
                _ => 0
            };

            // award points
            if (scoreManager != null)
            {
                scoreManager.AddPoints(cp.team, value);
                Debug.Log($"Awarded {value} points to {(cp.team == 0 ? "White" : "Black")}");
            }

            // If its enemy piece, we can capture it
            if (ocp.team == 0)
            {
                if (ocp.type == ChessPieceType.King)
                {
                    CheckMate(1);
                }
                deadWhites.Add(ocp);
                Vector3 originalScale = ocp.transform.localScale * 2;

                ocp.SetScale(originalScale * deadSize, true);
                ocp.SetPosition(new Vector3(8 * tileSize, yOffset - 0.05f, -1 * tileSize) - bounds
                    + new Vector3(tileSize / 2 - 0.17f , 0, tileSize / 2)
                    + (Vector3.forward * deadSpacing * 1.2f) * deadWhites.Count, true);
            }
            else 
            {
                if (ocp.type == ChessPieceType.King)
                {
                    CheckMate(0);
                }
                deadBlacks.Add(ocp);
                Vector3 originalScale = ocp.transform.localScale * 2;

                ocp.SetScale(originalScale * deadSize, true);
                ocp.SetPosition(new Vector3(-1 * tileSize, yOffset - 0.05f, 8 * tileSize) - bounds
                    + new Vector3(tileSize / 2 + 0.17f, 0, tileSize / 2)
                    + (Vector3.back * deadSpacing * 1.2f) * deadBlacks.Count, true);
            }
        }
        
        chessPieces[x, y] = cp;
        chessPieces[previousPosition.x, previousPosition.y] = null;

        PositionSinglePiece(x, y, true);

        isItWhiteTurn = !isItWhiteTurn;
        if (timerManager != null)
        {
            timerManager.SwitchTimer();
        }

        moveList.Add(new Vector2Int[] { previousPosition, new Vector2Int(x, y) });

        BeforeSpecialMove();

        CHECKMATE();

        switch (CHECKMATE())
        {
            default:
                break;
            case 1:
                CheckMate(cp.team);
                break;
            case 2:
                CheckMate(2);
                break;
        }

        return true;
    }

    // Override MoveTo to handle bot turns
    private bool MoveTo(ChessPiece cp, int x, int y)
    {
        if (!ContainsValidMove(ref availableMoves, new Vector2Int(x, y)))
        {
            return false; // Invalid move
        }

        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);
        // Is there another piece at the target position?
        if (chessPieces[x, y] != null)
        {
            ChessPiece ocp = chessPieces[x, y];
            if (cp.team == ocp.team)
            {
                return false; 
            }
            int value = ocp.type switch
            {
                ChessPieceType.Pawn => 1,
                ChessPieceType.Knight => 3,
                ChessPieceType.Bishop => 3,
                ChessPieceType.Rock => 5,  // your "Rock" is a Rook
                ChessPieceType.Queen => 9,
                _ => 0
            };

            // award points
            if (scoreManager != null)
            {
                scoreManager.AddPoints(cp.team, value);
                Debug.Log($"Awarded {value} points to {(cp.team == 0 ? "White" : "Black")}");
            }
            else
            {
                Debug.LogError("ScoreManager is null on Chessboard! Did you assign it in the Inspector?");
            }

            // If its enemy piece, we can capture it
            if (ocp.team == 0)
            {
                if (ocp.type == ChessPieceType.King)
                {
                    CheckMate(1);
                }
                deadWhites.Add(ocp);
                Vector3 originalScale = ocp.transform.localScale * 2;

                ocp.SetScale(originalScale * deadSize, true);
                ocp.SetPosition(new Vector3(8 * tileSize, yOffset - 0.05f, -1 * tileSize) - bounds
                    + new Vector3(tileSize / 2 - 0.17f , 0, tileSize / 2)
                    + (Vector3.forward * deadSpacing * 1.2f) * deadWhites.Count, true);
            }
            else 
            {
                if (ocp.type == ChessPieceType.King)
                {
                    CheckMate(0);
                }
                deadBlacks.Add(ocp);
                Vector3 originalScale = ocp.transform.localScale * 2;

                ocp.SetScale(originalScale * deadSize, true);
                ocp.SetPosition(new Vector3(-1 * tileSize, yOffset - 0.05f, 8 * tileSize) - bounds
                    + new Vector3(tileSize / 2 + 0.17f, 0, tileSize / 2)
                    + (Vector3.back * deadSpacing * 1.2f) * deadBlacks.Count, true);
            }
        }
        chessPieces[x, y] = cp;
        chessPieces[previousPosition.x, previousPosition.y] = null;

        PositionSinglePiece(x, y, true); // Force immediate positioning instead of lerping

        isItWhiteTurn = !isItWhiteTurn;
        if (timerManager != null)
        {
            timerManager.SwitchTimer();
        }

        moveList.Add(new Vector2Int[] { previousPosition, new Vector2Int(x, y) });

        BeforeSpecialMove();

        CHECKMATE();

        switch (CHECKMATE())
        {
            default:
                break;
            case 1:
                CheckMate(cp.team);
                break;
            case 2:
                CheckMate(2);
                break;
        }

        // Check if it's now the bot's turn
        if (isBotGame && !isBotTurn)
        {
            bool isCurrentTurnBot = (isItWhiteTurn && !isPlayerWhite) || (!isItWhiteTurn && isPlayerWhite);
            if (isCurrentTurnBot)
            {
                isBotTurn = true;
                Invoke(nameof(MakeBotMove), botMoveDelay);
            }
        }

        return true;
    }

    public void DebugBotState()
    {
        Debug.Log("=== BOT STATE DEBUG ===");
        Debug.Log($"isBotGame: {isBotGame}");
        Debug.Log($"isBotTurn: {isBotTurn}");
        Debug.Log($"isPlayerWhite: {isPlayerWhite}");
        Debug.Log($"isBotThinking: {isBotThinking}");
        Debug.Log($"isItWhiteTurn: {isItWhiteTurn}");
        Debug.Log($"chessAI assigned: {chessAI != null}");
        Debug.Log($"GameManager.Instance: {(GameManager.Instance != null ? "exists" : "null")}");
        if (GameManager.Instance != null)
        {
            Debug.Log($"GameManager.isSinglePlayerMode: {GameManager.Instance.isSinglePlayerMode}");
            Debug.Log($"GameManager.isPlayerWhite: {GameManager.Instance.isPlayerWhite}");
        }
        Debug.Log("=======================");
    }

    public void ForceBotInitialization()
    {
        Debug.Log("Force initializing bot game...");
        if (GameManager.Instance != null && GameManager.Instance.isSinglePlayerMode)
        {
            isBotGame = true;
            isPlayerWhite = GameManager.Instance.isPlayerWhite;
            isBotTurn = false;
            isBotThinking = false;
            
            // Ensure ChessAI is assigned
            if (chessAI == null)
            {
                AutoAssignChessAI();
            }
            
            Debug.Log($"Bot game force initialized - Player: {(isPlayerWhite ? "White" : "Black")}, Bot: {(isPlayerWhite ? "Black" : "White")}");
        }
        else
        {
            Debug.LogError("Cannot force initialize bot game - GameManager not found or not in single player mode");
        }
    }
}