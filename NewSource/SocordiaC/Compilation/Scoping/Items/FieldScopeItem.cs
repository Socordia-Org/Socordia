﻿using DistIL.AsmIO;
using System.Reflection;

namespace SocordiaC.Compilation.Scoping.Items;

public class FieldScopeItem : ScopeItem
{
    public FieldDesc Field { get; init; }
    public bool IsMutable => !Field.Attribs.HasFlag(FieldAttributes.InitOnly);
    public bool IsStatic => Field.IsStatic;

    public override TypeDesc Type => Field.Type;

    public void Deconstruct(out string name, out FieldDesc field, out bool isMutable, out bool isStatic)
    {
        name = Name;
        field = Field;
        isMutable = IsMutable;
        isStatic = IsStatic;
    }
}