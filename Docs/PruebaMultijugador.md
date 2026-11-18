# Prueba multijugador Photon

La build de Windows admite estos argumentos opcionales para repetir la prueba local sin pulsar botones:

- `-ucCreateRoom <nombre>`: crea una sala.
- `-ucJoinRoom <nombre>`: se une a esa sala.
- `-ucSpawnGhost`: junto con `-ucCreateRoom`, activa el agente del fantasma al cargar la mansión para probar el traspaso de autoridad.
- `-ucDropTest`: el creador agrega y suelta una llave al aparecer en la mansión.
- `-ucPickTest`: el cliente recoge el objeto de red cuando aparece.

Cada instancia debe usar un archivo de log distinto con `-logFile <ruta>`. En los logs se espera ver `adding player [Player:1]` y `adding player [Player:2]`. Para la prueba de objeto, el log del creador muestra `Creador soltó KEY_BASEMENT`, ambos muestran `Objeto de red visible` y ambos terminan con `Objeto de red marcado como recogido`. Al cerrar el creador, el invitado debe seguir en la escena y registrar `Autoridad ... transferida: activa=True` para el reloj, el estado global del fantasma y, si se usó `-ucSpawnGhost`, el agente de IA.

Al ejecutar con `-ucCreateRoom` o `-ucJoinRoom`, Bootstrap carga el Lobby automáticamente para QA. Sin esos argumentos, el flujo normal inicia en el menú principal.

Para validar entre dos PCs, copiar la carpeta completa de la build, iniciar una como creadora desde el lobby y unirse por el mismo nombre desde la otra PC. Los argumentos automatizados sirven solo para QA; el flujo normal del juego continúa siendo el lobby.
