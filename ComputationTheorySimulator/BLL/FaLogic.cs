using ComputationTheorySimulator.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ComputationTheorySimulator.BLL
{
    public class FaLogic
    {
        public string AddConcatOperator(string pattern)
        {
            var o = new System.Text.StringBuilder();
            for (int i = 0; i < pattern.Length; i++)
            {
                o.Append(pattern[i]);
                if (i + 1 < pattern.Length)
                {
                    char c = pattern[i], n = pattern[i + 1];
                    if ((char.IsLetterOrDigit(c) || c == ')' || c == '*') && (char.IsLetterOrDigit(n) || n == '('))
                        o.Append('.');
                }
            }
            return o.ToString();
        }

        public string InfixToPostfix(string p)
        {
            var pre = new Dictionary<char, int> { { '|', 1 }, { '.', 2 }, { '*', 3 } };
            var post = new System.Text.StringBuilder();
            var s = new Stack<char>();
            foreach (char c in p)
            {
                if (char.IsLetterOrDigit(c))
                    post.Append(c);
                else if (c == '(')
                    s.Push(c);
                else if (c == ')')
                {
                    while (s.Count > 0 && s.Peek() != '(')
                        post.Append(s.Pop());
                    if (s.Count == 0) throw new ArgumentException("أقواس غير متطابقة");
                    s.Pop();
                }
                else
                {
                    while (s.Count > 0 && s.Peek() != '(' && pre.ContainsKey(s.Peek()) && pre[s.Peek()] >= pre[c])
                        post.Append(s.Pop());
                    s.Push(c);
                }
            }
            while (s.Count > 0)
            {
                if (s.Peek() == '(') throw new ArgumentException("أقواس غير متطابقة");
                post.Append(s.Pop());
            }
            return post.ToString();
        }

        public NfaFragment PostfixToNfa(string postfix)
        {
            var s = new Stack<NfaFragment>();
            foreach (char c in postfix)
            {
                if (char.IsLetterOrDigit(c))
                {
                    var st = new State(); var en = new State(true); st.AddTransition(c, en); s.Push(new NfaFragment { Start = st, End = en });
                }
                else if (c == '.')
                {
                    var f2 = s.Pop(); var f1 = s.Pop(); f1.End.IsAcceptState = false; f1.End.AddTransition('\0', f2.Start); s.Push(new NfaFragment { Start = f1.Start, End = f2.End });
                }
                else if (c == '|')
                {
                    var f2 = s.Pop(); var f1 = s.Pop(); var st = new State(); st.AddTransition('\0', f1.Start); st.AddTransition('\0', f2.Start); var en = new State(true); f1.End.IsAcceptState = false; f2.End.IsAcceptState = false; f1.End.AddTransition('\0', en); f2.End.AddTransition('\0', en); s.Push(new NfaFragment { Start = st, End = en });
                }
                else if (c == '*')
                {
                    var f = s.Pop(); var st = new State(); var en = new State(true); st.AddTransition('\0', en); st.AddTransition('\0', f.Start); f.End.IsAcceptState = false; f.End.AddTransition('\0', en); f.End.AddTransition('\0', f.Start); s.Push(new NfaFragment { Start = st, End = en });
                }
            }
            return s.Pop();
        }

        public Tuple<State, List<State>> NfaToDfa(NfaFragment nfa, IEnumerable<char> alphabet)
        {
            var dfaStates = new Dictionary<string, State>();
            var unmarked = new Queue<HashSet<State>>();
            var all = new List<State>();
            var first = EpsilonClosure(new HashSet<State> { nfa.Start });
            unmarked.Enqueue(first);
            var dfaStart = new State(first.Any(s => s.IsAcceptState));
            string key1 = SetToKey(first);
            dfaStart.DfaStateIdentifier = key1;
            dfaStates[key1] = dfaStart;
            all.Add(dfaStart);
            while (unmarked.Count > 0)
            {
                var currentSet = unmarked.Dequeue();
                var currentDfa = dfaStates[SetToKey(currentSet)];
                foreach (char sym in alphabet)
                {
                    var moveSet = Move(currentSet, sym);
                    if (!moveSet.Any()) continue;
                    var closure = EpsilonClosure(moveSet);
                    string key2 = SetToKey(closure);
                    if (!dfaStates.ContainsKey(key2))
                    {
                        var newDfa = new State(closure.Any(s => s.IsAcceptState));
                        newDfa.DfaStateIdentifier = key2;
                        dfaStates[key2] = newDfa;
                        unmarked.Enqueue(closure);
                        all.Add(newDfa);
                    }
                    currentDfa.AddTransition(sym, dfaStates[key2]);
                }
            }
            return Tuple.Create(dfaStart, all);
        }

        public HashSet<State> EpsilonClosure(HashSet<State> states)
        {
            var closure = new HashSet<State>(states);
            var stack = new Stack<State>(states);
            while (stack.Count > 0)
            {
                var s = stack.Pop();
                if (s.Transitions.ContainsKey('\0'))
                    foreach (var t in s.Transitions['\0'])
                        if (closure.Add(t)) stack.Push(t);
            }
            return closure;
        }

        public HashSet<State> Move(HashSet<State> states, char symbol)
        {
            var res = new HashSet<State>();
            foreach (var s in states)
                if (s.Transitions.ContainsKey(symbol))
                    foreach (var t in s.Transitions[symbol])
                        res.Add(t);
            return res;
        }

        public bool TestDfaString(State startState, string input)
        {
            State current = startState;
            foreach (char c in input)
            {
                if (current.Transitions.ContainsKey(c))
                {
                    current = current.Transitions[c][0];
                }
                else
                {
                    return false; // Rejection
                }
            }
            return current.IsAcceptState;
        }

        public bool TestNfaString(State startState, string input)
        {
            var currentStates = EpsilonClosure(new HashSet<State> { startState });
            foreach (char c in input)
            {
                currentStates = EpsilonClosure(Move(currentStates, c));
                if (!currentStates.Any())
                {
                    return false; // Rejection
                }
            }
            return currentStates.Any(s => s.IsAcceptState);
        }

        public List<State> GetAllStatesFromNfa(State startState)
        {
            var allStates = new List<State>();
            var visited = new HashSet<State>();
            var stack = new Stack<State>();

            stack.Push(startState);
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (visited.Contains(current)) continue;

                visited.Add(current);
                allStates.Add(current);

                foreach (var transition in current.Transitions)
                {
                    foreach (var target in transition.Value)
                    {
                        if (!visited.Contains(target))
                        {
                            stack.Push(target);
                        }
                    }
                }
            }
            return allStates;
        }

        private string SetToKey(HashSet<State> set) => "{" + string.Join(",", set.Select(s => s.Id).OrderBy(id => id)) + "}";
    }
}