---
name: unity-csharp-conventions
description: Estilo y estructura del código C# del proyecto Última Cinta (carpetas, ScriptableObjects, GameConfig, rendimiento). Úsala al crear o modificar cualquier script.
---

# Convenciones de C# en Última Cinta

## Estructura de carpetas (dentro de `Assets/`)

```
Scripts/Player     Scripts/Ghost     Scripts/Network
Scripts/UI         Scripts/Data      Editor
Prefabs            Scenes
```

Un script por archivo, el nombre del archivo igual al de la clase.

## Código

- Namespace `UltimaCinta.<Carpeta>` (por ejemplo `UltimaCinta.Player`).
- Clases pequeñas con una responsabilidad. Si una clase pasa de ~250 líneas, sepárala.
- Campos para el Inspector: `[SerializeField] private`. Evita campos públicos.
- Nombres en inglés para código (clases, métodos, variables). Textos para el jugador en español.
- Comentarios solo cuando el motivo no es obvio. No expliques lo que el código ya dice.

## Datos

- Fantasmas, evidencias y objetos de ritual son ScriptableObjects en `Scripts/Data` (por ejemplo `GhostType`).
- `GameConfig` (ScriptableObject) guarda **todos** los tiempos y valores de balance. Nada de números mágicos en las clases de juego.
- Los tipos de evidencia y fantasma son enums o ScriptableObjects, no strings.

## Rendimiento

- No uses `Find*`, `GetComponent` ni `Camera.main` dentro de `Update`. Guarda la referencia en `Awake`/`Start`.
- No asignes memoria en `Update` (listas nuevas, strings concatenados, LINQ).
- Eventos (`System.Action` o UnityEvent) para comunicar sistemas, en vez de referencias cruzadas por todas partes.
- Desuscribe los eventos en `OnDisable`/`OnDestroy`.

## Input

Usa el Input System (acciones), no `Input.GetKey`. Teclas del GDD: E interactuar, N visión nocturna, rueda del ratón zoom, clic izquierdo foto de evidencia.

## Antes de entregar

- Compila con `unity-compile-check`.
- No dejes `Debug.Log` de depuración en el código final de la fase.
