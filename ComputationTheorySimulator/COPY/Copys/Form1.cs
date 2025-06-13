
//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.Drawing.Drawing2D;
//using System.Linq;
//using System.Windows.Forms;

//namespace ComputationTheorySimulator
//{
//    public partial class Form1 : Form
//    {
//        #region UI Components
//        // --- عناصر التحكم الرئيسية ---
//        private GroupBox gbMachineType;
//        private RadioButton rbFiniteAutomata, rbPushdownAutomata, rbTuringMachine;
//        private GroupBox gbSubType;
//        private RadioButton rbDeterministic, rbNondeterministic;
//        private Panel mainContentPanel; // لوحة المحتوى الرئيسية التي ستتغير

//        // --- عناصر التحكم الخاصة بالآلات المحدودة ---
//        private TextBox regexInput;
//        private Button buildFaButton;
//        private Panel faDiagramPanel;
//        private DataGridView faTransitionTable;
//        private TextBox faTestStringInput;
//        private Button faTestButton;
//        private Label faResultLabel;
//        private ToolTip toolTip;
//        #endregion

//        #region Models (طبقة النماذج)
//        public class State
//        {
//            private static int nextId = 0;
//            public int Id { get; }
//            public bool IsAcceptState { get; set; }
//            public Dictionary<char, List<State>> Transitions { get; } = new Dictionary<char, List<State>>();
//            public Point Position { get; set; }
//            public string DfaStateIdentifier { get; set; }

//            public State(bool isAccept = false)
//            {
//                this.Id = nextId++;
//                this.IsAcceptState = isAccept;
//            }

//            public void AddTransition(char symbol, State toState)
//            {
//                if (!Transitions.ContainsKey(symbol))
//                {
//                    Transitions[symbol] = new List<State>();
//                }
//                Transitions[symbol].Add(toState);
//            }
//            public static void ResetIdCounter() => nextId = 0;
//        }

//        public class NfaFragment
//        {
//            public State Start { get; set; }
//            public State End { get; set; }
//        }
//        #endregion

//        private State startStateDfa;
//        private List<State> allDfaStates = new List<State>();

//        public Form1()
//        {
//            SetupUI();
//            this.Text = "محاكي النظرية الاحتسابية";
//            this.MinimumSize = new Size(1000, 700);
//            this.Size = new Size(1100, 750);
//            // تفعيل واجهة الآلات المحدودة عند بدء التشغيل
//            UpdateActivePanel();
//        }

//        #region UI Setup (طبقة العرض)

//        // **تمت إعادة كتابة هذه الدالة بالكامل**
//        private void SetupUI()
//        {
//            this.BackColor = Color.FromArgb(240, 240, 240);
//            toolTip = new ToolTip();

//            // 1. لوحة التقسيم الرئيسية
//            var mainLayout = new TableLayoutPanel
//            {
//                Dock = DockStyle.Fill,
//                ColumnCount = 1,
//                RowCount = 2
//            };
//            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // صف الاختيارات
//            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // صف المحتوى

//            // 2. لوحة الاختيارات العلوية
//            var selectionPanel = new FlowLayoutPanel
//            {
//                Dock = DockStyle.Top,
//                AutoSize = true,
//                Padding = new Padding(5),
//                WrapContents = false
//            };

//            // --- مجموعة اختيار نوع الآلة ---
//            gbMachineType = new GroupBox { Text = "نوع الآلة", AutoSize = true };
//            var machineTypeLayout = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true };
//            rbFiniteAutomata = new RadioButton { Text = "آلات منتهية (FA)", Checked = true, AutoSize = true };
//            rbPushdownAutomata = new RadioButton { Text = "آلات الدفع للأسفل (PDA)", AutoSize = true };
//            rbTuringMachine = new RadioButton { Text = "آلة تورنغ (TM)", AutoSize = true };
//            // ربط حدث تغيير الاختيار
//            rbFiniteAutomata.CheckedChanged += OnMachineTypeChanged;
//            rbPushdownAutomata.CheckedChanged += OnMachineTypeChanged;
//            rbTuringMachine.CheckedChanged += OnMachineTypeChanged;
//            machineTypeLayout.Controls.AddRange(new Control[] { rbFiniteAutomata, rbPushdownAutomata, rbTuringMachine });
//            gbMachineType.Controls.Add(machineTypeLayout);

//            // --- مجموعة اختيار النوع الفرعي ---
//            gbSubType = new GroupBox { Text = "النوع الفرعي", AutoSize = true };
//            var subTypeLayout = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true };
//            rbDeterministic = new RadioButton { Text = "محدودة (Deterministic)", Checked = true, AutoSize = true };
//            rbNondeterministic = new RadioButton { Text = "غير محدودة (Non-deterministic)", AutoSize = true };
//            subTypeLayout.Controls.AddRange(new Control[] { rbDeterministic, rbNondeterministic });
//            gbSubType.Controls.Add(subTypeLayout);

//            selectionPanel.Controls.AddRange(new Control[] { gbMachineType, gbSubType });

//            // 3. لوحة المحتوى الرئيسية
//            mainContentPanel = new Panel { Dock = DockStyle.Fill };

//            // إضافة اللوحات إلى الواجهة الرئيسية
//            mainLayout.Controls.Add(selectionPanel, 0, 0);
//            mainLayout.Controls.Add(mainContentPanel, 0, 1);
//            this.Controls.Add(mainLayout);
//        }

//        // دالة لتحديث الواجهة بناءً على الاختيار
//        private void UpdateActivePanel()
//        {
//            mainContentPanel.Controls.Clear(); // مسح المحتوى الحالي

//            // إذا كان الاختيار هو آلة منتهية
//            if (rbFiniteAutomata.Checked)
//            {
//                var faPanel = CreateFiniteAutomataPanel(); // إنشاء واجهة الآلات المنتهية
//                mainContentPanel.Controls.Add(faPanel);
//            }
//            else // للأنواع الأخرى في المستقبل
//            {
//                var placeholderLabel = new Label
//                {
//                    Text = $"واجهة { (rbPushdownAutomata.Checked ? "PDA" : "TM") } سيتم تنفيذها مستقبلاً",
//                    Font = new Font("Segoe UI", 16),
//                    Dock = DockStyle.Fill,
//                    TextAlign = ContentAlignment.MiddleCenter,
//                    ForeColor = SystemColors.GrayText
//                };
//                mainContentPanel.Controls.Add(placeholderLabel);
//            }
//        }

//        // دالة لإنشاء واجهة الآلات المحدودة عند الحاجة
//        private Panel CreateFiniteAutomataPanel()
//        {
//            var faMainPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
//            faMainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
//            faMainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));

//            var leftPanel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
//            leftPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
//            leftPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

//            var inputPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(5), AutoSize = true };
//            inputPanel.Controls.Add(new Label { Text = "أدخل التعبير النمطي:", AutoSize = true, Margin = new Padding(0, 6, 0, 0), Font = new Font("Segoe UI", 10) });
//            regexInput = new TextBox { Width = 350, Font = new Font("Consolas", 11) };
//            toolTip.SetToolTip(regexInput, "مثال: (a|b)*.c");
//            buildFaButton = new Button { Text = "بناء الآلة", Font = new Font("Segoe UI", 9, FontStyle.Bold), BackColor = Color.FromArgb(0, 123, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Height = 30 };
//            buildFaButton.FlatAppearance.BorderSize = 0;
//            buildFaButton.Click += BuildFaButton_Click;
//            inputPanel.Controls.AddRange(new Control[] { regexInput, buildFaButton });

//            faDiagramPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
//            faDiagramPanel.Paint += FaDiagramPanel_Paint;

//            leftPanel.Controls.Add(inputPanel, 0, 0);
//            leftPanel.Controls.Add(faDiagramPanel, 0, 1);

//            var rightPanel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
//            rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
//            rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 40));

//            faTransitionTable = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, ReadOnly = true, RowHeadersVisible = false, AllowUserToResizeRows = false, BackgroundColor = Color.White };
//            faTransitionTable.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
//            faTransitionTable.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
//            faTransitionTable.DefaultCellStyle.Font = new Font("Segoe UI", 9);

//            var testGroup = new GroupBox { Text = "اختبار السلسلة", Dock = DockStyle.Fill, Padding = new Padding(10), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
//            var testPanel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 };
//            testPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
//            testPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
//            testPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
//            faTestStringInput = new TextBox { Dock = DockStyle.Fill, Font = new Font("Consolas", 11) };
//            faTestButton = new Button { Text = "اختبر", Dock = DockStyle.Right, Font = new Font("Segoe UI", 9, FontStyle.Bold), BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Height = 30 };
//            faTestButton.FlatAppearance.BorderSize = 0;
//            faTestButton.Click += FaTestButton_Click;
//            faResultLabel = new Label { Text = "النتيجة: في انتظار بناء الآلة", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 14, FontStyle.Bold) };
//            testPanel.Controls.AddRange(new Control[] { faTestStringInput, faTestButton, faResultLabel });
//            testGroup.Controls.Add(testPanel);

//            rightPanel.Controls.AddRange(new Control[] { faTransitionTable, testGroup });
//            faMainPanel.Controls.AddRange(new Control[] { leftPanel, rightPanel });

//            return faMainPanel;
//        }
//        #endregion

//        #region Event Handlers
//        // يتم استدعاء هذه الدالة عند تغيير نوع الآلة
//        private void OnMachineTypeChanged(object sender, EventArgs e)
//        {
//            UpdateActivePanel();
//        }

//        private void BuildFaButton_Click(object sender, EventArgs e)
//        {
//            string pattern = regexInput.Text;
//            if (string.IsNullOrWhiteSpace(pattern))
//            {
//                MessageBox.Show("الرجاء إدخال تعبير نمطي.", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
//                return;
//            }

//            try
//            {
//                State.ResetIdCounter();
//                string formattedPattern = AddConcatOperator(pattern);
//                string postfix = InfixToPostfix(formattedPattern);
//                var nfa = PostfixToNfa(postfix);

//                var alphabet = pattern.Where(c => char.IsLetterOrDigit(c)).Distinct();
//                var dfaInfo = NfaToDfa(nfa, alphabet);

//                startStateDfa = dfaInfo.Item1;
//                allDfaStates = dfaInfo.Item2;

//                UpdateFaTransitionTable(startStateDfa, allDfaStates, alphabet);
//                PositionStates(allDfaStates);
//                faDiagramPanel.Invalidate();
//                faResultLabel.Text = "الآلة جاهزة للاختبار";
//                faResultLabel.ForeColor = Color.Black;
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"حدث خطأ أثناء بناء الآلة: {ex.Message}\nتأكد من صحة التعبير النمطي.", "خطأ في التحويل", MessageBoxButtons.OK, MessageBoxIcon.Error);
//            }
//        }

//        private void FaTestButton_Click(object sender, EventArgs e)
//        {
//            if (startStateDfa == null)
//            {
//                faResultLabel.Text = "النتيجة: يجب بناء الآلة أولاً.";
//                faResultLabel.ForeColor = Color.DarkOrange;
//                return;
//            }

//            string input = faTestStringInput.Text;
//            State currentState = startStateDfa;

//            foreach (char c in input)
//            {
//                if (currentState.Transitions.ContainsKey(c) && currentState.Transitions[c].Any())
//                {
//                    currentState = currentState.Transitions[c][0];
//                }
//                else
//                {
//                    faResultLabel.Text = "النتيجة: مرفوضة (Rejected)";
//                    faResultLabel.ForeColor = Color.Red;
//                    return;
//                }
//            }

//            if (currentState.IsAcceptState)
//            {
//                faResultLabel.Text = "النتيجة: مقبولة (Accepted)";
//                faResultLabel.ForeColor = Color.Green;
//            }
//            else
//            {
//                faResultLabel.Text = "النتيجة: مرفوضة (Rejected)";
//                faResultLabel.ForeColor = Color.Red;
//            }
//        }
//        #endregion

//        #region Business Logic (طبقة منطق الأعمال - بدون تغيير)
//        private string AddConcatOperator(string pattern)
//        {
//            var output = new System.Text.StringBuilder();
//            for (int i = 0; i < pattern.Length; i++)
//            {
//                output.Append(pattern[i]);
//                if (i + 1 < pattern.Length)
//                {
//                    char current = pattern[i];
//                    char next = pattern[i + 1];
//                    if ((char.IsLetterOrDigit(current) || current == ')' || current == '*') && (char.IsLetterOrDigit(next) || next == '('))
//                    {
//                        output.Append('.');
//                    }
//                }
//            }
//            return output.ToString();
//        }

//        private string InfixToPostfix(string pattern)
//        {
//            var precedence = new Dictionary<char, int> { { '|', 1 }, { '.', 2 }, { '*', 3 } };
//            var postfix = new System.Text.StringBuilder();
//            var stack = new Stack<char>();

//            foreach (char c in pattern)
//            {
//                if (char.IsLetterOrDigit(c))
//                {
//                    postfix.Append(c);
//                }
//                else if (c == '(')
//                {
//                    stack.Push(c);
//                }
//                else if (c == ')')
//                {
//                    while (stack.Count > 0 && stack.Peek() != '(')
//                        postfix.Append(stack.Pop());
//                    if (stack.Count == 0) throw new ArgumentException("أقواس غير متطابقة.");
//                    stack.Pop();
//                }
//                else
//                {
//                    while (stack.Count > 0 && stack.Peek() != '(' && precedence.ContainsKey(stack.Peek()) && precedence[stack.Peek()] >= precedence[c])
//                        postfix.Append(stack.Pop());
//                    stack.Push(c);
//                }
//            }

//            while (stack.Count > 0)
//            {
//                if (stack.Peek() == '(') throw new ArgumentException("أقواس غير متطابقة.");
//                postfix.Append(stack.Pop());
//            }

//            return postfix.ToString();
//        }

//        private NfaFragment PostfixToNfa(string postfix)
//        {
//            var stack = new Stack<NfaFragment>();
//            foreach (char c in postfix)
//            {
//                if (char.IsLetterOrDigit(c))
//                {
//                    var start = new State();
//                    var end = new State(true);
//                    start.AddTransition(c, end);
//                    stack.Push(new NfaFragment { Start = start, End = end });
//                }
//                else if (c == '.')
//                {
//                    var frag2 = stack.Pop();
//                    var frag1 = stack.Pop();
//                    frag1.End.IsAcceptState = false;
//                    frag1.End.AddTransition('\0', frag2.Start);
//                    stack.Push(new NfaFragment { Start = frag1.Start, End = frag2.End });
//                }
//                else if (c == '|')
//                {
//                    var frag2 = stack.Pop();
//                    var frag1 = stack.Pop();
//                    var start = new State();
//                    start.AddTransition('\0', frag1.Start);
//                    start.AddTransition('\0', frag2.Start);
//                    var end = new State(true);
//                    frag1.End.IsAcceptState = false;
//                    frag2.End.IsAcceptState = false;
//                    frag1.End.AddTransition('\0', end);
//                    frag2.End.AddTransition('\0', end);
//                    stack.Push(new NfaFragment { Start = start, End = end });
//                }
//                else if (c == '*')
//                {
//                    var frag = stack.Pop();
//                    var start = new State();
//                    var end = new State(true);
//                    start.AddTransition('\0', end);
//                    start.AddTransition('\0', frag.Start);
//                    frag.End.IsAcceptState = false;
//                    frag.End.AddTransition('\0', end);
//                    frag.End.AddTransition('\0', frag.Start);
//                    stack.Push(new NfaFragment { Start = start, End = end });
//                }
//            }
//            if (stack.Count != 1) throw new ArgumentException("تعبير نمطي غير صالح.");
//            return stack.Pop();
//        }

//        private Tuple<State, List<State>> NfaToDfa(NfaFragment nfa, IEnumerable<char> alphabet)
//        {
//            var dfaStates = new Dictionary<string, State>();
//            var unmarkedStates = new Queue<HashSet<State>>();
//            var allCreatedDfaStates = new List<State>();

//            var first = EpsilonClosure(new HashSet<State> { nfa.Start });
//            unmarkedStates.Enqueue(first);

//            var dfaStartState = new State(first.Any(s => s.IsAcceptState));
//            string startKey = SetToKey(first);
//            dfaStartState.DfaStateIdentifier = startKey;
//            dfaStates[startKey] = dfaStartState;
//            allCreatedDfaStates.Add(dfaStartState);

//            while (unmarkedStates.Count > 0)
//            {
//                var currentSet = unmarkedStates.Dequeue();
//                var currentDfaState = dfaStates[SetToKey(currentSet)];

//                foreach (char symbol in alphabet)
//                {
//                    var moveSet = Move(currentSet, symbol);
//                    if (!moveSet.Any()) continue;

//                    var epsilonClosureSet = EpsilonClosure(moveSet);
//                    string key = SetToKey(epsilonClosureSet);

//                    if (!dfaStates.ContainsKey(key))
//                    {
//                        var newDfaState = new State(epsilonClosureSet.Any(s => s.IsAcceptState));
//                        newDfaState.DfaStateIdentifier = key;
//                        dfaStates[key] = newDfaState;
//                        unmarkedStates.Enqueue(epsilonClosureSet);
//                        allCreatedDfaStates.Add(newDfaState);
//                    }
//                    currentDfaState.AddTransition(symbol, dfaStates[key]);
//                }
//            }
//            return Tuple.Create(dfaStartState, allCreatedDfaStates);
//        }

//        private HashSet<State> EpsilonClosure(HashSet<State> states)
//        {
//            var closure = new HashSet<State>(states);
//            var stack = new Stack<State>(states);
//            while (stack.Count > 0)
//            {
//                var s = stack.Pop();
//                if (s.Transitions.ContainsKey('\0'))
//                {
//                    foreach (var target in s.Transitions['\0'])
//                    {
//                        if (closure.Add(target))
//                        {
//                            stack.Push(target);
//                        }
//                    }
//                }
//            }
//            return closure;
//        }

//        private HashSet<State> Move(HashSet<State> states, char symbol)
//        {
//            var result = new HashSet<State>();
//            foreach (var state in states)
//            {
//                if (state.Transitions.ContainsKey(symbol))
//                {
//                    foreach (var target in state.Transitions[symbol])
//                        result.Add(target);
//                }
//            }
//            return result;
//        }

//        private string SetToKey(HashSet<State> set)
//        {
//            return "{" + string.Join(",", set.Select(s => s.Id).OrderBy(id => id)) + "}";
//        }
//        #endregion

//        #region UI Drawing and Table Update (بدون تغيير)
//        private void UpdateFaTransitionTable(State dfaStart, List<State> allStates, IEnumerable<char> alphabet)
//        {
//            faTransitionTable.Rows.Clear();
//            faTransitionTable.Columns.Clear();
//            faTransitionTable.Columns.Add("State", "الحالة (State)");
//            foreach (char symbol in alphabet)
//            {
//                faTransitionTable.Columns.Add(symbol.ToString(), $"'{symbol}'");
//            }
//            faTransitionTable.Columns[0].Frozen = true;

//            foreach (var state in allStates.OrderBy(s => s.Id))
//            {
//                var row = new List<string>();
//                string stateName = "";
//                if (state.Id == dfaStart.Id) stateName += "-> ";
//                stateName += $"q{state.Id}";
//                if (state.IsAcceptState) stateName += " *";
//                row.Add(stateName);

//                foreach (char symbol in alphabet)
//                {
//                    if (state.Transitions.ContainsKey(symbol) && state.Transitions[symbol].Any())
//                    {
//                        row.Add($"q{state.Transitions[symbol][0].Id}");
//                    }
//                    else
//                    {
//                        row.Add("—");
//                    }
//                }
//                faTransitionTable.Rows.Add(row.ToArray());
//            }
//        }

//        private void PositionStates(List<State> allStates)
//        {
//            if (!allStates.Any()) return;
//            int radius = Math.Min(faDiagramPanel.Width, faDiagramPanel.Height) / 2 - 50;
//            int centerX = faDiagramPanel.Width / 2;
//            int centerY = faDiagramPanel.Height / 2;
//            double angleStep = 2 * Math.PI / allStates.Count;
//            var states = allStates.OrderBy(s => s.Id).ToList();
//            for (int i = 0; i < states.Count; i++)
//            {
//                double angle = i * angleStep - (Math.PI / 2);
//                int x = centerX + (int)(radius * Math.Cos(angle));
//                int y = centerY + (int)(radius * Math.Sin(angle));
//                states[i].Position = new Point(x, y);
//            }
//        }

//        private void FaDiagramPanel_Paint(object sender, PaintEventArgs e)
//        {
//            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
//            if (startStateDfa == null || !allDfaStates.Any()) return;

//            var font = new Font("Segoe UI", 10, FontStyle.Bold);
//            var stateBrush = new SolidBrush(Color.FromArgb(230, 247, 255));
//            var statePen = new Pen(Color.FromArgb(0, 123, 255), 2);
//            var acceptStatePen = new Pen(Color.FromArgb(40, 167, 69), 2.5f);
//            var textBrush = Brushes.Black;
//            var transitionPen = new Pen(Color.FromArgb(50, 50, 50), 2);
//            transitionPen.CustomEndCap = new AdjustableArrowCap(5, 5);

//            foreach (var state in allDfaStates)
//            {
//                var transitionsToDraw = new Dictionary<State, List<char>>();
//                foreach (var transition in state.Transitions)
//                {
//                    char symbol = transition.Key;
//                    State target = transition.Value[0];
//                    if (!transitionsToDraw.ContainsKey(target))
//                    {
//                        transitionsToDraw[target] = new List<char>();
//                    }
//                    transitionsToDraw[target].Add(symbol);
//                }

//                foreach (var item in transitionsToDraw)
//                {
//                    State targetState = item.Key;
//                    string symbols = string.Join(", ", item.Value.OrderBy(c => c));
//                    if (state == targetState)
//                    {
//                        using (var path = new GraphicsPath())
//                        using (var loopPen = (Pen)transitionPen.Clone())
//                        {
//                            int r = 18;
//                            int loopSize = 30;
//                            var loopRect = new Rectangle(state.Position.X, state.Position.Y - r, loopSize, r * 2);
//                            path.AddArc(loopRect, 270, 180);
//                            e.Graphics.DrawPath(loopPen, path);
//                            e.Graphics.DrawString(symbols, font, textBrush,
//                                loopRect.Right + 5,
//                                state.Position.Y,
//                                new StringFormat { LineAlignment = StringAlignment.Center });
//                        }
//                    }
//                    else
//                    {
//                        e.Graphics.DrawLine(transitionPen, state.Position, targetState.Position);
//                        Point midPoint = new Point(
//                            (state.Position.X + targetState.Position.X) / 2,
//                            (state.Position.Y + targetState.Position.Y) / 2 - 15);
//                        e.Graphics.DrawString(symbols, font, textBrush, midPoint);
//                    }
//                }
//            }

//            foreach (var state in allDfaStates)
//            {
//                int r = 18;
//                var rect = new Rectangle(state.Position.X - r, state.Position.Y - r, 2 * r, 2 * r);
//                e.Graphics.FillEllipse(stateBrush, rect);
//                e.Graphics.DrawEllipse(statePen, rect);
//                if (state.IsAcceptState)
//                {
//                    e.Graphics.DrawEllipse(acceptStatePen, new Rectangle(rect.X - 4, rect.Y - 4, rect.Width + 8, rect.Height + 8));
//                }
//                if (state.Id == startStateDfa.Id)
//                {
//                    e.Graphics.DrawString("→", new Font("Arial", 16, FontStyle.Bold), textBrush, state.Position.X - 45, state.Position.Y - 15);
//                }
//                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
//                e.Graphics.DrawString($"q{state.Id}", font, textBrush, rect, sf);
//            }
//            font.Dispose();
//            stateBrush.Dispose();
//            statePen.Dispose();
//            acceptStatePen.Dispose();
//            transitionPen.Dispose();
//        }
//        #endregion

//        #region Helpers (بدون تغيير)
//        private IEnumerable<char> GetAlphabet(string pattern)
//        {
//            return pattern.Where(c => char.IsLetterOrDigit(c)).Distinct().OrderBy(c => c);
//        }
//        #endregion
//    }
//}



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
    public partial class Form1 : Form
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
        private DataGridView pdaTransitionTable;
        private ListBox pdaTraceLog;
        private Label pdaResultLabel;
        // TM Components
        private TextBox tmTransitionsInput;
        private DataGridView tmTransitionTable;
        private Panel tmTapeVisualizer;
        private Label tmResultLabel;
        #endregion

        #region Models (تم تعديل هذا القسم لحل الأخطاء)
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
        public class PDATransition { public char InputSymbol { get; set; } public char StackPopSymbol { get; set; } public int NextStateId { get; set; } public string StackPushSymbols { get; set; } }
        public class PushdownAutomaton { public Dictionary<int, List<PDATransition>> Transitions { get; set; } = new Dictionary<int, List<PDATransition>>(); public int StartStateId { get; set; } public HashSet<int> AcceptStates { get; set; } = new HashSet<int>(); public char StartStackSymbol { get; set; } = 'Z'; }
        public class PDAConfiguration { public int CurrentStateId { get; set; } public int InputPointer { get; set; } public Stack<char> MachineStack { get; set; } public List<string> TraceHistory { get; set; } }

        // **تصحيح:** تم إنشاء هذا الكائن ليحل محل (bool, List, bool) لتجنب الأخطاء
        public class PdaSimulationResult
        {
            public bool IsAccepted { get; }
            public List<string> Trace { get; }
            public bool IsDeterministicViolation { get; }
            public PdaSimulationResult(bool accepted, List<string> trace, bool violation) { IsAccepted = accepted; Trace = trace; IsDeterministicViolation = violation; }
        }

        // --- TM Models ---
        public enum TapeMove { L, R }

        // **تصحيح:** تم إنشاء هذا الكائن ليحل محل (int, char) كمفتاح للقاموس
        public struct TMTransitionKey
        {
            public readonly int StateId;
            public readonly char ReadSymbol;
            public TMTransitionKey(int stateId, char readSymbol) { StateId = stateId; ReadSymbol = readSymbol; }
            public override bool Equals(object obj) => obj is TMTransitionKey other && this.StateId == other.StateId && this.ReadSymbol == other.ReadSymbol;
            public override int GetHashCode() { unchecked { return (StateId * 397) ^ ReadSymbol.GetHashCode(); } }
        }
        public class TMTransition { public int NextStateId { get; set; } public char WriteSymbol { get; set; } public TapeMove MoveDirection { get; set; } }
        public class TuringMachine { public Dictionary<TMTransitionKey, TMTransition> Transitions { get; set; } = new Dictionary<TMTransitionKey, TMTransition>(); public int StartStateId { get; set; } public int AcceptStateId { get; set; } public int RejectStateId { get; set; } }
        #endregion

        #region Machine Instances
        private State startStateDfa;
        private List<State> allDfaStates = new List<State>();
        private PushdownAutomaton currentPda;
        private TuringMachine currentTm;
        private Dictionary<int, char> tmTape = new Dictionary<int, char>();
        private int tmHeadPosition = 0;
        private int tmCurrentState = 0;
        #endregion

        public Form1()
        {
            SetupUI();
            this.Text = "محاكي النظرية الاحتسابية - الإصدار 1.1 (النهائي)";
            this.MinimumSize = new Size(1200, 800);
            this.Size = new Size(1200, 800);
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
            mainContentPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
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
            pdaMainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); pdaMainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            var leftPanel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            var definitionGroup = new GroupBox { Text = "تعريف انتقالات الآلة (PDA)", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            pdaTransitionsInput = new TextBox { Dock = DockStyle.Fill, Multiline = true, Font = new Font("Consolas", 11), ScrollBars = ScrollBars.Vertical, AcceptsReturn = true };
            toolTip.SetToolTip(pdaTransitionsInput, "الصيغة: q_start,input,pop;q_end,push\nمثال: q0,a,Z;q1,AZ\nاستخدم 'e' لـ ε");
            definitionGroup.Controls.Add(pdaTransitionsInput);
            pdaTransitionTable = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, ReadOnly = true, RowHeadersVisible = false, BackgroundColor = Color.White, Font = new Font("Segoe UI", 9) };
            leftPanel.Controls.AddRange(new Control[] { definitionGroup, pdaTransitionTable });
            var rightPanel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, RowStyles = { new RowStyle(SizeType.AutoSize), new RowStyle(SizeType.AutoSize), new RowStyle(SizeType.Percent, 100) } };
            var buildPdaButton = new Button { Text = "بناء الآلة (PDA)", Font = new Font("Segoe UI", 9, FontStyle.Bold), BackColor = Color.FromArgb(0, 123, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Height = 30, Dock = DockStyle.Top };
            buildPdaButton.Click += BuildPdaButton_Click;
            var testGroup = new GroupBox { Text = "اختبار السلسلة", AutoSize = true, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            var testLayout = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Dock = DockStyle.Fill, AutoSize = true };
            var pdaTestStringInput = new TextBox { Width = 250, Font = new Font("Consolas", 11) };
            var testPdaButton = new Button { Text = "اختبر", Font = new Font("Segoe UI", 9, FontStyle.Bold), BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Height = 30 };
            testPdaButton.Click += (s, e) => TestPdaButton_Click(s, e, pdaTestStringInput.Text);
            pdaResultLabel = new Label { Text = "النتيجة:", Font = new Font("Segoe UI", 12, FontStyle.Bold), AutoSize = true, Margin = new Padding(10, 6, 0, 0) };
            testLayout.Controls.AddRange(new Control[] { pdaTestStringInput, testPdaButton, pdaResultLabel });
            testGroup.Controls.Add(testLayout);
            var traceGroup = new GroupBox { Text = "سجل التتبع", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            pdaTraceLog = new ListBox { Dock = DockStyle.Fill, Font = new Font("Consolas", 10), HorizontalScrollbar = true };
            traceGroup.Controls.Add(pdaTraceLog);
            rightPanel.Controls.AddRange(new Control[] { buildPdaButton, testGroup, traceGroup });
            pdaMainPanel.Controls.AddRange(new Control[] { leftPanel, rightPanel });
            return pdaMainPanel;
        }

        private Panel CreateTuringMachinePanel()
        {
            var tmMainPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, ColumnStyles = { new ColumnStyle(SizeType.Percent, 50), new ColumnStyle(SizeType.Percent, 50) } };
            var leftPanel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, RowStyles = { new RowStyle(SizeType.Percent, 50), new RowStyle(SizeType.Percent, 50) } };
            var definitionGroup = new GroupBox { Text = "تعريف انتقالات آلة تورنغ (TM)", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            tmTransitionsInput = new TextBox { Dock = DockStyle.Fill, Multiline = true, Font = new Font("Consolas", 11), ScrollBars = ScrollBars.Vertical, AcceptsReturn = true };
            toolTip.SetToolTip(tmTransitionsInput, "الصيغة: q_start,read;q_end,write,move(L/R)\nمثال: q0,a;q1,b,R\nاستخدم '_' للرمز الفارغ.");
            definitionGroup.Controls.Add(tmTransitionsInput);
            tmTransitionTable = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, ReadOnly = true, RowHeadersVisible = false, BackgroundColor = Color.White, Font = new Font("Segoe UI", 9) };
            leftPanel.Controls.AddRange(new Control[] { definitionGroup, tmTransitionTable });
            var rightPanel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, RowStyles = { new RowStyle(SizeType.AutoSize), new RowStyle(SizeType.AutoSize), new RowStyle(SizeType.Percent, 100) } };
            var buildPanel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
            var buildTmButton = new Button { Text = "بناء الآلة (TM)", Font = new Font("Segoe UI", 9, FontStyle.Bold), BackColor = Color.FromArgb(0, 123, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Height = 30 };
            buildTmButton.Click += BuildTmButton_Click;
            var testTmInput = new TextBox { Width = 200, Font = new Font("Consolas", 11) };
            var testTmButton = new Button { Text = "شغل", Font = new Font("Segoe UI", 9, FontStyle.Bold), BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Height = 30 };
            testTmButton.Click += async (s, e) => await RunTmSimulation(testTmInput.Text);
            buildPanel.Controls.AddRange(new Control[] { buildTmButton, testTmInput, testTmButton });
            var tapeGroup = new GroupBox { Text = "الشريط (Tape)", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            tmTapeVisualizer = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.Fixed3D };
            tmTapeVisualizer.Paint += TmTapeVisualizer_Paint;
            tapeGroup.Controls.Add(tmTapeVisualizer);
            tmResultLabel = new Label { Text = "الحالة: جاهز", Dock = DockStyle.Bottom, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 14, FontStyle.Bold), Height = 40 };
            rightPanel.Controls.AddRange(new Control[] { buildPanel, tmResultLabel, tapeGroup });
            tmMainPanel.Controls.AddRange(new Control[] { leftPanel, rightPanel });
            return tmMainPanel;
        }
        #endregion

        #region Event Handlers
        private void OnMachineTypeChanged(object sender, EventArgs e) => UpdateActivePanel();

        // --- FA Handlers ---
        private void BuildFaButton_Click(object sender, EventArgs e) { try { State.ResetIdCounter(); string p = AddConcatOperator(regexInput.Text); var nfa = PostfixToNfa(InfixToPostfix(p)); var alphabet = p.Where(char.IsLetterOrDigit).Distinct(); var dfaInfo = NfaToDfa(nfa, alphabet); startStateDfa = dfaInfo.Item1; allDfaStates = dfaInfo.Item2; UpdateFaTransitionTable(startStateDfa, allDfaStates, alphabet); PositionStates(allDfaStates); faDiagramPanel.Invalidate(); faResultLabel.Text = "الآلة جاهزة"; faResultLabel.ForeColor = Color.Black; } catch (Exception ex) { MessageBox.Show(ex.Message); } }
        private void FaTestButton_Click(object sender, EventArgs e) { if (startStateDfa == null) return; State current = startStateDfa; foreach (char c in faTestStringInput.Text) { if (current.Transitions.ContainsKey(c)) current = current.Transitions[c][0]; else { faResultLabel.Text = "مرفوضة"; faResultLabel.ForeColor = Color.Red; return; } } if (current.IsAcceptState) { faResultLabel.Text = "مقبولة"; faResultLabel.ForeColor = Color.Green; } else { faResultLabel.Text = "مرفوضة"; faResultLabel.ForeColor = Color.Red; } }
        private void FaDiagramPanel_Paint(object sender, PaintEventArgs e) { e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; if (startStateDfa == null) return; var font = new Font("Segoe UI", 10, FontStyle.Bold); var stateBrush = new SolidBrush(Color.FromArgb(230, 247, 255)); var statePen = new Pen(Color.FromArgb(0, 123, 255), 2); var acceptStatePen = new Pen(Color.FromArgb(40, 167, 69), 2.5f); var textBrush = Brushes.Black; var transitionPen = new Pen(Color.FromArgb(50, 50, 50), 2); transitionPen.CustomEndCap = new AdjustableArrowCap(5, 5); foreach (var state in allDfaStates) { var transitionsToDraw = new Dictionary<State, List<char>>(); foreach (var transition in state.Transitions) { State target = transition.Value[0]; if (!transitionsToDraw.ContainsKey(target)) transitionsToDraw[target] = new List<char>(); transitionsToDraw[target].Add(transition.Key); } foreach (var item in transitionsToDraw) { State targetState = item.Key; string symbols = string.Join(", ", item.Value.OrderBy(c => c)); if (state == targetState) { using (var path = new GraphicsPath()) using (var loopPen = (Pen)transitionPen.Clone()) { int r = 18, ls = 30; var lr = new Rectangle(state.Position.X, state.Position.Y - r, ls, r * 2); path.AddArc(lr, 270, 180); e.Graphics.DrawPath(loopPen, path); e.Graphics.DrawString(symbols, font, textBrush, lr.Right + 5, state.Position.Y, new StringFormat { LineAlignment = StringAlignment.Center }); } } else { e.Graphics.DrawLine(transitionPen, state.Position, targetState.Position); Point mp = new Point((state.Position.X + targetState.Position.X) / 2, (state.Position.Y + targetState.Position.Y) / 2 - 15); e.Graphics.DrawString(symbols, font, textBrush, mp); } } } foreach (var state in allDfaStates) { int r = 18; var rect = new Rectangle(state.Position.X - r, state.Position.Y - r, 2 * r, 2 * r); e.Graphics.FillEllipse(stateBrush, rect); e.Graphics.DrawEllipse(statePen, rect); if (state.IsAcceptState) e.Graphics.DrawEllipse(acceptStatePen, new Rectangle(rect.X - 4, rect.Y - 4, rect.Width + 8, rect.Height + 8)); if (state == startStateDfa) e.Graphics.DrawString("→", new Font("Arial", 16, FontStyle.Bold), textBrush, state.Position.X - 45, state.Position.Y - 15); var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center }; e.Graphics.DrawString($"q{state.Id}", font, textBrush, rect, sf); } font.Dispose(); stateBrush.Dispose(); statePen.Dispose(); acceptStatePen.Dispose(); transitionPen.Dispose(); }

        // --- PDA Handlers ---
        private void BuildPdaButton_Click(object sender, EventArgs e) { try { currentPda = ParsePda(pdaTransitionsInput.Text); UpdatePdaTransitionTable(currentPda); pdaResultLabel.Text = "الآلة جاهزة"; pdaResultLabel.ForeColor = Color.Black; pdaTraceLog.Items.Clear(); } catch (Exception ex) { MessageBox.Show(ex.Message); } }
        private void TestPdaButton_Click(object sender, EventArgs e, string input) { if (currentPda == null) return; pdaTraceLog.Items.Clear(); pdaTraceLog.Items.Add("بدء المحاكاة..."); var result = SimulateNPD(currentPda, input, rbDeterministic.Checked); foreach (var step in result.Trace) pdaTraceLog.Items.Add(step); if (result.IsDeterministicViolation) { pdaResultLabel.Text = "انتهاك المحدودية!"; pdaResultLabel.ForeColor = Color.DarkOrange; pdaTraceLog.Items.Add("== الآلة غير محدودة والوضع المختار هو 'محدودة' =="); } else if (result.IsAccepted) { pdaResultLabel.Text = "مقبولة"; pdaResultLabel.ForeColor = Color.Green; pdaTraceLog.Items.Add("== السلسلة مقبولة =="); } else { pdaResultLabel.Text = "مرفوضة"; pdaResultLabel.ForeColor = Color.Red; pdaTraceLog.Items.Add("== السلسلة مرفوضة =="); } }

        // --- TM Handlers ---
        private void BuildTmButton_Click(object sender, EventArgs e) { try { currentTm = ParseTm(tmTransitionsInput.Text); UpdateTmTransitionTable(currentTm); tmResultLabel.Text = "الحالة: الآلة جاهزة"; tmResultLabel.ForeColor = Color.Black; } catch (Exception ex) { MessageBox.Show(ex.Message); } }
        private async Task RunTmSimulation(string input) { if (currentTm == null) { MessageBox.Show("يجب بناء الآلة أولاً."); return; } tmTape.Clear(); for (int i = 0; i < input.Length; i++) tmTape[i] = input[i]; tmHeadPosition = 0; tmCurrentState = currentTm.StartStateId; tmResultLabel.Text = "يعمل..."; tmResultLabel.ForeColor = Color.Blue; tmTapeVisualizer.Invalidate(); for (int step = 0; step < 1000; step++) { if (tmCurrentState == currentTm.AcceptStateId) { tmResultLabel.Text = "مقبولة"; tmResultLabel.ForeColor = Color.Green; return; } if (tmCurrentState == currentTm.RejectStateId) { tmResultLabel.Text = "مرفوضة"; tmResultLabel.ForeColor = Color.Red; return; } char readSymbol = tmTape.ContainsKey(tmHeadPosition) ? tmTape[tmHeadPosition] : '_'; var key = new TMTransitionKey(tmCurrentState, readSymbol); if (!currentTm.Transitions.ContainsKey(key)) { tmResultLabel.Text = "مرفوضة (Halted)"; tmResultLabel.ForeColor = Color.Red; return; } var trans = currentTm.Transitions[key]; tmTape[tmHeadPosition] = trans.WriteSymbol; tmCurrentState = trans.NextStateId; tmHeadPosition += (trans.MoveDirection == TapeMove.R ? 1 : -1); tmTapeVisualizer.Invalidate(); await Task.Delay(100); } tmResultLabel.Text = "تجاوز حد الخطوات"; tmResultLabel.ForeColor = Color.DarkOrange; }
        private void TmTapeVisualizer_Paint(object sender, PaintEventArgs e) { e.Graphics.Clear(Color.White); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; if (currentTm == null) return; int cellSize = 40; int visibleCells = tmTapeVisualizer.Width / cellSize; int startCellIndex = tmHeadPosition - (visibleCells / 2); using (var font = new Font("Consolas", 14, FontStyle.Bold)) using (var pen = new Pen(Color.Gray)) using (var headBrush = new SolidBrush(Color.FromArgb(100, 255, 193, 7))) { for (int i = 0; i < visibleCells; i++) { int cellIndex = startCellIndex + i; int x = i * cellSize; var rect = new Rectangle(x, (tmTapeVisualizer.Height / 2) - (cellSize / 2), cellSize, cellSize); char symbol = tmTape.ContainsKey(cellIndex) ? tmTape[cellIndex] : '_'; e.Graphics.DrawRectangle(pen, rect); TextRenderer.DrawText(e.Graphics, symbol.ToString(), font, rect, Color.Black, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); if (cellIndex == tmHeadPosition) { e.Graphics.FillRectangle(headBrush, rect); Point[] arrow = { new Point(x + cellSize / 2, 5), new Point(x + cellSize / 2 - 5, 15), new Point(x + cellSize / 2 + 5, 15) }; e.Graphics.FillPolygon(Brushes.Crimson, arrow); } } } }
        #endregion

        #region Business Logic
        // --- FA Logic ---
        private string AddConcatOperator(string pattern) { var o = new System.Text.StringBuilder(); for (int i = 0; i < pattern.Length; i++) { o.Append(pattern[i]); if (i + 1 < pattern.Length) { char c = pattern[i], n = pattern[i + 1]; if ((char.IsLetterOrDigit(c) || c == ')' || c == '*') && (char.IsLetterOrDigit(n) || n == '(')) o.Append('.'); } } return o.ToString(); }
        private string InfixToPostfix(string p) { var pre = new Dictionary<char, int> { { '|', 1 }, { '.', 2 }, { '*', 3 } }; var post = new System.Text.StringBuilder(); var s = new Stack<char>(); foreach (char c in p) { if (char.IsLetterOrDigit(c)) post.Append(c); else if (c == '(') s.Push(c); else if (c == ')') { while (s.Count > 0 && s.Peek() != '(') post.Append(s.Pop()); if (s.Count == 0) throw new ArgumentException("أقواس غير متطابقة"); s.Pop(); } else { while (s.Count > 0 && s.Peek() != '(' && pre.ContainsKey(s.Peek()) && pre[s.Peek()] >= pre[c]) post.Append(s.Pop()); s.Push(c); } } while (s.Count > 0) { if (s.Peek() == '(') throw new ArgumentException("أقواس غير متطابقة"); post.Append(s.Pop()); } return post.ToString(); }
        private NfaFragment PostfixToNfa(string postfix) { var s = new Stack<NfaFragment>(); foreach (char c in postfix) { if (char.IsLetterOrDigit(c)) { var st = new State(); var en = new State(true); st.AddTransition(c, en); s.Push(new NfaFragment { Start = st, End = en }); } else if (c == '.') { var f2 = s.Pop(); var f1 = s.Pop(); f1.End.IsAcceptState = false; f1.End.AddTransition('\0', f2.Start); s.Push(new NfaFragment { Start = f1.Start, End = f2.End }); } else if (c == '|') { var f2 = s.Pop(); var f1 = s.Pop(); var st = new State(); st.AddTransition('\0', f1.Start); st.AddTransition('\0', f2.Start); var en = new State(true); f1.End.IsAcceptState = false; f2.End.IsAcceptState = false; f1.End.AddTransition('\0', en); f2.End.AddTransition('\0', en); s.Push(new NfaFragment { Start = st, End = en }); } else if (c == '*') { var f = s.Pop(); var st = new State(); var en = new State(true); st.AddTransition('\0', en); st.AddTransition('\0', f.Start); f.End.IsAcceptState = false; f.End.AddTransition('\0', en); f.End.AddTransition('\0', f.Start); s.Push(new NfaFragment { Start = st, End = en }); } } return s.Pop(); }
        private Tuple<State, List<State>> NfaToDfa(NfaFragment nfa, IEnumerable<char> alphabet) { var dfaStates = new Dictionary<string, State>(); var unmarked = new Queue<HashSet<State>>(); var all = new List<State>(); var first = EpsilonClosure(new HashSet<State> { nfa.Start }); unmarked.Enqueue(first); var dfaStart = new State(first.Any(s => s.IsAcceptState)); string key1 = SetToKey(first); dfaStart.DfaStateIdentifier = key1; dfaStates[key1] = dfaStart; all.Add(dfaStart); while (unmarked.Count > 0) { var currentSet = unmarked.Dequeue(); var currentDfa = dfaStates[SetToKey(currentSet)]; foreach (char sym in alphabet) { var moveSet = Move(currentSet, sym); if (!moveSet.Any()) continue; var closure = EpsilonClosure(moveSet); string key2 = SetToKey(closure); if (!dfaStates.ContainsKey(key2)) { var newDfa = new State(closure.Any(s => s.IsAcceptState)); newDfa.DfaStateIdentifier = key2; dfaStates[key2] = newDfa; unmarked.Enqueue(closure); all.Add(newDfa); } currentDfa.AddTransition(sym, dfaStates[key2]); } } return Tuple.Create(dfaStart, all); }
        private HashSet<State> EpsilonClosure(HashSet<State> states) { var closure = new HashSet<State>(states); var stack = new Stack<State>(states); while (stack.Count > 0) { var s = stack.Pop(); if (s.Transitions.ContainsKey('\0')) foreach (var t in s.Transitions['\0']) if (closure.Add(t)) stack.Push(t); } return closure; }
        private HashSet<State> Move(HashSet<State> states, char symbol) { var res = new HashSet<State>(); foreach (var s in states) if (s.Transitions.ContainsKey(symbol)) foreach (var t in s.Transitions[symbol]) res.Add(t); return res; }
        private string SetToKey(HashSet<State> set) => "{" + string.Join(",", set.Select(s => s.Id).OrderBy(id => id)) + "}";
        private void UpdateFaTransitionTable(State dfaStart, List<State> allStates, IEnumerable<char> alphabet) { faTransitionTable.Rows.Clear(); faTransitionTable.Columns.Clear(); faTransitionTable.Columns.Add("State", "الحالة"); foreach (char s in alphabet) faTransitionTable.Columns.Add(s.ToString(), $"'{s}'"); foreach (var state in allStates.OrderBy(s => s.Id)) { var row = new List<string>(); string n = ""; if (state == dfaStart) n += "→"; n += $"q{state.Id}"; if (state.IsAcceptState) n += "*"; row.Add(n); foreach (char s in alphabet) { if (state.Transitions.ContainsKey(s)) row.Add($"q{state.Transitions[s][0].Id}"); else row.Add("—"); } faTransitionTable.Rows.Add(row.ToArray()); } }
        private void PositionStates(List<State> allStates) { if (!allStates.Any()) return; int r = Math.Min(faDiagramPanel.Width, faDiagramPanel.Height) / 2 - 50; int cx = faDiagramPanel.Width / 2, cy = faDiagramPanel.Height / 2; double step = 2 * Math.PI / allStates.Count; var states = allStates.OrderBy(s => s.Id).ToList(); for (int i = 0; i < states.Count; i++) { double angle = i * step - (Math.PI / 2); states[i].Position = new Point(cx + (int)(r * Math.Cos(angle)), cy + (int)(r * Math.Sin(angle))); } }

        // --- PDA Logic ---
        private PushdownAutomaton ParsePda(string definition) { var pda = new PushdownAutomaton(); var lines = definition.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries); var rx = new Regex(@"^q(\d+)\s*,\s*(.+)\s*,\s*(.+)\s*;\s*q(\d+)\s*,\s*(.+)$"); foreach (var line in lines) { if (string.IsNullOrWhiteSpace(line)) continue; var m = rx.Match(line.Trim()); if (!m.Success) throw new ArgumentException($"صيغة خاطئة: '{line}'"); int from = int.Parse(m.Groups[1].Value); char i = m.Groups[2].Value[0]; char p = m.Groups[3].Value[0]; int to = int.Parse(m.Groups[4].Value); string pu = m.Groups[5].Value; if (!pda.Transitions.ContainsKey(from)) pda.Transitions[from] = new List<PDATransition>(); pda.Transitions[from].Add(new PDATransition { InputSymbol = i, StackPopSymbol = p, NextStateId = to, StackPushSymbols = pu }); } pda.StartStateId = pda.Transitions.Keys.Min(); pda.AcceptStates = new HashSet<int>(pda.Transitions.Keys.Union(pda.Transitions.Values.SelectMany(t => t).Select(t => t.NextStateId))); return pda; }
        private PdaSimulationResult SimulateNPD(PushdownAutomaton pda, string input, bool isDeterministicMode) { var q = new Queue<PDAConfiguration>(); var initialStack = new Stack<char>(); initialStack.Push(pda.StartStackSymbol); q.Enqueue(new PDAConfiguration { CurrentStateId = pda.StartStateId, InputPointer = 0, MachineStack = initialStack, TraceHistory = new List<string>() }); int steps = 0; while (q.Count > 0 && steps++ < 2000) { var config = q.Dequeue(); config.TraceHistory.Add($"(q{config.CurrentStateId}, {input.Substring(config.InputPointer)}, {string.Join("", config.MachineStack.Reverse())})"); if (config.InputPointer == input.Length && pda.AcceptStates.Contains(config.CurrentStateId)) { config.TraceHistory.Add("=> حالة قبول!"); return new PdaSimulationResult(true, config.TraceHistory, false); } var possible = new List<PDATransition>(); if (pda.Transitions.ContainsKey(config.CurrentStateId)) { char top = config.MachineStack.Count > 0 ? config.MachineStack.Peek() : '#'; char cur = config.InputPointer < input.Length ? input[config.InputPointer] : 'e'; possible.AddRange(pda.Transitions[config.CurrentStateId].Where(t => (t.InputSymbol == cur) && (t.StackPopSymbol == top || t.StackPopSymbol == 'e'))); if (cur != 'e') possible.AddRange(pda.Transitions[config.CurrentStateId].Where(t => (t.InputSymbol == 'e') && (t.StackPopSymbol == top || t.StackPopSymbol == 'e'))); } if (isDeterministicMode && possible.Count > 1) return new PdaSimulationResult(false, config.TraceHistory, true); foreach (var trans in possible) { var newStack = new Stack<char>(config.MachineStack.Reverse()); if (trans.StackPopSymbol != 'e') { if (newStack.Count == 0 || newStack.Pop() != trans.StackPopSymbol) continue; } if (trans.StackPushSymbols != "e") for (int i = trans.StackPushSymbols.Length - 1; i >= 0; i--) newStack.Push(trans.StackPushSymbols[i]); var newHist = new List<string>(config.TraceHistory); newHist.Add($"  -> δ(q{config.CurrentStateId},{trans.InputSymbol},{trans.StackPopSymbol})=(q{trans.NextStateId},{trans.StackPushSymbols})"); q.Enqueue(new PDAConfiguration { CurrentStateId = trans.NextStateId, InputPointer = config.InputPointer + (trans.InputSymbol == 'e' ? 0 : 1), MachineStack = newStack, TraceHistory = newHist }); } } return new PdaSimulationResult(false, new List<string> { "لم يتم العثور على مسار مقبول" }, false); }
        private void UpdatePdaTransitionTable(PushdownAutomaton pda) { pdaTransitionTable.Rows.Clear(); pdaTransitionTable.Columns.Clear(); pdaTransitionTable.Columns.AddRange(new DataGridViewTextBoxColumn[] { new DataGridViewTextBoxColumn { HeaderText = "δ(q,a,X)" }, new DataGridViewTextBoxColumn { HeaderText = "(p,Y)" } }); pdaTransitionTable.AutoSizeColumnsMode = (DataGridViewAutoSizeColumnsMode)DataGridViewAutoSizeColumnMode.Fill; foreach (var s in pda.Transitions.Keys.OrderBy(k => k)) foreach (var t in pda.Transitions[s]) pdaTransitionTable.Rows.Add($"δ(q{s}, {t.InputSymbol}, {t.StackPopSymbol})", $"(q{t.NextStateId}, {t.StackPushSymbols})"); }

        // --- TM Logic ---
        private TuringMachine ParseTm(string definition) { var tm = new TuringMachine(); var lines = definition.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries); var rx = new Regex(@"^q(\d+)\s*,\s*(.)\s*;\s*q(\d+)\s*,\s*(.)\s*,\s*([LR])$", RegexOptions.IgnoreCase); var allStates = new HashSet<int>(); foreach (var line in lines) { if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue; var m = rx.Match(line.Trim()); if (!m.Success) throw new ArgumentException($"صيغة خاطئة: '{line}'"); int from = int.Parse(m.Groups[1].Value); char r = m.Groups[2].Value[0]; int to = int.Parse(m.Groups[3].Value); char w = m.Groups[4].Value[0]; TapeMove mov = m.Groups[5].Value.ToUpper() == "R" ? TapeMove.R : TapeMove.L; tm.Transitions[new TMTransitionKey(from, r)] = new TMTransition { NextStateId = to, WriteSymbol = w, MoveDirection = mov }; allStates.Add(from); allStates.Add(to); } if (!allStates.Any()) throw new ArgumentException("لم يتم تعريف حالات"); tm.StartStateId = allStates.Min(); tm.AcceptStateId = allStates.Max(); tm.RejectStateId = -1; if (allStates.Count > 1) { var sorted = allStates.OrderByDescending(s => s).ToList(); tm.AcceptStateId = sorted[0]; tm.RejectStateId = sorted[1]; } return tm; }
        private void UpdateTmTransitionTable(TuringMachine tm) { tmTransitionTable.Rows.Clear(); tmTransitionTable.Columns.Clear(); tmTransitionTable.Columns.AddRange(new DataGridViewTextBoxColumn[] { new DataGridViewTextBoxColumn { HeaderText = "δ(q,a)" }, new DataGridViewTextBoxColumn { HeaderText = "(p,b,M)" } }); tmTransitionTable.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; foreach (var t in tm.Transitions.OrderBy(t => t.Key.StateId).ThenBy(t => t.Key.ReadSymbol)) tmTransitionTable.Rows.Add($"δ(q{t.Key.StateId}, {t.Key.ReadSymbol})", $"(q{t.Value.NextStateId}, {t.Value.WriteSymbol}, {t.Value.MoveDirection})"); }
        #endregion
    }
}