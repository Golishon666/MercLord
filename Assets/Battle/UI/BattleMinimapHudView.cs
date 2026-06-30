using System;
using MercLord.Battle.ECS.Components;
using MercLord.Battle.Generation;
using MercLord.Battle.Tiles;
using MercLord.Game.Configs;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace MercLord.Battle.UI
{
    public sealed class BattleMinimapHudView : MonoBehaviour
    {
        private const int TextureSize = 168;
        private const float PanelWidth = 220f;
        private const float PanelHeight = 232f;
        private const float LabelHeight = 22f;

        private static readonly Color32 EmptyColor = new Color32(19, 22, 25, 225);
        private static readonly Color32 GroundColor = new Color32(48, 69, 54, 255);
        private static readonly Color32 RoadColor = new Color32(96, 87, 62, 255);
        private static readonly Color32 ObstacleColor = new Color32(34, 38, 41, 255);
        private static readonly Color32 AttackerZoneColor = new Color32(77, 168, 255, 72);
        private static readonly Color32 DefenderZoneColor = new Color32(255, 96, 78, 72);
        private static readonly Color32 ObjectiveZoneColor = new Color32(255, 204, 64, 96);
        private static readonly Color32 DangerZoneColor = new Color32(255, 68, 48, 112);
        private static readonly Color32 AllyPinColor = new Color32(72, 214, 164, 255);
        private static readonly Color32 EnemyPinColor = new Color32(255, 95, 88, 255);
        private static readonly Color32 NeutralPinColor = new Color32(230, 230, 230, 255);
        private static readonly Color32 PlayerPinColor = new Color32(255, 244, 128, 255);

        [SerializeField] private RawImage mapImage;
        [SerializeField] private TextMeshProUGUI objectiveLabel;
        [SerializeField] private TextMeshProUGUI countsLabel;
        [SerializeField] private Image backgroundImage;

        private readonly BattleMinimapHudPresenter presenter = new BattleMinimapHudPresenter();
        private BattleModel model;
        private Texture2D texture;
        private Color32[] pixels;

        public RawImage MapImage => mapImage;
        public TextMeshProUGUI ObjectiveLabel => objectiveLabel;
        public TextMeshProUGUI CountsLabel => countsLabel;

        public static BattleMinimapHudView CreateRuntime()
        {
            var canvasObject = new GameObject("Battle Minimap HUD", typeof(RectTransform));
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 104;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            var view = canvasObject.AddComponent<BattleMinimapHudView>();
            view.CreateDefaultLayout();
            return view;
        }

        public void Bind(BattleSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            EnsureLayout();
            EnsureTexture();
            model = session.Model;
            presenter.Bind(session);
            Refresh();
        }

        public void Refresh()
        {
            EnsureLayout();
            EnsureTexture();
            Render(presenter.BuildSnapshot());
        }

        public void Clear()
        {
            presenter.Dispose();
            model = null;
            ReleaseTexture();
        }

        private void OnDestroy()
        {
            presenter.Dispose();
            ReleaseTexture();
        }

        private void Render(BattleMinimapHudSnapshot snapshot)
        {
            if (!snapshot.IsValid || model == null)
            {
                ClearPixels(EmptyColor);
                ApplyTexture();
                objectiveLabel.text = "Objective: no battle";
                countsLabel.text = "Allies -  Enemies -";
                return;
            }

            DrawMap(snapshot);
            DrawSpawnZones(snapshot);
            DrawObjectives(snapshot);
            DrawDangerZones(snapshot);
            DrawPins(snapshot);
            ApplyTexture();

            objectiveLabel.text = snapshot.IsCompleted
                ? $"Result: {FormatOutcome(snapshot.Outcome)}"
                : FormatObjective(snapshot);
            countsLabel.text = FormatCounts(snapshot);
        }

        private void EnsureLayout()
        {
            if (mapImage != null &&
                objectiveLabel != null &&
                countsLabel != null)
            {
                return;
            }

            CreateDefaultLayout();
        }

        private void CreateDefaultLayout()
        {
            var rect = transform as RectTransform;
            if (rect == null)
            {
                rect = gameObject.AddComponent<RectTransform>();
            }

            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-12f, -12f);
            rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            backgroundImage ??= gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            backgroundImage.color = new Color(0.02f, 0.025f, 0.03f, 0.72f);
            backgroundImage.raycastTarget = false;

            mapImage ??= CreateMapImage();
            objectiveLabel ??= CreateLabel("Objective", -176f);
            countsLabel ??= CreateLabel("Battle Counts", -198f);
        }

        private RawImage CreateMapImage()
        {
            var imageObject = new GameObject("Minimap Image", typeof(RectTransform));
            imageObject.transform.SetParent(transform, false);
            var rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -10f);
            rect.sizeDelta = new Vector2(TextureSize, TextureSize);

            var image = imageObject.AddComponent<RawImage>();
            image.raycastTarget = false;
            image.color = Color.white;
            return image;
        }

        private TextMeshProUGUI CreateLabel(string labelName, float y)
        {
            var labelObject = new GameObject(labelName, typeof(RectTransform));
            labelObject.transform.SetParent(transform, false);
            var rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(PanelWidth - 20f, LabelHeight);

            var label = labelObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = 15f;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return label;
        }

        private void EnsureTexture()
        {
            if (texture != null && pixels != null)
            {
                return;
            }

            texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            pixels = new Color32[TextureSize * TextureSize];
            if (mapImage != null)
            {
                mapImage.texture = texture;
            }
        }

        private void ReleaseTexture()
        {
            if (texture != null)
            {
                Destroy(texture);
            }

            texture = null;
            pixels = null;
            if (mapImage != null)
            {
                mapImage.texture = null;
            }
        }

        private void DrawMap(BattleMinimapHudSnapshot snapshot)
        {
            if (model.Tiles == null || model.Tiles.Length != snapshot.MapWidth * snapshot.MapHeight)
            {
                ClearPixels(EmptyColor);
                return;
            }

            for (var y = 0; y < TextureSize; y++)
            {
                var tileY = math.clamp(y * snapshot.MapHeight / TextureSize, 0, snapshot.MapHeight - 1);
                for (var x = 0; x < TextureSize; x++)
                {
                    var tileX = math.clamp(x * snapshot.MapWidth / TextureSize, 0, snapshot.MapWidth - 1);
                    var tile = model.Tiles[tileY * snapshot.MapWidth + tileX];
                    pixels[y * TextureSize + x] = GetTileColor(tile);
                }
            }
        }

        private void DrawSpawnZones(BattleMinimapHudSnapshot snapshot)
        {
            if (model.SpawnZones == null)
            {
                return;
            }

            for (var index = 0; index < model.SpawnZones.Length; index++)
            {
                var zone = model.SpawnZones[index];
                var color = zone.Side == BattleSpawnSide.Attacker ? AttackerZoneColor : DefenderZoneColor;
                var minX = MapXToPixel(zone.Area.xMin, snapshot.MapWidth);
                var maxX = MapXToPixel(zone.Area.xMax, snapshot.MapWidth);
                var minY = MapYToPixel(zone.Area.yMin, snapshot.MapHeight);
                var maxY = MapYToPixel(zone.Area.yMax, snapshot.MapHeight);
                FillRect(minX, minY, maxX, maxY, color);
            }
        }

        private void DrawObjectives(BattleMinimapHudSnapshot snapshot)
        {
            if (model.Objectives == null)
            {
                return;
            }

            for (var index = 0; index < model.Objectives.Length; index++)
            {
                var objective = model.Objectives[index];
                var minX = MapXToPixel(objective.Area.xMin, snapshot.MapWidth);
                var maxX = MapXToPixel(objective.Area.xMax, snapshot.MapWidth);
                var minY = MapYToPixel(objective.Area.yMin, snapshot.MapHeight);
                var maxY = MapYToPixel(objective.Area.yMax, snapshot.MapHeight);
                FillRect(minX, minY, maxX, maxY, ObjectiveZoneColor);
            }
        }

        private void DrawDangerZones(BattleMinimapHudSnapshot snapshot)
        {
            for (var index = 0; index < snapshot.DangerZones.Count; index++)
            {
                var zone = snapshot.DangerZones[index];
                var centerX = MapXToPixel(zone.Position.x, snapshot.MapWidth);
                var centerY = MapYToPixel(zone.Position.y, snapshot.MapHeight);
                var radiusX = RadiusToPixels(zone.Radius, snapshot.MapWidth);
                var radiusY = RadiusToPixels(zone.Radius, snapshot.MapHeight);
                var alphaScale = Mathf.Lerp(0.35f, 1f, zone.RemainingFraction);
                var color = new Color32(
                    DangerZoneColor.r,
                    DangerZoneColor.g,
                    DangerZoneColor.b,
                    (byte)Mathf.Clamp(Mathf.RoundToInt(DangerZoneColor.a * alphaScale), 24, 160));
                FillEllipse(centerX, centerY, radiusX, radiusY, color);
            }
        }

        private void DrawPins(BattleMinimapHudSnapshot snapshot)
        {
            for (var index = 0; index < snapshot.Pins.Count; index++)
            {
                var pin = snapshot.Pins[index];
                var x = MapXToPixel(pin.Position.x, snapshot.MapWidth);
                var y = MapYToPixel(pin.Position.y, snapshot.MapHeight);
                var radius = pin.IsPlayer ? 2 : 1;
                DrawPin(x, y, radius, GetPinColor(pin, snapshot));
            }
        }

        private void DrawPin(int centerX, int centerY, int radius, Color32 color)
        {
            for (var y = centerY - radius; y <= centerY + radius; y++)
            {
                for (var x = centerX - radius; x <= centerX + radius; x++)
                {
                    if (!IsTextureCoordinate(x, y))
                    {
                        continue;
                    }

                    pixels[y * TextureSize + x] = color;
                }
            }
        }

        private void FillEllipse(int centerX, int centerY, int radiusX, int radiusY, Color32 color)
        {
            radiusX = Mathf.Max(1, radiusX);
            radiusY = Mathf.Max(1, radiusY);
            var radiusXSquared = radiusX * radiusX;
            var radiusYSquared = radiusY * radiusY;
            var ellipseLimit = radiusXSquared * radiusYSquared;

            for (var y = centerY - radiusY; y <= centerY + radiusY; y++)
            {
                for (var x = centerX - radiusX; x <= centerX + radiusX; x++)
                {
                    if (!IsTextureCoordinate(x, y))
                    {
                        continue;
                    }

                    var dx = x - centerX;
                    var dy = y - centerY;
                    if (dx * dx * radiusYSquared + dy * dy * radiusXSquared > ellipseLimit)
                    {
                        continue;
                    }

                    var pixelIndex = y * TextureSize + x;
                    pixels[pixelIndex] = Blend(pixels[pixelIndex], color);
                }
            }
        }

        private void FillRect(int minX, int minY, int maxX, int maxY, Color32 color)
        {
            minX = math.clamp(minX, 0, TextureSize - 1);
            maxX = math.clamp(maxX, 0, TextureSize - 1);
            minY = math.clamp(minY, 0, TextureSize - 1);
            maxY = math.clamp(maxY, 0, TextureSize - 1);

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var pixelIndex = y * TextureSize + x;
                    pixels[pixelIndex] = Blend(pixels[pixelIndex], color);
                }
            }
        }

        private void ClearPixels(Color32 color)
        {
            for (var index = 0; index < pixels.Length; index++)
            {
                pixels[index] = color;
            }
        }

        private void ApplyTexture()
        {
            texture.SetPixels32(pixels);
            texture.Apply(false);
            mapImage.texture = texture;
        }

        private static Color32 GetTileColor(BattleTile tile)
        {
            if (!tile.Walkable || tile.Surface == BattleTileSurface.Obstacle)
            {
                return ObstacleColor;
            }

            if (tile.Surface == BattleTileSurface.Road)
            {
                return RoadColor;
            }

            return tile.Cover == CoverType.None ? GroundColor : new Color32(55, 88, 62, 255);
        }

        private static Color32 GetPinColor(BattleMinimapHudPin pin, BattleMinimapHudSnapshot snapshot)
        {
            if (pin.IsPlayer)
            {
                return PlayerPinColor;
            }

            if (!snapshot.HasPlayer)
            {
                return pin.Team == BattleTeamType.Attacker ? AllyPinColor : EnemyPinColor;
            }

            if (pin.Team == snapshot.PlayerTeam)
            {
                return AllyPinColor;
            }

            return pin.Team == BattleTeamType.Attacker || pin.Team == BattleTeamType.Defender
                ? EnemyPinColor
                : NeutralPinColor;
        }

        private static string FormatCounts(BattleMinimapHudSnapshot snapshot)
        {
            if (!snapshot.HasPlayer)
            {
                return $"Attackers {snapshot.AttackerAlive}  Defenders {snapshot.DefenderAlive}";
            }

            var allies = snapshot.PlayerTeam == BattleTeamType.Attacker
                ? snapshot.AttackerAlive
                : snapshot.DefenderAlive;
            var enemies = snapshot.PlayerTeam == BattleTeamType.Attacker
                ? snapshot.DefenderAlive
                : snapshot.AttackerAlive;
            return $"Allies {allies}  Enemies {enemies}";
        }

        private static string FormatObjective(BattleMinimapHudSnapshot snapshot)
        {
            if (snapshot.ObjectiveCount <= 0)
            {
                return "Objective: defeat enemies";
            }

            if (snapshot.IsObjectiveContested)
            {
                return "Objective: contested";
            }

            if (!snapshot.HasObjectiveCaptureTeam)
            {
                return snapshot.ObjectiveCount == 1
                    ? "Objective: control point"
                    : $"Objective: control points x{snapshot.ObjectiveCount}";
            }

            var team = snapshot.ObjectiveCaptureTeam == BattleTeamType.Attacker
                ? "attackers"
                : "defenders";
            var progress = Mathf.RoundToInt(snapshot.ObjectiveCaptureProgress * 100f);
            return $"Objective: {team} {progress}%";
        }

        private static string FormatOutcome(BattleOutcome outcome)
        {
            switch (outcome)
            {
                case BattleOutcome.AttackerVictory:
                    return "Attacker victory";
                case BattleOutcome.DefenderVictory:
                    return "Defender victory";
                case BattleOutcome.Retreat:
                    return "Retreat";
                default:
                    return "None";
            }
        }

        private static int MapXToPixel(float x, int mapWidth)
        {
            if (mapWidth <= 1)
            {
                return 0;
            }

            return math.clamp((int)math.round(x / (mapWidth - 1) * (TextureSize - 1)), 0, TextureSize - 1);
        }

        private static int MapYToPixel(float y, int mapHeight)
        {
            if (mapHeight <= 1)
            {
                return 0;
            }

            return math.clamp((int)math.round(y / (mapHeight - 1) * (TextureSize - 1)), 0, TextureSize - 1);
        }

        private static int RadiusToPixels(float radius, int mapSize)
        {
            if (mapSize <= 1 || radius <= 0f)
            {
                return 1;
            }

            return math.clamp((int)math.ceil(radius / (mapSize - 1) * (TextureSize - 1)), 1, TextureSize - 1);
        }

        private static bool IsTextureCoordinate(int x, int y)
        {
            return x >= 0 && y >= 0 && x < TextureSize && y < TextureSize;
        }

        private static Color32 Blend(Color32 baseColor, Color32 overlay)
        {
            var alpha = overlay.a / 255f;
            return new Color32(
                (byte)Mathf.RoundToInt(baseColor.r * (1f - alpha) + overlay.r * alpha),
                (byte)Mathf.RoundToInt(baseColor.g * (1f - alpha) + overlay.g * alpha),
                (byte)Mathf.RoundToInt(baseColor.b * (1f - alpha) + overlay.b * alpha),
                255);
        }
    }
}
