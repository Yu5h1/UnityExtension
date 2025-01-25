using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using static UnityEngine.ParticleSystem;

namespace Yu5h1Lib
{
    [EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
    public static class ParticleSystemEx
    {
        public static MinMaxCurve Multiply(this ParticleSystem.MinMaxCurve curve, float multiplier)
        {
            MinMaxCurve newCurve = new ParticleSystem.MinMaxCurve
            {
                mode = curve.mode
            };

            switch (curve.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    newCurve.constant = curve.constant * multiplier;
                    break;

                case ParticleSystemCurveMode.TwoConstants:
                    newCurve.constantMin = curve.constantMin * multiplier;
                    newCurve.constantMax = curve.constantMax * multiplier;
                    break;

                case ParticleSystemCurveMode.Curve:
                    newCurve.curve = curve.curve; // 保留原始曲線
                    newCurve.curveMultiplier = curve.curveMultiplier * multiplier;
                    break;

                case ParticleSystemCurveMode.TwoCurves:
                    newCurve.curveMin = curve.curveMin; // 保留原始曲線
                    newCurve.curveMax = curve.curveMax; // 保留原始曲線
                    newCurve.curveMultiplier = curve.curveMultiplier * multiplier;
                    break;
            }

            return newCurve;
        }

        public static void ModifyTriggerParticles(this ParticleSystem particleSystem, ParticleSystemTriggerEventType eventType, Func<Particle, Particle> modifier)
        {
            var particles = new List<Particle>();
            int numOfparticles = particleSystem.GetTriggerParticles(eventType, particles);
            for (int i = 0; i < numOfparticles; i++)
                particles[i] = modifier(particles[i]);
            particleSystem.SetTriggerParticles(eventType, particles);
        }
        public static void ModifyParticle(this ParticleSystem particleSystem, ref Particle[] particles, Func<Particle, Particle> modifier)
        {
            if (particles.Length < particleSystem.main.maxParticles)
            {
                Debug.LogWarning($"particles length need to be same as maxParticles");
                return;
            }
            int numParticlesAlive = particleSystem.GetParticles(particles);
            for (int i = 0; i < numParticlesAlive; i++)
                particles[i] = modifier(particles[i]);
            particleSystem.SetParticles(particles, numParticlesAlive);
        }
    } 
}