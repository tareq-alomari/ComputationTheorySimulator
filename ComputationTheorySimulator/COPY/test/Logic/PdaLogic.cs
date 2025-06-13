using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ComputationTheorySimulator.Exceptions;
using ComputationTheorySimulator.Models;

namespace ComputationTheorySimulator.Logic
{
    public static class PdaLogic
    {
        public static PushdownAutomaton ParsePda(string definition, bool isDeterministic)
        {
            var pda = new PushdownAutomaton();
            var lines = definition.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var rx = new Regex(@"^q(\d+)\s*,\s*(.)\s*,\s*(.)\s*;\s*q(\d+)\s*,\s*(.+)$");
            var allStateIds = new HashSet<int>();

            var acceptLine = lines.FirstOrDefault(l => l.Trim().StartsWith("@accept"));
            if (acceptLine != null)
            {
                var acceptIds = Regex.Matches(acceptLine, @"q(\d+)").Cast<Match>().Select(m => int.Parse(m.Groups[1].Value));
                pda.AcceptStates = new HashSet<int>(acceptIds);
            }

            foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l) && !l.Trim().StartsWith("@accept")))
            {
                var m = rx.Match(line.Trim());
                if (!m.Success) throw new ParsingException($"صيغة خاطئة في السطر: '{line}'");

                int from = int.Parse(m.Groups[1].Value);
                char input = m.Groups[2].Value.Equals("e", StringComparison.OrdinalIgnoreCase) ? '\0' : m.Groups[2].Value[0];
                char pop = m.Groups[3].Value.Equals("e", StringComparison.OrdinalIgnoreCase) ? '\0' : m.Groups[3].Value[0];
                int to = int.Parse(m.Groups[4].Value);
                string push = m.Groups[5].Value.Equals("e", StringComparison.OrdinalIgnoreCase) ? "" : m.Groups[5].Value;

                if (!pda.Transitions.ContainsKey(from))
                {
                    pda.Transitions[from] = new List<PDATransition>();
                }

                if (isDeterministic)
                {
                    if (pda.Transitions[from].Any(t => t.InputSymbol == input && t.StackPopSymbol == pop))
                        throw new ParsingException($"انتهاك التحديدية: الانتقال (q{from}, {(input == '\0' ? 'ε' : input)}, {(pop == '\0' ? 'ε' : pop)}) معرف أكثر من مرة.");

                    if (input != '\0' && pda.Transitions[from].Any(t => t.InputSymbol == '\0' && t.StackPopSymbol == pop))
                        throw new ParsingException($"انتهاك التحديدية: وجود انتقال بـ ε-input يتعارض مع انتقال آخر للحالة q{from} ورمز المكدس {(pop == '\0' ? 'ε' : pop)}.");
                }

                pda.Transitions[from].Add(new PDATransition { FromStateId = from, InputSymbol = input, StackPopSymbol = pop, NextStateId = to, StackPushSymbols = push });
                allStateIds.Add(from);
                allStateIds.Add(to);
            }

            if (allStateIds.Any())
            {
                pda.StartStateId = allStateIds.Min();
                if (!pda.AcceptStates.Any()) { pda.AcceptStates.Add(allStateIds.Max()); }
            }
            else if (pda.AcceptStates.Any())
            {
                pda.StartStateId = pda.AcceptStates.Min();
            }
            else
            {
                throw new ParsingException("لم يتم تعريف أي حالات في الآلة.");
            }

            return pda;
        }

        public static PdaSimulationResult SimulatePda(PushdownAutomaton pda, string input, bool isDeterministicMode)
        {
            var q = new Queue<PDAConfiguration>();
            var initialStack = new Stack<char>();
            initialStack.Push(pda.StartStackSymbol);

            var visited = new HashSet<string>();

            q.Enqueue(new PDAConfiguration
            {
                CurrentStateId = pda.StartStateId,
                InputPointer = 0,
                MachineStack = initialStack,
                TraceHistory = new List<string>()
            });

            int steps = 0;
            const int maxSteps = 5000;

            while (q.Count > 0 && steps++ < maxSteps)
            {
                var config = q.Dequeue();

                string remainingInput = config.InputPointer < input.Length ? input.Substring(config.InputPointer) : "ε";
                string stackContents = config.MachineStack.Count == 0 ? "ε" : string.Join("", config.MachineStack.Reverse());
                string traceLine = $"(q{config.CurrentStateId}, {remainingInput}, {stackContents})";

                string configKey = $"{config.CurrentStateId}|{config.InputPointer}|{stackContents}";
                if (visited.Contains(configKey)) continue;
                visited.Add(configKey);

                config.TraceHistory.Add(traceLine);

                if (config.InputPointer == input.Length && pda.AcceptStates.Contains(config.CurrentStateId))
                {
                    config.TraceHistory.Add("=> حالة قبول!");
                    return new PdaSimulationResult(true, config.TraceHistory);
                }

                var applicableTransitions = new List<PDATransition>();
                if (pda.Transitions.ContainsKey(config.CurrentStateId))
                {
                    char currentInputSymbol = (config.InputPointer < input.Length) ? input[config.InputPointer] : '\0';
                    char stackTop = (config.MachineStack.Count > 0) ? config.MachineStack.Peek() : '\0';

                    // Find transitions that match input and stack
                    applicableTransitions.AddRange(pda.Transitions[config.CurrentStateId]
                        .Where(t => t.InputSymbol == currentInputSymbol && (t.StackPopSymbol == stackTop || t.StackPopSymbol == '\0')));

                    // Find epsilon-input transitions
                    if (currentInputSymbol != '\0') // Avoid adding duplicates
                    {
                        applicableTransitions.AddRange(pda.Transitions[config.CurrentStateId]
                            .Where(t => t.InputSymbol == '\0' && (t.StackPopSymbol == stackTop || t.StackPopSymbol == '\0')));
                    }
                }

                var distinctTransitions = applicableTransitions.Distinct().ToList();

                if (isDeterministicMode && distinctTransitions.Count > 1)
                {
                    config.TraceHistory.Add("! انتهاك التحديدية: أكثر من انتقال ممكن.");
                    return new PdaSimulationResult(false, config.TraceHistory, true);
                }

                foreach (var trans in distinctTransitions)
                {
                    var newStack = new Stack<char>(config.MachineStack.Reverse());

                    if (trans.StackPopSymbol != '\0')
                    {
                        if (newStack.Count == 0 || newStack.Peek() != trans.StackPopSymbol) continue;
                        newStack.Pop();
                    }

                    if (!string.IsNullOrEmpty(trans.StackPushSymbols))
                    {
                        foreach (char c in trans.StackPushSymbols.Reverse()) newStack.Push(c);
                    }

                    var newHist = new List<string>(config.TraceHistory);
                    newHist.Add($"  -> δ(q{config.CurrentStateId}, {(trans.InputSymbol == '\0' ? 'ε' : trans.InputSymbol)}, {(trans.StackPopSymbol == '\0' ? 'ε' : trans.StackPopSymbol)}) = (q{trans.NextStateId}, {(string.IsNullOrEmpty(trans.StackPushSymbols) ? "ε" : trans.StackPushSymbols)})");

                    q.Enqueue(new PDAConfiguration
                    {
                        CurrentStateId = trans.NextStateId,
                        InputPointer = config.InputPointer + (trans.InputSymbol == '\0' ? 0 : 1),
                        MachineStack = newStack,
                        TraceHistory = newHist
                    });
                }
            }

            var finalTrace = new List<string> { steps >= maxSteps ? "تجاوز حد الخطوات!" : "لم يتم العثور على مسار مقبول." };
            if (q.Any()) finalTrace = q.First().TraceHistory; // Get trace from one of the remaining paths

            return new PdaSimulationResult(false, finalTrace);
        }
    }
}