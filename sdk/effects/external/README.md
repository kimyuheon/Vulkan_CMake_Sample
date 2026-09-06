# Effekseer runtime samples

Unmodified examples from Effekseer 1.80.1 for Windows, `Sample/00_Basic`:

- `Simple_Sprite_BillBoard.efkefc`: a short burst of billboard particles.
- `Simple_Ribbon_Sword.efkefc`: a short sword trail.
- `Simple_Turbulence_Fireworks.efkefc`: multicolour fireworks.

Source: [official release](https://github.com/effekseer/Effekseer/releases/tag/1.80.1)
(`Effekseer1.80.1Win.zip`, SHA-256
`8fdd136e861a628bb009ce5edbad304774959803e297bbec50b847be18c7a1bb`).
The archive's `Sample/readme.txt` is preserved as `SAMPLE_LICENSE.txt`: these
sample effects are CC0. Runtime/dependency notices are in `../licenses`.
The editor is not required or shipped with the engine.

Keep `Texture` beside the effect files when importing. Import stores a snapshot
of referenced resources; saving a `.lot` embeds it. Original files are not
needed to reopen that drawing. Other downloaded assets can have other licenses.

Use **Effects > Particle emitter**, then drag an external sample onto the view
or double-click its name and click a position. Newly inserted/reopened effects
are stopped. Select it, enable **Loop**, and press **Play** in Properties for a
continuous demonstration. Short one-shots otherwise disappear quickly.

The default authoring conversion is 1 effect unit = 0.1 metre. Choose m/mm in
the particle library for the drawing, and use the object's Scale to resize.
The initial selection/zoom bounds are an approximate 2.5 m radius, not collision
geometry. These are runtime effects, not native fire/smoke preset JSON files.
