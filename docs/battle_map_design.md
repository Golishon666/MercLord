# Battle Map Design

**Project:** Dieselpunk 2D Warband  
**Жанр:** 2D sandbox / strategy RPG / party-based war game в духе Mount & Blade: Bannerlord  
**Цель документа:** описать архитектуру локальной боевой карты для сражений до **1000 ботов** в Unity 2D.

---

## 1. Основная идея

Локальная битва должна ощущаться как большая мясорубка в духе Mount & Blade, но в 2D top-down / pseudo top-down виде.

Игрок управляет персонажем вручную, участвует в бою лично, может стрелять, драться, использовать предметы, входить в технику и отдавать приказы отрядам.

Главный принцип:

```text
1000 ботов должны быть настоящими участниками боя,
но не должны быть 1000 дорогими Unity-объектами с тяжёлым AI, Physics и Update.
```

Поэтому бой строится через:

```text
Unity Tilemap для визуала
+
BattleTile[] для логики
+
BattleWorld / ECS-like симуляция
+
Squad AI
+
лёгкие UnitView из пула
```

---

## 2. Переход из глобальной карты в бой

Бой начинается не сам по себе, а из глобальной соты.

```text
Global Cell / Encounter
        ↓
BattleRequest
        ↓
Load BattleScene
        ↓
Generate BattleMap
        ↓
Create BattleTile[]
        ↓
Create BattleWorld
        ↓
Convert Squads → Battle Units
        ↓
Spawn Player / Allies / Enemies / Vehicles
        ↓
Run Battle
        ↓
BattleResult
        ↓
Return to GlobalScene
```

Глобальная сота задаёт параметры боя:

```text
- biome
- owner faction
- danger / corruption
- height
- roads
- nearby water
- armies involved
- seed генерации
```

Локальная карта не обязана быть hex-картой. Глобальная карта может быть hex/pentagon, а локальная битва должна быть обычной 2D Tilemap-картой.

---

## 3. Рекомендуемый размер карты

Для MVP:

```text
Small battle: 128 x 128 tiles
Normal battle: 256 x 256 tiles
Large battle: 320 x 320 tiles
```

Для теста 1000 ботов лучше начать с:

```text
256 x 256 tiles
500 союзников vs 500 врагов
1 tile = 1 условный метр / Unity unit
```

Не стоит сразу начинать с 512 x 512, потому что большая карта усложнит тестирование: боты будут долго идти до врага, бой станет размазанным, а pathfinding и AI сложнее отлаживать.

---

## 4. Структура BattleScene

```text
BattleScene
├── Map
│   ├── GroundTilemap
│   ├── RoadTilemap
│   ├── ObstacleTilemap
│   ├── DecorationTilemap
│   ├── OverlayTilemap
│   └── DebugTilemap
│
├── Runtime
│   ├── BattleWorld
│   ├── BattleMapGenerator
│   ├── PathfindingService
│   ├── SpatialGridService
│   ├── ProjectileService
│   └── BattleResultService
│
├── Units
│   ├── UnitViewPool
│   ├── VehicleViewPool
│   ├── ProjectileViewPool
│   └── VFXPool
│
├── Player
│   ├── PlayerController
│   └── PlayerCameraTarget
│
├── Camera
│   ├── BattleCamera
│   └── CameraShake
│
└── UI
    ├── PlayerHUD
    ├── SquadHUD
    ├── BattleStatusHUD
    ├── MinimapHUD
    ├── CommandWheelHUD
    └── TooltipHUD
```

---

## 5. Разделение визуала и логики

Tilemap отвечает только за картинку:

```text
- земля
- дороги
- препятствия
- декор
- следы боя
- руины
- деревья
- окопы
```

Геймплейная логика использует отдельный массив:

```text
BattleTile[]
```

`BattleTile[]` нужен для:

```text
- walkable / not walkable
- move cost
- cover
- height
- line of sight
- projectile blocking
- vehicle access
- region id
```

Пример структуры:

```csharp
public enum MoveLayer : byte
{
    None     = 0,
    Infantry = 1 << 0,
    Vehicle  = 1 << 1,
    Flying   = 1 << 2
}

public enum CoverType : byte
{
    None,
    Light,
    Medium,
    Heavy
}

public struct BattleTile
{
    public bool Walkable;
    public byte MoveCost;
    public CoverType Cover;
    public sbyte Height;

    public MoveLayer AllowedMoveLayers;

    public bool BlocksLineOfSight;
    public bool BlocksProjectiles;

    public ushort RegionId;
}
```

---

## 6. Генерация локальной карты

Карта генерируется из `BattleRequest`.

```csharp
public sealed class BattleRequest
{
    public int GlobalCellId;
    public int Seed;

    public BattleBiome Biome;

    public ArmySnapshot Attacker;
    public ArmySnapshot Defender;

    public Vector2Int MapSize;
}
```

Пайплайн генерации:

```text
1. Взять BattleRequest.
2. Выбрать BattleBiomeTemplate.
3. Создать noise-карты.
4. Сгенерировать ground.
5. Сгенерировать дороги.
6. Сгенерировать препятствия.
7. Сгенерировать cover.
8. Сгенерировать spawn zones.
9. Сгенерировать objectives.
10. Заполнить BattleTile[].
11. Нарисовать Tilemap.
```

Примеры биомов:

```text
Forest:
- grass
- dirt
- trees
- bushes
- narrow paths
- medium cover

AshWastes:
- ash ground
- cracked soil
- wrecks
- smoke
- open spaces

IndustrialRuins:
- concrete
- metal
- pipes
- rubble
- heavy cover

DemonScar:
- red cracks
- runes
- corrupted terrain
- danger zones
```

---

## 7. Дизайн карты для 1000 ботов

Карта не должна быть лабиринтом.

Плохо:

```text
- много узких коридоров
- проходы шириной 1 tile
- слишком много мелких препятствий
- деревья/камни по одному объекту везде
```

Хорошо:

```text
- широкие фронты
- 3–5 направлений атаки
- крупные группы укрытий
- дороги для техники
- несколько флангов
- редкие choke points
- открытые зоны для артиллерии
```

Рекомендуемая структура поля боя:

```text
Союзный спавн
        ↓
Передняя линия
        ↓
Центральные руины / холмы / окопы
        ↓
Вражеская линия
        ↓
Вражеский спавн
```

Фланги:

```text
Верхний фланг: лес / укрытия
Центр: дорога / руины / основная мясорубка
Нижний фланг: холмы / артиллерия / техника
```

---

## 8. Spawn System

Юниты не спавнятся случайно по карте. Нужны spawn zones.

```csharp
public struct SpawnZone
{
    public int TeamId;
    public Rect Area;
    public Vector2 ForwardDirection;
}
```

Расстановка:

```text
Пехота       → широкая линия
Мили-пехота  → ближе к центру
Стрелки      → за укрытиями
Артиллерия   → сзади
Танки        → дороги / открытые зоны
Игрок        → рядом со своим главным отрядом
```

---

## 9. Squad System

Для 1000 ботов нельзя делать 1000 полностью самостоятельных AI.

Правильная схема:

```text
20–30 отрядов думают.
1000 ботов исполняют.
```

Отряд хранит:

```text
- unit config
- count
- faction / team
- order
- target
- formation
- morale
- anchor position
```

Пример армии:

```text
Северный Союз:
F1 Стрелки        180 ботов
F2 Штурмовики     120 ботов
F3 Мили-пехота    100 ботов
F4 Артиллерия      12 орудий
F5 Танки            6 машин

Красная фракция:
A Стрелки         220 ботов
B Мили-пехота     160 ботов
C Тяжёлая пехота   90 ботов
D Артиллерия        8 орудий
E Танки             4 машины
```

MVP-приказы:

```text
AttackNearest
FollowPlayer
HoldPosition
Retreat
```

Later-приказы:

```text
Move
DefendPosition
FocusTarget
FireAtWill
HoldFire
EnterVehicle
```

---

## 10. Pathfinding

Не делать A* для каждого бота.

Плохой вариант:

```text
1000 ботов → каждый строит A* → лаги
```

Лучший вариант:

```text
Squad Flow Field
+
Formation Slots
+
Local Steering
```

Отряд строит одно поле направлений к цели. Все бойцы отряда используют это поле.

```csharp
public struct FlowCell
{
    public ushort Cost;
    public sbyte DirX;
    public sbyte DirY;
}
```

Движение юнита:

```csharp
Vector2 flowDirection = flowField.GetDirection(unit.Position);
Vector2 formationDirection = squad.GetFormationOffsetDirection(unit);
Vector2 separation = spatialGrid.GetSeparation(unit);

unit.Velocity =
    flowDirection * 0.65f +
    formationDirection * 0.25f +
    separation * 0.10f;
```

---

## 11. Spatial Grid

Для поиска врагов, соседей и столкновений нужен spatial grid.

Нельзя делать так:

```text
foreach unit
    foreach enemy
        check distance
```

Это даёт до 1 000 000 проверок.

Нужно делить карту на buckets:

```text
Bucket size: 4x4 или 8x8 Unity units
```

Юнит ищет врагов только в своём bucket и соседних buckets.

Это снижает нагрузку с миллионов проверок до десятков тысяч.

---

## 12. AI Update Rate

Не все системы должны обновляться каждый кадр.

```text
Каждый кадр:
- движение
- поворот спрайта
- синхронизация View
- простые снаряды / трассеры

20 раз/сек:
- local avoidance
- spatial grid update
- melee collision checks

5 раз/сек:
- поиск целей
- решение стрелять / двигаться / укрываться

1 раз/сек:
- morale
- squad order evaluation
- victory/defeat check
```

---

## 13. Unit Data и UnitView

Юнит должен быть данными, а не тяжёлым MonoBehaviour.

Плохо:

```text
Bot GameObject
├── Rigidbody2D Dynamic
├── Collider2D
├── Animator
├── AIController Update()
├── WeaponController Update()
├── HealthBar Canvas
└── PathfindingAgent
```

Хорошо:

```text
Unit Entity Data:
- position
- velocity
- hp
- team
- squad
- weapon
- armor
- order
- target

UnitView:
- Body SpriteRenderer
- Head SpriteRenderer
- Weapon SpriteRenderer
- Shadow
- Selection Marker
```

Правило:

```text
BattleWorld считает.
UnitView показывает.
```

---

## 14. Физика

Для 1000 ботов не использовать тяжёлую физику на каждом пехотинце.

Пехота:

```text
- custom movement через position / velocity
- простые проверки радиусов через SpatialGrid
- без Dynamic Rigidbody2D
```

Техника:

```text
- Kinematic Rigidbody2D или custom movement
- collider только для крупных объектов
```

Снаряды:

```text
- hitscan для стрелкового оружия
- pooled tracer visuals
- projectile simulation только для пушек / артиллерии
```

---

## 15. Стрельба и урон

Для автоматов лучше использовать hitscan-like логику.

```text
Логика:
- рассчитать шанс попадания
- учесть дистанцию
- учесть укрытие
- учесть движение цели
- применить урон

Визуал:
- tracer из пула
- muzzle flash из пула
- звук с лимитом
```

Для артиллерии:

```text
1. Найти скопление врагов через SpatialGrid.
2. Показать warning circle.
3. Через задержку создать взрыв.
4. Проверить targets в радиусе.
5. Нанести Explosion damage.
```

---

## 16. LOD симуляции

Юниты должны иметь разные уровни детализации.

```text
LOD 0 — рядом с камерой / игроком
- полный визуал
- частый AI
- анимации
- реакции на попадания

LOD 1 — на экране, но далеко
- упрощённая анимация
- AI реже
- меньше VFX

LOD 2 — вне экрана
- отрядная симуляция
- редкие обновления
- без анимаций
- без индивидуальных эффектов

Dead LOD
- труп как простой sprite
- без AI
- без collider
- без tween
```

---

## 17. UI боя

HUD не должен показывать 1000 карточек ботов. Он показывает отряды.

```text
Верх слева:
- союзники / враги
- численность
- мораль
- потери

Левая панель:
- F1 Стрелки 112/140
- F2 Штурмовики 80/100
- F3 Мили-пехота 65/90
- F4 Артиллерия 8/10
- F5 Танки 4/6

Низ слева:
- HP игрока
- броня
- выносливость
- оружие
- патроны

Низ центр:
- командное колесо
- Follow
- Hold
- Attack
- Retreat

Верх справа:
- миникарта
- цели боя
- опасные зоны

Низ справа:
- выбранный отряд
- выбранная техника
- наведённый враг
```

---

## 18. Что нельзя делать

```text
- 1000 MonoBehaviour Update
- 1000 Dynamic Rigidbody2D
- 1000 Animator Controller
- 1000 Canvas health bars
- A* для каждого бота
- физическая пуля на каждый выстрел автомата
- каждый куст отдельным тяжёлым объектом
- карта-лабиринт из узких проходов
- бесконечные DOTween-анимации на всех ботах
```

---

## 19. MVP-план разработки

### Этап 1 — карта без ботов

```text
- BattleScene
- Tilemap layers
- BattleTile[]
- генерация Forest / Plains
- spawn zones
- debug overlay walkable / cover / height
```

Цель: локальная карта генерируется из `GlobalCellId + Seed`.

### Этап 2 — 1000 пустых юнитов

```text
- заспавнить 500 vs 500
- без AI
- без стрельбы
- просто стоящие sprites
- проверить FPS
```

Цель: понять лимит визуала.

### Этап 3 — Spatial Grid

```text
- регистрация юнитов в buckets
- поиск соседей
- поиск врагов рядом
- debug display buckets
```

Цель: убрать all-vs-all проверки.

### Этап 4 — движение отрядов

```text
- Squad anchor
- formation slots
- flow field
- local separation
```

Цель: 500 ботов идут к цели без A* на каждого.

### Этап 5 — бой

```text
- ranged hitscan
- melee radius
- damage
- death
- morale
- simple victory condition
```

Цель: 500 vs 500 стабильно сражаются.

### Этап 6 — техника и артиллерия

```text
- артиллерия выбирает скопления
- warning circle
- AoE explosion
- танк движется по vehicle tiles
- танк стреляет
- игрок входит / выходит из танка
```

---

## 20. Итоговая схема

```text
Global Cell
    ↓
BattleRequest
    ↓
Biome Template
    ↓
Tilemap Visual
    ↓
BattleTile[] Logic Grid
    ↓
BattleWorld / Morpeh ECS
    ↓
Squads
    ↓
Flow Fields
    ↓
1000 Unit Entities
    ↓
Lightweight UnitViews
    ↓
BattleResult
```

Главная цель баттл-карты:

```text
дать ощущение огромной битвы,
но считать её достаточно дешёво,
чтобы Unity 2D выдерживала до 1000 ботов.
```
