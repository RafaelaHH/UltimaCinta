# PROGRESS — Última Cinta

Checklist transcrita de `Docs/Última Cinta — GDD y checklist de desarrollo.docx`. Se marcan tareas solo después de terminarlas y probarlas.

## Fase 0 — Preparación (días 1–2)

- [x] Crear PROGRESS.md en la raíz del proyecto con toda esta checklist, y marcar - [x] cada tarea al terminarla.
- [x] Crear proyecto Unity 6 con URP y Input System.
- [x] Instalar Photon Fusion 2, crear una app en el panel de Photon y activar el plan gratis (20 jugadores para desarrollo, 100 para publicar).
- [x] Estructura de carpetas: Scripts/Player, Scripts/Ghost, Scripts/Network, Scripts/UI, Scripts/Data, Prefabs, Scenes.
- [x] Crear GameConfig (ScriptableObject) con: duración real de la noche, velocidad del reloj, batería, cinta, cordura.
- [x] Escena Bootstrap → Lobby → Mansion.
  - Las tres escenas están creadas y registradas en ese orden. `SampleScene` se conserva y sigue primero en Build Settings.
- [x] Hacer la lista de assets que faltan (objetos, muebles, herramientas, fantasmas) con nombre y prioridad.
- [ ] Generar cada asset con Hunyuan3D en hy.3d.tencent.ai y guardarlo en la carpeta Assets/Generated/.
  - `Docs/AssetList.md` enumera 18 assets pendientes; no se generaron modelos durante esta tarea.
- [ ] Revisar los términos de uso de Hunyuan3D para juegos publicados antes de lanzar.
  - Pendiente de revisión.
- [x] Importar cada modelo a Unity, ajustar escala, pivote y colisionadores, y guardarlo como prefab.

## Fase 1 — Jugador y camcorder en solitario (días 3–7)

- [ ] Controlador en primera persona: caminar, correr (con stamina), agacharse, mirar con ratón.
- [ ] Cámara del jugador con overlay de camcorder: luz REC parpadeante, tiempo, batería, cinta.
- [ ] Postprocesado found footage: grano, viñeta, aberración cromática, ruido.
- [ ] Visión nocturna (tecla N) con consumo de batería 3×.
- [ ] Zoom con la rueda del ratón.
- [ ] Batería y cinta que se agotan, con avisos y apagado de la cámara a 0.
- [ ] Linterna independiente de la cámara.
- [ ] Sistema de interacción (tecla E): abrir puertas, recoger objetos, usar interruptores.
- [ ] Inventario simple de 3 ranuras más camcorder.
- [ ] Capa «Ghost» visible solo para la cámara del jugador (culling mask). Probar con un cubo de prueba.
- [ ] Prueba: recorrer una sala de prueba, ver el cubo solo por la cámara, gastar batería y cinta.

## Fase 2 — Mansión (días 6–12)

- [ ] Construir la mansión por bloques (modular) con paredes y pisos hechos con formas básicas de Unity (ProBuilder) y todos los objetos generados con Hunyuan3D: sótano, planta baja, primer piso, exterior con la camioneta.
- [ ] Iluminación baja, luces que se pueden encender/apagar, generador en el sótano.
- [ ] Puertas con llave y sistema de llaves que desbloquea áreas (en la Fase 4 se enlaza con los actos).
- [ ] Escondites (armarios y camas) que el jugador puede usar.
- [ ] Bake de NavMesh en toda la casa.
- [ ] Puntos de interés marcados: sala favorita candidata, puntos de objeto de ritual, puntos de evidencia.
- [ ] Prueba: recorrer toda la casa, que las puertas y llaves funcionen, que el NavMesh cubra todo.

## Fase 3 — Fantasma, evidencias y diario (días 10–17)

- [ ] ScriptableObject GhostType con: nombre, evidencias, velocidad, comportamiento, objetos de ritual.
- [ ] Crear los 4 fantasmas: Ghoul, Vampiro, Poltergeist, Espectro (modelos generados con Hunyuan3D; si salen sin esqueleto, se animan por código: flotar, arrastrarse, parpadear).
- [ ] IA base con estados: Deambular, Actividad, Cacería, Descanso.
- [ ] Elegir tipo de fantasma, sala favorita y agresividad al iniciar la partida.
- [ ] Comportamientos propios: Ghoul (oído), Vampiro (oscuridad y batería), Poltergeist (objetos), Espectro (paredes y teletransporte).
- [ ] Interferencia en la cámara según distancia al fantasma.
- [ ] Sistema de cacería: 45–90 s, luces que se apagan, estática en la cámara, jugador atrapado muere.
- [ ] Las 5 evidencias: aparición, EMF, termómetro, libro de escritura, orbes.
- [ ] Diario: marcar evidencias, filtrar fantasmas posibles, elegir uno (en local; se comparte en red en la Fase 5).
- [ ] Sistema de cordura con alucinaciones.
- [ ] Modo espectador al morir, con parpadeo de luces.
- [ ] Prueba: forzar cada tipo de fantasma, confirmar que las evidencias coinciden con la tabla del GDD.

## Fase 4 — Reloj y ritual (días 15–20)

- [ ] Reloj de la noche (22:00–06:00) configurable desde GameConfig.
- [ ] Cambio de actos según la hora, con desbloqueo de áreas.
- [ ] Objetos de ritual (3 por tipo de fantasma) colocados al azar en puntos válidos.
- [ ] Sala del ritual con el círculo: colocar objetos, encender velas.
- [ ] Ritual en 3 oleadas con cacerías cada vez más fuertes.
- [ ] Si el diario fue correcto: expulsión y victoria. Si no: el ritual falla y el fantasma se enfurece.
- [ ] Pantallas de victoria, derrota por amanecer y derrota por muerte total.
- [ ] Prueba: una partida completa solo, de principio a fin, con el reloj acelerado.

## Fase 5 — Multijugador (días 18–25)

- [ ] Pantalla de lobby: crear sala con un nombre y unirse por nombre de sala.
- [ ] Conexión con Photon Fusion 2 en Shared Mode; spawn de hasta 4 jugadores.
- [ ] Sincronizar jugadores: posición, animación, linterna, estado de cámara.
- [ ] Hacer que la computadora del creador de la sala calcule la IA del fantasma y el reloj (los demás reciben el resultado), y pasar ese cálculo a otro jugador si se desconecta.
- [ ] Sincronizar puertas, luces, objetos recogidos, evidencias y diario.
- [ ] Capa fantasma local por jugador según su cámara.
- [ ] Manejo de desconexiones: el fantasma sigue y el jugador queda como muerto.
- [ ] Bloquear el diario para que cualquiera pueda marcar pero se actualice para todos.
- [ ] Prueba: dos instancias en la misma PC, luego dos PCs distintas con Photon.

## Fase 6 — Audio, pulido y balance (días 24–29)

- [ ] Audio 3D: pasos, puertas, voz del fantasma, latidos según miedo (sonidos de terceros permitidos; guardar la licencia de cada uno).
- [ ] Sonido distinto por cada fantasma y por cada fase de cacería.
- [ ] Efectos visuales de la cámara según la interferencia.
- [ ] Menú principal, pausa y ajustes (volumen, sensibilidad, subtítulos).
- [ ] Tutorial corto en la camioneta: cómo usar cámara, diario y herramientas.
- [ ] Pasada de balance: probar 5 partidas completas y ajustar GameConfig hasta que dure 2.5–3.5 h.
- [ ] Corregir los errores más graves de la lista de pruebas.

## Fase 7 — Entrega (días 29–30)

- [ ] Build para Windows.
- [ ] Prueba final con 3–4 amigos en red real.
- [ ] Lista de errores conocidos y mejoras para la v2.

## Definición de «terminado»

- [ ] Se puede crear una sala, unirse por nombre de sala y jugar de 2 a 4 personas.
- [ ] Una partida completa dura entre 2.5 y 3.5 horas.
- [ ] Los 4 fantasmas se pueden identificar y expulsar correctamente.
- [ ] La camcorder es imprescindible: sin ella no se puede ver al fantasma.
- [ ] No hay errores que impidan terminar una partida.
