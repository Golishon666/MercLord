# ARCHITECTURE DOCUMENT

## Project Codename

Dieselpunk 2D Warband

## Цель документа

Этот документ описывает техническую архитектуру проекта для Unity/Codex. Он фиксирует стек, правила реализации, разделение систем, структуры данных, ограничения производительности и правила разработки.

Главный принцип:

~~~
Configs define gameplay.
Prefabs define visuals.
Morpeh systems define logic.
DOTween defines visual animation.
TextMeshPro defines all text.
Code must not hardcode tunable values.
~~~

## SOLID Enforcement Rule

All gameplay and generation code must strictly follow SOLID.

- `SphericalWorldGenerator` and similar pipeline classes must orchestrate focused collaborators, not accumulate terrain, faction, road, river, rendering, UI, or camera logic in one class.
- Terrain/noise, faction partitioning, roads, rivers, movement costs, rendering, and debug UI must stay in separate focused types.
- New behavior should be added through small collaborators or strategies instead of expanding god classes.
- Interfaces must remain narrow and substitutable.
- High-level systems should depend on abstractions or focused services, not unrelated concrete systems.

# 1. Technology Stack

## 1.1 Engine

`Unity 6.x`

## 1.2 Gameplay Architecture

`Morpeh ECS`

Используется для:

- боевых юнитов
- снарядов
- урона
- AI
- движения
- смерти
- ViewSync

## 1.3 Local Map

`Unity Tilemap`

Tilemap используется только для визуального отображения локальной карты. Логика боя работает через отдельный BattleTile[].

## 1.4 Visual Animation

`DOTween`

DOTween используется только для визуала.

## 1.5 Text

`TextMeshPro`

Запрещено использовать legacy Unity Text.

## 1.6 Dependency Injection

`VContainer`

Используется для:

- сервисов
- фабрик
- загрузчиков
- lifetime scopes
- state machine

## 1.7 Async / Loading

~~~
UniTask
Addressables
~~~

## 1.8 UI Events / Reactive

`R3`

Используется для UI, не для горячей боевой логики 1000 ботов.

## 1.9 Rendering

~~~
URP 2D Renderer
SpriteRenderer
SpriteAtlas
Shader Graph optional
Cinemachine optional
~~~

# 2. High-Level Architecture

## 2.1 Main Modes

Игра разделена на два основных режима:

~~~
Global Mode
Battle Mode
~~~

## 2.2 Global Mode

Global Mode отвечает за:

- планету
- соты
- биомы
- фракции
- армии
- поселения
- движение игрока
- глобальное время
- влияние фракций
- старт боя

## 2.3 Battle Mode

Battle Mode отвечает за:

- локальную Tilemap карту
- BattleTile[] grid
- Morpeh BattleWorld
- спавн юнитов
- бой
- снаряды
- техника
- урон
- лут
- BattleResult

## 2.4 Scene Flow

~~~
BootstrapScene
→ MainMenuScene
→ GlobalScene
→ BattleScene
→ GlobalScene
~~~

BattleScene загружается только на время боя.

# 3. Scenes

## 3.1 BootstrapScene

Содержит:

- GameLifetimeScope
- GameStateMachine
- SaveService
- ConfigDatabase
- AddressablesService
- SceneLoader

## 3.2 GlobalScene

Содержит:

- GlobalLifetimeScope
- GlobalMapRoot
- GlobalSceneRoot
- PlanetRenderer
- GlobalCamera
- GlobalUI

## 3.3 BattleScene

Содержит:

- BattleLifetimeScope
- BattleSceneRoot
- Tilemap Grid
- BattleCamera
- BattleUI
- View Pools
- BattleWorld runner

## 3.4 LoadingScene

Опционально.

Используется для:

- переходов
- прогресса загрузки
- асинхронной генерации

# 4. Game State Machine

## 4.1 States

~~~
BootstrapState
MainMenuState
GenerateWorldState
LoadGlobalState
GlobalMapState
EnterBattleState
BattleState
ExitBattleState
SaveLoadState
~~~

## 4.2 New Game Flow

1. MainMenu/UI sends `NewGameRequest` to `GenerateWorldState`.
2. `GenerateWorldState` reads `GlobalGenerationConfig`.
3. `IWorldGenerator` creates `WorldModel`.
4. Optional selected culture applies player start cell, culture id and credits.
5. `SaveService` creates current `SaveModel` from the generated world.
6. `LoadGlobalState` loads/activates `GlobalScene`.
7. `GlobalMapState` becomes active.

`NewGameRequest` may override seed and culture, but it must not carry faction
count or gameplay balance values. Those come from configs.

## 4.3 EnterBattle Flow

1. GlobalMapState detects encounter.
2. Create BattleGenerationRequest.
3. Save current global context.
4. Enter EnterBattleState.
5. Generate local map.
6. Create BattleWorld.
7. Convert squads into entities.
8. Store active BattleSession.
9. Load BattleScene.
10. BattleSceneRoot spawns prefab views for entities missing ViewRefComponent.
11. Switch to BattleState.

BattleSession is created before BattleScene loads so scene startup can bind
views to the already active Morpeh world.

## 4.4 ExitBattle Flow

1. BattleResult is produced.
2. Stop BattleWorld.
3. Return all views to pools.
4. Clear active BattleSession.
5. Apply BattleResult to SaveModel.
6. Load/activate GlobalScene.
7. Return to GlobalMapState.

BattleResult must be applied before GlobalScene renders so the global map never
briefly displays stale army, influence, inventory, or player data.

# 5. Project Folder Structure

~~~
Assets/
├── Bootstrap/
├── Game/
│   ├── StateMachine/
│   ├── Configs/
│   ├── Save/
│   ├── Services/
│   └── Input/
│
├── Global/
│   ├── Generation/
│   ├── Cells/
│   ├── Factions/
│   ├── Armies/
│   ├── Settlements/
│   ├── Time/
│   ├── Rendering/
│   └── UI/
│
├── Battle/
│   ├── Generation/
│   ├── Tiles/
│   ├── ECS/
│   │   ├── Components/
│   │   ├── Systems/
│   │   ├── Features/
│   │   └── Events/
│   ├── Bots/
│   ├── Combat/
│   ├── Projectiles/
│   ├── Vehicles/
│   ├── Pathfinding/
│   ├── Rendering/
│   └── UI/
│
├── Player/
│   ├── Inventory/
│   ├── Equipment/
│   ├── Input/
│   └── Profile/
│
├── Economy/
│   ├── Credits/
│   ├── Loot/
│   └── Trading/
│
├── Infrastructure/
│   ├── Addressables/
│   ├── Pooling/
│   ├── Audio/
│   ├── Debug/
│   ├── Validation/
│   └── Extensions/
│
└── Art/
    ├── Sprites/
    ├── Tiles/
    ├── VFX/
    ├── UI/
    └── Fonts/
~~~

# 6. Config System

## 6.1 Core Rule

Все gameplay-настройки идут через configs.

Запрещён хардкод:

- damage
- health
- speed
- range
- cooldown
- armor values
- price
- loot values
- AI intervals
- spatial hash cell size
- projectile speed
- parabolic arc height
- explosion radius

## 6.2 Config List

~~~
FactionConfig
CultureConfig
UnitConfig
WeaponConfig
ArmorConfig
AIConfig
VehicleConfig
ItemConfig
TradeGoodConfig
LootTableConfig
BiomeConfig
TileSetConfig
BattleMapGenerationConfig
BattleSimulationConfig
GlobalGenerationConfig
InputConfig optional
~~~

Количество активных фракций определяется массивом `ConfigDatabase.Factions`.
`WorldGenerationRequest` не передаёт количество фракций. Стартовые ресурсы,
сила и столица каждой фракции задаются в её `FactionConfig`.

Текущая модель влияния `Influence4` поддерживает до 4 активных слотов влияния.
Это ограничение модели влияния, а не параметр интерфейса генерации.

## 6.3 FactionConfig

~~~csharp
public sealed class FactionConfig
{
    public int Id;
    public string Name;
    public Color Color;
    public int StartingCredits;
    public int StartingStrength;
    public int CapitalCellId;
}
~~~

## 6.4 UnitConfig

~~~csharp
public sealed class UnitConfig
{
    public int Id;
    public string Name;

    public int FactionId;
    public UnitCategory Category;

    public int MaxHealth;
    public float MoveSpeed;
    public float RotationSpeed;

    public WeaponConfig Weapon;
    public ArmorConfig Armor;
    public AIConfig AI;

    public string ViewPrefabAddress;
}
~~~

## 6.5 WeaponConfig

~~~csharp
public sealed class WeaponConfig
{
    public int Id;
    public string Name;

    public WeaponType Type;
    public DamageType DamageType;

    public int Damage;
    public float Range;
    public float Cooldown;
    public float ProjectileSpeed;

    public bool IsProjectile;
    public bool UsesParabolicTrajectory;
    public float ParabolicArcHeight;
    public float ExplosionRadius;
}
~~~

## 6.6 ArmorConfig

~~~csharp
public sealed class ArmorConfig
{
    public int Id;
    public string Name;

    public int BallisticProtection;
    public int EnergyProtection;
    public int ExplosionProtection;
}
~~~

## 6.7 AIConfig

~~~csharp
public sealed class AIConfig
{
    public int Id;
    public AIType Type;

    public float ThinkInterval;
    public float TargetSearchRadius;
    public float PreferredAttackDistance;
    public float RetreatHealthPercent;
}
~~~

## 6.8 BattleSimulationConfig

~~~csharp
public sealed class BattleSimulationConfig
{
    public int Id;
    public string Name;

    public float SpatialHashCellSize;
    public UnitConfig PlayerUnit;
    public BattleSpawnSide PlayerSpawnSide;
    public int PlayerSpawnPointIndex;
    public float PlayerAimDotThreshold;
}
~~~

## 6.9 VehicleConfig

~~~csharp
public sealed class VehicleConfig
{
    public int Id;
    public string Name;

    public int MaxHealth;
    public float MoveSpeed;
    public float RotationSpeed;

    public ArmorConfig Armor;
    public WeaponConfig Weapon;

    public string ViewPrefabAddress;
}
~~~

## 6.9 BattleMapGenerationConfig

~~~csharp
public sealed class BattleMapGenerationConfig
{
    public int Width;
    public int Height;

    public int DefaultMoveCost;
    public int RoadMoveCost;
    public int DefaultCover;
    public int SettlementCover;
    public int MaxTileHeight;

    public int RoadColumn;
    public int RoadWidth;

    public int AttackerSpawnColumns;
    public int DefenderSpawnColumns;
}
~~~

# 7. GameObject Prefab Rule

## 7.1 Core Rule

Все визуальные объекты создаются только через Unity GameObject Prefabs.

Это относится к:

- пехоте
- игроку
- технике
- артиллерии
- снарядам
- взрывам
- эффектам
- луту
- UI
- тексту
- маркерам

Запрещено вручную собирать visual hierarchy через new GameObject().

Это жёсткое правило проекта, не рекомендация:

- Runtime-код не создаёт визуальные иерархии вручную.
- Runtime-код не задаёт визуальные размеры, offsets, sorting, anchors, цвета, tween durations или layout-числа.
- Runtime-код может только инстанцировать готовый prefab через фабрику/пул и передать ему gameplay-состояние.
- Editor tooling может создавать production-prefab assets только как ассеты проекта, а не как скрытый gameplay shortcut.
- Любой временный placeholder тоже должен быть prefab-based и иметь настройки в prefab/settings-компонентах.

## 7.2 Prefab Owns View Settings

Prefab хранит:

- scale
- visual size
- root transforms
- body/head/weapon/shield/backpack anchors
- SpriteRenderer references
- sorting layer/order
- pivot offsets
- muzzle point
- hit point
- selection radius
- shadow size
- vehicle seat points
- enter/exit points
- DOTween animation settings
- UI layout settings

## 7.3 Gameplay Configs vs Prefabs

~~~
Gameplay configs:
- баланс
- здоровье
- урон
- скорость
- броня
- AI
- цена

Prefabs:
- визуальный размер
- иерархия
- anchors
- sprite renderers
- sorting
- DOTween settings
- offsets
~~~

## 7.4 Final Rule

~~~
Gameplay numbers live in configs.
View numbers live in prefabs.
Code contains no magic numbers.
~~~

Запрещено добавлять hardcoded gameplay или view values ради ускорения разработки.

Если системе нужен параметр, он должен прийти из одного из источников:

- gameplay config
- prefab settings component
- scene/lifetime scope reference
- save/runtime model state
- input event/state

Если подходящего источника ещё нет, сначала создаётся источник данных, а не временный hardcode.

# 8. TextMeshPro Rule

## 8.1 Core Rule

Весь текст в игре должен использовать TextMeshPro.

Разрешено:

~~~
TextMeshProUGUI
TextMeshPro
TMP_InputField
~~~

Запрещено:

~~~
UnityEngine.UI.Text
legacy Text
~~~

## 8.2 Text Usage

TextMeshPro используется для:

- UI
- кнопок
- меню
- inventory text
- item descriptions
- Credits display
- tooltips
- city names
- army labels
- damage numbers
- floating combat text
- debug overlay
- FPS counter

## 8.3 Text Prefab Rule

Текстовые элементы тоже prefab-based.

Код может менять только:

- text value
- localization key later

Код не должен хардкодить:

- font
- font size
- color
- alignment
- outline
- spacing

# 9. DOTween Rule

## 9.1 Core Rule

DOTween используется только для визуальных анимаций.

Разрешено:

- idle bob
- move bob
- shoot recoil
- melee swing
- hit shake
- death collapse
- vehicle enter/exit visual
- UI animations
- floating combat text

Запрещено:

- gameplay movement
- damage calculation
- hit detection
- AI decisions
- pathfinding
- battle state

## 9.2 DOTween Settings

Все DOTween-параметры должны идти из prefab settings.

Запрещено:

~~~csharp
WeaponRoot.DOLocalMoveX(-0.05f, 0.04f);
~~~

Правильно:

~~~csharp
var settings = view.Settings.TweenSettings;

WeaponRoot.DOLocalMoveX(
    settings.ShootRecoilDistance,
    settings.ShootRecoilTime
);
~~~

# 10. Global Data Model

## 10.1 WorldModel

~~~csharp
public sealed class WorldModel
{
    public int Seed;
    public int CurrentDay;

    public WorldCell[] Cells;
    public CellNeighbours[] Neighbours;

    public FactionData[] Factions;
    public SettlementData[] Settlements;
    public ArmyData[] Armies;

    public PlayerGlobalData Player;
}
~~~

## 10.2 WorldCell

~~~csharp
public struct WorldCell
{
    public int Id;

    public BiomeType Biome;
    public int RegionId;

    public float Height;
    public float Moisture;
    public float Temperature;

    public int OwnerFactionId;
    public int DominantFactionId;

    public Influence4 Influence;

    public int SettlementId;
    public bool HasRoad;
    public bool IsPassable;
}
~~~

## 10.3 Influence4

~~~csharp
public struct Influence4
{
    public const int Capacity = 4;

    public float F0;
    public float F1;
    public float F2;
    public float F3;
}
~~~

`F0..F3` — это слоты влияния. Игровой `FactionId` берётся из
`FactionConfig.Id`; слот влияния не должен подменять id фракции.

## 10.4 CellNeighbours

~~~csharp
public struct CellNeighbours
{
    public int N0;
    public int N1;
    public int N2;
    public int N3;
    public int N4;
    public int N5;
}
~~~

Для pentagon-сот недостающий сосед = -1.

## 10.5 ArmyData

~~~csharp
public struct ArmyData
{
    public int Id;
    public int FactionId;
    public int CellId;
    public int TargetCellId;

    public SquadData[] Squads;
}
~~~

## 10.6 SquadData

~~~csharp
public struct SquadData
{
    public int UnitConfigId;
    public int Count;
}
~~~

# 11. Global Time Architecture

## 11.1 GlobalTimeMode

~~~csharp
public enum GlobalTimeMode
{
    Paused,
    PlayerMoving,
    Waiting
}
~~~

## 11.2 GlobalTimeService

~~~csharp
public sealed class GlobalTimeService
{
    public GlobalTimeMode Mode;
    public float SimulationSpeed;
}
~~~

## 11.3 Time-Affected Systems

Обновляются только если время активно:

~~~
ArmyMovementSystem
FactionWarSystem
FactionInfluenceSystem
EconomySystem
SettlementProductionSystem
QuestTimeSystem later
EventSystem
~~~

Не обновляются при Paused.

# 12. Battle Data Model

## 12.1 BattleGenerationRequest

~~~csharp
public struct BattleGenerationRequest
{
    public int Seed;
    public int SourceCellId;

    public BiomeType Biome;
    public int DominantFactionId;

    public bool HasRoad;
    public bool NearSettlement;

    public float Height;
    public float Moisture;
    public float Temperature;
}
~~~

## 12.2 BattleModel

~~~csharp
public sealed class BattleModel
{
    public int Seed;
    public int SourceCellId;

    public int Width;
    public int Height;

    public BattleTile[] Tiles;
    public BattleSpawnPoint[] AttackerSpawnPoints;
    public BattleSpawnPoint[] DefenderSpawnPoints;

    public BattleArmyData Attacker;
    public BattleArmyData Defender;
}
~~~

~~~csharp
public struct BattleSpawnPoint
{
    public int X;
    public int Y;
}
~~~

## 12.3 BattleTile

~~~csharp
public struct BattleTile
{
    public bool Walkable;
    public byte MoveCost;
    public byte Cover;
    public byte Height;
}
~~~

# 13. Battle Pipeline

## 13.1 Start Battle

1. Player encounters enemy army.
2. Read current WorldCell.
3. Create BattleGenerationRequest.
4. Generate BattleTile[].
5. Create Morpeh BattleWorld.
6. Convert squads into battle entities.
7. Store BattleSession.
8. Load BattleScene.
9. BattleSceneRoot spawns prefab-backed unit views through BattleViewSpawner.
10. Spawn player entity.
11. Spawn vehicles/projectiles pools.
12. Start battle systems.

`IBattlePipeline.StartBattleAsync` creates a `BattleSession` containing the
generated `BattleModel` and Morpeh `World`. It does not choose a winner or
fabricate rewards. Battle systems produce `BattleResult` later.

## 13.2 End Battle

1. Winner detected.
2. Create BattleResult.
3. Generate loot.
4. Add CreditsReward.
5. Stop BattleWorld.
6. Return views to pools.
7. Clear BattleSession.
8. Apply result to WorldModel.
9. Return to GlobalScene.

# 14. Morpeh BattleWorld

## 14.1 Core Components

~~~csharp
public struct BotComponent
{
    public int UnitConfigId;
}

public struct PositionComponent
{
    public Unity.Mathematics.float2 Value;
}

public struct VelocityComponent
{
    public Unity.Mathematics.float2 Value;
}

public struct HealthComponent
{
    public int Current;
    public int Max;
}

public struct TeamComponent
{
    public BattleTeamType Value;
}

public struct FactionComponent
{
    public int Value;
}

public struct MovementStatsComponent
{
    public float MoveSpeed;
    public float RotationSpeed;
}

public struct WeaponStatsComponent
{
    public int WeaponConfigId;
    public WeaponType Type;
    public DamageType DamageType;
    public int Damage;
    public float Range;
    public float Cooldown;
    public float ProjectileSpeed;
    public bool IsProjectile;
    public bool UsesParabolicTrajectory;
    public float ExplosionRadius;
}

public struct ArmorStatsComponent
{
    public int ArmorConfigId;
    public int BallisticProtection;
    public int EnergyProtection;
    public int ExplosionProtection;
}

public struct AIStatsComponent
{
    public int AIConfigId;
    public AIType Type;
    public float ThinkInterval;
    public float TargetSearchRadius;
    public float PreferredAttackDistance;
    public float RetreatHealthPercent;
}

public struct TargetComponent
{
    public Morpeh.Entity Target;
}

public struct AttackCooldownComponent
{
    public float Value;
}

public struct BotStateComponent
{
    public BotStateType Value;
}

public struct ViewRefComponent
{
    public int ViewId;
}
~~~

`ConfigDrivenBattleWorldFactory` converts `BattleModel` armies into Morpeh
entities. It reads `UnitConfig`, `WeaponConfig`, `ArmorConfig`, and `AIConfig`
once during world creation and stores runtime-ready values in ECS components.
The factory does not create views or manual GameObject hierarchies; view
creation belongs to the prefab pool/view sync layer.

## 14.2 Player Components

~~~csharp
public struct PlayerControlledComponent
{
}

public struct PlayerInputComponent
{
    public Unity.Mathematics.float2 MoveDirection;
    public Unity.Mathematics.float2 AimDirection;

    public bool FirePressed;
    public bool InteractPressed;
    public int SelectedWeaponSlot;
}
~~~

## 14.3 Vehicle Components

~~~csharp
public struct VehicleComponent
{
    public int VehicleConfigId;
    public Morpeh.Entity Driver;
}

public struct DriverComponent
{
    public Morpeh.Entity ControlledVehicle;
}
~~~

## 14.4 Projectile Components

~~~csharp
public struct ProjectileComponent
{
    public Morpeh.Entity Source;
    public int Damage;
    public DamageType DamageType;
    public float Speed;
}

public struct ParabolicProjectileComponent
{
    public Unity.Mathematics.float2 Start;
    public Unity.Mathematics.float2 Target;
    public float FlightTime;
    public float ElapsedTime;
    public float ArcHeight;
}

public struct ExplosionOnImpactComponent
{
    public float Radius;
}
~~~

# 15. Battle Systems

## 15.1 System List

~~~
BotSpawnSystem
PlayerInputSystem
VehicleInputSystem
VehicleEnterSystem
VehicleExitSystem
SpatialHashSystem
TargetSearchSystem
DecisionSystem
MovementSystem
AttackRequestSystem
WeaponSystem
ProjectileSystem
ParabolicProjectileSystem
ExplosionSystem
DamageSystem
ArmorSystem
DeathSystem
LootGenerationSystem
BattleResultSystem
ViewSyncSystem
ViewAnimationSystem
~~~

## 15.2 System Rules

- BattleSceneRoot owns one frame runner for the active BattleSession.
- SpatialHashSystem rebuilds a reusable position index from alive battle
  entities. Its cell size comes from BattleSimulationConfig.
- BattlePlayerSpawner creates the player entity from
  BattleSimulationConfig.PlayerUnit before views and runtime systems start.
- PlayerInputSystem reads IBattleInputSource, writes PlayerInputComponent,
  VelocityComponent and AttackRequestComponent, and never applies damage
  directly.
- PlayerInputSystem resolves the selected equipped weapon through
  SaveModel.PlayerEquipment -> ItemConfig -> WeaponConfig.
- TargetSearchSystem uses SpatialHashSystem and AIConfig.ThinkInterval to
  stagger target scans. It must not scan every opponent for every bot.
- DecisionSystem updates intent: VelocityComponent for movement and
  AttackRequestComponent for attacks. PlayerControlled entities are excluded.
- WeaponSystem consumes AttackRequestComponent, applies WeaponStatsComponent,
  starts cooldowns, and emits direct DamageRequestComponent or projectile
  entities.
- ProjectileSystem and ParabolicProjectileSystem move projectile entities and
  emit DamageRequestComponent on impact.
- MovementSystem can run every frame.
- MovementSystem updates PositionComponent from VelocityComponent and
  MovementStatsComponent; movement speed comes from UnitConfig.
- AI systems must use staggered update.
- Target search must use Spatial Hash.
- No O(N²) scan every frame.
- No per-bot MonoBehaviour Update.
- ViewSyncSystem is the only system that updates transforms.
- ViewSyncSystem maps PositionComponent to prefab instance transform through
  BattleViewCatalog and ViewRefComponent.
- DamageSystem processes DamageRequestComponent entities, applies
  CombatBalanceConfig damage formula, updates HealthComponent and sets
  DeadComponent/BotStateType.Dead when health reaches zero.
- Combat systems do not access SpriteRenderer or DOTween.

# 16. Shared Combat Architecture

## 16.1 Core Rule

Все боевые сущности используют общую систему боя.

Это относится к:

- игроку
- ботам
- технике
- артиллерии
- снарядам
- AoE
- спец-предметам

## 16.2 Attack Flow

~~~
PlayerInputSystem
→ AttackRequest

BotAISystem
→ AttackRequest

VehicleInputSystem
→ AttackRequest

WeaponSystem
→ ProjectileSystem / MeleeHit
→ DamageSystem
→ ArmorSystem
→ DeathSystem
~~~

## 16.3 Player Difference

Игрок отличается только:

~~~
PlayerControlledComponent
PlayerInputComponent
PlayerInventory
PlayerEquipment
~~~

Игрок не имеет отдельной захардкоженной боевой системы.

# 17. Damage and Armor System

## 17.1 DamageType

~~~csharp
public enum DamageType
{
    Ballistic,
    Energy,
    Explosion
}
~~~

## 17.2 DamageRequest

~~~csharp
public struct DamageRequest
{
    public Morpeh.Entity Target;
    public Morpeh.Entity Source;

    public int Amount;
    public DamageType DamageType;

    public Unity.Mathematics.float2 HitPosition;
}
~~~

## 17.3 Armor Resolution

~~~
Ballistic damage → BallisticProtection
Energy damage → EnergyProtection
Explosion damage → ExplosionProtection
~~~

Base formula:

`finalDamage = max(CombatBalanceConfig.MinimumDamage, incomingDamage - protection)`

# 18. Local Tilemap Architecture

## 18.1 Tilemap Layers

~~~
Grid
├── GroundTilemap
├── RoadTilemap
├── ObstacleTilemap
├── DecorationTilemap
├── OverlayTilemap
└── DebugTilemap
~~~

## 18.2 Tilemap Rule

Tilemap is visual.

BattleTile[] is gameplay.

Запрещено:

- читать Tilemap каждый кадр для AI/pathfinding
- использовать GameObject на каждый тайл

## 18.3 Tilemap Generator

Input:

~~~
BattleGenerationRequest
BattleMapGenerationConfig
TileSetConfig
~~~

Output:

- visual Tilemap
- BattleTile[]
- spawn points
- optional debug data

`ConfigDrivenBattleMapGenerator` produces the gameplay `BattleTile[]` and
spawn points from `BattleMapGenerationConfig`. Visual Tilemap rendering is a
separate layer and must not be read by AI/pathfinding at runtime.

# 19. Pathfinding and Movement

## 19.1 Forbidden

- 1000 NavMeshAgent
- A* every frame per bot
- full enemy scan per bot
- Unity Rigidbody2D for all bots

## 19.2 Allowed

- Spatial Hash for target search
- Flow Field per squad/group later
- simple steering for MVP
- separation
- A* only for special units/player if needed

## 19.3 MVP Movement

Для MVP:

- direct steering toward target
- simple obstacle avoidance
- simple separation
- no complex formation

Later:

- Flow Field per squad
- formation orders
- cover seeking

# 20. Spatial Hash

## 20.1 Purpose

Spatial Hash используется для:

- поиска ближайших врагов
- AoE checks
- local avoidance
- projectile hit checks

## 20.2 Rule

Запрещено делать полный перебор 1000x1000 каждый кадр.

Target search должен проверять только соседние spatial cells.

# 21. View Layer

## 21.1 Core Rule

View отображает состояние. View не принимает gameplay-решения.

View может:

- менять спрайты
- проигрывать DOTween
- обновлять transform
- отображать эффекты

View не может:

- искать цель
- считать урон
- управлять AI
- менять Health
- решать попадания

## 21.2 Global Map Rendering

`GlobalSceneRoot` получает текущий `SaveModel` и вызывает `IGlobalMapPresenter`.

`IGlobalMapPresenter`:

- читает `WorldModel`
- создаёт cell/player/army views только через `IPrefabFactory`
- берёт layout numbers из `GlobalMapViewSettings`
- берёт biome/faction colors из configs
- не генерирует мир и не меняет gameplay state

`IWorldCellLayout` отделяет способ раскладки сот от presenter. Текущая
configured grid layout является заменяемой стратегией для раннего global view.

## 21.3 InfantryView

~~~csharp
public sealed class InfantryView : MonoBehaviour
{
    public InfantryViewSettings Settings;

    public SpriteRenderer BodyRenderer;
    public SpriteRenderer HeadRenderer;
    public SpriteRenderer WeaponRenderer;
    public SpriteRenderer ShieldRenderer;
    public SpriteRenderer BackpackRenderer;
}
~~~

## 21.4 InfantryViewSettings

~~~csharp
public sealed class InfantryViewSettings : MonoBehaviour
{
    public Transform BodyRoot;
    public Transform HeadRoot;
    public Transform WeaponRoot;
    public Transform ShieldRoot;
    public Transform BackpackRoot;

    public Transform MuzzlePoint;
    public Transform HitPoint;
    public Transform SelectionRoot;

    public float SelectionRadius;
    public float ShadowScale;

    public InfantryTweenSettings TweenSettings;
}
~~~

# 22. Modular Infantry Visuals

## 22.1 Sprite Sets

~~~csharp
public sealed class BodySpriteSetConfig
{
    public int Id;
    public string Name;

    public Sprite Front;
    public Sprite Back;
    public Sprite Left;
    public Sprite Right;
}

public sealed class HeadSpriteSetConfig
{
    public int Id;
    public string Name;

    public Sprite Front;
    public Sprite Back;
    public Sprite Left;
    public Sprite Right;
}
~~~

## 22.2 Direction

~~~csharp
public enum UnitViewDirection
{
    Front,
    Back,
    Left,
    Right
}
~~~

Direction выбирается по aim direction, velocity direction или target direction.

# 23. Object Pooling

## 23.1 Pool Everything Frequent

Пулить:

- infantry views
- vehicle views
- projectiles
- explosions
- muzzle flashes
- floating text
- hit effects
- loot drops

## 23.2 Spawn Rule

~~~
ECS Spawn System
→ UnitViewFactory
→ ObjectPool
→ GameObject prefab
→ ViewRefComponent
~~~

Боевые системы не создают views напрямую.

`BattleViewSpawner` finds battle entities with `BotComponent` and without
`ViewRefComponent`, resolves `UnitConfig.ViewPrefabAddress` through
`BattleViewCatalog`, rents a prefab instance from `GameObjectPool`, and writes
the resulting id to `ViewRefComponent`.

Battle view position mapping is serialized in `BattleViewCatalog`. View
prefab internals and presentation settings stay on the prefab components.
`PrefabValidator` must validate `BattleSceneRoot`, `BattleLifetimeScope`, and
`BattleViewCatalog` references before the battle scene is considered ready.

# 24. Input Architecture

## 24.1 Use Unity Input System

Unity Input System adapters feed battle input through IBattleInputSource.
ECS battle systems depend on the input source contract, not directly on the
Unity package API.

Использовать Unity Input System.

## 24.2 Input Layers

~~~
GlobalInput
BattleInput
UIInput
~~~

## 24.3 Rule

Input systems создают intent/components.

Они не должны напрямую менять Health, Damage, AI или BattleResult.

# 25. Camera Architecture

## 25.1 Global Camera

Отвечает за:

- orbit/rotation around planet
- zoom
- selection raycast

## 25.2 Battle Camera

Отвечает за:

- follow player
- zoom
- camera bounds
- shake visual only

Камера не управляет ECS-логикой.

# 26. Sorting Layers

Рекомендуемые sorting layers:

~~~
Ground
Road
Obstacle
Decoration
Corpses
Units
Vehicles
Projectiles
VFX
Selection
WorldText
UI
~~~

Запрещено полагаться на случайный order.

# 27. Economy Architecture

## 27.1 Credits

~~~csharp
public sealed class PlayerWallet
{
    public int Credits;
}
~~~

## 27.2 CreditsService

Отвечает за:

- AddCredits
- SpendCredits
- CanAfford

## 27.3 Trade Goods

~~~csharp
public sealed class TradeGoodConfig
{
    public int Id;
    public string Name;

    public int BasePrice;
    public LootRarity Rarity;

    public string IconAddress;
    public string Description;
}
~~~

## 27.4 Loot

~~~csharp
public struct BattleLootEntry
{
    public int ItemConfigId;
    public int Amount;
    public int Durability;
}
~~~

# 28. Inventory and Equipment

## 28.1 ItemInstance

~~~csharp
public struct ItemInstance
{
    public int ConfigId;
    public int Amount;
    public int Durability;
}
~~~

## 28.1.1 ItemConfig Equipment Mapping

~~~csharp
public sealed class ItemConfig
{
    public ItemCategory Category;
    public int Price;

    public WeaponConfig Weapon;
    public ArmorConfig Armor;
}
~~~

Weapon equipment slots resolve to WeaponConfig through ItemConfig. Armor and
helmet slots resolve to ArmorConfig through ItemConfig. Runtime systems must
not hardcode weapon or armor ids.

## 28.2 PlayerInventory

~~~csharp
public sealed class PlayerInventory
{
    public List<ItemInstance> Items = new();
}
~~~

## 28.3 PlayerEquipment

~~~csharp
public sealed class PlayerEquipment
{
    public ItemInstance BodyArmor;
    public ItemInstance Helmet;

    public ItemInstance WeaponSlot1;
    public ItemInstance WeaponSlot2;
    public ItemInstance WeaponSlot3;
    public ItemInstance WeaponSlot4;

    public ItemInstance SpecialSlot1;
    public ItemInstance SpecialSlot2;
    public ItemInstance SpecialSlot3;
    public ItemInstance SpecialSlot4;
}
~~~

## 28.4 Equipment Slot Validation

~~~
Body slot accepts only Armor.
Helmet slot accepts only Helmet.
Weapon slots accept only Weapon.
Special slots accept only Special.
~~~

# 29. BattleResult

## 29.1 BattleResult Model

~~~csharp
public sealed class BattleResult
{
    public BattleOutcome Outcome;
    public int SourceCellId;
    public int WinnerFactionId;

    public bool PlayerSurvived;
    public bool HasPlayerPartyUpdate;

    public int CreditsReward;
    public SquadData[] PlayerParty;
    public BattleArmyUpdate[] ArmyUpdates;
    public BattleInfluenceChange[] InfluenceChanges;
    public BattleLootEntry[] Loot;
}
~~~

## 29.2 Application to Global World

After battle:

- remove dead units from armies
- add loot to inventory
- add credits to wallet
- update faction influence
- remove destroyed armies
- update player state

`BattleResult` содержит уже рассчитанные изменения. `BattleResultApplier`
не генерирует награды и не выбирает победителя; он только применяет результат
к `SaveModel`.

# 30. Save / Load

## 30.1 Save Contains

- version
- seed
- current day
- player culture
- player credits
- player inventory
- player equipment
- player global cell
- world cells ownership/influence
- factions
- armies
- settlements

## 30.2 Save Does Not Contain

- GameObjects
- Prefabs
- Tilemap runtime objects
- BattleWorld runtime entities
- DOTween state
- temporary projectiles
- temporary VFX
- temporary floating text

## 30.3 MVP Save Rule

Для MVP:

~~~
Saving is allowed only on global map.
Saving during battle is disabled.
~~~

# 31. Debug Tools

## 31.1 Required Debug Commands

- spawn 1000 bots
- kill all enemies
- win battle
- lose battle
- add credits
- add loot
- teleport player to cell
- regenerate planet
- show FPS
- show alive units
- show spatial hash
- show tile walkability
- show faction influence
- enter test battle
- spawn tank
- enter tank

## 31.2 Debug UI

Debug UI must use TextMeshPro.

Debug commands must not be part of release UI.

# 32. Validation

## 32.1 Config Validation

CultureConfig invalid if:

- StartingCellId is outside generated world cells
- StartingCredits < 0
- no starting weapon
- no starting armor

UnitConfig invalid if:

- no weapon
- no armor
- no AI config
- no ViewPrefabAddress
- MaxHealth <= 0
- MoveSpeed <= 0

WeaponConfig invalid if:

- Damage <= 0
- Range <= 0
- Cooldown <= 0
- ProjectileSpeed <= 0 for projectile weapons
- ParabolicArcHeight <= 0 for parabolic weapons

AIConfig invalid if:

- ThinkInterval <= 0
- TargetSearchRadius <= 0
- PreferredAttackDistance < 0
- RetreatHealthPercent is outside 0..1

ArmorConfig invalid if:

- any protection < 0

BattleSimulationConfig invalid if:

- SpatialHashCellSize <= 0
- PlayerUnit is missing or not registered in ConfigDatabase
- PlayerUnit.Category is not Player
- PlayerSpawnPointIndex < 0
- PlayerSpawnPointIndex is outside generated spawn capacity
- PlayerAimDotThreshold is outside -1..1

BattleMapGenerationConfig invalid if:

- Width <= 0
- Height <= 0
- move costs <= 0
- tile byte values are outside byte range
- RoadWidth <= 0
- RoadColumn/RoadWidth are outside map width
- attacker/defender spawn columns are outside map width
- referenced biome TileSetConfig is missing

## 32.2 Prefab Validation

Global map prefab invalid if:

- no PlanetRenderer
- no GlobalMapViewSettings
- missing cell/player/army prefabs
- cell/player/army prefabs miss required renderer/TextMeshPro refs
- missing cell/marker roots
- layout column count <= 0
- cell spacing is not configured
- cell visual scale <= 0
- influence overlay alpha is outside 0..1

Infantry prefab invalid if:

- no InfantryView
- no InfantryViewSettings
- no BodyRenderer
- no HeadRenderer
- no WeaponRenderer
- no BodyRoot
- no HeadRoot
- no WeaponRoot

Vehicle prefab invalid if:

- no VehicleView
- no VehicleViewSettings
- no BodyRoot
- no MuzzlePoint
- no EnterPoint
- no ExitPoint

Text prefab invalid if:

- uses UnityEngine.UI.Text
- missing TextMeshProUGUI/TextMeshPro

# 33. Performance Rules

## 33.1 Battle Target

~~~
1000 bots
60 FPS target
~~~

## 33.2 Hard Rules

- no per-bot MonoBehaviour Update
- no per-tile GameObject
- no 1000 NavMeshAgent
- no O(N²) target search every frame
- no allocations in hot ECS systems
- no hardcoded tunable values

## 33.3 Optimization Order

If performance drops:

1. reduce AI tick rate
2. reduce target search frequency
3. optimize Spatial Hash
4. batch ViewSync
5. reduce DOTween on far/hidden units
6. use simpler animations for distant units
7. move heavy calculations to Jobs/Burst
8. add Flow Field for group movement

# 34. Acceptance Criteria

## 34.1 DamageSystem Done When

- Ballistic uses BallisticProtection
- Energy uses EnergyProtection
- Explosion uses ExplosionProtection
- final damage modifies HealthComponent
- dead entity enters Dead state

## 34.2 TilemapGenerator Done When

- generates visual Tilemap
- generates BattleTile[]
- generates attacker/defender spawn points
- supports at least Plains, Forest, Desert, AshWastes, IndustrialRuins
- does not use GameObject per tile

Gameplay map generation is complete before visual Tilemap generation when
`IBattleMapGenerator` returns a valid `BattleModel` from configs.

## 34.3 BotSpawnSystem Done When

- spawns entities from UnitConfig
- army size and spawn capacity are validated before entity creation
- combat, movement, armor, and AI values come from configs
- creates view via prefab pool
- attaches ViewRefComponent
- no manual GameObject hierarchy creation

## 34.4 PlayerControl Done When

- player entity moves with input
- player can aim
- player can attack with equipped weapon
- player uses shared WeaponSystem
- AI ignores PlayerControlled entity

## 34.5 VehicleControl Done When

- player can enter tank
- input controls tank while inside
- player can exit tank
- tank uses shared WeaponSystem and DamageSystem

## 34.6 LootSystem Done When

- BattleResult contains loot
- loot is added to inventory
- TradeGood can be sold for Credits

## 34.7 PrefabRule Done When

- all views are prefab-based
- no hardcoded view size/offsets
- settings are serialized in Inspector
- configs only own gameplay values

## 34.8 TextMeshPro Done When

- no UnityEngine.UI.Text in project
- UI uses TextMeshProUGUI
- world labels use TextMeshPro
- floating damage text uses TMP prefab

# 35. Prototype Technical Roadmap

## Phase 1 — Foundation

- project folder structure
- VContainer bootstrap
- GameStateMachine
- ConfigDatabase
- basic save model
- TextMeshPro setup

## Phase 2 — Global Prototype

- test planet cells
- biomes
- configured factions
- influence coloring
- player global position
- paused global time when idle

## Phase 3 — Battle Prototype

- BattleScene
- Unity Tilemap generator
- BattleTile[]
- Morpeh BattleWorld
- spawn 1000 dummy bots
- simple movement
- FPS/debug overlay

## Phase 4 — Combat MVP

- UnitConfig
- WeaponConfig
- ArmorConfig
- DamageSystem
- automatic rifle
- sword + shield
- artillery parabolic projectile
- tank cannon

## Phase 5 — Player MVP

- player-controlled battle entity
- inventory
- equipment slots
- weapon switching
- enter/exit tank

## Phase 6 — Economy MVP

- Credits
- battle rewards
- trade goods
- sell loot

## Phase 7 — Global-to-Battle Loop

- encounter starts battle
- battle result returns to global map
- losses applied
- credits/loot applied
- faction influence updated

# 36. Final Architecture Summary

~~~
Unity:
- scenes
- prefabs
- Tilemap
- rendering

Morpeh:
- combat logic
- bots
- projectiles
- vehicles
- damage
- movement
- AI

Configs:
- gameplay values
- unit stats
- weapons
- armor
- economy

Prefabs:
- visuals
- size
- anchors
- DOTween settings
- TMP settings

DOTween:
- visual animation only

TextMeshPro:
- all text

Global Map:
- long-term simulation

Battle Map:
- temporary local simulation
~~~

Final rule:

~~~
No hardcode.
No per-bot Update.
No legacy Text.
No visual hierarchy creation from code.
No separate player combat logic.
~~~
