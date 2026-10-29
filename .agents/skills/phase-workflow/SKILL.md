---
name: phase-workflow
description: Ciclo de trabajo por fases de Última Cinta (implementar, compilar, explicar cómo probar, esperar OK, commit y push). Úsala al empezar y al cerrar cada fase o tarea.
---

# Ciclo de cada fase

El checklist de fases está al final del GDD (Fase 0 a Fase 7). Se trabaja una a la vez.

## Al empezar una fase

1. Lee la fase en el GDD y confirma en una o dos líneas qué vas a construir.
2. Si algo es ambiguo y cambia el resultado, pregunta antes. Si no, avanza.
3. Revisa que `git status` esté limpio y que estés al día con `origin`.

## Durante la fase

1. Trabaja tarea por tarea, en el orden del checklist.
2. Después de cada tarea:
   - Compila con `unity-compile-check`.
   - Dime qué archivos creaste o modificaste.
   - Dime cómo probarlo (escena a abrir, qué hacer en Play, qué debería verse).
3. Commit pequeño por tarea, mensaje en español, por ejemplo: `Fase 1: visión nocturna con consumo de batería 3x`.

## Al cerrar la fase

1. Repasa el checklist de la fase y marca qué quedó hecho y qué no.
2. Revisa la línea **Prueba** de la fase y dime exactamente cómo ejecutarla.
3. Push a `origin`.
4. Resume en pocas líneas: qué funciona, qué falta, qué pasos manuales me tocan.
5. **Espera mi confirmación** antes de empezar la siguiente fase.

## No hagas

- Adelantarte a la siguiente fase "para ahorrar tiempo".
- Cambiar el diseño del GDD por tu cuenta. Si crees que algo debería cambiar, propónmelo.
- Dar una tarea por terminada sin compilar.
