# Patio — prototipo físico para Unity

Prototipo 3D en tercera persona: muñeco articulado con equilibrio asistido, escenario pequeño y una dirección visual inicial tipo PS2. Usa geometría simple, colores apagados, niebla y una imagen de baja resolución. No incluye recursos de otros juegos.

## Abrir y jugar

1. Instala **Unity 2022.3 LTS** desde Unity Hub. La versión de referencia es **2022.3.62f1**. Añade el módulo de compilación de tu sistema si quieres crear un ejecutable.
2. En Hub, usa **Add / Add project from disk** y selecciona esta carpeta: la que contiene `Assets`, `Packages` y `ProjectSettings`.
3. Espera a que termine la importación. Usa **Patio > Create playground scene** en el menú superior.
4. Abre la pestaña **Game** y pulsa **Play**. El patio y el personaje se generan al iniciar.
5. Para crear un ejecutable: **File > Build Settings > Build And Run**. El menú Patio ya añade la escena a Build Settings.

También puedes pulsar Play desde una escena vacía: el prototipo se inicia automáticamente. Para trabajar con otras escenas en el futuro, desactiva ese arranque automático en `PrototypeWorld.Bootstrap`.

Este proyecto usa el renderizador integrado de Unity, sin URP/HDRP, y el sistema de entrada clásico. Si Unity pregunta por el sistema de entrada, selecciona **Input Manager (Old)** o **Both** en **Edit > Project Settings > Player > Other Settings > Active Input Handling**. No necesita paquetes externos, servicios ni claves.

## Obtener el proyecto desde GitHub y compilar para Windows

Clona `https://github.com/DanjoGo/example-codex.git` o descarga **Code > Download ZIP** desde la rama `main` en GitHub y descomprímelo. En Unity Hub selecciona la carpeta que contiene `Assets`, `Packages` y `ProjectSettings`, no la carpeta `Assets` por separado.

Los archivos fuente están incluidos en el repositorio. La escena se crea en el editor mediante **Patio > Create playground scene**; las demás opciones de proyecto que falten se generan al importar. No se incluye un ejecutable ni se ha validado la importación con Unity todavía.

Para generar un `.exe` desde Windows:

1. Abre el proyecto con Unity 2022.3 LTS y espera a que termine la importación. Resuelve cualquier error rojo de Console antes de continuar.
2. Selecciona **Patio > Create playground scene** y prueba el juego con **Play**. El comando guarda `Assets/Prototype/Scenes/Playground.unity` y la añade a Build Settings.
3. Abre **File > Build Settings**, selecciona **PC, Mac & Linux Standalone**, **Target Platform: Windows** y **Architecture: x86_64**. Si cambias de plataforma, pulsa **Switch Platform**.
4. Pulsa **Build**, elige `Builds/Windows` como carpeta de salida y `Patio.exe` como nombre. Si Unity solicita soporte de compilación, instálalo para esa misma versión desde Unity Hub; IL2CPP requiere además sus herramientas de C++.
5. Ejecuta `Patio.exe`. Para compartirlo, conserva toda la carpeta generada, incluyendo `Patio_Data`, `UnityPlayer.dll` y los demás archivos que Unity produzca; el `.exe` solo no es suficiente.

`Library`, `Temp`, `Logs`, `UserSettings` y las compilaciones se excluyen mediante `.gitignore`; Unity regenera sus cachés localmente.

## Controles

| Acción | Control |
|---|---|
| Mover | WASD |
| Correr | Shift izquierdo |
| Saltar | Espacio |
| Entrar / salir de cobertura | C |
| Subir un borde / superar un obstáculo | E |
| Soltarse como muñeco de trapo temporalmente | R |
| Volver al inicio | T |
| Girar cámara | Mantener botón derecho y mover el ratón |
| Acercar / alejar cámara | Rueda |

Acércate mirando a una pared para cubrirte. En cobertura, A/D permiten deslizarte; C o W salen de cobertura. La cobertura se abandona al llegar al extremo del muro. Para subir, mira hacia la caja o plataforma, acércate y pulsa E: requiere apoyo en el suelo, una superficie superior libre y un borde de hasta 1,9 metros. La subida es asistida y suspende temporalmente la simulación del personaje; todavía no hay agarre libre de manos.

## Recorrido de prueba

- Camina y corre por el patio; cambia de dirección y sube la rampa.
- Salta en suelo plano y comprueba el aterrizaje.
- Acércate al muro alto del lado derecho y pulsa C. Recorre sus dos extremos con A/D.
- Prueba la cobertura amarilla baja frente al inicio. Esta versión se apoya en el muro, pero todavía no tiene una postura completa de agachado.
- Usa E frente a la caja baja del lado izquierdo y frente a la plataforma más alta detrás de ella. Prueba también E debajo de un obstáculo: no debería atravesarlo.
- Pulsa R, espera la recuperación y pulsa T para reiniciar desde cualquier posición.
- Gira la cámara junto a paredes y acerca/aleja la vista. Comprueba Console durante todas estas acciones.

## Implementación

- `ActiveBody`: cuerpo central con Rigidbody y cinco piezas físicas con ConfigurableJoint: cabeza, brazos y piernas. Sin colisiones internas entre las piezas; resortes para mantener las posturas y alternar extremidades al caminar.
- `PhysicalMotor`: movimiento relativo a cámara, torque de equilibrio, asistencia vertical cerca del suelo, salto, cobertura y subida de obstáculos con comprobación del espacio y trayectoria.
- `RetroCamera`: cámara orbital con detección de obstáculos, resolución interna de 480 píxeles de ancho y HUD en español. Se puede subir el ángulo con el ratón para una vista más elevada.
- `PrototypeWorld`: patio generado con rampas, escalones, cajas, muros y plataforma. Iluminación y materiales simples con un shader propio incluido en Resources para las compilaciones.

Los parámetros de movimiento, equilibrio, articulaciones y cámara se exponen en sus componentes durante Play. Anota los ajustes que te gusten: los cambios hechos en Play no se conservan al detenerlo. El siguiente paso es probar y ajustar esa sensación antes de añadir armas o enemigos.

## Estado de validación

**El editor de Unity no está instalado en el entorno de creación: este proyecto todavía no se ha compilado ni ejecutado en Unity.** Se revisaron los archivos, el formato de los manifiestos y las referencias internas, sin sustituir las pruebas del editor.

Es una primera aproximación a un ragdoll activo, no una recreación de Euphoria ni un controlador terminado. Las físicas, las colisiones de la cámara y la escalada necesitan validación y ajuste en Unity. No incluye combate, enemigos, animaciones importadas, agarre independiente de manos, un esqueleto humano completo ni arte definitivo. La dirección visual se inspira en las referencias descritas; no reproduce sus gráficos exactamente.
