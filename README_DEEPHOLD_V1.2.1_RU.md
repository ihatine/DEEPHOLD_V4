# DEEPHOLD v1.2.1

Исправлена ошибка компиляции `ResourceToolRequirement` в `Gameplay/ToolSystem.cs`.

Причина: перечисление `ResourceToolRequirement` находится в namespace `OutOfSync.World`, а `ToolSystem` находился в `OutOfSync.Gameplay` без соответствующего `using`.

Исправление:
`using OutOfSync.World;`

Также предупреждения Unity про старый Input Manager и Dynamic Batching сами по себе не блокируют Play Mode.

Unity: 6000.0.59f1
