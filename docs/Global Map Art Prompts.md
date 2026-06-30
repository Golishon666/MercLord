# Prompts: Global Map Texture And Icon Pack

Ниже промты для генерации светлого, приятного глазу atlas/sprite-sheet пака глобальной карты. Лучше генерировать не один огромный лист сразу, а 3 согласованных atlas-листа: `Biome Tiles`, `Map Overlays`, `Settlement And Activity Icons`. Так генератор лучше держит детализацию и меньше путает сетку.

## Общий Стиль

Используй этот блок в начале каждого промта:

```text
Bright stylized fantasy strategy map asset pack, clean readable top-down orthographic game art, soft painterly texture, warm daylight palette, pleasant low-contrast colors, subtle hand-painted detail, no harsh shadows, no photorealism, no text, no labels, no UI, no watermark, transparent background where applicable, consistent scale, consistent lighting, crisp silhouettes, game-ready sprites.
```

Негативный блок:

```text
dark gloomy palette, muddy colors, noisy high contrast, realistic satellite photo, 3D render, perspective camera, blurry edges, random text, letters, numbers, watermark, icons touching each other, cropped sprites, inconsistent style, heavy black shadows, neon colors.
```

## Biome Tiles Sprite Sheet

Цель: тайловые текстуры для гексов. Лучше делать `4096x4096`, сетка `4 columns x 4 rows`, каждый тайл `1024x1024`, без текста, с небольшим внутренним padding. После генерации нарезать вручную или через Sprite Editor.

```text
Bright stylized fantasy strategy map biome texture atlas, 4 columns x 4 rows, 16 square seamless top-down terrain tiles, each tile is a clean tileable texture for a hex world map, soft painterly details, pleasant bright palette, low contrast, no text, no labels, no icons, no roads, no rivers, no grid lines.

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
12 industrial ruins broken concrete and overgrown debris, top-down texture,
13 demon scar muted red cracked corrupted ground, readable but not dark,
14 toxic swamp green blue marsh with subtle glowing pools, not neon,
15 highland tan grass and rocky patches,
16 fertile river valley rich green grass with damp soil.

Consistent lighting, seamless edges, game-ready terrain texture atlas, no perspective, no objects, no buildings.
```

Дополнительный вариант, если генератор плохо делает 16 тайлов сразу:

```text
Single seamless top-down terrain texture for a stylized fantasy strategy hex map: [BIOME NAME]. Bright pleasant daylight palette, soft painterly game texture, subtle natural detail, low contrast, tileable on all edges, no roads, no rivers, no buildings, no icons, no text, no grid, no shadows, no perspective. 1024x1024.
```

## Map Overlays Sprite Sheet

Цель: дороги, реки, берега, горные пятна, лесные пятна, декоративные детали поверх биомов. Лучше `2048x2048`, сетка `4 x 4`, прозрачный фон.

```text
Bright stylized strategy map overlay sprite sheet, 4 columns x 4 rows, transparent background, top-down orthographic, consistent scale, soft painterly game art, no text, no labels, no terrain background.

Sprites in order:
1 small dirt road straight segment, warm beige, transparent background,
2 medium road straight segment, wider compacted dirt, transparent background,
3 large road straight segment, pale stone road with soft edge, transparent background,
4 road junction crossroad, readable from top-down,
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
15 ruins debris overlay, tiny broken stone and metal fragments,
16 corrupted crack overlay, muted red fissures, not dark, not neon.

All sprites centered in their cells with padding, clean alpha, no cropping, no shadows outside sprite.
```

## Settlement And Activity Icons Sprite Sheet

Цель: маленькие маркеры на карте. Лучше `2048x2048`, сетка `8 columns x 4 rows`, каждый icon `256x256`, прозрачный фон. Стиль должен читаться на маленьком размере.

```text
Bright stylized fantasy strategy map icon sprite sheet, 8 columns x 4 rows, 32 centered icons, transparent background, top-down/front-readable hybrid map markers, clean silhouette, soft colored outline, pleasant pastel colors, game-ready UI/map sprites, consistent lighting and scale, no text, no labels, no numbers, no terrain background.

Icons in order, left to right, top to bottom:
1 capital city icon, elegant castle tower with small flag, gold and blue,
2 large city icon, clustered stone houses with walls,
3 town icon, three cozy houses,
4 village icon, one simple house and small fence,
5 harbor town icon, house with small dock anchor shape,
6 river town icon, house with blue water stripe,
7 mountain settlement icon, house with small gray peak,
8 forest settlement icon, house with green tree canopy,
9 camp icon, green tent,
10 bandit camp icon, red tent with small warning mark shape but no text,
11 caravan stop icon, small wagon and tent,
12 mine icon, mine entrance with pickaxe silhouette,
13 ruins icon, broken pale stone arch,
14 industrial ruins icon, small broken factory silhouette, light gray,
15 shrine icon, small white obelisk,
16 watchtower icon, wooden tower,
17 monster lair icon, dark cave mouth but still bright readable,
18 demon scar activity icon, muted red crystal crack marker,
19 toxic swamp activity icon, green flask-like swamp marker, not neon,
20 forest resource icon, log pile,
21 mineral resource icon, ore stones,
22 food resource icon, wheat bundle,
23 water resource icon, blue droplet spring,
24 relic resource icon, small ancient chest,
25 quest marker icon, parchment scroll, no symbol text,
26 battle marker icon, crossed swords,
27 danger marker icon, red triangular warning shape, no exclamation text,
28 trade marker icon, coin stack and small crate,
29 faction outpost blue icon, small banner post,
30 faction outpost purple icon, small banner post,
31 faction outpost red icon, small banner post,
32 neutral point of interest icon, pale diamond marker.

Keep all icons readable at 32x32 and 64x64, centered with generous padding, transparent alpha, no cropping.
```

## Faction Marker Variants

Если нужны отдельные цвета фракций без перегенерации формы:

```text
Create a sprite sheet of 16 color variants of the same stylized fantasy map settlement marker, transparent background, centered icon, consistent silhouette, soft outline, bright pleasant palette. Colors: sky blue, royal blue, violet, lavender, red, coral, green, mint, yellow, gold, orange, teal, white, gray, black slate, neutral beige. No text, no labels, no numbers.
```

## One Master Atlas Prompt

Если хочется попробовать одним паком:

```text
Complete bright stylized fantasy global strategy map asset pack sprite sheet, 4096x4096, organized clean atlas grid, transparent background for icons and overlays, no text, no labels, no watermark. Include top-down seamless biome tiles, road and river overlay segments, and readable settlement/activity icons. Pleasant daylight palette, soft painterly game art, consistent scale, low contrast, crisp silhouettes.

Top half: 16 seamless terrain tiles: ocean, coast, plains, forest, desert, snow, swamp, mountains, ash wastes, rust desert, dead forest, industrial ruins, demon scar, toxic swamp, highlands, fertile river valley.
Bottom left: road and river overlays: small road, medium road, large road, junction, thin river, medium river, wide river, river mouth, coastline foam, mountain patch, forest patch, swamp puddle, snow drift, desert dune, ruins debris, corrupted crack.
Bottom right: map icons: capital, city, town, village, harbor, river town, mountain settlement, forest settlement, camp, bandit camp, caravan stop, mine, ruins, industrial ruins, shrine, watchtower, monster lair, demon scar, toxic swamp, resources, quest, battle, danger, trade, faction outposts.

All elements separated with padding, no cropping, game-ready sprite atlas.
```

## Рекомендации По Импорту В Unity

- Для тайлов биомов: `Texture Type = Sprite (2D and UI)` или `Default`, `Wrap Mode = Repeat`, `Filter Mode = Bilinear`, `Compression = High Quality`.
- Для иконок/оверлеев: `Texture Type = Sprite (2D and UI)`, `Sprite Mode = Multiple`, `Alpha Is Transparency = true`, `Filter Mode = Bilinear`.
- Для карты с гексами лучше держать биомы как tileable diffuse textures, а дороги/реки/иконки отдельными overlay sprites.
- Если генератор делает грязные края, проси: `transparent background, generous padding, no icon touching grid borders`.
