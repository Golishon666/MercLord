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
- PlanetRenderer
- GlobalCamera
- GlobalUI

## 3.3 BattleScene

Содержит:

- BattleLifetimeScope
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

## 4.2 EnterBattle Flow

1. GlobalMapState detects encounter.
2. Create BattleGenerationRequest.
3. Save current global context.
4. Enter EnterBattleState.
5. Load BattleScene.
6. Generate local map.
7. Create BattleWorld.
8. Spawn units/player/vehicles.
9. Switch to BattleState.

## 4.3 ExitBattle Flow

1. BattleResult is produced.
2. Stop BattleWorld.
3. Return all views to pools.
4. Unload BattleScene.
5. Load/activate GlobalScene.
6. Apply BattleResult to WorldModel.
7. Return to GlobalMapState.

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
- projectile speed
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
GlobalGenerationConfig
InputConfig optional
~~~

## 6.3 UnitConfig

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

## 6.4 WeaponConfig

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
    public float ExplosionRadius;
}
~~~

## 6.5 ArmorConfig

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

## 6.6 AIConfig

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

## 6.7 VehicleConfig

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
    public float F0;
    public float F1;
    public float F2;
    public float F3;
}
~~~

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

    public BattleArmyData Attacker;
    public BattleArmyData Defender;
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
4. Load BattleScene.
5. Generate Tilemap.
6. Generate BattleTile[].
7. Create Morpeh BattleWorld.
8. Convert squads into battle entities.
9. Spawn player entity.
10. Spawn vehicles/projectiles pools.
11. Start battle systems.

## 13.2 End Battle

1. Winner detected.
2. Create BattleResult.
3. Generate loot.
4. Add CreditsReward.
5. Stop BattleWorld.
6. Return views to pools.
7. Unload BattleScene.
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
    public int Value;
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

- MovementSystem can run every frame.
- AI systems must use staggered update.
- Target search must use Spatial Hash.
- No O(N²) scan every frame.
- No per-bot MonoBehaviour Update.
- ViewSyncSystem is the only system that updates transforms.
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

MVP formula:

`finalDamage = max(1, incomingDamage - protection)`

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

## 21.2 InfantryView

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

## 21.3 InfantryViewSettings

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

# 24. Input Architecture

## 24.1 Use Unity Input System

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
    public int WinnerFactionId;

    public bool PlayerSurvived;

    public int AttackerSurvivors;
    public int DefenderSurvivors;

    public int CreditsReward;
    public List<BattleLootEntry> Loot;
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

ArmorConfig invalid if:

- any protection < 0

## 32.2 Prefab Validation

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
- supports at least Plains, Forest, Desert, AshWastes, IndustrialRuins
- does not use GameObject per tile

## 34.3 BotSpawnSystem Done When

- spawns entities from UnitConfig
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

# 35. MVP Technical Roadmap

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
- 4 factions
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
