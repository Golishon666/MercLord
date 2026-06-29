# MercLord Project Base

This folder follows `docs/Architecture Document.md`.

Core rules:

- Gameplay values live in ScriptableObject configs.
- Prefabs own visuals, hierarchy, anchors, sorting, and tween settings.
- Global simulation only advances when global time is active.
- Battle logic belongs in ECS/data systems, not per-unit MonoBehaviour `Update`.
- DOTween is visual-only.
- Text must use TextMeshPro when UI/world text is added.

Initial foundation:

- `Bootstrap`: VContainer lifetime scope and startup entry point.
- `Game`: state machine, configs, save model, scene services.
- `Global`: world data and global time.
- `Battle`: battle request/result/tile data.
- `Economy`: credits service.
- `Player`: inventory/equipment/profile data.
- `Infrastructure`: validation/debug/pooling/service helpers.
