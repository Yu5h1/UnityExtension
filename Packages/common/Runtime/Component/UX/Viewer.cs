using System.Collections.Generic;
using UnityEngine;
using Yu5h1Lib.UX;

namespace Yu5h1Lib
{
    [DisallowMultipleComponent]
    public class Viewer : MonoBehaviour, INavigator<View>
    {
        public delegate bool BackHandler();

        [SerializeField]
        private View _current;
        [SerializeField]
        private View _baseView;
        [SerializeField]
        private bool _hidePrevious = true;
        [SerializeField]
        private bool _showTarget = true;
        [SerializeField]
        private bool _bringToFront = true;
        [SerializeField]
        private bool _showCurrentOnStart = true;

        private readonly List<BackHandler> _backHandlers = new List<BackHandler>();

        public View Current => _current;

        public View baseView
        {
            get => _baseView;
            set => _baseView = value;
        }

        public event BackHandler backHandlers
        {
            add
            {
                if (value != null && !_backHandlers.Contains(value))
                    _backHandlers.Add(value);
            }
            remove => _backHandlers.Remove(value);
        }

        public bool hidePrevious
        {
            get => _hidePrevious;
            set => _hidePrevious = value;
        }

        public bool showTarget
        {
            get => _showTarget;
            set => _showTarget = value;
        }

        public bool bringToFront
        {
            get => _bringToFront;
            set => _bringToFront = value;
        }

        private void Start()
        {
            if (_showCurrentOnStart && _current)
                Focus(_current);
        }

        public bool CanNavigate(View view)
        {
            return view;
        }

        public bool MoveTo(View view)
        {
            if (!CanNavigate(view))
                return false;

            if (_current == view)
            {
                Focus(view);
                return false;
            }

            if (_hidePrevious && _current)
                _current.Hide();

            _current = view;
            Focus(_current);
            return true;
        }

        public void Back()
        {
            for (int i = _backHandlers.Count - 1; i >= 0; i--)
            {
                if (_backHandlers[i]?.Invoke() == true)
                    return;
            }

            if (_baseView && _current != _baseView)
                MoveTo(_baseView);
        }

        public void Return() => Back();

        private void Focus(View view)
        {
            if (_showTarget)
                view.Show();
            if (_bringToFront)
                view.transform.SetAsLastSibling();
        }
    }
}
