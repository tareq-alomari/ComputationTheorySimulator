using ComputationTheorySimulator.BLL;
using ComputationTheorySimulator.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ComputationTheorySimulator.Presentation
{
    public partial class MainSimulator : Form
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

        #region Logic Handlers
        private readonly FaLogic _faLogic;
        private readonly PdaLogic _pdaLogic;
        private readonly TmLogic _tmLogic;
        #endregion

        #region Machine Instances & State
        // FA
        private State startStateDfa;
        private List<State> allDfaStates = new List<State>();
        // PDA
        private PushdownAutomaton currentPda;
        private List<VisualState> allPdaStates = new List<VisualState>();
        // TM
        private TuringMachine currentTm;
        private List<VisualState> allTmStates = new List<VisualState>();
        private Dictionary<int, char> tmTape = new Dictionary<int, char>();
        private int tmHeadPosition = 0;
        private int tmCurrentState = 0;
        #endregion

        public MainSimulator()
        {
            _faLogic = new FaLogic();
            _pdaLogic = new PdaLogic();
            _tmLogic = new TmLogic();

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            SetupUI();
            UpdateActivePanel();
            this.Text = "محاكي النظرية الاحتسابية - بنية طبقية";
            this.MinimumSize = new Size(1200, 800);
            this.Size = new Size(1300, 850);
            this.Icon = SystemIcons.Information;
            this.FormClosed += (s, e) => Application.Exit();
        }

        #region UI Setup
        private void SetupUI()
        {
            this.BackColor = Color.FromArgb(45, 45, 60);
            this.ForeColor = Color.WhiteSmoke;

            toolTip = new ToolTip();
            var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            var selectionPanel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(5), WrapContents = false };

            var gbMachineType = new GroupBox { Text = "نوع الآلة", AutoSize = true, ForeColor = Color.FromArgb(100, 180, 255), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            var machineTypeLayout = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true };
            rbFiniteAutomata = new RadioButton { Text = "آلات منتهية (FA)", Checked = true, AutoSize = true };
            rbPushdownAutomata = new RadioButton { Text = "آلات الدفع للأسفل (PDA)", AutoSize = true };
            rbTuringMachine = new RadioButton { Text = "آلة تورنغ (TM)", AutoSize = true };

            rbFiniteAutomata.CheckedChanged += OnMachineTypeChanged;
            rbPushdownAutomata.CheckedChanged += OnMachineTypeChanged;
            rbTuringMachine.CheckedChanged += OnMachineTypeChanged;

            machineTypeLayout.Controls.AddRange(new Control[] { rbFiniteAutomata, rbPushdownAutomata, rbTuringMachine });
            gbMachineType.Controls.Add(machineTypeLayout);

            var gbSubType = new GroupBox { Text = "النوع الفرعي", AutoSize = true, ForeColor = Color.FromArgb(100, 180, 255), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
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
            inputPanel.Controls.Add(new Label { Text = "أدخل التعبير النمطي:", AutoSize = true, Margin = new Padding(0, 6, 0, 0), Font = new Font("Segoe UI", 10, FontStyle.Bold) });
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
            var testGroup = new GroupBox { Text = "اختبار السلسلة", Dock = DockStyle.Fill, Padding = new Padding(10), Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(100, 180, 255) };
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
            var definitionGroup = new GroupBox { Text = "تعريف انتقالات الآلة (PDA)", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(100, 180, 255) };
            pdaTransitionsInput = new TextBox { Dock = DockStyle.Fill, Multiline = true, Font = new Font("Consolas", 11), ScrollBars = ScrollBars.Vertical, AcceptsReturn = true };
            toolTip.SetToolTip(pdaTransitionsInput, "الصيغة: q_start,input,pop;q_end,push\nمثال: q0,a,Z;q1,AZ\nاستخدم 'e' لـ ε.");
            definitionGroup.Controls.Add(pdaTransitionsInput);

            var diagramGroup = new GroupBox { Text = "الرسم البياني للحالات", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(100, 180, 255) };
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

            var testGroup = new GroupBox { Text = "اختبار السلسلة", AutoSize = true, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(100, 180, 255) };
            var testLayout = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Dock = DockStyle.Fill, AutoSize = true };
            var pdaTestStringInput = new TextBox { Width = 200, Font = new Font("Consolas", 11) };
            var testPdaButton = new Button { Text = "اختبر", Font = new Font("Segoe UI", 9, FontStyle.Bold), BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Height = 30 };
            testPdaButton.Click += (s, e) => TestPdaButton_Click(s, e, pdaTestStringInput.Text);
            pdaResultLabel = new Label { Text = "النتيجة:", Font = new Font("Segoe UI", 12, FontStyle.Bold), AutoSize = true, Margin = new Padding(10, 6, 0, 0) };
            testLayout.Controls.AddRange(new Control[] { pdaTestStringInput, testPdaButton, pdaResultLabel });
            testGroup.Controls.Add(testLayout);

            var traceGroup = new GroupBox { Text = "سجل التتبع", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(100, 180, 255) };
            pdaTraceLog = new ListBox { Dock = DockStyle.Fill, Font = new Font("Consolas", 10), HorizontalScrollbar = true, BackColor = Color.LightGray };
            traceGroup.Controls.Add(pdaTraceLog);

            var tableGroup = new GroupBox { Text = "جدول الانتقالات", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(100, 180, 255) };
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
            var definitionGroup = new GroupBox { Text = "تعريف انتقالات آلة تورنغ (TM)", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(100, 180, 255) };
            tmTransitionsInput = new TextBox { Dock = DockStyle.Fill, Multiline = true, Font = new Font("Consolas", 11), ScrollBars = ScrollBars.Vertical, AcceptsReturn = true };
            toolTip.SetToolTip(tmTransitionsInput, "الصيغة: q_start,read;q_end,write,move(L/R/S)\nمثال: q0,a;q1,b,R\nاستخدم '_' للرمز الفارغ.");
            definitionGroup.Controls.Add(tmTransitionsInput);

            var diagramGroup = new GroupBox { Text = "الرسم البياني للحالات", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(100, 180, 255) };
            tmDiagramPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, AutoScroll = true };
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

            var tapeGroup = new GroupBox { Text = "الشريط (Tape)", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(100, 180, 255) };
            tmTapeVisualizer = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.Fixed3D };
            tmTapeVisualizer.Paint += TmTapeVisualizer_Paint;
            tapeGroup.Controls.Add(tmTapeVisualizer);

            var tableGroup = new GroupBox { Text = "جدول الانتقالات", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(100, 180, 255) };
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

        #region Event Handlers & Logic Calls

        private void OnMachineTypeChanged(object sender, EventArgs e) => UpdateActivePanel();

        private void BuildFaButton_Click(object sender, EventArgs e)
        {
            try
            {
                State.ResetIdCounter();
                string patternWithConcat = _faLogic.AddConcatOperator(regexInput.Text);
                var postfix = _faLogic.InfixToPostfix(patternWithConcat);
                var nfa = _faLogic.PostfixToNfa(postfix);
                var alphabet = patternWithConcat.Where(char.IsLetterOrDigit).Distinct();

                if (rbDeterministic.Checked)
                {
                    var dfaInfo = _faLogic.NfaToDfa(nfa, alphabet);
                    startStateDfa = dfaInfo.Item1;
                    allDfaStates = dfaInfo.Item2;
                    UpdateFaTransitionTable(startStateDfa, allDfaStates, alphabet);
                }
                else
                {
                    startStateDfa = nfa.Start;
                    allDfaStates = _faLogic.GetAllStatesFromNfa(nfa.Start);
                    UpdateNfaTransitionTable(startStateDfa, allDfaStates);
                }
                PositionFaStates(allDfaStates, faDiagramPanel);
                faDiagramPanel.Invalidate();
                faResultLabel.Text = "الآلة جاهزة";
                faResultLabel.ForeColor = Color.Green;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ في بناء FA", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FaTestButton_Click(object sender, EventArgs e)
        {
            if (startStateDfa == null) return;
            bool accepted;
            if (rbDeterministic.Checked)
            {
                accepted = _faLogic.TestDfaString(startStateDfa, faTestStringInput.Text);
            }
            else
            {
                accepted = _faLogic.TestNfaString(startStateDfa, faTestStringInput.Text);
            }
            faResultLabel.Text = accepted ? "مقبولة" : "مرفوضة";
            faResultLabel.ForeColor = accepted ? Color.Green : Color.Red;
        }

        private void BuildPdaButton_Click(object sender, EventArgs e)
        {
            try
            {
                currentPda = _pdaLogic.ParsePdaDefinition(pdaTransitionsInput.Text, rbDeterministic.Checked);
                var allStateIds = currentPda.Transitions.Keys
                    .Union(currentPda.Transitions.Values.SelectMany(t => t).Select(tr => tr.NextStateId))
                    .Distinct().OrderBy(id => id).ToList();
                allPdaStates = allStateIds.Select(id => new VisualState
                {
                    Id = id,
                    IsAcceptState = currentPda.AcceptStates.Contains(id)
                }).ToList();
                PositionVisualStates(allPdaStates, pdaDiagramPanel);
                UpdatePdaTransitionTable(currentPda, rbDeterministic.Checked);
                pdaDiagramPanel.Invalidate();
                pdaResultLabel.Text = "الآلة جاهزة للاختبار";
                pdaResultLabel.ForeColor = Color.Green;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في بناء PDA: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TestPdaButton_Click(object sender, EventArgs e, string input)
        {
            if (currentPda == null) return;
            pdaTraceLog.Items.Clear();
            var result = _pdaLogic.Simulate(currentPda, input, rbDeterministic.Checked);
            result.Trace.ForEach(step => pdaTraceLog.Items.Add(step));

            if (result.IsDeterministicViolation)
            {
                pdaResultLabel.Text = "انتهاك المحدودية!";
                pdaResultLabel.ForeColor = Color.DarkOrange;
            }
            else
            {
                pdaResultLabel.Text = result.IsAccepted ? "مقبولة" : "مرفوضة";
                pdaResultLabel.ForeColor = result.IsAccepted ? Color.Green : Color.Red;
                pdaTraceLog.Items.Add(result.IsAccepted ? "== السلسلة مقبولة ==" : "== السلسلة مرفوضة ==");
            }
        }

        private void BuildTmButton_Click(object sender, EventArgs e)
        {
            try
            {
                currentTm = _tmLogic.ParseTm(tmTransitionsInput.Text);
                UpdateTmTransitionTable(currentTm);
                var allStateIds = currentTm.Transitions.Keys.Select(k => k.StateId)
                    .Union(currentTm.Transitions.Values.Select(t => t.NextStateId)).Distinct();
                allTmStates = allStateIds.Select(id => new VisualState
                {
                    Id = id,
                    IsAcceptState = id == currentTm.AcceptStateId,
                    IsRejectState = id == currentTm.RejectStateId
                }).ToList();

                PositionVisualStates(allTmStates, tmDiagramPanel);

                if (allTmStates.Any())
                {
                    int stateRadius = 25;
                    int margin = 100;
                    int maxX = allTmStates.Max(s => s.Position.X) + stateRadius + margin;
                    int maxY = allTmStates.Max(s => s.Position.Y) + stateRadius + margin;
                    tmDiagramPanel.AutoScrollMinSize = new Size(maxX, maxY);
                }
                else
                {
                    tmDiagramPanel.AutoScrollMinSize = new Size(0, 0);
                }

                tmDiagramPanel.Invalidate();
                tmResultLabel.Text = "الحالة: الآلة جاهزة";
                tmResultLabel.ForeColor = Color.FromArgb(100, 180, 255);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في بناء TM: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            for (int step = 0; step < 2000; step++)
            {
                if (tmCurrentState == currentTm.AcceptStateId) { tmResultLabel.Text = "مقبولة"; tmResultLabel.ForeColor = Color.Green; return; }
                if (tmCurrentState == currentTm.RejectStateId) { tmResultLabel.Text = "مرفوضة"; tmResultLabel.ForeColor = Color.Red; return; }

                char readSymbol = tmTape.ContainsKey(tmHeadPosition) ? tmTape[tmHeadPosition] : '_';
                var key = new TMTransitionKey(tmCurrentState, readSymbol);

                if (!currentTm.Transitions.ContainsKey(key)) { tmResultLabel.Text = "مرفوضة (توقفت)"; tmResultLabel.ForeColor = Color.Red; return; }

                var trans = currentTm.Transitions[key];
                tmTape[tmHeadPosition] = trans.WriteSymbol;
                tmCurrentState = trans.NextStateId;

                switch (trans.MoveDirection)
                {
                    case TapeMove.R:
                        tmHeadPosition++;
                        break;
                    case TapeMove.L:
                        tmHeadPosition--;
                        break;
                    case TapeMove.S:
                        break;
                }

                tmTapeVisualizer.Invalidate();
                await Task.Delay(100);
            }
            tmResultLabel.Text = "تجاوز حد الخطوات";
            tmResultLabel.ForeColor = Color.DarkOrange;
        }

        #endregion

        #region Paint Handlers & UI Update

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

            var groupedTransitions = new Dictionary<string, List<char>>();
            foreach (var state in allDfaStates)
            {
                foreach (var trans in state.Transitions)
                {
                    string key = $"{state.Id},{trans.Value.First().Id}";
                    if (!groupedTransitions.ContainsKey(key))
                        groupedTransitions[key] = new List<char>();
                    groupedTransitions[key].Add(trans.Key);
                }
            }

            foreach (var group in groupedTransitions)
            {
                var ids = group.Key.Split(',');
                var fromState = allDfaStates.First(s => s.Id == int.Parse(ids[0]));
                var toState = allDfaStates.First(s => s.Id == int.Parse(ids[1]));
                string label = string.Join(",", group.Value.OrderBy(c => c));

                if (fromState.Id == toState.Id)
                {
                    int r = 18, ls = 30;
                    var lr = new Rectangle(fromState.Position.X, fromState.Position.Y - r, ls, r * 2);
                    e.Graphics.DrawArc(transitionPen, lr, 90, 270);
                    e.Graphics.DrawString(label, font, textBrush, lr.Right + 5, fromState.Position.Y, new StringFormat { LineAlignment = StringAlignment.Center });
                }
                else
                {
                    e.Graphics.DrawLine(transitionPen, fromState.Position, toState.Position);
                    Point mp = new Point((fromState.Position.X + toState.Position.X) / 2, (fromState.Position.Y + toState.Position.Y) / 2 - 15);
                    e.Graphics.DrawString(label, font, textBrush, mp);
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
            font.Dispose(); stateBrush.Dispose(); statePen.Dispose(); acceptStatePen.Dispose(); transitionPen.Dispose();
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

            var groupedTransitions = currentPda.Transitions.Values.SelectMany(x => x)
                .GroupBy(t => new { From = t.FromStateId, To = t.NextStateId })
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var group in groupedTransitions)
            {
                var fromState = allPdaStates.FirstOrDefault(s => s.Id == group.Key.From);
                var toState = allPdaStates.FirstOrDefault(s => s.Id == group.Key.To);
                if (fromState == null || toState == null) continue;

                string label = string.Join("\n", group.Value.Select(t => $"{(t.InputSymbol == '\0' ? 'ε' : t.InputSymbol)},{(t.StackPopSymbol == '\0' ? 'ε' : t.StackPopSymbol)}/{(string.IsNullOrEmpty(t.StackPushSymbols) ? "ε" : t.StackPushSymbols)}"));

                if (fromState.Id == toState.Id)
                {
                    int r = 18, ls = 30;
                    var lr = new Rectangle(fromState.Position.X, fromState.Position.Y - r, ls, r * 2);
                    e.Graphics.DrawArc(transitionPen, lr, 90, 270);
                    e.Graphics.DrawString(label, font, textBrush, lr.Right, fromState.Position.Y, sfCenter);
                }
                else
                {
                    e.Graphics.DrawLine(transitionPen, fromState.Position, toState.Position);
                    Point midPoint = new Point((fromState.Position.X + toState.Position.X) / 2, (fromState.Position.Y + toState.Position.Y) / 2 - 15);
                    e.Graphics.DrawString(label, font, textBrush, midPoint, sfCenter);
                }
            }

            foreach (var state in allPdaStates)
            {
                int r = 18;
                var rect = new Rectangle(state.Position.X - r, state.Position.Y - r, 2 * r, 2 * r);
                e.Graphics.FillEllipse(stateBrush, rect);
                e.Graphics.DrawEllipse(statePen, rect);

                if (state.IsAcceptState)
                {
                    e.Graphics.DrawEllipse(acceptStatePen, new Rectangle(rect.X - 4, rect.Y - 4, rect.Width + 8, rect.Height + 8));
                }

                if (state.Id == currentPda.StartStateId)
                {
                    e.Graphics.DrawString("→", new Font("Arial", 16, FontStyle.Bold), textBrush, state.Position.X - 45, state.Position.Y - 15);
                }
                e.Graphics.DrawString($"q{state.Id}", font, textBrush, rect, sfCenter);
            }
            font.Dispose(); stateBrush.Dispose(); statePen.Dispose(); acceptStatePen.Dispose(); transitionPen.Dispose(); sfCenter.Dispose();
        }

        private void TmDiagramPanel_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.TranslateTransform(tmDiagramPanel.AutoScrollPosition.X, tmDiagramPanel.AutoScrollPosition.Y);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(Color.White);

            if (currentTm == null || !allTmStates.Any()) return;

            var font = new Font("Segoe UI", 9, FontStyle.Bold);
            var stateBrush = new SolidBrush(Color.FromArgb(230, 247, 255));
            var statePen = new Pen(Color.FromArgb(0, 123, 255), 2);
            var acceptStatePen = new Pen(Color.FromArgb(40, 167, 69), 2.5f);
            var rejectStatePen = new Pen(Color.FromArgb(220, 53, 69), 2.5f);
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

                if (fromState.Id == toState.Id)
                {
                    int r = 18, ls = 30;
                    var lr = new Rectangle(fromState.Position.X, fromState.Position.Y - r, ls, r * 2);
                    e.Graphics.DrawArc(transitionPen, lr, 90, 270);
                    e.Graphics.DrawString(label, font, textBrush, lr.Right, fromState.Position.Y, sfCenter);
                }
                else
                {
                    e.Graphics.DrawLine(transitionPen, fromState.Position, toState.Position);
                    Point midPoint = new Point((fromState.Position.X + toState.Position.X) / 2, (fromState.Position.Y + toState.Position.Y) / 2 - 15);
                    e.Graphics.DrawString(label, font, textBrush, midPoint, sfCenter);
                }
            }

            foreach (var state in allTmStates)
            {
                int r = 25;
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
            font.Dispose(); stateBrush.Dispose(); statePen.Dispose(); acceptStatePen.Dispose(); rejectStatePen.Dispose(); transitionPen.Dispose(); sfCenter.Dispose();
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

        private void UpdateFaTransitionTable(State dfaStart, List<State> allStates, IEnumerable<char> alphabet)
        {
            faTransitionTable.Rows.Clear();
            faTransitionTable.Columns.Clear();
            faTransitionTable.Columns.Add("State", "الحالة");
            foreach (char s in alphabet.OrderBy(c => c))
                faTransitionTable.Columns.Add(s.ToString(), $"'{s}'");

            foreach (var state in allStates.OrderBy(s => s.Id))
            {
                var row = new List<string>();
                string n = "";
                if (state == dfaStart) n += "→";
                n += $"q{state.Id}";
                if (state.IsAcceptState) n += "*";
                row.Add(n);
                foreach (char s in alphabet.OrderBy(c => c))
                {
                    if (state.Transitions.ContainsKey(s))
                        row.Add($"q{state.Transitions[s][0].Id}");
                    else
                        row.Add("—");
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
            foreach (char s in symbols)
                faTransitionTable.Columns.Add(s.ToString(), $"'{s}'");
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

        private void UpdatePdaTransitionTable(PushdownAutomaton pda, bool isDeterministic)
        {
            pdaTransitionTable.Rows.Clear();
            pdaTransitionTable.Columns.Clear();
            pdaTransitionTable.Columns.Add("From", "من");
            pdaTransitionTable.Columns.Add("Input", "إدخال");
            pdaTransitionTable.Columns.Add("Pop", "إزالة");
            pdaTransitionTable.Columns.Add("To", "إلى");
            pdaTransitionTable.Columns.Add("Push", "إضافة");

            foreach (var fromState in pda.Transitions.Keys.OrderBy(k => k))
            {
                foreach (var trans in pda.Transitions[fromState].OrderBy(t => t.InputSymbol).ThenBy(t => t.StackPopSymbol))
                {
                    pdaTransitionTable.Rows.Add(
                        $"q{fromState}",
                        trans.InputSymbol == '\0' ? "ε" : trans.InputSymbol.ToString(),
                        trans.StackPopSymbol == '\0' ? "ε" : trans.StackPopSymbol.ToString(),
                        $"q{trans.NextStateId}",
                        string.IsNullOrEmpty(trans.StackPushSymbols) ? "ε" : trans.StackPushSymbols
                    );
                }
            }
            pdaTransitionTable.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
        }

        private void UpdateTmTransitionTable(TuringMachine tm)
        {
            tmTransitionTable.Rows.Clear();
            tmTransitionTable.Columns.Clear();
            tmTransitionTable.Columns.AddRange(new DataGridViewTextBoxColumn[] {
                new DataGridViewTextBoxColumn { HeaderText = "δ(الحالة, القراءة)", FillWeight = 40, Name = "Current" },
                new DataGridViewTextBoxColumn { HeaderText = "(الحالة الجديدة, الكتابة, الحركة)", FillWeight = 60, Name = "Next" }
            });

            foreach (var t in tm.Transitions.OrderBy(t => t.Key.StateId).ThenBy(t => t.Key.ReadSymbol))
            {
                tmTransitionTable.Rows.Add($"δ(q{t.Key.StateId}, {t.Key.ReadSymbol})", $"(q{t.Value.NextStateId}, {t.Value.WriteSymbol}, {t.Value.MoveDirection})");
            }
            tmTransitionTable.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void PositionFaStates(List<State> allStates, Panel diagramPanel)
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

        private void PositionVisualStates(List<VisualState> allStates, Panel diagramPanel)
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

        #endregion
    }
}