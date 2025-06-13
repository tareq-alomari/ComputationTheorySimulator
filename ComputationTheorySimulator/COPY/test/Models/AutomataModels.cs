using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComputationTheorySimulator.Models
{
    internal class AutomataModels
    {
    }
    // --- General Visual Model ---
    public class VisualState
    {
        public int Id { get; set; }
        public Point Position { get; set; }
        public bool IsAcceptState { get; set; }
        public bool IsRejectState { get; set; } // For Turing Machines
    }

    // --- FA Models ---
    public class State
    {
        private static int nextId = 0;
        public int Id { get; }
        public bool IsAcceptState { get; set; }
        public Dictionary<char, List<State>> Transitions { get; } = new Dictionary<char, List<State>>();
        public Point Position { get; set; }
        public string DfaStateIdentifier { get; set; }

        public State(bool isAccept = false) { this.Id = nextId++; this.IsAcceptState = isAccept; }
        public void AddTransition(char symbol, State toState) { if (!Transitions.ContainsKey(symbol)) { Transitions[symbol] = new List<State>(); } Transitions[symbol].Add(toState); }
        public static void ResetIdCounter() => nextId = 0;
    }

    public class NfaFragment { public State Start { get; set; } public State End { get; set; } }

    // --- PDA Models ---
    public class PDATransition { public int FromStateId { get; set; } public char InputSymbol { get; set; } public char StackPopSymbol { get; set; } public int NextStateId { get; set; } public string StackPushSymbols { get; set; } }
    public class PushdownAutomaton { public Dictionary<int, List<PDATransition>> Transitions { get; set; } = new Dictionary<int, List<PDATransition>>(); public int StartStateId { get; set; } public HashSet<int> AcceptStates { get; set; } = new HashSet<int>(); public char StartStackSymbol { get; set; } = 'Z'; }
    public class PDAConfiguration { public int CurrentStateId { get; set; } public int InputPointer { get; set; } public Stack<char> MachineStack { get; set; } public List<string> TraceHistory { get; set; } }
    public class PdaSimulationResult { public bool IsAccepted { get; } public List<string> Trace { get; } public bool IsDeterministicViolation { get; } public PdaSimulationResult(bool accepted, List<string> trace, bool violation = false) { IsAccepted = accepted; Trace = trace; IsDeterministicViolation = violation; } }

    // --- TM Models ---
    public enum TapeMove { L, R, S }
    public struct TMTransitionKey { public readonly int StateId; public readonly char ReadSymbol; public TMTransitionKey(int stateId, char readSymbol) { StateId = stateId; ReadSymbol = readSymbol; } public override bool Equals(object obj) => obj is TMTransitionKey other && this.StateId == other.StateId && this.ReadSymbol == other.ReadSymbol; public override int GetHashCode() { unchecked { return (StateId * 397) ^ ReadSymbol.GetHashCode(); } } }
    public class TMTransition { public int NextStateId { get; set; } public char WriteSymbol { get; set; } public TapeMove MoveDirection { get; set; } }
    public class TuringMachine { public Dictionary<TMTransitionKey, TMTransition> Transitions { get; set; } = new Dictionary<TMTransitionKey, TMTransition>(); public int StartStateId { get; set; } public int AcceptStateId { get; set; } public int RejectStateId { get; set; } }
}
