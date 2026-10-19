DISK CLEANUP HELPER
===================

Un asistente gratuito y de código abierto para Windows 10 y Windows 11.
Muestra qué llena tus discos y libera espacio de forma segura.
Es un solo archivo de programa pequeño. No envía nada sobre tu equipo; solo pregunta a GitHub si hay una versión nueva.


QUÉ HACE
--------
Limpiar                   Limpieza segura de cachés del navegador y de aplicaciones, archivos temporales, archivos de actualización antiguos,
                          restos de Windows y más. Cada elemento explica qué conservas y qué pierdes.
Analizar disco            Muestra las carpetas más grandes (doble clic para ver su contenido) y los archivos más grandes
                          de cualquier unidad, para que encuentres qué ocupa tu espacio.
Salud del disco           Muestra si cada disco y cada unidad está en buen estado y cuánto se ha llenado,
                          con la temperatura en tiempo real y gráficos del historial, e inicia la búsqueda de errores del propio Windows.
Detalles SMART            Los datos completos de autosupervisión de cada disco.
Rendimiento del disco     Actividad del disco en tiempo real y una prueba de velocidad.
Detalles del dispositivo  Windows, procesador, memoria, gráficos, pantallas, almacenamiento, batería, red
                          y sonido, como la Herramienta de diagnóstico de DirectX de Windows. Cópialo o guárdalo como archivo de texto.
Consejos y preguntas      Hábitos sencillos para que tu disco no se llene, y respuestas a preguntas frecuentes.
Acerca de                 Instálalo en tu equipo con una entrada en el menú Inicio y un acceso directo en el escritorio, o desinstálalo.
                          Buscar actualizaciones.


CÓMO EMPEZAR
------------
1. Haz doble clic en DiskCleanupHelper.exe.
2. Windows pregunta "¿Quieres permitir que esta aplicación haga cambios en el dispositivo?". Haz clic en Sí.
   Si haces clic en No, se abre igualmente, pero los elementos que necesitan derechos de administrador aparecen en gris.
3. Si Windows muestra "Windows protegió tu PC", haz clic en "Más información" y luego en "Ejecutar de todas formas".
   Puede aparecer con programas nuevos que Microsoft aún no conoce.
4. Idioma: haz clic en el botón de idioma (el globo) arriba a la derecha y elige un idioma. La ventana cambia
   a ese idioma de inmediato y recuerda tu elección.
5. Claro u oscuro: haz clic en el botón "Modo oscuro" / "Modo claro" arriba a la derecha. La aplicación recuerda tu elección.
6. Para conservarlo: abre la pestaña Acerca de y haz clic en "Instalar en este equipo".
   Después aparece en el menú Inicio, en el escritorio y en Configuración > Aplicaciones.
   No se necesitan derechos de administrador para instalarlo.


CÓMO USARLO
-----------
Pestaña Limpiar:
1. Cierra los programas que no necesites, sobre todo navegadores, aplicaciones de chat, editores de código y juegos.
2. Los tamaños se miden solos al abrir la aplicación. Tarda uno o dos minutos. Haz clic en "Detener" para parar,
   o en "Medir tamaños" para volver a medir.
3. Cada grupo muestra su tamaño total. Haz clic en el título de un grupo para plegarlo o volver a abrirlo;
   la aplicación recuerda qué grupos plegaste.
4. Haz clic en cualquier elemento para leer qué es, dónde está y qué conservas o pierdes.
5. Marca la casilla de cada elemento que quieras limpiar, o haz clic en "Seleccionar lo seguro".
   El título de cada grupo muestra cuántos de sus elementos están seleccionados.
6. Haz clic en "Limpiar seleccionados". Muestra la lista exacta y pregunta antes de borrar nada.

Pestaña Analizar disco:
1. Elige una unidad (o "Solo mi carpeta de usuario") y haz clic en "Examinar". Una unidad entera puede tardar unos minutos.
2. Lado izquierdo: las carpetas más grandes. Haz doble clic en una carpeta para ver su contenido y haz clic en "Subir" para volver.
3. Lado derecho: los archivos más grandes. Selecciona archivos y elige "Mover a la Papelera de reciclaje" o "Eliminar permanentemente".
   Los archivos de Windows y de programas están protegidos y no se pueden eliminar aquí.

Pestaña Salud del disco:
- Muestra cada disco (con temperatura, desgaste y número de errores cuando se inicia con derechos de administrador)
  y cuánto se ha llenado cada unidad.
- La temperatura se actualiza sola cada pocos segundos mientras la pestaña está abierta.
- Pasa el puntero sobre un gráfico del historial para ver el valor exacto y cuándo se guardó.
- "Buscar errores" ejecuta la comprobación de solo lectura del propio Windows en la unidad de Windows.
- "Optimizar unidades" abre el optimizador de unidades del propio Windows.

Pestaña Rendimiento del disco:
- Muestra lo ocupado que está ahora mismo el disco elegido, con gráficos de los dos últimos minutos.
  Pasa el puntero sobre un gráfico para ver los valores exactos.
- "Probar velocidad" mide la velocidad real a la que el disco puede escribir y leer. El resultado se guarda,
  para que puedas comparar más tarde.


QUÉ SIGNIFICAN LOS GRUPOS DE LIMPIEZA
-------------------------------------
Seguro        Cachés y archivos temporales. Los programas los regeneran por sí solos.
              Al limpiar las cachés del navegador y de las aplicaciones se conservan tus contraseñas, marcadores, historial y sesiones iniciadas.
Admin         Archivos de actualización antiguos de programas instalados (conserva los 3 más recientes de cada uno),
              y la limpieza de componentes de Windows propia de Microsoft.
Tú decides    Pierdes algo pequeño: historial de proyectos antiguos, canciones descargadas,
              vistas previas de video, archivos de volcado de bloqueo o lo que haya en la Papelera de reciclaje.
Ajuste        Opciones de Windows que liberan espacio: hibernación, Almacenamiento reservado, CompactOS
              y Sensor de almacenamiento.
Informativo   Cosas grandes que la herramienta solo muestra y abre, nunca borra: instalación anterior
              de Windows, programas más grandes, Restaurar sistema, Docker, WSL, Descargas.

Solo se muestran los elementos que existen en tu equipo.


SEGURIDAD
---------
- No se borra nada hasta que lo seleccionas y lo confirmas.
- Los archivos borrados desde la pestaña Limpiar no van a la Papelera de reciclaje y no se pueden restaurar.
- La pestaña Limpiar nunca toca tus documentos, fotos, música, videos, escritorio ni descargas.
- Te pide que cierres un navegador o una aplicación antes de limpiar su caché.
- Te avisa si el equipo tiene poca memoria antes de medir, examinar o limpiar.
- Nunca sigue los vínculos de carpeta hacia otras carpetas.
- Nunca borres a mano archivos de las carpetas Windows, WinSxS o Installer.


HISTORIAL
---------
Lo que se limpió se guarda solo en este equipo, en:
%LOCALAPPDATA%\DiskCleanupHelper\cleanup-history.log
Ábrelo desde la pestaña Acerca de.


ACTUALIZACIONES
---------------
Al iniciarse, la aplicación pregunta a su página de GitHub si hay una versión nueva (como máximo una vez al día).
Una versión nueva se descarga en segundo plano y se comprueba (SHA256 y número de versión) antes de usarse.
Luego una barra ofrece "Reiniciar ahora". Si eliges "Más tarde", la actualización se instala cuando cierras la aplicación.
Para desactivar la comprobación automática, desmarca la casilla en la pestaña Acerca de. "Buscar actualizaciones" sigue funcionando ahí.
No se envía nada sobre tu equipo.


DESINSTALAR
-----------
Configuración > Aplicaciones > Disk Cleanup Helper > Desinstalar, o haz clic en "Desinstalar" en la pestaña Acerca de.
Si nunca lo instalaste, simplemente borra DiskCleanupHelper.exe.


DESHACER UN AJUSTE DE WINDOWS
-----------------------------
Abre el Símbolo del sistema como administrador y ejecuta la línea que necesites:
    powercfg /h on                                          (vuelve a activar la hibernación)
    DISM /Online /Set-ReservedStorageState /State:Enabled   (vuelve a activar el Almacenamiento reservado)
    compact.exe /compactos:never                            (deja de comprimir los archivos de Windows)


REQUISITOS
----------
Windows 10 o Windows 11. No hay que instalar nada más: usa el .NET Framework integrado en Windows.


LICENCIA Y AUTOR
----------------
Disk Cleanup Helper by Philip Droubi - https://github.com/Philip-Droubi
Copyright (C) 2026 Philip Droubi.
Software libre y de código abierto: puedes usarlo, modificarlo y compartirlo bajo la GNU General Public License v3
con condiciones adicionales (sección 7): consulta LICENSE.txt y NOTICE.txt incluidos en la descarga, o "Licencia y condiciones" en la pestaña Acerca de.
Código fuente: https://github.com/Philip-Droubi/disk-cleanup-helper
Contacto: https://philip-droubi.github.io/Philip_Droubi_portfolio/ y https://www.linkedin.com/in/philip-droubi/


EXENCIÓN DE RESPONSABILIDAD
---------------------------
Este programa se proporciona tal cual, sin garantía de ningún tipo. Úsalo bajo tu propia responsabilidad.
Asegúrate de tener una copia de seguridad de los archivos importantes antes de limpiar cualquier equipo.
