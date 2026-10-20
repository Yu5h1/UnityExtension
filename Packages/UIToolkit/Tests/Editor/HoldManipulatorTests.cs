using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Yu5h1Lib.UIToolkit.Tests
{
    /// <summary>
    /// Verifies Yu5h1Lib.UX Foundation/Input (3.0.1) HoldManipulator contract and neutral cases:
    /// INPUT-PRESS-TAP, INPUT-PRESS-HOLD, INPUT-PRESS-SUPPRESS, INPUT-DRAG-START,
    /// INPUT-CHOICE-HOLD, INPUT-CANCEL, and INPUT-LIFECYCLE.
    /// </summary>
    public class HoldManipulatorTests
    {
        private const string ExpectedSpecSha256 = "99583d7c704d8c586068e49e7c4e74b093bb4e3c241685aa73cf23a30ef88bcb";
        private const string ExpectedDesignSha256 = "b31fb027abd5fad8a506bda2caa8ff35e1ece6813d61e6773e1ddf6ba5699d66";
        private const string ExpectedCasesSha256 = "203344648e246c75349444e07f3a0a36806610270140b901f6b2cb6bd9e19188";

        private VisualElement target;
        private HoldManipulator hold;
        private List<string> events;
        private Rect targetRect;

        [SetUp]
        public void SetUp()
        {
            target = new VisualElement();
            target.style.width = 62f;
            target.style.height = 62f;
            targetRect = new Rect(-31f, -31f, 62f, 62f);

            hold = new HoldManipulator();
            events = new List<string>();

            hold.HoldStarted += _ => events.Add("start");
            hold.HoldTriggered += pos => events.Add($"hold:{pos.x:0.#},{pos.y:0.#}");
            hold.HoldMoved += pos => events.Add($"move:{pos.x:0.#},{pos.y:0.#}");
            hold.HoldEnded += pos => events.Add($"end:{pos.x:0.#},{pos.y:0.#}");
            hold.HoldCanceled += () => events.Add("cancel");
            hold.TapTriggered += () => events.Add("tap");

            target.AddManipulator(hold);
        }

        [Test]
        public void PinnedUxInputSnapshot_MatchesManifestHashesAndDesignTokens()
        {
            string baseDir = Path.GetFullPath("Packages/com.yu5h1.uitoolkit/Tests/Editor/UX~/Input");
            if (!Directory.Exists(baseDir))
            {
                baseDir = Path.GetFullPath("Packages/UIToolkit/Tests/Editor/UX~/Input");
            }

            Assert.IsTrue(Directory.Exists(baseDir), $"Pinned UX Input directory not found: {baseDir}");

            string specPath = Path.Combine(baseDir, "Input.md");
            string designPath = Path.Combine(baseDir, "input-design.json");
            string casesPath = Path.Combine(baseDir, "input-cases.json");

            Assert.AreEqual(ExpectedSpecSha256, ComputeSha256(specPath), "Input.md SHA-256 mismatch");
            Assert.AreEqual(ExpectedDesignSha256, ComputeSha256(designPath), "input-design.json SHA-256 mismatch");
            Assert.AreEqual(ExpectedCasesSha256, ComputeSha256(casesPath), "input-cases.json SHA-256 mismatch");

            Assert.AreEqual(400L, HoldManipulator.DefaultHoldDurationMs, "holdDuration must match 0.4s (400ms)");
            Assert.AreEqual(6f, HoldManipulator.DefaultDragThreshold, 0.001f, "dragThreshold must match 6px");
        }

        // INPUT-PRESS-TAP: tap-activates & half-open edge cases (1.3.0 / 1.3.1)
        [Test]
        public void TapAndHalfOpenEdges_MatchUxInputPressTap()
        {
            // Center tap activates
            hold.SimulatePointerDown(1, 0, Vector2.zero, 0);
            hold.SimulatePointerUp(1, Vector2.zero, 120, targetRect);
            CollectionAssert.AreEqual(new[] { "start", "tap" }, events);

            // Left edge (-31, 0) inclusive -> activates
            events.Clear();
            hold.SimulatePointerDown(1, 0, Vector2.zero, 200);
            hold.SimulatePointerUp(1, new Vector2(-31f, 0f), 260, targetRect);
            CollectionAssert.AreEqual(new[] { "start", "tap" }, events);

            // Top edge (0, -31) inclusive -> activates
            events.Clear();
            hold.SimulatePointerDown(1, 0, Vector2.zero, 300);
            hold.SimulatePointerUp(1, new Vector2(0f, -31f), 360, targetRect);
            CollectionAssert.AreEqual(new[] { "start", "tap" }, events);

            // Right edge (31, 0) exclusive -> silent
            events.Clear();
            hold.SimulatePointerDown(1, 0, Vector2.zero, 400);
            hold.SimulatePointerUp(1, new Vector2(31f, 0f), 460, targetRect);
            CollectionAssert.AreEqual(new[] { "start" }, events);

            // Bottom edge (0, 31) exclusive -> silent
            events.Clear();
            hold.SimulatePointerDown(1, 0, Vector2.zero, 500);
            hold.SimulatePointerUp(1, new Vector2(0f, 31f), 560, targetRect);
            CollectionAssert.AreEqual(new[] { "start" }, events);

            // Outside release (80, 0) -> silent
            events.Clear();
            hold.SimulatePointerDown(1, 0, Vector2.zero, 600);
            hold.SimulatePointerUp(1, new Vector2(80f, 0f), 660, targetRect);
            CollectionAssert.AreEqual(new[] { "start" }, events);
        }

        // INPUT-PRESS-TAP: non-primary-button-is-ignored
        [Test]
        public void NonPrimaryButton_IsIgnored()
        {
            hold.SimulatePointerDown(1, 1, Vector2.zero, 0);
            hold.AdvanceClockTo(500);
            hold.SimulatePointerUp(1, Vector2.zero, 520, targetRect);
            Assert.IsEmpty(events);
        }

        // INPUT-PRESS-HOLD & INPUT-PRESS-SUPPRESS: hold-at-duration-suppresses-pointer-activate
        [Test]
        public void HoldAtDuration_FiresOnceAndSuppressesTap()
        {
            hold.SimulatePointerDown(1, 0, Vector2.zero, 0);
            hold.AdvanceClockTo(399);
            Assert.IsFalse(hold.IsHolding, "Should not hold at 399ms");

            hold.AdvanceClockTo(400);
            Assert.IsTrue(hold.IsHolding, "Should hold at 400ms");

            hold.SimulatePointerUp(1, Vector2.zero, 450, targetRect);
            Assert.IsFalse(hold.IsHolding, "Should clear IsHolding on release");

            CollectionAssert.AreEqual(new[] { "start", "hold:0,0", "end:0,0" }, events);
        }

        // INPUT-DRAG-START: threshold-equal-is-not-drag (=6px) vs drag-starts-past-threshold (>6px)
        [Test]
        public void DragThresholdBoundary_EqualStillHolds_GreaterCancelsHold()
        {
            // Move exactly 6px (= DragThreshold) before 400ms -> still triggers hold at 400ms
            hold.SimulatePointerDown(1, 0, Vector2.zero, 0);
            hold.SimulatePointerMove(1, new Vector2(6f, 0f), 100);
            hold.AdvanceClockTo(400);
            Assert.IsTrue(hold.IsHolding, "Movement equal to 6px must not cancel hold");
            hold.SimulatePointerUp(1, new Vector2(6f, 0f), 450, targetRect);
            CollectionAssert.AreEqual(new[] { "start", "hold:6,0", "end:6,0" }, events);

            // Move 7px (> DragThreshold) before 400ms -> cancels hold and does not fire tap
            events.Clear();
            hold.SimulatePointerDown(1, 0, Vector2.zero, 500);
            hold.SimulatePointerMove(1, new Vector2(7f, 0f), 600);
            Assert.IsFalse(hold.IsPressed, "Moving past threshold must cancel pending press");
            hold.AdvanceClockTo(1000);
            hold.SimulatePointerUp(1, new Vector2(7f, 0f), 1050, targetRect);
            CollectionAssert.AreEqual(new[] { "start", "cancel" }, events);
        }

        // INPUT-CHOICE-HOLD: hold-and-slide lifecycle (onStart -> onTrigger -> onMove -> onEnd)
        [Test]
        public void HoldAndSlideLifecycle_StreamsMoveAndEnd()
        {
            hold.SimulatePointerDown(1, 0, new Vector2(10f, 10f), 0);
            hold.SimulatePointerMove(1, new Vector2(14f, 10f), 100); // 4px <= 6px
            hold.AdvanceClockTo(400);
            Assert.IsTrue(hold.IsHolding);

            // Slide far past threshold after hold is already active -> streams HoldMoved without canceling
            hold.SimulatePointerMove(1, new Vector2(50f, 10f), 450);
            hold.SimulatePointerMove(1, new Vector2(90f, 10f), 500);
            hold.SimulatePointerUp(1, new Vector2(90f, 10f), 550, targetRect);

            CollectionAssert.AreEqual(new[]
            {
                "start",
                "hold:14,10",
                "move:50,10",
                "move:90,10",
                "end:90,10"
            }, events);
        }

        // INPUT-CANCEL & INPUT-LIFECYCLE: second pointer & manipulator removal
        [Test]
        public void SecondPointerAndLifecycle_CancelCleanly()
        {
            // Second pointer during pending press cancels
            hold.SimulatePointerDown(1, 0, Vector2.zero, 0);
            hold.SimulatePointerDown(2, 0, new Vector2(10f, 10f), 100);
            hold.AdvanceClockTo(500);
            hold.SimulatePointerUp(1, Vector2.zero, 520, targetRect);
            CollectionAssert.AreEqual(new[] { "start", "cancel" }, events);

            // Second pointer during active hold cancels
            events.Clear();
            hold.SimulatePointerDown(1, 0, Vector2.zero, 600);
            hold.AdvanceClockTo(1000);
            Assert.IsTrue(hold.IsHolding);
            hold.SimulatePointerDown(2, 0, Vector2.zero, 1050);
            Assert.IsFalse(hold.IsHolding);
            CollectionAssert.AreEqual(new[] { "start", "hold:0,0", "cancel" }, events);

            // RemoveManipulator during active press cancels
            events.Clear();
            hold.SimulatePointerDown(1, 0, Vector2.zero, 1200);
            target.RemoveManipulator(hold);
            CollectionAssert.AreEqual(new[] { "start", "cancel" }, events);
        }

        // OptionPicker integration with HoldManipulator
        [Test]
        public void OptionPicker_UsesHoldManipulatorForPickerAndDisablesHoldInCycleMode()
        {
            var picker = new OptionPicker();
            picker.SetOptions(new[]
            {
                new OptionItem("a", "Alpha"),
                new OptionItem("b", "Beta")
            }, "a");

            Assert.IsNotNull(picker.TriggerHoldManipulator);
            Assert.AreEqual(400L, picker.TriggerHoldManipulator.HoldDurationMs);
            Assert.AreEqual(6f, picker.TriggerHoldManipulator.DragThreshold, 0.001f);

            // Both mode: hold opens panel
            picker.Model.interaction = OptionPickerInteraction.Both;
            picker.TriggerHoldManipulator.SimulatePointerDown(1, 0, Vector2.zero, 0);
            picker.TriggerHoldManipulator.AdvanceClockTo(400);
            Assert.IsTrue(picker.Model.isOpen, "Hold should open OptionPicker in Both mode");
            picker.TriggerHoldManipulator.Cancel();
            Assert.IsFalse(picker.Model.isOpen, "Canceling hold should close OptionPicker");

            // Cycle mode: hold does not start, tap cycles value
            picker.Model.interaction = OptionPickerInteraction.Cycle;
            picker.TriggerHoldManipulator.SimulatePointerDown(1, 0, Vector2.zero, 500);
            picker.TriggerHoldManipulator.AdvanceClockTo(1000);
            Assert.IsFalse(picker.Model.isOpen, "Hold must not open panel in Cycle mode");
            picker.TriggerHoldManipulator.SimulatePointerUp(1, Vector2.zero, 1050, new Rect(-10f, -10f, 40f, 40f));
            Assert.AreEqual("b", picker.Model.currentValue, "Short press release in Cycle mode should cycle to next option");
        }

        private static string ComputeSha256(string filePath)
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            using var sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(bytes);
            var sb = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
