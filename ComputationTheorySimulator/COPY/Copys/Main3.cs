using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ComputationTheorySimulator
{
    public partial class Main3 : Form
    {
        #region UI Components
        private RadioButton rbFiniteAutomata, rbPushdownAutomata, rbTuringMachine;
        private RadioButton rbDeterministic, rbNondeterministic;
        private Panel mainContentPanel;
        private ToolTip toolTip;
        // FA Components
        private TextBox regexInput;
        private Panel faDiagramPanel;
        private DataGridView faTransitionTable;
        private TextBox faTestStringInput;
        private Button faTestButton;
        private Label faResultLabel;
        // PDA Components
        private TextBox pdaTransitionsInput;
        private Panel pdaDiagramPanel; // **جديد**
        private DataGridView pdaTransitionTable;
        private ListBox pdaTraceLog;
        private Label pdaResultLabel;
        // TM Components
        private TextBox tmTransitionsInput;
        private Panel tmDiagramPanel; // **جديد**
        private DataGridView tmTransitionTable;
        private Panel tmTapeVisualizer;
        private Label tmResultLabel;
        #endregion

        #region Models
        // --- General Visual Model ---
        // **جديد:** فئة عامة لتمثيل أي حالة مرئية في الرسم البياني
        public class VisualState
        {
            public int Id { get; set; }
            public Point Position { get; set; }
            public bool IsAcceptState { get; set; }
            public bool IsRejectState { get; set; } // خاص بآلة تورنغ
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
        public class PDATransitionKey
        {
            public int FromStateId { get; set; }
            public char InputSymbol { get; set; }
            public char StackPopSymbol { get; set; }

            public override bool Equals(object obj)
            {
                if (!(obj is PDATransitionKey other)) return false;
                return FromStateId == other.FromStateId &&
                       InputSymbol == other.InputSymbol &&
                       StackPopSymbol == other.StackPopSymbol;
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 23 + FromStateId.GetHashCode();
                    hash = hash * 23 + InputSymbol.GetHashCode();
                    hash = hash * 23 + StackPopSymbol.GetHashCode();
                    return hash;
                }
            }
        }
        // public class PDATransition { public int FromStateId { get; set; } public char InputSymbol { get; set; } public char StackPopSymbol { get; set; } public int NextStateId { get; set; } public string StackPushSymbols { get; set; } }
        public class PDATransition
        {
            public int FromStateId { get; set; }
            public char InputSymbol { get; set; } // '\0' لـ ε
            public char StackPopSymbol { get; set; } // '\0' لـ ε
            public int NextStateId { get; set; }
            public string StackPushSymbols { get; set; } // string.Empty لـ ε
        }
        // -------------------------------------

        //  public class PushdownAutomaton { public Dictionary<int, List<PDATransition>> Transitions { get; set; } = new Dictionary<int, List<PDATransition>>(); public int StartStateId { get; set; } public HashSet<int> AcceptStates { get; set; } = new HashSet<int>(); public char StartStackSymbol { get; set; } = 'Z'; }
        public class PushdownAutomaton
        {
            public Dictionary<int, List<PDATransition>> Transitions { get; } = new Dictionary<int, List<PDATransition>>();
            public int StartStateId { get; set; }
            public HashSet<int> AcceptStates { get; } = new HashSet<int>();
            public char StartStackSymbol { get; set; } = 'Z';
        }
        // دالة محسنة لتحليل تعريف PDA
        private PushdownAutomaton ParsePdaDefinition(string definition, bool isDeterministic)
        {
            var pda = new PushdownAutomaton();
            var lines = definition.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var transitionRegex = new Regex(@"q(\d+)\s*,\s*(.)\s*,\s*(.)\s*;\s*q(\d+)\s*,\s*(.+)");

            // معالجة سطر حالات القبول إذا وجد
            var acceptLine = lines.FirstOrDefault(l => l.Trim().StartsWith("@accept:"));
            if (acceptLine != null)
            {
                var acceptIds = acceptLine.Split(':')[1].Split(',')
                    .Select(id => int.Parse(id.Trim().Substring(1))); // q0,q1 => 0,1
                // **التعديل هنا:** استخدام UnionWith() بدلاً من إعادة التعيين
                pda.AcceptStates.UnionWith(acceptIds);
            }

            // معالجة الانتقالات
            foreach (var line in lines.Where(l => !l.Trim().StartsWith("@")))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var match = transitionRegex.Match(line.Trim());
                if (!match.Success) throw new ArgumentException($"صيغة انتقال غير صالحة: {line}");

                var transition = new PDATransition
                {
                    FromStateId = int.Parse(match.Groups[1].Value),
                    InputSymbol = match.Groups[2].Value == "e" ? '\0' : match.Groups[2].Value[0],
                    StackPopSymbol = match.Groups[3].Value == "e" ? '\0' : match.Groups[3].Value[0],
                    NextStateId = int.Parse(match.Groups[4].Value),
                    StackPushSymbols = match.Groups[5].Value == "e" ? string.Empty : match.Groups[5].Value
                };

                // التحقق من التحديدية إذا لزم الأمر
                if (isDeterministic)
                {
                    var existing = pda.Transitions.ContainsKey(transition.FromStateId) ?
                        pda.Transitions[transition.FromStateId].FirstOrDefault(t =>
                            t.InputSymbol == transition.InputSymbol &&
                            t.StackPopSymbol == transition.StackPopSymbol) :
                        null;

                    if (existing != null)
                    {
                        throw new ArgumentException($"انتقال غير محدد: يوجد بالفعل انتقال من q{transition.FromStateId} بالرمز '{transition.InputSymbol}' ورمز المكدس '{transition.StackPopSymbol}'");
                    }
                }

                if (!pda.Transitions.ContainsKey(transition.FromStateId))
                {
                    pda.Transitions[transition.FromStateId] = new List<PDATransition>();
                }
                pda.Transitions[transition.FromStateId].Add(transition);
            }

            // تحديد حالة البداية إذا لم يتم تحديدها
            if (pda.Transitions.Any() && pda.StartStateId == 0)
            {
                pda.StartStateId = pda.Transitions.Keys.Min();
            }

            return pda;
        }
        // دالة محسنة لرسم PDA
        private void DrawPdaDiagram(PaintEventArgs e, PushdownAutomaton pda, List<VisualState> states)
        {
            if (pda == null || states == null || !states.Any()) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            // إعداد الأقلام والفرش
            var stateBrush = new SolidBrush(Color.LightBlue);
            var statePen = new Pen(Color.DarkBlue, 2);
            var acceptPen = new Pen(Color.Green, 3);
            var transitionPen = new Pen(Color.Black, 2);
            var textBrush = Brushes.Black;
            var font = new Font("Arial", 10, FontStyle.Regular);
            var boldFont = new Font("Arial", 10, FontStyle.Bold);

            // رسم الانتقالات أولاً (تحت الحالات)
            var transitionGroups = pda.Transitions
                .SelectMany(kvp => kvp.Value)
                .GroupBy(t => new { t.FromStateId, t.NextStateId });

            foreach (var group in transitionGroups)
            {
                var fromState = states.First(s => s.Id == group.Key.FromStateId);
                var toState = states.First(s => s.Id == group.Key.NextStateId);

                // تحديد إذا كان انتقال ذاتي (self-loop)
                if (fromState.Id == toState.Id)
                {
                    DrawSelfLoop(e, fromState, group.ToList(), statePen, transitionPen, font);
                }
                else
                {
                    DrawTransitionBetweenStates(e, fromState, toState, group.ToList(), transitionPen, font);
                }
            }

            // رسم الحالات
            foreach (var state in states)
            {
                DrawState(e, state, pda, stateBrush, statePen, acceptPen, textBrush, font, boldFont);
            }

            // تنظيف الموارد
            stateBrush.Dispose();
            statePen.Dispose();
            acceptPen.Dispose();
            transitionPen.Dispose();
            font.Dispose();
            boldFont.Dispose();
        }
        private void DrawState(PaintEventArgs e, VisualState state, PushdownAutomaton pda,
    Brush stateBrush, Pen statePen, Pen acceptPen, Brush textBrush,
    Font font, Font boldFont)
        {
            const int stateRadius = 20;
            var rect = new Rectangle(
                state.Position.X - stateRadius,
                state.Position.Y - stateRadius,
                stateRadius * 2,
                stateRadius * 2);

            // رسم الحالة
            e.Graphics.FillEllipse(stateBrush, rect);
            e.Graphics.DrawEllipse(statePen, rect);

            // إذا كانت حالة قبول، رسم دائرة خارجية
            if (pda.AcceptStates.Contains(state.Id))
            {
                e.Graphics.DrawEllipse(acceptPen,
                    rect.X - 5, rect.Y - 5,
                    rect.Width + 10, rect.Height + 10);
            }

            // إذا كانت حالة البداية، رسم سهم
            if (state.Id == pda.StartStateId)
            {
                DrawStartArrow(e, state, statePen);
            }

            // كتابة اسم الحالة
            var stateName = $"q{state.Id}";
            var textSize = e.Graphics.MeasureString(stateName, font);
            e.Graphics.DrawString(stateName, font, textBrush,
                state.Position.X - textSize.Width / 2,
                state.Position.Y - textSize.Height / 2);
        }

        private void DrawStartArrow(PaintEventArgs e, VisualState state, Pen pen)
        {
            const int arrowLength = 30;
            var startPoint = new Point(
                state.Position.X - arrowLength - 20,
                state.Position.Y);
            var endPoint = new Point(
                state.Position.X - 20,
                state.Position.Y);

            // رسم السهم
            e.Graphics.DrawLine(pen, startPoint, endPoint);

            // رأس السهم
            var arrowHead = new GraphicsPath();
            arrowHead.AddLine(endPoint, new Point(endPoint.X - 10, endPoint.Y - 5));
            arrowHead.AddLine(endPoint, new Point(endPoint.X - 10, endPoint.Y + 5));
            e.Graphics.FillPath(Brushes.Black, arrowHead);
        }

        private void DrawSelfLoop(PaintEventArgs e, VisualState state,
            List<PDATransition> transitions, Pen statePen, Pen transitionPen, Font font)
        {
            const int loopRadius = 15;
            var loopRect = new Rectangle(
                state.Position.X,
                state.Position.Y - loopRadius * 2,
                loopRadius * 2,
                loopRadius * 2);

            // رسم الحلقة الذاتية
            e.Graphics.DrawArc(transitionPen, loopRect, 0, 360);

            // كتابة تسمية الانتقال
            var label = FormatTransitionLabel(transitions);
            var labelSize = e.Graphics.MeasureString(label, font);
            e.Graphics.DrawString(label, font, Brushes.Black,
                state.Position.X + loopRadius + 5,
                state.Position.Y - loopRadius * 2 - labelSize.Height);
        }

        private void DrawTransitionBetweenStates(PaintEventArgs e,
            VisualState fromState, VisualState toState,
            List<PDATransition> transitions, Pen transitionPen, Font font)
        {
            // رسم الخط بين الحالتين
            e.Graphics.DrawLine(transitionPen, fromState.Position, toState.Position);

            // حساب نقطة المنتصف
            var midPoint = new Point(
                (fromState.Position.X + toState.Position.X) / 2,
                (fromState.Position.Y + toState.Position.Y) / 2);

            // كتابة تسمية الانتقال
            var label = FormatTransitionLabel(transitions);
            var labelSize = e.Graphics.MeasureString(label, font);
            e.Graphics.DrawString(label, font, Brushes.Black,
                midPoint.X - labelSize.Width / 2,
                midPoint.Y - labelSize.Height - 10);

            // رسم رأس السهم
            DrawArrowHead(e, fromState.Position, toState.Position, transitionPen);
        }

        private string FormatTransitionLabel(List<PDATransition> transitions)
        {
            return string.Join("\n", transitions.Select(t =>
                $"{GetSymbolDisplay(t.InputSymbol)}," +
                $"{GetSymbolDisplay(t.StackPopSymbol)}/" +
                $"{(string.IsNullOrEmpty(t.StackPushSymbols) ? "ε" : t.StackPushSymbols)}"));
        }

        private string GetSymbolDisplay(char symbol)
        {
            return symbol == '\0' ? "ε" : symbol.ToString();
        }

        private void DrawArrowHead(PaintEventArgs e, Point from, Point to, Pen pen)
        {
            // حساب زاوية الخط
            float angle = (float)Math.Atan2(to.Y - from.Y, to.X - from.X);
            const int arrowSize = 10;

            // نقاط رأس السهم
            PointF[] arrowPoints =
            {
        new PointF(
            to.X - arrowSize * (float)Math.Cos(angle + Math.PI / 6),
            to.Y - arrowSize * (float)Math.Sin(angle + Math.PI / 6)),
        to,
        new PointF(
            to.X - arrowSize * (float)Math.Cos(angle - Math.PI / 6),
            to.Y - arrowSize * (float)Math.Sin(angle - Math.PI / 6))
    };

            // رسم رأس السهم
            e.Graphics.FillPolygon(Brushes.Black, arrowPoints);
        }

        // دالة محسنة لتحديد مواقع الحالات
        private void PositionPdaStates(List<VisualState> states, Panel panel)
        {
            if (states == null || !states.Any() || panel == null) return;

            int centerX = panel.Width / 2;
            int centerY = panel.Height / 2;
            int radius = Math.Min(panel.Width, panel.Height) / 3;

            // ترتيب الحالات في دائرة
            double angleStep = 2 * Math.PI / states.Count;
            for (int i = 0; i < states.Count; i++)
            {
                double angle = i * angleStep;
                states[i].Position = new Point(
                    (int)(centerX + radius * Math.Cos(angle)),
                    (int)(centerY + radius * Math.Sin(angle)));
            }
        }

        // دالة محسنة لبناء PDA
        private void BuildPda()
        {
            try
            {
                bool isDeterministic = rbDeterministic.Checked;
                currentPda = ParsePdaDefinition(pdaTransitionsInput.Text, isDeterministic);

                // جمع جميع معرفات الحالات
                var allStateIds = currentPda.Transitions.Keys
                    .Union(currentPda.Transitions.Values.SelectMany(t => t.Select(tr => tr.NextStateId)))
                    .Distinct()
                    .OrderBy(id => id)
                    .ToList();

                // إنشاء كائنات الحالات المرئية
                allPdaStates = allStateIds.Select(id => new VisualState
                {
                    Id = id,
                    IsAcceptState = currentPda.AcceptStates.Contains(id)
                }).ToList();

                // تحديد مواقع الحالات
                PositionPdaStates(allPdaStates, pdaDiagramPanel);

                // تحديث جدول الانتقالات
                UpdatePdaTransitionTable(currentPda, isDeterministic);

                // إعادة رسم اللوحة
                pdaDiagramPanel.Invalidate();

                pdaResultLabel.Text = "الآلة جاهزة للاختبار";
                pdaResultLabel.ForeColor = Color.Green;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في بناء الآلة: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                currentPda = null;
                allPdaStates.Clear();
                pdaDiagramPanel.Invalidate();
                pdaResultLabel.Text = "خطأ في بناء الآلة";
                pdaResultLabel.ForeColor = Color.Red;
            }
        }

        // دالة محسنة لتحديث جدول الانتقالات
        private void UpdatePdaTransitionTable(PushdownAutomaton pda, bool isDeterministic)
        {
            pdaTransitionTable.Rows.Clear();
            pdaTransitionTable.Columns.Clear();

            // إعداد الأعمدة
            pdaTransitionTable.Columns.Add("From", "من");
            pdaTransitionTable.Columns.Add("Input", "إدخال");
            pdaTransitionTable.Columns.Add("Pop", "إزالة");
            pdaTransitionTable.Columns.Add("To", "إلى");
            pdaTransitionTable.Columns.Add("Push", "إضافة");

            if (!isDeterministic)
            {
                pdaTransitionTable.Columns.Add("NonDet", "غير محددة");
            }

            // ملء الجدول
            foreach (var fromState in pda.Transitions.Keys.OrderBy(k => k))
            {
                foreach (var trans in pda.Transitions[fromState].OrderBy(t => t.InputSymbol).ThenBy(t => t.StackPopSymbol))
                {
                    var row = new DataGridViewRow();

                    row.Cells.Add(new DataGridViewTextBoxCell { Value = $"q{fromState}" });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = trans.InputSymbol == '\0' ? "ε" : trans.InputSymbol.ToString() });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = trans.StackPopSymbol == '\0' ? "ε" : trans.StackPopSymbol.ToString() });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = $"q{trans.NextStateId}" });
                    row.Cells.Add(new DataGridViewTextBoxCell { Value = string.IsNullOrEmpty(trans.StackPushSymbols) ? "ε" : trans.StackPushSymbols });

                    if (!isDeterministic)
                    {
                        // التحقق إذا كان هناك انتقالات أخرى لها نفس الإدخال وإزالة المكدس
                        bool isNonDet = pda.Transitions[fromState]
                            .Count(t => t != trans &&
                                         t.InputSymbol == trans.InputSymbol &&
                                         t.StackPopSymbol == trans.StackPopSymbol) > 0;

                        row.Cells.Add(new DataGridViewTextBoxCell { Value = isNonDet ? "✔" : "" });
                    }

                    pdaTransitionTable.Rows.Add(row);
                }
            }

            pdaTransitionTable.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
        }

        #endregion

        public class PDAConfiguration { public int CurrentStateId { get; set; } public int InputPointer { get; set; } public Stack<char> MachineStack { get; set; } public List<string> TraceHistory { get; set; } }
        public class PdaSimulationResult { public bool IsAccepted { get; } public List<string> Trace { get; } public bool IsDeterministicViolation { get; } public PdaSimulationResult(bool accepted, List<string> trace, bool violation) { IsAccepted = accepted; Trace = trace; IsDeterministicViolation = violation; } }

        // --- TM Models ---
        public enum TapeMove { L, R }
        public struct TMTransitionKey { public readonly int StateId; public readonly char ReadSymbol; public TMTransitionKey(int stateId, char readSymbol) { StateId = stateId; ReadSymbol = readSymbol; } public override bool Equals(object obj) => obj is TMTransitionKey other && this.StateId == other.StateId && this.ReadSymbol == other.ReadSymbol; public override int GetHashCode() { unchecked { return (StateId * 397) ^ ReadSymbol.GetHashCode(); } } }
        public class TMTransition { public int NextStateId { get; set; } public char WriteSymbol { get; set; } public TapeMove MoveDirection { get; set; } }
        public class TuringMachine { public Dictionary<TMTransitionKey, TMTransition> Transitions { get; set; } = new Dictionary<TMTransitionKey, TMTransition>(); public int StartStateId { get; set; } public int AcceptStateId { get; set; } public int RejectStateId { get; set; } }


        #region Machine Instances
        private State startStateDfa;
        private List<State> allDfaStates = new List<State>();
        private PushdownAutomaton currentPda;
        private List<VisualState> allPdaStates = new List<VisualState>(); // **جديد**
        private TuringMachine currentTm;
        private List<VisualState> allTmStates = new List<VisualState>(); // **جديد**
        private Dictionary<int, char> tmTape = new Dictionary<int, char>();
        private int tmHeadPosition = 0;
        private int tmCurrentState = 0;
        #endregion

        public Main3()
        {
            SetupUI();
            this.Text = "محاكي النظرية الاحتسابية - الإصدار 2.0 (مع الرسوم البيانية)";
            this.MinimumSize = new Size(1200, 800);
            this.Size = new Size(1300, 850);
            UpdateActivePanel();
        }

        #region UI Setup
        private void SetupUI()
        {
            this.BackColor = Color.FromArgb(240, 240, 240);
            toolTip = new ToolTip();
            var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            var selectionPanel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(5), WrapContents = false };
            var gbMachineType = new GroupBox { Text = "نوع الآلة", AutoSize = true };
            var machineTypeLayout = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true };
            rbFiniteAutomata = new RadioButton { Text = "آلات منتهية (FA)", Checked = true, AutoSize = true };
            rbPushdownAutomata = new RadioButton { Text = "آلات الدفع للأسفل (PDA)", AutoSize = true };
            rbTuringMachine = new RadioButton { Text = "آلة تورنغ (TM)", AutoSize = true };

            rbFiniteAutomata.CheckedChanged += OnMachineTypeChanged;
            rbPushdownAutomata.CheckedChanged += OnMachineTypeChanged;
            rbTuringMachine.CheckedChanged += OnMachineTypeChanged;

            machineTypeLayout.Controls.AddRange(new Control[] { rbFiniteAutomata, rbPushdownAutomata, rbTuringMachine });
            gbMachineType.Controls.Add(machineTypeLayout);
            var gbSubType = new GroupBox { Text = "النوع الفرعي", AutoSize = true };

            var subTypeLayout = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true };

            rbDeterministic = new RadioButton { Text = "محدودة (Deterministic)", Checked = true, AutoSize = true };
            rbNondeterministic = new RadioButton { Text = "غير محدودة (Non-deterministic)", AutoSize = true };

            subTypeLayout.Controls.AddRange(new Control[] { rbDeterministic, rbNondeterministic });
            gbSubType.Controls.Add(subTypeLayout);
            selectionPanel.Controls.AddRange(new Control[] { gbMachineType, gbSubType });
            mainLayout.Controls.AddRange(new Control[] { selectionPanel, mainContentPanel });
            this.Controls.Add(mainLayout);
        }

        private void UpdateActivePanel()
        {
            mainContentPanel.Controls.Clear();
            rbNondeterministic.Enabled = !rbTuringMachine.Checked;
            if (rbTuringMachine.Checked) rbDeterministic.Checked = true;

            if (rbFiniteAutomata.Checked) mainContentPanel.Controls.Add(CreateFiniteAutomataPanel());
            else if (rbPushdownAutomata.Checked) mainContentPanel.Controls.Add(CreatePushdownAutomataPanel());
            else if (rbTuringMachine.Checked) mainContentPanel.Controls.Add(CreateTuringMachinePanel());
        }

        private Panel CreateFiniteAutomataPanel()
        {
            var faMainPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            faMainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60)); faMainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            var leftPanel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            leftPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); leftPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var inputPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(5), AutoSize = true };
            inputPanel.Controls.Add(new Label { Text = "أدخل التعبير النمطي:", AutoSize = true, Margin = new Padding(0, 6, 0, 0), Font = new Font("Segoe UI", 10) });
            regexInput = new TextBox { Width = 350, Font = new Font("Consolas", 11) };
            var buildFaButton = new Button { Text = "بناء الآلة", Font = new Font("Segoe UI", 9, FontStyle.Bold), BackColor = Color.FromArgb(0, 123, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Height = 30 };
            buildFaButton.Click += BuildFaButton_Click;
            inputPanel.Controls.AddRange(new Control[] { regexInput, buildFaButton });
            faDiagramPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            faDiagramPanel.Paint += FaDiagramPanel_Paint;
            leftPanel.Controls.AddRange(new Control[] { inputPanel, faDiagramPanel });
            var rightPanel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 60)); rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
            faTransitionTable = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, ReadOnly = true, RowHeadersVisible = false, AllowUserToResizeRows = false, BackgroundColor = Color.White, Font = new Font("Segoe UI", 9) };
            var testGroup = new GroupBox { Text = "اختبار السلسلة", Dock = DockStyle.Fill, Padding = new Padding(10), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            var testPanel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 };
            faTestStringInput = new TextBox { Dock = DockStyle.Fill, Font = new Font("Consolas", 11) };
            var faTestButton = new Button { Text = "اختبر", Dock = DockStyle.Right, Font = new Font("Segoe UI", 9, FontStyle.Bold), BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Height = 30 };
            faTestButton.Click += FaTestButton_Click;
            faResultLabel = new Label { Text = "النتيجة: في انتظار بناء الآلة", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 14, FontStyle.Bold) };
            testPanel.Controls.AddRange(new Control[] { faTestStringInput, faTestButton, faResultLabel });
            testGroup.Controls.Add(testPanel);
            rightPanel.Controls.AddRange(new Control[] { faTransitionTable, testGroup });
            faMainPanel.Controls.AddRange(new Control[] { leftPanel, rightPanel });
            return faMainPanel;
        }

        private Panel CreatePushdownAutomataPanel()
        {
            var pdaMainPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            pdaMainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            pdaMainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));

            // -- الجزء الأيسر (الإدخال والرسم) --
            var leftSplitContainer = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 250 };
            var definitionGroup = new GroupBox { Text = "تعريف انتقالات الآلة (PDA)", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            pdaTransitionsInput = new TextBox { Dock = DockStyle.Fill, Multiline = true, Font = new Font("Consolas", 11), ScrollBars = ScrollBars.Vertical, AcceptsReturn = true };
            toolTip.SetToolTip(pdaTransitionsInput, "الصيغة: q_start,input,pop;q_end,push\nمثال: q0,a,Z;q1,AZ\nاستخدم 'e' لـ ε.");
            definitionGroup.Controls.Add(pdaTransitionsInput);

            var diagramGroup = new GroupBox { Text = "الرسم البياني للحالات", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            pdaDiagramPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            pdaDiagramPanel.Paint += PdaDiagramPanel_Paint;
            diagramGroup.Controls.Add(pdaDiagramPanel);

            leftSplitContainer.Panel1.Controls.Add(definitionGroup);
            leftSplitContainer.Panel2.Controls.Add(diagramGroup);

            // -- الجزء الأيمن (التحكم والنتائج) --
            var rightPanel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4 };
            rightPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rightPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            var buildPdaButton = new Button { Text = "بناء الآلة (PDA)", Font = new Font("Segoe UI", 9, FontStyle.Bold), BackColor = Color.FromArgb(0, 123, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Height = 30, Dock = DockStyle.Top };
            buildPdaButton.Click += BuildPdaButton_Click;

            var testGroup = new GroupBox { Text = "اختبار السلسلة", AutoSize = true, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            var testLayout = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Dock = DockStyle.Fill, AutoSize = true };
            var pdaTestStringInput = new TextBox { Width = 200, Font = new Font("Consolas", 11) };
            var testPdaButton = new Button { Text = "اختبر", Font = new Font("Segoe UI", 9, FontStyle.Bold), BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Height = 30 };
            testPdaButton.Click += (s, e) => TestPdaButton_Click(s, e, pdaTestStringInput.Text);
            pdaResultLabel = new Label { Text = "النتيجة:", Font = new Font("Segoe UI", 12, FontStyle.Bold), AutoSize = true, Margin = new Padding(10, 6, 0, 0) };
            testLayout.Controls.AddRange(new Control[] { pdaTestStringInput, testPdaButton, pdaResultLabel });
            testGroup.Controls.Add(testLayout);

            var traceGroup = new GroupBox { Text = "سجل التتبع", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            pdaTraceLog = new ListBox { Dock = DockStyle.Fill, Font = new Font("Consolas", 10), HorizontalScrollbar = true };
            traceGroup.Controls.Add(pdaTraceLog);

            var tableGroup = new GroupBox { Text = "جدول الانتقالات", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            pdaTransitionTable = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, ReadOnly = true, RowHeadersVisible = false, BackgroundColor = Color.White, Font = new Font("Segoe UI", 9) };
            tableGroup.Controls.Add(pdaTransitionTable);

            rightPanel.Controls.Add(buildPdaButton, 0, 0);
            rightPanel.Controls.Add(testGroup, 0, 1);
            rightPanel.Controls.Add(traceGroup, 0, 2);
            rightPanel.Controls.Add(tableGroup, 0, 3);

            pdaMainPanel.Controls.Add(leftSplitContainer, 0, 0);
            pdaMainPanel.Controls.Add(rightPanel, 1, 0);
            return pdaMainPanel;
        }



        private Panel CreateTuringMachinePanel()
        {
            var tmMainPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            tmMainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            tmMainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));

            // -- الجزء الأيسر (الإدخال والرسم) --
            var leftSplitContainer = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 250 };
            var definitionGroup = new GroupBox { Text = "تعريف انتقالات آلة تورنغ (TM)", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            tmTransitionsInput = new TextBox { Dock = DockStyle.Fill, Multiline = true, Font = new Font("Consolas", 11), ScrollBars = ScrollBars.Vertical, AcceptsReturn = true };
            toolTip.SetToolTip(tmTransitionsInput, "الصيغة: q_start,read;q_end,write,move(L/R)\nمثال: q0,a;q1,b,R\nاستخدم '_' للرمز الفارغ.");
            definitionGroup.Controls.Add(tmTransitionsInput);

            var diagramGroup = new GroupBox { Text = "الرسم البياني للحالات", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            tmDiagramPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            tmDiagramPanel.Paint += TmDiagramPanel_Paint;
            diagramGroup.Controls.Add(tmDiagramPanel);

            leftSplitContainer.Panel1.Controls.Add(definitionGroup);
            leftSplitContainer.Panel2.Controls.Add(diagramGroup);

            // -- الجزء الأيمن (التحكم والنتائج) --
            var rightPanel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4 };
            rightPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rightPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            var buildPanel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
            var buildTmButton = new Button { Text = "بناء الآلة (TM)", Font = new Font("Segoe UI", 9, FontStyle.Bold), BackColor = Color.FromArgb(0, 123, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Height = 30 };
            buildTmButton.Click += BuildTmButton_Click;
            var testTmInput = new TextBox { Width = 150, Font = new Font("Consolas", 11) };
            var testTmButton = new Button { Text = "شغل", Font = new Font("Segoe UI", 9, FontStyle.Bold), BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Height = 30 };
            testTmButton.Click += async (s, e) => await RunTmSimulation(testTmInput.Text);
            buildPanel.Controls.AddRange(new Control[] { buildTmButton, testTmInput, testTmButton });

            tmResultLabel = new Label { Text = "الحالة: جاهز", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 14, FontStyle.Bold), Height = 40 };

            var tapeGroup = new GroupBox { Text = "الشريط (Tape)", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            tmTapeVisualizer = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.Fixed3D };
            tmTapeVisualizer.Paint += TmTapeVisualizer_Paint;
            tapeGroup.Controls.Add(tmTapeVisualizer);

            var tableGroup = new GroupBox { Text = "جدول الانتقالات", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            tmTransitionTable = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, ReadOnly = true, RowHeadersVisible = false, BackgroundColor = Color.White, Font = new Font("Segoe UI", 9) };
            tableGroup.Controls.Add(tmTransitionTable);

            rightPanel.Controls.Add(buildPanel, 0, 0);
            rightPanel.Controls.Add(tmResultLabel, 0, 1);
            rightPanel.Controls.Add(tapeGroup, 0, 2);
            rightPanel.Controls.Add(tableGroup, 0, 3);

            tmMainPanel.Controls.Add(leftSplitContainer, 0, 0);
            tmMainPanel.Controls.Add(rightPanel, 1, 0);
            return tmMainPanel;
        }
        #endregion

        #region Event Handlers
        private void OnMachineTypeChanged(object sender, EventArgs e) => UpdateActivePanel();



        private void BuildFaButton_Click(object sender, EventArgs e)
        {
            try
            {
                State.ResetIdCounter();
                string p = AddConcatOperator(regexInput.Text);
                var postfix = InfixToPostfix(p);

                if (rbDeterministic.Checked)
                {
                    // بناء DFA
                    var nfa = PostfixToNfa(postfix);
                    var alphabet = p.Where(char.IsLetterOrDigit).Distinct();
                    var dfaInfo = NfaToDfa(nfa, alphabet);
                    startStateDfa = dfaInfo.Item1;
                    allDfaStates = dfaInfo.Item2;
                    UpdateFaTransitionTable(startStateDfa, allDfaStates, alphabet);
                }
                else
                {
                    // بناء NFA مباشرة
                    var nfa = PostfixToNfa(postfix);
                    startStateDfa = nfa.Start;
                    allDfaStates = GetAllStatesFromNfa(nfa.Start);
                    UpdateNfaTransitionTable(nfa.Start, allDfaStates);
                }

                PositionStates(allDfaStates, faDiagramPanel);
                faDiagramPanel.Invalidate();
                faResultLabel.Text = "الآلة جاهزة";
                faResultLabel.ForeColor = Color.Black;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // دالة مساعدة لجمع جميع الحالات من NFA
        private List<State> GetAllStatesFromNfa(State startState)
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
        private void UpdateNfaTransitionTable(State startState, List<State> allStates)
        {
            faTransitionTable.Rows.Clear();
            faTransitionTable.Columns.Clear();

            // جمع جميع الرموز المستخدمة في الانتقالات
            var symbols = allStates
                .SelectMany(s => s.Transitions.Keys)
                .Where(c => c != '\0') // استبعاد ε-transitions إذا أردنا
                .Distinct()
                .OrderBy(c => c);

            faTransitionTable.Columns.Add("State", "الحالة");
            foreach (char s in symbols)
                faTransitionTable.Columns.Add(s.ToString(), $"'{s}'");

            // عمود منفصل لـ ε-transitions إذا أردنا
            faTransitionTable.Columns.Add("ε", "ε");

            foreach (var state in allStates.OrderBy(s => s.Id))
            {
                var row = new List<string>();
                string stateName = "";
                if (state == startState) stateName += "→";
                stateName += $"q{state.Id}";
                if (state.IsAcceptState) stateName += "*";
                row.Add(stateName);

                foreach (char s in symbols)
                {
                    if (state.Transitions.ContainsKey(s))
                        row.Add(string.Join(",", state.Transitions[s].Select(t => $"q{t.Id}")));
                    else
                        row.Add("—");
                }

                // معالجة ε-transitions
                if (state.Transitions.ContainsKey('\0'))
                    row.Add(string.Join(",", state.Transitions['\0'].Select(t => $"q{t.Id}")));
                else
                    row.Add("—");

                faTransitionTable.Rows.Add(row.ToArray());
            }
        }




        private void FaTestButton_Click(object sender, EventArgs e)
        {
            if (startStateDfa == null) return;

            if (rbDeterministic.Checked)
            {
                TestDfaString(faTestStringInput.Text);
            }
            else
            {
                TestNfaString(faTestStringInput.Text);
            }
        }

        private void TestDfaString(string input)
        {
            State current = startStateDfa;
            foreach (char c in input)
            {
                if (current.Transitions.ContainsKey(c))
                {
                    current = current.Transitions[c][0];
                }
                else
                {
                    faResultLabel.Text = "مرفوضة";
                    faResultLabel.ForeColor = Color.Red;
                    return;
                }
            }

            faResultLabel.Text = current.IsAcceptState ? "مقبولة" : "مرفوضة";
            faResultLabel.ForeColor = current.IsAcceptState ? Color.Green : Color.Red;
        }

        private void TestNfaString(string input)
        {
            var currentStates = EpsilonClosure(new HashSet<State> { startStateDfa });

            foreach (char c in input)
            {
                currentStates = EpsilonClosure(Move(currentStates, c));
                if (!currentStates.Any())
                {
                    faResultLabel.Text = "مرفوضة";
                    faResultLabel.ForeColor = Color.Red;
                    return;
                }
            }

            faResultLabel.Text = currentStates.Any(s => s.IsAcceptState) ? "مقبولة" : "مرفوضة";
            faResultLabel.ForeColor = currentStates.Any(s => s.IsAcceptState) ? Color.Green : Color.Red;
        }

        //-----------------------
        private void FaDiagramPanel_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            if (startStateDfa == null || !allDfaStates.Any()) return;

            var font = new Font("Segoe UI", 10, FontStyle.Bold);
            var stateBrush = new SolidBrush(Color.FromArgb(230, 247, 255));
            var statePen = new Pen(Color.FromArgb(0, 123, 255), 2);
            var acceptStatePen = new Pen(Color.FromArgb(40, 167, 69), 2.5f);
            var textBrush = Brushes.Black;
            var transitionPen = new Pen(Color.FromArgb(50, 50, 50), 2);
            transitionPen.CustomEndCap = new AdjustableArrowCap(5, 5);

            foreach (var state in allDfaStates)
            {
                var transitionsToDraw = new Dictionary<State, List<char>>();
                foreach (var transition in state.Transitions)
                {
                    State target = transition.Value[0];
                    if (!transitionsToDraw.ContainsKey(target))
                    {
                        transitionsToDraw[target] = new List<char>();
                    }
                    transitionsToDraw[target].Add(transition.Key);
                }

                foreach (var item in transitionsToDraw)
                {
                    State targetState = item.Key;
                    string symbols = string.Join(", ", item.Value.OrderBy(c => c));
                    if (state == targetState) // Self-loop
                    {
                        int r = 18, ls = 30;
                        var lr = new Rectangle(state.Position.X, state.Position.Y - r, ls, r * 2);
                        e.Graphics.DrawArc(transitionPen, lr, 90, 270);
                        e.Graphics.DrawString(symbols, font, textBrush, lr.Right + 5, state.Position.Y, new StringFormat { LineAlignment = StringAlignment.Center });
                    }
                    else
                    {
                        e.Graphics.DrawLine(transitionPen, state.Position, targetState.Position);
                        Point mp = new Point((state.Position.X + targetState.Position.X) / 2, (state.Position.Y + targetState.Position.Y) / 2 - 15);
                        e.Graphics.DrawString(symbols, font, textBrush, mp);
                    }
                }
            }
            foreach (var state in allDfaStates)
            {
                int r = 18;
                var rect = new Rectangle(state.Position.X - r, state.Position.Y - r, 2 * r, 2 * r);
                e.Graphics.FillEllipse(stateBrush, rect);
                e.Graphics.DrawEllipse(statePen, rect);
                if (state.IsAcceptState)
                {
                    e.Graphics.DrawEllipse(acceptStatePen, new Rectangle(rect.X - 4, rect.Y - 4, rect.Width + 8, rect.Height + 8));
                }
                if (state == startStateDfa)
                {
                    e.Graphics.DrawString("→", new Font("Arial", 16, FontStyle.Bold), textBrush, state.Position.X - 45, state.Position.Y - 15);
                }
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString($"q{state.Id}", font, textBrush, rect, sf);
            }
            font.Dispose();
            stateBrush.Dispose();
            statePen.Dispose();
            acceptStatePen.Dispose();
            transitionPen.Dispose();
        }
        private PushdownAutomaton ParsePda(string definition, bool isDeterministic)
        {
            var pda = new PushdownAutomaton();
            var lines = definition.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var rx = new Regex(@"^q(\d+)\s*,\s*(.)\s*,\s*(.)\s*;\s*q(\d+)\s*,\s*(.+)$");
            var allStateIds = new HashSet<int>();

            // استخدام Tuple التقليدية للتوافق مع الإصدارات القديمة
            var transitionMap = new Dictionary<Tuple<int, char, char>, List<PDATransition>>();

            // معالجة سطر حالات القبول إذا وجد
            var acceptLine = lines.FirstOrDefault(l => l.Trim().StartsWith("@accept"));
            if (acceptLine != null)
            {
                var acceptIds = Regex.Matches(acceptLine, @"q(\d+)")
                                       .Cast<Match>()
                                       .Select(m => int.Parse(m.Groups[1].Value));
                // **التعديل هنا:** استخدام UnionWith() بدلاً من إعادة التعيين
                pda.AcceptStates.UnionWith(acceptIds);
            }

            // معالجة الانتقالات
            foreach (var line in lines.Where(l => !l.Trim().StartsWith("@accept")))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var m = rx.Match(line.Trim());
                if (!m.Success) throw new ArgumentException($"صيغة خاطئة في السطر: '{line}'");

                int from = int.Parse(m.Groups[1].Value);
                char input = m.Groups[2].Value.Equals("e", StringComparison.OrdinalIgnoreCase) ? '\0' : m.Groups[2].Value[0];
                char pop = m.Groups[3].Value.Equals("e", StringComparison.OrdinalIgnoreCase) ? '\0' : m.Groups[3].Value[0];
                int to = int.Parse(m.Groups[4].Value);
                string push = m.Groups[5].Value.Equals("e", StringComparison.OrdinalIgnoreCase) ? "" : m.Groups[5].Value;

                var key = Tuple.Create(from, input, pop);

                // التحقق من التحديدية إذا كان المطلوب DPDA
                if (isDeterministic && transitionMap.ContainsKey(key) &&
                    transitionMap[key].Any(t => t.InputSymbol == input && t.StackPopSymbol == pop))
                {
                    throw new ArgumentException($"الآلة المحددة لا يمكن أن تحتوي على انتقالات غير محددة: q{from},{input},{pop}");
                }

                if (!transitionMap.ContainsKey(key))
                {
                    transitionMap[key] = new List<PDATransition>();
                }

                var transition = new PDATransition
                {
                    FromStateId = from,
                    InputSymbol = input,
                    StackPopSymbol = pop,
                    NextStateId = to,
                    StackPushSymbols = push
                };

                transitionMap[key].Add(transition);
                allStateIds.Add(from);
                allStateIds.Add(to);
            }

            // بناء قاموس الانتقالات النهائي
            foreach (var kvp in transitionMap)
            {
                if (!pda.Transitions.ContainsKey(kvp.Key.Item1))
                {
                    pda.Transitions[kvp.Key.Item1] = new List<PDATransition>();
                }
                pda.Transitions[kvp.Key.Item1].AddRange(kvp.Value);
            }

            // باقي الكود كما هو...
            return pda;
        }

        private void BuildPdaButton_Click(object sender, EventArgs e)
        {
            try
            {
                currentPda = ParsePda(pdaTransitionsInput.Text, rbDeterministic.Checked);
                UpdatePdaTransitionTable(currentPda, rbDeterministic.Checked);

                // بناء وتحديد مواقع الحالات للرسم
                var allStateIds = currentPda.Transitions.Keys
                    .Union(currentPda.Transitions.Values.SelectMany(t => t).Select(t => t.NextStateId))
                    .Distinct();

                allPdaStates = allStateIds.Select(id => new VisualState
                {
                    Id = id,
                    IsAcceptState = currentPda.AcceptStates.Contains(id)
                }).ToList();

                PositionStates(allPdaStates, pdaDiagramPanel);
                pdaDiagramPanel.Invalidate();

                pdaResultLabel.Text = "الآلة جاهزة";
                pdaResultLabel.ForeColor = Color.Black;
                pdaTraceLog.Items.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في بناء الآلة: {ex.Message}");
                currentPda = null;
                allPdaStates.Clear();
                pdaDiagramPanel.Invalidate();
            }
        }




        private void TestPdaButton_Click(object sender, EventArgs e, string input)
        {
            if (currentPda == null) return;

            pdaTraceLog.Items.Clear();
            pdaTraceLog.Items.Add("بدء المحاكاة...");

            bool isDeterministicMode = rbDeterministic.Checked;
            var result = SimulatePda(currentPda, input, isDeterministicMode);

            foreach (var step in result.Trace)
                pdaTraceLog.Items.Add(step);

            if (result.IsDeterministicViolation)
            {
                pdaResultLabel.Text = "انتهاك المحدودية!";
                pdaResultLabel.ForeColor = Color.DarkOrange;
                pdaTraceLog.Items.Add("== الآلة غير محدودة والوضع المختار هو 'محدودة' ==");
            }
            else if (result.IsAccepted)
            {
                pdaResultLabel.Text = "مقبولة";
                pdaResultLabel.ForeColor = Color.Green;
                pdaTraceLog.Items.Add("== السلسلة مقبولة ==");
            }
            else
            {
                pdaResultLabel.Text = "مرفوضة";
                pdaResultLabel.ForeColor = Color.Red;
                pdaTraceLog.Items.Add("== السلسلة مرفوضة ==");
            }
        }

        private PdaSimulationResult SimulatePda(PushdownAutomaton pda, string input, bool isDeterministicMode)
        {
            var q = new Queue<PDAConfiguration>();
            var initialStack = new Stack<char>();
            initialStack.Push(pda.StartStackSymbol);

            q.Enqueue(new PDAConfiguration
            {
                CurrentStateId = pda.StartStateId,
                InputPointer = 0,
                MachineStack = initialStack,
                TraceHistory = new List<string>()
            });

            int steps = 0;
            bool deterministicViolation = false;

            while (q.Count > 0 && steps++ < 2000)
            {
                var config = q.Dequeue();

                // تسجيل حالة التكوين الحالية
                string remainingInput = config.InputPointer < input.Length ?
                    input.Substring(config.InputPointer) : "ε";
                string stackContents = config.MachineStack.Count == 0 ?
                    "ε" : string.Join("", config.MachineStack.Reverse());

                config.TraceHistory.Add($"(q{config.CurrentStateId}, {remainingInput}, {stackContents})");

                // التحقق من حالة القبول
                if (config.InputPointer == input.Length && pda.AcceptStates.Contains(config.CurrentStateId))
                {
                    config.TraceHistory.Add("=> حالة قبول!");
                    return new PdaSimulationResult(true, config.TraceHistory, false);
                }

                // جمع جميع الانتقالات الممكنة
                var possibleTransitions = new List<PDATransition>();
                if (pda.Transitions.ContainsKey(config.CurrentStateId))
                {
                    char currentInput = config.InputPointer < input.Length ?
                        input[config.InputPointer] : '\0';
                    char stackTop = config.MachineStack.Count > 0 ?
                        config.MachineStack.Peek() : '\0';

                    // الانتقالات التي تطابق الإدخال الحالي (أو ε) ورمز المكدس
                    possibleTransitions = pda.Transitions[config.CurrentStateId]
                        .Where(t => (t.InputSymbol == currentInput || t.InputSymbol == '\0') &&
                                     (t.StackPopSymbol == stackTop || t.StackPopSymbol == '\0'))
                        .ToList();
                }

                // التحقق من انتهاك التحديدية إذا كان في الوضع المحدد
                if (isDeterministicMode && possibleTransitions.Count > 1)
                {
                    config.TraceHistory.Add("! انتهاك التحديدية: أكثر من انتقال ممكن");
                    deterministicViolation = true;
                    continue;
                }

                // معالجة كل انتقال ممكن
                foreach (var trans in possibleTransitions)
                {
                    var newStack = new Stack<char>(config.MachineStack.Reverse());

                    // معالجة إزالة من المكدس
                    if (trans.StackPopSymbol != '\0')
                    {
                        if (newStack.Count == 0 || newStack.Pop() != trans.StackPopSymbol)
                            continue;
                    }

                    // معالجة الإضافة إلى المكدس
                    if (!string.IsNullOrEmpty(trans.StackPushSymbols))
                    {
                        foreach (char c in trans.StackPushSymbols.Reverse())
                        {
                            newStack.Push(c);
                        }
                    }

                    // إنشاء التكوين الجديد
                    var newHist = new List<string>(config.TraceHistory);
                    newHist.Add($"  -> δ(q{config.CurrentStateId}, " +
                                 $"{(trans.InputSymbol == '\0' ? 'ε' : trans.InputSymbol)}, " +
                                 $"{(trans.StackPopSymbol == '\0' ? 'ε' : trans.StackPopSymbol)}) = " +
                                 $"(q{trans.NextStateId}, " +
                                 $"{(string.IsNullOrEmpty(trans.StackPushSymbols) ? "ε" : trans.StackPushSymbols)})");

                    q.Enqueue(new PDAConfiguration
                    {
                        CurrentStateId = trans.NextStateId,
                        InputPointer = config.InputPointer + (trans.InputSymbol == '\0' ? 0 : 1),
                        MachineStack = newStack,
                        TraceHistory = newHist
                    });
                }
            }

            return new PdaSimulationResult(false,
                q.Count > 0 ? q.Peek().TraceHistory : new List<string> { "لم يتم العثور على مسار مقبول" },
                deterministicViolation);
        }


        private void PdaDiagramPanel_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            if (currentPda == null || !allPdaStates.Any()) return;

            var font = new Font("Segoe UI", 9, FontStyle.Bold);
            var stateBrush = new SolidBrush(Color.FromArgb(230, 247, 255));
            var statePen = new Pen(Color.FromArgb(0, 123, 255), 2);
            var acceptStatePen = new Pen(Color.FromArgb(40, 167, 69), 2.5f);
            var textBrush = Brushes.Black;
            var transitionPen = new Pen(Color.FromArgb(50, 50, 50), 2);
            transitionPen.CustomEndCap = new AdjustableArrowCap(5, 5);
            var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

            // تجميع الانتقالات بين نفس الحالتين
            var groupedTransitions = currentPda.Transitions.Values.SelectMany(x => x)
                .GroupBy(t => new { From = t.FromStateId, To = t.NextStateId })
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var group in groupedTransitions)
            {
                var fromState = allPdaStates.FirstOrDefault(s => s.Id == group.Key.From);
                var toState = allPdaStates.FirstOrDefault(s => s.Id == group.Key.To);
                if (fromState == null || toState == null) continue;

                // تمييز الانتقالات غير المحددة بلون مختلف إذا كانت الآلة غير محددة
                if (!rbDeterministic.Checked && group.Value.Count > 1)
                {
                    transitionPen.Color = Color.FromArgb(220, 53, 69); // أحمر للانتقالات غير المحددة
                }
                else
                {
                    transitionPen.Color = Color.FromArgb(50, 50, 50); // أسود عادي
                }

                string label = string.Join("\n", group.Value.Select(t =>
                    $"{(t.InputSymbol == '\0' ? 'ε' : t.InputSymbol)}," +
                    $"{(t.StackPopSymbol == '\0' ? 'ε' : t.StackPopSymbol)}/" +
                    $"{(string.IsNullOrEmpty(t.StackPushSymbols) ? "ε" : t.StackPushSymbols)}"));

                if (fromState.Id == toState.Id) // Self-loop
                {
                    int r = 18, ls = 30;
                    var lr = new Rectangle(fromState.Position.X, fromState.Position.Y - r, ls, r * 2);
                    e.Graphics.DrawArc(transitionPen, lr, 90, 270);
                    e.Graphics.DrawString(label, font, textBrush, lr.Right, fromState.Position.Y, sfCenter);
                }
                else
                {
                    e.Graphics.DrawLine(transitionPen, fromState.Position, toState.Position);
                    Point midPoint = new Point(
                        (fromState.Position.X + toState.Position.X) / 2,
                        (fromState.Position.Y + toState.Position.Y) / 2 - 15);
                    e.Graphics.DrawString(label, font, textBrush, midPoint, sfCenter);
                }
            }

            // رسم الحالات
            foreach (var state in allPdaStates)
            {
                int r = 18;
                var rect = new Rectangle(state.Position.X - r, state.Position.Y - r, 2 * r, 2 * r);
                e.Graphics.FillEllipse(stateBrush, rect);
                e.Graphics.DrawEllipse(statePen, rect);

                if (state.IsAcceptState)
                {
                    e.Graphics.DrawEllipse(acceptStatePen,
                        new Rectangle(rect.X - 4, rect.Y - 4, rect.Width + 8, rect.Height + 8));
                }

                if (state.Id == currentPda.StartStateId)
                {
                    e.Graphics.DrawString("→", new Font("Arial", 16, FontStyle.Bold),
                        textBrush, state.Position.X - 45, state.Position.Y - 15);
                }

                e.Graphics.DrawString($"q{state.Id}", font, textBrush, rect, sfCenter);
            }

            // تنظيف الموارد
            font.Dispose();
            stateBrush.Dispose();
            statePen.Dispose();
            acceptStatePen.Dispose();
            transitionPen.Dispose();
        }

        // --- TM Handlers ---
        private void BuildTmButton_Click(object sender, EventArgs e)
        {
            try
            {
                currentTm = ParseTm(tmTransitionsInput.Text);
                UpdateTmTransitionTable(currentTm);

                // **جديد:** بناء وتحديد مواقع الحالات للرسم
                var allStateIds = currentTm.Transitions.Keys.Select(k => k.StateId)
                    .Union(currentTm.Transitions.Values.Select(t => t.NextStateId)).Distinct();
                allTmStates = allStateIds.Select(id => new VisualState
                {
                    Id = id,
                    IsAcceptState = id == currentTm.AcceptStateId,
                    IsRejectState = id == currentTm.RejectStateId
                }).ToList();
                PositionStates(allTmStates, tmDiagramPanel);
                tmDiagramPanel.Invalidate(); // إعادة رسم اللوحة

                tmResultLabel.Text = "الحالة: الآلة جاهزة";
                tmResultLabel.ForeColor = Color.Black;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في بناء الآلة: {ex.Message}");
                currentTm = null;
                allTmStates.Clear();
                tmDiagramPanel.Invalidate();
            }
        }
        private async Task RunTmSimulation(string input)
        {
            if (currentTm == null) { MessageBox.Show("يجب بناء الآلة أولاً."); return; }
            tmTape.Clear();
            for (int i = 0; i < input.Length; i++) tmTape[i] = input[i];
            tmHeadPosition = 0;
            tmCurrentState = currentTm.StartStateId;
            tmResultLabel.Text = "يعمل...";
            tmResultLabel.ForeColor = Color.Blue;
            tmTapeVisualizer.Invalidate();
            for (int step = 0; step < 1000; step++)
            {
                if (tmCurrentState == currentTm.AcceptStateId) { tmResultLabel.Text = "مقبولة"; tmResultLabel.ForeColor = Color.Green; return; }
                if (tmCurrentState == currentTm.RejectStateId) { tmResultLabel.Text = "مرفوضة"; tmResultLabel.ForeColor = Color.Red; return; }
                char readSymbol = tmTape.ContainsKey(tmHeadPosition) ? tmTape[tmHeadPosition] : '_';
                var key = new TMTransitionKey(tmCurrentState, readSymbol);
                if (!currentTm.Transitions.ContainsKey(key)) { tmResultLabel.Text = "مرفوضة (Halted)"; tmResultLabel.ForeColor = Color.Red; return; }
                var trans = currentTm.Transitions[key];
                tmTape[tmHeadPosition] = trans.WriteSymbol;
                tmCurrentState = trans.NextStateId;
                tmHeadPosition += (trans.MoveDirection == TapeMove.R ? 1 : -1);
                tmTapeVisualizer.Invalidate();
                await Task.Delay(100);
            }
            tmResultLabel.Text = "تجاوز حد الخطوات";
            tmResultLabel.ForeColor = Color.DarkOrange;
        }
        private void TmTapeVisualizer_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.White);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            if (currentTm == null) return;
            int cellSize = 40;
            int visibleCells = tmTapeVisualizer.Width / cellSize;
            int startCellIndex = tmHeadPosition - (visibleCells / 2);
            using (var font = new Font("Consolas", 14, FontStyle.Bold))
            using (var pen = new Pen(Color.Gray))
            using (var headBrush = new SolidBrush(Color.FromArgb(100, 255, 193, 7)))
            {
                for (int i = 0; i < visibleCells; i++)
                {
                    int cellIndex = startCellIndex + i;
                    int x = i * cellSize;
                    var rect = new Rectangle(x, (tmTapeVisualizer.Height / 2) - (cellSize / 2), cellSize, cellSize);
                    char symbol = tmTape.ContainsKey(cellIndex) ? tmTape[cellIndex] : '_';
                    e.Graphics.DrawRectangle(pen, rect);
                    TextRenderer.DrawText(e.Graphics, symbol.ToString(), font, rect, Color.Black, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    if (cellIndex == tmHeadPosition)
                    {
                        e.Graphics.FillRectangle(headBrush, rect);
                        Point[] arrow = { new Point(x + cellSize / 2, 5), new Point(x + cellSize / 2 - 5, 15), new Point(x + cellSize / 2 + 5, 15) };
                        e.Graphics.FillPolygon(Brushes.Crimson, arrow);
                    }
                }
            }
        }

        private void TmDiagramPanel_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            if (currentTm == null || !allTmStates.Any()) return;

            var font = new Font("Segoe UI", 9, FontStyle.Bold);
            var stateBrush = new SolidBrush(Color.FromArgb(230, 247, 255));
            var statePen = new Pen(Color.FromArgb(0, 123, 255), 2);
            var acceptStatePen = new Pen(Color.FromArgb(40, 167, 69), 2.5f);
            var rejectStatePen = new Pen(Color.FromArgb(220, 53, 69), 2.5f); // لون لحالة الرفض
            var textBrush = Brushes.Black;
            var transitionPen = new Pen(Color.FromArgb(50, 50, 50), 2);
            transitionPen.CustomEndCap = new AdjustableArrowCap(5, 5);
            var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

            var groupedTransitions = currentTm.Transitions
                .GroupBy(t => new { From = t.Key.StateId, To = t.Value.NextStateId })
                .ToDictionary(g => g.Key, g => g.ToList());

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
                    e.Graphics.DrawArc(transitionPen, lr, 90, 270);
                    e.Graphics.DrawString(label, font, textBrush, lr.Right, fromState.Position.Y, sfCenter);
                }
                else // Transition between different states
                {
                    e.Graphics.DrawLine(transitionPen, fromState.Position, toState.Position);
                    Point midPoint = new Point((fromState.Position.X + toState.Position.X) / 2, (fromState.Position.Y + toState.Position.Y) / 2 - 15);
                    e.Graphics.DrawString(label, font, textBrush, midPoint, sfCenter);
                }
            }

            foreach (var state in allTmStates)
            {
                int r = 18;
                var rect = new Rectangle(state.Position.X - r, state.Position.Y - r, 2 * r, 2 * r);
                e.Graphics.FillEllipse(stateBrush, rect);
                e.Graphics.DrawEllipse(statePen, rect);

                if (state.IsAcceptState)
                {
                    e.Graphics.DrawEllipse(acceptStatePen, new Rectangle(rect.X - 4, rect.Y - 4, rect.Width + 8, rect.Height + 8));
                }
                else if (state.IsRejectState)
                {
                    e.Graphics.DrawEllipse(rejectStatePen, new Rectangle(rect.X - 4, rect.Y - 4, rect.Width + 8, rect.Height + 8));
                }

                if (state.Id == currentTm.StartStateId)
                {
                    e.Graphics.DrawString("→", new Font("Arial", 16, FontStyle.Bold), textBrush, state.Position.X - 45, state.Position.Y - 15);
                }
                e.Graphics.DrawString($"q{state.Id}", font, Brushes.Black, rect, sfCenter);
            }
            font.Dispose(); stateBrush.Dispose(); statePen.Dispose(); acceptStatePen.Dispose(); rejectStatePen.Dispose(); transitionPen.Dispose();
        }
        #endregion

        #region Business Logic
        // --- FA Logic ---
        private string AddConcatOperator(string pattern)
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
        private string InfixToPostfix(string p)
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
        private NfaFragment PostfixToNfa(string postfix)
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
        private Tuple<State, List<State>> NfaToDfa(NfaFragment nfa, IEnumerable<char> alphabet)
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
        private HashSet<State> EpsilonClosure(HashSet<State> states)
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
        private HashSet<State> Move(HashSet<State> states, char symbol)
        {
            var res = new HashSet<State>();
            foreach (var s in states)
                if (s.Transitions.ContainsKey(symbol))
                    foreach (var t in s.Transitions[symbol])
                        res.Add(t);
            return res;
        }
        private string SetToKey(HashSet<State> set) => "{" + string.Join(",", set.Select(s => s.Id).OrderBy(id => id)) + "}";
        private void UpdateFaTransitionTable(State dfaStart, List<State> allStates, IEnumerable<char> alphabet)
        {
            faTransitionTable.Rows.Clear();
            faTransitionTable.Columns.Clear();
            faTransitionTable.Columns.Add("State", "الحالة");
            foreach (char s in alphabet)
                faTransitionTable.Columns.Add(s.ToString(), $"'{s}'");
            foreach (var state in allStates.OrderBy(s => s.Id))
            {
                var row = new List<string>();
                string n = "";
                if (state == dfaStart) n += "→";
                n += $"q{state.Id}";
                if (state.IsAcceptState) n += "*";
                row.Add(n);
                foreach (char s in alphabet)
                {
                    if (state.Transitions.ContainsKey(s))
                        row.Add($"q{state.Transitions[s][0].Id}");
                    else
                        row.Add("—");
                }
                faTransitionTable.Rows.Add(row.ToArray());
            }
        }
        private void PositionStates(List<State> allStates, Panel diagramPanel)
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

        private void PositionStates(List<VisualState> allStates, Panel diagramPanel)
        {
            if (diagramPanel == null || !allStates.Any()) return;
            int r = Math.Min(diagramPanel.Width, diagramPanel.Height) / 2 - 60;
            if (r < 20) r = 20; // Ensure radius is not too small
            int cx = diagramPanel.Width / 2, cy = diagramPanel.Height / 2;
            double step = 2 * Math.PI / allStates.Count;
            var states = allStates.OrderBy(s => s.Id).ToList();
            for (int i = 0; i < states.Count; i++)
            {
                double angle = i * step - (Math.PI / 2);
                states[i].Position = new Point(cx + (int)(r * Math.Cos(angle)), cy + (int)(r * Math.Sin(angle)));
            }
        }


        //private void UpdatePdaTransitionTable(PushdownAutomaton pda, bool isDeterministic)
        //{
        //    pdaTransitionTable.Rows.Clear();
        //    pdaTransitionTable.Columns.Clear();

        //    // أعمدة الجدول
        //    pdaTransitionTable.Columns.Add("FromState", "الحالة الحالية");
        //    pdaTransitionTable.Columns.Add("Input", "الإدخال");
        //    pdaTransitionTable.Columns.Add("Pop", "إزالة من المكدس");
        //    pdaTransitionTable.Columns.Add("ToState", "الحالة التالية");
        //    pdaTransitionTable.Columns.Add("Push", "إضافة إلى المكدس");

        //    // إذا كانت غير محددة، نضيف عمودًا للإشارة إلى ذلك
        //    if (!isDeterministic)
        //    {
        //        pdaTransitionTable.Columns.Add("NonDet", "غير محددة?");
        //    }

        //    foreach (var stateTransitions in pda.Transitions)
        //    {
        //        int fromState = stateTransitions.Key;
        //        foreach (var trans in stateTransitions.Value)
        //        {
        //            var row = new List<string>
        //    {
        //        $"q{fromState}",
        //        trans.InputSymbol == '\0' ? "ε" : trans.InputSymbol.ToString(),
        //        trans.StackPopSymbol == '\0' ? "ε" : trans.StackPopSymbol.ToString(),
        //        $"q{trans.NextStateId}",
        //        string.IsNullOrEmpty(trans.StackPushSymbols) ? "ε" : trans.StackPushSymbols
        //    };

        //            if (!isDeterministic)
        //            {
        //                // التحقق إذا كان هناك انتقالات أخرى لها نفس المفتاح
        //                var similarTransitions = stateTransitions.Value
        //                    .Where(t => t != trans &&
        //                                 t.InputSymbol == trans.InputSymbol &&
        //                                 t.StackPopSymbol == trans.StackPopSymbol)
        //                    .Count();

        //                row.Add(similarTransitions > 0 ? "✔" : "");
        //            }

        //            pdaTransitionTable.Rows.Add(row.ToArray());
        //        }
        //    }

        //    pdaTransitionTable.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        //}



        private PdaSimulationResult SimulateNPD(PushdownAutomaton pda, string input, bool isDeterministicMode)
        {
            var q = new Queue<PDAConfiguration>();
            var initialStack = new Stack<char>();
            initialStack.Push(pda.StartStackSymbol);
            q.Enqueue(new PDAConfiguration { CurrentStateId = pda.StartStateId, InputPointer = 0, MachineStack = initialStack, TraceHistory = new List<string>() });
            int steps = 0;
            while (q.Count > 0 && steps++ < 2000)
            {
                var config = q.Dequeue();
                config.TraceHistory.Add($"(q{config.CurrentStateId}, {input.Substring(config.InputPointer)}, {string.Join("", config.MachineStack.Reverse())})");
                if (config.InputPointer == input.Length && pda.AcceptStates.Contains(config.CurrentStateId))
                {
                    config.TraceHistory.Add("=> حالة قبول!");
                    return new PdaSimulationResult(true, config.TraceHistory, false);
                }
                var possible = new List<PDATransition>();
                if (pda.Transitions.ContainsKey(config.CurrentStateId))
                {
                    char top = config.MachineStack.Count > 0 ? config.MachineStack.Peek() : '#';
                    char cur = config.InputPointer < input.Length ? input[config.InputPointer] : '\0';

                    // Match specific input or epsilon
                    possible.AddRange(pda.Transitions[config.CurrentStateId]
                        .Where(t => (t.InputSymbol == cur || t.InputSymbol == '\0') && (t.StackPopSymbol == top || t.StackPopSymbol == '\0')));
                }
                if (isDeterministicMode && possible.Count > 1)
                    return new PdaSimulationResult(false, config.TraceHistory, true);
                foreach (var trans in possible)
                {
                    var newStack = new Stack<char>(config.MachineStack.Reverse());
                    if (trans.StackPopSymbol != '\0')
                    {
                        if (newStack.Count == 0 || newStack.Pop() != trans.StackPopSymbol) continue;
                    }
                    if (!string.IsNullOrEmpty(trans.StackPushSymbols))
                        for (int i = trans.StackPushSymbols.Length - 1; i >= 0; i--) newStack.Push(trans.StackPushSymbols[i]);
                    var newHist = new List<string>(config.TraceHistory);
                    newHist.Add($"  -> δ(q{config.CurrentStateId},{(trans.InputSymbol == '\0' ? 'ε' : trans.InputSymbol)},{(trans.StackPopSymbol == '\0' ? 'ε' : trans.StackPopSymbol)})=(q{trans.NextStateId},{(string.IsNullOrEmpty(trans.StackPushSymbols) ? "ε" : trans.StackPushSymbols)})");
                    q.Enqueue(new PDAConfiguration
                    {
                        CurrentStateId = trans.NextStateId,
                        InputPointer = config.InputPointer + (trans.InputSymbol == '\0' ? 0 : 1),
                        MachineStack = newStack,
                        TraceHistory = newHist
                    });
                }
            }
            return new PdaSimulationResult(false, new List<string> { "لم يتم العثور على مسار مقبول" }, false);
        }

        // --- TM Logic ---
        private TuringMachine ParseTm(string definition)
        {
            var tm = new TuringMachine();
            var lines = definition.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var rx = new Regex(@"^q(\d+)\s*,\s*(.)\s*;\s*q(\d+)\s*,\s*(.)\s*,\s*([LR])$", RegexOptions.IgnoreCase);
            var allStates = new HashSet<int>();
            foreach (var line in lines)
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
            if (!allStates.Any()) throw new ArgumentException("لم يتم تعريف حالات");
            tm.StartStateId = allStates.Min();

            // الافتراض: أعلى حالة هي القبول، والثانية هي الرفض
            if (allStates.Count > 1)
            {
                var sorted = allStates.OrderByDescending(s => s).ToList();
                tm.AcceptStateId = sorted[0];
                tm.RejectStateId = sorted[1];
            }
            else
            {
                tm.AcceptStateId = allStates.First();
                tm.RejectStateId = -1; // No reject state
            }
            return tm;
        }
        // --- تصحيح دالة جدول انتقالات TM ---
        private void UpdateTmTransitionTable(TuringMachine tm)
        {
            tmTransitionTable.Rows.Clear();
            tmTransitionTable.Columns.Clear();
            tmTransitionTable.Columns.AddRange(new DataGridViewTextBoxColumn[] {
                new DataGridViewTextBoxColumn { HeaderText = "δ(q,a)", FillWeight = 40 },
                new DataGridViewTextBoxColumn { HeaderText = "(p,b,M)", FillWeight = 60 }
            });

            foreach (var t in tm.Transitions.OrderBy(t => t.Key.StateId).ThenBy(t => t.Key.ReadSymbol))
            {
                tmTransitionTable.Rows.Add($"δ(q{t.Key.StateId}, {t.Key.ReadSymbol})", $"(q{t.Value.NextStateId}, {t.Value.WriteSymbol}, {t.Value.MoveDirection})");
            }

            // **التصحيح:** يتم وضع هذا السطر في النهاية بعد ملء الجدول
            tmTransitionTable.AutoSizeColumnsMode = (DataGridViewAutoSizeColumnsMode)DataGridViewAutoSizeColumnMode.Fill;
        }
        #endregion
    }
}