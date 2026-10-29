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

### What is not carried over

- Bone translation (arm stretch and the like): a humanoid clip holds muscles only. Stretching is to be added later as
  an additive Animation Job on the humanoid result.
- Hand and foot IK goal curves; the Animator derives the goals when IK is used.
- Props (`prop_l`, `prop_r`) and `root` have no human bone; they follow their parents as ordinary children.
- A character is expected to be in the base skeleton's T-pose; the importer does not correct a different rest pose.

### Tests

`Tests/Editor/HumanoidImportTests.cs` builds a skeleton in code and checks the bone map, the Avatar, baked and
unbaked channels, offsets and regeneration. Run with the EditMode test runner (`Yu5h1Lib.Animation.Editor.Tests`).
