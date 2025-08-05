# Side Selection Setup Guide (Separate Scene Approach)

This guide explains how to set up the side selection feature using a separate scene.

## Overview

The side selection system allows players to choose whether they want to play as White or Black when playing against the AI. This uses a separate scene approach.

## Setup Instructions

### 1. Create SideSelectionMenu Scene

1. **Create New Scene:**
   - In Unity, go to File → New Scene
   - Save it as "SideSelectionMenu"
   - Add it to Build Settings

2. **Set Up UI Elements:**
   - Create a Canvas with UI elements
   - **Title Text**: "Choose Your Side"
   - **Description Text**: "Select which color you want to play as:"
   - **White Button**: "Play as White"
   - **Black Button**: "Play as Black"
   - **Back Button**: "Back to Menu"

3. **Add the SideSelection Script:**
   - Add the `SideSelection` script to an empty GameObject
   - Connect the UI elements:
     - Drag the White button to "White Button"
     - Drag the Black button to "Black Button"
     - Drag the Back button to "Back Button"
     - Drag the title text to "Title Text"
     - Drag the description text to "Description Text"

### 2. Update Build Settings

1. **Add Scene to Build:**
   - Go to File → Build Settings
   - Add "SideSelectionMenu" to the scene list
   - Make sure it's in the correct order

### 3. Test the System

1. **Run the game** and go to Main Menu
2. **Click "Play vs Bot"** - should load SideSelectionMenu scene
3. **Choose White or Black** - should start the game with correct settings
4. **Click "Back to Menu"** - should return to MainMenu
5. **Check console** for debug messages about player side

## How It Works

### **Scene Flow:**
```
MainMenu → SideSelectionMenu → Game
```

### **Player Side Logic:**
- **White Button**: Sets `GameManager.isPlayerWhite = true`
- **Black Button**: Sets `GameManager.isPlayerWhite = false`
- **AI Side**: Automatically determined as opposite of player

### **AI Team Logic:**
- If player is White → AI is Black (team=1)
- If player is Black → AI is White (team=0)

### **Turn Management:**
- AI only moves on its own turn
- Proper turn switching between player and AI
- AI can play as either White or Black

## Troubleshooting

### **Common Issues:**

1. **"Scene not found" error**
   - Make sure SideSelectionMenu is added to Build Settings
   - Check scene name spelling in MainMenu.cs

2. **AI not moving**
   - Check console for debug messages
   - Verify `GameManager.isPlayerWhite` is set correctly
   - Ensure AI team logic is working

3. **Wrong side playing**
   - Check that `GameManager.isPlayerWhite` is set to correct value
   - Verify AI team calculation: `aiTeam = isPlayerWhite ? 1 : 0`

### **Debug Commands:**
```csharp
// Check current player side
Debug.Log($"Player is {(GameManager.Instance.isPlayerWhite ? "White" : "Black")}");

// Check AI team
int aiTeam = GameManager.Instance.isPlayerWhite ? 1 : 0;
Debug.Log($"AI is team {aiTeam} ({(aiTeam == 0 ? "White" : "Black")})");
```

## Expected Behavior

✅ **Scene Loading**: MainMenu → SideSelectionMenu → Game  
✅ **White/Black Choice**: Player can select their side  
✅ **Correct AI Side**: AI plays as opposite color  
✅ **Proper Turn Order**: AI moves on its turn, player on theirs  
✅ **Back Navigation**: Returns to MainMenu when clicking back  
✅ **Game Start**: Correctly starts bot game with chosen settings  

## Integration with Ranking System

The side selection works seamlessly with the ranking system:
- Player names are set based on the chosen side
- Game results are properly recorded
- Rankings update correctly regardless of which side was played 