---
name: ultima-cinta-design
description: Resumen del diseño de Última Cinta (camcorder, fantasmas, evidencias, reloj, cordura, ritual). Úsala cuando necesites reglas de juego o valores y no quieras releer todo el GDD.
---

# Última Cinta — resumen de diseño

Fuente completa: el GDD en `Docs/`. Esto es un resumen; si hay diferencia, manda el GDD (y avísame).

## Idea

De 2 a 4 investigadores entran de noche a una mansión. El fantasma solo se ve a través de la camcorder. Reúnen evidencia, identifican el tipo de fantasma en el diario y lo expulsan con un ritual **antes del amanecer**. No hay combate: la tensión viene de batería, cinta y cordura.

## Camcorder

- Siempre grabando: la vista del jugador es la cámara. Overlay con luz REC, contador de tiempo, batería e icono de cinta.
- Capa Ghost: el fantasma, huellas y orbes solo los renderiza la cámara.
- Visión nocturna (N): gasta batería 3 veces más rápido. Batería ~12 min con ella, ~35 min sin ella.
- Cinta: 45 min por jugador. Se cambia en la camioneta.
- Zoom con la rueda del ratón. Foto de evidencia con clic izquierdo.
- Interferencia (estática, distorsión, eco) cuando el fantasma está cerca.

## Reloj

Noche de 22:00 a 06:00, ~2.7 min de juego por minuto real (~3 h). Si llega el amanecer sin expulsar al fantasma, pierden. Todo configurable en `GameConfig`.

Actos: 1 Llegada, 2 Planta baja, 3 Primer piso, 4 Identificación, 5 Ritual.

## Evidencias

A Aparición (2 s en cámara) · B EMF 5 · C Frío (bajo 0 °C) · D Escritura · E Orbes (en modo nocturno).

## Fantasmas (uno por partida, al azar)

| Fantasma | Evidencias | Comportamiento | Ritual |
| --- | --- | --- | --- |
| Ghoul | A, C, D | Lento, caza por sonido; agacharse y silencio lo despistan | Sal, campana, vela negra |
| Vampiro | B, C, E | Solo caza en oscuridad, drena batería; luz y ajo lo repelen | Ajo, estaca, espejo |
| Poltergeist | B, D, E | Lanza objetos, cacerías cortas y frecuentes; ordenar la sala lo debilita | Incienso, crucifijo, agua bendita |
| Espectro | A, B, E | Atraviesa paredes, se teletransporta junto a un jugador; la sal lo detiene un instante | Sal, amuleto, vela blanca |

Con 3 evidencias el tipo es único. Cada partida tiene sala favorita y agresividad aleatorias. Cacería: 45–90 s, luces apagadas, estática, el jugador atrapado muere.

## Cordura (0–100)

Baja con oscuridad, cercanía del fantasma y apariciones. Bajo 40: alucinaciones. Bajo 15: el fantasma te detecta desde más lejos. Se recupera con luz, compañía y pastillas (limitadas). Al morir: modo espectador, puedes parpadear luces.

## Ritual

3 objetos según el fantasma, llevados al círculo del sótano, velas encendidas, 3 oleadas de cacería cada vez más fuertes. Si el diario estaba bien: expulsión y victoria. Si no: el ritual falla y el fantasma se enfurece.

## Fuera de alcance en v1

Chat de voz, más de una mansión, más de 4 fantasmas, progresión, tienda, personalización, modificadores. No lo implementes.
