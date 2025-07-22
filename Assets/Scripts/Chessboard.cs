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
    private void Awake()
    {
        isItWhiteTurn = true;

        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);
        SpawnAllPieces();
        PositionAllPieces();
    }

    private void Update()
    {
        if (!currentCamera)
        {
            currentCamera = Camera.main;
            if (!currentCamera) return;
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
                    // is it our turn?
                    if ((chessPieces[hitPosition.x, hitPosition.y].team == 0 && isItWhiteTurn) || (chessPieces[hitPosition.x, hitPosition.y].team == 1 && !isItWhiteTurn))
                    {
                        currentlyDragging = chessPieces[hitPosition.x, hitPosition.y];

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
                currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffset);
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
        chessPieces[x, y].currentX = x;
        chessPieces[x, y].currentY = y;
        chessPieces[x, y].SetPosition(GetTileCenter(x, y), force);
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
        winningScreen.transform.GetChild(0).gameObject.SetActive(false);
        winningScreen.transform.GetChild(1).gameObject.SetActive(false);
        winningScreen.SetActive(false);

        // reset drag & move lists
        currentlyDragging = null;
        availableMoves.Clear();
        moveList.Clear();

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
    }

    public void OnExitButton()
    {
        // go back to your MainMenu scene
        SceneManager.LoadScene("MainMenu");
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
    private bool CHECKMATE()
    {
        var lastMove = moveList[moveList.Count - 1];
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
        //Is the king being attacked? 
        List<Vector2Int> currentAvailableMoves = new List<Vector2Int>();
        for (int i = 0; i < attackingPieces.Count; i++)
        {
            var pieceMove = attackingPieces[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
            for (int  b= 0; b < pieceMove.Count; b++)
                currentAvailableMoves.Add(pieceMove[b]);
        }
        //Are we in check? 
        if (ContainsValidMove(ref currentAvailableMoves, new Vector2Int(targetKing.currentX, targetKing.currentY)))
        {
            for (int i = 0; i < defendingPieces.Count; i++)
            {
                List<Vector2Int> defendingMoves = defendingPieces[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                SimulateMoveForSinglePiece(defendingPieces[i], ref defendingMoves, targetKing);

                if (defendingMoves.Count != 0)
                    return false;
            }
            return true; //checkmate
        }
        return false;
                    


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

        PositionSinglePiece(x, y);

        isItWhiteTurn = !isItWhiteTurn;
        if (timerManager != null)
        {
            timerManager.SwitchTimer();
        }
            


        moveList.Add(new Vector2Int[] { previousPosition, new Vector2Int(x, y) });

        BeforeSpecialMove();

        CHECKMATE();

        if (CHECKMATE() == true)
        {
            CheckMate(cp.team);
        }

        return true;
       
    }
    private Vector2Int LookupTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (tiles[x, y] == hitInfo)
                    return new Vector2Int(x, y);

        return -Vector2Int.one; // Invalid
    }

}