# MercLord Project Base

This folder follows `docs/Architecture Document.md`.

Core rules:

- Gameplay values live in ScriptableObject configs.
- Prefabs own visuals, hierarchy, anchors, sorting, and tween settings.
- No hardcoded gameplay or view values in runtime code.
- Runtime code instantiates configured prefabs through factories or pools; it does not build visual hierarchies by hand.
- If a new value is needed, add it to a config, prefab settings component, scene reference, save model, or input state first.
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
