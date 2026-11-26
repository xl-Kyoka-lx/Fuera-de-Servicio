# Guía para reemplazar el arte

Todo el arte actual es placeholder. Esta guía explica cómo cambiarlo sin romper nada.

## La regla de oro

Unity no enlaza las imágenes por su nombre, sino por el archivo `.meta` que tiene al lado.
Si se borra o renombra una imagen desde el Explorador de Windows, el `.meta` se pierde y
**todas las escenas y prefabs que la usaban quedan con un hueco rosa o vacío**.

Por eso: **no borres el placeholder y metas uno nuevo. Sobrescribe su contenido.**

## Forma fácil: Inventario de Arte

En Unity, menú **Herramientas > Inventario de Arte**. La ventana muestra cada imagen del
proyecto con su tamaño y en qué escenas o prefabs se usa.

1. Busca el placeholder (hay un buscador arriba).
2. Pulsa **Reemplazar…** y elige tu imagen nueva (PNG, JPG, PSD o TGA).
3. Listo: todas las escenas que lo usaban ya muestran el arte nuevo.

Qué hace sola la herramienta:

- Conserva el `.meta`, así no se pierde ninguna referencia (también si cambias de .jpg a .png).
- Con **Mantener tamaño en pantalla** activado, el objeto ocupa lo mismo en el juego aunque tu
  imagen tenga otra resolución (ajusta los *Pixels Per Unit*).
- Sube el tamaño máximo de textura para que Unity no te reduzca imágenes grandes.
- Guarda una copia del placeholder en `Library/ArteRespaldo/` por si quieres volver atrás.

Si tu imagen tiene **otras proporciones** que el placeholder (por ejemplo, más ancha), el
objeto se verá con esas proporciones nuevas. Puede que haya que ajustar su escala o su
collider en la escena.

## Imágenes con varios sprites

Algunas imágenes están cortadas en varias piezas (una hoja de sprites):

| Imagen | Piezas |
|---|---|
| `emogis feli.png` | 5 |
| `emogis mal.png` | 2 |
| `PanelDialopgo.png` | 2 |

Si dibujas la hoja nueva con **la misma distribución** que el placeholder, la herramienta
reubica los cortes sola. Si no, ábrela en el **Sprite Editor** y **mueve** los cortes
existentes encima del dibujo nuevo. No los borres para crear otros: los cortes nuevos tienen
otro ID y los prefabs dejan de encontrarlos.

## Qué hay que reemplazar

Todo está en `Assets/Proyecto/Sprite/`, salvo los fondos.

| Imagen | Tamaño actual | Dónde se usa |
|---|---|---|
| `Ascensor.png` | 434×201 | EscenaDia, EscenaNoche |
| `Camila.png` | 1100×1368 | EscenaDia (retrato de diálogo) |
| `Cielo.png` | 1549×800 | EscenaDia, EscenaNoche |
| `Mar.png` | 1600×1600 | EscenaDia, EscenaNoche |
| `Piso.png` | 1600×1600 | EscenaDia, EscenaNoche |
| `Palanca.png` | 325×213 | prefab Planca |
| `Pasajero.png` | 1600×1600 | prefab Pasajero 1 |
| `emogis feli.png` | 613×593 | prefab EmojiFeliz |
| `emogis mal.png` | 615×596 | prefab EmojiEnojado |
| `PanelDialopgo.png` | 1344×768 | EscenaDia (panel de diálogo) |
| `panelOpciones.png` | 1344×768 | Menu inicio |
| `pixellab-Pixel-art-landscape-…png` | 128×96 | Menu inicio |
| `pixellab-pixel-art-UI-element-…png` | 256×256 | EscenaDia |
| `reloj.png` | 256×256 | EscenaDia |
| `tele.png` | 256×256 | EscenaDia |
| `sin título.png` | 64×64 | EscenaDia |
| `Menu/Fondo/Fondo prueba.jpg` | — | Menu inicio (fondo) |
| `Fondos/Tienda/scratched-steel-…jpg` | — | Tienda (fondo) |

**Sin uso ahora mismo** (no hace falta dibujarlas salvo que se vayan a usar):
`Montañas.png`, `cosa de limpieza.png`, `Fondos/menu/Fondo prueba.jpg` (copia de la del menú).

La lista exacta y al día siempre está en la ventana **Inventario de Arte**.

## Arte que hoy se dibuja por código

Los minijuegos **Pestillo** y **Colillas y Latas** dibujan sus círculos por código. Ahora
tienen dos campos opcionales en el Inspector, sección **Arte (opcional)**:

- `Sprite Disco Arte`: el círculo relleno.
- `Sprite Anillo Arte`: el anillo que se va cerrando.

Si se dejan vacíos se siguen dibujando por código. El arte se tiñe con los colores de la
sección **Colores**; si tu dibujo ya trae su color, pon `Color Disco` y `Color Anillo` en
blanco. Los colores de acierto y fallo se siguen aplicando encima.

## Retratos de diálogo

El `DialogoManager` (en EscenaDia) tiene una lista **Retratos**: nombre del personaje y su
sprite. Para un personaje nuevo, añade una fila con el nombre tal como aparece en el JSON
de diálogos (campo `hablante` o `imagen`).

## Consejos de formato

- PNG con fondo transparente para personajes, objetos y UI.
- Para **pixel art**: en el Inspector de la imagen pon *Filter Mode* en **Point (no filter)** y
  *Compression* en **None**; si no, se verá borroso.
- Se pueden usar archivos **.psd** directamente (el importador de PSD está instalado).
- Nombres sin tildes ni espacios evitan problemas (`montanas.png` mejor que `Montañas.png`).
  Para renombrar, hazlo **dentro de Unity**, nunca desde Windows.
