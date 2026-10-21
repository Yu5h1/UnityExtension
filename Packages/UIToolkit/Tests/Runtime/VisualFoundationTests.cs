using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Yu5h1Lib.UIToolkit.Tests
{
    /// <summary>
    /// Platform checks for UX VisualFoundation 1.3.0 goal 1 on UI Toolkit: VP01 reads the palette the panel actually resolves,
    /// and the state checks read which role the Button style sheet reaches in each state. Whether the result looks right
    /// (VP03) is the user's judgement on the acceptance page.
    /// </summary>
    public class VisualFoundationTests
    {
        private const int PanelWidth = 320, PanelHeight = 240;
        private const string ButtonStyleResource = "Yu5h1Lib.UIToolkit/Yu5h1Button";

        private static readonly Regex ColorToken = new Regex(
            "\"color\\.(light|dark)\\.([A-Za-z]+)\"\\s*:\\s*\\{[^}]*?\"value\"\\s*:\\s*\"#([0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})\"");

        private GameObject host;
        private PanelSettings settings;
        private RenderTexture target;
        private VisualElement root;
        private StyleSheet buttonStyle;

        [SetUp]
        public void SetUp()
        {
            target = new RenderTexture(PanelWidth, PanelHeight, 0, RenderTextureFormat.ARGB32);
            settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.targetTexture = target;
            host = new GameObject("VisualFoundationTests");
            var document = host.AddComponent<UIDocument>();
            document.panelSettings = settings;

            root = document.rootVisualElement;
            root.style.width = PanelWidth;
            root.style.height = PanelHeight;
            root.style.alignItems = Align.Center;
            root.style.justifyContent = Justify.Center;

            buttonStyle = Resources.Load<StyleSheet>(ButtonStyleResource);
            Assert.IsNotNull(buttonStyle, "Yu5h1Button.uss style sheet could not be loaded");
            Assert.IsNotNull(VisualFoundationTheme.Sheet, "Yu5h1VisualFoundation.uss style sheet could not be loaded");
        }

        [TearDown]
        public void TearDown()
        {
            if (host != null) Object.DestroyImmediate(host);
            if (settings != null) Object.DestroyImmediate(settings);
            if (target != null) Object.DestroyImmediate(target);
        }

        private static IEnumerator Settle()
        {
            for (int i = 0; i < 6; i++) yield return null;
        }

        private static Dictionary<string, Color32> DesignRoles(string scheme)
        {
#if UNITY_EDITOR
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(VisualFoundationTests).Assembly);
            Assert.IsNotNull(package, "Yu5h1Lib.UIToolkit package not found");
            var json = File.ReadAllText(Path.Combine(package.resolvedPath, "Tests", "Editor", "UX~", "VisualFoundation", "visual-design.json"));
            var roles = new Dictionary<string, Color32>();
            foreach (Match match in ColorToken.Matches(json))
            {
                if (match.Groups[1].Value != scheme) continue;
                var hex = match.Groups[3].Value;
                byte Channel(int index) => System.Convert.ToByte(hex.Substring(index * 2, 2), 16);
                roles[match.Groups[2].Value] = new Color32(Channel(0), Channel(1), Channel(2), hex.Length == 8 ? Channel(3) : (byte)255);
            }
            return roles;
#else
            Assert.Inconclusive("The design data is read from the package source in the Editor.");
            return null;
#endif
        }

        private static string VariableOf(string role)
        {
            var name = "--yu5h1-color-";
            foreach (var character in role) name += char.IsUpper(character) ? "-" + char.ToLowerInvariant(character) : character.ToString();
            return name;
        }

        private static Color32 Bytes(Color color) => new Color32(
            (byte)Mathf.RoundToInt(color.r * 255f), (byte)Mathf.RoundToInt(color.g * 255f),
            (byte)Mathf.RoundToInt(color.b * 255f), (byte)Mathf.RoundToInt(color.a * 255f));

        private static void AreSame(Color32 expected, Color actual, string what)
        {
            var bytes = Bytes(actual);
            Assert.AreEqual((expected.r, expected.g, expected.b, expected.a), (bytes.r, bytes.g, bytes.b, bytes.a), what);
        }

        private Button CreateButton(VisualFoundationScheme scheme, bool ghost = false)
        {
            VisualFoundationTheme.Apply(root, scheme);
            var button = new Button { text = "Label" };
            button.styleSheets.Add(buttonStyle);
            button.AddToClassList("yu5h1-button");
            if (ghost) button.AddToClassList("yu5h1-button--ghost");
            root.Add(button);
            return button;
        }

        private static string SchemeName(VisualFoundationScheme scheme) => scheme == VisualFoundationScheme.Light ? "light" : "dark";

        [UnityTest]
        public IEnumerator VP01_PanelResolvesAllFifteenRolesOfTheDesignData([Values(VisualFoundationScheme.Light, VisualFoundationScheme.Dark)] VisualFoundationScheme scheme)
        {
            var roles = DesignRoles(SchemeName(scheme));
            Assert.AreEqual(15, roles.Count, "The design data defines 15 colour roles per scheme");

            var resolved = new Dictionary<string, Color>();
            root.RegisterCallback<CustomStyleResolvedEvent>(evt =>
            {
                foreach (var role in roles.Keys)
                {
                    if (evt.customStyle.TryGetValue(new CustomStyleProperty<Color>(VariableOf(role)), out var color)) resolved[role] = color;
                }
            });
            VisualFoundationTheme.Apply(root, scheme);
            yield return Settle();

            foreach (var role in roles)
            {
                Assert.IsTrue(resolved.ContainsKey(role.Key), $"{SchemeName(scheme)} role {role.Key} was not resolved by the panel");
                AreSame(role.Value, resolved[role.Key], $"{SchemeName(scheme)} role {role.Key}");
            }
        }

        [UnityTest]
        public IEnumerator VS_RestingHoverSelectedAndDisabledReachTheirRoles([Values(VisualFoundationScheme.Light, VisualFoundationScheme.Dark)] VisualFoundationScheme scheme)
        {
            var roles = DesignRoles(SchemeName(scheme));
            var solid = CreateButton(scheme);
            var ghost = CreateButton(scheme, ghost: true);
            var hovered = CreateButton(scheme);
            hovered.AddToClassList("yu5h1-button--hover");
            var selected = CreateButton(scheme);
            selected.AddToClassList("yu5h1-button--selected");
            var selectedGhost = CreateButton(scheme, ghost: true);
            selectedGhost.AddToClassList("yu5h1-button--selected");
            var selectedHovered = CreateButton(scheme);
            selectedHovered.AddToClassList("yu5h1-button--selected");
            selectedHovered.AddToClassList("yu5h1-button--hover");
            var disabled = CreateButton(scheme);
            disabled.SetEnabled(false);
            yield return Settle();

            AreSame(roles["itemText"], solid.resolvedStyle.color, "Solid resting text uses itemText");
            AreSame(roles["panelBackground"], solid.resolvedStyle.backgroundColor, "Solid resting fill uses panelBackground");
            Assert.IsNotNull(solid.resolvedStyle.backgroundImage.texture, "A solid button is outlined at rest");
            AreSame(roles["controlBorder"], solid.resolvedStyle.unityBackgroundImageTintColor, "Solid resting outline uses controlBorder");
            Assert.AreEqual(0f, ghost.resolvedStyle.backgroundColor.a, "A ghost button has no fill at rest");
            Assert.IsNull(ghost.resolvedStyle.backgroundImage.texture, "A ghost button has no outline at rest");
            AreSame(roles["itemText"], ghost.resolvedStyle.color, "Ghost text uses itemText");
            AreSame(roles["hover"], hovered.resolvedStyle.backgroundColor, "Hover fill uses hover");
            AreSame(roles["accent"], selected.resolvedStyle.backgroundColor, "Selected fill uses accent");
            AreSame(roles["onAccent"], selected.resolvedStyle.color, "Selected text uses onAccent");
            AreSame(roles["accent"], selectedGhost.resolvedStyle.backgroundColor, "Selected ghost fill uses accent");
            AreSame(roles["accent"], selectedHovered.resolvedStyle.backgroundColor, "Selected wins over hover");
            AreSame(roles["onAccent"], selectedHovered.resolvedStyle.color, "Selected text stays onAccent under hover");
            Assert.AreEqual(0f, selectedGhost.resolvedStyle.unityBackgroundImageTintColor.a, "A selected button is filled and needs no outline");
            AreSame(roles["disabled"], disabled.resolvedStyle.color, "Disabled text uses disabled");
        }

        [UnityTest]
        public IEnumerator VS_KeyboardFocusDrawsTheFocusRoleOutline([Values(VisualFoundationScheme.Light, VisualFoundationScheme.Dark)] VisualFoundationScheme scheme)
        {
            var roles = DesignRoles(SchemeName(scheme));
            var solid = CreateButton(scheme);
            var ghost = CreateButton(scheme, ghost: true);
            yield return Settle();
            AreSame(roles["controlBorder"], solid.resolvedStyle.unityBackgroundImageTintColor, "A solid button is outlined in controlBorder at rest");
            Assert.IsNull(ghost.resolvedStyle.backgroundImage.texture, "A ghost button has no outline at rest");

            solid.Focus();
            yield return Settle();
            Assert.IsNotNull(solid.resolvedStyle.backgroundImage.texture, "Focus draws the outline");
            AreSame(roles["focus"], solid.resolvedStyle.unityBackgroundImageTintColor, "Focus outline uses focus");
            Assert.AreEqual(32f, solid.worldBound.height, 0.5f, "The focus outline does not change the size of the button");

            ghost.Focus();
            yield return Settle();
            AreSame(roles["focus"], ghost.resolvedStyle.unityBackgroundImageTintColor, "Ghost focus outline uses focus");
        }
    }
}
