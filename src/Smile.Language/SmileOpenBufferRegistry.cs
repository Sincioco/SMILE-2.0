using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Smile.Language;

public sealed class SmileOpenBufferRegistry
{
    private readonly object _gate = new();
    private readonly Dictionary<string, BufferState> _buffers = new(StringComparer.OrdinalIgnoreCase);

    public int OpenBufferCount
    {
        get { lock (_gate) return _buffers.Count; }
    }

    public IDisposable Register(string filePath, string currentText, Action invalidate)
    {
        var normalizedPath = Path.GetFullPath(filePath);
        lock (_gate)
        {
            if (!_buffers.TryGetValue(normalizedPath, out var state))
                _buffers[normalizedPath] = state = new BufferState();
            state.CurrentText = currentText;
            state.Invalidations.Add(invalidate);
        }
        return new BufferRegistration(this, normalizedPath, invalidate);
    }

    public bool TryGetText(string filePath, out string text)
    {
        var normalizedPath = Path.GetFullPath(filePath);
        lock (_gate)
        {
            if (_buffers.TryGetValue(normalizedPath, out var state))
            {
                text = state.CurrentText;
                return true;
            }
        }
        text = string.Empty;
        return false;
    }

    public void Update(string filePath, string currentText)
    {
        var normalizedPath = Path.GetFullPath(filePath);
        lock (_gate)
        {
            if (_buffers.TryGetValue(normalizedPath, out var state))
                state.CurrentText = currentText;
        }
    }

    public IReadOnlyList<Action> GetInvalidations(IEnumerable<string> sourcePaths)
    {
        lock (_gate)
            return sourcePaths.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(_buffers.ContainsKey)
                .SelectMany(path => _buffers[path].Invalidations).Distinct().ToArray();
    }

    public int GetInvalidationCount(string filePath)
    {
        var normalizedPath = Path.GetFullPath(filePath);
        lock (_gate)
            return _buffers.TryGetValue(normalizedPath, out var state) ? state.Invalidations.Count : 0;
    }

    private void Unregister(string sourcePath, Action invalidate)
    {
        lock (_gate)
        {
            if (!_buffers.TryGetValue(sourcePath, out var state))
                return;
            state.Invalidations.Remove(invalidate);
            if (state.Invalidations.Count == 0)
                _buffers.Remove(sourcePath);
        }
    }

    private sealed class BufferState
    {
        public string CurrentText { get; set; } = string.Empty;
        public HashSet<Action> Invalidations { get; } = new();
    }

    private sealed class BufferRegistration : IDisposable
    {
        private SmileOpenBufferRegistry? _owner;
        private readonly string _sourcePath;
        private readonly Action _invalidate;

        public BufferRegistration(SmileOpenBufferRegistry owner, string sourcePath, Action invalidate)
        { _owner = owner; _sourcePath = sourcePath; _invalidate = invalidate; }

        public void Dispose()
        {
            var owner = _owner;
            if (owner == null)
                return;
            _owner = null;
            owner.Unregister(_sourcePath, _invalidate);
        }
    }
}
