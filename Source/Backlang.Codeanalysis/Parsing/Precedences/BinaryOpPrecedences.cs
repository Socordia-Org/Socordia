﻿namespace Backlang.Codeanalysis.Parsing.Precedences;
public enum BinaryOpPrecedences
{

    Hat = 2,

    Ampersand = 3,
    ShiftOperator = Ampersand,

    EqualsEquals = 4,
    DashedOps = EqualsEquals, // add, sub
    Range = DashedOps,
    And = Range, // &&

    DottedOps = 5, // mul, div
    Percent = DottedOps,
    Or = Percent,
    Comparisons = Or, // < <= >= >
    SwapOperator = 2,
    
    FunctionCalls = 7, // . ::

    PipeOperator = 8, // |>

    OperationShortcuts = 9, // += -= *= /=
    Equals = OperationShortcuts,

}
