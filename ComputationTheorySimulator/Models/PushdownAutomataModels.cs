using System;
using System.Collections.Generic;

namespace ComputationTheorySimulator.Models2
{
    /// <summary>
    /// يمثل انتقال في آلة الدفع للأسفل (PDA).
    /// </summary>
    public class PDATransition
    {
        public int FromStateId { get; set; }
        public char InputSymbol { get; set; } // '\0' لـ ε
        public char StackPopSymbol { get; set; } // '\0' لـ ε
        public int NextStateId { get; set; }
        public string StackPushSymbols { get; set; } // string.Empty لـ ε
    }

    /// <summary>
    /// يمثل آلة الدفع للأسفل (PDA) مع حالاتها وانتقالاتها.
    /// </summary>
    public class PushdownAutomaton
    {
        public Dictionary<int, List<PDATransition>> Transitions { get; } = new Dictionary<int, List<PDATransition>>();
        public int StartStateId { get; set; }
        public HashSet<int> AcceptStates { get; } = new HashSet<int>();
        public char StartStackSymbol { get; set; } = 'Z';
    }

    /// <summary>
    /// يمثل تكوين (حالة) المحاكاة لآلة PDA في لحظة معينة.
    /// </summary>
    public class PDAConfiguration
    {
        public int CurrentStateId { get; set; }
        public int InputPointer { get; set; }
        public Stack<char> MachineStack { get; set; }
        public List<string> TraceHistory { get; set; }
    }

    /// <summary>
    /// يمثل نتيجة محاكاة PDA.
    /// </summary>
    public class PdaSimulationResult
    {
        public bool IsAccepted { get; }
        public List<string> Trace { get; }
        public bool IsDeterministicViolation { get; }

        public PdaSimulationResult(bool accepted, List<string> trace, bool violation)
        {
            IsAccepted = accepted;
            Trace = trace;
            IsDeterministicViolation = violation;
        }
    }
}