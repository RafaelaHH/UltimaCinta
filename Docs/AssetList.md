# Auditoría de modelos

Revisado en `Assets/Generated/`, `Assets/Prefabs/Generated/` y `Assets/Scenes/Mansion_Generated.unity` el 16/11/2026. La lista anterior decía “pendientes”, pero los FBX y elementos de escena ya existen.

| Requisito | Estado | Ubicación / uso |
|---|---|---|
| Ghoul | Disponible y asignado | `Assets/Generated/Fantasmas/Ghoul.fbx` → `Assets/Prefabs/Generated/Fantasmas/Ghoul.prefab` |
| Vampiro | Disponible y asignado | `Assets/Generated/Fantasmas/Vampiro.fbx` → `Assets/Prefabs/Generated/Fantasmas/Vampiro.prefab` |
| Poltergeist | Disponible y asignado | `Assets/Generated/Fantasmas/Poltergeist.fbx` → `Assets/Prefabs/Generated/Fantasmas/Poltergeist.prefab` |
| Espectro | Disponible y asignado | `Assets/Generated/Fantasmas/Espectro.fbx` → `Assets/Prefabs/Generated/Fantasmas/Espectro.prefab` |
| Sal, campana, vela negra, espejo roto y crucifijo | Disponibles | `Assets/Generated/Ritual/` |
| Ajo, estaca, incienso, agua bendita, amuleto y vela blanca | Disponibles y referenciados | `Assets/Generated/Herramientas/` y assets de `Assets/Scripts/Data/RitualItems/` |
| Camcorder, diario, libro de escritura, medidor EMF y termómetro | Disponibles y referenciados | `Assets/Generated/Herramientas/` y `Assets/Generated/KeyItems/` |
| Generador, armario, cama y camioneta | Disponibles y colocados | `Assets/Generated/Herramientas/` y `Mansion_Generated.unity` |
| Círculo de piedra y módulos de la mansión | Generados en escena | `Assets/Scenes/Mansion_Generated.unity`, por `Assets/Editor/MansionGenerator.cs` |
| Puertas con cerradura y mobiliario | Generados en escena | `Assets/Scenes/Mansion_Generated.unity` |

## Pendientes detectados

- No falta ningún modelo del inventario original por archivo.
- `Assets/Generated/Fantasmas/Fantasma.fbx` está sin asignar; parece un modelo genérico de respaldo.
- `Assets/Generated/Fantasmas/Nuevos/Ghoul.fbx` y `Vampiro.fbx` también están sin asignar. Los prefabs usan las versiones de la carpeta principal. Hay que comparar su aspecto en Unity antes de sustituirlos; el inventario solo puede confirmar que existen, no su calidad visual.
- La lista no confirma que cada mesh tenga materiales, escala, orientación y silueta correctas dentro del juego. Esa comprobación visual queda pendiente porque no se abre el Editor desde esta tarea.
