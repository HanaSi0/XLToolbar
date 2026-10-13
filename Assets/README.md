# Toolbar icon atlas

Original artwork generated for XLToolbar: pale ivory/ice blue, transparent background, matching charcoal outlines.

`toolbar-icons.png` is embedded as `XLToolbar.ToolbarIcons.png`. It is loaded through Dalamud's shared texture provider; the seven toolbar entries never fall back to native game textures.

4 columns × 2 rows, read in row-major order:

1. Effects: magic sparks.
2. Equipment durability: shield and durability meter.
3. Job switching: crossed sword/staff and switching arrows.
4. World map: folded map and location pin.
5. Inventory: satchel.
6. Quest list: scroll and checkmark.
7. Currency: coin stack.
8. Unused.

UV coordinates are defined in `src/ToolbarIconAtlas.cs`. The shared texture is owned by the provider. While it loads, existing font glyphs provide a fallback. The game menu window continues to use native game icons.
