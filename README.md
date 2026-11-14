# NASA Challenge — First on Mars

El repositorio mantiene la portada del equipo y la versión activa del escenario jugable `Mars_Natural_Landscape_01`.

## Estructura

- `My project (4)/Assets/Portada/`: portada, menú y recursos gráficos de la portada.
- `My project (4)/Assets/Audio/`: efectos de sonido, manteniendo la ruta de carga de Unity `Resources/Aurora`.
- `My project (4)/Assets/Art/Characters/`: astronauta Z-2 y sprites.
- `My project (4)/Assets/Art/Environment/MarsNaturalLandscape01/`: arte geológico y prefabs de la escena vigente.
- `My project (4)/Assets/Scenes/`: portada, escenario actual, plantilla y escenas del equipo.
- `My project (4)/Assets/Scripts/Gameplay/`: movimiento, cámara e interacción.
- `Documentation/`: guías del astronauta y del escenario.
- `Tools/AssetGeneration/`: generadores y fuentes de construcción fuera de los assets importados.
- `Previews/`: vistas completas y de detalle del escenario actual.
- `nasa-data/` y `.github/`: datos y automatizaciones existentes del equipo.

Unity solo importa a la aplicación los recursos ubicados bajo `Assets/`; por eso el contenido ejecutable conserva esa carpeta y la documentación, herramientas y vistas previas están separadas en la raíz del repositorio.

## Inicio

Abre `My project (4)` como proyecto Unity. La primera escena de Build Settings es `Portada`; su botón **Despegar** carga `Mars_Natural_Landscape_01`.
