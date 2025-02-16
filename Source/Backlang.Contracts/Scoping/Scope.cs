﻿using Backlang.Contracts.Scoping.Items;

namespace Backlang.Contracts.Scoping;

#nullable enable

public class Scope
{
    private readonly List<ScopeItem> _items = new();

    public Scope(Scope parent)
    {
        Parent = parent;
        TypeAliases = new Dictionary<string, IType>();
    }

    public Dictionary<string, IType> TypeAliases { get; set; }
    public Scope Parent { get; set; }

    public bool Add(ScopeItem item)
    {
        if (_items.FirstOrDefault(_ => _.Name == item.Name) is FunctionScopeItem fsi
            && item is FunctionScopeItem isI)
        {
            fsi.Overloads.AddRange(isI.Overloads);
            return true;
        }

        if (Contains(item.Name))
        {
            return false;
        }

        _items.Add(item);
        return true;
    }

    public bool Contains(string name)
    {
        return _items.Any(_ => _.Name == name);
    }

    public Scope CreateChildScope()
    {
        return new Scope(this);
    }

    public IEnumerable<string> GetAllScopeNames()
    {
        var scope = this;
        while (scope != null)
        {
            foreach (var item in scope._items)
            {
                yield return item.Name;
            }

            scope = scope.Parent;
        }
    }

    public bool TryGet<T>(string name, out T? item)
        where T : ScopeItem
    {
        item = (T)_items.FirstOrDefault(i => i is T && i.Name == name);

        if (item == null && Parent != null)
        {
            if (!Parent.TryGet(name, out item))
            {
                item = null;
            }
        }

        return item != null;
    }
}