using ComputationTheorySimulator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ComputationTheorySimulator.BLL
{
    public class TmLogic
    {
        public TuringMachine ParseTm(string definition)
        {
            var tm = new TuringMachine();
            var lines = definition.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var rx = new Regex(@"^q(\d+)\s*,\s*(.)\s*;\s*q(\d+)\s*,\s*(.)\s*,\s*([LR])$", RegexOptions.IgnoreCase);
            var allStates = new HashSet<int>();

            // تعريف الحالات الخاصة
            var startLine = lines.FirstOrDefault(l => l.Trim().StartsWith("@start:"));
            var acceptLine = lines.FirstOrDefault(l => l.Trim().StartsWith("@accept:"));
            var rejectLine = lines.FirstOrDefault(l => l.Trim().StartsWith("@reject:"));

            if (startLine != null) tm.StartStateId = int.Parse(Regex.Match(startLine, @"\d+").Value);
            if (acceptLine != null) tm.AcceptStateId = int.Parse(Regex.Match(acceptLine, @"\d+").Value);
            if (rejectLine != null) tm.RejectStateId = int.Parse(Regex.Match(rejectLine, @"\d+").Value);

            foreach (var line in lines.Where(l => !l.Trim().StartsWith("@")))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;
                var m = rx.Match(line.Trim());
                if (!m.Success) throw new ArgumentException($"صيغة خاطئة: '{line}'");

                int from = int.Parse(m.Groups[1].Value);
                char r = m.Groups[2].Value[0];
                int to = int.Parse(m.Groups[3].Value);
                char w = m.Groups[4].Value[0];
                TapeMove mov = m.Groups[5].Value.ToUpper() == "R" ? TapeMove.R : TapeMove.L;

                tm.Transitions[new TMTransitionKey(from, r)] = new TMTransition { NextStateId = to, WriteSymbol = w, MoveDirection = mov };
                allStates.Add(from);
                allStates.Add(to);
            }

            if (!allStates.Any() && tm.StartStateId == 0) throw new ArgumentException("لم يتم تعريف أي حالات أو انتقالات.");

            // إذا لم يتم تحديد الحالات الخاصة، افترضها
            if (startLine == null && allStates.Any()) tm.StartStateId = allStates.Min();
            if (acceptLine == null && allStates.Any()) tm.AcceptStateId = allStates.Max();
            if (rejectLine == null && allStates.Count > 1)
            {
                // افترض أن ثاني أكبر حالة هي حالة الرفض
                tm.RejectStateId = allStates.OrderByDescending(s => s).Skip(1).FirstOrDefault();
            }
            else if (rejectLine == null)
            {
                tm.RejectStateId = -1; // لا توجد حالة رفض محددة
            }

            return tm;
        }
    }
}