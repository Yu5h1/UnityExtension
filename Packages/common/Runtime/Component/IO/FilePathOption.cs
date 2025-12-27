using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib
{
    public class FilePathOption : OptionSet<string>
    {
        public enum PathType { Files, Directories, Both }
        public enum DisplayMode { FullPath, RelativePath, FileNameWithExtension, FileNameOnly }
        public enum RelativeRoot { Custom, DataPath, StreamingAssets, PersistentData }

        [Header("Path Settings")]
        [SerializeField] private RelativeRoot _relativeRoot = RelativeRoot.Custom;
        [SerializeField] private string _rootPath = "";
        [SerializeField] private bool _useRelativePath = true;
        [SerializeField] private PathType _pathType = PathType.Files;
        [SerializeField] private SearchOption _searchOption = SearchOption.TopDirectoryOnly;

        [Header("Filter")]
        [SerializeField] private List<FileTypeFilter> _filters = new List<FileTypeFilter>();

        [SerializeField] private string[] _excludes;
        public string[] excludes => _excludes;

        [Header("Display")]
        [SerializeField] private DisplayMode _displayMode = DisplayMode.FileNameWithExtension;

        public RelativeRoot relativeRoot
        {
            get => _relativeRoot;
            set
            {
                if (_relativeRoot != value)
                {
                    _relativeRoot = value;
                    Reload();
                }
            }
        }

        public string rootPath
        {
            get => _rootPath;
            set
            {
                if (_rootPath != value)
                {
                    _rootPath = value;
                    Reload();
                }
            }
        }

        public List<FileTypeFilter> filters
        {
            get => _filters;
            set
            {
                _filters = value;
                Reload();
            }
        }

        /// <summary>
        /// Gets the base path based on RelativeRoot setting
        /// </summary>
        public string BasePath
        {
            get
            {
                switch (_relativeRoot)
                {
                    case RelativeRoot.StreamingAssets:
                        return Application.streamingAssetsPath;
                    case RelativeRoot.PersistentData:
                        return Application.persistentDataPath;
                    case RelativeRoot.DataPath:
                        return Application.dataPath;
                    case RelativeRoot.Custom:
                    default:
                        return "";
                }
            }
        }

        /// <summary>
        /// Gets the resolved root path
        /// </summary>
        public string ResolvedRootPath
        {
            get
            {
                var basePath = BasePath;
                
                if (string.IsNullOrEmpty(_rootPath))
                    return string.IsNullOrEmpty(basePath) ? Application.dataPath : basePath;

                if (_relativeRoot == RelativeRoot.Custom)
                {
                    return Path.IsPathRooted(_rootPath) 
                        ? _rootPath 
                        : Path.Combine(Application.dataPath, _rootPath);
                }

                return Path.Combine(basePath, _rootPath);
            }
        }

        [SerializeField] private UnityEvent<IList<string>> _refreshed = new UnityEvent<IList<string>>();
        public event UnityAction<IList<string>> refreshed
        {
            add => _refreshed.AddListener(value);
            remove => _refreshed.RemoveListener(new UnityAction<IList<string>>(value));
        }
        private void Start()
        {
            Reload();
        }
        [ContextMenu(nameof(Reload))]
        public void Reload()
        {
            _Items.Clear();

            string root = ResolvedRootPath;
            if (!Directory.Exists(root))
            {
                Debug.LogWarning($"[FilePathOption] Directory not found: {root}");
                return;
            }

            try
            {
                var paths = new List<string>();

                if (_pathType == PathType.Directories || _pathType == PathType.Both)
                {
                    paths.AddRange(Directory.GetDirectories(root, "*", _searchOption));
                }

                if (_pathType == PathType.Files || _pathType == PathType.Both)
                {
                    var allowedExtensions = _filters.GetFileTypes();
                    var allFiles = _filters.SelectMany(f => PathInfo.GetFiles(root, f.ToSearchFilter(), _searchOption));

                    if (allowedExtensions.Length == 0 || allowedExtensions.Contains("*"))
                    {
                        paths.AddRange(allFiles);
                    }
                    else
                    {
                        paths.AddRange(allFiles.Where(f =>
                            allowedExtensions.Any(ext =>
                                f.EndsWith("." + ext, StringComparison.OrdinalIgnoreCase))));
                    }
                }

                foreach (var path in paths.OrderBy(p => p))
                {
                    var item = _useRelativePath ? GetRelativePath(root, path) : path;
                    if (excludes.Contains(item))
                        continue;
                    _Items.Add(item);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[FilePathOption] Error scanning directory: {e.Message}");
            }
            _refreshed?.Invoke(_Items);
        }

        public string GetFullPath()
        {
            if (string.IsNullOrEmpty(current))
                return string.Empty;

            return _useRelativePath ? Path.Combine(ResolvedRootPath, current) : current;
        }

        public string GetFullPath(int index)
        {
            if (!Items.IsValid(index))
                return string.Empty;

            var path = Items[index];
            return _useRelativePath ? Path.Combine(ResolvedRootPath, path) : path;
        }

        public override string ToString(string item)
        {
            if (string.IsNullOrEmpty(item))
                return string.Empty;

            switch (_displayMode)
            {
                case DisplayMode.FullPath:
                    return _useRelativePath ? Path.Combine(ResolvedRootPath, item) : item;
                case DisplayMode.RelativePath:
                    return _useRelativePath ? item : GetRelativePath(ResolvedRootPath, item);
                case DisplayMode.FileNameWithExtension:
                    return Path.GetFileName(item);
                case DisplayMode.FileNameOnly:
                    return Path.GetFileNameWithoutExtension(item);
                default:
                    return item;
            }
        }

        private string GetRelativePath(string root, string fullPath)
        {
            if (fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                var relative = fullPath.Substring(root.Length);
                return relative.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            return fullPath;
        }

        protected override void OnInitializing()
        {
            base.OnInitializing();
            if (_Items == null)
                _Items = new List<string>();
        }
    }
}
