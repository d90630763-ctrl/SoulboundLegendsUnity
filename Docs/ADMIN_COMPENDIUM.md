# SOULBOUND LEGENDS - ADMIN COMPENDIUM
Generated from the game's data tables. Coordinates are world metres (x = east, z = north) inside each zone; (0,0) is the plaza centre.

## 1. Progression overview
- Players start at Level 0 in Dawnrest Village (instanced; 12 players per instance).
- XP needed to go from Lv0 to Lv10: 1575. Max level: 100.
- Level 3: awaken the elemental inheritance (Elder Maren). Level 10 + promotion quest 'Trial of Ascension' unlocks the Kingdom Token, the village portal to Aurelion Kingdom (24 players per instance, extra kingdoms open when full), top-up and modes.
- LV Up Pill: levels 1-9 only; becomes claimable once 5 players have completed the promotion.
- Skills: max 10 equipped at once; the HUD shows 8 per page.

## 2. NPCs and what they give
### Dawnrest Village
| NPC | Id | Role | Location | Quests given |
|---|---|---|---|---|
| Elder Maren | elder | quest | (-9, 4) town centre, north-west | Welcome to Dawnrest; Gloop Trouble; Awaken the Inheritance; The Wolf Pack; Stone Guardians; Trial of Ascension |
| Borin the Smith | smith | smith | (11, 8) town centre, north-east | Metal for Borin; A Proper Blade |
| Sylva the Alchemist | alchemist | alchemist | (13, -6) town centre, south-east | Herbs for Sylva; First Brew |
| Tamsin | innkeeper | lore | (-12, -6) town centre, south-west | Tavern Gossip; Hidden Caches |
| Guard Roan | guard | guard | (26, 43) inner fields, north-east | Across the Bridge; Venom in the Air |

### Aurelion Kingdom
| NPC | Id | Role | Location | Quests given |
|---|---|---|---|---|
| Seneschal Valen | kguide | quest | (6, -14) town centre, south-east | Welcome to Aurelion |
| Hale the Armorer | ksmith | smith | (-17, 2) town centre, west | Armor for the Guard |
| Orla the Brewer | kalch | alchemist | (17, 2) town centre, east | Brewer's Order |
| Mabel the Cook | kcook | quest | (-12, -14) town centre, south-west | Hearty Stew |
| Pike the Fisher | kfisher | quest | (12, -14) town centre, south-east | River Lore |
| Dorn the Farmer | kfarmer | quest | (-24, -8) town centre, west | The Herb Harvest |
| Kessa the Hunter | khunter | quest | (24, -8) town centre, east | Beast Season; Drake Hunt |
| Gruff the Miner | kminer | quest | (-27, -20) town centre, south-west | Ore for the Crown |
| Zara the Merchant | kmerchant | quest | (27, -20) town centre, south-east | Leather Trade |
| Ilyan the Scribe | kscribe | quest | (-8, 8) town centre, north-west | Census |
| Captain Marek | kcaptain | quest | (8, 8) town centre, north-east | Wraith Patrol; Ashen Menace |
| Opal the Jeweler | kjeweler | quest | (-26, 12) town centre, north-west | A Stone for the Queen |
| Wilm the Tailor | ktailor | quest | (26, 12) town centre, north-east | Winter Coats |
| Lyra the Bard | kbard | quest | (-4, -18) town centre, south | A Song for Aurelion |
| Brother Aldous | kpriest | quest | (4, -18) town centre, south | Winged Terrors |

## 3. All quests
| Quest | Id | Given by | Min Lv | Needs first | Objectives | Rewards |
|---|---|---|---|---|---|---|
| Welcome to Dawnrest | q_intro | elder | 0 | - | Speak to Borin the Smith (1) | 25 XP, 5g, Minor Healing Potion x2 |
| Gloop Trouble | q_gloop | elder | 0 | q_intro | Defeat Mossy Gloops (5) | 45 XP, 10g, Minor Healing Potion x2 |
| Herbs for Sylva | q_herbs | alchemist | 0 | q_intro | Collect Redleaf (5); Collect Bluebell (3) | 40 XP, 8g, Minor Mana Potion x2 |
| First Brew | q_brew | alchemist | 0 | q_herbs | Brew Minor Healing Potions (2) | 35 XP, 5g, Redleaf x3 |
| Metal for Borin | q_forge | smith | 1 | q_intro | Collect Timber (4); Collect Iron Ore (3) | 50 XP, 10g, Tough Hide x2 |
| A Proper Blade | q_sword | smith | 3 | q_forge | Forge an Iron Sword (1) | 70 XP, 20g, Ember Ring x1 |
| Awaken the Inheritance | q_awaken | elder | 3 | q_gloop | Reach Level 3 (3); Choose your inheritance (1) | 90 XP, 0g, Wood Chest x1 |
| The Wolf Pack | q_wolves | elder | 3 | q_awaken | Defeat Fangrunners (6) | 110 XP, 20g, Dark Iron Chest x1, Minor Healing Potion x3 |
| Across the Bridge | q_patrol | guard | 2 | q_intro | Cross the river bridge (1) | 50 XP, 10g, Minor Mana Potion x2 |
| Venom in the Air | q_spitters | guard | 4 | q_patrol | Defeat Venom Wisps (4) | 140 XP, 25g, Healing Potion x2 |
| Tavern Gossip | q_lore | innkeeper | 0 | - | Speak to Elder Maren (1); Speak to Borin (1); Speak to Sylva (1) | 40 XP, 8g, Minor Healing Potion x2 |
| Hidden Caches | q_chests | innkeeper | 2 | q_lore | Open chests (3) | 70 XP, 15g, Lucky Charm x1 |
| Stone Guardians | q_golems | elder | 6 | q_wolves | Defeat Stone Golems (2); Carry Iron Ore (3) | 220 XP, 40g, Iron Armor x1, Bronze Chest x1 |
| Trial of Ascension **(promotion)** | q_ascension | elder | 10 | q_golems | Reach Level 10 (10); Defeat the Ancient Warden (1) | 500 XP, 100g, Kingdom Token x1, Silver Chest x1 |
| A Friendly Face | h_hello | HIDDEN (discovered by doing) | 0 | - | Use an emote near another player (1) | 40 XP, 10g, Lucky Charm x1 |
| The Forager | h_forager | HIDDEN (discovered by doing) | 0 | - | Harvest herbs (15) | 80 XP, 15g, Dark Iron Chest x1 |
| Echoes in the Ruins | h_ruins | HIDDEN (discovered by doing) | 0 | - | Find the ancient ruins (1) | 60 XP, 10g, Wood Chest x1 |
| Welcome to Aurelion | k_welcome | kguide | 10 | - | Speak to Hale the Armorer (1); Speak to Orla the Brewer (1) | 300 XP, 80g, Bronze Chest x1 |
| Armor for the Guard | k_armor | ksmith | 10 | - | Forge Iron Armor (1) | 250 XP, 60g, Bronze Chest x1 |
| Brewer's Order | k_potions | kalch | 10 | - | Brew Healing Potions (3) | 220 XP, 50g, Healing Potion x2 |
| Hearty Stew | k_cook | kcook | 10 | - | Bring Greenmoss (6); Bring Redleaf (4) | 200 XP, 40g, Healing Potion x3 |
| River Lore | k_fisher | kfisher | 10 | - | Reach the river (1) | 150 XP, 30g, Bronze Chest x1 |
| The Herb Harvest | k_farmer | kfarmer | 10 | - | Harvest herbs (10) | 220 XP, 40g, Healing Potion x2 |
| Beast Season | k_hunter | khunter | 10 | - | Defeat Fangrunners (8) | 260 XP, 50g, Tough Hide x4 |
| Drake Hunt | k_drake | khunter | 14 | k_hunter | Defeat Ember Drakelings (2) | 700 XP, 150g, Gold Chest x1 |
| Ore for the Crown | k_miner | kminer | 10 | - | Bring Iron Ore (10) | 240 XP, 60g, Bronze Chest x1 |
| Census | k_scribe | kscribe | 10 | - | Meet Mabel (1); Meet Pike (1); Meet Dorn (1); Meet Lyra (1) | 180 XP, 30g, Lucky Charm x1 |
| Leather Trade | k_merchant | kmerchant | 10 | - | Bring Tough Hide (6) | 150 XP, 120g, - |
| Wraith Patrol | k_captain | kcaptain | 10 | - | Defeat Grave Wraiths (6) | 400 XP, 80g, Silver Chest x1 |
| Ashen Menace | k_brutes | kcaptain | 12 | k_captain | Defeat Ashen Brutes (4) | 520 XP, 100g, Silver Chest x1, Knight Plate x1 |
| A Stone for the Queen | k_jeweler | kjeweler | 10 | - | Bring Crystal Shards (3) | 300 XP, 90g, Sun Crest x1 |
| Winter Coats | k_tailor | ktailor | 10 | - | Bring Tough Hide (5); Bring Sharp Bone (4) | 180 XP, 40g, Traveller's Robe x1, Lucky Charm x1 |
| A Song for Aurelion | k_bard | kbard | 10 | - | Use an emote near another player (1) | 120 XP, 20g, Dark Iron Chest x1 |
| Winged Terrors | k_priest | kpriest | 12 | - | Defeat Gale Harpies (4) | 450 XP, 90g, Greater Healing Potion x2 |

Hidden quests have no giver: they are discovered by doing the thing (emote near another player, harvest many herbs, visit the ruins...). The player sees a toast when one is found.

## 4. Milestone / first-time rewards
| Milestone | Id | Condition | Chest | Gold | World-first bonus |
|---|---|---|---|---|---|
| First Blood | first_kill | kills >= 1 | Wood | 10 | no |
| Monster Hunter | kills_100 | kills >= 100 | Bronze | 150 | no |
| Apprentice Crafter | first_craft | crafted >= 1 | Wood | 10 | no |
| Herbalist | herbalist | herbs >= 20 | Dark Iron | 40 | no |
| Treasure Seeker | chests_10 | chests >= 10 | Bronze | 100 | no |
| Rising Adventurer | lv5 | level >= 5 | Dark Iron | 100 | no |
| Breakthrough | lv10 | level >= 10 | Silver | 300 | yes (+2 chest grades for the first player in the world) |
| Ascended | promoted | promoted >= 1 | Gold | 500 | yes (+2 chest grades for the first player in the world) |
| Citizen of Aurelion | kingdom | kingdom >= 1 | Silver | 400 | yes (+2 chest grades for the first player in the world) |
| Veteran | lv20 | level >= 20 | Diamond | 600 | no |
| Champion | lv30 | level >= 30 | Emerald | 900 | no |
| Hero | lv40 | level >= 40 | Sapphire | 1200 | no |
| Legend | lv50 | level >= 50 | Ruby | 1800 | no |
| Mythic | lv70 | level >= 70 | Mythril | 3000 | no |
| Soulbound Legend | lv100 | level >= 100 | Celestial | 8000 | yes (+2 chest grades for the first player in the world) |

## 5. Monsters and bosses
| Monster | Lv | HP | Damage | Speed | Weak to | Resists | Ability notes | XP | Gold | Drops (chance when it drops) |
|---|---|---|---|---|---|---|---|---|---|---|
| Mossy Gloop | 1 | 40 | 5 | 2.6 | Fire | Water | Hops and bounces into its target. Slow, harmless alone but they come in groups. Weak to Fire. | 8 | 1-3 | Redleaf 45% (x1-2), Greenmoss 20% (x1-1), Timber 15% (x1-1) |
| Fangrunner | 3 | 90 | 10 | 5.5 | Thunder | Air | Fast pack hunter that circles and bites. Dodge when it lunges. Weak to Thunder. | 18 | 2-6 | Tough Hide 55% (x1-2), Sharp Bone 30% (x1-2), Redleaf 12% (x1-1) |
| Venom Wisp | 4 | 70 | 12 | 3.0 | Earth | Nature | Floats out of melee range and spits venom bolts from afar. Close in quickly. Weak to Earth. | 22 | 3-8 | Bluebell 45% (x1-2), Crystal Shard 15% (x1-1) |
| Stone Golem | 6 | 260 | 22 | 2.4 | Water | Earth | Slow and heavy. Slams with huge arms; very high health. Weak to Water, shrugs off Earth. | 55 | 6-14 | Iron Ore 65% (x1-3), Stone Heart 15% (x1-1) |
| Grave Wraith | 10 | 220 | 28 | 4.5 | Light | Void | Fast ghost that drifts through the dark and slashes. Weak to Light, immune-like to Void. | 90 | 10-22 | Wraith Essence 45% (x1-2), Spirit Dust 12% (x1-1) |
| Ashen Brute | 12 | 600 | 45 | 3.2 | Ice | Fire | Ashen ogre with crushing blows and heavy armour. Weak to Ice, resists Fire. | 140 | 14-30 | Tough Hide 70% (x2-4), Iron Ore 50% (x2-4), Stone Heart 20% (x1-1) |
| Gale Harpy | 14 | 420 | 38 | 5.0 | Earth | Air | Dives and rakes from the air; hard to hit while flying. Weak to Earth, resists Air. | 170 | 16-34 | Sharp Bone 60% (x1-3), Spirit Dust 15% (x1-1) |
| Ember Drakeling | 18 | 900 | 60 | 4.5 | Water | Fire | Young dragon with claws and a breath-like rush. Weak to Water, resists Fire. | 300 | 30-70 | Ember Core 35% (x1-1), Crystal Shard 30% (x1-2), Tough Hide 50% (x2-4) |
| Ancient Warden **BOSS** | 10 | 2400 | 55 | 3.0 | Thunder | Earth | BOSS. Heavy melee combos plus a ground slam every few seconds (watch the red circle). Enrages below 40% health. Weak to Thunder. | 600 | 100-160 | Kingdom Token 100% (x1-1), Silver Chest 100% (x1-1), Stone Heart 80% (x1-2) |

Drops: a monster only drops loot some of the time (see section 6); each entry is then rolled separately. Higher-level monsters drop more often and better.

### Where each monster lives
**Dawnrest Village**
- Mossy Gloop: 10 per instance, e.g. inner fields, south-west
- Fangrunner: 6 per instance, e.g. inner fields, north-east
- Venom Wisp: 4 per instance, e.g. outskirts, east
- Stone Golem: 2 per instance, e.g. outskirts, north, across the river
- Ancient Warden: 1 per instance, e.g. Trial Grounds ruins at (-62, -48), outskirts, south-west

**Aurelion Kingdom**
- Fangrunner: 8 per instance, e.g. far borderlands, south-east
- Stone Golem: 6 per instance, e.g. outskirts, south
- Grave Wraith: 8 per instance, e.g. outskirts, north
- Ashen Brute: 6 per instance, e.g. inner fields, north
- Gale Harpy: 6 per instance, e.g. far borderlands, south-east
- Ember Drakeling: 3 per instance, e.g. outskirts, east
- Venom Wisp: 4 per instance, e.g. outskirts, east

**Area bosses:** Dawnrest Village - Ancient Warden (promotion boss, respawns after 4 minutes). Aurelion Kingdom - Ember Drakelings (elite) roam the far borderlands; a full kingdom boss is reserved for a later version.

## 6. Loot: how drops work
Chance that ANY loot drops when a monster dies (simulated 4000 kills each):
- Mossy Gloop (Lv1): 42%
- Fangrunner (Lv3): 43%
- Venom Wisp (Lv4): 34%
- Stone Golem (Lv6): 50%
- Grave Wraith (Lv10): 38%
- Ashen Brute (Lv12): 60%
- Gale Harpy (Lv14): 43%
- Ember Drakeling (Lv18): 54%
- Ancient Warden (Lv10): 100%

## 7. Chests
Chest grades from lowest to highest: Wood > Dark Iron > Bronze > Silver > Gold > Diamond > Emerald > Sapphire > Ruby > Mythril > Celestial. A chest is locked if the player is too low level (level + 6 must be at least grade x 4). Opened chests refill after 6 minutes in that instance.

### Chest contents by grade (simulated 3000 openings each)
| Grade | Average gold | Items per chest | Items that can appear (% of chests) |
|---|---|---|---|
| Wood | 4 | 0.9 | Timber 19%, Minor Mana Potion 18%, Minor Healing Potion 18%, Redleaf 17%, Bluebell 17% |
| Dark Iron | 20 | 1.0 | Sharp Bone 17%, Iron Ore 16%, Minor Healing Potion 15%, Tough Hide 15%, Greenmoss 15%, Minor Mana Potion 15% |
| Bronze | 36 | 1.0 | Iron Ore 15%, Healing Potion 15%, Lucky Charm 14%, Tough Hide 14%, Timber 14%, Crystal Shard 14%, Mana Potion 14% |
| Silver | 55 | 1.9 | Iron Sword 24%, Iron Armor 23%, Crystal Shard 23%, Frost Pendant 22%, Mana Potion 22%, Ember Ring 21%, Healing Potion 21%, Iron Spear 21% |
| Gold | 74 | 2.0 | Greater Healing Potion 24%, Iron Armor 24%, Soul Elixir 24%, Arcane Staff 24%, Longbow 23%, Sun Crest 23%, Spirit Dust 23%, Steel Sword 22% |
| Diamond | 93 | 2.1 | Sun Crest 26%, Crystal Blade 25%, Ember Core 25%, Spirit Dust 25%, Greater Healing Potion 24%, Moonlit Spear 24%, Soul Elixir 23%, Knight Plate 22% |
| Emerald | 114 | 3.1 | Ember Core 38%, Spirit Dust 38%, Knight Plate 38%, Greater Healing Potion 38%, Crystal Blade 37%, Moonlit Spear 37%, Sun Crest 37% |
| Sapphire | 133 | 3.2 | Spirit Dust 46%, Sun Crest 44%, Knight Plate 44%, Ember Core 43%, Soul Elixir 42%, Moonlit Spear 42% |
| Ruby | 153 | 3.2 | Mythril Mail 51%, Sun Crest 51%, Moonlit Spear 51%, Ember Core 50%, Soul Elixir 49% |
| Mythril | 170 | 4.2 | Soul Elixir 60%, Sun Crest 59%, Crystal Blade 59%, Ember Core 58%, Mythril Mail 57% |
| Celestial | 190 | 4.2 | Soul Elixir 60%, Ember Core 60%, Sun Crest 59%, Spirit Dust 59%, Mythril Mail 58% |

### Chest locations - Dawnrest Village (16 chests)
| Id | Grade | Position | Area | Min level to open |
|---|---|---|---|---|
| 0 | Wood | (-36, -24) | inner fields, south-west | 0 |
| 1 | Wood | (25, -20) | inner fields, south-east | 0 |
| 2 | Wood | (25, -34) | inner fields, south-east | 0 |
| 3 | Wood | (19, -33) | inner fields, south-east | 0 |
| 4 | Wood | (17, 25) | inner fields, north-east | 0 |
| 5 | Wood | (42, 8) | inner fields, east | 0 |
| 6 | Dark Iron | (-34, 9) | inner fields, west | 0 |
| 7 | Dark Iron | (47, 8) | inner fields, east | 0 |
| 8 | Dark Iron | (-26, -25) | inner fields, south-west | 0 |
| 9 | Dark Iron | (17, -22) | inner fields, south-east | 0 |
| 10 | Bronze | (20, 39) | inner fields, north-east | 2 |
| 11 | Bronze | (-11, -31) | inner fields, south | 2 |
| 12 | Bronze | (4, -62) | outskirts, south | 2 |
| 13 | Silver | (-63, 13) | outskirts, west | 6 |
| 14 | Silver | (-25, 54) | inner fields, north-west | 6 |
| 15 | Gold | (-54, -39) | outskirts, south-west | 10 |

### Chest locations - Aurelion Kingdom (26 chests)
| Id | Grade | Position | Area | Min level to open |
|---|---|---|---|---|
| 0 | Bronze | (-12, -82) | outskirts, south | 2 |
| 1 | Bronze | (68, 0) | outskirts, east | 2 |
| 2 | Bronze | (36, -69) | outskirts, south-east | 2 |
| 3 | Bronze | (-36, -43) | inner fields, south-west | 2 |
| 4 | Bronze | (-41, 21) | inner fields, north-west | 2 |
| 5 | Bronze | (48, 40) | outskirts, north-east | 2 |
| 6 | Silver | (6, 51) | inner fields, north | 6 |
| 7 | Silver | (-55, 26) | outskirts, north-west | 6 |
| 8 | Silver | (-25, 44) | inner fields, north-west | 6 |
| 9 | Silver | (33, 61) | outskirts, north-east | 6 |
| 10 | Silver | (8, 83) | outskirts, north | 6 |
| 11 | Silver | (-10, -51) | inner fields, south | 6 |
| 12 | Silver | (16, -70) | outskirts, south | 6 |
| 13 | Gold | (-85, -26) | outskirts, west | 10 |
| 14 | Gold | (79, -35) | outskirts, south-east | 10 |
| 15 | Gold | (42, 79) | outskirts, north-east | 10 |
| 16 | Gold | (-56, 38) | outskirts, north-west | 10 |
| 17 | Gold | (33, -59) | outskirts, south-east | 10 |
| 18 | Diamond | (42, -64) | outskirts, south-east | 14 |
| 19 | Diamond | (63, 1) | outskirts, east | 14 |
| 20 | Diamond | (69, -65) | far borderlands, south-east | 14 |
| 21 | Emerald | (-86, 42) | far borderlands, north-west | 18 |
| 22 | Emerald | (-81, -25) | outskirts, west | 18 |
| 23 | Sapphire | (29, 104) | far borderlands, north, across the river | 22 |
| 24 | Ruby | (80, 69) | far borderlands, north-east | 26 |
| 25 | Mythril | (-16, 84) | outskirts, north | 30 |

## 8. Gathering nodes (herbs, ore, crystal, wood)
### Dawnrest Village
- Herb nodes: 28 (19 within 48 m of the plaza; found between 20 m and 78 m). Yields red/blue/green herbs (potions). Respawn 45-75 s.
- Tree nodes: 26 (6 within 48 m of the plaza; found between 31 m and 89 m). Yields wood. Respawn 45-75 s.
- Ore nodes: 14 (0 within 48 m of the plaza; found between 55 m and 94 m). Yields iron ore (12% crystal shard). Respawn 45-75 s.
- Crystal nodes: 7 (0 within 48 m of the plaza; found between 71 m and 94 m). Yields crystal shards. Respawn 45-75 s.

### Aurelion Kingdom
- Herb nodes: 30 (25 within 48 m of the plaza; found between 14 m and 73 m). Yields red/blue/green herbs (potions). Respawn 45-75 s.
- Tree nodes: 20 (4 within 48 m of the plaza; found between 31 m and 88 m). Yields wood. Respawn 45-75 s.
- Ore nodes: 22 (0 within 48 m of the plaza; found between 58 m and 97 m). Yields iron ore (12% crystal shard). Respawn 45-75 s.
- Crystal nodes: 14 (0 within 48 m of the plaza; found between 72 m and 98 m). Yields crystal shards. Respawn 45-75 s.

## 9. Equipment and where to get it
| Item | Type | Rarity | Req Lv | Stats | Where to get it |
|---|---|---|---|---|---|
| Training Sword | Weapon (Sword) | Common | 0 | STR 2 | - |
| Iron Sword | Weapon (Sword) | Uncommon | 3 | STR 6 | Craft at smith (3 Iron Ore + 2 Timber, 10g); Chest: Silver |
| Steel Sword | Weapon (Sword) | Rare | 8 | STR 11  TOU 1 | Craft at smith (5 Iron Ore + 2 Tough Hide + 2 Sharp Bone, 80g); Chest: Gold |
| Crystal Blade | Weapon (Sword) | Epic | 12 | STR 15  MAG 6 | Craft at smith (3 Crystal Shard + 4 Iron Ore, 200g); Chest: Diamond; Chest: Emerald; Chest: Mythril |
| Iron Greatsword | Weapon (Greatsword) | Uncommon | 6 | STR 12 | Craft at smith (8 Iron Ore + 3 Timber, 40g) |
| Oak Staff | Weapon (Staff) | Common | 0 | MAG 5 | - |
| Arcane Staff | Weapon (Staff) | Rare | 8 | STR 2  MAG 12 | Craft at smith (4 Timber + 3 Crystal Shard, 80g); Chest: Gold |
| Hunter Bow | Weapon (Bow) | Common | 0 | STR 4 | Craft at smith (4 Timber + 2 Tough Hide) |
| Longbow | Weapon (Bow) | Rare | 6 | STR 9 | Chest: Gold |
| Iron Spear | Weapon (Spear) | Uncommon | 3 | STR 7 | Craft at smith (3 Iron Ore + 3 Timber, 10g); Chest: Silver |
| Moonlit Spear | Weapon (Spear) | Epic | 10 | STR 13  MAG 3 | Chest: Diamond; Chest: Emerald; Chest: Sapphire; Chest: Ruby |
| Traveller's Robe | Armor | Common | 0 | TOU 2  MAG 2 | Quest reward: Winter Coats |
| Leather Armor | Armor | Uncommon | 0 | TOU 5  HP 20 | Craft at smith (4 Tough Hide + 2 Timber) |
| Iron Armor | Armor | Rare | 5 | TOU 9  HP 50 | Craft at smith (6 Iron Ore + 4 Tough Hide, 30g); Quest reward: Stone Guardians; Chest: Silver; Chest: Gold |
| Knight Plate | Armor | Epic | 12 | TOU 15  HP 110 | Craft at smith (10 Iron Ore + 6 Tough Hide + 2 Stone Heart, 250g); Quest reward: Ashen Menace; Chest: Diamond; Chest: Emerald; Chest: Sapphire |
| Mythril Mail | Armor | Legendary | 20 | TOU 22  HP 200  MAG 4 | Chest: Ruby; Chest: Mythril; Chest: Celestial |
| Lucky Charm | Accessory | Uncommon | 0 | MAG 2  BEA 3 | Quest reward: Hidden Caches; Quest reward: A Friendly Face; Quest reward: Census; Quest reward: Winter Coats; Chest: Bronze |
| Ember Ring | Accessory | Rare | 5 | STR 4 | Quest reward: A Proper Blade; Chest: Silver |
| Frost Pendant | Accessory | Rare | 5 | TOU 4 | Chest: Silver |
| Sun Crest | Accessory | Epic | 12 | STR 6  TOU 5  MAG 6  BEA 2 | Quest reward: A Stone for the Queen; Chest: Gold; Chest: Diamond; Chest: Emerald; Chest: Sapphire; Chest: Ruby; Chest: Mythril; Chest: Celestial |

## 10. Rare and crafting materials
| Material | Rarity | Where to get it |
|---|---|---|
| Timber | Common | Drop: Mossy Gloop 15%; Chest: Wood; Chest: Bronze; Gather: trees |
| Iron Ore | Common | Drop: Stone Golem 65%; Drop: Ashen Brute 50%; Chest: Dark Iron; Chest: Bronze; Gather: ore nodes |
| Crystal Shard | Rare | Drop: Venom Wisp 15%; Drop: Ember Drakeling 30%; Chest: Bronze; Chest: Silver; Gather: crystal nodes / ore nodes (12%) |
| Redleaf | Common | Drop: Mossy Gloop 45%; Drop: Fangrunner 12%; Quest reward: First Brew; Chest: Wood; Gather: herb nodes |
| Bluebell | Common | Drop: Venom Wisp 45%; Chest: Wood; Gather: herb nodes |
| Greenmoss | Common | Drop: Mossy Gloop 20%; Chest: Dark Iron; Gather: herb nodes |
| Tough Hide | Common | Drop: Fangrunner 55%; Drop: Ashen Brute 70%; Drop: Ember Drakeling 50%; Quest reward: Metal for Borin; Quest reward: Beast Season; Chest: Dark Iron; Chest: Bronze |
| Sharp Bone | Common | Drop: Fangrunner 30%; Drop: Gale Harpy 60%; Chest: Dark Iron |
| Stone Heart | Rare | Drop: Stone Golem 15%; Drop: Ashen Brute 20%; Drop: Ancient Warden 80% |
| Wraith Essence | Rare | Drop: Grave Wraith 45% |
| Spirit Dust | Epic | Drop: Grave Wraith 12%; Drop: Gale Harpy 15%; Chest: Gold; Chest: Diamond; Chest: Emerald; Chest: Sapphire; Chest: Celestial |
| Ember Core | Epic | Drop: Ember Drakeling 35%; Chest: Diamond; Chest: Emerald; Chest: Sapphire; Chest: Ruby; Chest: Mythril; Chest: Celestial |
| Kingdom Token | Epic | Drop: Ancient Warden 100%; Quest reward: Trial of Ascension |

## 11. Consumables
- Minor Healing Potion: Restores 60 HP.  Source: Craft at alchemist (2 Redleaf); Quest reward: Welcome to Dawnrest; Quest reward: Gloop Trouble; Quest reward: The Wolf Pack; Quest reward: Tavern Gossip; Chest: Wood; Chest: Dark Iron
- Healing Potion: Restores 150 HP.  Source: Craft at alchemist (3 Redleaf + 1 Greenmoss, 5g); Quest reward: Venom in the Air; Quest reward: Brewer's Order; Quest reward: Hearty Stew; Quest reward: The Herb Harvest; Chest: Bronze; Chest: Silver
- Greater Healing Potion: Restores 400 HP.  Source: Craft at alchemist (5 Redleaf + 2 Greenmoss + 1 Spirit Dust, 60g); Quest reward: Winged Terrors; Chest: Gold; Chest: Diamond; Chest: Emerald
- Minor Mana Potion: Restores 40 mana.  Source: Craft at alchemist (2 Bluebell); Quest reward: Herbs for Sylva; Quest reward: Across the Bridge; Chest: Wood; Chest: Dark Iron
- Mana Potion: Restores 100 mana.  Source: Craft at alchemist (3 Bluebell + 1 Greenmoss, 5g); Chest: Bronze; Chest: Silver
- Soul Elixir: Restores 300 HP and 150 mana.  Source: Craft at alchemist (4 Redleaf + 4 Bluebell + 1 Crystal Shard, 40g); Chest: Gold; Chest: Diamond; Chest: Sapphire; Chest: Ruby; Chest: Mythril; Chest: Celestial
- Level-Up Pill: Raises your level by 1. Works from Level 1 to 10 only.  Source: -

## 12. Skills
| Skill | Element | Type | Unlock Lv | Mana | Cooldown | Notes |
|---|---|---|---|---|---|---|
| Power Strike | none | Melee | 1 | 8 | 4s | A heavy strike in front of you. |
| Whirlwind | none | Nova | 2 | 14 | 7s | Spin and hit everything around you. |
| Guardian Stance | none | Shield | 3 | 12 | 14s | Absorb damage for a few seconds. |
| Leap Slam | none | Dash | 4 | 18 | 9s | Leap forward and slam the ground. (leaves a crater) |
| Second Wind | none | Heal | 5 | 20 | 18s | Restore 30% HP to you and nearby allies. |
| Focus Burst | none | Projectile | 6 | 16 | 6s | A burst of focused energy. |
| Tidal Lance | Water | Projectile | 3 | 12 | 3s | A bolt of Water energy. |
| Wave Crash | Water | Nova | 5 | 24 | 9s | A burst of Water around the target area. |
| Riptide Step | Water | Dash | 7 | 18 | 8s | Dash forward in a flash of Water. |
| Mending Spring | Water | Heal | 9 | 28 | 16s | Protect yourself and allies with Water. |
| Fire Bolt | Fire | Projectile | 3 | 12 | 3s | A bolt of Fire energy. |
| Inferno Burst | Fire | Nova | 5 | 24 | 9s | A burst of Fire around the target area. (leaves a crater) |
| Blaze Rush | Fire | Dash | 7 | 18 | 8s | Dash forward in a flash of Fire. |
| Phoenix Ward | Fire | Heal | 9 | 28 | 16s | Protect yourself and allies with Fire. |
| Stone Shard | Earth | Projectile | 3 | 12 | 3s | A bolt of Earth energy. |
| Quake | Earth | Nova | 5 | 24 | 9s | A burst of Earth around the target area. (leaves a crater) |
| Boulder Charge | Earth | Dash | 7 | 18 | 8s | Dash forward in a flash of Earth. |
| Earthen Hide | Earth | Shield | 9 | 28 | 16s | Protect yourself and allies with Earth. |
| Void Bolt | Void | Projectile | 3 | 12 | 3s | A bolt of Void energy. |
| Singularity | Void | Nova | 5 | 24 | 9s | A burst of Void around the target area. |
| Rift Step | Void | Dash | 7 | 18 | 8s | Dash forward in a flash of Void. |
| Dark Barrier | Void | Shield | 9 | 28 | 16s | Protect yourself and allies with Void. |
| Thunder Spear | Thunder | Projectile | 3 | 12 | 3s | A bolt of Thunder energy. |
| Storm Nova | Thunder | Nova | 5 | 24 | 9s | A burst of Thunder around the target area. (leaves a crater) |
| Lightning Dash | Thunder | Dash | 7 | 18 | 8s | Dash forward in a flash of Thunder. |
| Static Shield | Thunder | Shield | 9 | 28 | 16s | Protect yourself and allies with Thunder. |
| Thorn Shot | Nature | Projectile | 3 | 12 | 3s | A bolt of Nature energy. |
| Bloom Burst | Nature | Nova | 5 | 24 | 9s | A burst of Nature around the target area. |
| Vine Leap | Nature | Dash | 7 | 18 | 8s | Dash forward in a flash of Nature. |
| Verdant Heal | Nature | Heal | 9 | 28 | 16s | Protect yourself and allies with Nature. |
| Light Ray | Light | Projectile | 3 | 12 | 3s | A bolt of Light energy. |
| Radiant Nova | Light | Nova | 5 | 24 | 9s | A burst of Light around the target area. |
| Dawn Step | Light | Dash | 7 | 18 | 8s | Dash forward in a flash of Light. |
| Holy Mend | Light | Heal | 9 | 28 | 16s | Protect yourself and allies with Light. |
| Gale Cutter | Air | Projectile | 3 | 12 | 3s | A bolt of Air energy. |
| Cyclone | Air | Nova | 5 | 24 | 9s | A burst of Air around the target area. |
| Wind Rush | Air | Dash | 7 | 18 | 8s | Dash forward in a flash of Air. |
| Zephyr Guard | Air | Shield | 9 | 28 | 16s | Protect yourself and allies with Air. |
| Ice Shard | Ice | Projectile | 3 | 12 | 3s | A bolt of Ice energy. |
| Frost Nova | Ice | Nova | 5 | 24 | 9s | A burst of Ice around the target area. |
| Glacier Slide | Ice | Dash | 7 | 18 | 8s | Dash forward in a flash of Ice. |
| Frozen Armor | Ice | Shield | 9 | 28 | 16s | Protect yourself and allies with Ice. |
| Prism Bolt | Omnigod | Projectile | 3 | 12 | 3s | A bolt of Omnigod energy. |
| Harmony Nova | Omnigod | Nova | 5 | 24 | 9s | A burst of Omnigod around the target area. |
| Astral Step | Omnigod | Dash | 7 | 18 | 8s | Dash forward in a flash of Omnigod. |
| Divine Grace | Omnigod | Heal | 9 | 28 | 16s | Protect yourself and allies with Omnigod. |

## 13. Element weaknesses
A skill of an element a monster is weak to deals x1.4 damage; a resisted element deals x0.7; Omnigod skills deal x1.15 to everything. Basic weapon attacks are neutral.
