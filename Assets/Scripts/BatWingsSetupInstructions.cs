/*
BAT WINGS POWERUP SETUP INSTRUCTIONS

1. Create the Bat Wings Potion Prefab:
   a. Right-click in the Project window > Create > 2D Object > Sprite
   b. Name it "BatWingsPotion"
   c. Add a CircleCollider2D component and set it as a trigger (check "Is Trigger")
   d. Add a Rigidbody2D component and set it to "Kinematic"
   e. Add the "BatWingsPotion" script to this GameObject

2. Set up the animation sprites:
   a. Import the 4 bat wing sprites into your project
   b. In the BatWingsPotion inspector, expand the "Animation" section
   c. Set the Size of "Bat Wings Animation" to 4
   d. Drag each of the bat wing sprites into the slots (Element 0-3)
   e. The idle sprite (Element 0) will be used when the potion is floating

3. Adjust Potion Settings:
   a. Set the bob speed, height, and rotation speed as desired
   b. Set the bat wings duration to 10 seconds (or adjust if needed)
   c. Optionally assign a collect effect prefab for visual feedback when collected

4. Update the LevelGenerator:
   a. Find your LevelGenerator GameObject in the scene
   b. Drag the BatWingsPotion prefab into the "Bat Wings Potion Prefab" slot

5. Update the PlayerController:
   a. Find your Player GameObject in the scene
   b. In the Power-up Settings section, you can adjust the "Bat Wings Timer Color" if needed
      (Default is purple)

6. Test the game:
   a. Platforms should now have a 5% chance to spawn a powerup
   b. Each type of powerup (Jump, Slow, Speed, Bat Wings) has an equal 25% chance to spawn
   c. When collected, the bat wings will make the player fly upward rapidly for 10 seconds
   d. The powerup timer will display in purple during the effect

The bat wings sprites can be animated directly on the player during the effect thanks to
the BatWingsEffect coroutine that handles the animation and upward movement.
*/ 