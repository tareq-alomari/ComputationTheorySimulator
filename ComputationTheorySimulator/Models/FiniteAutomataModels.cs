using System.Collections.Generic;
using System.Drawing;

namespace ComputationTheorySimulator.Models2
{
    /// <summary>
    /// يمثل حالة في الآلة المنتهية (FA).
    /// </summary>
    public class State
    {
        private static int nextId = 0;
        public int Id { get; }
        public bool IsAcceptState { get; set; }
        public Dictionary<char, List<State>> Transitions { get; } = new Dictionary<char, List<State>>();
        public Point Position { get; set; } // للرسم البياني
        public string DfaStateIdentifier { get; set; } // لتعريف مجموعة حالات DFA

        public State(bool isAccept = false)
        {
            this.Id = nextId++;
            this.IsAcceptState = isAccept;
        }

        public void AddTransition(char symbol, State toState)
        {
            if (!Transitions.ContainsKey(symbol))
            {
                Transitions[symbol] = new List<State>();
            }
            Transitions[symbol].Add(toState);
        }

        public static void ResetIdCounter() => nextId = 0;
    }

    /// <summary>
    /// يمثل جزءًا من NFA (بداية ونهاية) يستخدم في بناء الآلة.
    /// </summary>
    public class NfaFragment
    {
        public State Start { get; set; }
        public State End { get; set; }
    }
}