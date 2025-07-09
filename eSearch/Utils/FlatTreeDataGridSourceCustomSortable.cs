using Avalonia.Controls.Models;
using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using Avalonia.Input;
using System.ComponentModel;

namespace eSearch.Utils
{
    public class FlatTreeDataGridSourceCustomSortable<TModel> :
        NotifyingBase,
        ITreeDataGridSource<TModel>,
        IDisposable
            where TModel : class
    {
        private IEnumerable<TModel> _items;
        private TreeDataGridItemsSourceView<TModel> _itemsView;
        private AnonymousSortableRows<TModel>? _rows;
        private IComparer<TModel>? _comparer;
        private ITreeDataGridSelection? _selection;
        private bool _isSelectionSet;

        public delegate void SortEventHandler(IColumn? column, ListSortDirection direction);

        public event SortEventHandler SortEvent;

        public FlatTreeDataGridSourceCustomSortable(IEnumerable<TModel> items)
        {
            _items = items;
            _itemsView = TreeDataGridItemsSourceView<TModel>.GetOrCreate(items);
            Columns = new ColumnList<TModel>();
        }

        public void SetItems(IEnumerable<TModel> items)
        {
            Items = items;
        }

        public ColumnList<TModel> Columns { get; }
        public IRows Rows => _rows ??= CreateRows();
        IColumns ITreeDataGridSource.Columns => Columns;

        public IEnumerable<TModel> Items
        {
            get => _items;
            set
            {
                if (_items != value)
                {
                    _items = value;
                    _itemsView = TreeDataGridItemsSourceView<TModel>.GetOrCreate(value);
                    _rows?.SetItems(_itemsView);
                    if (_selection is object)
                        _selection.Source = value;
                    RaisePropertyChanged();
                }
            }
        }

        public ITreeDataGridSelection? Selection
        {
            get
            {
                if (_selection == null && !_isSelectionSet)
                    _selection = new TreeDataGridRowSelectionModel<TModel>(this);
                return _selection;
            }
            set
            {
                if (_selection != value)
                {
                    if (value?.Source != _items)
                        throw new InvalidOperationException("Selection source must be set to Items.");
                    _selection = value;
                    _isSelectionSet = true;
                    RaisePropertyChanged();
                }
            }
        }

        IEnumerable<object> ITreeDataGridSource.Items => Items;

        public ITreeDataGridCellSelectionModel<TModel>? CellSelection => Selection as ITreeDataGridCellSelectionModel<TModel>;
        public ITreeDataGridRowSelectionModel<TModel>? RowSelection => Selection as ITreeDataGridRowSelectionModel<TModel>;
        public bool IsHierarchical => false;
        public bool IsSorted => true;

        public event Action? Sorted;

        public void Dispose()
        {
            _rows?.Dispose();
            GC.SuppressFinalize(this);
        }

        void ITreeDataGridSource.DragDropRows(
            ITreeDataGridSource source,
            IEnumerable<IndexPath> indexes,
            IndexPath targetIndex,
            TreeDataGridRowDropPosition position,
            DragDropEffects effects)
        {
            if (effects != DragDropEffects.Move)
                throw new NotSupportedException("Only move is currently supported for drag/drop.");
            if (IsSorted)
                throw new NotSupportedException("Drag/drop is not supported on sorted data.");
            if (position == TreeDataGridRowDropPosition.Inside)
                throw new ArgumentException("Invalid drop position.", nameof(position));
            if (indexes.Any(x => x.Count != 1))
                throw new ArgumentException("Invalid source index.", nameof(indexes));
            if (targetIndex.Count != 1)
                throw new ArgumentException("Invalid target index.", nameof(targetIndex));
            if (_items is not IList<TModel> items)
                throw new InvalidOperationException("Items does not implement IList<T>.");

            if (position == TreeDataGridRowDropPosition.None)
                return;

            var ti = targetIndex[0];

            if (position == TreeDataGridRowDropPosition.After)
                ++ti;

            var sourceItems = new List<TModel>();

            foreach (var src in indexes.OrderByDescending(x => x))
            {
                var i = src[0];
                sourceItems.Add(items[i]);
                items.RemoveAt(i);

                if (i < ti)
                    --ti;
            }

            for (var si = sourceItems.Count - 1; si >= 0; --si)
            {
                items.Insert(ti++, sourceItems[si]);
            }
        }

        /// <summary>
        /// HACK to get the arrows to appear...
        /// </summary>
        public void InvokeSortedEvent()
        {
            Sorted?.Invoke();
        }

        bool ITreeDataGridSource.SortBy(IColumn? column, ListSortDirection direction)
        {
            SortEvent?.Invoke(column, direction);
            Sorted?.Invoke();
            foreach (var c in Columns)
                c.SortDirection = c == column ? direction : null;
            return true;
        }

        IEnumerable<object> ITreeDataGridSource.GetModelChildren(object model)
        {
            return Enumerable.Empty<object>();
        }

        private AnonymousSortableRows<TModel> CreateRows()
        {
            return new AnonymousSortableRows<TModel>(_itemsView, _comparer);
        }
    }
}
