using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yu5h1Lib;
using Object = UnityEngine.Object;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    ///   Builds the Motion package's copy of Unified-Solver's "test solver mesh" scene into a project
    ///   folder (GPU physics migration plan, stage 3 and 4 verification scene): the 100 x 100 net
    ///   held by five anchors, the animated hand that grabs it, the open box and the floor slab, two
    ///   fish emitters with BonghuoVR's fish profile and their motion targets, the school bounds and
    ///   the sea, which the original keeps disabled, so the fish fall onto the net and boxes. Object
    ///   positions and values copy the original scene; the fish mesh is Unity's capsule because the
    ///   BonghuoVR mesh is not in this package. Profiles, materials, the hand animation and the scene
    ///   are written fresh on every run, so the build can be repeated. Unified-Solver's ice
    ///   generator, hose prefab and splash volume are not copied: their assets are missing in the
    ///   test project and the splash uses a ParticleSystem-shaped volume Motion does not support.
    /// </summary>
    public static class ParticlePhysicsTestScene
    {
        public const string DefaultFolder = "Assets/ParticlePhysicsTest";
        public const string SceneName = "ParticlePhysicsTest.unity";
        const string FolderArgument = "-ptsFolder";
        static readonly List<Object> s_saved = new List<Object>();

        static readonly (string name, int x, int y)[] NetAnchors =
        {
            ("Anchor (6, 95)", 6, 95), ("Anchor (5, 44)", 5, 44), ("Anchor (9, 8)", 9, 8),
            ("Anchor (96, 7)", 96, 7), ("Anchor (97, 97)", 97, 97)
        };

        static readonly (string name, Vector3 position, Quaternion rotation, Vector3 scale)[] BoxWalls =
        {
            ("Floor", new Vector3(0f, -1.25f, 0f), Quaternion.identity, new Vector3(3.5f, 0.5f, 3.5f)),
            ("Wall Back", new Vector3(0f, 0f, -1.65f), new Quaternion(0f, 0.7071068f, 0f, 0.7071068f), new Vector3(0.5f, 3f, 3.75f)),
            ("Wall Left", new Vector3(-1.65f, 0f, 0f), new Quaternion(0f, 1f, 0f, 0f), new Vector3(0.5f, 3f, 3.75f)),
            ("Wall Front", new Vector3(0f, 0f, 1.65f), new Quaternion(0f, 0.7071068f, 0f, -0.7071068f), new Vector3(0.5f, 3f, 3.75f)),
            ("Wall Right", new Vector3(1.65f, 0f, 0f), Quaternion.identity, new Vector3(0.5f, 3f, 3.75f))
        };

        [MenuItem("Tools/Motion/Build Particle Physics Test Scene")]
        static void BuildFromMenu()
        {
            string scene = Build(DefaultFolder);
            EditorSceneManager.OpenScene(scene);
        }

        /// <summary>
        ///   Batchmode entry: builds into the folder after -ptsFolder, or <see cref="DefaultFolder"/>.
        /// </summary>
        public static void BuildFromCommandLine()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            string folder = DefaultFolder;
            int index = Array.IndexOf(arguments, FolderArgument);
            if (index >= 0 && index + 1 < arguments.Length)
                folder = arguments[index + 1];
            Debug.Log($"ParticlePhysicsTestScene: built {Build(folder)}");
        }

        /// <summary>
        ///   Writes the scene and its assets into <paramref name="folder"/> (a project path under
        ///   Assets), replacing what an earlier build left there, and returns the scene path.
        /// </summary>
        public static string Build(string folder)
        {
            folder = folder.TrimEnd('/');
            EnsureFolder(folder);
            s_saved.Clear();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0f, 1f, -10f);
            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.SetPositionAndRotation(new Vector3(0f, 3f, 0f), new Quaternion(0.408217877f, -0.234569684f, 0.109381631f, 0.8754261f));

            ParticleWorld world = BuildWorld();
            Material surface = SaveMaterial(folder, "Surface", FindShader("Universal Render Pipeline/Lit", "Standard"), new Color(0.7f, 0.7f, 0.72f));
            PhysicsParticle net = BuildNet(folder, world);
            BuildHand(folder, net);
            BuildBox(surface);
            BuildSlab(surface);
            BuildFish(folder, world);

            string path = $"{folder}/{SceneName}";
            EditorSceneManager.SaveScene(scene, path);
            foreach (Object saved in s_saved)
                EditorUtility.SetDirty(saved);
            s_saved.Clear();
            AssetDatabase.SaveAssets();
            return path;
        }

        static ParticleWorld BuildWorld()
        {
            var world = new GameObject("particle-world").AddComponent<ParticleWorld>();
            world.Capacity.particles = 16384;
            world.Capacity.constraints = 50000;
            world.Capacity.rigidGroups = 64;
            ParticleSolverSettings settings = world.Settings;
            settings.substeps = 30;
            settings.sor = 1.1f;
            settings.contactWeight = 2f;
            settings.gravity = new Vector3(0f, -9.81f, 0f);
            settings.groundY = -100f;
            settings.damping = 2f;
            settings.frictionStatic = 0.02f;
            settings.frictionKinetic = 0.005f;
            settings.maxDepenetrationSpeed = 5f;
            return world;
        }

        static PhysicsParticle BuildNet(string folder, ParticleWorld world)
        {
            var netObject = new GameObject("net");
            netObject.transform.SetPositionAndRotation(new Vector3(16.9f, 7.43f, 11.33f), new Quaternion(0.7071068f, 0f, 0f, 0.7071068f));

            var layout = Save<GridParticleLayout>(folder, "Net Layout");
            layout.Columns = 100;
            layout.Rows = 100;
            layout.Spacing = 0.1f;
            var cloth = Save<ClothInteraction>(folder, "Net Interaction");
            cloth.ParticleMass = 1f;
            cloth.ParticleRadius = 0.05f;
            cloth.SelfCollision = false;
            cloth.Shear = true;
            cloth.Bend = false;
            cloth.Material.compliance = 1e-7f;
            cloth.Material.damping = 99999f;
            var appearance = Save<GridSurfaceAppearance>(folder, "Net Appearance");
            appearance.Color = new Color(1f, 0f, 0.13966465f, 1f);
            appearance.UseNet = true;
            appearance.DoubleSided = true;
            appearance.Material = SaveMaterial(folder, "Net", Shader.Find("Yu5h1Lib/Particles/GridSurface"), Color.white);

            var net = netObject.AddComponent<PhysicsParticle>();
            net.World = world;
            net.Layout = layout;
            net.Interaction = cloth;
            net.Appearance = appearance;
            net.Count = 1;

            var anchor = netObject.AddComponent<ParticleAnchor>();
            anchor.Source = net;
            float half = (layout.Columns - 1) * layout.Spacing * 0.5f;
            foreach ((string name, int x, int y) in NetAnchors)
            {
                var holder = new GameObject(name).transform;
                holder.SetParent(netObject.transform, false);
                holder.localPosition = new Vector3(x * layout.Spacing - half, y * layout.Spacing - half, 0f);
                anchor.Bindings.Add(new ParticleAnchor.Binding
                {
                    target = holder,
                    points = new List<ParticleAnchor.Point> { new ParticleAnchor.Point { index = y * layout.Columns + x } }
                });
            }
            return net;
        }

        static void BuildHand(string folder, PhysicsParticle net)
        {
            var hand = new GameObject("hand");
            hand.transform.SetPositionAndRotation(new Vector3(24.22f, 10.42f, 1.01f), new Quaternion(0f, 1f, 0f, 0f));
            var grab = new GameObject("grab");
            grab.transform.SetParent(hand.transform, false);
            var grabber = grab.AddComponent<ParticleGrabber>();
            grabber.Target = net;
            grabber.Radius = 0.89f;
            grab.AddComponent<Animator>().runtimeAnimatorController = SaveHandController(folder);
        }

        /// <summary>
        ///   The hand animation of the original scene: the grab point swings 3 m along local X and
        ///   back twice in 2.5 s at a depth of 2.19 m, grabbing at 0.5 s and 1.5 s and releasing at
        ///   1 s and 2 s, looping.
        /// </summary>
        static RuntimeAnimatorController SaveHandController(string folder)
        {
            var clip = new AnimationClip { name = "grab" };
            float[] times = { 0f, 0.5f, 1f, 1.5f, 2f, 2.5f };
            float[] swing = { 0f, 3f, 0f, 3f, 0f, 0f };
            var x = new AnimationCurve();
            var y = new AnimationCurve();
            var z = new AnimationCurve();
            for (int i = 0; i < times.Length; i++)
            {
                x.AddKey(times[i], swing[i]);
                y.AddKey(times[i], 0f);
                z.AddKey(times[i], 2.19f);
            }
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalPosition.x"), x);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalPosition.y"), y);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalPosition.z"), z);
            AnimationUtility.SetAnimationEvents(clip, new[]
            {
                new AnimationEvent { time = 0.5f, functionName = nameof(ParticleGrabber.Grab) },
                new AnimationEvent { time = 1f, functionName = nameof(ParticleGrabber.Release) },
                new AnimationEvent { time = 1.5f, functionName = nameof(ParticleGrabber.Grab) },
                new AnimationEvent { time = 2f, functionName = nameof(ParticleGrabber.Release) }
            });
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            string clipPath = $"{folder}/Hand Grab.anim";
            AssetDatabase.DeleteAsset(clipPath);
            AssetDatabase.CreateAsset(clip, clipPath);
            string controllerPath = $"{folder}/Hand.controller";
            AssetDatabase.DeleteAsset(controllerPath);
            return AnimatorController.CreateAnimatorControllerAtPathWithClip(controllerPath, clip);
        }

        static void BuildBox(Material surface)
        {
            var box = new GameObject("box");
            box.transform.position = new Vector3(7.68f, 6.4f, -2.25f);
            box.transform.localScale = Vector3.one * 0.5f;
            foreach ((string name, Vector3 position, Quaternion rotation, Vector3 scale) in BoxWalls)
            {
                GameObject wall = VisibleBox(name, surface);
                wall.transform.SetParent(box.transform, false);
                wall.transform.SetLocalPositionAndRotation(position, rotation);
                wall.transform.localScale = scale;
            }
        }

        static void BuildSlab(Material surface)
        {
            GameObject slab = VisibleBox("Cube", surface);
            slab.transform.position = new Vector3(8.460078f, -0.34f, 8.3f);
            slab.transform.localScale = new Vector3(10f, 0.5f, 10f);
        }

        static GameObject VisibleBox(string name, Material surface)
        {
            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            go.AddComponent<MeshRenderer>().sharedMaterial = surface;
            go.AddComponent<ParticleBoxCollider>();
            return go;
        }

        static void BuildFish(string folder, ParticleWorld world)
        {
            var volumes = new GameObject("volumes");
            volumes.transform.position = new Vector3(6.8f, 2.91f, 5.4f);

            var sea = Save<ParticleMediumEffect>(folder, "Sea Medium");
            sea.Density = 510f;
            GameObject seaObject = Volume(volumes.transform, "Sea", Vector3.zero, new Vector3(10f, 5f, 10f), world, sea);
            seaObject.SetActive(false);
            var bounds = Save<ParticleBoundsEffect>(folder, "School Bounds");
            bounds.Mode = ParticleBoundsMode.Contain;
            bounds.FadeOut = 0.5f;
            bounds.FadeIn = 0.5f;
            Volume(volumes.transform, "Bound", new Vector3(0f, 2.5f, 0f), new Vector3(39.48f, 12f, 21.8f), world, bounds);

            var layout = Save<ParticleBodyLayout>(folder, "Fish Layout");
            layout.Topology = ParticleBodyTopology.GuideChain4;
            layout.BaseDimensions = new Vector3(0.12f, 0.4f, 0.08f);
            var interaction = Save<ParticleBodyInteraction>(folder, "Fish Interaction");
            interaction.BodyMass = 1f;
            interaction.ParticleRadius = 0.05f;
            interaction.JointCompliance = 0.00002f;
            interaction.BendCompliance = 0.05f;
            interaction.JointDamping = 0.08f;
            var oscillation = Save<ParticleOscillationProfile>(folder, "Fish Oscillation");
            oscillation.acceleration = 0f;
            oscillation.vitality = 0.065f;
            oscillation.directionRandomness = 12f;
            oscillation.muscleTension = 0f;
            oscillation.tensionRandomness = 0.069f;
            oscillation.tailBias = 0.5f;
            oscillation.frequency = 1f;
            oscillation.frequencyRandomness = 0.04f;
            oscillation.duration = 0.333f;
            oscillation.durationRandomness = 0.666f;
            var locomotion = Save<ParticleLocomotionProfile>(folder, "Fish Locomotion");
            locomotion.speed = 10f;
            locomotion.mediumThrust = 0.5f;
            locomotion.frequency = 1f;
            locomotion.duration = 0.3f;
            locomotion.randomness = 1f;
            locomotion.headingSpread = 25f;
            locomotion.turnRate = 180f;
            locomotion.uprightRate = 120f;
            var behaviour = Save<ParticleBehaviourProfile>(folder, "Fish Behaviour");
            behaviour.RollDamping = 1f;
            behaviour.SleepSpeed = 0.05f;
            behaviour.SleepDelay = 0.5f;
            behaviour.WakeDistance = 0.005f;
            behaviour.Modifiers = new ParticleBehaviourModifier[] { oscillation, locomotion };
            var appearance = Save<ArticulatedMeshAppearance>(folder, "Fish Appearance");
            appearance.Mesh = Resources.GetBuiltinResource<Mesh>("New-Capsule.fbx");
            appearance.ForwardAxis = Vector3.up;
            appearance.FitMeshToDimensions = true;
            appearance.Color = new Color(0.65739584f, 0.716242f, 0.745283f, 1f);
            appearance.Material = SaveMaterial(folder, "Fish", Shader.Find("Yu5h1Lib/Particles/ArticulatedMesh"), Color.white);

            foreach ((string name, Vector3 position) in new[] { ("FishGenerator", new Vector3(0f, 2.42f, 1.68f)), ("FishGenerator (1)", new Vector3(11.8f, 6.23f, 1.68f)) })
            {
                var emitterObject = new GameObject(name);
                emitterObject.transform.SetParent(volumes.transform, false);
                emitterObject.transform.localPosition = position;
                var emitter = emitterObject.AddComponent<ParticleEmitter>();
                emitter.World = world;
                emitter.Layout = layout;
                emitter.Interaction = interaction;
                emitter.Behaviour = behaviour;
                emitter.Appearance = appearance;
                emitter.MaxInstances = 512;
                emitter.ShapeSeed = 12345;
                emitter.PlayOnAwake = true;
                emitter.InitialCount = 256;
                emitter.SpawnVolume = new Vector3(5f, 2f, 5f);
                emitter.InitialVelocity = Vector3.zero;
                emitter.VelocityVariation = 0.5f;
                emitter.ColorVariation = 0.15f;
                var target = new GameObject("MotionTarget");
                target.transform.SetParent(emitterObject.transform, false);
                var motionTarget = target.AddComponent<ParticleMotionTarget>();
                motionTarget.World = world;
                motionTarget.Mode = ParticleMotionTargetMode.Direction;
            }
        }

        static GameObject Volume(Transform parent, string name, Vector3 position, Vector3 size, ParticleWorld world, ParticleVolumeEffect effect)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            var volume = go.AddComponent<ParticleVolume>();
            volume.World = world;
            volume.Effects = new[] { effect };
            return go;
        }

        static T Save<T>(string folder, string name) where T : ScriptableObject
        {
            string path = $"{folder}/{name}.asset";
            AssetDatabase.DeleteAsset(path);
            var asset = ScriptableObject.CreateInstance<T>();
            asset.name = name;
            AssetDatabase.CreateAsset(asset, path);
            s_saved.Add(asset);
            return asset;
        }

        static Material SaveMaterial(string folder, string name, Shader shader, Color color)
        {
            string path = $"{folder}/{name}.mat";
            AssetDatabase.DeleteAsset(path);
            var material = new Material(shader) { name = name, enableInstancing = true };
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static Shader FindShader(string preferred, string fallback) => Shader.Find(preferred) ?? Shader.Find(fallback);

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;
            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
