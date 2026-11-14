# Generación de assets

Los scripts de esta carpeta escriben sus resultados dentro de `My project (4)/Assets` y mantienen allí los archivos `.meta` para Unity. Instala las dependencias con `npm install` y `python -m pip install -r requirements.txt` desde esta carpeta.

## Astronauta Z-2

- `AstronautaZ2/make_z2_master.py`: regenera los maestros vectoriales desde el constructor.
- `AstronautaZ2/build_z2_svgs.py`: produce los SVG de vistas y poses.
- `AstronautaZ2/render_z2_walks.cjs`: renderiza las cuatro poses de caminata frontal y trasera a PNG.

## Escenario marciano

- `MarsNaturalLandscape01/generate_natural_assets.cjs`: genera los SVG y PNG de terreno y geología.
- `MarsNaturalLandscape01/create_natural_scene.py`: recompone `Mars_Natural_Landscape_01.unity`, los prefabs y las vistas de `Previews/`.

La escena es la única versión de escenario que se conserva en el proyecto activo. Las fuentes visuales del traje y los SVG editables permanecen junto a los sprites en `Assets/Art/Characters/AstronautaZ2/Source`.
