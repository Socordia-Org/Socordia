﻿using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestProject1.AST.Declarations;

[TestClass]
public class ImplementationTests : ParserTestBase
{
    [TestMethod]
    public void Range_Impl_Should_Pass()
    {
        var src = "implement u8..u32 { func something() {  } }";
        var node = ParseAndGetNodes(src);
    }

    [TestMethod]
    public void Simple_Impl_Should_Pass()
    {
        var src = "implement u8 { func something() {  } }";
        var node = ParseAndGetNodes(src);
    }
}