# Medieval War Simulation - Game Design Document

## 1. Overview

The game is a desktop application developed in C# with MonoGame, chosen for its excellent 2D performance, ability to handle hundreds of simultaneous units, strong ecosystem, and significantly lower complexity compared to C++ while maintaining near-equivalent performance.

**Setting**: Medieval. All units, terrain, structures, and narrative elements are grounded in a medieval context.

**Visual Perspective**: Top-down 2D. The camera looks straight down at the map. No isometric or side-scrolling perspective.

**Time Flow**: Real-time. All units and factions move and act simultaneously at all times.

**Game Name**: Not yet decided. The name will be chosen once the game has taken sufficient shape during development.

**Project Scope**: The game is a solo personal project maintained in a private GitHub repository. If development reaches a satisfactory level of quality, the game may be released publicly on a distribution platform, either free or as a paid title. All development will remain private until that point.

**Performance Targets**: 100+ FPS with 2000+ units actively visible on screen, with the potential to scale to 10k+ total units through squad-level efficiency (formations, group LOD, dormant simulation). Performance benchmarking is part of every development phase to ensure targets are met before adding complexity. Entity system supports up to 100,000 entities (`MAX_ENTITIES = 100000`).

---

## 2. Map & World

### Map Structure
Free movement with no underlying grid. Units move freely through space and are not constrained to tiles or hexagonal cells.

### Map Creation
Campaign maps are hand-authored. Test/development maps are also hand-made initially. Procedural map generation is planned for a future phase.

### Map Visual Style
Map visuals use smooth, rounded borders (not pixelated) for terrain regions, provinces, and territories, similar in style to Pax Humana or War of Dots.
- Province borders are thin lines
- Conquered/controlled territory borders are thicker lines in the faction's color
- Provinces themselves are not filled with color -- only the borders are colored
- Cities and other map features will be designed in a later phase

The overall aesthetic is clean, abstract-geometric maps with well-defined smooth boundaries.

### Terrain Types
- **Hills**: Provide a vision bonus and a defensive bonus. Movement speed is reduced going uphill and increased going downhill. The mid-hill position is the optimal defensive location, balancing vision and protection.
- **Mountains**: Similar to hills but with more severe movement penalties. They provide strong defensive positions and vision bonuses but are very difficult to traverse.
- **Forests**: Reduce movement speed. Cavalry suffers an additional penalty in forests. Units inside forests are partially concealed from enemies. Concealment is not absolute; enemy units nearby have a probability-based chance of detecting a hidden unit, but they cannot identify its type. The hidden unit appears as a circle regardless of its actual type.
- **Rivers**: Units can cross rivers at any point but doing so without a bridge is slow and weakens the unit temporarily. Cavalry cannot cross water at all and must use bridges. Bridges allow normal movement speed with no penalty.

### Scale / Unit Count
Both small and large scale depending on the context. Some missions or scenarios may involve small elite forces of ten to fifty units per side, while others may involve hundreds of units across multiple fronts simultaneously. The engine must support both extremes. Map sizes range from small to medium to enormous. A game session on a small map may conclude in a few hours of real time. A full session on an enormous map could last days or weeks of real time.

### Zoom and Level of Detail
Continuous zoom with configurable min and max limits. LOD transitions happen at defined zoom thresholds. At maximum zoom out, a large group of units appears as a single large icon representing the whole legion. Zooming in progressively reveals subgroups, and at full zoom individual units become visible and selectable. At full zoom the player can move individual soldiers. At maximum zoom out with no units visible in an area, only small dots indicate presence.

**Zoom controls**: Mouse scroll wheel. Zoom limits: Min = 0.01x (fully zoomed out), Max = 1.0x (fully zoomed in), default = 0.5x. Scroll up = zoom in (x1.1), scroll down = zoom out (x0.9).

### Visual Hierarchy Zoom LOD (Planned)
1. **Soldier Level (1.0x to 0.25x)**: Focus on individual micro-management and combat. Full geometric shapes (polygons), individual rotations, health bars, and smooth borders. Direct control of every single unit.
2. **Lieutenant Level (0.25x to 0.1x)**: Focus on squad-level operations (50-100 units). Individual soldiers fade into compact color blocks/formations. Medium-sized Lieutenant icons appear as primary tactical anchors. Individual HP bars are replaced by a single squad health indicator.
3. **Strategic Level (0.1x to 0.01x)**: Focus on global theater and division maneuvers. Generals appear as large, prominent icons representing entire army wings (1000+ units). Army Chief appears as a small but visually unique/special icon (e.g., golden crown/eagle), smaller than Generals to emphasize the brain of the army over brute force. Map-wide perspective; only command figures and major movement vectors are rendered.

### Pathfinding
The pathfinding system uses a hierarchical approach with two layers. At the macro level, the map is divided into provinces (large regions) that units navigate between. Within each province, a localized spatial grid handles fine-grained movement. For groups, units follow the group leader in a loose formation rather than each unit calculating its own full path. The leader computes the macro-level route through provinces; individual units handle local obstacle avoidance within the spatial grid. Terrain types affect path costs at the spatial grid level, with hills, forests, rivers, and mountains each applying their respective movement speed modifiers and bonuses during path calculation.

---

## 3. Units

### Unit Types
Four primary combat unit types: infantry, archers, cavalry, and ballista. A fifth unit type, the war medic, exists as a secondary support role. Each type has distinct strengths, weaknesses, speed, range, and behavior.

**Stat relationships** (relative, exact values to be defined later):
- **Infantry**: Balanced all-around stats. No extreme strengths or weaknesses.
- **Archers**: Long attack range, slow movement speed. Vulnerable in melee.
- **Cavalry**: Very fast, high dodge/evasion chance. Strong against archers and ballistas. Weak against infantry in melee. Reduced effectiveness in forests.
- **Ballista**: Extremely high damage (one-shots most units). Very slow movement and reload. Vulnerable when unprotected.
- **War Medic**: Fast movement, very low combat stats, can fight but deals minimal damage. Primary role is healing support.

### Unit Stats (Current Implementation)

| Unit | Shape | Radius | Speed | Vision Range | Base HP | Base Stamina |
|---|---|---|---|---|---|---|
| Infantry | Square | 30px | 160 px/s | 1500px | 110 | 100 |
| Archer | Triangle | 28px | 180 px/s | 2250px | 60 | 100 |
| Cavalry | Pentagon | 40px | 500 px/s | 3000px | 90 | 100 |
| Ballista | Octagon | 55px | 50 px/s | 2250px | 150 | 100 |
| Medic | Hexagon | 28px | 200 px/s | 1500px | 80 | 100 |

Each stat has a +-5% random variance on creation, rolled via `UnitStats.Roll*()`.

### Visual Design (Shapes per Type)
Units are represented by geometric shapes. The same shapes are used for both player and enemy units; color is the primary distinguishing factor. Shapes were refactored on 17/05 to the following current mapping:
- Infantry: Square (quadrato)
- Archer: Triangle (triangolo)
- Cavalry: Pentagon (pentagono)
- Ballista: Octagon (ottagono)
- Medic: Hexagon (esagono)

Unit color is determined by team (see Factions & Teams section): White team = white, Red team = red, Blue team = blue. Stamina desaturation is applied on top of the team color. Any unit that is hidden or partially concealed by terrain or fog of war appears as a plain circle, regardless of its actual type. The player can identify the unit type of a hidden enemy only after getting close enough with their own units.

### Commander Rank Visual Indicators
Commander rank is indicated by visual borders added to the base shape:
- Soldier: Plain shape with no border
- Capogruppo (Group Leader): Shape with one border
- Luogotenente (Lieutenant): Shape with two borders
- Generale (General): Shape with two borders and a star
- Capo dell'Esercito (Army Chief): Shape with unique special borders distinct from all other ranks

Enemy commanders are only identifiable when spotted at close range with adequate visibility.

### Facing Direction
In the initial implementation, facing direction is purely visual (units rotate toward their movement direction). Vision range is a circle radius. Unit facing direction and its effects on combat (e.g., flanking/rear bonuses) and vision (directional cones) are planned for a future phase of development.

### Vision Range
Yes to both different vision ranges per type and identification at range. Each unit type has its own vision radius. Identifying what a spotted unit actually is requires being close enough. From a long distance, a player's units may detect that something is present but cannot determine its type. Hidden units always appear as plain circles until identified. Vision range is a circle radius for the current implementation. A directional cone based on the unit's facing direction will be considered for a future phase.

### War Medic
The war medic is a support unit that can be assigned to a group, in which case it automatically heals nearby wounded units within its radius. It can also be used as a standalone unit and directed manually to specific targets. Not every group is required to have a medic; it is an optional assignment. While healing, the medic must remain stationary. It cannot heal and move at the same time. The medic can heal multiple units simultaneously up to the limit defined by its healing radius. Healing time is proportional to how much damage the target has taken. A unit with nearly full health may recover in as little as twelve hours of game time. A unit near zero health may require up to fourteen game days to fully recover. This makes medics strategically valuable and their protection a tactical priority.

### Cavalry in Terrain
Cavalry is the fastest unit on open ground but is fragile in sustained combat. It is weaker against infantry and takes longer to kill them (20s) than infantry needs to kill cavalry (11.3s). Cavalry excels at chasing down archers and ballistas due to its speed advantage. In forests, cavalry suffers additional movement and efficiency penalties. Cavalry cannot cross rivers through water at all and must use bridges exclusively.

---

## 4. Combat

### Combat Stats (Attack Values)

| Unit | Attack Range | Damage | Cooldown | DPS | Type |
|---|---|---|---|---|---|
| Infantry | 35 (melee) | 8 | 1.0s | 8 | Melee, beats cavalry |
| Archer | 400 (ranged) | 5 | 1.5s | 3.3 | Ranged, lightly armored |
| Cavalry | 35 (melee) | 5 | 1.0s | 5 | Melee, fragile, beats ranged |
| Ballista | 600 (ranged) | 80 | 5.0s | 16 | Siege, 1-2 shot most units |
| Medic | 80 (heal) | 0 | — | — | Support, heals only |

All damage is flat (no armor in initial implementation). Targets have no damage reduction. Planned for future: armor/penetration system, charge bonus for cavalry.

**Matchup outcomes (1v1, equal skill):**
- Infantry vs Cavalry: Infantry wins (11.3s vs 20s) -- spears beat cavalry
- Cavalry vs Archer: Cavalry wins (12s vs 32s) -- fast catch
- Archer vs Infantry (kiting): Archer wins if space to retreat (180 > 160 speed)
- Ballista vs Archer/Ballista vs Medic: 1-hit kill (80 > 60/80)
- Ballista vs Infantry/Cavalry/Ballista: 2-hit kill (160 > 110/90/150)

### Stances
The player can toggle between stances per unit or group, modeled after the stance system in Hearts of Iron IV:
- **Attack mode**: Units automatically attack any enemy that enters their attack range. When moving in attack mode, units stop to fight enemies they encounter and resume moving after the threat is eliminated.
- **Hold mode**: Units do not initiate attacks but will counter-attack if directly attacked. Units in hold mode do not retreat automatically; retreat must be explicitly ordered.
- **Move mode**: Units focus on movement and ignore enemies, but if attacked, the unit counter-attacks. If a squad is moving and one unit is attacked, the entire squad joins to defend and counter-attack.

Default stance options for a selected group: Hold/Defensive (units hold position with shields up, defensive bonus), Attack (engage aggressively), Retreat (ordered withdrawal, details to be defined).

### Melee Collision
Units have a small collision radius -- they cannot overlap and maintain a minimum distance from each other. Melee combat requires units to be adjacent (within their attack reach). Ranged units engage from their respective weapon range. Multiple units can attack or target the same enemy simultaneously. The player can configure how densely units should position themselves (spread out vs. tight formation), affecting both collision spacing and combat engagement distances.

### Combat Calculation
Combat uses numeric formulas with probability and random factors. Each unit has stats (attack power, defense, health) that interact with the target's type and current state.

**Hit chance** depends on multiple factors: the attacker's unit type, the target's unit type (e.g., cavalry has reduced hit chance against infantry in melee), range, terrain, fatigue level, and morale status. A random roll determines if the attack lands.

When a hit lands, **damage is direct** (flat reduction to the target's health), modified by the attacker's attack stat versus the target's defense stat, plus a small random variance. Wounded units deal and receive modified damage proportional to their remaining health.

### Ballista Mechanics
The ballista is extremely powerful but very slow to move and requires time to reload between shots. It excels against densely packed groups of units. A direct hit on a single unit kills it instantly. A shot that travels through a line of units kills those in the path sequentially. A shot that hits a line of shielded units kills the first two and leaves the third critically wounded. Cavalry hit by a ballista bolt is killed. Due to its power, the ballista is best used by carefully targeting dense formations or specific high-value units such as commanders. Its movement speed makes repositioning during a battle very difficult.

**Ballista targeting**: The player can fire manually by right-clicking a target, or the ballista can fire automatically when in attack/move mode. In hold mode it does not fire automatically -- only manual orders work.

### Archer Targeting
Archers follow the same stance-based targeting as other units:
- Attack / Move mode: auto-target enemies in range
- Hold mode: do not auto-attack, only counter-attack if attacked, plus manual fire orders

The player can also manually order archers to fire at a specific target regardless of stance.

### Projectile Visibility
At high zoom levels, arrows appear as very small fast-moving lines that are barely visible. At lower zoom levels, projectiles are not rendered and damage simply appears on the target. Arrow damage is constant regardless of distance, but accuracy decreases as range increases, giving archers a lower probability of hitting targets at maximum range.

### Visual Feedback in Combat
When a unit takes damage, the border of its geometric shape briefly flashes dark red. There are no particle effects, blood, or complex animations, consistent with the abstract geometric visual style. When a unit dies, its shape remains on the battlefield as a darkened or greyed-out silhouette for a few seconds before disappearing entirely.

### Wounded Penalties
A unit's combat effectiveness and movement speed decrease proportionally with its remaining health. A unit at fifty percent health is significantly slower and weaker than one at full health. This makes the war medic a strategically important unit rather than a secondary consideration.

### Permanent Death
No. Death is permanent. Every unit lost in battle is gone for the rest of the game or mission. This makes every tactical decision carry real weight.

---

## 5. Command & Organization

### Command Hierarchy
From lowest to highest rank:
1. Soldier - standard unit, no special indicator
2. Capogruppo (Group Leader) - manages a small group
3. Luogotenente (Lieutenant) - manages multiple groups
4. Generale (General) - manages multiple lieutenants and their forces
5. Capo dell'Esercito (Army Chief) - commands the entire army, only one permitted at a time

The Capo dell'Esercito is mandatory. All other command ranks are optional and assigned by the player as needed. Having no commanders means units are less coordinated. A single army chief without subordinate commanders is possible but limits large-scale coordination. The player designs the command structure to suit their strategy.

### Detailed Command Assignment
Any unit can be promoted to a commander rank. The Army Chief is mandatory; all other ranks are optional and assigned by the player.

Each rank provides:
- **Capo dell'Esercito (Army Chief)**: Highest stats (slightly above General), provides a bonus radius within its own group and surrounding area. Can issue global stance orders (expand, hold, attack-move, etc.) that propagate to every unit in the army. Must be designated from a non-engaged group; death causes massive army-wide morale loss and requires 3+ in-game days to replace.
- **Generale (General)**: Moderate stats, issues orders to all groups under their command. If General says hold, all units in their command chain hold.
- **Luogotenente (Lieutenant)**: Lower stats, controls multiple groups. Orders apply only to groups directly under them.
- **Capogruppo (Group Leader)**: Small bonus to their own group only.
- **Soldier**: No command ability.

Orders flow top-down. A higher-ranking order overrides lower ones. The player can issue orders at any level of the hierarchy.

### Command Bonuses
Commanders provide bonuses within a small physical radius around them on the map. The bonus applies to units nearby regardless of group assignment. As rank increases, the quality of the bonus changes, but the radius remains small, meaning the physical position of the commander on the battlefield is tactically important. Commanders higher up in the hierarchy still belong to a specific group and position on the map; their bonus does not apply army-wide from anywhere.

### Army Chief Death
The army chief's death causes a massive army-wide morale loss. The player must then appoint a new army chief. To do so, a unit must be designated from a group that is not currently engaged in active combat. The new chief can be any unit type: a regular soldier, a group leader, a lieutenant, or a general if one exists. The appointment cannot happen instantly; it takes a set number of in-game days. During this period the army operates without a chief, compounding the morale loss. In campaign mode, the consequences of the army chief's death may also be defined by the specific mission parameters.

### Army Chief Role
Yes, the army chief can engage in direct combat as a standard unit of its type while also providing command bonuses to nearby units. Using the chief in combat carries the risk of losing the most important command figure in the army.

### Legions and Grouping
Any number of units from two upward can be grouped into a legion. The player can define the internal order of the legion (e.g., cavalry in front, infantry behind). The player manually positions units to form their own formation -- there is no preset formation grid. When moving, the group travels together in the order the player defined. Specific formation types (line, column, wedge, etc.) are still to be designed. Multiple squads can be grouped into higher-level formations based on the command hierarchy. Legions persist until disbanded; the player can disband and reassign them freely. Groups can also be merged with other groups or individual units, and can be disbanded at any time.

### Hierarchy Tree Panel
The panel shows an expandable/collapsible tree of all legions and their subgroups (similar to Windows Explorer tree view). It does not show individual units -- the player must use the map to see individual units after selecting a legion. Each legion entry displays the legion name (defaults to the commander's name, changeable by the player) and its commander rank. Clicking a legion entry selects it and can center the camera on it. It is part of the collapsible HOI4-style panel system.

---

## 6. Controls & UI

### Controls (Basic)
- **Left-click**: select unit
- **Left-click drag box**: select multiple units
- **Right-click on ground**: move selected units to position
- **Right-click on enemy**: attack target
- **Right-click on ally**: follow unit
- **Ctrl+click**: add/remove unit from selection
- **Shift+click** (or alternative key, TBD): queue orders
- **Right-click with selection > "Create squad"**: create legion from selected units

### Unit Selection
- **Single click**: selects one unit
- **Drag box**: selects all units within the drawn rectangle (small drags < 5px register as clicks / unit toggle)
- **Ctrl+click**: adds/removes a unit from the current selection

Hotkeys (Ctrl+1,2,3...) for saving and recalling group selections.

### Camera Controls
- WASD or arrow keys
- Middle mouse button drag
- Edge scrolling (mouse at screen edge)
- Click on minimap to pan

Additional camera features (to be confirmed): follow/center mode that tracks the selected unit or group when issued from the map or hierarchy panel.

### HUD Layout
The HUD is designed to be clean, orderly, and intuitive:
- **Top-right**: Minimap (clickable to expand to full-screen map view)
- **Left side**: Unit information panel, hierarchy tree panel, prisoner panel, and other secondary panels (collapsible, HOI4-style)
- **Center**: Main game map / viewport

Primary information (minimap, selected unit info) is always visible and prominent. Secondary panels (hierarchy tree, prisoners, morale overview) are accessible but do not clutter the default view. Specific positioning of remaining elements is still to be decided, with cleanliness as the guiding principle.

### Minimap
The minimap shows the full map with terrain and fog of war (only visible areas are displayed). It is clickable to pan the camera.

Displayed content depends on zoom level:
- At maximum zoom: individual units are shown as dots
- At medium zoom: only group leaders (Capogruppo) are shown
- At higher zoom: Luogotenente (small icons), Generale (medium icons), Capo dell'Esercito (large icon)
- At maximum zoom out: only the Army Chief icon and large legion indicators are visible

The minimap also shows a viewport rectangle indicating the current camera position and a red exclamation mark alert for groups at risk of surrendering.

### Unit Information Panel
When a single unit is selected, a panel displays that unit's individual statistics. When a group is selected, a panel displays the group's collective information. If only part of a group is selected, only that portion's information is shown. Health values are simple and readable. The player cannot see the health or status of enemy units unless those enemies are in direct close-range combat with the player's own units that have full line of sight.

### User Interface Philosophy
The interface is designed to be simple and approachable for new players but progressively reveals complexity as the player engages deeper systems. All panels are collapsible and toggleable in a style similar to Hearts of Iron IV. The currently planned panels are: the unit and group hierarchy tree, the minimap, the morale overview, the selected unit or group information panel, and the prisoner management panel. Additional panels may be added as development progresses.

### Hotkeys
Hotkey remapping will be implemented in a future phase of development.

### Screen Modes
Three display modes:
- **Fullscreen**: exclusive fullscreen at native resolution
- **Fullscreen (borderless windowed)**: with taskbar visible
- **Windowed**: fixed size (smaller than fullscreen, e.g., 1440x920 or similar that maintains proper aspect ratio)

The window is not freely resizable by the user. The UI scales to fit the selected resolution while maintaining aspect ratio.

### Loading Screens
Yes. Loading screens appear during transitions (menu to game, between missions, loading saves). They are minimal but polished -- showing a loading indicator and possibly a simple visual or tip, given that save files may take some time to load.

### Main Menu
Yes, but it will be implemented in a later phase. The initial focus is entirely on core gameplay mechanics.

### Settings
Graphics quality is fixed at a low level for the initial implementation and may be expanded later. Volume controls and FPS cap settings will also be added in a later phase. Default language is English for initial development. Italian and additional languages will be added later.

### Fonts
For the initial implementation, a clean, clearly readable sans-serif font (e.g., a standard system font or a freely licensed alternative like Open Sans or Roboto). Font styling and themed fonts (medieval-style, etc.) will be considered in a later visual polish phase.

### Language Support
English and Italian from the start. Additional languages may be added after the core game is stable.

### Development Console
A developer console accessible in-game (toggleable hotkey) provides:
- **FPS overlay** always available
- **Commands**: spawn units, delete units, set unit state/stance/team, teleport, change morale/stamina/health, trigger events, and other debug operations
- Console is available during development builds only

### Tutorial
No tutorial is planned for the current phase. Players are expected to explore the mechanics independently. Tooltips and a manual are not planned for the initial implementation -- they may be considered in a future phase.

### Multiplayer
Multiplayer is not planned for the initial release. After the single-player game is stable and bugs are resolved, offline multiplayer (hot-seat or shared-screen with save sharing) may be considered -- allowing friends to play in sandbox mode, cooperatively on the same nation, or as opposing nations.

### Audio
Not in the current phase. Sound effects and music may be added in a future phase of development.

---

## 7. Game Systems

### Time System
Five real-world minutes equals one in-game day. Movement distances are realistic relative to this scale; for example, a march equivalent to the distance between Rome and Vicenza would take between ten and fifteen in-game days depending on unit speed and terrain.

### Time Speed Controls
The player can set time to any of the following speeds at any moment: 0 (paused), 1/8, 1/4, 1/2, 1, 2, 4, or 8 times normal speed. At 1x speed one day lasts five real minutes. At 1/8 speed one day lasts approximately forty real minutes, enabling deep strategic play. At 8x speed one day lasts approximately thirty-seven seconds, suitable for rapid resolution of low-intensity periods.

### Save System
Both manual and automatic saving are supported. Multiple save slots are available. Save files use JSON format (via System.Text.Json) for readability and ease of debugging. Every state necessary to resume a session is saved: unit positions, health, status, morale, fatigue, command hierarchy, legion assignments, diplomacy state, territory control, fog of war progress, construction progress, and all other relevant data.

### Fog of War
Yes. Each unit has its own vision radius. Areas outside the combined vision of all player units are hidden. The fog of war also applies to the minimap, where unexplored or currently invisible areas appear dark. Hidden units always appear as circles until identified.

The fog of war implementation uses a CPU circle list approach (accumulated circles at 50 WU coarseness) after multiple iterations: GPU multi-RT, ping-pong RT, CPU low-res 1/3, GPU ping-pong full-res, world-space fog RT were each attempted before settling on the current CPU circle list approach which has no world limits, no GPU texture, no SetData stall, and perfect zoom/pan.

### Morale System
Yes. Each group or legion has its own independent morale value. Morale decreases when a group is surrounded, slowed, taking heavy casualties, or near its breaking point. At critically low morale, a group becomes less efficient in combat and risks surrendering entirely. When a group surrenders, the player loses control of that front.

Morale is hierarchical. Groups that are part of a larger command structure share an approximate average morale plus a multiplier based on the main legion that contains the commanding officer. Low morale in subgroups slightly affects the morale of the parent group above them.

When a group is at risk of surrendering, a red exclamation mark alert appears on the minimap, similar to the missile warning system in Jetpack Joyride. The player must notice and respond without the game pausing or forcing attention. There is no forced pause or pop-up interruption. The player must monitor the minimap actively and respond on their own initiative.

The player can also choose to surrender units intentionally. Captured prisoners can be converted into resources, labor that generates passive output, or new troops.

### Stamina / Fatigue
Yes. Units that march or fight for extended periods accumulate fatigue, which reduces their movement speed and combat effectiveness proportionally. Units can recover stamina by resting inside an encampment.

Fatigue is visible through color saturation. Player units are displayed in bright red at full stamina and progressively fade to a dull, desaturated red as they tire. Enemy units follow the same logic using blue. The enemy unit's fatigue state is only visible when they are in direct close-range combat with player units that have clear line of sight.

Detailed stamina parameters: MaxStamina (100 base +-5%), DrainRate (0.2/s, consumed only while moving), RecoveryRate (1.0/s, only while stationary). From full to empty: ~8 minutes moving. From empty to full: ~1.7 minutes stationary.

| Stamina % | Speed Mult | Effect |
|---|---|---|
| 100-60% | 1.0x | Normal |
| 60-30% | 0.7x | Affaticato |
| 30-15% | 0.3x | Stanco + 0.5 HP/s |
| <15% | 0.25x | Esausto + 2.0 HP/s |

### Resources
Not in the initial implementation. Future versions may include gold, passive food generation, and a dynamic reinforcement system where the arrival rate and quantity of reinforcements depends on player actions such as attacking, defending, or remaining stationary, combined with a randomness factor.

### Recruitment
In sandbox and freeplay modes, provinces and cities can contain training bases or similar structures where new units can be recruited. The player starts with a base set of units but can recruit additional forces over time. Reinforcements are limited and build slowly -- the system is still to be defined in detail. Recruitment speed, capacity, and availability will depend on the province or city infrastructure.

### Prisoner Management
Prisoners are tracked in a dedicated secondary panel accessible from the main panel list, consistent with the collapsible HOI4-style interface. The panel shows current prisoner count and their output in resources, labor, or potential troop conversion.

### Base Building
Base building is restricted to the sandbox and freeplay modes only. In campaign and story missions, gameplay is limited to pure field combat with no construction. The one exception is constructible bridges and camps, which are available in all modes.

### Encampments
Encampments provide three benefits: a defensive combat bonus when the group inside is attacked, a reduction in morale loss over time, and an improvement to war medic healing efficiency. Units must be inside the encampment to receive these benefits. Any unit can build an encampment. Construction takes 2-10 in-game days depending on how many units assist. Units building cannot attack and must be defended. If they enter combat, construction progress is lost over time. No resources are required.

### Bridge Construction and Destruction
Yes. Any unit can destroy a bridge. Any unit can construct a bridge given enough time. Construction takes 2-10 in-game days depending on assisting units. The following conditions must be met: at least one unit must be positioned on each bank of the river, the units engaged in construction cannot attack while building, and the construction site must not come under attack or it will be interrupted. Both sides of the river must be controlled by the same faction before construction can begin. Construction takes significant time and the building units are vulnerable throughout the process, requiring dedicated defense.

### Future Structures
Watchtowers and mini fortresses are planned for a future development phase. They will not be part of the initial implementation.

### Day and Night Cycle
Not currently planned, but noted as a strong candidate for a future feature. A day and night cycle would reduce visibility for all factions at night and potentially affect unit efficiency and behavior.

---

## 8. Factions & Teams

### Team System (Initial)
For testing and initial combat development, factions are represented by a simple `Team` enum with three values:
- **White**: No faction / test base. Default for all new units. Neutral -- usable as free-for-all or unaffiliated units.
- **Red**: Faction 1. Rendered in red shades.
- **Blue**: Faction 2. Rendered in blue shades.

Units on the same team never attack each other (White team attacks/gets attacked by everyone). The `team <id|all> white|red|blue` console command sets a unit's team. The unit's render color is determined by its team (Red=red, Blue=blue, White=white), with stamina-based desaturation applied.

### Faction Customization (Planned)
The player chooses a name and a color for their faction. AI factions are each assigned a distinct color. The color system must support the brightness and saturation variation used by the stamina system, so any chosen color can be rendered at full and reduced intensity to represent unit fatigue.

### Number of Factions
The player chooses freely. The minimum is one AI faction opposing the player. There is no defined maximum, though the engine's performance limits will determine a practical ceiling.

### AI Approach
AI development is deferred until all core mechanics, unit systems, and terrain systems are stable and implemented. The initial implementation will use simple aggressive behavior (rush toward player units). Tactical behaviors such as flanking, strategic retreats, and encirclement are planned for a later phase. The specific AI architecture (utility AI, behavior tree, state machine, etc.) has not been decided yet and will be determined when AI development begins. In sandbox mode, the AI manages its own resources, troop production, morale, territorial decisions, and diplomacy autonomously. Future versions will support multiple AI factions operating simultaneously on the same map.

### AI Alliances and Diplomacy
Yes. AI factions independently evaluate their strategic situation and may form alliances with each other if it is in their interest. The player may find multiple AI factions cooperating against them.

The player can interact diplomatically during a game. Available diplomatic actions include:
- **Non-aggression pacts**: mutual agreement not to attack each other
- **Temporary alliances**: short-term military cooperation
- **Truces**: cease-fire agreements
- **Peace treaties**: territorial settlements to end a war
- **Betrayal**: breaking an existing agreement

Reputation starts at a mid-level value. Each day that passes increases reputation very slightly. Breaking agreements or betraying allies decreases reputation and generates hostility. Hostility also spreads: if the player breaks an agreement with a faction, that faction's allies also gain hostility toward the player. Low reputation makes future negotiations significantly harder.

### Game Start Flow
In sandbox/freeplay mode, the player goes through a setup phase where they choose:
- Map
- Faction name and color
- Number of AI factions
- AI difficulty settings (if applicable)

At the start of the match, the player has a pre-organized mini army with pre-configured legions and command hierarchy. Before unpausing, the player can reorganize legions, reassign commanders, adjust formations, and position units. The game begins paused to allow this setup.

---

## 9. Game Modes

### Campaign
Story-driven missions on a world map with branching paths. Each mission has its own specific victory and defeat conditions (to be defined per mission). The campaign uses a series of missions with branching paths, presented on a World Conqueror 4-style world map. The player can see and navigate between territories. Each mission is fresh: nothing is carried over between missions -- no global resources, upgrades, or tech trees. The world map uses provinces with defined territory borders and capitulation conditions. Units belong to provinces but can move freely within them (no grid). Territory control works similarly to War of Dots -- units conquer and control territory by presence. Missions have varied objectives including capturing a specific point, defending for a set duration, escorting the commanding officer, eliminating all enemy forces, and combinations of these objectives.

### Versus-AI Skirmish
Single-map battles with defined objectives.

### Sandbox
Permanent free-play mode with AI opponents. No victory or game over condition -- the player can continue observing even after losing all units, watching other factions interact.

### Freeplay
Like sandbox, but with win/lose conditions. The player wins by controlling 70% of all territories (including allies and puppets) while not at war. Game over occurs if the player is fully conquered. However, if a peace treaty leaves the player as an independent nation or puppet with reduced territory, the game continues.

### Story and Narrative
Not yet defined. Story, narrative, and campaign-specific content will be designed after the core mechanics, unit systems, and terrain systems are fully implemented and stable.

---

## 10. Technical Architecture

### Code Architecture
The solution is split into multiple assemblies for separation of concerns and testability:

```
MedievalWarSim.sln
├── MedievalWarSim.Game/          -- Main executable (MonoGame project)
│   ├── Game1.cs                  -- MonoGame Game class
│   ├── Screens/                  -- Game states (Menu, Playing, Paused)
│   └── Content/                  -- Assets (.mgcb)
├── MedievalWarSim.Core/          -- Core library (no MonoGame dependency)
│   ├── Camera.cs                 -- Camera state (X, Y, Zoom), transforms (ScreenToWorld, WorldToScreen)
│   ├── Components/               -- Health, Position, Move, Stamina, Vision, UnitType
│   ├── Data/                     -- UnitStats, stat tables and roll methods
│   ├── DataStructures/           -- SpatialGrid
│   ├── Enums/                    -- UnitType, TerrainType, Stance, etc.
│   └── Managers/                 -- EntityManager (SoA arrays, free-list O(1) create/destroy)
├── MedievalWarSim.Game/          -- Main executable (MonoGame project)
│   ├── Game1.cs                  -- MonoGame Game class
│   ├── CameraController.cs       -- Input: WASD pan, scroll zoom, middle-mouse drag
│   ├── Screens/                  -- Game states (Menu, Playing, Paused)
│   └── Content/                  -- Assets (.mgcb)
├── MedievalWarSim.Rendering/     -- Rendering (MonoGame-dependent)
│   ├── Shapes/                   -- ShapeRenderer: polygon fill/border, circle, rectangle drawing
│   └── ...
├── MedievalWarSim.UI/            -- UI panels
│   ├── Panels/                   -- Minimap, Info, Hierarchy, Prisoner
│   └── Controls/                 -- Buttons, sliders, tree view
└── MedievalWarSim.Tests/         -- Unit tests (xUnit)
```

**Pattern**: Component-based composition with a system-update loop. Entities carry components (HealthComponent, PositionComponent, etc.) and systems process them each frame. This is similar to ECS but with simpler C# objects for iteration speed. An actual ECS library may be adopted later if profiling shows it necessary for 2000-unit performance.

**Key principles**: Game logic in Core has zero dependency on MonoGame, making it testable. Rendering and UI are thin layers that read state and draw.

### Technology Stack (Detailed)
- **Runtime**: .NET 8 (LTS) -- stable, cross-platform, good performance
- **Framework**: MonoGame 3.8.x -- mature 2D engine
- **UI**: Custom UI system built on MonoGame SpriteBatch (no external UI library) -- full control over HOI4-style collapsible panels, hierarchy tree, and minimap
- **ECS Architecture**: Optional -- may use an ECS library (e.g., Arch or DefaultEcs) for managing hundreds of units if performance requires it; otherwise plain GameObject composition
- **Serialization**: System.Text.Json for save files and configuration data
- **Audio**: MonoGame's built-in audio (SoundEffect) -- deferred to future phase
- **Input**: MonoGame's built-in Keyboard, Mouse input handling
- **Content Pipeline**: MonoGame Content Pipeline (.mgcb) for asset management
- **Rendering**: SpriteBatch for 2D drawing of geometric shapes, terrain, and UI

### Development Approach
Incremental and methodical. Each system is implemented, tested, and benchmarked independently before moving to the next. FPS benchmarking is part of every phase to ensure the engine can handle five hundred to one thousand or more simultaneous units before complexity increases. No system is added until the foundation beneath it is confirmed to be stable and performant.

### Initial Development Roadmap

#### Phase 0 -- Project Setup
- Create solution with all projects (Game, Core, Rendering, UI, Tests)
- MonoGame empty project with Game1 loop
- Dev console with basic commands (spawn, delete, set position, help)
- FPS overlay

#### Phase 1 -- Single Unit
- Render a geometric shape (pentagon) on screen
- Unit entity with PositionComponent, ShapeComponent
- Camera pan (WASD/middle mouse/edge scroll) and zoom (scroll wheel)
- Select unit with left-click (raycast against shape bounds)
- Right-click to move unit (simple direct movement, no pathfinding yet)

#### Phase 2 -- Multiple Units
- Render configurable number of units
- Drag-box selection
- Ctrl+click to add/remove from selection
- Move selected units toward clicked position (formation-relative offsets)
- Basic collision avoidance (simple separation steering)

#### Phase 3 -- Groups and Legions
- Group selected units into a legion via right-click context menu
- Group movement following leader in formation order
- Hierarchy tree panel (read-only initially)
- Dev console commands for legion management

#### Phase 4 -- Terrain and Input Polish
- Render terrain provinces with smooth borders (colored edges, no fill)
- Basic spatial grid for local movement/pathfinding
- Terrain speed modifiers affecting movement
- Right-click on enemy selection stub (no combat yet)

#### Phase 5 -- Benchmarking
- Stress test with 500, 1000, 2000 units
- Optimize rendering (LOD, batching)
- Optimize update loop (spatial queries, sleeping dormant units off-screen)
- Ensure 100+ FPS target before adding any combat system

Each phase must be stable and performant before moving to the next.

---

## 11. NEXT STEPS: Core Unit Mechanics

Before implementing the LOD system, development will focus on:
1. **Combat System**: Auto-attack logic, damage calculation, and unit-type advantages.
2. **Stances**: Implementation of **Hold**, **Attack**, and **Move** behaviors.
3. **Morale & Stamina**: Finalizing the impact of fatigue and fear on unit performance.

---

## 12. DEVELOPING

### 16/05/2026 — Mouse click detection, focus handling, thread safety

**Bug:** Game didn't register mouse clicks (left-click on units did nothing).

**Root cause:** `IsClickOnGameWindow()` used `GetActiveWindow()` to detect the game window handle, but SDL2 creates TWO windows: an internal helper (`GetActiveWindow()` returned e.g. `0xF038C`) and the visible game window (`GetAncestor(WindowFromPoint(...))` returned e.g. `0x10140`). The handle comparison always failed.

**Fix:** Replaced `GetActiveWindow()` with `Process.MainWindowHandle` (returns the real visible window) and `GetForegroundWindow()` (checks if the game is actually focused). Removed the old `WindowFromPoint` + `GetAncestor` + `ClientToScreen` approach.

**Bug:** `showclick true` crashed the game when clicking while the System.Console window had focus.

**Root cause:** The game thread called `System.Console.WriteLine()` (debug output) while the DevConsole background thread was reading input via `System.Console.ReadKey()` — concurrent access to the console from two threads.

**Fix:** Debug output only runs when `onGame == true` (game window has focus → console thread is idle).

**Bug:** Ctrl+click on empty space cleared the selection.

**Fix:** Changed `HandleClick` empty-space case from `_selectedUnitIds.Clear()` to `else if (!addToSelection)`.

**Housekeeping:**
- `showclick` command + `_showClick` field + debug block commented out in `GameScreen.cs` for future use
- `// showclick` appears at the bottom of the help list to document its existence
- DevConsole thread-safety additions (`volatile`, `__close__`, `Join`) reverted — not needed without `showclick` since the game thread never writes to `System.Console` during normal gameplay

**Current state:**
- Mouse clicks register only when the game window has focus (`Process.MainWindowHandle` + `GetForegroundWindow()`)
- Console commands (`create`, `remove`, `set`, `info`, `selected`) work for entity management
- Selection persists across console commands
- Ctrl+click toggles unit selection; Ctrl+click on empty space preserves selection
- Unit renderer (`ShapeRenderer`) and game screen (`GameScreen`) implement `IDisposable`; `Game1.UnloadContent` calls dispose
- EntityManager uses free-list O(1) allocation + HighWaterMark + bounds checking

**Known issues:**
- First click on the game window after focusing the console is always ignored (`GetForegroundWindow()` still returns the console window handle during that frame)

### 17/05/2026 — Refactor + camera + culling + unit speed + movement + selection + shapes + facing

- **Drag-to-select**: hold left button on game window → drag a selection rectangle → release to select all units inside. Small drags (<5px) register as clicks (unit toggle).
- **Ctrl+drag / Ctrl+click**: appends to or toggles selection instead of replacing.
- **ShapeRenderer.DrawRectangle()**: new method with 1x1 pixel texture for filled rectangles + borders (used for selection box rendering).
- **Selection box visuals**: semi-transparent green fill + lime border drawn during drag.
- **Crash log**: unhandled exceptions written to `crash.txt` via try-catch + `AppDomain.UnhandledException`.
- **`create random [count]`**: optional count parameter (>=1) to spawn multiple units (e.g. `create random 10`).
- **Right-click movement**: select units, right-click → all move to that point.
- **`MoveComponent`** (TargetX, TargetY, Speed, IsMoving) per entity.
- **`move <id> <x> <y>` / `move random`**: console movement commands.
- **`info` / `selected`**: coords use `;` separator; `info` shows target + Moving flag.
- **Focus detection**: checks foreground window PID against our process — no cached handle, works even if console was opened as first action.
- **DevConsole.Close()**: double-join ensures background thread exits before FreeConsole → Open, fixing `IOException: Handle non valido`.
- **Per-unit speed by type**: `Core/Data/UnitStats.cs` with dictionary-based unit stats (speed). Infantry=100, Archer=95, Cavalry=175, Ballista=50, Medic=100 px/s. ±5% random variance on creation.
- **`set speed <value|default>`**: console command to set or reset speed.
- **`info selected`**: shows info for single selection, lists all if multiple.
- **`move selected <x> <y>`**: moves all selected units to specified coordinates.
- **`set type` now recalculates speed**: changing unit type resets speed via `UnitStats.RollSpeed(newType)`.
- **HWND-based focus detection**: `FindWindow("MedievalWarSim")` + `GetForegroundWindow()` — works with both AllocConsole open.
- **Removed unused code**: PID-based focus check, `RestoreGameWindow`, `_consoleWasOpen` etc.
- **Refactored GameScreen.cs** (622 lines → 4 partial files in `Screens/GameScreen/`):
  - `GameScreen.cs` — fields, constructor, Dispose, P/Invokes
  - `GameScreen.Commands.cs` — RegisterCommands
  - `GameScreen.Input.cs` — ProcessMouseInput, HandleClick
  - `GameScreen.Update.cs` — Update, Draw, PrintUnitInfo
- **Camera system extracted**:
  - `Core/Camera.cs` — pure state (X, Y, Zoom) + transforms (ScreenToWorld, WorldToScreen). No MonoGame dependency.
  - `Game/CameraController.cs` — WASD pan, middle-mouse drag, scroll zoom with zoom-towards-mouse (world point under cursor stays fixed).
- **Zoom limits**: Min=0.25x (fully zoomed in), Max=4x (fully zoomed out). Scroll up = zoom in (x1.1), scroll down = zoom out (x0.9).
- **Debug overlay** (SpriteFont via content pipeline): FPS counter top-right, zoom level bottom-right.
- **`zoom` command**: shows current zoom + limits in console.
- **Culling system** (3 zones):
  - **Visible** (≤200px outside viewport): draw + update every frame.
  - **Intermediate** (200-400px outside): skip draw, update every frame.
  - **Far** (≥400px outside): skip draw, update every 5th frame.
- **Bug fixes**: removed duplicate `float dt` in Update; fixed namespace conflict `MedievalWarSim.Game` ↔ `Game` class (Game1.cs now uses fully qualified `Microsoft.Xna.Framework.Game`); cleaned up unused usings.
- **Shape per tipo**: formas geometricas per ogni unità (Infantry=quadrato, Archer=triangolo, Cavalry=pentagono, Ballista=ottagono, Medic=esagono).
- **ShapeRenderer.DrawShape()**: pre-renderizza texture poligonali (fill + border, anti-aliased) con parametro `rotation`.
- **FacingAngle** in `MoveComponent`: aggiornato durante il movimento (`atan2(dy,dx) + PI/2`), l'unità ruota verso la direzione.
- **`UnitTypeToSides()`**: mapping in `GameScreen`.
- **Random type on spawn**: `create random`, `create <x> <y>`, e unità iniziale usano tipo casuale invece di Infantry fisso.
- **`create <type> <x> <y>`**: crea unità di tipo specifico (per nome o ID). Es: `create cavalry 500 300`, `create 3 100 200`.
- **Per-type radius**: `UnitStats.BaseRadius`. Infantry/Cavalry=16, Archer/Medic=14, Ballista=20 (1.25×). Click detection e culling usano il raggio dell'unità.
- **Even-sided polygon offset**: poligoni con lati pari (4, 6, 8) ruotati di `π/sides` — lato piatto davanti invece di vertice.
- **HealthComponent**: MaxHP, CurrentHP. Per-type BaseHP + RollHP ±5%. Morte quando HP ≤ 0 → Destroy.
- **`health <id|all> add|remove|set <amount>`**: comando per manipolare HP.
- **Health bar**: barra 3px sopra ogni unità danneggiata (larghezza 0.85× diametro). Sfondo scuro + bordo bianco sempre visibile, fill verde/giallo/rosso.
- **UnitStats ottimizzato**: `Dictionary<UnitType, ...>` → array `_stats[(int)type]`. `UnitStatData` da `record` class a `record struct`.
- **`set` rimosso**, rimpiazzato da comandi separati:
  - `speed <id|all> set <val> | random`
  - `type <id|all> set <typename|id> | random`
  - `select <id> | all` / `deselect <id> | all`
- **`info` stampa HP**: mostra `HP: current/max`.

### 17/05/2026 — Collision system + spatial grid + stuck detection + performance fixes + bugfixes

- **SpatialGrid** (`Core/DataStructures/SpatialGrid.cs`): cella 200×200px, rebuild a ogni frame. `Query(x, y, radius, result)` su celle 3×3. Usata per trovare vicini in collisione.
- **Sliding collision**: durante il movimento, per ogni unità si querya la grid, si separano le posizioni sovrapposte e si rimuove la componente di velocità verso l'ostacolo. Unità ferma non viene spostata.
- **StuckTimer** in `MoveComponent`: reset quando si setta `IsMoving = true` e quando arriva a `dist < 1f`.
- **Stationary separation pass**: dopo il movimento, risolve overlap anche per unità ferme (es. spawnate sullo stesso punto). Split 50/50 se entrambe ferme o entrambe in movimento; solo l'unità in movimento viene spostata se l'altra è ferma.
- **Exact overlap fix**: se due unità hanno `rDistSq < 0.0001f` (stessa posizione), push in direzione random invece di `continue`. Applicato in entrambi i pass.
- **Stuck detection evoluta**:
  - **Iterazione 1 — speed assoluta**: `effectiveSpeed < 1f` (px/frame) — FPS-dipendente, bug ad alto FPS.
  - **Iterazione 2 — speed in px/s**: `effectiveSpeed / dt < 1` — FPS-indipendente, ma unità in orbita hanno alta velocità laterale e non triggerano.
  - **Iterazione 3 — velocità radiale**: `(vx·dx + vy·dy) / (dist·dt) < 10` — osserva solo la componente verso il target. L'oscillazione frame-to-frame resettava il timer.
  - **Iterazione 4 — progresso netto (finale)**: ogni 0.5s si misura `PrevDist - dist` (progresso netto verso il target). Se < 1px in 0.5s per 2 check consecutivi (1s), unità ferma. Indipendente da oscillazioni istantanee.
- **Stationary unit non spostata**: nella stationary separation, se una delle due unità è ferma e l'altra no, solo quella in movimento viene spostata.
- **Performance fixes**:
  - `SpatialGrid.Clear()` ora riutilizza le List (`foreach list.Clear()`) invece di allocare nuove ogni frame.
  - Stationary separation con far culling (`_tick % FarUpdateInterval` per entità lontane).
  - `IsGameFocused()` chiamato una volta sola per frame e passato a `ProcessMouseInput`.
  - `remove all` usa `_selectedUnitIds.Clear()` invece di `Remove(i)` per ogni entità.
  - `type random` genera fresh per ogni entità (single e all consistenti).

### 17/05/2026 — Fog of war persistente (stile StarCraft / War of Dots)
- **VisionComponent**: per-unità `SightRange` ±5% random. Archer=350, Ballista=320, Cavalry=180, Infantry=150, Medic=150.
- **`vision unit <id>` / `vision all` / `vision off`**: comando per attivare/disattivare nebbia, singola unità o tutte.
- **FogBlend statico** (`Zero/SourceColor`): moltiplicazione sul backbuffer. _rtFinal: 255=visibile, 180~0.7=esplorato, 0=inesplorato.
- **Nessun bordo cerchio**: solo contrasto luminosità tra rivelato (×1) e nebbia (×<1), stile War of Dots.

### 17/05/2026 — Fog of war: iterazioni tecniche
1. **GPU multi-RT (tentativo)**: 3 RTs (visione + esplorato + combine). Letto dopo scritto → nero (driver bug: read-after-write stesso frame).
2. **Ping-pong RT**: 2 RTs, scrittura frame N, lettura frame N+1. Fallito per stesso problema GPU.
3. **CPU low-res 1/3**: accumulo `Color[]` su CPU a 1/3 risoluzione, `SetData` ogni frame. Funzionava ma FPS 3000→300 con 1 unità (SetData stall).
4. **GPU ping-pong full-res**: 2 RT PreserveContents, accumulo diretto su GPU. FPS ok ma zoom errava (screen-space, non world-space).
5. **World-space fog RT**: texture 4096×4096, 8 WU/texel, matrice view per proiezione corretta. Limitato a 32768 WU copertura.
6. **CPU circle list (attuale)**: `List<(wx,wy,radius)>` + deduplica a 50 WU. Nessun limite mondo, nessuna texture GPU, nessun SetData, zoom/pan perfetti.

### 18/05/2026 — Ottimizzazioni Core + Scala Ultra-HD (1m = 40px)

**Ottimizzazioni Strutturali:**
- **EntityManager O(N)**: Implementata la gestione delle entità attive tramite swap-on-destroy. I loop di update e draw ora iterano solo sulle unità effettivamente vive, garantendo prestazioni stabili anche dopo migliaia di spawn/destroy.
- **SpatialGrid Ottimizzata**: Il metodo `Clear()` ora pulisce solo le celle "sporche" utilizzate nel frame precedente, riducendo drasticamente il carico sulla CPU ad ogni tick.
- **DevConsole PID Check**: Il controllo del focus della console ora utilizza il PID del processo, risolvendo bug di input quando il gioco viene avviato o rifocalizzato.

**Nebbia di Guerra (FOW):**
- **Seamless Exploration**: Implementato `BlendState.Max` per l'accumulo dei cerchi esplorati. Eliminati gli artefatti visivi (giunture) tra le aree scoperte.
- **Vision Reset**: Corretto il bug che permetteva alle nuove unità di ereditare la visione da ID entità riutilizzati.

**Sistema Ultra-HD Realistico (1m = 40px):**
- **Nuova Scala**: Transizione a una scala metrica reale dove 40 pixel corrispondono a 1 metro.
- **Parametri Unità**:
    - **Fanteria**: Raggio 30px (1.5m), Velocità 200px/s, Visione 6000px.
    - **Cavalleria**: Raggio 40px (2.0m), Velocità 600px/s, Visione 12000px.
    - **Arciere**: Raggio 28px (1.4m), Velocità 180px/s, Visione 9000px.
    - **Ballista**: Raggio 55px (2.75m), Velocità 60px/s, Visione 9000px.
    - **Medico**: Raggio 28px (1.4m), Velocità 240px/s, Visione 6000px.
- **Grafica Smooth**: Raddoppiata la risoluzione delle texture interne dello `ShapeRenderer` (da 128px a 256px di diametro) per garantire bordi anti-aliased cristallini.
- **Camera Dinamica**: Esteso il limite di Zoom Out a 30x per permettere la gestione tattica di campi di battaglia vasti chilometri.
- **Margini di Rendering**: Aumentati a 2500px per supportare le alte velocità senza effetti di comparsa improvvisa (pop-in).

### 19/05/2026 — Aggiornamento valori definitivi unità (Speed/HP/Radius)

- **Nuovi valori unità** (design definitivo, ±5% roll):

| Unit | Speed | Radius | HP | Sight | MaxStamina |
|---|---|---|---|---|---|
| Infantry | 160 | 30 | 110 | 1500 | 100 |
| Archer | 180 | 28 | 60 | 2250 | 100 |
| Cavalry | 500 | 40 | 90 | 3000 | 100 |
| Ballista | 50 | 55 | 150 | 2250 | 100 |
| Medic | 200 | 28 | 80 | 1500 | 100 |

- **README**: aggiornata tabella stats sezione 3, aggiunta tabella Combat Stats (AttackRange/Damage/Cooldown/DPS) sezione 4 con matchup 1v1
- **UnitStats.cs**: allineato codice ai nuovi valori Speed/HP/Radius

### 19/05/2026 — Team System (5 squadre colorate)

- **Team enum**: `White, Red, Blue, Green, Yellow` in `Enums/Team.cs`
- **TeamComponent**: struct con campo `Team` in `Components/TeamComponent.cs`
- **EntityManager**: array `_teams` + `GetTeam()` getter
- **TeamColors**: helper `TeamColors.GetColor(team)` → `Color`, in `Game.Data/TeamColors.cs` (5 colori: White, Red(200,60,60), Blue(60,100,220), Green(60,180,60), Yellow(220,200,40))
- **Comando `create`**: ora accetta team opzionale come 4° argomento o dopo count in `create random`, default `White`
  - `create Infantry 400 300` → White
  - `create Infantry 400 300 Red` → Red
  - `create random 10 Blue` → 10 unità random Blue
- **Comando `team`**: `team <id|all> white|red|blue|green|yellow` (o 0-4)
- **Rendering**: colore unità ora determinato dal team, stamina desaturation fluida sopra colore team (invece di `Color.Red` fisso)
- **`info`**: mostra team dell'unità
- **Stamina desaturation**: `Color.Lerp(teamColor, grey(100,100,100), 1 - stamina%)` — non più `Color.Red` fisso

### 19/05/2026 — Stamina System + Fix iterazione codice morto

**Stamina System:**
- **`StaminaComponent`**: `MaxStamina` (100 base ±5%), `CurrentStamina`, `DrainRate` (0.2/s), `RecoveryRate` (1.0/s)
- **Consumo**: solo in movimento (`move.IsMoving`), drain lineare 0.2/s → ~8 min da pieno a vuoto
- **Recupero**: solo da fermo, 1.0/s → ~1.7 min da vuoto a pieno
- **Speed multiplier** (applicato a `step` prima del movimento):

| Stamina | Speed mult | Effetto |
|---|---|---|
| 100–60% | 1.0× | Normale |
| 60–30% | 0.7× | Affaticato |
| 30–15% | 0.3× | Stanco + 0.5 HP/s |
| <15% | 0.25× | Esausto + 2.0 HP/s |

- **Colore desaturazione fluida**: `Color.Lerp(Color.Red, Color(100,100,100), 1 - stamina%)` in entrambi i Draw branch (vision e normal)
- **Death cleanup**: entity con HP ≤ 0 da stamina drain distrutte in pass separato (iterazione backward su snapshot per sicurezza)
- **Comandi console**: `stamina <id|all> add|remove|set|random [amount]` con supporto ±5% roll
- **`info`**: mostra stamina `Current/Max`
- **UnitStats**: `BaseMaxStamina` (100 flat per tutti i tipi) + `RollMaxStamina()`

**Code quality fixes:**
- **HandleClick** e **drag-select**: ora iterano `ActiveEntities` (solo entità vive) invece di `HighWaterMark` + `IsAlive` — eliminata scansione di slot morti
- **Stamina drain loop**: non chiama più `Destroy()` dentro `foreach` su `ActiveEntities` (swap corrompe iterazione), usa death cleanup backward pass separato
- **Focus detection fix**: `IsGameFocused()` confronta HWND diretto (`fw == _gameWindowHandle`) invece di `pid == _processId` — AllocConsole ha lo stesso PID del gioco, quindi con `pid` check la console faceva passare drag/click sul game window. Rimosso `_processId` e `GetWindowThreadProcessId` inutilizzati.

### 19/05/2026 — Refactoring GameScreen + Performance Optimization

**Refactoring (10 partial class file):**
- `GameScreen.cs` splittato da 2 file (Commands 809L + Update 513L) a **10 file**, nessuno >250L
- **Commands**: diviso in 5 file (basic, create, move, units, vision)
- **Update/Draw/Info**: Update separato da Draw e PrintUnitInfo
- Rimossa property `GetUnitRadius()` inline `UnitStats.GetBaseRadius()`

**Performance:**
- **Facing angle caching**: `MoveComponent.PrevTargetX/Y` — `Atan2` solo quando target cambia
- **SpatialGrid Insert**: `CollectionsMarshal.GetValueRefOrAddDefault` — eliminato doppio dictionary lookup
- **GetUnitRadius inline**: rimossa 1 indirezione, `UnitStats.GetBaseRadius()` diretto
- **Unused import**: `GraphicsDevice` rimosso da `Update.cs`

**Esplorato persistente (fix memory leak):**
- Aggiunto `HashSet<long> _exploredCellKeys` — celle 200x200 marcate UNA volta per area
- Sostituito `RemoveAt(0)` cap (perdeva aree vecchie) con deduplica cell-based
- Memoria O(area esplorata) non O(tempo x unita) — mondo 100km2 circa 8MB
- Nessuna perdita di esplorazione: cella marcata resta esplorata per sempre

**Bug fix:**
- Comando `team`: rimosso `[id]` in piu dalla usage string
