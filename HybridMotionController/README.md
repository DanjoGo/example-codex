# Hybrid Motion Controller — Fase 1

Proyecto nuevo e independiente de Patio. **Abre esta carpeta `HybridMotionController` en Unity Hub**, no la raíz del repositorio ni `Assets`. No carga los scripts, el ragdoll ni el arranque automático del proyecto Patio.

## Alcance y estado

Se implementó exclusivamente la Fase 1: desplazamiento relativo a cámara, caminar/trotar/correr, aceleración y frenado, giro suave, salto bajo, detección de suelo, cámara orbital de hombro y un generador de escenario/prefab provisional.

**Pendiente de revisión dentro de Unity.** Este entorno no tiene el editor de Unity: no se ha importado, compilado ni ejecutado el proyecto allí. Se comprobó la sintaxis de C# mediante un parser, el formato de entrada y las referencias de los archivos. Eso no demuestra el comportamiento físico ni la compatibilidad de todas las APIs. No se considera aprobada la Fase 1 hasta completar la prueba manual.

## Versión, renderizado y dependencias

- Unity **2022.3.62f1 LTS**, misma versión que el proyecto existente.
- Renderizador integrado (Built-in). No se instala URP.
- **Input System 1.7.0**, único paquete adicional: necesario para los controles solicitados. Unity lo descarga al importar mediante Package Manager. Se conserva la verificación normal de los paquetes.
- Módulos integrados Physics e IMGUI para colisiones y panel de diagnóstico.
- Sin modelos, animaciones, plugins de terceros, claves ni servicios externos. El personaje provisional es geometría creada por el generador, sin recursos copiados de otros juegos.

## Instrucciones exactas

1. Obtén la rama `main` del repositorio `DanjoGo/example-codex` mediante Git o **Code > Download ZIP** en GitHub y descomprime si corresponde.
2. Instala Unity **2022.3.62f1** desde Unity Hub. En **Projects > Add > Add project from disk**, elige la subcarpeta **`HybridMotionController`**, donde están `Assets`, `Packages` y `ProjectSettings`.
3. Abre el proyecto y espera la importación y descarga de Input System. Si Unity pregunta si quieres habilitar los backends del sistema nuevo, responde **Yes** y deja que reinicie.
4. Comprueba **Edit > Project Settings > Player > Other Settings > Configuration > Active Input Handling**: debe estar en **Input System Package (New)** o **Both**. Reinicia el editor si solicita hacerlo. El generador bloquea su ejecución con un mensaje claro si el sistema nuevo no está habilitado.
5. Cuando Console esté sin errores de importación o compilación, usa **Hybrid Motion > Generate MovementTest (Phase 1)**.
6. El generador crea y abre `Assets/HybridMotion/Generated/MovementTest.unity`, guarda `HybridPlayer.prefab` y los materiales en esa carpeta, y añade la escena como primera en Build Settings sin quitar otras escenas. **Estos archivos se generan al ejecutar el menú; no se afirma que ya se hayan generado en este entorno.**
7. Selecciona **Game**, pulsa **Play** y haz clic dentro de la vista si hace falta darle el foco. Pulsa **Esc** para liberar/capturar el cursor. Al perder el foco de la aplicación se libera y se suspende la entrada de movimiento.
8. Detén Play para ajustar parámetros y conservarlos. Las modificaciones durante Play se pierden al detenerlo. Guardar la escena conserva los cambios hechos fuera de Play.

Regenerar solicita confirmación si la escena generada ya existe, porque reemplaza la escena, el prefab y los materiales generados. Conserva tus personalizaciones en otra escena antes de regenerar.

## Controles de esta fase

| Control | Acción |
|---|---|
| WASD | Trotar, velocidad predeterminada 4 m/s |
| Shift izquierdo + WASD | Correr, 6,5 m/s |
| Ctrl izquierdo + WASD | Caminar, 2 m/s; tiene prioridad sobre Shift |
| Espacio | Salto, altura inicial aproximada 0,8 m |
| Mouse | Rotar cámara horizontal y verticalmente |
| Esc | Liberar/capturar cursor |

**E y R no tienen acciones todavía:** cobertura y ragdoll pertenecen a fases posteriores. No hay teclas heredadas de Patio.

## Arquitectura

| Componente | Responsabilidad |
|---|---|
| `PlayerInputHandler` | Acciones del asset `Player.inputactions`, captura del cursor y foco. Instancia privada del asset al ejecutar. |
| `PlayerMotor` | CharacterController, aceleración/frenado, giro, gravedad, salto, control aéreo y reinicio si cae fuera del mapa. |
| `GroundDetector` | SphereCast que descarta el propio personaje, triggers y pendientes demasiado inclinadas. |
| `PlayerStateController` | Estados Idle, Walk, Jog, Sprint, Jump, Fall y Land, sin animaciones aún. |
| `ThirdPersonCamera` | Seguimiento y órbita suavizados, offset de hombro, límites verticales y SphereCast contra obstáculos. |
| `MovementDebugHUD` | Estado, velocidad horizontal real y contacto con suelo para las pruebas. |
| `MovementTestGenerator` | Creación desde el editor de escena, prefab y materiales persistentes. |

El motor usa **CharacterController sin Rigidbody**. Los cubos dinámicos tienen sus propios Rigidbody; no se aplica fuerza de empuje desde el personaje en esta fase. No existe física secundaria ni simulación física del esqueleto. No se mezclan Animator y Rigidbody en el jugador.

En el Inspector de `HybridPlayer` puedes ajustar las velocidades, aceleración, frenado, giro, gravedad, altura de salto, control aéreo, margen para saltar al abandonar un borde y buffer del salto. `CharacterController` tiene radio 0,3 m, altura 1,85 m, límite de pendiente 45° y step offset 0,28 m. Mantén su escala en (1,1,1).

El prefab incluye referencias a sus componentes y al asset de entrada. La instancia en `MovementTest` recibe la referencia a la cámara de la escena. Si colocas el prefab manualmente en otra escena, asigna `PlayerMotor.cameraTransform` y las referencias de `ThirdPersonCamera` en el Inspector: el prefab no puede almacenar referencias a objetos de escena externos.

El salto calcula la velocidad con `sqrt(2 * gravedad * altura)`, con gravedad descendente, terminal y control aéreo limitado. El estado Land es diagnóstico, no flexión de rodillas. La cámara se retrae inmediatamente para evitar atravesar superficies; su foco y órbita se suavizan. No incluye seguimiento especial de cobertura.

## Escenario generado

Suelo de 36 × 36 m, rampa de 15°, escalera de 12 peldaños de 0,22 m que llega a una plataforma elevada, bloques para salto, pared alta, caja baja y tres cubos dinámicos. Los obstáculos de cobertura son solo geometría durante esta fase. Caer por debajo de -20 m reposiciona al personaje al inicio; no hay daño por caída ni ragdoll.

El personaje tiene torso, cabeza y extremidades geométricas estáticas y una marca frontal para comprobar el giro. **Todavía no tiene Avatar humanoide, esqueleto animado, Animator Controller ni clips.** El rig compatible con Animator/ragdoll corresponde a las siguientes fases, después de revisar la base de locomoción.

## Revisión manual obligatoria de Fase 1

Registra la versión de Unity, resultado y errores de Console antes de aprobarla:

- [ ] El paquete se importa, el generador crea escena y prefab y Console no muestra errores.
- [ ] Play muestra un solo jugador y una cámara; no aparece el patio antiguo.
- [ ] WASD trota a unos 4 m/s; Ctrl baja a unos 2 y Shift sube a unos 6,5 en suelo plano tras acelerar. Las diagonales no superan la velocidad cardinal.
- [ ] Al soltar las teclas frena de forma breve y gradual; contra una pared no acumula velocidad ni atraviesa la colisión.
- [ ] Cambiar de dirección gira el personaje suavemente y sigue la orientación de cámara.
- [ ] Espacio salta aproximadamente 0,8 m en suelo plano; mantenerlo pulsado no repite saltos y pulsarlo en el aire no permite saltos ilimitados.
- [ ] Saltar bajo un techo corta el ascenso; caminar desde un borde pasa a Fall y aterrizar vuelve a locomoción sin quedar suspendido.
- [ ] Sube y baja rampa y escalones, se detiene en ellos y no vibra, flota o atraviesa peldaños.
- [ ] Mouse orbita sin saltos, respeta los límites verticales y mantiene la distancia. Acércate a la pared y revisa colisiones con la cámara en todos los ángulos.
- [ ] Esc libera y recaptura el cursor. Cambiar de ventana interrumpe la entrada; al volver se puede capturar sin giro inesperado.
- [ ] Caer desde la plataforma vuelve a contacto estable con el suelo. No se espera ragdoll todavía.
- [ ] Guardar, cerrar y reabrir `MovementTest` conserva las referencias del jugador y cámara. El prefab se genera sin referencias de escena persistidas accidentalmente.

No se avanza a Fase 2 sin revisar estos resultados. No hay ejecutable precompilado. Tras validar, se puede compilar para Windows desde **File > Build Settings > PC, Mac & Linux Standalone > Windows > x86_64 > Build** con el módulo de plataforma necesario. Distribuye toda la carpeta resultante, no solo el `.exe`.

## Pendientes explícitos

Fase 2: rig/animación, transiciones visuales, inclinación, respiración y balanceo. Fase 3: ragdoll completo, impactos, detección de boca arriba/abajo y recuperación. Fase 4: entrada/salida y desplazamiento lateral en cobertura alta/baja. Fase 5: integración, validación y ajuste. Ninguno de esos sistemas se implementa o se da por probado aquí.
