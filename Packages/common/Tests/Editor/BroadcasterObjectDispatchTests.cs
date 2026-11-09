// Verifies explicit object subscriptions, method dispatch, proxy arguments and teardown safety.
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Yu5h1Lib.Tests
{
    public class BroadcasterObjectDispatchTests
    {
        private static readonly FieldInfo InstanceField = typeof(SingletonBehaviour<Broadcaster>)
            .GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly FieldInfo QuittingField = typeof(Broadcaster)
            .GetField("_quitting", BindingFlags.NonPublic | BindingFlags.Static);
        private readonly List<Object> _owned = new();
        private object _previousInstance, _previousQuitting;
        private Broadcaster _hub;
        private BroadcasterProxy _proxy;

        [SetUp]
        public void SetUp()
        {
            _previousInstance = InstanceField.GetValue(null);
            _previousQuitting = QuittingField.GetValue(null);
            QuittingField.SetValue(null, false);
            var root = Own(new GameObject("Broadcaster test"));
            _hub = root.AddComponent<Broadcaster>();
            _proxy = root.AddComponent<BroadcasterProxy>();
            InstanceField.SetValue(null, _hub);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _owned.Count - 1; i >= 0; i--)
                if (_owned[i]) Object.DestroyImmediate(_owned[i]);
            _owned.Clear();
            InstanceField.SetValue(null, _previousInstance);
            QuittingField.SetValue(null, _previousQuitting);
        }

        private T Own<T>(T target) where T : Object
        {
            _owned.Add(target);
            return target;
        }

        private BroadcasterObjectProbe Probe() => Own(ScriptableObject.CreateInstance<BroadcasterObjectProbe>());

        [Test]
        public void OnlyExplicitlyRegisteredObjectsReceiveCalls()
        {
            var registered = Probe();
            var other = Probe();
            _hub.Register(registered);
            _hub.Register(registered);
            Assert.IsTrue(_hub.Dispatch("Record", "signal"));
            Assert.AreEqual(1, registered.calls);
            Assert.AreEqual("signal", registered.value);
            Assert.AreEqual(0, other.calls);
            _hub.Unregister(registered);
            Assert.IsFalse(_hub.Dispatch("Record", "signal"));
        }

        [Test]
        public void ZeroArgumentsAndOneNullArgumentSelectDifferentOverloads()
        {
            var target = Probe();
            _hub.Register(target);
            Assert.IsTrue(_hub.Dispatch("Record"));
            Assert.AreEqual("zero", target.value);
            Assert.IsTrue(_hub.Dispatch<string>("Record", null));
            Assert.IsNull(target.value);
            Assert.AreEqual(2, target.calls);
        }

        [Test]
        public void NonNullArgumentsUseTheirRuntimeType()
        {
            var target = Probe();
            _hub.Register(target);
            Object argument = target;
            Assert.IsTrue(_hub.Dispatch("Select", argument));
            Assert.AreEqual("probe", target.value);
        }

        [Test]
        public void InheritedPublicMethodsCanBeInvoked()
        {
            var target = Probe();
            _hub.Register(target);
            Assert.IsTrue(_hub.Dispatch("Inherited"));
            Assert.AreEqual(1, target.inheritedCalls);
        }

        [Test]
        public void UnregisterDuringDispatchSkipsTheRemovedObject()
        {
            var first = Probe();
            var removed = Probe();
            _hub.Register(first);
            _hub.Register(removed);
            first.callback = () => _hub.Unregister(removed);
            _hub.Dispatch("Record");
            Assert.AreEqual(1, first.calls);
            Assert.AreEqual(0, removed.calls);
        }

        [Test]
        public void NestedDispatchDoesNotCorruptTheOuterSnapshot()
        {
            var first = Probe();
            var second = Probe();
            _hub.Register(first);
            _hub.Register(second);
            first.callback = () =>
            {
                first.callback = null;
                _hub.Dispatch("Record", "nested");
            };
            _hub.Dispatch("Record");
            Assert.AreEqual(2, first.calls);
            Assert.AreEqual(2, second.calls);
        }

        [Test]
        public void NewlyRegisteredObjectsWaitForTheNextDispatch()
        {
            var first = Probe();
            var added = Probe();
            _hub.Register(first);
            first.callback = () => _hub.Register(added);
            _hub.Dispatch("Record");
            Assert.AreEqual(0, added.calls);
            _hub.Dispatch("Record");
            Assert.AreEqual(1, added.calls);
        }

        [Test]
        public void ReceiverCallsRemainLocalUntilExplicitlyRegistered()
        {
            var receiver = Own(new GameObject("receiver")).AddComponent<MessageReceiver>();
            int calls = 0;
            var entry = new UnityEvent();
            entry.AddListener(() => calls++);
            receiver.events.Add("test", entry);
            receiver.Call("test");
            Assert.AreEqual(1, calls);
            Assert.IsFalse(_hub.Dispatch("Call", "test"));
            _hub.Register(receiver);
            _hub.Dispatch("Call", "test");
            Assert.AreEqual(2, calls);
            receiver.enabled = false;
            Assert.IsFalse(_hub.Dispatch("Call", "test"));
        }

        [Test]
        public void ProxyStoresTheMethodNameAndSuppliesNativeEventArguments()
        {
            var target = Probe();
            _proxy.methodName = "Record";
            _proxy.Register(target);
            _proxy.Dispatch();
            _proxy.Dispatch("payload");
            Assert.AreEqual(2, target.calls);
            Assert.AreEqual("payload", target.value);
            _proxy.methodName = "SetNumber";
            _proxy.Dispatch(42);
            Assert.AreEqual(42, target.number);
            _proxy.Unregister(target);
            Assert.IsFalse(_hub.Dispatch("Record"));
        }

        [Test]
        public void MissingMethodsAndTargetExceptionsDoNotBlockOtherObjects()
        {
            var failed = Probe();
            var healthy = Probe();
            failed.throwOnCall = true;
            _hub.Register(failed);
            _hub.Register(healthy);
            Assert.IsFalse(_hub.Dispatch("MissingMethod"));
            Assert.IsTrue(_hub.Dispatch("Record"));
            Assert.AreEqual(1, healthy.calls);
        }

        [Test]
        public void DisablingHubClearsSubscriptions()
        {
            var target = Probe();
            _hub.Register(target);
            _hub.enabled = false;
            _hub.enabled = true;
            Assert.IsFalse(_hub.Dispatch("Record"));
        }

        [Test]
        public void DestroyedTargetsAreIgnoredAndPruned()
        {
            var target = Probe();
            _hub.Register(target);
            Object.DestroyImmediate(target);
            Assert.IsFalse(_hub.Dispatch("Record"));
        }

        [Test]
        public void UnregisterAfterHubDestructionDoesNotCreateAnotherHub()
        {
            var target = Probe();
            _hub.Register(target);
            Object.DestroyImmediate(_hub);
            int count = ObjectUtility.FindObjects<Broadcaster>().Length;
            _proxy.Unregister(target);
            Assert.AreEqual(count, ObjectUtility.FindObjects<Broadcaster>().Length);
        }

        [Test]
        public void QuitClearsSubscriptionsAndPreventsHubLookupOrDispatch()
        {
            var target = Probe();
            _hub.Register(target);
            typeof(Broadcaster).GetMethod("OnApplicationQuit", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(_hub, null);
            Assert.IsNull(Broadcaster.instance);
            Assert.IsFalse(_hub.Dispatch("Record"));
            _proxy.Unregister(target);
            Assert.AreEqual(0, target.calls);
        }
    }

    public class BroadcasterObjectProbeBase : ScriptableObject
    {
        public int inheritedCalls;
        public void Inherited() => inheritedCalls++;
    }

    public class BroadcasterObjectProbe : BroadcasterObjectProbeBase
    {
        public int calls, number;
        public string value;
        public bool throwOnCall;
        public Action callback;

        public void Record()
        {
            if (throwOnCall) throw new InvalidOperationException("probe failure");
            calls++;
            value = "zero";
            callback?.Invoke();
        }

        public void Record(string argument)
        {
            calls++;
            value = argument;
            callback?.Invoke();
        }

        public void Select(Object argument) => value = "object";
        public void Select(BroadcasterObjectProbe argument) => value = "probe";
        public void SetNumber(int argument) => number = argument;
    }
}
