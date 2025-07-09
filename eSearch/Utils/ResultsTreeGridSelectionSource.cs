using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace eSearch.Utils
{
    public class CustomTreeDataGridRowSelectionModel<TModel> : ITreeDataGridRowSelectionModel, INotifyPropertyChanged
    {
        private bool _isSelectAll;
        private readonly HashSet<IndexPath> _selectedIndices = new HashSet<IndexPath>();
        private bool _isBatchUpdating;
        private readonly ITreeDataGridSource<TModel> _source;
        private event EventHandler<TreeSelectionModelIndexesChangedEventArgs>? _indexesChanged;
        private List<IndexPath> _batchAdded = new List<IndexPath>();
        private List<IndexPath> _batchRemoved = new List<IndexPath>();
        private IndexPath _anchorIndex;
        private IndexPath _rangeAnchorIndex;

        public CustomTreeDataGridRowSelectionModel(ITreeDataGridSource<TModel> source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        event EventHandler<TreeSelectionModelIndexesChangedEventArgs>? ITreeSelectionModel.IndexesChanged
        {
            add => _indexesChanged += value;
            remove => _indexesChanged -= value;
        }

        public ITreeDataGridSource Source => _source;

        public event EventHandler<TreeSelectionModelSourceResetEventArgs>? SourceReset;

        public bool IsSelectAll
        {
            get => _isSelectAll;
            set
            {
                if (_isSelectAll != value)
                {
                    var previousSelected = SelectedIndexes.ToList();
                    _isSelectAll = value;
                    if (_isSelectAll)
                    {
                        _selectedIndices.Clear();
                    }
                    var newSelected = SelectedIndexes.ToList();
                    var added = newSelected.Except(previousSelected).ToList();
                    var removed = previousSelected.Except(newSelected).ToList();
                    RaiseIndexesChanged(added, removed);
                    RaisePropertyChanged(nameof(IsSelectAll));
                    RaiseSelectionChanged();
                }
            }
        }

        public IndexPath SelectedIndex
        {
            get => SelectedIndexes.FirstOrDefault();
            set
            {
                if (value != null)
                {
                    Clear();
                    Select((IndexPath)value);
                }
                else
                {
                    Clear();
                }
            }
        }

        public IReadOnlyList<IndexPath> SelectedIndexes =>
            _isSelectAll ? GetAllIndices().ToList() : _selectedIndices.ToList();

        public IReadOnlyList<object?> SelectedItems =>
            SelectedIndexes.Select(index => (object?)_source.Rows[index[0]].Model).ToList();

        public object? SelectedItem => SelectedItems.FirstOrDefault();

        public bool SingleSelect { get; set; }

        public event EventHandler<TreeSelectionModelSelectionChangedEventArgs>? IndexesChanged;

        public event PropertyChangedEventHandler? PropertyChanged;

        public event EventHandler<TreeSelectionModelSelectionChangedEventArgs>? SelectionChanged;

        public IndexPath RangeAnchorIndex
        {
            get => _rangeAnchorIndex;
            set => _rangeAnchorIndex = value;
        }

        public IndexPath AnchorIndex
        {
            get => _anchorIndex;
            set => _anchorIndex = value;
        }

        public int Count => SelectedIndexes.Count;

        IEnumerable? ITreeSelectionModel.Source
        {
            get => (IEnumerable?)_source;
            set => throw new NotImplementedException();
        }


        IEnumerable? ITreeDataGridSelection.Source
        {
            get => (IEnumerable?)_source;
            set => throw new NotImplementedException();
        }

        public void BeginBatchUpdate()
        {
            _isBatchUpdating = true;
            _batchAdded.Clear();
            _batchRemoved.Clear();
        }

        public void EndBatchUpdate()
        {
            _isBatchUpdating = false;
            RaiseIndexesChanged(_batchAdded, _batchRemoved);
            RaiseSelectionChanged();
            _batchAdded.Clear();
            _batchRemoved.Clear();
        }

        public void Select(IndexPath index)
        {
            if (_isSelectAll)
                return;

            var previousSelected = SelectedIndexes.ToList();
            if (_selectedIndices.Add(index))
            {
                if (SingleSelect)
                {
                    var toRemove = _selectedIndices.Except(new[] { index }).ToList();
                    foreach (var r in toRemove)
                    {
                        _selectedIndices.Remove(r);
                    }
                }
                var newSelected = SelectedIndexes.ToList();
                var added = newSelected.Except(previousSelected).ToList();
                var removed = previousSelected.Except(newSelected).ToList();
                RaiseIndexesChanged(added, removed);
                if (!_isBatchUpdating)
                    RaiseSelectionChanged();
            }
        }

        public void Deselect(IndexPath index)
        {
            if (_isSelectAll)
            {
                _isSelectAll = false;
                var allIndices = GetAllIndices().ToList();
                allIndices.Remove(index);
                _selectedIndices.Clear();
                _selectedIndices.UnionWith(allIndices);
                var previousSelected = GetAllIndices().ToList();
                var newSelected = SelectedIndexes.ToList();
                var added = newSelected.Except(previousSelected).ToList();
                var removed = previousSelected.Except(newSelected).ToList();
                RaiseIndexesChanged(added, removed);
                if (!_isBatchUpdating)
                    RaiseSelectionChanged();
            }
            else if (_selectedIndices.Remove(index))
            {
                var previousSelected = SelectedIndexes.ToList();
                var newSelected = SelectedIndexes.ToList();
                var added = new List<IndexPath>();
                var removed = previousSelected.Except(newSelected).ToList();
                RaiseIndexesChanged(added, removed);
                if (!_isBatchUpdating)
                    RaiseSelectionChanged();
            }
        }

        public void SelectAll()
        {
            IsSelectAll = true;
            RaiseSelectionChanged();
        }

        public void Clear()
        {
            if (_isSelectAll || _selectedIndices.Count > 0)
            {
                var previousSelected = SelectedIndexes.ToList();
                _isSelectAll = false;
                _selectedIndices.Clear();
                var newSelected = SelectedIndexes.ToList();
                var added = new List<IndexPath>();
                var removed = previousSelected;
                RaiseIndexesChanged(added, removed);
                if (!_isBatchUpdating)
                    RaiseSelectionChanged();
            }
        }

        public bool IsSelected(IndexPath index)
        {
            return _isSelectAll || _selectedIndices.Contains(index);
        }

        private IEnumerable<IndexPath> GetAllIndices()
        {
            for (int i = 0; i < _source.Rows.Count; i++)
            {
                yield return new IndexPath(i);
            }
        }

        private void RaiseSelectionChanged()
        {
            var args = new TreeSelectionModelSelectionChangedEventArgs<TModel>();
            SelectionChanged?.Invoke(this, args);
        }

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void RaiseIndexesChanged(List<IndexPath> added, List<IndexPath> removed)
        {
            if (_isBatchUpdating)
            {
                _batchAdded.AddRange(added);
                _batchRemoved.AddRange(removed);
            }
            else
            {
                // The following logic is me fudging it, I'm not actually sure this is what it's expecting here.
                IndexPath parentIndex = added.FirstOrDefault( removed.FirstOrDefault( new IndexPath()));
                int startIndex = added.Any() ? added.Min(idx => idx[0]) : (removed.Any() ? removed.Min(idx => idx[0]) : 0);
                int endIndex = added.Any() ? added.Max(idx => idx[0]) : (removed.Any() ? removed.Max(idx => idx[0]) : 0);
                int delta = added.Count - removed.Count;

                var args = new TreeSelectionModelIndexesChangedEventArgs(parentIndex, startIndex, endIndex, delta);
                _indexesChanged?.Invoke(this, args);
            }
        }
    }
}