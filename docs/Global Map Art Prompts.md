# Prompts: Global Map Texture And Feature Pack

Этот документ является основным источником art prompts для глобальной карты. Он покрывает три режима ассетов:

- biome tiles - текстуры самих сот;
- overlays - дороги, реки, береговые линии и декоративные пятна;
- map points - дальние иконки и ближние объемные текстуры городов, районов и активностей.

Markdown-документы `Game Design Document.md` и `Architecture Document.md` описывают правила геймплея и реализации, а этот файл хранит полные prompts для генерации ассетов.

## Common Style Block

Используй этот блок в начале каждого prompt, если не указано другое:

```text
Bright stylized high-tech modern military strategy map asset pack, clean readable top-down orthographic game art, tactical near-future design, modular armored architecture, concrete, steel, composite panels, radar dishes, antennas, helipads, floodlights, fortified logistics details, cool daylight palette, desaturated military greens, grays, whites, black graphite, hazard yellow and red accents, low visual noise, crisp silhouettes, game-ready sprites, no text, no labels, no UI, no watermark, transparent background where applicable, consistent scale, consistent lighting.
```

Для zoom-in feature textures используй более объемную формулировку:

```text
Stylized 2.5D top-down high-tech modern military strategy map feature texture, tactical near-future structures, readable armored silhouettes, roof equipment, antennas, vents, floodlights, helipads, perimeter walls, soft contact shadow painted into the sprite, clear alpha edge, orthographic camera, no real 3D render, no text, no labels, no UI, no watermark, transparent background, consistent scale and lighting, game-ready sprite.
```

Negative prompt:

```text
dark gloomy palette, muddy colors, noisy high contrast, realistic satellite photo, photorealistic 3D render, perspective camera, blurry edges, random text, letters, numbers, watermark, icons touching each other, cropped sprites, inconsistent style, heavy black shadows, excessive neon, medieval fantasy, castles, cottages, swords, scrolls, magic crystals, parchment, UI frame, label plate, grid lines.
```

## Biome Tiles Sprite Sheet

Цель: tileable текстуры для глобальных сот. Рекомендуемый размер: `4096x4096`, сетка `4 columns x 4 rows`, каждый tile `1024x1024`, без текста, без иконок, без дорог и рек.

```text
Bright stylized high-tech modern military strategy map biome texture atlas, 4 columns x 4 rows, 16 square seamless top-down terrain tiles, each tile is a clean tileable texture for a hex world map, tactical near-future game art, controlled detail, desaturated military palette, low visual noise, no text, no labels, no icons, no roads, no rivers, no grid lines.

Tile themes in order, left to right, top to bottom:
1 ocean deep blue water with subtle waves,
2 coast shallow turquoise water with pale sand edge,
3 plains soft yellow-green grassland with tiny natural variation,
4 forest lush green woodland canopy texture,
5 desert warm pale sand with gentle dunes,
6 snow white polar snowfield with slight blue shadows,
7 swamp muted green wet marsh with small pools,
8 mountains light gray rocky alpine terrain with snow speckles,
9 ash wastes pale gray volcanic dust and cracked ground,
10 rust desert orange tan mineral desert, not too saturated,
11 dead forest gray green dry woodland texture,
12 industrial ruins broken reinforced concrete, steel debris, overgrown machinery, top-down texture,
13 anomaly scar muted red cracked contaminated ground, battlefield burn marks, readable but not dark,
14 toxic swamp green blue contaminated marsh with subtle chemical pools, not neon,
15 highland tan grass and rocky patches,
16 fertile river valley rich green grass with damp soil.

Consistent lighting, seamless edges, game-ready terrain texture atlas, no perspective, no objects, no buildings.
```

Если генератор плохо делает 16 тайлов сразу:

```text
Single seamless top-down terrain texture for a stylized high-tech modern military strategy hex map: [BIOME NAME]. Clean tactical daylight palette, controlled game texture detail, subtle natural or industrial variation, low visual noise, tileable on all edges, no roads, no rivers, no buildings, no icons, no text, no grid, no shadows, no perspective. 1024x1024.
```

## Map Overlays Sprite Sheet

Цель: дороги, реки, береговые линии и декоративные детали поверх biome tiles. Рекомендуемый размер: `2048x2048`, сетка `4 x 4`, прозрачный фон.

```text
Bright stylized high-tech modern military strategy map overlay sprite sheet, 4 columns x 4 rows, transparent background, top-down orthographic, consistent scale, tactical near-future game art, no text, no labels, no terrain background.

Sprites in order:
1 small patrol track straight segment, compacted dirt with tire marks, transparent background,
2 medium service road straight segment, reinforced gravel and asphalt patches, transparent background,
3 large military road straight segment, concrete plates with soft edge, transparent background,
4 road junction checkpoint crossroad, readable from top-down,
5 thin river stream segment, clear blue, transparent background,
6 medium river segment, clear blue with soft highlight,
7 wide river segment, deeper blue with gentle edge,
8 river mouth delta into sea, transparent edge,
9 coastline foam edge strip, pale sand and soft water edge,
10 mountain rocky patch overlay, light gray stones,
11 forest cluster overlay, small green tree canopy clumps,
12 swamp puddle overlay, muted green water pools,
13 snow drift overlay, soft white irregular patch,
14 desert dune detail overlay, pale sand brush strokes,
15 ruins debris overlay, tiny broken concrete, rebar, and metal fragments,
16 anomaly crack overlay, muted red contaminated fissures, not dark, not neon.

All sprites centered in their cells with padding, clean alpha, no cropping, no shadows outside sprite.
```

## Far Zoom Settlement And Activity Icons

Цель: маленькие читаемые маркеры для дальнего масштаба карты. Рекомендуемый размер: `2048x2048`, сетка `8 columns x 4 rows`, каждый icon `256x256`, прозрачный фон. Порядок должен совпадать с `GlobalMapIconSpriteId`.

```text
Bright stylized high-tech modern military strategy map icon sprite sheet, 8 columns x 4 rows, 32 centered icons, transparent background, top-down/front-readable hybrid tactical map markers, clean silhouette, subtle colored outline, desaturated military palette with hazard accents, game-ready UI/map sprites, consistent lighting and scale, no text, no labels, no numbers, no terrain background.

Icons in order, left to right, top to bottom:
1 capital city icon, fortified command tower with radar dish and tiny faction light, gold and blue accents,
2 city icon, clustered modular high-rise blocks with perimeter wall,
3 town icon, compact modular buildings and service road,
4 village icon, small prefab settlement block with fence,
5 harbor city icon, modular building with dock and anchor-like silhouette,
6 river city icon, building cluster with blue canal stripe,
7 mountain outpost icon, bunker with gray peak silhouette,
8 forest station icon, camouflaged station with green canopy outline,
9 expedition camp icon, tactical field tents and antenna,
10 hostile camp icon, red tactical tents with barricade shape but no text,
11 logistics stop icon, cargo truck and supply tent,
12 mine icon, armored mine entrance with drill silhouette,
13 pre-war ruins icon, broken concrete gate and rebar,
14 industrial ruins icon, damaged factory silhouette and pipes, light gray,
15 research station icon, sensor mast and lab block with blue accent,
16 watchtower icon, steel observation tower,
17 underground bunker icon, dark bunker entrance but still bright readable,
18 anomaly marker icon, muted red containment crack marker,
19 toxic zone icon, green chemical hazard swamp marker, not neon,
20 biomass resource icon, biomass tanks and leaf cluster,
21 ore resource icon, ore stones and drill bit,
22 food resource icon, hydroponic crop tray,
23 water resource icon, blue water purifier and spring droplet,
24 tech cache icon, sealed military crate,
25 mission icon, tactical dossier tablet, no symbol text,
26 battle icon, crossed rifles,
27 danger icon, red triangular hazard shape, no exclamation text,
28 trade icon, cargo crates and credit chips,
29 blue outpost icon, blue comms mast and small fortified post,
30 purple outpost icon, purple comms mast and small fortified post,
31 red outpost icon, red comms mast and small fortified post,
32 neutral point icon, pale sensor beacon marker.

Keep all icons readable at 32x32 and 64x64, centered with generous padding, transparent alpha, no cropping.
```

## Near Zoom City Level Feature Textures

Цель: после приближения иконки исчезают, а города отображаются как объемные текстуры на сотах. Город всегда имеет центральную соту и компактную область вокруг нее.

Размер: `4096x2048`, сетка `5 columns x 1 row`, каждый sprite имеет прозрачный фон и достаточно padding. Каждый sprite должен читаться как объект, который ложится на несколько hex-like сот:

- level 1 - 1 cell;
- level 2 - 3 cells;
- level 3 - 5 cells;
- level 4 - 7 cells;
- level 5 - 9 cells, capital only.

```text
Stylized 2.5D top-down high-tech modern military city feature texture sheet, 5 centered city sprites, transparent background, tactical near-future design, hand-painted volume, soft contact shadows, readable armored roofs, modular blocks, radar dishes, antennas, vents, perimeter walls, no text, no labels, no UI, no grid, no terrain background, consistent lighting, consistent faction-neutral concrete, steel, composite material palette with small recolorable faction light accents.

Sprites in order, left to right:
1 level 1 settlement, one-cell compact prefab colony core, a few modular shelters around a comms mast, fits one hex cell,
2 level 2 settlement, three-cell small fortified town cluster, command hub plus two attached residential and logistics blocks, compact footprint,
3 level 3 settlement, five-cell military city, central command bunker, service roads, small perimeter walls, residential modules and hydroponic edges,
4 level 4 settlement, seven-cell large fortified city, dense districts around a command core, industrial block, military yard, research tower, logistics market, partial outer wall,
5 level 5 capital city, nine-cell grand faction capital, dominant hardened command fortress, administrative district, military district, research district, logistics quarter, residential quarter, industrial district, hydroponic outskirts, one armored central avenue, larger but still readable from top-down.

Each sprite should visually imply its intended multi-cell footprint without drawing actual hex grid lines. Keep every city centered, with alpha padding, no cropping, no labels, no numbers.
```

## Near Zoom City District Feature Textures

Цель: отдельные районы вокруг центрального города. Эти sprites могут использоваться как overlay на занятых сотах города или как составные части большого города.

Размер: `4096x2048`, сетка `5 columns x 2 rows`, прозрачный фон. Порядок должен совпадать с будущим `SettlementDistrictType`.

```text
Stylized 2.5D top-down high-tech modern military city district texture sheet, 5 columns x 2 rows, 10 centered district sprites, transparent background, tactical near-future structures, hand-painted volume and soft contact shadows, armored roofs, concrete, steel, composite panels, antennas, vents, no text, no labels, no UI, no grid, no terrain background, consistent scale and lighting.

Districts in order:
1 Core, central command core with hardened HQ, comms mast, civic plaza, compact blast walls,
2 Residential, clustered modular habitation blocks, courtyards, narrow service roads, armored roofs,
3 Industrial, fabrication halls, vents, smokeless stacks, foundry roofs, storage yards, not dark or dirty,
4 Farming, hydroponic fields, greenhouse domes, irrigation tanks, automated rural outskirts,
5 Military, barracks, vehicle yard, training ground, watch posts, small defensive wall,
6 Science, research tower, sensor dome, clean lab block, blue glass accent,
7 Trade, logistics market, cargo depots, container stacks, drone pad,
8 Administrative, command office complex, paved plaza, faction light strips,
9 Port-adjacent, concrete docks, warehouses, patrol boats, water-edge structures, no full ocean tile,
10 Mining, armored mine headframe, ore sheds, conveyor belts, drill rigs, rocky service yard.

Each district must fit one map cell visually, but combine naturally with neighboring district sprites into a larger city.
```

## Near Zoom Activity Feature Textures

Цель: все точки активности и POI на ближнем zoom отображаются не как иконки, а как объемные texture features на поверхности карты. Порядок совпадает с `GlobalMapIconSpriteId`, чтобы один enum мог выбирать дальнюю иконку и ближнюю текстуру.

Размер: `4096x4096`, сетка `8 columns x 4 rows`, прозрачный фон, каждый sprite `512x512`.

```text
Stylized 2.5D top-down high-tech modern military strategy map point feature texture sheet, 8 columns x 4 rows, 32 centered feature sprites, transparent background, tactical near-future structures and equipment, hand-painted height and volume, soft contact shadows, crisp silhouettes, no text, no labels, no numbers, no UI, no terrain background, consistent scale and lighting.

Feature textures in order, left to right, top to bottom:
1 capital city core, grand hardened HQ block and command fortress, faction light accents, for level 5 capital center,
2 city core, walled modular urban block with radar towers,
3 town core, compact service plaza with clustered prefab buildings,
4 village core, small prefab colony with fence and utility well,
5 harbor city district, concrete docks, warehouses, patrol boats, coastal military architecture,
6 river city district, armored bridge, pump station, river quay buildings,
7 mountain outpost, hardened bunker and cliff-side structures,
8 forest station, camouflaged lodges, watch platforms, sensor masts among tree canopies,
9 expedition camp, tactical tents, comms antenna, supply crates,
10 hostile camp, rough red tactical tents, barricades, weapon racks, still readable and not overly dark,
11 logistics stop, cargo trucks, tents, supply depot,
12 mine, armored mine entrance, steel supports, ore piles and cart tracks,
13 pre-war ruins, broken concrete gate, rebar, columns, overgrown plaza,
14 industrial ruins, collapsed factory, pipes, concrete slabs, small metal debris,
15 research station, clean sensor monolith, lab containers, blue glowing accent not neon,
16 watchtower, steel observation tower, small fence, lookout platform,
17 underground bunker, rocky bunker mouth, crates and warning lights near entrance, bright readable silhouette,
18 anomaly marker, cracked red containment scar and scorched ground, not dark,
19 toxic zone, green-blue chemical vents and barrels, subtle glow not neon,
20 biomass resource, dense bio tanks, leaf clusters, natural resource containers,
21 ore resource, exposed mineral rocks, drill rig, small mining tools,
22 food resource, hydroponic crop trays, small granary module, farm crates,
23 water resource, spring pool, purifier station, clean water tanks,
24 tech cache, half-buried sealed military crate and small concrete pedestal,
25 mission point, courier tent and tactical terminal shape without text,
26 battle point, broken armor plates, crossed rifles on ground, small smoke plume,
27 danger point, barricade and hazard stones, red cloth marker without symbol text,
28 trade point, cargo stalls, credit crates, small awning,
29 blue outpost, blue faction comms mast with fortified post,
30 purple outpost, purple faction comms mast with fortified post,
31 red outpost, red faction comms mast with fortified post,
32 neutral point, pale sensor beacon and small concrete platform.

Every feature should read as a physical place on the world map, not a flat UI icon. Keep all sprites centered with padding and clean alpha.
```

## Faction Tint Variants

Если нужны варианты без перегенерации формы, проси recolorable accents rather than full recolors. Базовые материалы города должны оставаться нейтральными, а цвет фракции должен жить в флагах, крышах, тканях, огнях или small trim.

```text
Create 16 color variants of the same stylized 2.5D high-tech modern military strategy map settlement feature, transparent background, centered sprite, consistent silhouette, soft contact shadow, tactical near-future palette. Only recolor faction light strips, antenna lights, roof trims, signal panels, hazard markings, and small accent details; keep concrete, steel, composite panels, roads, and ground neutral. Colors: sky blue, royal blue, violet, lavender, red, coral, green, mint, yellow, gold, orange, teal, white, gray, black slate, neutral beige. No text, no labels, no numbers.
```

## One Master Atlas Prompt

Если нужно попробовать одним большим паком:

```text
Complete bright stylized high-tech modern military global strategy map asset pack sprite sheet, 4096x4096, organized clean atlas grid, transparent background for icons, overlays, and feature sprites, no text, no labels, no watermark. Include top-down seamless biome tiles, road and river overlay segments, far zoom readable tactical icons, and near zoom 2.5D physical feature textures for fortified cities, districts, bases, logistics points, bunkers, research stations, mines, resources, and activities. Cool daylight military palette, tactical near-future game art, consistent scale, low visual noise, crisp silhouettes.

Top band: seamless terrain tiles for ocean, coast, plains, forest, desert, snow, swamp, mountains, ash wastes, rust desert, dead forest, industrial ruins, anomaly scar, toxic swamp, highlands, fertile river valley.
Middle left: road and river overlays: patrol track, service road, military concrete road, checkpoint junction, thin river, medium river, wide river, river mouth, coastline foam, mountain patch, forest patch, swamp puddle, snow drift, desert dune, concrete ruins debris, anomaly crack.
Middle right: far zoom tactical map icons: capital command center, city, town, prefab village, harbor city, river city, mountain bunker, forest station, expedition camp, hostile camp, logistics stop, mine, pre-war ruins, industrial ruins, research station, watchtower, underground bunker, anomaly marker, toxic zone, resources, mission, battle, danger, trade, faction outposts, neutral sensor point.
Bottom band: near zoom 2.5D feature textures: fortified city levels 1 to 5, city districts, expedition camp, hostile camp, logistics stop, mine, pre-war ruins, industrial ruins, research station, watchtower, underground bunker, anomaly, toxic zone, resource clusters, mission point, battle point, danger point, trade point, faction outposts.

All elements separated with padding, no cropping, clean alpha, game-ready sprite atlas.
```

## Unity Import Recommendations

- Biome tiles: `Texture Type = Sprite (2D and UI)` или `Default`, `Wrap Mode = Repeat`, `Filter Mode = Bilinear`, `Compression = High Quality`.
- Overlays/icons/features: `Texture Type = Sprite (2D and UI)`, `Sprite Mode = Multiple`, `Alpha Is Transparency = true`, `Filter Mode = Bilinear`, `Compression = High Quality`.
- Far zoom icons должны оставаться читаемыми в `32x32` и `64x64`.
- Near zoom feature textures должны иметь чистый alpha padding и painted contact shadow внутри sprite, чтобы объект выглядел объемно без 3D-модели.
- Для городов level 2-5 не рисовать hex grid в самом sprite. Footprint задается данными игры, а sprite только визуально намекает на размер.
- Если генератор делает грязные края, добавь в prompt: `transparent background, generous padding, no sprite touching grid borders, clean alpha edge`.
