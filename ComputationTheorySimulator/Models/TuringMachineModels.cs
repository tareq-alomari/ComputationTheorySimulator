using System;
using System.Collections.Generic;

namespace ComputationTheorySimulator.Models2
{
    public enum TapeMove { L, R }

    /// <summary>
    /// مفتاح فريد لجدول انتقالات آلة تورنغ.
    /// </summary>
    public struct TMTransitionKey
    {
        public readonly int StateId;
        public readonly char ReadSymbol;

        public TMTransitionKey(int stateId, char readSymbol)
        {
            StateId = stateId;
            ReadSymbol = readSymbol;
        }

        public override bool Equals(object obj) => obj is TMTransitionKey other && this.StateId == other.StateId && this.ReadSymbol == other.ReadSymbol;
        public override int GetHashCode() { unchecked { return (StateId * 397) ^ ReadSymbol.GetHashCode(); } }
    }

    /// <summary>
    /// يمثل انتقال في آلة تورنغ.
    /// </summary>
    public class TMTransition
    {
        public int NextStateId { get; set; }
        public char WriteSymbol { get; set; }
        public TapeMove MoveDirection { get; set; }
    }

    /// <summary>
    /// يمثل آلة تورنغ.
    /// </summary>
    public class TuringMachine
    {
        public Dictionary<TMTransitionKey, TMTransition> Transitions { get; set; } = new Dictionary<TMTransitionKey, TMTransition>();
        public int StartStateId { get; set; }
        public int AcceptStateId { get; set; }
        public int RejectStateId { get; set; }
    }
}