using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Utils
{
    public static class AvaloniaChildrenFinder
    {
        public static T? FindChildByClass<T>(this Visual visual, string className) where T : Control
        {
            return visual.GetVisualDescendants()
                .OfType<T>()
                .FirstOrDefault(x => x.Classes.Contains(className));
        }
    }
}
