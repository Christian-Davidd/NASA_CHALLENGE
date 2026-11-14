# NASA Z-2 astronaut sprites

Paquete de sprites 2D para Unity reconstruido tomando como referencia principal las cuatro vistas fotográficas Z-2 compartidas en la conversación. La maleta tiene una silueta convexa tipo caparazón y una línea vertical gris de unión con la espalda; incluye emblema NASA trasero y tres ópticas frontales. Se eliminaron la barra negra superior y los módulos tipo cámara de los perfiles laterales del casco.

Es una ilustración vectorial hecha para el juego, no una extracción de la malla 3D oficial ni una reproducción fotográfica píxel por píxel. Se conservaron las formas y detalles visibles en las referencias; los detalles no visibles se mantuvieron sencillos para no inventar componentes.

## Sprites

`Sprites` contiene cuatro vistas neutras (frente, espalda y ambos perfiles), dos cuadros de caminata frontal, dos traseros y cinco poses laterales en las dos direcciones: caminata, interacción, reparación y alerta. Cada imagen es un PNG transparente de 640 × 960 con un pivote común y una escala de importación de Unity de 448 Pixels Per Unit. Las piernas alternan su avance al caminar hacia arriba o abajo.

`Layers` contiene los recortes superior e inferior de las cuatro vistas neutras. Comparten el lienzo y pivote de los sprites completos para alinearse en un personaje por capas.

`Source` conserva los maestros y SVG editables. Las herramientas de generación están en `Tools/AssetGeneration/AstronautaZ2`; `render_z2_walks.cjs` rasteriza las cuatro poses verticales. Los PNG de `Sprites` y `Layers` son los recursos listos para Unity.

## NASA references

- [NASA Z-2 3D model and overview](https://science.nasa.gov/3d-resources/z2-spacesuit/)
- [NASA technical paper: Z-2 Architecture Description and Requirements Verification Results](https://ntrs.nasa.gov/api/citations/20150022144/downloads/20150022144.pdf?attachment=true)
- [Official NASA model preview image](https://raw.githubusercontent.com/nasa/NASA-3D-Resources/master/3D%20Models/Z2%20Spacesuit/Z2%20Spacesuit.png)

The Z-2 is a prototype developed toward future planetary surface suits, not a flight suit currently used on Mars.
