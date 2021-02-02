// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager
{
    /// <summary>
    /// Scoped transactional store that uses an INI file as the persistent storage.
    /// </summary>
    public class IniFileStore : IScopedTransactionalStore
    {
        private readonly IFileSystem _fileSystem;
        private readonly IniSerializer _serializer;
        private readonly string _filePath;
        private readonly string _parentPath;
        private readonly object _fileLock = new object();
        private IniFile _iniFile;

        public IniFileStore(IFileSystem fileSystem, IniSerializer serializer, string filePath)
        {
            _fileSystem = fileSystem;
            _serializer = serializer;
            _filePath = filePath;
            _parentPath = Path.GetDirectoryName(_filePath);

            ReloadAsync();
        }

        public Task ReloadAsync()
        {
            lock (_fileLock)
            {
                if (_fileSystem.FileExists(_filePath))
                {
                    using (Stream fs = _fileSystem.OpenFileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var reader = new StreamReader(fs))
                    {
                        _iniFile = _serializer.Deserialize(reader);
                    }
                }
                else
                {
                    // Create a new empty INI file object
                    _iniFile = new IniFile();
                }
            }

            return Task.CompletedTask;
        }

        public Task CommitAsync()
        {
            lock (_fileLock)
            {
                // Ensure parent directory exists
                if (!_fileSystem.DirectoryExists(_parentPath))
                {
                    _fileSystem.CreateDirectory(_parentPath);
                }

                using (Stream fs = _fileSystem.OpenFileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.Write))
                using (var writer = new StreamWriter(fs))
                {
                    _serializer.Serialize(_iniFile, writer);
                }
            }

            return Task.CompletedTask;
        }

        public IEnumerable<string> GetSectionScopes(string sectionName)
        {
            lock (_fileLock)
            {
                IEnumerable<IniSection> sections = _iniFile.Sections
                    .Where(x => StringComparer.OrdinalIgnoreCase.Equals(x.Name, sectionName));
                foreach (var section in sections)
                {
                    yield return section.Scope;
                }
            }
        }

        public bool TryGetValue(string key, out string value)
        {
            lock (_fileLock)
            {
                value = null;

                if (!TrySplitKey(key, out string section, out string scope, out string property))
                {
                    throw new ArgumentException($"Invalid key '{key}'.", nameof(key));
                }

                return _iniFile.TryGetValue(section, scope, property, out value);
            }
        }

        public void SetValue(string key, string value)
        {
            lock (_fileLock)
            {
                if (!TrySplitKey(key, out string section, out string scope, out string property))
                {
                    throw new ArgumentException($"Invalid key '{key}'.", nameof(key));
                }

                _iniFile.SetValue(section, scope, property, value);
            }
        }

        public void Remove(string key)
        {
            lock (_fileLock)
            {
                if (!TrySplitKey(key, out string section, out string scope, out string property))
                {
                    throw new ArgumentException($"Invalid key '{key}'.", nameof(key));
                }

                _iniFile.UnsetValue(section, scope, property);
            }
        }

        private static bool TrySplitKey(string key, out string section, out string scope, out string property)
        {
            section = null;
            scope = null;
            property = null;

            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            int first = key.IndexOf('.');
            int last = key.LastIndexOf('.');

            if (first < 0 || last < 0)
            {
                return false;
            }

            // section.property
            if (first == last)
            {
                section = key.Substring(0, first);
                property = key.Substring(last + 1);

                return true;
            }

            // section.scope.maybe.with.periods.property
            section = key.Substring(0, first);
            scope = key.Substring(first + 1, last - first - 1);
            property = key.Substring(last + 1);

            return true;
        }
    }
}
