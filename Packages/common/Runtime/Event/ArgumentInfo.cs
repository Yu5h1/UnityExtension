using System;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Yu5h1Lib
{
    [Serializable]
    public class ArgumentInfo
    {
        [SerializeField] private string _targetName;
        [SerializeField] private string _argumentAssemblyTypeName;
        [SerializeField,StringOptionsContext("Properties")] private string _methodName;

        [SerializeField] private PersistentListenerMode _listenerMode;
        [SerializeField] private Object _objectArgument;
        [SerializeField] private int _intArgument;
        [SerializeField] private float _floatArgument;
        [SerializeField] private string _stringArgument;
        [SerializeField] private bool _boolArgument;

        public string targetName { get => _targetName; set => _targetName = value; }
        public string methodName { get => _methodName; set => _methodName = value; }
        public PersistentListenerMode listenerMode { get => _listenerMode; set => _listenerMode = value; }
        public Object objectArgument { get => _objectArgument; set => _objectArgument = value; }
        public string argumentAssemblyTypeName { get => _argumentAssemblyTypeName; set => _argumentAssemblyTypeName = value; }
        public int intArgument { get => _intArgument; set => _intArgument = value; }
        public float floatArgument { get => _floatArgument; set => _floatArgument = value; }
        public string stringArgument { get => _stringArgument; set => _stringArgument = value; }
        public bool boolArgument { get => _boolArgument; set => _boolArgument = value; }
        public Type argumentType
        {
            get
            {
                switch (_listenerMode)
                {
                    case PersistentListenerMode.Int:
                        return typeof(int);

                    case PersistentListenerMode.Float:
                        return typeof(float);

                    case PersistentListenerMode.String:
                        return typeof(string);

                    case PersistentListenerMode.Bool:
                        return typeof(bool);

                    case PersistentListenerMode.Object:
                        return _objectArgument != null ? _objectArgument.GetType() : typeof(Object);

                    default:
                        return typeof(void);
                }
            }
        }
        public object value
        {
            get
            {
                switch (_listenerMode)
                {
                    case PersistentListenerMode.Int:
                        return _intArgument;

                    case PersistentListenerMode.Float:
                        return _floatArgument;

                    case PersistentListenerMode.String:
                        return _stringArgument;

                    case PersistentListenerMode.Bool:
                        return _boolArgument;

                    case PersistentListenerMode.Object:
                        return _objectArgument;

                    default:
                        return null;
                }
            }
        }
    }
}
