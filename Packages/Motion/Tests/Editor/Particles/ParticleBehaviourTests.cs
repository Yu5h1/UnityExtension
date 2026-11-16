using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Yu5h1Lib.Tests
{
    /// <summary>
    ///   Platform check PEXT-CHECK-12 of ParticleExtensions 1.2.0: one case per behaviour kernel, each
    ///   with expected values derived from the formulas of Unified-Solver's SolverParticleModifiers.compute
    ///   (the C# below is a transcription of those formulas for one body, not of the code under test),
    ///   plus the kernel order and the keep-awake rule. Constraints are made so soft that the solver
    ///   leaves a body's shape to the behaviour under test.
    /// </summary>
    public sealed class ParticleBehaviourTests
    {
        const float Dt = ParticleBodyTestKit.Dt;
        static readonly Vector3 Dimensions = new Vector3(0.12f, 0.4f, 0.08f);

        ParticleBodyTestKit _kit;

        [SetUp]
        public void SetUp()
        {
            if (!SystemInfo.supportsComputeShaders)
                Assert.Ignore("Compute shaders are not supported on this device.");
            _kit = new ParticleBodyTestKit();
        }

        [TearDown]
        public void TearDown() => _kit?.Dispose();

        ParticleEmitter Emitter(ParticleWorld world, ParticleBodyTopology topology, ParticleBehaviourProfile behaviour, float radius = 0.01f)
        {
            ParticleBodyLayout layout = _kit.Layout(topology, Dimensions);
            ParticleBodyInteraction interaction = _kit.Interaction(1f, radius, 1f, 1f, 0f);
            return _kit.Emitter(world, layout, interaction, behaviour);
        }

        static float Hash01(uint value)
        {
            unchecked
            {
                value ^= value >> 16;
                value *= 0x7feb352du;
                value ^= value >> 15;
                value *= 0x846ca68bu;
                value ^= value >> 16;
                return (value & 0x00ffffffu) / 16777215f;
            }
        }

        static Vector3 Velocity(ParticleWorld world, ParticleEntity entity, int index = 0) =>
            ParticleBodyTestKit.Read(world, entity).Velocities[index];

        [Test]
        public void SpeedLimitDecaysTheExcessExponentially()
        {
            ParticleWorld world = _kit.World();
            ParticleBehaviourProfile behaviour = _kit.Behaviour(speedLimit: 2f);
            behaviour.SpeedDecayRate = 10f;
            ParticleEmitter emitter = Emitter(world, ParticleBodyTopology.Single, behaviour);
            var bodies = _kit.Spawn(emitter,
                ParticleBodyTestKit.At(Vector3.zero, new Vector3(10f, 0f, 0f)),
                ParticleBodyTestKit.At(new Vector3(0f, 0f, 5f), new Vector3(1f, 0f, 0f)));

            float expected = 10f;
            for (int step = 1; step <= 3; step++)
            {
                _kit.Frame(world, emitter);
                expected = 2f + (expected - 2f) * Mathf.Exp(-10f * Dt);
                Assert.AreEqual(expected, Velocity(world, bodies[0]).x, 2e-3f, $"fast body after step {step}");
                Assert.AreEqual(1f, Velocity(world, bodies[1]).x, 1e-3f, "a body below the limit is left alone");
            }
        }

        [Test]
        public void SpeedLimitOffLeavesSpeedAlone()
        {
            ParticleWorld world = _kit.World();
            ParticleEmitter emitter = Emitter(world, ParticleBodyTopology.Single, _kit.Behaviour(speedLimit: 0f));
            var bodies = _kit.Spawn(emitter, ParticleBodyTestKit.At(Vector3.zero, new Vector3(10f, 0f, 0f)));
            _kit.Frame(world, emitter, 3);
            Assert.AreEqual(10f, Velocity(world, bodies[0]).x, 1e-3f);
        }

        static float RollRate(ParticleReadback body)
        {
            float numerator = 0f;
            float inertia = 0f;
            for (int pair = 0; pair < 3; pair++)
            {
                Vector3 a = body.Positions[pair * 2];
                Vector3 b = body.Positions[pair * 2 + 1];
                Vector3 center = (a + b) * 0.5f;
                Vector3 meanVelocity = (body.Velocities[pair * 2] + body.Velocities[pair * 2 + 1]) * 0.5f;
                for (int i = 0; i < 2; i++)
                {
                    Vector3 r = body.Positions[pair * 2 + i] - center;
                    Vector3 perpendicular = new Vector3(r.x, 0f, r.z);
                    numerator += Vector3.Dot(Vector3.Cross(perpendicular, body.Velocities[pair * 2 + i] - meanVelocity), Vector3.up);
                    inertia += perpendicular.sqrMagnitude;
                }
            }
            return numerator / inertia;
        }

        [Test]
        public void RollDampingRemovesTheConfiguredShareOfTheSpin()
        {
            ParticleWorld world = _kit.World();
            var rates = new Dictionary<float, float>();
            foreach (float strength in new[] { 0f, 0.25f, 1f })
            {
                ParticleEmitter emitter = Emitter(world, ParticleBodyTopology.DualRail6, _kit.Behaviour(rollDamping: strength));
                ParticleBodySpawn spawn = ParticleBodyTestKit.At(new Vector3(rates.Count * 10f, 0f, 0f));
                spawn.angularVelocity = new Vector3(0f, 3f, 0f);
                ParticleEntity body = _kit.Spawn(emitter, spawn)[0];
                _kit.Frame(world, emitter);
                rates[strength] = RollRate(ParticleBodyTestKit.Read(world, body));
            }
            Assert.AreEqual(3f, rates[0f], 0.06f, "without damping the spin is what was given");
            Assert.AreEqual(0.75f, rates[0.25f] / rates[0f], 0.01f, "a quarter of the spin is removed");
            Assert.AreEqual(0f, rates[1f] / rates[0f], 0.02f, "full damping removes it all");
        }

        [Test]
        public void ARollingGuideChainLosesItsOrbitingGuide()
        {
            ParticleWorld world = _kit.World();
            ParticleEmitter free = Emitter(world, ParticleBodyTopology.GuideChain4, _kit.Behaviour(rollDamping: 0f));
            ParticleEmitter damped = Emitter(world, ParticleBodyTopology.GuideChain4, _kit.Behaviour(rollDamping: 0.5f));
            ParticleBodySpawn a = ParticleBodyTestKit.At(Vector3.zero);
            a.angularVelocity = new Vector3(0f, 3f, 0f);
            ParticleBodySpawn b = ParticleBodyTestKit.At(new Vector3(10f, 0f, 0f));
            b.angularVelocity = new Vector3(0f, 3f, 0f);
            ParticleEntity freeBody = _kit.Spawn(free, a)[0];
            ParticleEntity dampedBody = _kit.Spawn(damped, b)[0];
            _kit.Frame(world, free);

            float freeSpeed = ParticleBodyTestKit.Read(world, freeBody).Velocities[3].magnitude;
            float dampedSpeed = ParticleBodyTestKit.Read(world, dampedBody).Velocities[3].magnitude;
            Assert.AreEqual(3f * 0.06f, freeSpeed, 0.01f, "the guide orbits at its spawn speed");
            Assert.AreEqual(0.5f, dampedSpeed / freeSpeed, 0.05f, "half of the orbit speed is removed");
        }

        [Test]
        public void ChainEndsFoldedOntoEachOtherAreOpenedAtTheConfiguredPace()
        {
            ParticleWorld world = _kit.World();
            ParticleEmitter emitter = Emitter(world, ParticleBodyTopology.Chain3, _kit.Behaviour());
            var bodies = _kit.Spawn(emitter, ParticleBodyTestKit.At(Vector3.zero), ParticleBodyTestKit.At(new Vector3(0f, 0f, 5f)));
            _kit.Frame(world, emitter);
            _kit.Place(world, bodies[0], new[] { new Vector3(0.025f, 0f, 0f), new Vector3(0f, 0.2f, 0f), new Vector3(-0.025f, 0f, 0f) });
            _kit.Place(world, bodies[1], new[] { new Vector3(0.2f, 0f, 5f), new Vector3(0f, 0f, 5f), new Vector3(-0.2f, 0f, 5f) });
            _kit.Frame(world, emitter);

            Vector3[] folded = ParticleBodyTestKit.Read(world, bodies[0]).Positions;
            float opened = 0.3f * Dt;
            ParticleBodyTestKit.AreClose(new Vector3(0.025f + opened, 0f, 0f), folded[0], 3e-4f, "head moves along the chord");
            ParticleBodyTestKit.AreClose(new Vector3(0f, 0.2f, 0f), folded[1], 3e-4f, "the middle stays");
            ParticleBodyTestKit.AreClose(new Vector3(-0.025f - opened, 0f, 0f), folded[2], 3e-4f, "tail moves the other way");
            foreach (Vector3 velocity in ParticleBodyTestKit.Read(world, bodies[0]).Velocities)
                Assert.That(velocity.magnitude, Is.LessThan(0.02f), "the correction adds no momentum");

            Vector3[] straight = ParticleBodyTestKit.Read(world, bodies[1]).Positions;
            ParticleBodyTestKit.AreClose(new Vector3(0.2f, 0f, 5f), straight[0], 3e-4f, "a body that is not folded is left alone");
            ParticleBodyTestKit.AreClose(new Vector3(-0.2f, 0f, 5f), straight[2], 3e-4f, "a body that is not folded is left alone");
        }

        static Vector3[] OscillationDeltas(ParticleOscillationProfile p, uint index, float time, Vector3[] pose, out float wave)
        {
            const float TwoPi = 6.28318530718f;
            float phaseRandom = Hash01(index * 3u + 1u);
            float frequencyRandom = Hash01(index * 3u + 2u);
            float directionRandom = Hash01(index * 3u + 3u) * 2f - 1f;
            float bendRandom = Hash01(index * 5u + 4u);
            float durationRandom = Hash01(index * 13u + 11u);
            float frequencyScale = Mathf.Lerp(1f - p.frequencyRandomness, 1f + p.frequencyRandomness, frequencyRandom);
            float tensionScale = Mathf.Lerp(1f - p.tensionRandomness, 1f + p.tensionRandomness, bendRandom);
            float muscleTension = Mathf.Clamp01(1f - (1f - p.muscleTension) * tensionScale);
            float peakHalfAngle = Mathf.Min(Mathf.Asin(Mathf.Clamp01(1f - muscleTension)), 1.2217f);
            float headPeak = Mathf.Min(peakHalfAngle * 2f * (1f - p.tailBias), 1.2217f);
            float tailPeak = Mathf.Min(peakHalfAngle * 2f * p.tailBias, 1.2217f);
            float burstFrequency = p.frequency * frequencyScale;
            float durationScale = Mathf.Lerp(1f - p.durationRandomness, 1f + p.durationRandomness, durationRandom);
            float duration = Mathf.Max(0f, p.duration * durationScale);
            float burstFraction = Mathf.Clamp01(duration * burstFrequency);

            wave = 0f;
            float playing = 0f;
            if (burstFraction > 1e-5f)
            {
                float cyclePhase = time * burstFrequency + phaseRandom;
                cyclePhase -= Mathf.Floor(cyclePhase);
                if (cyclePhase < burstFraction)
                {
                    wave = Mathf.Sin(cyclePhase / burstFraction * TwoPi);
                    playing = 1f;
                }
            }

            Vector3 head = pose[0], middle = pose[1], tail = pose[2];
            float bodyLength = (head - middle).magnitude + (middle - tail).magnitude;
            Vector3 tangent = (head - tail).normalized;
            Vector3 side = Vector3.right;
            Vector3 normal = Vector3.forward;
            float angle = (p.directionAngle + directionRandom * p.directionRandomness) * Mathf.Deg2Rad;
            Vector3 direction = (side * Mathf.Cos(angle) + normal * Mathf.Sin(angle)).normalized;

            float headAngle = headPeak * wave;
            float tailAngle = tailPeak * wave;
            float segment = bodyLength * 0.5f;
            Vector3 headOffset = segment * (tangent * Mathf.Cos(headAngle) + direction * Mathf.Sin(headAngle));
            Vector3 tailOffset = -segment * (tangent * Mathf.Cos(tailAngle) - direction * Mathf.Sin(tailAngle));
            Vector3 center = (head + middle + tail) / 3f;
            Vector3 offsetMean = (headOffset + tailOffset) / 3f;
            float drive = p.vitality * (1f - p.stiffness) * playing;
            var deltas = new[]
            {
                (center + headOffset - offsetMean - head) * drive,
                (center - offsetMean - middle) * drive,
                (center + tailOffset - offsetMean - tail) * drive
            };

            if (duration > 1e-5f)
            {
                float maximumStep = TwoPi * segment * Mathf.Max(headPeak, tailPeak) / duration * Dt;
                float longest = deltas.Max(d => d.magnitude);
                if (longest > maximumStep && longest > 1e-9f)
                    for (int i = 0; i < 3; i++)
                        deltas[i] *= maximumStep / longest;
            }

            float maximumDrop = drive * 3f * (Dt / 3f);
            float[] rises = deltas.Select(d => Vector3.Dot(d, Vector3.up)).ToArray();
            float deepest = -rises.Min();
            if (deepest > maximumDrop && deepest > 1e-9f)
                for (int i = 0; i < 3; i++)
                    deltas[i] += Vector3.up * (rises[i] * maximumDrop / deepest - rises[i]);
            return deltas;
        }

        [Test]
        public void OscillationBendsTheBodyByTheKernelsPoseAndMovesNothingOnAverage()
        {
            ParticleWorld world = _kit.World();
            var oscillation = _kit.Profile<ParticleOscillationProfile>();
            oscillation.tensionRandomness = 0.15f;
            oscillation.frequency = 1f;
            oscillation.duration = 1f;
            ParticleBehaviourProfile behaviour = _kit.Behaviour();
            ParticleEmitter emitter = Emitter(world, ParticleBodyTopology.Chain3, behaviour);
            var bodies = _kit.Spawn(emitter, Enumerable.Range(0, 8).Select(i => ParticleBodyTestKit.At(new Vector3(i * 2f, 0f, 0f))).ToArray());
            _kit.Frame(world, emitter);
            Vector3[][] before = bodies.Select(b => ParticleBodyTestKit.Read(world, b).Positions).ToArray();

            behaviour.Modifiers = new ParticleBehaviourModifier[] { oscillation };
            _kit.Frame(world, emitter);
            float time = emitter.Runner.Time;

            float strongest = 0f;
            for (int i = 0; i < bodies.Count; i++)
            {
                Vector3[] after = ParticleBodyTestKit.Read(world, bodies[i]).Positions;
                Vector3[] expected = OscillationDeltas(oscillation, (uint)i, time, before[i], out float wave);
                strongest = Mathf.Max(strongest, Mathf.Abs(wave));
                Vector3 sum = Vector3.zero;
                for (int p = 0; p < 3; p++)
                {
                    ParticleBodyTestKit.AreClose(before[i][p] + expected[p], after[p], 3e-4f, $"body {i} particle {p}");
                    sum += after[p] - before[i][p];
                }
                Assert.That(sum.magnitude, Is.LessThan(3e-4f), $"body {i}: the displacements sum to zero");
            }
            Assert.Greater(strongest, 0.2f, "at least one body is in the middle of its beat, so the case is not vacuous");
        }

        [Test]
        public void OscillationAtFullStiffnessOrZeroVitalityMovesNothing()
        {
            ParticleWorld world = _kit.World();
            var stiff = _kit.Profile<ParticleOscillationProfile>();
            stiff.stiffness = 1f;
            stiff.frequency = 1f;
            var limp = _kit.Profile<ParticleOscillationProfile>();
            limp.vitality = 0f;
            limp.frequency = 1f;
            ParticleBehaviourProfile behaviour = _kit.Behaviour();
            ParticleEmitter emitter = Emitter(world, ParticleBodyTopology.Chain3, behaviour);
            var bodies = _kit.Spawn(emitter, Enumerable.Range(0, 4).Select(i => ParticleBodyTestKit.At(new Vector3(i * 2f, 0f, 0f))).ToArray());
            _kit.Frame(world, emitter);
            Vector3[][] before = bodies.Select(b => ParticleBodyTestKit.Read(world, b).Positions).ToArray();
            behaviour.Modifiers = new ParticleBehaviourModifier[] { stiff, limp };
            _kit.Frame(world, emitter, 3);
            for (int i = 0; i < bodies.Count; i++)
            {
                Vector3[] after = ParticleBodyTestKit.Read(world, bodies[i]).Positions;
                for (int p = 0; p < 3; p++)
                    ParticleBodyTestKit.AreClose(before[i][p], after[p], 3e-4f, $"body {i} particle {p}");
            }
        }

        [Test]
        public void SurfaceImpulseRaisesUpwardSpeedToTheLaunchSpeedInsideItsWindow()
        {
            const int Count = 40;
            ParticleWorld world = _kit.World();
            var surface = _kit.Profile<ParticleSurfaceImpulseProfile>();
            ParticleBehaviourProfile behaviour = _kit.Behaviour();
            ParticleEmitter emitter = Emitter(world, ParticleBodyTopology.Single, behaviour);
            var spawns = new List<ParticleBodySpawn>();
            for (int i = 0; i < Count; i++)
                spawns.Add(ParticleBodyTestKit.At(new Vector3(i * 2f, 0f, 0f)));
            for (int i = 0; i < Count; i++)
                spawns.Add(ParticleBodyTestKit.At(new Vector3(i * 2f, 0f, 5f), new Vector3(0f, -2f, 0f)));
            var bodies = _kit.Spawn(emitter, spawns.ToArray());
            _kit.Frame(world, emitter);

            behaviour.Modifiers = new ParticleBehaviourModifier[] { surface };
            _kit.Frame(world, emitter);
            float time = emitter.Runner.Time;
            float spread = Mathf.Max(Dt, Mathf.Min(Mathf.Max(1f, surface.impactSpread) * Dt, 1f / Mathf.Max(1e-4f, surface.frequency) * 0.9f));

            int fired = 0;
            for (int i = 0; i < Count; i++)
            {
                float random = Hash01((uint)(i * 2 + 5));
                float scale = Mathf.Lerp(1f - surface.frequencyRandomness, 1f + surface.frequencyRandomness, random);
                float angular = surface.frequency * scale * 6.28318530718f;
                float phase = time * angular + random * 6.28318530718f;
                float sinceHop = (phase - 6.28318530718f * Mathf.Floor(phase / 6.28318530718f)) / angular;
                bool fires = sinceHop < spread;
                float expected = fires ? surface.impulseSpeed : 0f;
                fired += fires ? 1 : 0;
                Assert.AreEqual(expected, Velocity(world, bodies[i]).y, 2e-3f, $"resting body {i} (since hop {sinceHop:F3} s, window {spread:F3} s)");
            }
            Assert.That(fired, Is.InRange(1, Count - 1), "some bodies hop and some wait, so the window is exercised");
            for (int i = Count; i < 2 * Count; i++)
                Assert.AreEqual(-2f, Velocity(world, bodies[i]).y, 2e-3f, $"falling body {i - Count} is not launched");
        }

        ParticleVolume Water(string name, Vector3 position, Vector3 size, ParticleWorld world, float density, Vector3 flow = default, Quaternion? rotation = null, bool local = false)
        {
            ParticleMediumEffect medium = _kit.Medium(density, flow, local);
            return _kit.Volume(name, position, size, rotation, world, true, medium);
        }

        static float NeutralDensity(float bodyMass, int particles, float radius) =>
            bodyMass / (particles * 4f / 3f * Mathf.PI * radius * radius * radius);

        [Test]
        public void LocomotionMeanVelocityConvergesOnTheTargetSpeed()
        {
            ParticleWorld world = _kit.World();
            Water("Water", Vector3.zero, new Vector3(40f, 40f, 40f), world, NeutralDensity(1f, 3, 0.01f));
            _kit.Target(new Vector3(100f, 0f, 0f), Quaternion.LookRotation(Vector3.right), ParticleMotionTargetMode.Direction, 0f, world);
            var swim = _kit.Profile<ParticleLocomotionProfile>();
            swim.speed = 1.5f;
            swim.mediumThrust = 1f;
            swim.frequency = 1f;
            swim.duration = 1f;
            swim.randomness = 0f;
            swim.headingSpread = 0f;
            swim.turnRate = 0f;
            swim.uprightRate = 0f;
            ParticleEmitter emitter = Emitter(world, ParticleBodyTopology.Chain3, _kit.Behaviour(0f, 0.25f, 0f, swim));
            ParticleEntity body = _kit.Spawn(emitter, ParticleBodyTestKit.At(Vector3.zero))[0];

            float blend = 1f - Mathf.Exp(-3f / swim.duration * Dt);
            float expected = 0f;
            for (int step = 1; step <= 12; step++)
            {
                _kit.Frame(world, emitter);
                expected = expected * Mathf.Exp(-Dt);
                expected += (swim.speed - expected) * blend;
                Vector3 mean = ParticleBodyTestKit.Mean(ParticleBodyTestKit.Read(world, body).Velocities);
                Assert.AreEqual(expected, mean.x, 2e-3f, $"mean speed after step {step}");
                Assert.That(Mathf.Abs(mean.y) + Mathf.Abs(mean.z), Is.LessThan(2e-3f), "no sideways motion");
            }
        }

        [Test]
        public void LocomotionHeadsForAPointAndOnlyForTargetsThatReachIt()
        {
            ParticleWorld world = _kit.World();
            Water("Water", Vector3.zero, new Vector3(40f, 40f, 40f), world, NeutralDensity(1f, 1, 0.01f));
            _kit.Target(new Vector3(0f, 0f, 10f), Quaternion.identity, ParticleMotionTargetMode.Point, 0f, world);
            _kit.Target(new Vector3(-20f, 0f, 0f), Quaternion.identity, ParticleMotionTargetMode.Point, 1f, world);
            var swim = _kit.Profile<ParticleLocomotionProfile>();
            swim.frequency = 1f;
            swim.duration = 1f;
            swim.randomness = 0f;
            swim.headingSpread = 0f;
            ParticleEmitter emitter = Emitter(world, ParticleBodyTopology.Single, _kit.Behaviour(0f, 0.25f, 0f, swim));
            ParticleEntity body = _kit.Spawn(emitter, ParticleBodyTestKit.At(Vector3.zero))[0];
            _kit.Frame(world, emitter, 5);
            Vector3 velocity = Velocity(world, body);
            Assert.Greater(velocity.z, 0.05f, "the body heads for the point target at +z");
            Assert.That(Mathf.Abs(velocity.x), Is.LessThan(1e-3f), "the target at -x has a radius of 1 m and does not reach it");
        }

        [Test]
        public void LocomotionNeedsAMediumToPushAgainst()
        {
            ParticleWorld world = _kit.World();
            _kit.Target(new Vector3(100f, 0f, 0f), Quaternion.identity, ParticleMotionTargetMode.Point, 0f, world);
            var swim = _kit.Profile<ParticleLocomotionProfile>();
            swim.frequency = 1f;
            swim.duration = 1f;
            ParticleEmitter emitter = Emitter(world, ParticleBodyTopology.Single, _kit.Behaviour(0f, 0.25f, 0f, swim));
            ParticleEntity body = _kit.Spawn(emitter, ParticleBodyTestKit.At(Vector3.zero))[0];
            _kit.Frame(world, emitter, 5);
            Assert.That(Velocity(world, body).magnitude, Is.LessThan(1e-4f), "out of water the stroke has nothing to push against");
        }

        [Test]
        public void MediumStateReportsSubmersionDriftLiftAndDensityRatio()
        {
            ParticleWorld world = _kit.World(gravity: new Vector3(0f, -9.81f, 0f));
            float density = NeutralDensity(1f, 3, 0.01f);
            Water("Water", new Vector3(0f, -5f, 0f), new Vector3(20f, 10f, 20f), world, density, new Vector3(1f, 0f, 0f));
            ParticleEmitter emitter = Emitter(world, ParticleBodyTopology.Chain3, _kit.Behaviour());
            ParticleEntity body = _kit.Spawn(emitter, ParticleBodyTestKit.At(new Vector3(0f, -0.01f, 0f)))[0];
            _kit.Frame(world, emitter);

            Vector4 state = ParticleBodyTestKit.Read<Vector4>(emitter.Runner.MediumStateBuffer, 1)[0];
            ParticleReadback read = ParticleBodyTestKit.Read(world, body);
            Vector3 drift = Vector3.zero;
            for (int i = 1; i < 3; i++)
                drift += new Vector3(1f, 0f, 0f) - read.Velocities[i];
            Assert.AreEqual(2f / 3f, state.x, 1e-5f, "two of three particles are under the surface");
            Assert.AreEqual((drift / 3f).magnitude, state.y, 2e-3f, "drift is the flow minus the velocity after the medium acted");
            Assert.AreEqual(-9.81f / 3f, state.z, 2e-3f, "lift: the dry particle falls, the wet ones are neutral");
            Assert.AreEqual(2f / 3f, state.w, 1e-4f, "mean density ratio over the body");
        }

        [Test]
        public void KernelsRunInTheOrderOfTheModifierRunner()
        {
            ParticleWorld world = _kit.World();
            var swim = _kit.Profile<ParticleLocomotionProfile>();
            var beat = _kit.Profile<ParticleOscillationProfile>();
            var hop = _kit.Profile<ParticleSurfaceImpulseProfile>();
            hop.Enabled = false;
            var beat2 = _kit.Profile<ParticleOscillationProfile>();
            ParticleBehaviourProfile behaviour = _kit.Behaviour(0.04f, 0.25f, 3f, swim, beat, hop, beat2);
            ParticleEmitter emitter = Emitter(world, ParticleBodyTopology.Chain3, behaviour);
            _kit.Spawn(emitter, ParticleBodyTestKit.At(Vector3.zero));

            var log = new List<string>();
            emitter.Runner.onKernel = log.Add;
            _kit.Frame(world, emitter);
            CollectionAssert.AreEqual(
                new[] { "ApplySpeedLimit", "ApplyMediumState", "ApplyRollDamping", "ApplyLocomotion", "ApplyOscillation", "ApplyOscillation", "ApplyBounds", "ApplySleep" },
                log);

            behaviour.SpeedLimit = 0f;
            behaviour.SleepSpeed = 0f;
            behaviour.Modifiers = new ParticleBehaviourModifier[] { beat, swim };
            log.Clear();
            _kit.Frame(world, emitter);
            CollectionAssert.AreEqual(
                new[] { "ApplyMediumState", "ApplyRollDamping", "ApplyOscillation", "ApplyLocomotion", "ApplyBounds" },
                log, "no speed limit without a limit, no sleep without a sleep speed, and the list order decides the behaviours");
        }

        [Test]
        public void ThePostStepExtensionDeclaresPositionAndVelocityWrites()
        {
            ParticleWorld world = _kit.World();
            ParticleEmitter emitter = Emitter(world, ParticleBodyTopology.Chain3, _kit.Behaviour());
            _kit.Spawn(emitter, ParticleBodyTestKit.At(Vector3.zero));
            _kit.Frame(world, emitter);
            Assert.AreEqual(1, world.Extensions.OfType<ParticleBodyRunner>().Count(), "one extension per emitter");
            ParticleBodyRunner runner = world.Extensions.OfType<ParticleBodyRunner>().Single();
            Assert.AreEqual(ParticleExtensionStage.PostStep, runner.Stage);
            Assert.AreEqual(ParticleExtensionWrites.Positions | ParticleExtensionWrites.Velocities, runner.Writes);
        }
    }
}
