# Flat toolbar icon atlas

Original monochrome artwork generated for XLToolbar. Uniform pale ice-white, flat silhouettes, transparent background, no gradients or 3D effects.

`toolbar-icons.png` is embedded as `XLToolbar.ToolbarIcons.png`, and loaded through Dalamud's shared texture provider.

4 columns × 4 rows, row-major:

1. Effects; tribes; online status; durability.
2. Jobs; teleport; map; inventory.
3. Quests; DTR; game menu; currency.
4. FPS; volume; clock; settings.

`src/ToolbarIconAtlas.cs` defines UV coordinates. All toolbar icons use this atlas. The shared texture is provider-owned; loading frames reserve icon space without substituting a different icon family. Independent game menus continue to use native menu icons.
