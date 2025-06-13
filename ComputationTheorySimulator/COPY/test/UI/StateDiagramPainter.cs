using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using ComputationTheorySimulator.Models;

namespace ComputationTheorySimulator.UI
{
    public static class StateDiagramPainter
    {
        public static void PositionFaStates(List<State> allStates, Panel diagramPanel)
        {
            if (diagramPanel == null || !allStates.Any()) return;
            int r = Math.Min(diagramPanel.Width, diagramPanel.Height) / 2 - 50;
            if (r < 20) r = 20;
            int cx = diagramPanel.Width / 2, cy = diagramPanel.Height / 2;
            double step = 2 * Math.PI / allStates.Count;
            var states = allStates.OrderBy(s => s.Id).ToList();
            for (int i = 0; i < states.Count; i++)
            {
                double angle = i * step - (Math.PI / 2);
                states[i].Position = new Point(cx + (int)(r * Math.Cos(angle)), cy + (int)(r * Math.Sin(angle)));
            }
        }

        public static void PositionStates<T>(List<T> allStates, Panel diagramPanel) where T : VisualState
        {
            if (diagramPanel == null || !allStates.Any()) return;
            int r = Math.Min(diagramPanel.Width, diagramPanel.Height) / 2 - 60;
            if (r < 20) r = 20;
            int cx = diagramPanel.Width / 2, cy = diagramPanel.Height / 2;
            double step = 2 * Math.PI / allStates.Count;
            var states = allStates.OrderBy(s => s.Id).ToList();
            for (int i = 0; i < states.Count; i++)
            {
                double angle = i * step - (Math.PI / 2);
                states[i].Position = new Point(cx + (int)(r * Math.Cos(angle)), cy + (int)(r * Math.Sin(angle)));
            }
        }

        public static void DrawFiniteAutomaton(Graphics g, List<State> allStates, State startState, Panel panel)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (startState == null || !allStates.Any()) return;

            // استخدام القاموس يظل أفضل ممارسة للوصول السريع والآمن
            var positionedStates = allStates.ToDictionary(s => s.Id, s => s);

            using (var font = new Font("Segoe UI", 9, FontStyle.Bold))
            using (var stateBrush = new SolidBrush(Color.FromArgb(230, 247, 255)))
            using (var statePen = new Pen(Color.FromArgb(0, 123, 255), 2))
            using (var acceptStatePen = new Pen(Color.FromArgb(40, 167, 69), 2.5f))
            using (var textBrush = Brushes.Black)
            using (var transitionPen = new Pen(Color.FromArgb(50, 50, 50), 2) { CustomEndCap = new AdjustableArrowCap(5, 5) })
            {
                var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

                // ===================================================================
                // ===== [تصحيح نهائي] منطق الرسم المبسط والمباشر =====
                // ===================================================================

                // 1. نرسم جميع الانتقالات أولاً
                foreach (var sourceState in positionedStates.Values)
                {
                    foreach (var transition in sourceState.Transitions)
                    {
                        var symbol = transition.Key == '\0' ? "ε" : transition.Key.ToString();

                        foreach (var targetState in transition.Value)
                        {
                            // التحقق الأهم: هل الحالة الهدف لها موقع محسوب؟
                            if (positionedStates.ContainsKey(targetState.Id))
                            {
                                var positionedTarget = positionedStates[targetState.Id];

                                if (sourceState.Id == positionedTarget.Id) // Self-loop
                                {
                                    int r = 18, ls = 30;
                                    var lr = new Rectangle(sourceState.Position.X, sourceState.Position.Y - r, ls, r * 2);
                                    g.DrawArc(transitionPen, lr, 90, 270);
                                    // نعطي إزاحة بسيطة للرمز لتجنب التداخل
                                    g.DrawString(symbol, font, textBrush, lr.Right + 5, sourceState.Position.Y - 10);
                                }
                                else // انتقال بين حالتين
                                {
                                    g.DrawLine(transitionPen, sourceState.Position, positionedTarget.Position);
                                    Point mp = new Point(
                                        (sourceState.Position.X + positionedTarget.Position.X) / 2,
                                        (sourceState.Position.Y + positionedTarget.Position.Y) / 2 - 15);
                                    g.DrawString(symbol, font, textBrush, mp);
                                }
                            }
                        }
                    }
                }

                // 2. نرسم دوائر الحالات فوق الأسهم لضمان وضوحها
                foreach (var state in positionedStates.Values)
                {
                    int r = 18;
                    var rect = new Rectangle(state.Position.X - r, state.Position.Y - r, 2 * r, 2 * r);
                    g.FillEllipse(stateBrush, rect);
                    g.DrawEllipse(statePen, rect);

                    if (state.IsAcceptState)
                    {
                        g.DrawEllipse(acceptStatePen, new Rectangle(rect.X - 4, rect.Y - 4, rect.Width + 8, rect.Height + 8));
                    }
                    if (state.Id == startState.Id)
                    {
                        using (var arrowFont = new Font("Arial", 16, FontStyle.Bold))
                            g.DrawString("→", arrowFont, textBrush, state.Position.X - 45, state.Position.Y - 15);
                    }
                    g.DrawString($"q{state.Id}", font, textBrush, rect, sfCenter);
                }
            }
        }

        public static void DrawPushdownAutomaton(Graphics g, List<VisualState> allPdaStates, PushdownAutomaton pda, bool isDeterministic, Panel panel)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (pda == null || !allPdaStates.Any()) return;

            using (var font = new Font("Segoe UI", 9, FontStyle.Bold))
            using (var stateBrush = new SolidBrush(Color.FromArgb(230, 247, 255)))
            using (var statePen = new Pen(Color.FromArgb(0, 123, 255), 2))
            using (var acceptStatePen = new Pen(Color.FromArgb(40, 167, 69), 2.5f))
            using (var textBrush = Brushes.Black)
            using (var transitionPen = new Pen(Color.FromArgb(50, 50, 50), 2) { CustomEndCap = new AdjustableArrowCap(5, 5) })
            {
                var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

                // 1. تجميع الانتقالات بين نفس الحالتين
                var groupedTransitions = pda.Transitions.Values.SelectMany(x => x)
                    .GroupBy(t => new { From = t.FromStateId, To = t.NextStateId })
                    .ToDictionary(grp => grp.Key, grp => grp.ToList());

                // 2. رسم الانتقالات (الأسهم)
                foreach (var group in groupedTransitions)
                {
                    var fromState = allPdaStates.FirstOrDefault(s => s.Id == group.Key.From);
                    var toState = allPdaStates.FirstOrDefault(s => s.Id == group.Key.To);
                    if (fromState == null || toState == null) continue;

                    // إنشاء التسمية (label) مجمعة
                    string label = string.Join("\n", group.Value.Select(t =>
                        $"{(t.InputSymbol == '\0' ? 'ε' : t.InputSymbol)}," +
                        $"{(t.StackPopSymbol == '\0' ? 'ε' : t.StackPopSymbol)}/" +
                        $"{(string.IsNullOrEmpty(t.StackPushSymbols) ? "ε" : t.StackPushSymbols)}"));

                    if (fromState.Id == toState.Id) // Self-loop
                    {
                        int r = 18, ls = 30;
                        var lr = new Rectangle(fromState.Position.X, fromState.Position.Y - r, ls, r * 2);
                        g.DrawArc(transitionPen, lr, 90, 270);
                        g.DrawString(label, font, textBrush, lr.Right + 5, fromState.Position.Y, sfCenter);
                    }
                    else // انتقال بين حالتين
                    {
                        g.DrawLine(transitionPen, fromState.Position, toState.Position);
                        Point midPoint = new Point((fromState.Position.X + toState.Position.X) / 2, (fromState.Position.Y + toState.Position.Y) / 2 - 15);
                        g.DrawString(label, font, textBrush, midPoint, sfCenter);
                    }
                }

                // 3. رسم الحالات (الدوائر) فوق الأسهم
                foreach (var state in allPdaStates)
                {
                    int r = 18;
                    var rect = new Rectangle(state.Position.X - r, state.Position.Y - r, 2 * r, 2 * r);
                    g.FillEllipse(stateBrush, rect);
                    g.DrawEllipse(statePen, rect);

                    if (state.IsAcceptState)
                    {
                        g.DrawEllipse(acceptStatePen, new Rectangle(rect.X - 4, rect.Y - 4, rect.Width + 8, rect.Height + 8));
                    }
                    if (state.Id == pda.StartStateId)
                    {
                        using (var arrowFont = new Font("Arial", 16, FontStyle.Bold))
                            g.DrawString("→", arrowFont, textBrush, state.Position.X - 45, state.Position.Y - 15);
                    }
                    g.DrawString($"q{state.Id}", font, textBrush, rect, sfCenter);
                }
            }
        }
        public static void DrawTuringMachine(Graphics g, List<VisualState> allTmStates, TuringMachine tm, Panel panel)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (tm == null || !allTmStates.Any()) return;

            using (var font = new Font("Segoe UI", 9, FontStyle.Bold))
            using (var stateBrush = new SolidBrush(Color.FromArgb(230, 247, 255)))
            using (var statePen = new Pen(Color.FromArgb(0, 123, 255), 2))
            using (var acceptStatePen = new Pen(Color.FromArgb(40, 167, 69), 2.5f))
            using (var rejectStatePen = new Pen(Color.FromArgb(220, 53, 69), 2.5f))
            using (var textBrush = Brushes.Black)
            using (var transitionPen = new Pen(Color.FromArgb(50, 50, 50), 2) { CustomEndCap = new AdjustableArrowCap(5, 5) })
            {
                var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

                var groupedTransitions = tm.Transitions
                    .GroupBy(t => new { From = t.Key.StateId, To = t.Value.NextStateId })
                    .ToDictionary(grp => grp.Key, grp => grp.ToList());

                foreach (var group in groupedTransitions)
                {
                    var fromState = allTmStates.FirstOrDefault(s => s.Id == group.Key.From);
                    var toState = allTmStates.FirstOrDefault(s => s.Id == group.Key.To);
                    if (fromState == null || toState == null) continue;

                    string label = string.Join("\n", group.Value.Select(t => $"{t.Key.ReadSymbol} → {t.Value.WriteSymbol}, {t.Value.MoveDirection}"));

                    if (fromState.Id == toState.Id) // Self-loop
                    {
                        int r = 18, ls = 30;
                        var lr = new Rectangle(fromState.Position.X, fromState.Position.Y - r, ls, r * 2);
                        g.DrawArc(transitionPen, lr, 90, 270);
                        g.DrawString(label, font, textBrush, lr.Right, fromState.Position.Y, sfCenter);
                    }
                    else
                    {
                        g.DrawLine(transitionPen, fromState.Position, toState.Position);
                        // [تصحيح] تم تعديل اسم المتغير هنا
                        Point midPoint = new Point((fromState.Position.X + toState.Position.X) / 2, (fromState.Position.Y + toState.Position.Y) / 2 - 15);
                        g.DrawString(label, font, textBrush, midPoint, sfCenter);
                    }
                }

                foreach (var state in allTmStates)
                {
                    int r = 18;
                    var rect = new Rectangle(state.Position.X - r, state.Position.Y - r, 2 * r, 2 * r);
                    g.FillEllipse(stateBrush, rect);
                    g.DrawEllipse(statePen, rect);

                    if (state.IsAcceptState)
                    {
                        g.DrawEllipse(acceptStatePen, new Rectangle(rect.X - 4, rect.Y - 4, rect.Width + 8, rect.Height + 8));
                    }
                    else if (state.IsRejectState)
                    {
                        g.DrawEllipse(rejectStatePen, new Rectangle(rect.X - 4, rect.Y - 4, rect.Width + 8, rect.Height + 8));
                    }

                    if (state.Id == tm.StartStateId)
                    {
                        using (var arrowFont = new Font("Arial", 16, FontStyle.Bold))
                            g.DrawString("→", arrowFont, textBrush, state.Position.X - 45, state.Position.Y - 15);
                    }
                    g.DrawString($"q{state.Id}", font, textBrush, rect, sfCenter);
                }
            }
        }
    }
}