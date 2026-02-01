# Operation Brevity

A wargame about Operation Brevity:

https://en.wikipedia.org/wiki/Operation_Brevity

Game submission to Historical Accurate Game Jam 12.

## Screenshots

## Controls

Game Controls:

- **Right-click and drag**: Move the camera
- **Scroll wheel**: Zoom the camera view in or out
- **Toggle button in the upper-right corner or Spacebar** (you may need to click on the map first for Spacebar to take effect): Start/pause time progression
- **Left-click on a unit**: Select the top unit in the stack
- **Left-click repeatedly on a unit**: Move the top unit in the stack to the bottom and select the new top unit
- **Right-click**: Set a mission line for a unit. When a unit is given a mission line, it and all subordinates without assigned mission lines will move to the target and deploy.
- **Ctrl + Right-click**: Plan a path directly (usually unnecessary).

Combat occurs when a unit moves into a hex occupied by enemy forces. If a unit’s readiness is very low, it will automatically retreat.

Generally speaking, you should assign mission lines to top-level HQ units (for example, for the Allies, the 7th Armoured Brigade HQ and three other top-level HQs fall into this category). This way, they will advance toward the objective. When you feel the need for finer adjustments, you can assign mission lines to subordinate units, which will then be considered detached and will no longer be commanded by their superior HQ. Once the micro-adjustments are complete, you can use the "Reattach" option on a unit to return it to its original superior. Alternatively, the superior unit can use the "Reattach All Subordinate" button to reassign all units back to its command.

## Note

Global AI now is pretty bad so you may want to play it hotseat.

Players familiar with other games may notice similarities to the control mechanics of Hearts of Iron, including how readiness can be compared to Hearts of Iron's organization stat (with the key difference being that here, readiness directly modifies combat effectiveness proportionally) and most units are controlled automatically, while a small number are micro-managed to encircle and annihilate the enemy. For more experienced players, it might be evident that the combat system is essentially a continuous version of some mechanics from the Panzer Campaign series (though many features remain unimplemented), and also shares similarities with Command Ops 2.

I originally intended to simulate the entire Operation Brevity, but due to time constraints, only the first day is included. Therefore, the dramatic German reinforcements that appeared later in the battle are not featured in this scenario, even though they are present in the order of battle . The AI still lacks some essential steps to function fully. In the combat system, artillery is automatically placed in the second line, but their long-range attack capability has not yet been implemented. So, if you really want to use them, you could try employing them in assaults (😓). Additionally, air forces are not yet implemented.


## Credits

- Libraries:
    - CsvHelper: https://github.com/JoshClose/CsvHelper
- Sounds:
    - Firefight (TheBuilder15 (Freesound)): https://pixabay.com/sound-effects/film-special-effects-firefight-70199/
    - Machine gun fire (beetpro): https://pixabay.com/sound-effects/film-special-effects-machin-gun-mg34-double-sound-effect-7-11005/
    - Artillery Gunfire (qubodup (Freesound)): https://pixabay.com/sound-effects/film-special-effects-artillery-gunfire-14607/
    - panzer35(t) firing (cabbageheadfilmz): https://pixabay.com/sound-effects/film-special-effects-panzer35t-firing-231033/
    - Tank Track Ratteling (u_3rdmeaw7un): https://pixabay.com/sound-effects/film-special-effects-tank-track-ratteling-197409/
- Research:
    - Order of Battle references the Brevity scenarios from PZC, CO2 and BCS.

