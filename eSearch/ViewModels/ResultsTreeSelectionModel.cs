using Avalonia.Controls;
using Avalonia.Controls.Selection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.ViewModels
{
    internal class ResultsTreeSelectionModel : ITreeSelectionModel
    {
        public IEnumerable? Source { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool SingleSelect { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IndexPath SelectedIndex { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IReadOnlyList<IndexPath> SelectedIndexes => throw new NotImplementedException();

        public object? SelectedItem => throw new NotImplementedException();

        public IReadOnlyList<object?> SelectedItems => throw new NotImplementedException();

        public IndexPath AnchorIndex { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IndexPath RangeAnchorIndex { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public int Count => throw new NotImplementedException();

        public event EventHandler<TreeSelectionModelSelectionChangedEventArgs>? SelectionChanged;
        public event EventHandler<TreeSelectionModelIndexesChangedEventArgs>? IndexesChanged;
        public event EventHandler<TreeSelectionModelSourceResetEventArgs>? SourceReset;
        public event PropertyChangedEventHandler? PropertyChanged;

        public void BeginBatchUpdate()
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public void Deselect(IndexPath index)
        {
            throw new NotImplementedException();
        }

        public void EndBatchUpdate()
        {
            throw new NotImplementedException();
        }

        public bool IsSelected(IndexPath index)
        {
            throw new NotImplementedException();
        }

        public void Select(IndexPath index)
        {
            throw new NotImplementedException();
        }
    }
}
