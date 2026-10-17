using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Yu5h1Lib.UIToolkit.Tests
{
    /// <summary>
    /// Executes the 10 neutral OptionPicker test cases from option-picker-cases.json (schema 1, version 1.0.0).
    /// Verifies OPT-VALUE, OPT-OPTIONS, OPT-OPEN, OPT-CYCLE, and OPT-PLACE in EditMode.
    /// </summary>
    public class OptionPickerTests
    {
        private OptionPickerModel model;

        [SetUp]
        public void SetUp()
        {
            model = new OptionPickerModel();
        }

        // Case 1: choice-tap-opens-on-selected-and-escape-returns-focus
        [Test]
        public void Case01_TapOpensOnSelectedAndEscapeReturnsFocus()
        {
            model.interaction = OptionPickerInteraction.Picker;
            model.orientation = OptionPickerOrientation.Horizontal;
            model.placement = OptionPickerPlacement.Aligned;

            var options = new List<OptionItem>
            {
                new OptionItem("one", "One"),
                new OptionItem("two", "Two"),
                new OptionItem("three", "Three")
            };
            model.SetOptions(options);
            model.currentValue = "two";

            // tap trigger opens
            model.HandleTriggerTap();

            Assert.IsTrue(model.isOpen, "Panel should be open");
            Assert.AreEqual("two", model.focusedId, "Focus should be on current selected item 'two'");

            // verify aligned placement
            var triggerRect = new Rect(100, 100, 40, 40);
            var panelSize = new Vector2(120, 40);
            var selectedLocalRect = new Rect(40, 0, 40, 40); // "two" is at index 1
            var viewport = new Rect(0, 0, 400, 300);

            var pos = OptionPickerPlacementSolver.ComputePanelPosition(triggerRect, panelSize, selectedLocalRect, model.placement, model.orientation, viewport);
            Assert.AreEqual(60f, pos.x, 0.01f);
            Assert.AreEqual(100f, pos.y, 0.01f);

            // Escape closes
            model.HandleKey("Escape");
            Assert.IsFalse(model.isOpen, "Panel should be closed");
            Assert.AreEqual("trigger", model.focusedId, "Focus should return to trigger");
        }

        // Case 2: choice-hold-release-in-place-chooses-current
        [Test]
        public void Case02_HoldReleaseInPlaceChoosesCurrent()
        {
            model.interaction = OptionPickerInteraction.Picker;
            model.orientation = OptionPickerOrientation.Horizontal;
            model.placement = OptionPickerPlacement.Aligned;

            var options = new List<OptionItem>
            {
                new OptionItem("one", "One"),
                new OptionItem("two", "Two"),
                new OptionItem("three", "Three")
            };
            model.SetOptions(options);
            model.currentValue = "two";

            string chosen = null;
            model.OnChosen += id => chosen = id;

            // Hold opens panel
            model.HandleTriggerHold();
            Assert.IsTrue(model.isOpen);
            Assert.AreEqual("two", model.focusedId);

            // Release in place chooses current
            model.Choose(model.GetResolvedCurrentOption().id);
            Assert.AreEqual("two", chosen);
            Assert.IsFalse(model.isOpen);
        }

        // Case 3: choice-disabled-options-are-skipped
        [Test]
        public void Case03_DisabledOptionsAreSkipped()
        {
            model.interaction = OptionPickerInteraction.Picker;
            model.orientation = OptionPickerOrientation.Horizontal;

            var options = new List<OptionItem>
            {
                new OptionItem("one", "One"),
                new OptionItem("two", "Two", disabled: true),
                new OptionItem("three", "Three")
            };
            model.SetOptions(options);
            model.currentValue = "one";

            model.HandleTriggerTap();
            Assert.IsTrue(model.isOpen);
            Assert.AreEqual("one", model.focusedId);

            // Arrow right skips disabled 'two' to 'three'
            model.HandleKey("ArrowRight");
            Assert.AreEqual("three", model.focusedId);

            // Arrow left skips back to 'one'
            model.HandleKey("ArrowLeft");
            Assert.AreEqual("one", model.focusedId);
        }

        // Case 4: choice-adjacent-flips-to-fit
        [Test]
        public void Case04_AdjacentFlipsToFit()
        {
            var triggerRect = new Rect(360, 100, 40, 40);
            var panelSize = new Vector2(120, 40);
            var selectedLocalRect = new Rect(0, 0, 40, 40);
            var viewport = new Rect(0, 0, 400, 300);

            var pos = OptionPickerPlacementSolver.ComputePanelPosition(
                triggerRect, panelSize, selectedLocalRect,
                OptionPickerPlacement.Adjacent, OptionPickerOrientation.Horizontal, viewport);

            // Exceeds 396 -> flips to left: 360 - 4 - 120 = 236
            Assert.AreEqual(236f, pos.x, 0.01f);
            Assert.AreEqual(100f, pos.y, 0.01f);
        }

        // Case 5: choice-adjacent-vertical-flips-above
        [Test]
        public void Case05_AdjacentVerticalFlipsAbove()
        {
            var triggerRect = new Rect(100, 260, 40, 40);
            var panelSize = new Vector2(40, 120);
            var selectedLocalRect = new Rect(0, 0, 40, 40);
            var viewport = new Rect(0, 0, 400, 300);

            var pos = OptionPickerPlacementSolver.ComputePanelPosition(
                triggerRect, panelSize, selectedLocalRect,
                OptionPickerPlacement.Adjacent, OptionPickerOrientation.Vertical, viewport);

            // Exceeds 296 -> flips to above: 260 - 4 - 120 = 136
            Assert.AreEqual(100f, pos.x, 0.01f);
            Assert.AreEqual(136f, pos.y, 0.01f);
        }

        // Case 6: choice-aligned-clamps-to-viewport
        [Test]
        public void Case06_AlignedClampsToViewport()
        {
            var triggerRect = new Rect(0, 0, 40, 40);
            var panelSize = new Vector2(120, 40);
            var selectedLocalRect = new Rect(40, 0, 40, 40); // center x=60, y=20
            var viewport = new Rect(0, 0, 400, 300);

            var pos = OptionPickerPlacementSolver.ComputePanelPosition(
                triggerRect, panelSize, selectedLocalRect,
                OptionPickerPlacement.Aligned, OptionPickerOrientation.Horizontal, viewport);

            // Aligned would be -40, clamped to pickerViewportMargin = 4
            Assert.AreEqual(4f, pos.x, 0.01f);
            Assert.AreEqual(4f, pos.y, 0.01f);
        }

        // Case 7: option-both-tap-cycles-and-arrow-opens
        [Test]
        public void Case07_BothTapCyclesAndArrowOpens()
        {
            model.interaction = OptionPickerInteraction.Both;
            model.orientation = OptionPickerOrientation.Vertical;

            var options = new List<OptionItem>
            {
                new OptionItem("one", "One"),
                new OptionItem("two", "Two"),
                new OptionItem("three", "Three")
            };
            model.SetOptions(options);
            model.currentValue = "two";

            string chosen = null;
            model.OnChosen += id => chosen = id;

            // Tap cycles to three, does not open panel
            model.HandleTriggerTap();
            Assert.AreEqual("three", chosen);
            Assert.IsFalse(model.isOpen);

            // Host updates value
            model.currentValue = "three";

            // ArrowDown opens panel
            model.HandleKey("ArrowDown");
            Assert.IsTrue(model.isOpen);
            Assert.AreEqual("three", model.focusedId);
        }

        // Case 8: option-cycle-tap-wraps-to-first
        [Test]
        public void Case08_CycleTapWrapsToFirst()
        {
            model.interaction = OptionPickerInteraction.Cycle;

            var options = new List<OptionItem>
            {
                new OptionItem("one", "One"),
                new OptionItem("two", "Two"),
                new OptionItem("three", "Three")
            };
            model.SetOptions(options);
            model.currentValue = "three";

            string chosen = null;
            model.OnChosen += id => chosen = id;

            model.HandleTriggerTap();
            Assert.AreEqual("one", chosen, "Should wrap around to first option");
            Assert.IsFalse(model.isOpen);
        }

        // Case 9: option-cycle-skips-disabled
        [Test]
        public void Case09_CycleSkipsDisabled()
        {
            model.interaction = OptionPickerInteraction.Cycle;

            var options = new List<OptionItem>
            {
                new OptionItem("one", "One"),
                new OptionItem("two", "Two", disabled: true),
                new OptionItem("three", "Three")
            };
            model.SetOptions(options);
            model.currentValue = "one";

            string chosen = null;
            model.OnChosen += id => chosen = id;

            model.HandleTriggerTap();
            Assert.AreEqual("three", chosen, "Should skip disabled 'two' to 'three'");
            Assert.IsFalse(model.isOpen);
        }

        // Case 10: option-cycle-keyboard-never-opens
        [Test]
        public void Case10_CycleKeyboardNeverOpens()
        {
            model.interaction = OptionPickerInteraction.Cycle;
            model.orientation = OptionPickerOrientation.Horizontal;

            var options = new List<OptionItem>
            {
                new OptionItem("one", "One"),
                new OptionItem("two", "Two")
            };
            model.SetOptions(options);
            model.currentValue = "one";

            // Arrow keys must not open panel in cycle mode
            model.HandleKey("ArrowRight");
            Assert.IsFalse(model.isOpen);

            // Space cycles
            string chosen = null;
            model.OnChosen += id => chosen = id;
            model.HandleKey("Space");
            Assert.AreEqual("two", chosen);
            Assert.IsFalse(model.isOpen);
        }

        [Test]
        public void DuplicateOrEmptyId_InvalidatesOptions()
        {
            var invalid = new List<OptionItem>
            {
                new OptionItem("one", "One"),
                new OptionItem("", "Empty"),
                new OptionItem("one", "Duplicate")
            };

            bool ok = model.SetOptions(invalid);
            Assert.IsFalse(ok);
            Assert.IsFalse(model.hasValidOptions);
            Assert.IsFalse(model.TryOpen());
            Assert.IsFalse(model.TryCycleNext(out _));
        }
    }
}
