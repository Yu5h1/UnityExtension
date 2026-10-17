namespace Yu5h1Lib.UIToolkit
{
    /// <summary>
    /// Design data of the Yu5h1Lib.UX Button, pinned from <c>Button/button-design.json</c> 2.0.0
    /// (UX <c>d9a9790</c>). <c>ButtonDesignTests</c> checks every value against the snapshot kept with the
    /// package tests, so a new UX delivery that changes a value fails until this file is updated.
    /// </summary>
    public static class ButtonDesign
    {
        /// <summary>Default button size in px: the side of an icon-only button and the height with a label.</summary>
        public const float Size = 32;

        /// <summary>Icon side as a fraction of <see cref="Size"/> when only the icon is shown; also the cap beside a label.</summary>
        public const float IconRatio = .75f;

        /// <summary>Icon side in px beside a label set in <see cref="LabelFontSize"/>; it scales with the label's font size.</summary>
        public const float IconWithLabelSize = 18;

        /// <summary>The label font size, in px, that <see cref="IconWithLabelSize"/> is given for.</summary>
        public const float LabelFontSize = 14;

        /// <summary>Space in px between the icon and the label.</summary>
        public const float IconLabelGap = 8;
    }
}
