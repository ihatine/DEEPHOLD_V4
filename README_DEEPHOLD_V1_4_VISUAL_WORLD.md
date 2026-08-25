# DEEPHOLD v1.4 — Volumetric World & Character Pass

Unity: 6000.0.59f1

## What changed
- 2.5D angled orthographic camera for visible world depth.
- Chunk terrain now has exposed side walls / volume instead of flat planes.
- Built-in-pipeline surface shader adds procedural material variation and lighting response.
- Animated water shader with highlights, foam-like edge accents and smoothness.
- Warm directional sun + soft shadows + ambient tri-light + fog.
- Lightweight fake sun shafts for atmosphere without extra post-processing packages.
- Trees rebuilt as layered volumetric low-poly canopies with trunks and ground shadows.
- Character sprite sheet redesigned around the supplied cyan-hair / blue-scarf / light-armor concept.
- PC quality preset receives 4x MSAA and a sane shadow distance.

## Important
This remains deliberately package-light and uses the project's existing built-in render pipeline. No URP/HDRP migration was introduced, so the patch remains compatible with the current project architecture.
