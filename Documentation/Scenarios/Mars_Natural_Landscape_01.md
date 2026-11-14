# Mars Natural Landscape 01

Escena 2D top-down amplia para recorrer con el astronauta Z-2 del proyecto. El terreno se apoya en rasgos que aparecen en imágenes marcianas reales: mesetas erosionadas, escarpes escalonados, laderas con talud y bloques, dunas y afloramientos sedimentarios. Está dibujado como ilustración 2D para mantener la coherencia visual con el personaje; no pretende ser una textura fotográfica.

## Escena jugable

- `My project (4)/Assets/Scenes/Mars_Natural_Landscape_01.unity` mide **108 × 72 unidades**. La aplicación arranca en la portada; el botón **Despegar** abre este escenario.
- `SampleScene` se conserva como plantilla técnica para generar escenas. También se conservaron las escenas de los módulos del equipo (`Entrenamiento`, `RelevoMarte` y `Tormenta`).
- El astronauta usa los sprites ya existentes de frente, espalda, perfiles, caminata e interacción. Su altura de sprite es aproximadamente **2,14 unidades**; los bloques mayores miden cerca de **4 unidades de alto** y los menores se dispersan alrededor de ellos.
- **WASD o flechas:** movimiento en ocho direcciones. **E:** registrar una muestra geológica cuando el personaje esté junto al macizo, el escarpe escalonado o el talud.
- La cámara ortográfica sigue al jugador y se limita a los bordes del mapa. Hay Rigidbody2D, colisiones del jugador, rocas, formaciones y límites exteriores.
- En el sector este queda una explanada natural de **28 × 20 unidades**, sin rocas ni formaciones encima, reservada para construir la base. Se muestra directamente el terreno base, sin un óvalo o marca encima. La distribución de rocas evita esa zona además de mantener separación entre piedras.
- La escena generada contiene **84 SpriteRenderer y 74 BoxCollider2D**. Las **54 rocas sueltas** tienen collider: las pequeñas usan una caja más ajustada a su silueta y las medianas y grandes una huella mayor. `Prefabs/` contiene 11 formaciones y agrupaciones de roca reutilizables.

## Scripts

- `Assets/Scripts/Gameplay/AstronautController2D.cs`: movimiento con WASD o flechas, poses de caminata y orientación, consulta de interacción con E y aviso del resultado de la interacción. No muestra un recordatorio fijo de controles.
- `Assets/Scripts/Gameplay/MarsCameraFollow2D.cs`: cámara con seguimiento suave y límites del mapa.
- `Assets/Scripts/Gameplay/MarsInteractable2D.cs`: registro de muestras en tres formaciones geológicas.
- `Tools/AssetGeneration/MarsNaturalLandscape01/generate_natural_assets.cjs` genera **33 SVG editables y sus PNG**. Solo conserva assets usados por la versión actual; el antiguo marcador ovalado de la explanada se retiró.
- `Tools/AssetGeneration/MarsNaturalLandscape01/create_natural_scene.py` genera la escena, los prefabs y las vistas previas. La distribución es determinista para facilitar cambios repetibles.

## Referencias geológicas

Las fotografías compartidas guían la paleta y las formas generales. Para las formaciones vistas desde arriba se consultaron imágenes orbitales y de superficie de NASA: [colinas escalonadas en Marte](https://science.nasa.gov/resource/stair-stepped-hills/), [mesetas y depresiones](https://science.nasa.gov/photojournal/mesas-and-pits/), [bloques en un deslizamiento](https://science.nasa.gov/photojournal/boulders-on-a-landslide/) y [llanura con bloques en Utopia Planitia](https://science.nasa.gov/photojournal/boulder-strewn-plain-in-northern-utopia-planitia/). La traducción de esos rasgos al mapa 2D —por ejemplo, representar el relieve como contornos, terrazas y caras sombreadas— es una interpretación para el juego.

## Vistas previas

`Previews/Mars_Natural_Landscape_01_preview.png` muestra el mapa completo; `Previews/Mars_Natural_Landscape_01_detail.png` encuadra al astronauta a una escala similar a la cámara de juego.
