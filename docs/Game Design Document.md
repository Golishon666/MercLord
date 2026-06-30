# GAME DESIGN DOCUMENT

## Project Codename

Dieselpunk 2D Warband

## Жанр

2D sandbox / strategy RPG / party-based war game в духе Mount & Blade: Bannerlord, но с локальными 2D боями, глобальной картой-планетой, фракциями, армиями, лутом, техникой и управляемым персонажем игрока.

# 1. Core Vision

Игрок начинает один, выбирает народность/культуру, появляется на глобальной карте-планете и постепенно строит свою силу: сражается, собирает лут, продаёт трофеи, покупает снаряжение, нанимает юнитов, участвует в войнах фракций и влияет на политическую карту мира.

Главная структура игры:

~~~
Глобальная карта
→ передвижение игрока и армий
→ встреча с врагом / событием / городом
→ локальная битва
→ результат боя
→ лут / кредиты / потери
→ изменение глобального мира
~~~

# 2. Core Pillars

## 2.1 Bannerlord-like Global Simulation

Глобальная карта работает как стратегический слой.

Ключевая особенность:

~~~
Если игрок стоит и не ждёт — весь мир стоит.
Если игрок двигается или включает ожидание — мир симулируется.
~~~

Это создаёт ощущение настольной стратегической карты: игрок сам управляет течением времени.

## 2.2 Local Battles with Many Units

Бои происходят на отдельной локальной карте.

Особенности:

- до 1000 ботов
- Unity Tilemap
- пехота
- мили-пехота
- артиллерия
- техника
- игрок управляет персонажем вручную
- игрок может управлять техникой

## 2.3 Data-Driven Units

Юниты не захардкожены.

Каждый юнит каждой фракции собирается через конфиги:

~~~
UnitConfig
→ WeaponConfig
→ ArmorConfig
→ AIConfig
→ Visual/Prefab Config
~~~

Один и тот же тип юнита может быть разным у разных фракций.

## 2.4 Modular Visual Infantry

Пехота собирается из частей:

~~~
Body
Head
Weapon
Shield
Backpack / Equipment
~~~

Минимум для MVP:

`Body + Head + Weapon`

Тело и голова имеют 4 направления:

~~~
Front
Back
Left
Right
~~~

## 2.5 Shared Combat

Игрок, AI-боты, техника, артиллерия и снаряды используют одну общую боевую систему.

Разница только в источнике команды:

~~~
Игрок → input
Бот → AI
Техника → driver input или AI
~~~

А исполнение атаки, урона, брони, снарядов и эффектов общее.

# 3. Setting and Visual Style

## 3.1 Setting

Сеттинг: grimdark dieselpunk / industrial sci-fi / demon-apocalypse.

Мир должен ощущаться как мрачная индустриальная планета после долгой войны, демонического заражения и техногенного распада.

## 3.2 Visual Style

Основной стиль:

- 2D stylized hand-painted
- top-down / pseudo top-down
- крупные читаемые силуэты
- тёмная палитра
- ржавый металл
- грязь
- масло
- индустриальные детали
- красные emissive-акценты
- мультяшная, но мрачная форма

Для пехоты визуальный референс:

- массивный солдат
- тёмный костюм/плащ
- ржаво-оранжевые бронепластины
- ремни и подсумки
- рюкзак/техмодуль
- чёрный шлем
- красный визор
- толстый outline
- читаемый силуэт

# 4. Main Game Loop

## 4.1 Global Loop

1. Игрок на глобальной карте.
2. Игрок выбирает направление движения.
3. Пока игрок движется, мир симулируется.
4. Армии фракций тоже движутся.
5. Игрок встречает врага, город, событие или караван.
6. Игрок выбирает действие.
7. При боевом столкновении загружается локальная битва.
8. После боя игрок получает результат, лут и кредиты.
9. Глобальный мир обновляется.

## 4.2 Battle Loop

1. Создать запрос генерации боя из глобальной соты.
2. Загрузить BattleScene.
3. Сгенерировать Tilemap.
4. Создать BattleTile[] для логики.
5. Создать BattleWorld.
6. Заспавнить игрока, союзников, врагов, технику.
7. Запустить бой.
8. Определить победителя.
9. Выдать BattleResult.
10. Вернуться на GlobalScene.
11. Применить потери, лут, кредиты и влияние.

# 5. Global Map Design

## 5.1 Global Map Structure

Глобальная карта — планета в виде шара, разбитая на соты.

Соты являются логическими клетками мира.

Важно:

~~~
Идеально покрыть шар только шестиугольниками нельзя.
Сферическая hex-like сетка будет содержать в основном hex-соты и несколько pentagon-сот.
~~~

Это нормально и должно учитываться в соседях сот.

## 5.2 Cell Data

Каждая сота хранит:

- id
- biome
- height
- moisture
- temperature
- owner faction
- dominant faction
- faction influence slots
- settlement id
- has road
- passability
- danger/corruption later

## 5.2.1 Settlements, City Levels, And District Footprint

Поселение на глобальной карте может занимать одну или несколько сот. На дальнем масштабе оно читается как marker icon, а после приближения - как объемная texture feature на поверхности карты.

У города есть 5 уровней:

| Level | Cells | Meaning |
| --- | ---: | --- |
| 1 | 1 | village / small settlement |
| 2 | 3 | town |
| 3 | 5 | city |
| 4 | 7 | large city |
| 5 | 9 | capital city |

Правила столицы:

- только столица фракции может быть level 5;
- у каждой фракции может быть только одна столица level 5;
- если столица потеряна или перенесена, level 5 должен быть переоценен вместе с новым capital cell.

Footprint города строится как compact area от центральной соты:

- центральная сота хранит city core;
- соседние валидные соты становятся районами;
- расширение выбирает ближайшие доступные соты вокруг центра, пока не набран размер уровня;
- город не может расширяться в `Ocean`, `Coast` / мелководье, непроходимые соты и недоступные горы;
- если вокруг центра не хватает валидных сот, город должен остаться меньшего уровня или выбрать другой центр.

Районы вокруг центра:

- residential
- industrial
- farming
- military
- science
- trade
- administrative
- port-adjacent
- mining

Районы должны отражать окружение. Port-adjacent район допустим только рядом с водой, mining - рядом с горами или богатыми ресурсами, farming - на плодородных и проходных сотах.

## 5.3 Biomes

Биомы:

~~~
Ocean
Coast
Plains
Forest
Desert
Snow
Swamp
Mountains
AshWastes
RustDesert
DeadForest
IndustrialRuins
DemonScar
ToxicSwamp
~~~

Биом влияет на:

- цвет глобальной соты
- тип локальной карты
- тайлы локальной карты
- препятствия
- декор
- лут later
- опасность later

## 5.4 Factions

Активные фракции задаются конфигами.

Фракция имеет:

- id
- name
- color
- credits
- capital
- settlements
- armies
- strength
- relations

## 5.5 Faction Influence

Каждая сота хранит влияние активных фракций через слоты `Influence4`.
Текущая модель поддерживает до 4 активных фракций.

~~~
Influence4:
- F0
- F1
- F2
- F3
~~~

Доминирующая фракция — та, у которой максимальное влияние.

Визуально сота окрашивается так:

~~~
base biome color
+ overlay dominant faction color
+ overlay strength based on influence
~~~

Биом должен оставаться читаемым, фракционный цвет — быть политическим слоем поверх.

## 5.6 Influence Sources

Источники влияния:

- столица
- город
- крепость
- армия
- победа в бою
- захват соты
- дороги
- события
- квесты later

Обновлять влияние не каждый кадр.

Влияние обновляется:

- после боя
- после захвата города
- при смене владельца соты
- раз в игровой день
- после крупного события

## 5.7 Zoom-In Map Points

Глобальная карта использует два визуальных режима для городов, поселений и активностей:

- far zoom - компактные readable icons;
- near zoom - объемные texture features на сотах карты.

После приближения far zoom icons городов и активностей исчезают плавным fade-out, а near zoom feature textures появляются fade-in. Ближние текстуры должны выглядеть как физические места на карте: здания, районы, лагеря, шахты, руины, торговые точки и другие активности.

Для городов near zoom texture должна учитывать уровень:

- level 1 занимает одну соту и выглядит как компактное поселение;
- level 2 занимает три соты и показывает маленький город с центром и двумя районами;
- level 3 занимает пять сот и показывает полноценный город;
- level 4 занимает семь сот и показывает крупный город с несколькими специализированными районами;
- level 5 занимает девять сот и показывает столицу с центральным городом и выраженными районами вокруг.

Полные prompts для генерации far zoom icons и near zoom feature textures находятся в `docs/Global Map Art Prompts.md`.

# 6. Global Time Design

## 6.1 Time Modes

Глобальное время имеет режимы:

~~~
Paused
PlayerMoving
Waiting
~~~

## 6.2 Paused

Мир стоит, если игрок:

- стоит на месте
- осматривает карту
- открыт инвентарь
- открыт город
- открыт UI
- выбирает действие

В этом режиме не обновляются:

- армии
- экономика
- фракционные войны
- распространение влияния
- события
- таймеры квестов

## 6.3 PlayerMoving

Мир движется, если игрок движется по глобальной карте.

Обновляются:

- позиция игрока
- армии фракций
- караваны later
- события
- столкновения

## 6.4 Waiting

Игрок может ждать/отдыхать.

В этом режиме глобальная симуляция идёт ускоренно.

# 7. Player Design

## 7.1 Start

Игрок начинает один, как в Bannerlord.

Перед стартом игрок выбирает народность/культуру.

Народность может давать:

~~~
MVP:
- стартовую соту
- стартовое оружие
- стартовую броню
- стартовое количество Credits

Later:
- отношения с фракциями
- бонусы к найму
- уникальные предметы
~~~

## 7.2 Player Actions on Global Map

Игрок может:

- двигаться по глобальной карте
- остановиться
- ждать
- войти в город
- атаковать армию
- торговать
- нанимать юнитов
- открыть инвентарь
- открыть отряд

## 7.3 Player Actions in Battle

Игрок может:

- бегать
- целиться мышью
- стрелять
- атаковать в ближнем бою
- переключать 4 оружейных слота
- использовать 4 спец-предмета
- входить в технику
- выходить из техники
- управлять танком

## 7.4 Player Equipment Slots

У игрока есть:

~~~
Armor:
- Body
- Helmet

Weapons:
- Weapon Slot 1
- Weapon Slot 2
- Weapon Slot 3
- Weapon Slot 4

Special:
- Special Slot 1
- Special Slot 2
- Special Slot 3
- Special Slot 4
~~~

## 7.5 Inventory

Игрок имеет инвентарь.

Инвентарь хранит:

- оружие
- броню
- шлемы
- спец-предметы
- расходники
- trade goods
- квестовые предметы later

# 8. Global Resource: Credits

## 8.1 Credits

Credits — основной глобальный ресурс/валюта.

Используется для:

- покупки оружия
- покупки брони
- покупки шлемов
- покупки спец-предметов
- найма юнитов
- ремонта техники
- торговли
- наград после боя
- наград за квесты later

## 8.2 MVP Credits Rules

Для MVP:

- игрок получает Credits после боя
- игрок продаёт лут за Credits
- игрок покупает базовые предметы за Credits
- цены одинаковые во всех поселениях

Not MVP:

- динамическая экономика
- спрос/предложение
- цены по регионам
- инфляция
- фракционные рынки

# 9. Armies and Unit Categories

## 9.1 Army Types

В игре есть следующие категории юнитов:

~~~
Ranged Infantry
Melee Infantry
Artillery
Vehicles
~~~

## 9.2 Ranged Infantry

Стрелковая пехота.

MVP оружие:

`Automatic Rifle`

Поведение:

- держит дистанцию
- ищет цель
- стреляет очередями
- отступает при сильном сближении later

## 9.3 Melee Infantry

Мили-пехота.

MVP оружие:

`Sword + Shield`

Поведение:

- сближается с целью
- атакует в ближнем бою
- щит может снижать входящий урон спереди

## 9.4 Artillery

Артиллерия стреляет по параболе.

MVP оружие:

`Artillery Cannon`

Поведение:

- стоит на дистанции
- выбирает скопления врагов
- стреляет с задержкой
- снаряд летит по дуге
- при попадании создаёт Explosion AoE

## 9.5 Vehicles

Техника.

MVP техника:

`Tank`

Поведение:

- высокая броня
- медленнее пехоты
- стреляет пушкой
- может управляться AI
- может управляться игроком

# 10. Damage and Armor

## 10.1 Damage Types

В игре 3 типа урона:

~~~
Ballistic
Energy
Explosion
~~~

## 10.2 Damage Type Meanings

~~~
Ballistic:
- пули
- мечи
- кинетические попадания
- осколки без AoE

Energy:
- лазеры
- плазма
- энергетическое оружие
- демоническая энергия

Explosion:
- артиллерия
- ракеты
- танковые снаряды
- гранаты
- AoE взрывы
~~~

## 10.3 Armor Protections

Броня защищает от тех же типов:

~~~
BallisticProtection
EnergyProtection
ExplosionProtection
~~~

## 10.4 MVP Damage Formula

Для MVP:

`finalDamage = max(1, incomingDamage - armorProtection)`

Позже можно добавить:

- пробитие
- проценты
- критический урон
- зоны попадания
- durability
- сопротивления эффектам

# 11. Weapons and Equipment MVP

## 11.1 MVP Weapons

Для MVP добавить:

~~~
Automatic Rifle
Sword + Shield
Artillery Cannon
Tank Cannon
~~~

## 11.2 Automatic Rifle

~~~
Damage Type: Ballistic
Role: Ranged Infantry
Mode: ranged
Projectile: simple projectile or hitscan-like projectile
~~~

## 11.3 Sword + Shield

~~~
Damage Type: Ballistic
Role: Melee Infantry
Mode: melee
Shield: reduces/block frontal damage later
~~~

## 11.4 Artillery Cannon

~~~
Damage Type: Explosion
Role: Artillery
Mode: parabolic projectile
AoE: yes
~~~

## 11.5 Tank Cannon

~~~
Damage Type: Explosion or Ballistic
Role: Vehicle
Mode: direct projectile
AoE: optional
~~~

# 12. Vehicle Gameplay

## 12.1 Vehicle States

Техника имеет состояния:

~~~
Empty
AIControlled
PlayerControlled
Destroyed
~~~

## 12.2 Player Vehicle Control

Игрок может войти в технику, если находится рядом.

Когда игрок входит:

- персонаж становится водителем
- input переключается на технику
- техника становится PlayerControlled

Когда игрок выходит:

- персонаж появляется рядом с техникой
- input возвращается персонажу
- техника становится Empty или AIControlled

## 12.3 MVP Vehicle Rules

Для MVP:

- только танк
- один водитель
- без пассажиров
- без экипажа
- enter/exit по кнопке

Not MVP:

- экипаж танка
- несколько мест
- повреждение модулей
- ремонт по частям
- топливо

# 13. Local Battle Map

## 13.1 Tilemap

Локальная карта создаётся через Unity Tilemap.

Слои:

~~~
GroundTilemap
RoadTilemap
ObstacleTilemap
DecorationTilemap
OverlayTilemap
DebugTilemap
~~~

## 13.2 Tilemap Responsibilities

Tilemap отвечает за визуал:

- земля
- дороги
- препятствия
- декор
- следы боя

Геймплейная логика использует отдельный BattleTile[].

## 13.3 BattleTile Data

BattleTile хранит:

- walkable
- move cost
- cover
- height

## 13.4 Local Map Generation From Global Cell

Локальная карта зависит от глобальной соты.

Пример:

~~~
Forest cell:
- grass
- dirt
- trees
- bushes

AshWastes:
- ash ground
- cracked soil
- wrecks
- smoke

IndustrialRuins:
- concrete
- metal
- pipes
- rubble

DemonScar:
- red cracks
- runes
- corrupted terrain
~~~

# 14. Loot After Battle

## 14.1 Loot Categories

После боя игрок получает продаваемый лут:

~~~
Technical Equipment
Scrap Metal
Weapon Parts
Armor Plates
Vehicle Parts
Ammunition Boxes
Energy Cores
Electronic Modules
Fuel Canisters
Rare Components
Faction Trophies
Artifacts
~~~

## 14.2 Loot Sources

~~~
Infantry defeated:
- weapon parts
- armor plates
- ammunition boxes
- faction trophies

Artillery defeated:
- shell casings
- explosive parts
- targeting modules
- metal debris

Vehicle destroyed:
- vehicle parts
- fuel canisters
- engine components
- armor plates
- rare components

Energy unit defeated:
- energy cores
- electronic modules
- unstable cells

Demon/corrupted unit defeated:
- artifacts
- corrupted metal
- rune fragments
~~~

## 14.3 MVP Loot Rules

Для MVP:

- после боя выпадает 1–3 loot entries за squad или группу врагов
- loot добавляется в инвентарь
- loot можно продать за Credits

# 15. Modular Infantry Visual Design

## 15.1 Infantry Structure

Пехота собирается из частей:

~~~
InfantryUnitView
├── Body
├── Head
├── Weapon
├── Shield
├── Backpack / Equipment
└── Selection Marker
~~~

Минимум для MVP:

~~~
Body
Head
Weapon
~~~

## 15.2 Direction Variants

Тело:

~~~
Body Front
Body Back
Body Left
Body Right
~~~

Голова:

~~~
Head Front
Head Back
Head Left
Head Right
~~~

Допустимо для MVP:

`Left and Right can use same sprite with flipX.`

## 15.3 Equipment Visual Changes

Экипировка влияет на визуал:

~~~
Body armor → changes body sprite set
Helmet → changes head sprite set
Active weapon → changes weapon sprite
Shield → changes shield sprite
Special item → can add backpack/equipment visual later
~~~

MVP:

- броня меняет тело
- шлем меняет голову
- активное оружие меняет weapon sprite

# 16. Animation Design

## 16.1 DOTween Rule

Все визуальные анимации через DOTween.

DOTween используется для:

- idle bob
- movement bob
- shoot recoil
- melee swing
- hit shake
- death collapse
- vehicle enter/exit
- UI animations
- floating combat text

DOTween не используется для:

- расчёта урона
- AI
- gameplay movement
- попаданий
- pathfinding
- состояния боя

## 16.2 Animation States

Юнит может иметь визуальные состояния:

~~~
Idle
Moving
Attacking
Hit
Dead
~~~

## 16.3 Performance Rule

Так как в бою может быть 1000 ботов:

- не запускать тяжёлые бесконечные tweens на всех ботах без необходимости
- анимации должны быть event-driven
- невидимые или далёкие юниты могут иметь упрощённую анимацию
- трупы переводятся в дешёвый режим

# 17. Input Design

## 17.1 Global Map Input

Глобальная карта:

~~~
Left click cell:
- выбрать точку движения

Right click:
- действие / attack / context action

Space:
- stop / pause

Wait key:
- ждать

Inventory key:
- открыть инвентарь

Party key:
- открыть отряд

Map interaction:
- выбрать армию
- выбрать город
- выбрать соту
~~~

## 17.2 Battle Input

Бой:

~~~
WASD:
- movement

Mouse:
- aim

Left mouse:
- attack / shoot

Right mouse:
- block / alternative action later

1–4:
- switch weapon slots

Special keys:
- use special items

E:
- interact / enter vehicle / exit vehicle

Tab:
- battle info

Esc:
- pause menu
~~~

# 18. Camera Design

## 18.1 Global Camera

Глобальная камера:

- вращение вокруг планеты
- zoom in/out
- выбор сот
- отображение иконок городов и армий
- переключение городов и активностей между far zoom icons и near zoom feature textures

Far zoom показывает иконки для быстрой стратегической читаемости. Near zoom скрывает иконки поселений и активностей и показывает объемные текстуры мест на сотах. Армии могут оставаться marker views, потому что они являются движущимися объектами, а не статическими точками карты.

## 18.2 Battle Camera

Боевая камера:

- следует за игроком
- ограничена границами карты
- имеет zoom
- может делать shake от взрывов

Камера не управляет боевой логикой.

# 19. Recruitment and Party

## 19.1 Player Starts Alone

Игрок начинает без армии.

## 19.2 Recruitment

Игрок может нанимать юнитов в поселениях.

Для MVP:

- нанять ranged infantry
- нанять melee infantry
- купить/получить artillery later
- купить/получить tank later

## 19.3 Player Party

PlayerParty хранит squads:

~~~
Squad:
- UnitConfig
- Count
~~~

При входе в бой squads превращаются в Battle entities.

# 20. Squad System

## 20.1 Squad Concept

Squad — группа одинаковых юнитов.

Squad хранит:

- unit config
- count
- faction/team
- order
- target

## 20.2 MVP Squad Order

Для MVP:

`Attack nearest enemy`

Later:

~~~
Hold
Move
FollowPlayer
Retreat
DefendPosition
FocusTarget
~~~

# 21. Trading

## 21.1 MVP Trading

Для MVP:

- игрок может продать TradeGood items
- игрок получает Credits
- цены одинаковые везде

## 21.2 Later Trading

Позже:

- цены зависят от поселения
- цены зависят от фракции
- дефицит влияет на цену
- разные регионы имеют разные рынки

# 22. Foundation Scope

## 22.1 Foundation Includes

- глобальная карта с тестовыми сотами
- настроенные фракции
- фракционное влияние
- выбор культуры
- игрок начинает один
- глобальное время как в Bannerlord
- Credits
- инвентарь
- слоты экипировки
- продаваемый лут после боя
- Unity Tilemap локальная карта
- Morpeh BattleWorld
- тест 1000 ботов
- ranged infantry with automatic rifle
- melee infantry with sword + shield
- artillery with parabolic shot
- tank
- 3 типа урона
- броня против 3 типов урона
- player control in battle
- enter/exit tank
- modular infantry body/head/weapon
- DOTween visual animations
- TextMeshPro text

## 22.2 Not MVP

- мультиплеер
- сложная дипломатия
- глубокая экономика
- квестовая система
- прокачка героя
- сложные формации
- сохранение во время боя
- разрушаемость карты
- экипаж техники
- динамическая погода
- сложный рынок
- полноценная симуляция населения
- сложный GOAP AI

# 23. Game Design Summary

~~~
Глобальная карта:
- планета из сот
- биомы
- настроенные фракции
- влияние
- пауза мира, когда игрок стоит

Локальная карта:
- Unity Tilemap
- отдельная BattleScene
- до 1000 ботов

Игрок:
- начинает один
- выбирает культуру
- управляет персонажем
- может управлять техникой
- имеет инвентарь и слоты

Бой:
- общая система оружия/урона/брони
- Ballistic / Energy / Explosion
- пехота / мили / артиллерия / техника

Экономика:
- Credits
- loot после боя
- продажа TradeGoods

Визуал:
- модульная пехота
- Body + Head + Weapon
- 4 направления
- DOTween анимации
~~~
