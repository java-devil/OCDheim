# OCDheim - DEATH TO EYEBALLING!
The purpose of this mod is for me, a person suffering from severe OCD and selective perfectionism, to be able to enjoy Valheim in its full Glory™ while keeping noninvasive, consistent and respectful of the intended Valheim experience.  

OCDheim is an opinionated collection of tools hoping to bring 22nd-century-level space laser precision, in full force, from an artificial Moon directly to... your merry Viking settlement "Stokhölm" 🍻❤️

If by publicly releasing this mod I please only one more person of similar needs/preferences/disorders - it was worth it to do so 😊

# Propaganda Movie
[clickedy-click! click! To Play on YouTube... C'mon clickedy!](https://youtu.be/uBOj1TKbugQ)
[![clickedy-click! click!](https://img.youtube.com/vi/uBOj1TKbugQ/maxresdefault.jpg)](https://youtu.be/uBOj1TKbugQ)

## HOW to install OCDheim?
- Ensure [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) is properly installed
- Ensure [Jötunn](https://valheim.thunderstore.io/package/ValheimModding/Jotunn/) is properly installed
- Download OCDheim from your preferred mirror:
	- [Thunderstore](https://valheim.thunderstore.io/package/javadevils/OCDheim/)
	- [Nexus Mods](https://www.nexusmods.com/valheim/mods/2087)
- Extract the contents of the OCDheim .zip file to the BepInEx/plugins subdirectory of your Valheim installation
	- Typical Windows path: C:/Program Files (x86)/Steam/steamapps/common/Valheim/BepInEx/plugins
	- Typical Linux path: ~/.steam/debian-installation/steamapps/common/Valheim/BepInEx/plugins

## WHY install OCDheim?
- **Furniture Pieces[^1] snap to Construction Pieces![^2]**
	- Press & hold SHIFT [LB on XBOX controller] to revert to non-snapping behavior (consistent with Construction Piece snapping behavior in Vanilla Valheim)
	- Furniture Pieces snap to Construction Pieces on arbitrary sides, with arbitrary rotation (as smoothly and intuitively as one could hope... hopefully 😉)
	- Furniture Pieces also snap to specified other Furniture Pieces (for now: Table, Round Table, Long Heavy Table and Black Marble Table; however I'm open to proposals)
	- Construction Pieces receive additional Snap Points **UP TO 0.5m PRECISION** WHILE snapping with Furniture Pieces (for example: a 2x2 piece would receive Snap Points not only in the corners, but also in the middle of the sides as well as in the middle of the piece)
	- **Especially useful when:** you suffer from severe OCD and you bleed from every orifice while trying to eye-ball a line of torches, a wall of chests, or trophies on wooden poles
- Press ALT [RB on XBOX controller] to toggle GRID MODE
	- The Hoe and The Cultivator become tools of divine precision
		- The terrain modification AoE snaps to the World Grid
		- The terrain modification AoE visualization is razor sharp, with well defined borders, overlayed fully over affected tiles and only over affected tiles
		- The terrain modification AoE is applied uniformly over the affected tiles (doesn't dissolve closer to the border)
		- The terrain modification AoE is decreased in order to enable more precise terrain manipulation by the player
		- The tile position (x, y, h) is overlayed over the terrain modification visualization for convenient reference
		- **Especially useful when:** You cherish your time in this life 😉 Also... when working with ledges (think: you whish to fully level, pave or cultivate a ledge while not overlapping the slope, or worse still - a different ledge)
	- Spin the MOUSE SCROLL WHEEL [hold LT & press D-PAD UP or D-PAD DOWN on XBOX controller] while in the Raise Ground Tool to precisely fine-tune the intensity of the effect
		- **Especially useful when:** the vicinity of your keep is TOO perfect and TOO even for your preference from the extensive use of the Level Ground Tool in GRID MODE 😉
	- All Build Pieces[^3] snap to the World Grid (as opposed to each other)
		- **Especially useful when:** building in the open world with no other reference, or laying down the first piece of your build as a reference for other pieces (think: you wish your whole build to lie on the World Grid)
	- All Cultivator Pieces[^4] snap to the World Grid
		- **Especially useful when:** well... quickly sowing while utilizing available terrain as efficiently as possible
- press Z [X on XBOX controller] to toggle PRECISION MODE
	- Construction Pieces receive further additional Snap Points while snapping with Furniture Pieces (for example: a 4m pole receives 2 Snap Points while snapping with a different Construction Piece, 5 Snap Points while snapping with a Furniture Piece with PRECISION MODE disabled and 9 Snap Points while snapping with a Furniture Piece with PRECISION MODE enabled)
	- **Construction pieces receive additional Snap Points (UP TO 0.5m PRECISION) while snapping with other Construction Pieces** (for example: a 4m pole receives 2 Snap Points while snapping with a different Construction Piece with PRECISION MODE disabled and 5 Snap Points while snapping with a different Construction Piece with PRECISION MODE enabled)
	- The Hoe and The Cultivator precision in GRID MODE is unaffected (since the minimal sensible AoE for terrain modification and sowing is a 1x1 tile)
	- The Build Tool precision in GRID MODE is increased from 1x1 tiles to 0.5x0.5 tiles
	- The Level Ground Tool fine-tune precision is increased several fold
- "Remove Terrain Modifications" Tool added to The Hoe
	- Especially useful when: covering up silver node excavation sites or covering up "one click too many while mining tin" crime sites 😉
- Aesthetically pleasing "Stone Floor 2x2"j-inspired drop-in alternatives to the Stone Pillar, Stone Wall 1x1, Stone Wall 2x1 and Stone Wall 2x2

## Considered Possible Improvements
- Configuration file
	- Functionality toggles
	- Keybinding overrides
	- Debug Mode toggle
- The Hammer
	- PRECISION MODE increases Snap Points of Construction Pieces (where ordinary mode would work as precision mode works nowadays, and precision mode would work as precision mode works for Furniture Pieces nowadays)
	- PRECISION MODE increases rotation precision of Build Pieces
	- Unlock Dvergr Build Pieces to enable full restoration of Dvergr Structures
	- Show OCDheim keybinding-tips side by side with Vanilla Valheim keybinding-tips while in Build Mode
- The Hoe and The Cultivator
	- Undo & Redo functionality
	- ~~The Remove Terrain Modifications Tool works as intended on Tar Pits and in The Mistlands~~
	- Visualization of the whole World Grid (as opposed to visualization only of the affected tiles
	- The Remove Terrain Modifications Tool works on stone formations (think: fixing destroyed plains pillars)
	- Differentiate Terrain Modification visualization from Terrain Recoloring visualization (they do not overlap) in Tools that do both
- Visual cues when GRID MODE or PERCISION MODE are enabled
- Performance improvements if proven necessary
- Revise compatibility issues with other mods

## Known Issues
https://github.com/java-devil/OCDheim/issues

## Acknowledgments
- My Wife.
- My Wife once more.
- Seriously Guys, my Wife. She screened my ideas. Helped test them. Helped with the GUI elements. Recorded the hilarious promotional video for you to enjoy. Provided me with back rubs and brain rubs... not to mention two kids and more love than I would possibly know what to with.
- ...also the Valheim developers for providing us with this, dunno... MASTERPIECE OF A GAME
- ...and the Jötunn developers for being more helpful than is permissible by law (seriously if you ever decide to mod Valheim - look up their Wiki and their Discord)

## Donations
If you enjoy my work please consider a second to donate 😉

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/Z8Z3GZVCJ)

[^1]: Everything that is not a Construction Piece (think: a Forge or a Forge Cooler) - corresponds to the Misc Tab, Crafting Tab and Furniture Tab
[^2]: Everything that is snappable in Vanilla Valheim (think: a Darkwood Pole or a Black Marble Column) - corresponds to the Build Tab
[^3]: Construction Pieces and Furniture Pieces combined
[^4]: aka "Seeds" - sorry, couldn't resist 😛
