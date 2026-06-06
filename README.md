# OCDheim - DEATH TO EYEBALLING!
The purpose of this mod is for me, a person suffering from severe OCD, to be able to enjoy Valheim in its full Glory™ while keeping noninvasive, consistent and respectful of the intended OG experience.

OCDheim is an opinionated collection of building and terraforming tools that bring 22nd-century-level laser precision to... your merry Viking settlement 'Stokhölm' 🍻❤️

Do you bleed from every orifice while trying to eyeball a line of torches, a wall of chests, or pin trophies on a wooden pole?
Do you suffer a violent seizure while trying to level a ledge?
Then this is the mod for you.

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

### ADDITIONAL SNAP POINTS
- **Furniture Pieces[^1] snap to Construction Pieces![^2]**
	- if [PRECISION MODE] is DISABLED → insert additional Snap Points every ~1.0m
		- for example: a 2m x 2m Wood Floor would receive 5 additional Snap Points (not only in the corners, but also in the middle of the sides as well as in the middle of the piece)
		- for example a 4m Log Pole would receive 3 additional Snap Points
	- if [PRECISION MODE] is ENABLED  → insert additional Snap Points every ~0.5m
		- for example: a 2m x 2m Wood Floor would receive 21 additional Snap Points
		- for example a 4m Log Pole would receive 7 additional Snap Points
- **Construction Pieces receive additional Snap Points while snapping with other Construction Pieces**
	- if [PRECISION MODE] is ENABLED  → insert additional Snap Points every ~0.5m
	- if [PRECISION MODE] is DISABLED → no additional Snap Points
- Furniture Pieces snap to select other Furniture Pieces
	- Table
	- Round Table
	- Long Heavy Table
	- Black Marble Table
	- *I'm open to other proposals*

### GIRD MODE (press `ALT` [`LB` + `RS`] to toggle)
- **The World Grid is visually imposed over terrain**
![World Grid](https://github.com/java-devil/OCDheim/blob/main/screenshots/world-grid.png?raw=true)
- The Hoe and The Cultivator become tools of divine precision
	- The terrain modification AoE snaps to the World Grid
	- The terrain modification AoE visualization works under Ground Level (incl. under Sea Level)
	- The terrain modification AoE visualization is razor sharp, with well-defined borders (visualized FULLY over affected tiles AND ONLY over affected tiles)
	- The terrain modification AoE is applied uniformly over the affected tiles (doesn't dissolve closer to the border)
	- The tile position (x, y, h) is overlayed over the terrain modification visualization for player convenience
	- **Especially useful when:** leveling or cultivating ledges without spilling over slopes or corners
- `MOUSE WHEEL SCROLL ↑ or ↓` [`LT` + `D-PAD ↑` or `LT` + `D-PAD ↓`] when `Raise Ground` is active to precisely fine-tune the intensity of the effect
	- **Especially useful when:** the vicinity of your keep is TOO perfect and TOO even from the extensive use of `Level Ground` in GRID MODE 😉
- All Build Pieces snap to the World Grid (as opposed to other Build Pieces)
	- **Especially useful when:** laying down the foundation piece of your build so that the whole build resides on the World Grid
- All Seeds snap to the World Grid
	- **Especially useful when:** well... quickly sowing while efficiently utilizing available terrain

### PRECISION MODE (press `Z` [`LB` + `X`] to toggle)
- [Multiple Additional Snap Points](#additional-snap-points)
- if [GRID MODE] is ALSO enabled + The Hoe    is equipped → no effect (since the minimal sensible AoE for terrain modification is a 1m x 1m tile)
- if [GRID MODE] is ALSO enabled + The Hammer is equipped → The World Grid density is increased from 1m x 1m tiles to 0.5m x 0.5m tiles

### Remove Terrain Modifications (Hoe Tool)
**Especially useful when:**
- re-naturalizing your build
- covering up silver node excavation sites
- covering up "one click too many" crime sites 😉

### Vertical Stacking of... Stacks
- Perhaps somewhat ironically stacks[^3] in Vanilla Valheim do not... well... erm... stack. Well now they do.
![Vertical Stacks](https://github.com/java-devil/OCDheim/blob/main/screenshots/stacked-stacks.png?raw=true)

### Additional Build Pieces
- Aesthetically Pleasing "Stone Floor 2x2"-inspired drop-in alternatives to:
	- Stone Pillar 1x2 (Smooth Stone Pillar)
	- Stone Wall 1x1 (Smooth Stone Wall 1x1)
	- Stone Wall 2x1 (Smooth Stone Wall 2x1)
	- Stone Wall 4x2 (Smooth Stone Wall 4x2)

## Considered Possible Improvements
- Config File
	- Keybinding Overrides
	- Functional Toggles
	- Logging Levels
	- Min/Max Terrain Modification Depth
- Add `Smooth Slope` Tool → The Hoe (`MOUSE WHEEL SCROLL ↑ or ↓` to precisely fine-tune the Slope °)
- if [GRID MODE] is ENABLED + The Cultivator is equipped → visualize The World Grid vs Terrain Type Grid drift (they do not fully overlap)
- if [GRID MODE] is ENABLED + The Pickaxe is equipped → visualize on The World Grid where The Pickaxe is going to land
- if [PRECISION MODE] is ENABLED → increase the rotation precision of Build Pieces
- if [PRECISION MODE] is ENABLED → visualize Snap Points
- Aesthetically Pleasing Crafting Stations superseed inferior Crafting Stations
	- Workbench → Artisan Table
	- Smelter → Blast Furnance
	- Forge → Black Forge
- Unlock Dvergr Build Pieces to enable full restoration of Dvergr structures
- Show OCDheim keybind tips side by side with Vanilla Valheim keybind tips 
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

## Feedback
[![Discord](banners/discord.png)](https://discord.com/users/890153569905414144)
[![GitHub](banners/github.png)](https://github.com/java-devil/OCDheim)
[![Nexus Mods](banners/nexus.png)](https://www.nexusmods.com/valheim/mods/2087)

## Donations
If you enjoy my work please consider a second to donate 😉

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/Z8Z3GZVCJ)

[^1]: Every Build Piece that is NOT snappable in Vanilla Valheim (think: a Torch, a Forge or a Forge Cooler) - corresponds to the `Misc` `Crafting` and `Furniture` Tabs
[^2]: Every Build Piece that is snappable in Vanilla Valheim (think: a Wood Floor, a Darkwood Pole or a Black Marble Column) - corresponds to the `Build` and `Heavy Building` Tabs
[^3]: Every Build Piece suffixed with "Stack" or "Pile". Wood Stack, Corewood Stacks, Finewood Stacks, Bone Stacks, Stone Piles, Coal Piles, Coin Piles...
