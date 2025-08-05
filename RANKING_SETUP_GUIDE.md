# Ranking System Setup Guide

This guide explains how to set up the top 5 player ranking system in your 3D Chess game.

## Overview

The ranking system includes:
- **PlayerRanking.cs**: Manages player scores and rankings with persistence
- **RankingDisplay.cs**: Handles UI display of top 5 players
- **GameResultHandler.cs**: Updates rankings when games end
- **RankingTestData.cs**: Adds sample data for testing

## Setup Instructions

### 1. Add PlayerRanking to Game Scene

1. In your Game scene, create an empty GameObject named "PlayerRanking"
2. Add the `PlayerRanking` script to this GameObject
3. This will automatically persist across scenes and manage player data

### 2. Add GameResultHandler to Game Scene

1. In your Game scene, create an empty GameObject named "GameResultHandler"
2. Add the `GameResultHandler` script to this GameObject
3. Configure the player names and score settings in the inspector

### 3. Connect GameResultHandler to Victory Detection

1. In the **GameTimer** component:
   - Drag the GameResultHandler GameObject to the "Game Result Handler Ref" field

2. In the **Chessboard** component:
   - Drag the GameResultHandler GameObject to the "Game Result Handler Ref" field

### 4. Set Up Main Menu Ranking Display

1. In your MainMenu scene, create a UI panel for rankings:
   - Create a Panel named "RankingPanel"
   - Add a TextMeshPro Text for the title "TOP PLAYERS"
   - Create 5 child panels for ranking slots (RankingSlot1, RankingSlot2, etc.)

2. For each ranking slot, add:
   - Player Name Text (TextMeshPro)
   - Player Score Text (TextMeshPro)
   - Player Stats Text (TextMeshPro)

3. Add the `RankingDisplay` script to the RankingPanel:
   - Drag the ranking panel to "Ranking Panel"
   - Drag the title text to "Ranking Title Text"
   - Drag the 5 ranking slot GameObjects to "Ranking Slots" array
   - Drag the 5 player name texts to "Player Name Texts" array
   - Drag the 5 player score texts to "Player Score Texts" array
   - Drag the 5 player stats texts to "Player Stats Texts" array

4. Connect to MainMenuController:
   - In the MainMenuController component, drag the RankingDisplay component to "Ranking Display Ref"

### 5. Add Test Data (Optional)

1. Create an empty GameObject named "RankingTestData"
2. Add the `RankingTestData` script
3. Check "Add Test Data" in the inspector to populate sample players

## How It Works

### Player Scoring
- **Win**: 100 points + 50 bonus for checkmate
- **Loss**: 10 points
- **Draw**: 55 points (half of win + loss)

### Ranking Display
- Shows top 5 players by total score
- Displays player name, score, and win rate
- Shows "nth ..." for empty slots when less than 5 players exist
- Updates automatically when returning to main menu

### Data Persistence
- Player scores are saved using PlayerPrefs
- Rankings persist between game sessions
- Data includes: name, total score, games won, games played

## Testing

1. **Add Test Data**: Use the RankingTestData component to add sample players
2. **Play Games**: Win/lose games to see rankings update
3. **Check Persistence**: Restart the game to verify data is saved
4. **Clear Data**: Use the context menu "Clear All Rankings" in RankingTestData

## Customization

### Player Names
- Modify `whitePlayerName` and `blackPlayerName` in GameResultHandler
- For multiplayer, you can set custom names via `SetPlayerNames()`

### Scoring System
- Adjust `winScore`, `loseScore`, `checkmateBonus` in GameResultHandler
- Modify the scoring logic in `OnGameEnd()` and `OnGameDraw()`

### UI Styling
- Customize the ranking panel appearance in the MainMenu scene
- Modify text formatting in `RankingDisplay.UpdateRankingDisplay()`

## Troubleshooting

### Common Issues:
1. **"PlayerRanking instance not found"**: Make sure PlayerRanking GameObject exists in Game scene
2. **Rankings not updating**: Check that GameResultHandler is connected to GameTimer and Chessboard
3. **UI not showing**: Verify all text components are assigned in RankingDisplay inspector
4. **Compilation errors**: Ensure all scripts are in the Scripts folder and Unity has compiled them
5. **Array index errors**: Make sure all text arrays in RankingDisplay have exactly 5 elements

### Debug Commands:
- Use `RankingTestData.AddTestPlayer()` to add random test players
- Use `RankingTestData.ClearAllRankings()` to reset all data
- Use `RankingErrorHandler.LogRankingStatus()` to check system status
- Check Console for debug messages about game results and ranking updates

### Error Handling:
The system now includes robust error handling:
- `RankingErrorHandler` validates all components
- Null checks prevent crashes
- Detailed error messages help identify issues
- Graceful degradation when components are missing 