# Portrait Atelier

Portrait Atelier creates portrait variations from your current in-game portrait. It keeps the captured pose, expression, animation, banner frame and decoration while changing the camera distance and lighting.

## In-game workflow

1. Open the game's Portrait Editor and wait for the character preview to load.
2. In Portrait Atelier, click **Capture current portrait**, or paste a compatible Portrait Helper code.
3. Search the featured looks, set the style strength, and generate a variation.
4. Click **Apply to open Portrait Editor** to preview it. An imported code can also be applied as-is.
5. If you want to keep it, review the result and use the game's own save button.

Capture and apply read or change the open portrait editor only after the user presses the corresponding button. Apply changes the editor preview; it never presses the game's save button. The plugin never sends portrait data to a server. It does not collect usage statistics or character data. Copy code creates a portable version 1 Portrait Helper preset string for sharing or importing with HaselTweaks.

The editor bridge uses version-sensitive FFXIV client structures. Patch changes can require plugin updates. If an imported code's frame or decoration is unavailable on the current character, the plugin keeps the current selections and still applies the camera and lighting. Runtime preview and saving still need to be validated in-game.

## Featured looks

The starter gallery contains Golden Hour, Moonlit, Soft Studio, Rose Bloom, Ember Drama, Arcane Violet, Verdant Glow, and Hero Frame. These are curated style directions, not a popularity ranking. Use **Refresh styles** to fetch the public catalog when online; refreshing is manual.

## Contributing looks

See [CONTRIBUTING.md](CONTRIBUTING.md) for the catalog schema and submission guidance. A look is a set of lighting and camera-distance targets, not a copied player portrait or image.

## Preset code compatibility

The encoder reads and writes version 1 `HTPS` portrait helper codes. It preserves fields it does not deliberately style. Unsupported versions and malformed input are rejected.

## Build

Requires the Dalamud API 15 development SDK and .NET 10. Open `PortraitAtelier.csproj` and build with the Dalamud SDK installed. GitHub Actions builds against the public Dalamud staging SDK and creates release zips from version tags.

## Installation

After a release is published, add this custom repository URL in Dalamud's Experimental settings:

`https://raw.githubusercontent.com/Jacob5800/PortraitAtelier/main/repo.json`

Then install **Portrait Atelier** through the plugin installer. The repository URL will work once the project is published and its first release has been built.

## License

All rights reserved. See [LICENSE](LICENSE).
