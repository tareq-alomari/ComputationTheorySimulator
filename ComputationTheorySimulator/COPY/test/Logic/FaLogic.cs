using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComputationTheorySimulator.Exceptions;
using ComputationTheorySimulator.Models;

namespace ComputationTheorySimulator.Logic
{
    public static class FaLogic
    {
        public static string AddConcatOperator(string pattern)
        {
            var o = new StringBuilder();
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

        public static string InfixToPostfix(string p)
        {
            var pre = new Dictionary<char, int> { { '|', 1 }, { '.', 2 }, { '*', 3 } };
            var post = new StringBuilder();
            var s = new Stack<char>();
            foreach (char c in p)
            {
                if (char.IsLetterOrDigit(c)) post.Append(c);
                else if (c == '(') s.Push(c);
                else if (c == ')')
                {
                    while (s.Count > 0 && s.Peek() != '(') post.Append(s.Pop());
                    if (s.Count == 0) throw new ParsingException("أقواس غير متطابقة في التعبير.");
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
                if (s.Peek() == '(') throw new ParsingException("أقواس غير متطابقة في التعبير.");
                post.Append(s.Pop());
            }
            return post.ToString();
        }

        public static NfaFragment PostfixToNfa(string postfix)
        {
            var s = new Stack<NfaFragment>();
            if (string.IsNullOrEmpty(postfix)) throw new ParsingException("التعبير النمطي لا يمكن أن يكون فارغًا.");
            foreach (char c in postfix)
            {
                if (char.IsLetterOrDigit(c))
                {
                    var st = new State(); var en = new State(true); st.AddTransition(c, en); s.Push(new NfaFragment { Start = st, End = en });
                }
                else if (c == '.')
                {
                    if (s.Count < 2) throw new ParsingException("صيغة خاطئة (خطأ في عامل التسلسل).");
                    var f2 = s.Pop(); var f1 = s.Pop(); f1.End.IsAcceptState = false; f1.End.AddTransition('\0', f2.Start); s.Push(new NfaFragment { Start = f1.Start, End = f2.End });
                }
                else if (c == '|')
                {
                    if (s.Count < 2) throw new ParsingException("صيغة خاطئة (خطأ في عامل الاختيار).");
                    var f2 = s.Pop(); var f1 = s.Pop(); var st = new State(); st.AddTransition('\0', f1.Start); st.AddTransition('\0', f2.Start); var en = new State(true); f1.End.IsAcceptState = false; f2.End.IsAcceptState = false; f1.End.AddTransition('\0', en); f2.End.AddTransition('\0', en); s.Push(new NfaFragment { Start = st, End = en });
                }
                else if (c == '*')
                {
                    if (s.Count < 1) throw new ParsingException("صيغة خاطئة (خطأ في عامل التكرار).");
                    var f = s.Pop(); var st = new State(); var en = new State(true); st.AddTransition('\0', en); st.AddTransition('\0', f.Start); f.End.IsAcceptState = false; f.End.AddTransition('\0', en); f.End.AddTransition('\0', f.Start); s.Push(new NfaFragment { Start = st, End = en });
                }
            }
            if (s.Count != 1) throw new ParsingException("التعبير النمطي غير صالح أو معقد بشكل غير صحيح.");
            return s.Pop();
        }

        public static Tuple<State, List<State>> NfaToDfa(NfaFragment nfa, IEnumerable<char> alphabet)
        {
            var dfaStates = new Dictionary<string, State>();
            var unmarked = new Queue<HashSet<State>>();
            var allDfaNewStates = new List<State>();

            var firstSet = EpsilonClosure(new HashSet<State> { nfa.Start });
            if (!firstSet.Any()) throw new SimulationException("لا يمكن الوصول لأي حالة من حالة البداية.");

            string firstKey = string.Join(",", firstSet.Select(st => st.Id).OrderBy(id => id));
            var dfaStart = new State(firstSet.Any(s => s.IsAcceptState)) { DfaStateIdentifier = firstKey };

            dfaStates[firstKey] = dfaStart;
            unmarked.Enqueue(firstSet);
            allDfaNewStates.Add(dfaStart);

            while (unmarked.Count > 0)
            {
                var currentSet = unmarked.Dequeue();
                string currentKey = string.Join(",", currentSet.Select(st => st.Id).OrderBy(id => id));
                var currentDfaState = dfaStates[currentKey];

                foreach (char sym in alphabet)
                {
                    var moveSet = Move(currentSet, sym);
                    if (!moveSet.Any()) continue;

                    var closureSet = EpsilonClosure(moveSet);
                    string newKey = string.Join(",", closureSet.Select(st => st.Id).OrderBy(id => id));

                    if (!dfaStates.ContainsKey(newKey))
                    {
                        var newDfaState = new State(closureSet.Any(s => s.IsAcceptState)) { DfaStateIdentifier = newKey };
                        dfaStates[newKey] = newDfaState;
                        unmarked.Enqueue(closureSet);
                        allDfaNewStates.Add(newDfaState);
                    }
                    currentDfaState.AddTransition(sym, dfaStates[newKey]);
                }
            }
            return Tuple.Create(dfaStart, allDfaNewStates);
        }

        public static HashSet<State> EpsilonClosure(HashSet<State> states)
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

        public static HashSet<State> Move(HashSet<State> states, char symbol)
        {
            var res = new HashSet<State>();
            foreach (var s in states)
                if (s.Transitions.ContainsKey(symbol))
                    foreach (var t in s.Transitions[symbol])
                        res.Add(t);
            return res;
        }

        public static bool TestDfaString(State startState, string input)
        {
            State current = startState;
            foreach (char c in input)
            {
                if (current.Transitions.ContainsKey(c) && current.Transitions[c].Any())
                {
                    current = current.Transitions[c][0];
                }
                else
                {
                    return false;
                }
            }
            return current.IsAcceptState;
        }

        public static bool TestNfaString(State startState, string input)
        {
            var currentStates = EpsilonClosure(new HashSet<State> { startState });
            foreach (char c in input)
            {
                currentStates = EpsilonClosure(Move(currentStates, c));
                if (!currentStates.Any())
                {
                    return false;
                }
            }
            return currentStates.Any(s => s.IsAcceptState);
        }

        public static List<State> GetAllStatesFromNfa(State startState)
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
    }
}