# Unity GPU rendering and compute

Use this skill for GPU-driven rendering and simulation in Yu5h1Lib Unity work: procedural instancing, ComputeBuffer ownership, shader variants, and sharing data between C#, `.compute` and `.shader`.

## Scope

- Procedural instanced drawing (`Graphics.DrawMeshInstancedProcedural`) and its per-draw data.
- `ComputeBuffer` creation, growth and release.
- Struct layout shared across C#, compute shaders and vertex shaders.
- Reading state out of a vendored or third-party solver without modifying it.

Not in scope: Inspector and Editor extensions ([editor-tooling.md](editor-tooling.md)), ScriptableObject data modeling ([data-architecture.md](data-architecture.md)).

## Instanced drawing

- `DrawMeshInstancedProcedural` takes exactly one mesh per call. Instances whose geometry differs in **topology** cannot share a call, so group them and issue one call per group.
- The shader receives a **batch-local** `SV_InstanceID`, not a global index. Any call that draws a subset needs an index buffer plus an offset uniform to map it back:

  ```hlsl
  uint index = (uint)_GroupInstances[(uint)_GroupOffset + instanceID];
  ```

  Keep that mapping on the component that owns the instances, since it is the only thing that knows which instance went into which group.
- Set per-draw values through a `MaterialPropertyBlock`, not on the material, or concurrent draws overwrite each other.
- `MaterialPropertyBlock.SetInt` is backed by a float. Reading it as `int` in HLSL is fine for small integers and is the established pattern here; do not switch to `SetInteger` in one place only.
- Pass draw bounds large enough to cover where instances actually travel. Culling uses those bounds, not the instances.

## Shader variants over shader forks

- When two draws differ only in **where the vertex comes from**, add a `#pragma multi_compile_local _ MY_KEYWORD` branch to the existing shader instead of writing a second one. Lighting, fragment, ShadowCaster and material properties then stay shared, and a fix to one is a fix to both.
- Put the branch in the one function both passes call, so the ShadowCaster cannot silently diverge from the visible pass.
- Toggle with `Material.EnableKeyword` / `DisableKeyword` when the runtime material is built, not per frame.

## Vertex channels

- Carry integer payloads in **UV channels**, never in `NORMAL`. A normal is semantically a direction and mesh tooling is entitled to renormalise one; nothing ever rewrites a UV. `Mesh.SetUVs(channel, Vector3[])` gives three floats per vertex.
- `appdata_base` has POSITION, NORMAL and **one** texcoord. A ShadowCaster pass that needs more must declare its own struct. `TRANSFER_SHADOW_CASTER_NORMALOFFSET` only requires fields named `vertex` and `normal` to exist, so a custom struct works.
- Flat shading of procedural geometry needs one vertex per face, not per corner. Give each vertex its face's three source indices with its own first, and compute the facet normal in the vertex shader:

  ```hlsl
  float3 n = normalize(cross(second - own, third - own));
  ```

  Rotate the triple per vertex rather than reordering it, or the winding, and therefore the normal direction, changes between the three.

## Shared struct layout

- A struct used by C#, `.compute` and `.shader` is declared **three times** and nothing checks that they agree. A mismatch is silent and shows as garbled data, not as an error.
- Keep an explicit stride constant next to the C# struct and construct the `ComputeBuffer` from it, so the layout has one stated source of truth.
- Prefer reusing an existing padding field over appending a new one when a struct needs another value; appending changes the stride and every copy of the declaration.
- Put shared HLSL in an `.hlsl` include used by both the compute and the vertex shader. Two implementations of the same math will diverge, and when one drives physics and the other drives skinning, the result is a body that simulates in one frame and is drawn in another.

## ComputeBuffer ownership

- Release every buffer in `OnDestroy`. A leaked `ComputeBuffer` survives play-mode exit and logs on domain reload.
- Grow by reallocating when the required count exceeds `buffer.count`, releasing the old one first. Never allocate per frame.
- Rebuild contents from a dirty flag set by whoever mutates the source, not by comparing counts every frame.

## Reading a vendored solver

- Reach private fields of a read-only dependency through a reflection bridge in one file, never inline at the call site. Resolve `FieldInfo` once in a static initialiser and check the field type, not just the name.
- Fail **closed**: if the contract does not resolve, return false and let the caller skip the feature. Do not fall back to a guess.
- Keep one availability flag per feature rather than one for the whole bridge, so a renamed field costs only the feature that needed it.
- Record the exact field names and types the bridge depends on in the owning `handoff.md`, and add them to the IL2CPP `link.xml`, or the build strips what only reflection uses.
- Before adding data of your own, check what the dependency already stores. A solver that does shape matching already keeps rest offsets; a renderer that needs local geometry can read those instead of duplicating them.

## Hot paths

- Spawn and per-frame paths must not allocate. Preallocate one scratch array per size you actually use rather than slicing a single large one — an API that sizes its work from `array.Length` will silently read the slots you did not fill.
