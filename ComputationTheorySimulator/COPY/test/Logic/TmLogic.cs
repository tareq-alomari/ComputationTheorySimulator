using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ComputationTheorySimulator.Exceptions; // تأكد من أن هذا الـ namespace صحيح لـ ParsingException
using ComputationTheorySimulator.Models;

namespace ComputationTheorySimulator.Logic
{
    public static class TmLogic
    {
        public static TuringMachine ParseTm(string definition)
        {
            var tm = new TuringMachine();
            var lines = definition.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries); // تقسيم بالسطور، يشمل CR و LF

            // Regex for transitions: q<digit>,<symbol>;q<digit>,<symbol>,<L|R|S>
            // [a-zA-Z0-9_] يسمح بالحروف الأبجدية الرقمية ورمز الشرطة السفلية (عادةً للفراغ)
            var transitionRegex = new Regex(@"^q(\d+)\s*,\s*([a-zA-Z0-9_])\s*;\s*q(\d+)\s*,\s*([a-zA-Z0-9_])\s*,\s*([LRS])$", RegexOptions.IgnoreCase);

            var allStates = new HashSet<int>();
            // متغيرات لتخزين الحالات المعرفة صراحة
            int? startStateId = null;
            var acceptStateIds = new HashSet<int>(); // نستخدم HashSet لدعم أكثر من حالة قبول (إذا كان الـ TM Model يدعم ذلك)
            int? rejectStateId = null;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("//")) continue;

                // تحليل سطور تعريف الحالات الخاصة (START_STATE, ACCEPT_STATES, REJECT_STATE)
                if (trimmedLine.StartsWith("START_STATE:", StringComparison.OrdinalIgnoreCase))
                {
                    var match = Regex.Match(trimmedLine, @"START_STATE:\s*q(\d+)");
                    if (match.Success) startStateId = int.Parse(match.Groups[1].Value);
                    continue;
                }
                if (trimmedLine.StartsWith("ACCEPT_STATES:", StringComparison.OrdinalIgnoreCase))
                {
                    var match = Regex.Match(trimmedLine, @"ACCEPT_STATES:\s*(q\d+(?:\s*,\s*q\d+)*)");
                    if (match.Success)
                    {
                        foreach (Match stateMatch in Regex.Matches(match.Groups[1].Value, @"q(\d+)"))
                        {
                            acceptStateIds.Add(int.Parse(stateMatch.Groups[1].Value));
                        }
                    }
                    continue;
                }
                if (trimmedLine.StartsWith("REJECT_STATE:", StringComparison.OrdinalIgnoreCase))
                {
                    var match = Regex.Match(trimmedLine, @"REJECT_STATE:\s*q(\d+)");
                    if (match.Success) rejectStateId = int.Parse(match.Groups[1].Value);
                    continue;
                }

                // تحليل سطور الانتقالات العادية
                var m = transitionRegex.Match(trimmedLine);
                if (!m.Success)
                {
                    throw new ParsingException($"صيغة خاطئة أو تعريف غير معروف في السطر: '{line}'");
                }

                int from = int.Parse(m.Groups[1].Value);
                char read = m.Groups[2].Value[0];
                int to = int.Parse(m.Groups[3].Value);
                char write = m.Groups[4].Value[0];
                TapeMove moveDirection; // تم تغيير اسم المتغير لتجنب التعارض

                // *** التعديل الأول: معالجة حركة S بشكل صحيح ***
                switch (m.Groups[5].Value.ToUpper())
                {
                    case "R": moveDirection = TapeMove.R; break;
                    case "L": moveDirection = TapeMove.L; break;
                    case "S": moveDirection = TapeMove.S; break;
                    default: throw new ParsingException($"اتجاه حركة غير صالح: {m.Groups[5].Value} في السطر: '{line}'");
                }

                var key = new TMTransitionKey(from, read);
                if (tm.Transitions.ContainsKey(key))
                    throw new ParsingException($"الآلة محددة، والانتقال من (q{from}, {read}) معرف أكثر من مرة.");

                tm.Transitions[key] = new TMTransition { NextStateId = to, WriteSymbol = write, MoveDirection = moveDirection };
                allStates.Add(from);
                allStates.Add(to);
            }

            if (!allStates.Any()) throw new ParsingException("لم يتم تعريف أي حالات في الآلة.");

            // *** التعديل الثاني: تعيين حالات البداية، القبول، والرفض بشكل ديناميكي ***
            tm.StartStateId = startStateId ?? allStates.Min(); // إذا لم يتم تحديده، خذ أقل حالة ID

            if (acceptStateIds.Any())
            {
                // إذا كان TuringMachine لديه خاصية AcceptStates كـ HashSet<int> أو List<int>:
                // tm.AcceptStates = acceptStateIds;
                // بما أن TuringMachine يحتوي فقط على AcceptStateId واحد (int)، نستخدم الأول:
                tm.AcceptStateId = acceptStateIds.First();
            }
            else
            {
                // إذا لم يتم تحديد حالة قبول صريحة، ارجع إلى المنطق الافتراضي (أعلى رقم)
                tm.AcceptStateId = allStates.Max();
            }

            tm.RejectStateId = rejectStateId ?? -1; // إذا لم يتم تحديد حالة رفض صريحة، عيّنها إلى -1

            return tm;
        }

        // دالة RunTmStep لا تحتاج إلى تعديل في هذا السياق
        public static bool RunTmStep(TuringMachine tm, ref int currentState, ref Dictionary<int, char> tape, ref int headPosition)
        {
            if (currentState == tm.AcceptStateId) // الآلة في حالة قبول
            {
                return false; // توقف
            }
            if (currentState == tm.RejectStateId) // الآلة في حالة رفض صريحة
            {
                return false; // توقف
            }

            char readSymbol = tape.ContainsKey(headPosition) ? tape[headPosition] : '_';
            var key = new TMTransitionKey(currentState, readSymbol);

            if (!tm.Transitions.ContainsKey(key))
            {
                // إذا لم يكن هناك انتقال معرف، فإن الآلة تتوقف وترفض
                // يمكنك اختيار إما تعيين currentState إلى RejectStateId أو فقط إنهاء التنفيذ.
                // التعيين إلى RejectStateId هنا مفيد للـ UI لعرض حالة الرفض بوضوح.
                currentState = tm.RejectStateId; // توقف وارفض
                return false;
            }

            var trans = tm.Transitions[key];
            tape[headPosition] = trans.WriteSymbol;
            currentState = trans.NextStateId;

            // تحديث headPosition بناءً على MoveDirection
            switch (trans.MoveDirection)
            {
                case TapeMove.R: headPosition += 1; break;
                case TapeMove.L: headPosition -= 1; break;
                case TapeMove.S: /* headPosition لا تتغير */ break;
            }

            return true; // الآلة تستمر في العمل
        }
    }
}