---
name: fusion2-shared-mode
description: Patrones correctos de Photon Fusion 2 en Shared Mode (NetworkBehaviour, Networked, Spawned, autoridad, RPCs). Úsala al escribir o cambiar cualquier código de red.
---

# Photon Fusion 2 — Shared Mode

Fusion 2 es más nuevo que casi todo lo que sabes de memoria. **No inventes API.**

## Regla principal

Antes de usar una clase, atributo o método de Fusion, búscalo en el código fuente del SDK dentro del proyecto (`Assets/Photon/Fusion/`). Si no lo encuentras ahí, no lo uses: pregúntame o pídeme la documentación.

## Modelo de este proyecto

- Shared Mode: la sesión corre en la nube de Photon, sin abrir puertos.
- Cada objeto de red tiene un jugador con **State Authority**. Solo ese jugador escribe su estado.
- El jugador que crea la sala controla el fantasma (IA/NavMesh), el reloj de la noche, el sorteo del tipo de fantasma, las evidencias y el estado del ritual.
- Si ese jugador se va, la autoridad pasa a otro jugador. Todo lo que dependa de la autoridad debe poder retomarse desde el estado ya sincronizado, no desde variables locales.

## Patrones

- Scripts de red heredan de `NetworkBehaviour`, no de `MonoBehaviour`.
- Estado sincronizado: propiedades `[Networked]`. Se escriben solo si `HasStateAuthority`.
- Inicialización de red en `Spawned()`, no en `Awake()`/`Start()`.
- Lógica que debe ser determinista y de red: `FixedUpdateNetwork()`.
- Cambios de estado que otros deben reaccionar: usa un callback de cambio en la propiedad, o un RPC si es un evento puntual.
- RPCs: pocos y con un propósito claro. Indica quién los envía y quién los recibe.
- Un objeto que cualquiera puede tocar (puertas, objetos recogidos, diario): el jugador que interactúa pide la autoridad o envía un RPC a quien la tiene. Elige un patrón y úsalo en todo el proyecto.

## Qué es local y qué es de red

- **De red:** posición y animación de jugadores, linterna, estado de la cámara de cada jugador, puertas, luces, objetos recogidos, evidencias, diario, hora del juego, posición del fantasma, estado del ritual.
- **Local:** la capa fantasma (se renderiza en cada cliente según su cámara; el creador de la sala solo dice dónde está el fantasma), postprocesado, audio de interfaz, overlay de la camcorder.

## Configuración que debes recordarme

- `Project Settings > Editor > Asset Serialization` en **Force Text**.
- App ID de Fusion 2 en el panel de Photon, pegado en `PhotonAppSettings` (lo hago yo).
- Jugabilidad objetivo con hasta 150 ms de latencia.

## Al terminar una tarea de red

Dime cómo probarla con dos instancias en la misma PC, y qué debería verse en cada una.
