using System;
using System.Reflection;
using UnityEngine;

namespace Yu5h1.UnifiedSolver
{
    internal static class SolverManagerAccess
    {
        const BindingFlags InstancePrivate =
            BindingFlags.Instance |
            BindingFlags.NonPublic;

        static readonly FieldInfo RigidBodyBufferField =
            ResolveField<ComputeBuffer>(
                "_rigidBodyBuffer");
        static readonly FieldInfo
            RigidParticleIndexBufferField =
                ResolveField<ComputeBuffer>(
                    "_rigidParticleIndexBuffer");
        static readonly FieldInfo
            RigidParticleRefCountField =
                ResolveField<SolverManager, int>(
                    "_rigidParticleRefCount");
        // Rest offsets q_i = x_i0 - x_cm0, flat and indexed in parallel with
        // _rigidParticleIndexBuffer. The solver keeps these for shape matching,
        // which means a fragment's own corner geometry is already on the GPU and
        // the hull renderer needs no shape buffer of its own.
        static readonly FieldInfo
            RigidRestOffsetBufferField =
                ResolveField<ComputeBuffer>(
                    "_rigidRestOffsetBuffer");
        static readonly FieldInfo
            ClothParticleOffsetField =
                ResolveField<ClothGenerator, int>(
                    "_particleOffset");

        static bool _reportedRigidContractFailure;
        static bool _reportedRestOffsetContractFailure;
        static bool _reportedClothContractFailure;
        static bool _reportedAccessFailure;

        internal static bool ContractAvailable =>
            RigidContractAvailable &&
            ClothContractAvailable;

        internal static bool RigidContractAvailable =>
            RigidBodyBufferField != null &&
            RigidParticleIndexBufferField != null &&
            RigidParticleRefCountField != null;

        // Separate from the rigid contract on purpose. Only hull rendering needs
        // rest offsets, so a solver version that renamed this field should cost
        // the hull path and nothing else.
        internal static bool RigidRestOffsetsAvailable =>
            RigidRestOffsetBufferField != null;

        internal static bool ClothContractAvailable =>
            ClothParticleOffsetField != null;

        internal static string ContractDescription =>
            "SolverManager private fields " +
            "'_rigidBodyBuffer' (ComputeBuffer), " +
            "'_rigidParticleIndexBuffer' " +
            "(ComputeBuffer), and " +
            "'_rigidParticleRefCount' (Int32); " +
            "ClothGenerator private fields " +
            "'_particleOffset' (Int32)";

        internal static bool TryGetRigidBuffers(
            SolverManager solver,
            out ComputeBuffer rigidBodyBuffer,
            out ComputeBuffer rigidParticleIndexBuffer)
        {
            rigidBodyBuffer = null;
            rigidParticleIndexBuffer = null;
            if (!EnsureRigidContract(solver))
                return false;

            try
            {
                rigidBodyBuffer =
                    (ComputeBuffer)
                    RigidBodyBufferField.GetValue(solver);
                rigidParticleIndexBuffer =
                    (ComputeBuffer)
                    RigidParticleIndexBufferField.GetValue(
                        solver);
                return true;
            }
            catch (Exception exception)
            {
                ReportAccessFailure(
                    solver,
                    exception);
                return false;
            }
        }

        internal static bool TryGetRigidRestOffsets(
            SolverManager solver,
            out ComputeBuffer restOffsetBuffer)
        {
            restOffsetBuffer = null;
            if (solver == null ||
                !RigidRestOffsetsAvailable)
            {
                if (!_reportedRestOffsetContractFailure)
                {
                    Debug.LogError(
                        "Unified Solver compatibility " +
                        "bridge could not resolve " +
                        "SolverManager's private " +
                        "'_rigidRestOffsetBuffer'. Hull " +
                        "rendering needs it; the original " +
                        "solver remains unchanged.",
                        solver);
                    _reportedRestOffsetContractFailure =
                        true;
                }
                return false;
            }

            try
            {
                restOffsetBuffer =
                    (ComputeBuffer)
                    RigidRestOffsetBufferField.GetValue(
                        solver);
                return restOffsetBuffer != null;
            }
            catch (Exception exception)
            {
                ReportAccessFailure(
                    solver,
                    exception);
                return false;
            }
        }

        internal static bool TryGetRigidParticleRefCount(
            SolverManager solver,
            out int count)
        {
            count = 0;
            if (!EnsureRigidContract(solver))
                return false;

            try
            {
                count =
                    (int)
                    RigidParticleRefCountField.GetValue(
                        solver);
                return true;
            }
            catch (Exception exception)
            {
                ReportAccessFailure(
                    solver,
                    exception);
                return false;
            }
        }

        internal static bool TryGetClothParticleRange(
            SolverManager solver,
            ClothGenerator cloth,
            out int particleOffset,
            out int particleCount)
        {
            particleOffset = -1;
            particleCount = 0;
            if (!EnsureClothContract(cloth) ||
                solver == null)
            {
                return false;
            }

            long requestedCount =
                (long)cloth.resolutionX *
                cloth.resolutionY;
            if (requestedCount <= 0 ||
                requestedCount > int.MaxValue)
            {
                return false;
            }

            try
            {
                MeshFilter meshFilter =
                    cloth.GetComponent<MeshFilter>();
                if (meshFilter == null ||
                    meshFilter.sharedMesh == null)
                {
                    return false;
                }

                particleOffset =
                    (int)
                    ClothParticleOffsetField.GetValue(
                        cloth);
                particleCount = (int)requestedCount;
                return particleOffset >= 0 &&
                    particleOffset <= solver.ActiveCount &&
                    particleCount <=
                        solver.ActiveCount -
                        particleOffset;
            }
            catch (Exception exception)
            {
                particleOffset = -1;
                particleCount = 0;
                ReportAccessFailure(
                    cloth,
                    exception);
                return false;
            }
        }

        static FieldInfo ResolveField<T>(
            string fieldName)
        {
            return ResolveField<SolverManager, T>(
                fieldName);
        }

        static FieldInfo ResolveField<
            TDeclaring,
            TValue>(
            string fieldName)
        {
            FieldInfo field =
                typeof(TDeclaring).GetField(
                    fieldName,
                    InstancePrivate);
            return field != null &&
                field.FieldType == typeof(TValue)
                    ? field
                    : null;
        }

        static bool EnsureRigidContract(
            SolverManager solver)
        {
            if (solver == null)
                return false;
            if (RigidContractAvailable)
                return true;

            if (!_reportedRigidContractFailure)
            {
                Debug.LogError(
                    "Unified Solver compatibility bridge " +
                    "could not resolve the installed " +
                    "SolverManager rigid-body private " +
                    "field contract. The original " +
                    "solver remains unchanged; update " +
                    "SolverManagerAccess for this solver " +
                    "version.",
                    solver);
                _reportedRigidContractFailure = true;
            }
            return false;
        }

        static bool EnsureClothContract(
            ClothGenerator cloth)
        {
            if (cloth == null)
                return false;
            if (ClothContractAvailable)
                return true;

            if (!_reportedClothContractFailure)
            {
                Debug.LogError(
                    "Unified Solver compatibility bridge " +
                    "could not resolve the installed " +
                    "ClothGenerator particle-range private " +
                    "field contract. The original solver " +
                    "remains unchanged; update " +
                    "SolverManagerAccess for this solver " +
                    "version.",
                    cloth);
                _reportedClothContractFailure = true;
            }
            return false;
        }

        static void ReportAccessFailure(
            UnityEngine.Object context,
            Exception exception)
        {
            if (_reportedAccessFailure)
                return;

            Debug.LogError(
                "Unified Solver compatibility bridge " +
                "failed to read original solver state: " +
                $"{exception.GetType().Name}: " +
                exception.Message,
                context);
            _reportedAccessFailure = true;
        }
    }
}
