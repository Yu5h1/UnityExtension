# Yu5h1Lib.Animation

Animation components for Unity (drives, reactions, transitions, locomotion helpers) and the **humanoid importer** for
glTF characters and motions from the Limb Character Builder.

## Humanoid importer (LIMB-ANIM-1)

Character Builder delivers one character GLB and one GLB per motion (the skeleton and one animation). glTFast
imports them as Generic rigs with no root motion. The humanoid importer turns them into a **humanoid Avatar** and
**humanoid clips with root motion**, so one motion plays on characters of any proportion and the code that uses the
humanoid API (IK, `GetBoneTransform`, `Animator.deltaPosition`) keeps working. It does not touch glTFast or the GLB.

Everything lives in the Editor assembly (`Yu5h1Lib.Animation.Editor`); nothing of it is needed at run time. The
generated Avatar and clips are ordinary assets.

### Use

1. Import the GLB with glTFast as usual (character GLB, or motion GLB).
2. Select the GLB in the Project window and choose **Assets > Humanoid Import Settings**. This creates
   `<name>.humanoid.asset` beside it (or selects the one that exists).
3. Set the options (below) and press **Generate**.

The result is stored as sub-assets of the settings asset:

| Source | Output |
|---|---|
| Character GLB (has a skinned mesh, or no clip) | `<name>Avatar`: a humanoid Avatar, `isValid` and `isHuman` |
| Motion GLB (every clip in the file) | One humanoid `AnimationClip` per clip, named like the source clip |

Press **Generate** again after changing the options or after the GLB was exported again: the sub-assets are overwritten
in place, so Animator Controllers and prefabs that reference them keep working. Sub-assets the source no longer
produces are removed.

Assign the Avatar to the Animator of the character (any character from the same base skeleton can use its own Avatar)
and put the clips in a controller as usual; turn **Apply Root Motion** on to move the GameObject.

### Avatar

Bones are matched with `humanoid.bones.json`, the table from the Character Builder skeleton folder (our joint names
↔ `HumanBodyBones`). The copy in `Editor/Humanoid/humanoid.bones.json` must stay byte-identical to
`CharacterBuilder/skeleton/humanoid.bones.json`; update it when the skeleton document changes it. The imported node
transforms are the base skeleton's T-pose and are used as the Avatar's T-pose. Joints the character does not have
(fingers, toes, eyes, jaw, clavicles, upper chest) are skipped; the Avatar is refused when a required bone (hips,
spine, head, arms, legs) is missing.

### Settings

Modelled on `ModelImporter`.

| Setting | Meaning |
|---|---|
| Motion Node | The node whose movement becomes root motion. `root` (default), or `pelvis` for a motion that has no `root` track. |
| Root Transform Rotation | Heading (rotation around the vertical axis). **Bake Into Pose** keeps it in the pose; **Offset** is in degrees. |
| Root Transform Position (Y) | Height. **Bake Into Pose**, **Offset** in meters. |
| Root Transform Position (XZ) | Travel. **Bake Into Pose**, **Offset** in meters (X, Z) in the frame of the GameObject. |
| Loop Time / Loop Pose | Stored in the clip settings. |

How a clip plays:

- An **unbaked** channel is the root motion: the GameObject follows it. Its movement is taken relative to the first
  frame, so the clip starts at the GameObject (as ModelImporter does with *Based Upon (at Start)*).
- A **baked** channel stays in the pose: the character moves, turns or rises while the GameObject stays put, exactly
  as the generic clip has it.
- **Offset** moves the pose of a baked channel in the GameObject's frame. It has no effect on a channel that is not
  baked (the channel is the motion itself), so the inspector enables it only while Bake Into Pose is on.
- With nothing baked and no offset, root motion plus pose reproduce the generic clip exactly.

Root motion is scaled by the human scale of the character that plays it, which is how Unity retargets humanoid root
motion: a chibi covers a smaller distance than an adult with the same clip.

### Control intervals

A motion GLB from Character Builder may carry the named control intervals of its animation file
(ANIMATION-LIMB-8): `animations[i].extras.yu5h1.intervals` = `{ fps, length, duration, items }`, each item
`{ name, start, end, startTime, endTime, underControl, cancellable }` (frames, and seconds = frame ÷ `fps`).
Generating a motion GLB that has them also creates one `MotionIntervals` ScriptableObject per clip, named
`<clip>Intervals`, as a sub-asset of the settings asset, and lists it in the settings' `intervals`. It is
overwritten in place when you generate again, so references to it survive. A GLB without intervals creates nothing,
and the clips and Avatar come out exactly as before.

`MotionIntervals` (runtime assembly `Yu5h1Lib.Animation`, namespace `Yu5h1Lib`) is what a game reads:

| Member | Meaning |
|---|---|
| `clip`, `frameRate`, `length`, `duration` | The humanoid clip, the file's fps, its last frame and its length in seconds. |
| `intervals` | `MotionInterval` list in file order: `name`, `startFrame`, `endFrame`, `startTime`, `endTime` (seconds), `underControl`, `cancellable`. |
| `Normalize(seconds)`, `NormalizedStart(i)`, `NormalizedEnd(i)` | Normalized time 0..1 (seconds ÷ `duration`, equal to frame ÷ `length`). |
| `TryFind(name, out i)` | The first interval with that name. |
| `At(seconds)`, `AtNormalized(t)` | The intervals that hold the time; an interval is [start, end), and one ending at the last frame holds the end too. |
| `IsUnderControl(seconds)`, `IsCancellable(seconds)` | Whether any interval holding the time has the flag. |

Why a separate asset: an `AnimationClip` cannot carry data of its own, and the settings class lives in the Editor
assembly, which a game does not ship. `MotionIntervals` is a runtime ScriptableObject next to the clip, so a game
references it from a field and plays the clip and asks it, with no editor code. The importer reads the file's JSON
itself (`GlbIntervalReader`), because glTFast does not expose animation extras; damaged or missing extras give no
intervals and never an error.

### What is not carried over

- Bone translation (arm stretch and the like): a humanoid clip holds muscles only. Stretching is to be added later as
  an additive Animation Job on the humanoid result.
- Hand and foot IK goal curves; the Animator derives the goals when IK is used.
- `root`, the fist nodes (`fist_l`, `fist_r`) and the other joints without a human bone follow their parents as ordinary children. The base skeleton no longer has hand-held `prop_*` anchors.
- A character is expected to be in the base skeleton's T-pose; the importer does not correct a different rest pose.

### Tests

`Tests/Editor/HumanoidImportTests.cs` builds a skeleton in code and checks the bone map, the Avatar, baked and
unbaked channels, offsets and regeneration; `Tests/Editor/MotionIntervalTests.cs` checks the interval reader and the
`MotionIntervals` queries. Run with the EditMode test runner (`Yu5h1Lib.Animation.Editor.Tests`).

## Equipment part import and bone attachment (COMBAT-UNITYEXTENSION-1)

Imports Character Builder equipment parts (`.part.json` and companion `.glb`) into Unity assets, and attaches them to character bones at runtime with optional opposite-side mirroring across the X axis.

Part format and static attachment mathematics follow Character Builder's [Equipment System](file:///W:/Yu5h1Lib/CharacterBuilder/equipment/README.md#零件描述格式) (`yu5h1.part.draft`, version `0.1.0`).

### Import

In Editor, a part is defined by `<id>.part.json` with `<id>.glb` in the same directory.
Importing occurs automatically via `PartPostprocessor` when `.part.json` assets are imported or refreshed in the project, or explicitly via `PartImporter.Import(path)`.

Validation rules:
- Format must be `yu5h1.part.draft`, version `0.1.0`.
- Required fields: `format`, `formatVersion`, `id`, `version`, `kind`, `glb`, and `attach.target` (bone name).
- Optional fields: `attach.offset.position` and `attach.offset.rotation` (XYZ Euler degrees).
- The companion GLB must exist and contain exactly one node named `attach` without mesh or scale.
- Non-compliant descriptions or GLBs log errors to the Console identifying the cause and JSON pointer path (or missing node), producing no asset.

The importer creates or updates a `PartAsset` ScriptableObject (`<id>.asset`) containing metadata, target bone name, offset transform, GLB `attach` node transform, and the model prefab.

### Attachment API

At runtime, `PartAttachment.Attach` (or `partAsset.AttachTo`) instantiates the part and attaches it under the character's target bone:

```csharp
// Attach to default target bone (e.g., wrist_r for sword.arming):
GameObject swordInstance = PartAttachment.Attach(character, swordPart);

// Attach with opposite-side mirroring (e.g., attach sword to left hand wrist_l):
GameObject leftSwordInstance = PartAttachment.Attach(character, swordPart, boneName: "wrist_l", mirror: true);

// Detach part:
PartAttachment.Detach(character, swordPart);
// or directly:
PartAttachment.Detach(swordInstance);
```

### Coordinate conversion, mirroring, and limitations

- **Coordinate conversion**: Character Builder uses glTF coordinates (right-handed, Y-up, -Z-forward) and intrinsic XYZ Euler rotations ($R = R_x \cdot R_y \cdot R_z$). glTFast and this package map to Unity (left-handed) by negating the X axis (`x_unity = -x_gltf`, Euler angles `y` and `z` negated). Rotations are evaluated using intrinsic XYZ order (`PartAsset.EulerXYZ`). In bind pose, the world position and orientation match Character Builder `partWorldMatrix` within 1 mm.
- **Bone following**: The part GameObject is parented under the target bone Transform with fixed local transform $T_{local} = T_{offset} \times T_{attach}^{-1}$. As bones animate, the part follows naturally without per-frame script updates.
- **Mirroring**: When attaching to the opposite side (`mirror = true`), the offset position X component is negated, and offset rotation Y and Z Euler angles are negated (X angle unchanged). The part mesh itself is not mirrored, ensuring sword blades continue to point forward (+Z).
- **Missing bone**: If the target bone is not found in the character hierarchy, an error containing the bone name is logged, and no GameObject is instantiated.
- **Limitations (Round 1)**: Covers static rigid attachments only. Skinned parts with internal bone chains (e.g. hair ponytails, pleated skirts), dynamic secondary sway (`attach.sway`), character body-shape scaling, and equipment slot management are deferred to later rounds.

### Tests

- `Tests/Editor/PartImportTests.cs`: EditMode tests checking valid asset import (`sword.arming`, `helmet.kettle`, `sword.sheathed`), multi-axis XYZ Euler rotation validation against Character Builder `partWorldMatrix`, real-model attachment against `punch-character.glb`, and error logging / non-generation for invalid parts (missing target, bad version, missing attach node).
- `Tests/Runtime/PartAttachmentPlayModeTests.cs`: PlayMode tests validating bind-pose world positions within 1 mm, multi-axis XYZ Euler orientation matching `partWorldMatrix`, constant local transform across bone movements, left-hand mirroring, clean detachment, and missing bone rejection.

