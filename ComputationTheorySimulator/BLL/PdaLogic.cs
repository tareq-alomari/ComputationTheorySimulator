using ComputationTheorySimulator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ComputationTheorySimulator.BLL
{
    public class PdaLogic
    {
        public PushdownAutomaton ParsePdaDefinition(string definition, bool isDeterministic)
        {
            var pda = new PushdownAutomaton();
            var lines = definition.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var transitionRegex = new Regex(@"q(\d+)\s*,\s*(.)\s*,\s*(.)\s*;\s*q(\d+)\s*,\s*(.+)");

            var acceptLine = lines.FirstOrDefault(l => l.Trim().StartsWith("@accept:"));
            if (acceptLine != null)
            {
                var acceptIds = acceptLine.Split(':')[1].Split(',')
                    .Select(id => int.Parse(id.Trim().Substring(1)));
                pda.AcceptStates.UnionWith(acceptIds);
            }

            foreach (var line in lines.Where(l => !l.Trim().StartsWith("@")))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var match = transitionRegex.Match(line.Trim());
                if (!match.Success) throw new ArgumentException($"صيغة انتقال غير صالحة: {line}");

                var transition = new PDATransition
                {
                    FromStateId = int.Parse(match.Groups[1].Value),
                    InputSymbol = match.Groups[2].Value.Equals("e", StringComparison.OrdinalIgnoreCase) ? '\0' : match.Groups[2].Value[0],
                    StackPopSymbol = match.Groups[3].Value.Equals("e", StringComparison.OrdinalIgnoreCase) ? '\0' : match.Groups[3].Value[0],
                    NextStateId = int.Parse(match.Groups[4].Value),
                    StackPushSymbols = match.Groups[5].Value.Equals("e", StringComparison.OrdinalIgnoreCase) ? string.Empty : match.Groups[5].Value
                };

                if (isDeterministic)
                {
                    var key = new { transition.FromStateId, transition.InputSymbol, transition.StackPopSymbol };
                    if (pda.Transitions.ContainsKey(key.FromStateId) &&
                        pda.Transitions[key.FromStateId].Any(t => t.InputSymbol == key.InputSymbol && t.StackPopSymbol == key.StackPopSymbol))
                    {
                        throw new ArgumentException($"انتقال غير محدد: يوجد بالفعل انتقال من q{key.FromStateId} بالرمز '{key.InputSymbol}' ورمز المكدس '{key.StackPopSymbol}'");
                    }
                }

                if (!pda.Transitions.ContainsKey(transition.FromStateId))
                {
                    pda.Transitions[transition.FromStateId] = new List<PDATransition>();
                }
                pda.Transitions[transition.FromStateId].Add(transition);
            }

            if (pda.Transitions.Any())
            {
                pda.StartStateId = pda.Transitions.Keys.Min();
            }

            return pda;
        }

        public PdaSimulationResult Simulate(PushdownAutomaton pda, string input, bool isDeterministicMode)
        {
            var queue = new Queue<PDAConfiguration>();
            var initialStack = new Stack<char>();
            initialStack.Push(pda.StartStackSymbol);

            queue.Enqueue(new PDAConfiguration
            {
                CurrentStateId = pda.StartStateId,
                InputPointer = 0,
                MachineStack = initialStack,
                TraceHistory = new List<string>() { "بدء المحاكاة..." }
            });

            int maxSteps = 2000;
            int steps = 0;
            var visitedConfigs = new HashSet<string>();

            while (queue.Count > 0 && steps++ < maxSteps)
            {
                var config = queue.Dequeue();

                string remainingInput = config.InputPointer < input.Length ? input.Substring(config.InputPointer) : "ε";
                string stackContents = config.MachineStack.Any() ? new string(config.MachineStack.Reverse().ToArray()) : "ε";

                string configKey = $"{config.CurrentStateId}|{config.InputPointer}|{stackContents}";
                if (visitedConfigs.Contains(configKey)) continue;
                visitedConfigs.Add(configKey);

                config.TraceHistory.Add($"(q{config.CurrentStateId}, {remainingInput}, {stackContents})");

                if (config.InputPointer == input.Length && pda.AcceptStates.Contains(config.CurrentStateId))
                {
                    config.TraceHistory.Add("=> حالة قبول!");
                    return new PdaSimulationResult(true, config.TraceHistory, false);
                }

                var possibleTransitions = GetPossibleTransitions(pda, config, input);

                if (isDeterministicMode && possibleTransitions.Count > 1)
                {
                    config.TraceHistory.Add("! انتهاك الحتمية: تم العثور على أكثر من انتقال ممكن.");
                    return new PdaSimulationResult(false, config.TraceHistory, true);
                }

                foreach (var trans in possibleTransitions)
                {
                    var newConfig = CreateNewConfiguration(config, trans);
                    queue.Enqueue(newConfig);
                }
            }

            return new PdaSimulationResult(false, new List<string> { "لم يتم العثور على مسار مقبول أو تم تجاوز حد الخطوات." }, false);
        }

        private List<PDATransition> GetPossibleTransitions(PushdownAutomaton pda, PDAConfiguration config, string input)
        {
            if (!pda.Transitions.ContainsKey(config.CurrentStateId)) return new List<PDATransition>();
            char currentInput = config.InputPointer < input.Length ? input[config.InputPointer] : '\0';
            char stackTop = config.MachineStack.Any() ? config.MachineStack.Peek() : '\0';

            return pda.Transitions[config.CurrentStateId]
                .Where(t => (t.InputSymbol == currentInput || t.InputSymbol == '\0') &&
                             (t.StackPopSymbol == stackTop || t.StackPopSymbol == '\0'))
                .ToList();
        }

        private PDAConfiguration CreateNewConfiguration(PDAConfiguration currentConfig, PDATransition transition)
        {
            var newStack = new Stack<char>(currentConfig.MachineStack.Reverse());
            if (transition.StackPopSymbol != '\0')
            {
                if (newStack.Any() && newStack.Peek() == transition.StackPopSymbol) newStack.Pop();
                else if (newStack.Count == 0 && transition.StackPopSymbol != '\0') return null; // لا يمكن السحب من مكدس فارغ
            }

            if (!string.IsNullOrEmpty(transition.StackPushSymbols))
            {
                foreach (char c in transition.StackPushSymbols.Reverse()) newStack.Push(c);
            }

            var traceMessage = $" -> δ(q{currentConfig.CurrentStateId}, {(transition.InputSymbol == '\0' ? 'ε' : transition.InputSymbol)}, {(transition.StackPopSymbol == '\0' ? 'ε' : transition.StackPopSymbol)}) = (q{transition.NextStateId}, {(string.IsNullOrEmpty(transition.StackPushSymbols) ? "ε" : transition.StackPushSymbols)})";
            var newHistory = new List<string>(currentConfig.TraceHistory);
            newHistory.Add(traceMessage);

            return new PDAConfiguration
            {
                CurrentStateId = transition.NextStateId,
                InputPointer = currentConfig.InputPointer + (transition.InputSymbol == '\0' ? 0 : 1),
                MachineStack = newStack,
                TraceHistory = newHistory
            };
        }
    }
}