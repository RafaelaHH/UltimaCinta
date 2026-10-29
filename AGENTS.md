# Última Cinta — Reglas para Codex

Juego cooperativo de terror 3D en primera persona. El jugador ve el mundo a través de una camcorder; el fantasma solo se ve por el lente. De 2 a 4 jugadores, una sola sesión larga (~3 h), una mansión.

## Stack

- Unity 2022.3.62f2 LTS, URP, Input System
- C#
- Photon Fusion 2 en **Shared Mode**
- Windows (PC)
- Repo: https://github.com/RafaelaHH/UltimaCinta.git
- Carpeta local: `D:\2026-II\REALIDAD VIRTUAL\ULTIMACINTA\My project` (la raíz del repo es la raíz del proyecto Unity; la ruta tiene espacios, entrecomilla siempre las rutas)

## Fuente de verdad

El GDD está en `Docs/`. Léelo antes de empezar cualquier fase. Si algo del GDD y de esta guía se contradicen, pregúntame.

## Reglas de trabajo

1. Trabaja **una fase a la vez**, en el orden del checklist del GDD. No avances a la siguiente hasta que yo haya probado la actual y te lo confirme.
2. No hagas nada fuera de la fase actual. No añadas funciones, paquetes ni refactors que no se pidieron.
3. Clases pequeñas y claras. Datos en ScriptableObjects (fantasmas, evidencias, objetos de ritual).
4. **Nunca pongas números de balance en el código.** Todos los tiempos y valores (reloj, batería, cinta, cordura, duración de cacerías) viven en `GameConfig`.
5. El jugador que crea la sala tiene la autoridad (State Authority de Fusion) sobre el fantasma y el reloj de la noche.
6. No inventes API de Fusion 2. Verifica cada función en el código fuente de `Assets/Photon/` o pregúntame.
7. No puedes abrir el Editor de Unity. Antes de entregar, compila (skill `unity-compile-check`). Los pasos que me tocan a mí en el Editor van numerados (skill `unity-editor-handoff`).
8. Después de cada tarea: qué archivos creaste o modificaste y cómo probarlo.
9. Un commit por tarea o fase terminada, con mensaje claro en español. Push a `origin` al cerrar cada fase.

## Skills del proyecto

Están en `.agents/skills/`. Úsalas cuando apliquen:

- `phase-workflow`: ciclo de cada fase.
- `unity-compile-check`: compilar por consola y leer errores.
- `unity-editor-handoff`: prefabs y escenas por script, pasos manuales para mí.
- `unity-csharp-conventions`: estilo y estructura del código.
- `fusion2-shared-mode`: patrones de red correctos.
- `ultima-cinta-design`: resumen de diseño (fantasmas, evidencias, camcorder, reloj).
