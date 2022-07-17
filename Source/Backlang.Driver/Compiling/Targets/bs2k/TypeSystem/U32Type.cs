﻿using Furesoft.Core.CodeDom.Compiler.Core;
using Furesoft.Core.CodeDom.Compiler.Core.Names;
using Furesoft.Core.CodeDom.Compiler.Core.TypeSystem;

namespace Backlang.Driver.Compiling.Targets.bs2k.TypeSystem;

public class U32Type : DescribedType
{
    public U32Type(IAssembly assembly) : base(new SimpleName("UInt32").Qualify("System"), assembly)
    {
    }
}