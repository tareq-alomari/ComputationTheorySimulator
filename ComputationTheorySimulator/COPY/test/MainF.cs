using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

// استيراد الفئات الجديدة
using ComputationTheorySimulator.Exceptions;
using ComputationTheorySimulator.Logic;
using ComputationTheorySimulator.Models;
using ComputationTheorySimulator.UI;

namespace ComputationTheorySimulator
{
    public partial class MainF : Form
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
        private Panel pdaDiagramPanel;
        private DataGridView pdaTransitionTable;
        private ListBox pdaTraceLog;
        private Label pdaResultLabel;
        // TM Components
        private TextBox tmTransitionsInput;
        private Panel tmDiagramPanel;
        private DataGridView tmTransitionTable;
        private Panel tmTapeVisualizer;
        private Label tmResultLabel;
        #endregion

        #region Machine Instances
        private State startStateDfa;
        private List<State> allDfaStates = new List<State>();
        private PushdownAutomaton currentPda;
        private List<VisualState> allPdaStates = new List<VisualState>();
        private TuringMachine currentTm;
        private List<VisualState> allTmStates = new List<VisualState>();
        private Dictionary<int, char> tmTape = new Dictionary<int, char>();
        private int tmHeadPosition = 0;
        private int tmCurrentState = 0;
        #endregion

        public MainF()
        {
            SetupUI();
            this.Text = "محاكي النظرية الاحتسابية - الإصدار 3.0 (Refactored)";
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
            faTestButton = new Button { Text = "اختبر", Dock = DockStyle.Right, Font = new Font("Segoe UI", 9, FontStyle.Bold), BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Height = 30 };
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

        // --- FA Handlers ---
        private void BuildFaButton_Click(object sender, EventArgs e)
        {
            try
            {
                State.ResetIdCounter();
                string pattern = regexInput.Text;
                if (string.IsNullOrWhiteSpace(pattern)) throw new ParsingException("التعبير النمطي فارغ.");

                string p = FaLogic.AddConcatOperator(pattern);
                var postfix = FaLogic.InfixToPostfix(p);
                var nfa = FaLogic.PostfixToNfa(postfix);
                var alphabet = pattern.Where(char.IsLetterOrDigit).Distinct();

                if (rbDeterministic.Checked)
                {
                    var dfaInfo = FaLogic.NfaToDfa(nfa, alphabet);
                    startStateDfa = dfaInfo.Item1;
                    allDfaStates = dfaInfo.Item2;
                    UpdateFaTransitionTable(startStateDfa, allDfaStates, alphabet);
                }
                else
                {
                    startStateDfa = nfa.Start;
                    allDfaStates = FaLogic.GetAllStatesFromNfa(nfa.Start);
                    UpdateNfaTransitionTable(nfa.Start, allDfaStates);
                }

                StateDiagramPainter.PositionFaStates(allDfaStates, faDiagramPanel);
                faDiagramPanel.Invalidate();
                faResultLabel.Text = "الآلة جاهزة";
                faResultLabel.ForeColor = Color.Black;
            }
            catch (ParsingException ex)
            {
                MessageBox.Show(this, $"خطأ في بناء الآلة: {ex.Message}", "خطأ في التحليل", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"خطأ غير متوقع: {ex.Message}", "خطأ فادح", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FaTestButton_Click(object sender, EventArgs e)
        {
            if (startStateDfa == null) { MessageBox.Show("يرجى بناء الآلة أولاً."); return; }

            bool isAccepted = rbDeterministic.Checked
                ? FaLogic.TestDfaString(startStateDfa, faTestStringInput.Text)
                : FaLogic.TestNfaString(startStateDfa, faTestStringInput.Text);

            faResultLabel.Text = isAccepted ? "مقبولة" : "مرفوضة";
            faResultLabel.ForeColor = isAccepted ? Color.Green : Color.Red;
        }

        private void FaDiagramPanel_Paint(object sender, PaintEventArgs e)
        {
            if (allDfaStates.Any())
            {
                StateDiagramPainter.DrawFiniteAutomaton(e.Graphics, allDfaStates, startStateDfa, faDiagramPanel);
            }
        }

        // --- PDA Handlers ---
        private void BuildPdaButton_Click(object sender, EventArgs e)
        {
            try
            {
                currentPda = PdaLogic.ParsePda(pdaTransitionsInput.Text, rbDeterministic.Checked);
                UpdatePdaTransitionTable(currentPda);

                var allStateIds = currentPda.Transitions.Keys
                    .Union(currentPda.Transitions.Values.SelectMany(t => t).Select(t => t.NextStateId))
                    .Union(currentPda.AcceptStates)
                    .Distinct();

                allPdaStates = allStateIds.Select(id => new VisualState { Id = id, IsAcceptState = currentPda.AcceptStates.Contains(id) }).ToList();

                StateDiagramPainter.PositionStates(allPdaStates, pdaDiagramPanel);
                pdaDiagramPanel.Invalidate();

                pdaResultLabel.Text = "الآلة جاهزة";
                pdaResultLabel.ForeColor = Color.Black;
                pdaTraceLog.Items.Clear();
            }
            catch (ParsingException ex)
            {
                MessageBox.Show($"خطأ في بناء الآلة: {ex.Message}");
                currentPda = null;
                allPdaStates.Clear();
                pdaDiagramPanel.Invalidate();
            }
        }

        private void TestPdaButton_Click(object sender, EventArgs e, string input)
        {
            if (currentPda == null) { MessageBox.Show("يرجى بناء الآلة أولاً."); return; }

            pdaTraceLog.Items.Clear();
            pdaTraceLog.Items.Add("بدء المحاكاة...");

            var result = PdaLogic.SimulatePda(currentPda, input, rbDeterministic.Checked);

            foreach (var step in result.Trace) pdaTraceLog.Items.Add(step);

            if (result.IsDeterministicViolation)
            {
                pdaResultLabel.Text = "انتهاك المحدودية!";
                pdaResultLabel.ForeColor = Color.DarkOrange;
            }
            else
            {
                pdaResultLabel.Text = result.IsAccepted ? "مقبولة" : "مرفوضة";
                pdaResultLabel.ForeColor = result.IsAccepted ? Color.Green : Color.Red;
            }
        }

        private void PdaDiagramPanel_Paint(object sender, PaintEventArgs e)
        {
            if (currentPda != null && allPdaStates.Any())
            {
                StateDiagramPainter.DrawPushdownAutomaton(e.Graphics, allPdaStates, currentPda, rbDeterministic.Checked, pdaDiagramPanel);
            }
        }

        // --- TM Handlers ---
        private void BuildTmButton_Click(object sender, EventArgs e)
        {
            try
            {
                currentTm = TmLogic.ParseTm(tmTransitionsInput.Text);
                UpdateTmTransitionTable(currentTm);

                var allStateIds = currentTm.Transitions.Keys.Select(k => k.StateId)
                   .Union(currentTm.Transitions.Values.Select(t => t.NextStateId))
                   .Union(new[] { currentTm.AcceptStateId, currentTm.RejectStateId })
                   .Where(id => id != -1)
                   .Distinct();

                allTmStates = allStateIds.Select(id => new VisualState { Id = id, IsAcceptState = id == currentTm.AcceptStateId, IsRejectState = id == currentTm.RejectStateId }).ToList();

                StateDiagramPainter.PositionStates(allTmStates, tmDiagramPanel);
                tmDiagramPanel.Invalidate();

                tmResultLabel.Text = "الحالة: الآلة جاهزة";
                tmResultLabel.ForeColor = Color.Black;
            }
            catch (ParsingException ex)
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

            tmTape = new Dictionary<int, char>();
            for (int i = 0; i < input.Length; i++) tmTape[i] = input[i];

            tmHeadPosition = 0;
            tmCurrentState = currentTm.StartStateId;
            tmResultLabel.Text = "يعمل...";
            tmResultLabel.ForeColor = Color.Blue;
            tmTapeVisualizer.Invalidate();

            for (int step = 0; step < 2000; step++) // Max steps
            {
                if (!TmLogic.RunTmStep(currentTm, ref tmCurrentState, ref tmTape, ref tmHeadPosition))
                {
                    if (tmCurrentState == currentTm.AcceptStateId) { tmResultLabel.Text = "مقبولة"; tmResultLabel.ForeColor = Color.Green; }
                    else { tmResultLabel.Text = "مرفوضة"; tmResultLabel.ForeColor = Color.Red; }
                    tmTapeVisualizer.Invalidate();
                    return;
                }

                tmTapeVisualizer.Invalidate();
                await Task.Delay(100);
            }
            tmResultLabel.Text = "تجاوز حد الخطوات";
            tmResultLabel.ForeColor = Color.DarkOrange;
        }

        private void TmDiagramPanel_Paint(object sender, PaintEventArgs e)
        {
            if (currentTm != null && allTmStates.Any())
            {
                StateDiagramPainter.DrawTuringMachine(e.Graphics, allTmStates, currentTm, tmDiagramPanel);
            }
        }

        private void TmTapeVisualizer_Paint(object sender, PaintEventArgs e)
        {
            if (currentTm != null)
            {
                TapeVisualizerPainter.DrawTape(e.Graphics, tmTapeVisualizer, tmTape, tmHeadPosition);
            }
        }

        #endregion

        #region UI Update Helpers

        private void UpdateFaTransitionTable(State dfaStart, List<State> allStates, IEnumerable<char> alphabet)
        {
            faTransitionTable.Rows.Clear();
            faTransitionTable.Columns.Clear();
            faTransitionTable.Columns.Add("State", "الحالة");
            foreach (char s in alphabet) faTransitionTable.Columns.Add(s.ToString(), $"'{s}'");

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
                    if (state.Transitions.ContainsKey(s)) row.Add($"q{state.Transitions[s][0].Id}");
                    else row.Add("—");
                }
                faTransitionTable.Rows.Add(row.ToArray());
            }
        }

        private void UpdateNfaTransitionTable(State startState, List<State> allStates)
        {
            faTransitionTable.Rows.Clear();
            faTransitionTable.Columns.Clear();

            var symbols = allStates.SelectMany(s => s.Transitions.Keys).Where(c => c != '\0').Distinct().OrderBy(c => c);

            faTransitionTable.Columns.Add("State", "الحالة");
            foreach (char s in symbols) faTransitionTable.Columns.Add(s.ToString(), $"'{s}'");
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
                        row.Add("{" + string.Join(",", state.Transitions[s].Select(t => $"q{t.Id}")) + "}");
                    else
                        row.Add("—");
                }

                if (state.Transitions.ContainsKey('\0'))
                    row.Add("{" + string.Join(",", state.Transitions['\0'].Select(t => $"q{t.Id}")) + "}");
                else
                    row.Add("—");

                faTransitionTable.Rows.Add(row.ToArray());
            }
        }

        private void UpdatePdaTransitionTable(PushdownAutomaton pda)
        {
            pdaTransitionTable.Rows.Clear();
            pdaTransitionTable.Columns.Clear();
            pdaTransitionTable.Columns.Add("FromState", "الحالة الحالية");
            pdaTransitionTable.Columns.Add("Input", "الإدخال");
            pdaTransitionTable.Columns.Add("Pop", "إزالة");
            pdaTransitionTable.Columns.Add("ToState", "الحالة التالية");
            pdaTransitionTable.Columns.Add("Push", "إضافة");

            foreach (var stateTransitions in pda.Transitions.OrderBy(kvp => kvp.Key))
            {
                foreach (var trans in stateTransitions.Value)
                {
                    pdaTransitionTable.Rows.Add(
                        $"q{trans.FromStateId}",
                        trans.InputSymbol == '\0' ? "ε" : trans.InputSymbol.ToString(),
                        trans.StackPopSymbol == '\0' ? "ε" : trans.StackPopSymbol.ToString(),
                        $"q{trans.NextStateId}",
                        string.IsNullOrEmpty(trans.StackPushSymbols) ? "ε" : trans.StackPushSymbols
                    );
                }
            }
            pdaTransitionTable.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

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
            tmTransitionTable.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }
        #endregion
    }
}