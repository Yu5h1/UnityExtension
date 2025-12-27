using System;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

namespace MotionGen
{
    /// <summary>
    /// 檔案變更事件參數
    /// </summary>
    [Serializable]
    public class FileChangedEventArgs
    {
        public string FilePath;
        public DateTime ModifiedTime;
        public string Content;
    }

    /// <summary>
    /// 檔案變更 UnityEvent
    /// </summary>
    [Serializable]
    public class FileChangedEvent : UnityEvent<FileChangedEventArgs> { }

    /// <summary>
    /// 檔案錯誤 UnityEvent
    /// </summary>
    [Serializable]
    public class FileErrorEvent : UnityEvent<string> { }

    /// <summary>
    /// 通用檔案監控器
    /// </summary>
    public class FileWatcher : MonoBehaviour
    {
        [Tooltip("檢查間隔（秒），0 = 每幀檢查")]
        [Range(0f, 2f)]
        [SerializeField] private float _checkInterval = 0.1f;

        [FilePath]
        [SerializeField] private string _filePath;

        [Header("Events")]
        [SerializeField] private FileChangedEvent _onFileChanged = new FileChangedEvent();
        [SerializeField] private FileErrorEvent _onFileError = new FileErrorEvent();

        private DateTime _lastModified;
        private float _nextCheckTime;

        #region Properties

        public float CheckInterval
        {
            get => _checkInterval;
            set => _checkInterval = Mathf.Max(0f, value);
        }

        public string FilePath
        {
            get => _filePath;
            set => _filePath = value;
        }

        public bool IsValidPath => !string.IsNullOrEmpty(_filePath) && File.Exists(_filePath);

        #endregion

        #region Events

        public event UnityAction<FileChangedEventArgs> OnFileChanged
        {
            add => _onFileChanged.AddListener(value);
            remove => _onFileChanged.RemoveListener(value);
        }

        public event UnityAction<string> OnFileError
        {
            add => _onFileError.AddListener(value);
            remove => _onFileError.RemoveListener(value);
        }

        #endregion

        #region Unity Lifecycle

        protected virtual void OnEnable()
        {
            _lastModified = DateTime.MinValue;
            _nextCheckTime = 0f;

            // 立即檢查一次
            if (IsValidPath)
            {
                CheckFileChange();
            }
        }

        protected virtual void Update()
        {
            if (!IsValidPath)
                return;

            if (Time.time < _nextCheckTime)
                return;

            _nextCheckTime = Time.time + _checkInterval;
            CheckFileChange();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 強制重新載入檔案
        /// </summary>
        public void Reload()
        {
            _lastModified = DateTime.MinValue;
            CheckFileChange();
        }

        /// <summary>
        /// 立即檢查檔案變更
        /// </summary>
        public void CheckNow()
        {
            CheckFileChange();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// 檔案變更時呼叫，子類可覆寫處理內容
        /// </summary>
        protected virtual void OnFileContentChanged(FileChangedEventArgs args)
        {
            _onFileChanged.Invoke(args);
        }

        /// <summary>
        /// 發生錯誤時呼叫
        /// </summary>
        protected virtual void OnError(string message)
        {
            _onFileError.Invoke(message);
        }

        #endregion

        #region Private Methods

        private void CheckFileChange()
        {
            if (string.IsNullOrEmpty(_filePath))
                return;

            if (!File.Exists(_filePath))
            {
                OnError($"File not found: {_filePath}");
                return;
            }

            try
            {
                var modTime = File.GetLastWriteTime(_filePath);
                if (modTime > _lastModified)
                {
                    _lastModified = modTime;
                    LoadFile();
                }
            }
            catch (Exception e)
            {
                OnError($"Check failed: {e.Message}");
            }
        }

        private void LoadFile()
        {
            try
            {
                string content = File.ReadAllText(_filePath);
                
                var args = new FileChangedEventArgs
                {
                    FilePath = _filePath,
                    ModifiedTime = _lastModified,
                    Content = content
                };

                OnFileContentChanged(args);
            }
            catch (Exception e)
            {
                OnError($"Load failed: {e.Message}");
            }
        }

        #endregion
    }
}
