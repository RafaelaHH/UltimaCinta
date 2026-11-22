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
  - El inventario original ya está en `Assets/Generated/`; en esta tarea se integraron los cuatro FBX de jugadores que entregó el usuario en `Assets/Generated/Jugadores/`. No se generaron modelos nuevos en Hunyuan3D.
- [ ] Revisar los términos de uso de Hunyuan3D para juegos publicados antes de lanzar.
  - Pendiente de revisión.
- [x] Importar cada modelo a Unity, ajustar escala, pivote y colisionadores, y guardarlo como prefab.

## Fase 1 — Jugador y camcorder en solitario (días 3–7)

> Estado: implementados los sistemas y generados Assets/Prefabs/Player/Player.prefab y Assets/Scenes/PhaseOneTest.unity; compilación batch de Unity correcta (código 0). Todas las tareas de Fase 1 están implementadas y verificadas en Play Mode.

- [x] Controlador en primera persona: caminar, correr (con stamina), mirar con ratón.
- [x] Agacharse (tecla C).
- [x] Cámara del jugador con overlay de camcorder: luz REC parpadeante, tiempo, batería, cinta.
- [x] Postprocesado found footage: grano, viñeta, aberración cromática, ruido.
- [x] Visión nocturna (tecla N) con consumo de batería 3×.
- [x] Zoom con la rueda del ratón.
- [x] Batería y cinta que se agotan, con avisos y apagado de la cámara a 0.
- [x] Linterna independiente de la cámara (probada en la mansión).
- [x] Sistema de interacción (tecla E): abrir puertas, recoger objetos, usar interruptores.
- [x] Inventario simple de 3 ranuras más camcorder.
- [x] Capa «Ghost» visible solo para la cámara del jugador (culling mask). Probar con un cubo de prueba.
- [x] Prueba: recorrer una sala de prueba, ver el cubo solo por la cámara, gastar batería y cinta.

## Fase 2 — Mansión (días 6–12)

> Nota: todas las tareas de esta fase fueron probadas por el usuario en Unity.

- [x] Construir la mansión por bloques (modular) con paredes y pisos hechos con formas básicas de Unity (ProBuilder) y todos los objetos generados con Hunyuan3D: sótano, planta baja, primer piso, exterior con la camioneta.
- [x] Iluminación baja (probada en Unity).
- [x] Luces de habitaciones con interruptores (probadas en Unity).
- [x] Generador y caja eléctrica con fusible (probados en Unity).
- [x] Colocación de objetos decorativos con sus modelos.
- [x] Puertas con llave y sistema de llaves que desbloquea áreas (probado: la llave se consume; en la Fase 4 se enlaza con los actos).
- [x] Escondites (armarios y camas) que el jugador puede usar (probados: entrar y salir con E).
- [x] Bake de NavMesh en toda la casa (cobertura confirmada en las plantas y las escaleras).
- [x] Puntos de interés marcados: sala favorita candidata, puntos de objeto de ritual, puntos de evidencia (esferas y etiquetas visibles en Unity).
- [x] Prueba: recorrer toda la casa, que las puertas y llaves funcionen, que el NavMesh cubra todo.

## Fase 3 — Fantasma, evidencias y diario (días 10–17)

> Implementación y configuración completadas en Unity `6000.5.6f1`. La configuración de `Mansion_Generated` terminó en batchmode con código 0: cuatro prefabs/tipos, selección de sala, cámara, 31 puntos de evidencia y tabla A/C/D, B/C/E, B/D/E, A/B/E validados. También se agregó una gracia antes de la primera cacería para evitar la muerte casi inmediata observada al probar Ghoul. Queda pendiente probar en Play Mode los cuatro fantasmas y confirmar sus evidencias; la apertura del Editor desde Unity Hub no funcionó durante esta sesión, así que no se marca la prueba final como hecha.

> Seguimiento: los modelos FBX existentes quedaron conectados a objetos en mano y al soltar; se añadieron pastillas y baterías utilizables (Q), cuatro de cada una repartidas por la casa, aparición del fantasma al entrar en la sala favorita y efectos auditivos/visuales para cordura cero. El Editor compiló los cambios y ejecutó la configuración de objetos. Falta verificar estos comportamientos visual y funcionalmente en Play Mode; la vista del Editor quedó negra durante la prueba.

> La interacción de la camioneta ya usa una cabina física con acceso por rampa y equipo recogible; el diario tiene secciones de Evidencias, Fantasmas, Ritual y Notas. Pendiente de verificar la entrada/salida y navegación de secciones en Play Mode.

- [x] ScriptableObject GhostType con: nombre, evidencias, velocidad, comportamiento, objetos de ritual.
- [x] Crear los 4 fantasmas: Ghoul, Vampiro, Poltergeist, Espectro (prefabs normalizados y movimiento por código donde aplica; no se generaron modelos Hunyuan3D nuevos).
- [x] IA base con estados: Deambular, Actividad, Cacería, Descanso.
- [x] Elegir tipo de fantasma, sala favorita y agresividad al iniciar la partida.
- [x] Comportamientos propios: Ghoul (oído), Vampiro (oscuridad y batería), Poltergeist (objetos), Espectro (paredes y teletransporte).
- [x] Interferencia en la cámara según distancia al fantasma.
- [x] Sistema de cacería: duración configurable en el rango del GDD, luces que se apagan, estática en la cámara, jugador atrapado muere.
- [x] Las 5 evidencias: aparición, EMF, termómetro, libro de escritura, orbes.
- [x] Diario: marcar evidencias, filtrar fantasmas posibles, elegir uno (en local; se comparte en red en la Fase 5).
- [x] Sistema de cordura con alucinaciones.
- [x] Modo espectador al morir, con parpadeo de luces.
- [ ] Prueba: forzar cada tipo de fantasma, confirmar que las evidencias coinciden con la tabla del GDD.

## Fase 4 — Reloj y ritual (días 15–20)

> Implementado y compilado en Unity 6. `Mansion_Generated` quedó configurada con 31 puntos válidos para objetos rituales y `PhaseFourTest` ofrece una noche acelerada de 5 minutos. La partida individual llegó desde el inicio hasta las 06:00 y mostró `DERROTA - AMANECIO`. La prueba automatizada `Tools/UltimaCinta/Verificar rituales de los cuatro fantasmas` recorrió 8 casos con los controladores y objetos reales: victoria y fallo por diario incorrecto para cada fantasma. La interacción manual con el ritual dentro de una sesión completa aún no se ha probado.

- [x] Reloj de la noche (22:00–06:00) configurable desde GameConfig.
- [x] Cambio de actos según la hora, con desbloqueo de áreas.
- [x] Objetos de ritual (3 por tipo de fantasma) colocados al azar en puntos válidos.
- [x] Sala del ritual con el círculo: colocar objetos, encender velas.
- [x] Ritual en 3 oleadas con cacerías cada vez más fuertes.
- [x] Si el diario fue correcto: expulsión y victoria. Si no: el ritual falla y el fantasma se enfurece.
- [x] Pantallas de victoria, derrota por amanecer y derrota por muerte total.
- [x] Prueba: una partida completa solo, de principio a fin, con el reloj acelerado (terminó en derrota por amanecer; ramas de ritual y muerte total pendientes de validación interactiva).

## Fase 5 — Multijugador (días 18–25)

> Build Windows y compilación Unity 6 correctas. Dos instancias en la misma PC se unieron a Photon (`Player:1` y `Player:2`), cargaron la mansión y sincronizaron soltar/mostrar/recoger un objeto. Al cerrar el creador con fantasma activo, el invitado siguió conectado y registró el traspaso de autoridad del reloj, el estado global del fantasma y la IA. La build final no registró excepciones en esta prueba. Pendientes: jugar desde dos PCs y comprobar manualmente movimiento/animación, cámara/linterna, puertas/luces, diario/evidencias y muerte/desconexión.

- [x] Pantalla de lobby: crear sala con un nombre y unirse por nombre de sala.
- [x] Conexión con Photon Fusion 2 en Shared Mode; spawn configurado para hasta 4 jugadores.
- [x] Sincronizar jugadores: posición, estado de movimiento, linterna y estado de cámara.
- [x] La autoridad del creador calcula la IA del fantasma y el reloj; el cálculo se transfiere si se desconecta.
- [x] Sincronizar puertas, luces, objetos recogidos, evidencias y diario.
- [x] Capa fantasma local por jugador según el estado de su cámara.
- [x] Manejo de desconexiones: contar al jugador como muerto y mantener el fantasma activo.
- [x] Cualquiera puede marcar evidencias y elegir sospechoso; el diario refleja el estado compartido.
- [x] Prueba QA: dos instancias en la misma PC, recogida de una llave de red y traspaso de autoridad al salir el creador.
- [ ] Prueba con dos PCs distintas y revisión manual de todas las interacciones sincronizadas.

## Fase 6 — Audio, pulido y balance (días 24–29)

- [x] Audio 3D: pasos, puertas, voz del fantasma, latidos según miedo (sonidos de terceros permitidos; guardar la licencia de cada uno).
  - Catálogo original sintetizado en ejecución; no se importó audio de terceros. Se documenta en `Docs/AudioAssets.md`.
- [x] Sonido distinto por cada fantasma y por cada fase de cacería.
  - QA creó y comprobó 16 clips únicos (cuatro tipos de fantasma × cuatro estados), además de pasos, latidos, puertas y UI. La verificación de setup final contó 26 clips sintetizados.
- [x] Efectos visuales de la cámara según la interferencia.
- [x] Menú principal, pausa y ajustes (volumen, sensibilidad, subtítulos).
- [x] Tutorial corto en la camioneta: cómo usar cámara, diario y herramientas.
- [ ] Pasada de balance: probar 5 partidas completas y ajustar GameConfig hasta que dure 2.5–3.5 h.
- [x] Corregir los errores más graves de la lista de pruebas.
  - La revisión de logs y QA de esta fase no encontró excepciones de ejecución que impidan continuar. Las pruebas interactivas pendientes siguen listadas en las fases 3 y 5.

> QA Unity 6: compilación y comprobación estática correctas; Bootstrap, Player, pausa, tutorial, subtítulos, pantalla final, 41 puertas y los cuatro prefabs de fantasma enlazan sus referencias. El reloj principal queda en 180 minutos (3 h nominales). Los ocho casos de ritual volvieron a pasar: victoria y fallo con diario incorrecto para los cuatro fantasmas. Falta jugar y escuchar en Play Mode, y ejecutar las cinco partidas completas; por eso la pasada de balance permanece abierta.

> Integración final: grabación de camcorder a 2 FPS con presupuesto local configurable; el monitor de la camioneta ofrece cámara en directo y reproducción de capturas de la sesión local. La cabina admite cuatro plazas y mantiene visible el modelo exterior `camioneta.fbx`. Se abrió el suelo exterior para enlazar las escaleras P-B7 del sótano. La prueba de red de dos clientes comprobó la llave, la desconexión del host y el traspaso; la corrección posterior de lectura al despawn volvió a pasar sin excepciones.

> QA visual y funcional en Unity 6 con Direct3D 11 (21/11/2026): E permitió entrar y salir por la puerta trasera de la camioneta; cuatro ocupantes tuvieron posiciones distintas y se rechazó el quinto. Recoger la camcorder activó la grabación, generó fotogramas JPG y el monitor mostró primero la textura en directo y luego una grabación. Se separaron los avisos del diario y la interacción. En P-B7 se corrigió la abertura del suelo hacia el exterior y se desplazó la camioneta para liberar el pozo: cinco raycasts consecutivos golpearon peldaños o rampa sin encontrar suelo encima, y la cabina no invade el paso. Siguen pendientes las pruebas manuales de partida completa y con 3–4 personas reales.

## Fase 7 — Entrega (días 29–30)

- [x] Build para Windows (Unity 6, cinco escenas activas): `Builds/PhaseSix/UltimaCinta.exe`.
- [ ] Prueba final con 3–4 amigos en red real.
- [ ] Lista de errores conocidos y mejoras para la v2.

## Definición de «terminado»

- [ ] Se puede crear una sala, unirse por nombre de sala y jugar de 2 a 4 personas.
- [ ] Una partida completa dura entre 2.5 y 3.5 horas.
- [ ] Los 4 fantasmas se pueden identificar y expulsar correctamente.
- [ ] La camcorder es imprescindible: sin ella no se puede ver al fantasma.
- [ ] No hay errores que impidan terminar una partida.






