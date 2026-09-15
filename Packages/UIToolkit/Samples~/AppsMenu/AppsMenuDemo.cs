using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Yu5h1Lib;
using Yu5h1Lib.UIToolkit;

namespace Yu5h1LibTest
{
    /// <summary>
    /// An Apps menu: a scatterable desktop plus a float Apps panel to stash things in, modelled on
    /// HealthAI's own. Assembled entirely from the Yu5h1Lib packages.
    /// <para>
    /// <b>One rule governs every gesture here:</b> a gesture belongs to one element, and that element both
    /// captures the pointer and runs the gesture's own timers. Dragging an app belongs to the app, so the
    /// app captures; dragging the background belongs to the whole surface, so the desktop captures. Splitting
    /// those halves across different elements is what fails silently - the owner stops hearing about
    /// movement, or the release arrives somewhere that is no longer listening.
    /// </para>
    /// <para>
    /// A 3D scene joins in the same way and not a second way: a plain <see cref="VisualElement"/> stands in
    /// for the object, takes the pointer events, and the object is projected onto its rectangle with
    /// <c>World.TryPlace</c>. Interaction is always elements and pointer events.
    /// </para>
    /// <para>
    /// This is the hands-on rig for what no unit test can judge - group drag, inertia, the throw and its
    /// bounce, the hold and its swallowed click, the catch wobble, the staggered entrance, the panel spring.
    /// It is also the evidence for whether a <c>DraggableDesktop</c> control needs to exist: everything here
    /// that is not a package call is policy.
    /// </para>
    /// <para>Drop it on an empty GameObject and press Play. No scene setup is needed.</para>
    /// </summary>
    [AddComponentMenu("Yu5h1LibTest/Apps Menu Demo")]
    public sealed class AppsMenuDemo : MonoBehaviour
    {
        [Header("Apps")]
        [Range(2, 16)] public int appCount = 8;

        [Tooltip("How many start on the desktop; the rest start in the Apps panel.")]
        [Range(0, 16)] public int onDesktopAtStart = 5;

        [Range(32, 160)] public float iconSize = 62;

        [Header("Throw")]
        public Reaction.Throw.Settings throwSettings = new Reaction.Throw.Settings();

        [Tooltip("Pointer speed above which a release becomes a throw.")]
        [Min(0)] public float throwThreshold = 180;

        [Header("Hold")]
        [Tooltip("Hold time before an app is marked, in milliseconds.")]
        [Min(50)] public long holdDuration = 400;

        [Tooltip("How far the pointer may wander before a hold is abandoned. App 0 ignores this.")]
        [Min(0)] public float moveTolerance = 6;

        [Header("Diagnostics")]
        public bool trace;

        // --- package pieces -------------------------------------------------
        private GroupDragLayout layout;
        private readonly ShakePlayer shake = new ShakePlayer();
        private readonly Transition.Opening opening = new Transition.Opening();
        private readonly Reaction.Throw flight = new Reaction.Throw();

        /// <summary>Panel travel: 1 closed, 0 open. A spring, so toggling mid-animation never jumps.</summary>
        private readonly Reaction.Spring panelTravel = new Reaction.Spring(1);

        // --- elements -------------------------------------------------------
        private UIDocument document;
        private PanelSettings settings;
        private VisualElement root, desktop, obstacle, appsButton, appsPanel;
        private Label hint;

        // --- app model (policy: where each app lives) ------------------------
        private readonly List<string> apps = new List<string>();
        private readonly HashSet<string> onDesktop = new HashSet<string>();
        private readonly HashSet<string> marked = new HashSet<string>();
        private readonly Dictionary<string, VisualElement> icons = new Dictionary<string, VisualElement>();
        private readonly List<string> desktopOrder = new List<string>();

        // --- gesture state (policy) -----------------------------------------
        private int pointer = -1;
        private VisualElement owner;          // the element this gesture belongs to
        private string dragging, flying;
        private Vector2 dragSample, dragVelocity, groupLast, pressAt;
        private float dragSampleTime;
        private bool appsPressed, catchArmed;
        private string slotTarget;
        private VisualElement spacer;
        private int clicks, holds, stashes;

        private bool PanelOpen => panelTravel.Target == 0;
        private bool Busy => pointer >= 0;

        /// <summary>
        /// What desktop apps keep clear of.
        /// <para>
        /// The Apps button counts, because it is permanent and because an app resting under it would
        /// otherwise be swallowed the instant it was thrown, without ever travelling. The float panel does
        /// not: it is an overlay that comes and goes, and rearranging the whole desktop each time it opens
        /// would make opening a menu shuffle the user's own layout.
        /// </para>
        /// </summary>
        private IReadOnlyList<Rect> Obstacles =>
            obstacle == null || appsButton == null
                ? Array.Empty<Rect>()
                : new[] { obstacle.layout, appsButton.layout };

        // ====================================================================
        // Lifecycle
        // ====================================================================

        private void OnEnable()
        {
            layout = new GroupDragLayout(scatterSize: iconSize, sortedSize: iconSize * .7f,
                                         sortedStride: iconSize * 1.16f);
            BuildPanel();
            BuildDesktop();
            BuildApps();
            Sync();
            root.schedule.Execute(StartOpening).ExecuteLater(50);
        }

        private void OnDisable()
        {
            shake.StopAll();
            opening.Skip();
            if (document != null) Destroy(document.gameObject);
            if (settings != null) Destroy(settings);
        }

        private void BuildPanel()
        {
            settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.name = "AppsMenuDemo Panel";
            settings.scaleMode = PanelScaleMode.ConstantPixelSize;

            // The theme must be on the settings before a UIDocument builds a panel from them, or Unity warns
            // and the panel renders without default styling. Assigning it afterwards is too late.
            var theme = Resources.Load<ThemeStyleSheet>("AppsMenuTheme");
            if (theme != null) settings.themeStyleSheet = theme;
            else Debug.LogError("AppsMenuTheme.tss not found under a Resources folder.");

            var host = new GameObject("AppsMenuDemo UI");
            host.transform.SetParent(transform, false);
            document = host.AddComponent<UIDocument>();
            document.panelSettings = settings;

            root = document.rootVisualElement;
            root.style.flexGrow = 1;
            root.style.backgroundColor = new Color(.11f, .12f, .15f);

            var style = Resources.Load<StyleSheet>("AppsMenuDemo");
            if (style != null) root.styleSheets.Add(style);
            else Debug.LogError("AppsMenuDemo.uss not found under a Resources folder.");
        }

        // ====================================================================
        // Desktop and float panel
        // ====================================================================

        private void BuildDesktop()
        {
            desktop = new VisualElement { name = "desktop" };
            desktop.style.position = Position.Absolute;
            desktop.style.left = 0; desktop.style.top = 0;
            desktop.style.right = 0; desktop.style.bottom = 0;
            root.Add(desktop);

            obstacle = new VisualElement { name = "obstacle" };
            obstacle.style.position = Position.Absolute;
            obstacle.style.left = 40; obstacle.style.top = 220;
            obstacle.style.width = 220; obstacle.style.height = 140;
            obstacle.style.backgroundColor = new Color(.25f, .22f, .30f);
            obstacle.style.borderTopLeftRadius = obstacle.style.borderTopRightRadius =
                obstacle.style.borderBottomLeftRadius = obstacle.style.borderBottomRightRadius = 12;
            obstacle.pickingMode = PickingMode.Ignore;
            desktop.Add(obstacle);
            desktop.Add(Caption(obstacle, "obstacle - desktop apps keep clear"));

            appsButton = new VisualElement { name = "apps-button" };
            appsButton.AddToClassList("apps-button");
            appsButton.Add(Center("Apps", new Color(.86f, .92f, .90f)));
            // The Apps button owns its own press, so it consumes it. Letting it through would start the
            // desktop's background gesture underneath, and the button would never see its own release.
            appsButton.RegisterCallback<PointerDownEvent>(e => { appsPressed = true; e.StopPropagation(); });
            appsButton.RegisterCallback<PointerUpEvent>(e =>
            {
                if (!appsPressed) return;
                appsPressed = false;
                TogglePanel();
                e.StopPropagation();
            });
            appsButton.RegisterCallback<PointerLeaveEvent>(_ => appsPressed = false);
            appsButton.RegisterCallback<PointerCancelEvent>(_ => appsPressed = false);
            desktop.Add(appsButton);

            appsPanel = new VisualElement { name = "apps-panel" };
            appsPanel.AddToClassList("apps-panel");
            appsPanel.AddToClassList("hidden");
            // A translucent surface is still a surface. Without this the press would carry on to the
            // desktop underneath and start a background drag through the panel.
            appsPanel.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
            appsPanel.RegisterCallback<PointerUpEvent>(e => e.StopPropagation());
            desktop.Add(appsPanel);

            hint = new Label { name = "hint" };
            hint.style.position = Position.Absolute;
            hint.style.left = 12; hint.style.top = 12;
            hint.style.color = new Color(.85f, .87f, .92f);
            hint.style.whiteSpace = WhiteSpace.Normal;
            hint.pickingMode = PickingMode.Ignore;
            desktop.Add(hint);

            // The background gesture belongs to the desktop, so the desktop owns all of its events.
            desktop.RegisterCallback<PointerDownEvent>(OnDesktopDown);
            desktop.RegisterCallback<PointerMoveEvent>(OnDesktopMove);
            desktop.RegisterCallback<PointerUpEvent>(OnDesktopUp);
            desktop.RegisterCallback<PointerCaptureOutEvent>(OnCaptureLost);
            desktop.RegisterCallback<GeometryChangedEvent>(_ => Resolve());
        }

        private static Label Center(string text, Color color)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.style.flexGrow = 1;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.color = color;
            return label;
        }

        private static Label Caption(VisualElement anchor, string text)
        {
            var label = new Label(text);
            label.style.position = Position.Absolute;
            label.style.color = new Color(.55f, .58f, .66f);
            label.style.fontSize = 11;
            label.pickingMode = PickingMode.Ignore;
            label.schedule.Execute(() =>
            {
                Rect bounds = anchor.layout;
                if (!bounds.IsValid()) return;
                label.style.left = bounds.xMin;
                label.style.top = bounds.yMax + 4;
            }).Every(250);
            return label;
        }

        private void TogglePanel()
        {
            if (PanelOpen) ClosePanel(); else OpenPanel();
        }

        private void OpenPanel()
        {
            appsPanel.RemoveFromClassList("hidden");
            appsPanel.SetEnabled(true);
            panelTravel.MoveTo(0);
            UpdateHint("apps panel opening");
        }

        private void ClosePanel() => panelTravel.MoveTo(1);

        /// <summary>Fades and slides the float panel from the spring, and parks it once shut.</summary>
        private void RenderPanel(float deltaTime)
        {
            panelTravel.Tick(deltaTime, reduceMotion: false);

            appsPanel.style.top = appsButton.layout.yMax + 8;
            appsPanel.style.opacity = 1 - Mathf.Clamp01(panelTravel.Value);
            appsPanel.style.translate = new Translate(0, -18 * panelTravel.Value);

            if (PanelOpen || panelTravel.IsMoving || appsPanel.ClassListContains("hidden")) return;
            // Never take an element out of the interaction while a press is live: a hidden or disabled
            // element receives nothing, so the release would never arrive and the gesture would never end.
            if (Busy) return;
            appsPanel.AddToClassList("hidden");
            appsPanel.SetEnabled(false);
        }

        // ====================================================================
        // Apps
        // ====================================================================

        private void BuildApps()
        {
            for (int i = 0; i < appCount; i++)
            {
                string id = $"app-{i}";
                apps.Add(id);
                if (i < Mathf.Clamp(onDesktopAtStart, 0, appCount)) onDesktop.Add(id);
                icons[id] = BuildIcon(id);
            }
        }

        private VisualElement BuildIcon(string id)
        {
            var icon = new VisualElement { name = id };
            icon.Add(Center(id.Replace("app-", ""), Color.white));

            // The hold belongs to the icon, and so does the drag - so both live here, on the element that
            // captures. App 0 never abandons its hold, the way a colour picker must not.
            var hold = new Gesture.LongPress
            {
                Duration = holdDuration,
                MoveTolerance = id.EndsWith("-0") ? 0 : moveTolerance
            };
            hold.Triggered += () =>
            {
                holds++;
                if (!marked.Add(id)) marked.Remove(id);
                icon.EnableInClassList("marked", marked.Contains(id));
                UpdateHint($"held {id}" + (hold.MoveTolerance <= 0 ? " (ignores movement)" : ""));
            };
            icon.AddManipulator(hold);

            icon.RegisterCallback<PointerDownEvent>(e => OnIconDown(e, id));
            icon.RegisterCallback<PointerMoveEvent>(e => OnIconMove(e, id));
            icon.RegisterCallback<PointerUpEvent>(e => OnIconUp(e, id, hold));
            icon.RegisterCallback<PointerCaptureOutEvent>(OnCaptureLost);
            return icon;
        }

        /// <summary>
        /// Reparents every app to wherever it lives and re-resolves the desktop. Panel items lay out in
        /// flow; desktop apps are positioned absolutely by <see cref="GroupDragLayout"/>.
        /// </summary>
        private void Sync(string active = null, Vector2? desired = null)
        {
            desktopOrder.Clear();
            foreach (string id in apps)
            {
                var icon = icons[id];
                if (onDesktop.Contains(id))
                {
                    desktopOrder.Add(id);
                    icon.RemoveFromClassList("panel-item");
                    icon.AddToClassList("desktop-app");
                    icon.style.position = Position.Absolute;
                    if (icon.parent != desktop) desktop.Add(icon);
                }
                else
                {
                    icon.RemoveFromClassList("desktop-app");
                    icon.AddToClassList("panel-item");
                    icon.style.position = Position.Relative;
                    icon.style.left = StyleKeyword.Null;
                    icon.style.top = StyleKeyword.Null;
                    icon.style.width = StyleKeyword.Null;
                    icon.style.height = StyleKeyword.Null;
                    icon.style.translate = StyleKeyword.Null;
                    if (icon.parent != appsPanel) appsPanel.Add(icon);
                }
            }
            RaiseChrome();
            Resolve(active, desired);
        }

        /// <summary>
        /// Stashing is only ever the result of a drop or a throw landing on Apps.
        /// <para>
        /// The wobble always plays on the Apps button, never on the float panel. A panel is a surface things
        /// are listed on, not a physical object that can be struck; wobbling it would say the panel moved,
        /// when what actually happened is that the button took an app in.
        /// </para>
        /// </summary>
        private void Stash(string id)
        {
            if (!onDesktop.Remove(id)) return;
            stashes++;
            Sync();
            shake.Play(appsButton, Reaction.Shake.Settings.Default, reduceMotion: false);
            UpdateHint($"{id} stashed in Apps");
        }

        private void Resolve(string active = null, Vector2? desired = null)
        {
            if (desktop == null || desktop.panel == null) return;
            Rect bounds = new Rect(0, 0, desktop.layout.width, desktop.layout.height);
            if (!bounds.IsValid()) return;

            layout.Resolve(desktopOrder, bounds, Obstacles, compact: false, active, desired);
            Apply();
        }

        private void Apply()
        {
            foreach (var pair in layout.Positions)
            {
                if (pair.Key == flying || !icons.TryGetValue(pair.Key, out var icon)) continue;
                icon.style.width = icon.style.height = layout.Size;
                icon.style.left = pair.Value.x - layout.Size / 2;
                icon.style.top = pair.Value.y - layout.Size / 2;
            }
        }

        /// <summary>
        /// Puts the fixed furniture back above the apps. Desktop apps are absolutely positioned, so sibling
        /// order only decides what draws over what - reordering them costs nothing.
        /// </summary>
        private void RaiseChrome()
        {
            appsButton.BringToFront();
            appsPanel.BringToFront();
            hint.BringToFront();
        }

        /// <summary>
        /// Lifts an app above everything while it is in play. An app being dragged or still flying can end
        /// up over the panel or the Apps button, and it has to be seen doing it - it is the thing the user
        /// is steering.
        /// <para>
        /// A desktop app is absolutely positioned, so raising it only changes what draws over what. A panel
        /// app is not: inside a flex row the child order <em>is</em> the layout order, so raising it there
        /// would send it to the last slot and shuffle every other app along. It is lifted out of the flow
        /// instead, with an invisible stand-in holding its place so the row does not close up behind it.
        /// </para>
        /// </summary>
        private void Raise(string id)
        {
            if (!icons.TryGetValue(id, out var icon)) return;

            if (onDesktop.Contains(id)) { icon.BringToFront(); return; }

            Rect slot = icon.layout;
            if (!slot.IsValid()) return;

            spacer = new VisualElement { pickingMode = PickingMode.Ignore };
            spacer.AddToClassList("panel-item");
            spacer.style.visibility = Visibility.Hidden;
            appsPanel.Insert(appsPanel.IndexOf(icon), spacer);

            icon.style.position = Position.Absolute;
            icon.style.left = slot.x;
            icon.style.top = slot.y;
            icon.style.width = slot.width;
            icon.style.height = slot.height;
            icon.BringToFront();
        }

        /// <summary>Puts the row back the way it was once the app being carried is dealt with.</summary>
        private void DropStandIn()
        {
            if (spacer == null) return;
            spacer.RemoveFromHierarchy();
            spacer = null;
        }

        /// <summary>The panel app the pointer is currently over, if any. The one being dragged never counts.</summary>
        private string SlotUnder(Vector2 world, string dragged)
        {
            foreach (string id in apps)
            {
                if (id == dragged || onDesktop.Contains(id)) continue;
                if (icons[id].worldBound.Contains(world)) return id;
            }
            return null;
        }

        private void MarkSlot(string id)
        {
            if (slotTarget == id) return;
            if (slotTarget != null && icons.TryGetValue(slotTarget, out var previous))
                previous.RemoveFromClassList("slot-target");
            slotTarget = id;
            if (slotTarget != null) icons[slotTarget].AddToClassList("slot-target");
        }

        /// <summary>Moves an app to sit where another one is, keeping the panel's own order meaningful.</summary>
        private void Reorder(string id, string before)
        {
            if (id == before) return;
            apps.Remove(id);
            apps.Insert(apps.IndexOf(before), id);
            Sync();
            UpdateHint($"moved {id} before {before}");
        }

        /// <summary>Where a drop would stash the app, or null if it would not.</summary>
        private VisualElement DropTarget(Vector2 local)
        {
            if (appsButton.layout.Contains(local)) return appsButton;
            if (PanelOpen && appsPanel.layout.Contains(local)) return appsPanel;
            return null;
        }

        // ====================================================================
        // Gesture plumbing - one owner per gesture
        // ====================================================================

        private void Begin(VisualElement element, int pointerId, Vector2 position)
        {
            pointer = pointerId;
            owner = element;
            element.CapturePointer(pointerId);
            dragSample = position;
            dragSampleTime = Time.unscaledTime;
            dragVelocity = Vector2.zero;
            if (trace) Debug.Log($"[trace] begin on {element.name} pointer={pointerId}");
        }

        private void End()
        {
            if (trace) Debug.Log($"[trace] end owner={owner?.name} pointer={pointer}");
            if (owner != null && pointer >= 0 && owner.HasPointerCapture(pointer)) owner.ReleasePointer(pointer);
            appsButton.RemoveFromClassList("drop-ready");
            appsButton.RemoveFromClassList("hinting");
            appsPanel.RemoveFromClassList("drop-ready");
            owner = null;
            pointer = -1;
            dragging = null;
        }

        /// <summary>
        /// The last resort. A capture can vanish without a release when its element is hidden, disabled or
        /// reparented; without this the gesture would stay open and every later press be ignored.
        /// </summary>
        private void OnCaptureLost(PointerCaptureOutEvent e)
        {
            if (e.target != owner) return;
            if (trace) Debug.Log($"[trace] capture lost from {(e.target as VisualElement)?.name}");
            appsButton.RemoveFromClassList("hinting");
            MarkSlot(null);
            DropStandIn();
            owner = null;
            pointer = -1;
            dragging = null;
        }

        private float Sample(Vector2 position)
        {
            float elapsed = Time.unscaledTime - dragSampleTime;
            if (elapsed <= 0) return 0;
            dragVelocity = PointerVelocity.Sample(dragVelocity, position - dragSample, elapsed);
            dragSample = position;
            dragSampleTime = Time.unscaledTime;
            return elapsed;
        }

        // ====================================================================
        // The app's own gesture
        // ====================================================================

        private void OnIconDown(PointerDownEvent e, string id)
        {
            if (Busy || e.button != 0) return;
            dragging = id;
            pressAt = e.position;
            Begin(icons[id], e.pointerId, e.position);
            appsButton.AddToClassList("hinting");
            Raise(id);
            e.StopPropagation();
        }

        private void OnIconMove(PointerMoveEvent e, string id)
        {
            if (e.pointerId != pointer || dragging != id) return;
            Sample(e.position);
            e.StopPropagation();

            // A panel item must not change parent mid-gesture - that would destroy the very capture it is
            // holding. It can still follow the finger: translate moves what is drawn without moving what
            // owns the pointer.
            if (!onDesktop.Contains(id))
            {
                Vector2 carried = (Vector2)e.position - pressAt;
                // Note there is no BringToFront here. In a flex container the child order *is* the layout
                // order, so raising the dragged item would send it to the last slot and shuffle every other
                // app along - which looks like the panel rearranging itself for no reason.
                icons[id].style.translate = new Translate(carried.x, carried.y);

                MarkSlot(SlotUnder(e.position, id));
                appsButton.EnableInClassList("drop-ready",
                    DropTarget(desktop.WorldToLocal(e.position)) == appsButton);
                return;
            }

            Vector2 local = desktop.WorldToLocal(e.position);
            Resolve(id, local);

            var over = DropTarget(local);
            appsButton.EnableInClassList("drop-ready", over == appsButton);
            appsPanel.EnableInClassList("drop-ready", over == appsPanel);
        }

        private void OnIconUp(PointerUpEvent e, string id, Gesture.LongPress hold)
        {
            if (e.pointerId != pointer || dragging != id) return;
            e.StopPropagation();

            Vector2 local = desktop.WorldToLocal(e.position);
            var over = DropTarget(local);
            bool quick = Time.unscaledTime - dragSampleTime < .1f;
            bool held = hold.IsHolding;
            if (held) hold.Release();

            string slot = slotTarget;
            MarkSlot(null);
            DropStandIn();

            // The carry offset was only ever a drawing trick; the real placement happens below.
            icons[id].style.translate = StyleKeyword.Null;

            // Release the capture before anything reparents an icon, so the reparent cannot cut it short.
            End();

            if (onDesktop.Contains(id))
            {
                if (over != null) Stash(id);
                else if (quick && dragVelocity.magnitude > throwThreshold) Launch(id, local);
                else layout.Commit();
            }
            else if (slot != null) Reorder(id, slot);
            else if (over == null && !appsPanel.worldBound.Contains(e.position))
            {
                // Dropped out of the panel onto open desktop: it lands where it was let go.
                onDesktop.Add(id);
                Sync(id, local);
                ClosePanel();
                UpdateHint($"pulled {id} out of Apps");
            }
            else Sync();   // let go inside the panel over nothing: put it back in its slot


            if (flying == null) RaiseChrome();

            if (held) return;   // a press that did its work as a hold is not also a click
            clicks++;
            UpdateHint($"clicked {id}");
        }

        // ====================================================================
        // The background's own gesture
        // ====================================================================

        private void OnDesktopDown(PointerDownEvent e)
        {
            if (Busy || e.button != 0) return;

            Vector2 local = desktop.WorldToLocal(e.position);
            if (PanelOpen && DropTarget(local) == null) ClosePanel();

            groupLast = local;
            layout.StopMotion();
            StopFlight();
            Begin(desktop, e.pointerId, e.position);
        }

        private void OnDesktopMove(PointerMoveEvent e)
        {
            if (e.pointerId != pointer || owner != desktop) return;
            float elapsed = Sample(e.position);

            Vector2 local = desktop.WorldToLocal(e.position);
            layout.DragGroup(groupLast, local - groupLast, Mathf.Max(.008f, elapsed), Obstacles);
            groupLast = local;
            Apply();
        }

        private void OnDesktopUp(PointerUpEvent e)
        {
            if (e.pointerId != pointer || owner != desktop) return;
            End();
        }

        // ====================================================================
        // Throw
        // ====================================================================

        private void Launch(string id, Vector2 from)
        {
            flight.Strikes = (start, end, speed) =>
            {
                // Swept, so a fast throw cannot pass straight through the obstacle between two frames.
                if (!SweptContact.SegmentHit(obstacle.layout, start, end, out _)) return false;
                UpdateHint($"{id} struck the obstacle at {speed:0} /s");
                return true;
            };

            // An app must leave Apps before it can be thrown into it, or one that started under the button
            // would be swallowed on its first frame - which reads as no throw at all.
            catchArmed = DropTarget(from) == null;

            flying = id;
            Raise(id);
            flight.Launch(from, desktop.WorldToLocal(dragSample + dragVelocity)
                                - desktop.WorldToLocal(dragSample));
            UpdateHint($"threw {id}");
        }

        /// <summary>
        /// Ends the flight, telling the layout where the app actually came to rest before committing.
        /// Committing first would cache the position it was thrown <em>from</em>, and the next resolve would
        /// put it back there - the app would appear to snap home the instant it stopped.
        /// </summary>
        private void Land()
        {
            if (flying == null) return;
            string landed = flying;
            Vector2 at = flight.Position;
            flight.Stop();
            flying = null;
            Resolve(landed, at);
            layout.Commit();
            RaiseChrome();
        }

        private void StopFlight()
        {
            if (flying == null) return;
            flight.Stop();
            flying = null;
        }

        private void TickFlight(float deltaTime)
        {
            if (flying == null) return;

            Rect bounds = new Rect(0, 0, desktop.layout.width, desktop.layout.height);
            float radius = layout.Size / 2;
            bool moving = flight.Tick(deltaTime, bounds, radius, throwSettings);

            var over = DropTarget(flight.Position);
            if (!catchArmed && over == null) catchArmed = true;

            if (icons.TryGetValue(flying, out var icon))
            {
                icon.style.left = flight.Position.x - radius;
                icon.style.top = flight.Position.y - radius;

                if (catchArmed && over != null)
                {
                    string caught = flying;
                    flight.Stop();
                    flying = null;
                    Stash(caught);
                    UpdateHint($"{caught} was thrown into Apps");
                    return;
                }
            }

            if (moving) return;
            UpdateHint($"{flying} came to rest");
            Land();
        }

        // ====================================================================
        // Entrance
        // ====================================================================

        private void StartOpening()
        {
            if (desktop == null || desktop.panel == null) return;
            Resolve();

            Rect frame = new Rect(0, 0, desktop.layout.width, desktop.layout.height);
            if (!frame.IsValid() || desktopOrder.Count == 0) return;

            var random = new System.Random(20260915);
            var track = Transition.ScaleTrack.From(t => t);
            var entrances = new List<Transition.Entrance>();

            for (int i = 0; i < desktopOrder.Count; i++)
            {
                string id = desktopOrder[i];
                if (!icons.TryGetValue(id, out var icon)) continue;

                Transition.Entrance.Schedule(i, desktopOrder.Count, duration: 1.4f, startInterval: .075f,
                    endTogether: .35f, out float start, out float end);
                Transition.Entrance.Plan(frame, random, layout.Size, Transition.ScaleRange.Default,
                    direction: new Vector2(-1, -1), angleRange: 90, clearance: 1.15f,
                    out Vector2 origin, out float scale);

                string captured = id;
                entrances.Add(new Transition.Entrance(new ElementTarget(icon), () => Home(captured),
                    absorb: false, origin, scale, start, end, layout.Size, track, t => t, t => t));
            }

            opening.Begin(entrances, timeout: 3);
            UpdateHint("entrance playing - press the background to skip");
        }

        private Rect Home(string id)
        {
            if (!layout.Positions.TryGetValue(id, out var centre)) return default;
            float size = layout.Size;
            return new Rect(centre.x - size / 2, centre.y - size / 2, size, size);
        }

        // ====================================================================
        // Frame
        // ====================================================================

        private void Update()
        {
            float deltaTime = Time.unscaledDeltaTime;
            RenderPanel(deltaTime);

            if (opening.IsPlaying)
            {
                if (Busy) { opening.Skip(); UpdateHint("entrance skipped"); }
                else if (!opening.Tick(deltaTime)) UpdateHint("entrance finished");
                shake.Tick(deltaTime);
                return;
            }

            if (owner != desktop && layout.IsMoving)
            {
                layout.AdvanceGroup(deltaTime, Obstacles);
                Apply();
            }

            TickFlight(deltaTime);
            shake.Tick(deltaTime);
        }

        private void UpdateHint(string message)
        {
            if (hint == null) return;
            hint.text =
                $"{message}\n\n" +
                "tap Apps = open / close the float panel\n" +
                "drag background = move the whole group (release for inertia)\n" +
                "drag a desktop app = reposition it; flick it = throw it\n" +
                "into Apps only by: dropping on the panel, dropping on the Apps icon,\n" +
                "  or a throw whose inertia carries it there\n" +
                "drag an app out of the panel and release = place it on the desktop\n" +
                "hold an app = mark it (app 0 ignores movement)\n" +
                $"desktop {onDesktop.Count}/{apps.Count}   clicks {clicks}   holds {holds}   stashed {stashes}";
        }
    }
}
