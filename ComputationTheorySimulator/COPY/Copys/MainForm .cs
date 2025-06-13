using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Threading;

namespace ComputationTheorySimulator
{
    public partial class MainForm : Form
    {
        private TabControl mainTabControl;

        // Finite Automata UI
        private TextBox regexInput;
        private Button buildFaButton;
        private Panel faDiagramPanel;
        private DataGridView faTransitionTable;
        private TextBox faTestStringInput;
        private Button faTestButton;
        private Label faResultLabel;
        private Automata.Dfa dfaInstance;

        // Pushdown Automata UI
        private TextBox cfgInput;
        private Button buildPdaButton;
        private Panel pdaDiagramPanel;
        private DataGridView pdaTransitionTable;
        private TextBox pdaTestStringInput;
        private Button pdaTestButton;
        private Label pdaResultLabel;
        private Automata.Pda pdaInstance;

        // Turing Machine UI
        private TextBox langDescInput;
        private Button buildTmButton;
        private Panel tmDiagramPanel;
        private DataGridView tmTransitionTable;
        private TextBox tmTestStringInput;
        private Button tmTestButton;
        private Label tmResultLabel;
        private DataGridView tmTapeDisplay;
        private Automata.TuringMachine tmInstance;

        public MainForm()
        {
            InitializeArabicUI();
            this.Text = "محاكي آلات النظرية الاحتسابية";
            this.Size = new Size(1200, 800);
            this.MinimumSize = new Size(1000, 700);
        }

        private void InitializeArabicUI()
        {
            this.RightToLeft = RightToLeft.No;
            this.RightToLeftLayout = false;

            mainTabControl = new TabControl { Dock = DockStyle.Fill };
            this.Controls.Add(mainTabControl);

            mainTabControl.TabPages.Add(CreateFaTabPage());
            mainTabControl.TabPages.Add(CreatePdaTabPage());
            mainTabControl.TabPages.Add(CreateTmTabPage());
        }

        private TabPage CreateFaTabPage()
        {
            var faTabPage = new TabPage("الآلات المحدودة (Finite Automata)");
            var faMainPanel = CreateMainViewPanel();

            var leftPanel = CreateLeftPanel();
            var inputPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(5),
                WrapContents = false,
                AutoSize = true
            };

            regexInput = new TextBox
            {
                Width = 300,
                Font = new Font("Tahoma", 10),
                Text = "(a|b)*abb",
                RightToLeft = RightToLeft.No
            };

            buildFaButton = new Button
            {
                Text = "بناء الآلة",
                Font = new Font("Tahoma", 10, FontStyle.Bold)
            };
            buildFaButton.Click += BuildFaButton_Click;

            inputPanel.Controls.Add(buildFaButton);
            inputPanel.Controls.Add(regexInput);
            inputPanel.Controls.Add(new Label
            {
                Text = "أدخل التعبير النمطي:",
                AutoSize = true,
                Margin = new Padding(0, 6, 0, 0),
                Font = new Font("Tahoma", 10)
            });

            faDiagramPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            faDiagramPanel.Paint += (s, e) => DiagramPainter.DrawAutomaton(e.Graphics, dfaInstance);

            leftPanel.Controls.Add(inputPanel, 0, 0);
            leftPanel.Controls.Add(faDiagramPanel, 0, 1);

            var rightPanel = CreateRightPanel();
            faTransitionTable = CreateDataGridView();
            var testPanel = CreateTestPanel(out faTestStringInput, out faTestButton, out faResultLabel);
            faTestButton.Click += FaTestButton_Click;

            rightPanel.Controls.Add(faTransitionTable, 0, 0);
            rightPanel.Controls.Add(testPanel, 0, 1);

            faMainPanel.Controls.Add(leftPanel, 0, 0);
            faMainPanel.Controls.Add(rightPanel, 1, 0);
            faTabPage.Controls.Add(faMainPanel);
            return faTabPage;
        }

        private TabPage CreatePdaTabPage()
        {
            var pdaTabPage = new TabPage("آلات المكدس (Pushdown Automata)");
            var pdaMainPanel = CreateMainViewPanel();

            var leftPanel = CreateLeftPanel();
            var inputPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(5)
            };

            cfgInput = new TextBox
            {
                Multiline = true,
                Height = 100,
                Width = 400,
                Font = new Font("Consolas", 10),
                ScrollBars = ScrollBars.Vertical,
                Text = "S -> aSb\r\nS -> ε",
                RightToLeft = RightToLeft.No
            };

            buildPdaButton = new Button
            {
                Text = "بناء الآلة",
                AutoSize = true,
                Font = new Font("Tahoma", 10, FontStyle.Bold)
            };
            buildPdaButton.Click += BuildPdaButton_Click;

            inputPanel.Controls.Add(new Label
            {
                Text = "أدخل القواعد الخالية من السياق (CFG):",
                AutoSize = true,
                Font = new Font("Tahoma", 10)
            });
            inputPanel.Controls.Add(cfgInput);
            inputPanel.Controls.Add(buildPdaButton);

            pdaDiagramPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            pdaDiagramPanel.Paint += (s, e) => DiagramPainter.DrawAutomaton(e.Graphics, pdaInstance);

            leftPanel.Controls.Add(inputPanel, 0, 0);
            leftPanel.Controls.Add(pdaDiagramPanel, 0, 1);
            leftPanel.RowStyles[0].SizeType = SizeType.Absolute;
            leftPanel.RowStyles[0].Height = 150;

            var rightPanel = CreateRightPanel();
            pdaTransitionTable = CreateDataGridView();
            var testPanel = CreateTestPanel(out pdaTestStringInput, out pdaTestButton, out pdaResultLabel);
            pdaTestButton.Click += PdaTestButton_Click;

            rightPanel.Controls.Add(pdaTransitionTable, 0, 0);
            rightPanel.Controls.Add(testPanel, 0, 1);

            pdaMainPanel.Controls.Add(leftPanel, 0, 0);
            pdaMainPanel.Controls.Add(rightPanel, 1, 0);
            pdaTabPage.Controls.Add(pdaMainPanel);
            return pdaTabPage;
        }

        private TabPage CreateTmTabPage()
        {
            var tmTabPage = new TabPage("آلات تورنغ (Turing Machine)");
            var tmMainPanel = CreateMainViewPanel();

            var leftPanel = CreateLeftPanel();
            var inputPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(5)
            };

            langDescInput = new TextBox
            {
                Width = 400,
                Font = new Font("Consolas", 10),
                Text = "a^n b^n | n >= 1",
                RightToLeft = RightToLeft.No
            };

            buildTmButton = new Button
            {
                Text = "بناء الآلة",
                AutoSize = true,
                Font = new Font("Tahoma", 10, FontStyle.Bold)
            };
            buildTmButton.Click += BuildTmButton_Click;

            inputPanel.Controls.Add(new Label
            {
                Text = "أدخل وصف اللغة (أمثلة مدعومة):\r\na^n b^n | n >= 1\r\nw#w | w in {0,1}*",
                AutoSize = true,
                Height = 60,
                Font = new Font("Tahoma", 10)
            });
            inputPanel.Controls.Add(langDescInput);
            inputPanel.Controls.Add(buildTmButton);

            tmDiagramPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            tmDiagramPanel.Paint += (s, e) => DiagramPainter.DrawAutomaton(e.Graphics, tmInstance);

            leftPanel.Controls.Add(inputPanel, 0, 0);
            leftPanel.Controls.Add(tmDiagramPanel, 0, 1);
            leftPanel.RowStyles[0].SizeType = SizeType.Absolute;
            leftPanel.RowStyles[0].Height = 110;

            var rightPanel = CreateRightPanel();
            tmTransitionTable = CreateDataGridView();
            var testPanel = CreateTestPanel(out tmTestStringInput, out tmTestButton, out tmResultLabel);
            tmTestButton.Click += TmTestButton_Click;
            tmTapeDisplay = new DataGridView
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                AllowUserToResizeRows = false,
                AllowUserToResizeColumns = false,
                RightToLeft = RightToLeft.No
            };
            testPanel.Controls.Add(tmTapeDisplay);

            rightPanel.Controls.Add(tmTransitionTable, 0, 0);
            rightPanel.Controls.Add(testPanel, 0, 1);

            tmMainPanel.Controls.Add(leftPanel, 0, 0);
            tmMainPanel.Controls.Add(rightPanel, 1, 0);
            tmTabPage.Controls.Add(tmMainPanel);
            return tmTabPage;
        }

        private TableLayoutPanel CreateMainViewPanel()
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            return panel;
        }

        private TableLayoutPanel CreateLeftPanel()
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            return panel;
        }

        private TableLayoutPanel CreateRightPanel()
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
            return panel;
        }

        private DataGridView CreateDataGridView()
        {
            return new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                AllowUserToResizeRows = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                BackgroundColor = Color.White,
                RightToLeft = RightToLeft.Yes,
                Font = new Font("Tahoma", 9)
            };
        }

        private Panel CreateTestPanel(out TextBox input, out Button button, out Label result)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            input = new TextBox
            {
                Dock = DockStyle.Top,
                Margin = new Padding(0, 10, 0, 10),
                Font = new Font("Consolas", 11),
                RightToLeft = RightToLeft.No
            };
            button = new Button
            {
                Text = "اختبار السلسلة",
                Dock = DockStyle.Top,
                Font = new Font("Tahoma", 10, FontStyle.Bold)
            };
            result = new Label
            {
                Text = "النتيجة: في انتظار الإدخال...",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Tahoma", 12, FontStyle.Bold),
                RightToLeft = RightToLeft.Yes
            };
            panel.Controls.Add(result);
            panel.Controls.Add(button);
            panel.Controls.Add(input);
            return panel;
        }

        private void BuildFaButton_Click(object sender, EventArgs e)
        {
            string pattern = regexInput.Text;
            if (string.IsNullOrWhiteSpace(pattern))
            {
                MessageBox.Show("الرجاء إدخال تعبير نمطي.", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            try
            {
                var nfa = RegexConverter.ToNfa(pattern);
                dfaInstance = NfaConverter.ToDfa(nfa);
                TableManager.UpdateFaTable(faTransitionTable, dfaInstance);
                dfaInstance.PositionStatesForDrawing(faDiagramPanel.Width, faDiagramPanel.Height);
                faDiagramPanel.Invalidate();
                faResultLabel.Text = "الآلة جاهزة للاختبار.";
                faResultLabel.ForeColor = Color.Black;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في بناء الآلة: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FaTestButton_Click(object sender, EventArgs e)
        {
            if (dfaInstance == null)
            {
                faResultLabel.Text = "يجب بناء الآلة أولاً";
                faResultLabel.ForeColor = Color.Red;
                return;
            }
            bool accepted = dfaInstance.Simulate(faTestStringInput.Text);
            faResultLabel.Text = accepted ? "النتيجة: مقبولة (Accepted)" : "النتيجة: مرفوضة (Rejected)";
            faResultLabel.ForeColor = accepted ? Color.Green : Color.Red;
        }

        private void BuildPdaButton_Click(object sender, EventArgs e)
        {
            try
            {
                var cfg = CfgParser.Parse(cfgInput.Text);
                pdaInstance = CfgConverter.ToPda(cfg);
                TableManager.UpdatePdaTable(pdaTransitionTable, pdaInstance);
                pdaInstance.PositionStatesForDrawing(pdaDiagramPanel.Width, pdaDiagramPanel.Height);
                pdaDiagramPanel.Invalidate();
                pdaResultLabel.Text = "الآلة جاهزة للاختبار.";
                pdaResultLabel.ForeColor = Color.Black;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في بناء الآلة: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void PdaTestButton_Click(object sender, EventArgs e)
        {
            if (pdaInstance == null)
            {
                pdaResultLabel.Text = "يجب بناء الآلة أولاً";
                pdaResultLabel.ForeColor = Color.Red;
                return;
            }
            var simulator = new PdaSimulator(pdaInstance, pdaTestStringInput.Text);
            pdaTestButton.Enabled = false;
            var result = await simulator.RunAsync((stepResult) =>
            {
                pdaResultLabel.Text = $"الحالة: {stepResult.State.Name}\nالمدخل المتبقي: '{stepResult.RemainingInput}'\nالمكدس: {stepResult.StackSnapshot}";
                pdaResultLabel.ForeColor = Color.Blue;
                pdaResultLabel.Update();
            });
            pdaTestButton.Enabled = true;

            pdaResultLabel.Text = result ? "النتيجة: مقبولة (Accepted)" : "النتيجة: مرفوضة (Rejected)";
            pdaResultLabel.ForeColor = result ? Color.Green : Color.Red;
        }

        private void BuildTmButton_Click(object sender, EventArgs e)
        {
            try
            {
                tmInstance = TmBuilder.BuildFromDescription(langDescInput.Text);
                TableManager.UpdateTmTable(tmTransitionTable, tmInstance);
                tmInstance.PositionStatesForDrawing(tmDiagramPanel.Width, tmDiagramPanel.Height);
                tmDiagramPanel.Invalidate();
                tmResultLabel.Text = "الآلة جاهزة للاختبار.";
                tmResultLabel.ForeColor = Color.Black;
            }
            catch (NotImplementedException ex)
            {
                MessageBox.Show(ex.Message, "غير مدعوم", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في بناء الآلة: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void TmTestButton_Click(object sender, EventArgs e)
        {
            if (tmInstance == null)
            {
                tmResultLabel.Text = "يجب بناء الآلة أولاً";
                tmResultLabel.ForeColor = Color.Red;
                return;
            }
            var simulator = new TmSimulator(tmInstance, tmTestStringInput.Text);
            tmTestButton.Enabled = false;
            var result = await simulator.RunAsync((tape, head, state) =>
            {
                TableManager.UpdateTmTapeDisplay(tmTapeDisplay, tape, head);
                tmResultLabel.Text = $"الحالة الحالية: {state.Name}";
                tmResultLabel.ForeColor = Color.Blue;
                tmResultLabel.Update();
            });
            tmTestButton.Enabled = true;

            tmResultLabel.Text = result ? "النتيجة: مقبولة (Accepted)" : "النتيجة: مرفوضة (Rejected)";
            tmResultLabel.ForeColor = result ? Color.Green : Color.Red;
        }
    }

    public static class Automata
    {
        public abstract class BaseAutomaton
        {
            public State StartState { get; set; }
            public HashSet<State> States { get; } = new HashSet<State>();
            public HashSet<char> Alphabet { get; } = new HashSet<char>();

            public void PositionStatesForDrawing(int width, int height)
            {
                int radius = Math.Min(width, height) / 2 - 50;
                int centerX = width / 2;
                int centerY = height / 2;
                var stateList = States.ToList();
                double angleStep = 2 * Math.PI / stateList.Count;
                for (int i = 0; i < stateList.Count; i++)
                {
                    double angle = i * angleStep;
                    stateList[i].Position = new Point(
                        centerX + (int)(radius * Math.Cos(angle)),
                        centerY + (int)(radius * Math.Sin(angle)));
                }
            }
        }

        public class State
        {
            private static int _idCounter = 0;
            public int Id { get; }
            public string Name { get; set; }
            public bool IsAcceptState { get; set; }
            public Point Position { get; set; }
            public List<object> Transitions { get; } = new List<object>();

            public State(bool isAccept = false, string name = null)
            {
                Id = _idCounter++;
                Name = name ?? $"q{Id}";
                IsAcceptState = isAccept;
            }

            public static void ResetCounter() => _idCounter = 0;
            public override string ToString() => Name;
        }

        public class Dfa : BaseAutomaton
        {
            public Dfa() { State.ResetCounter(); }

            public bool Simulate(string input)
            {
                var currentState = StartState;
                foreach (char c in input)
                {
                    var transition = currentState.Transitions.Cast<DfaTransition>()
                        .FirstOrDefault(t => t.Symbol == c);
                    if (transition == null) return false;
                    currentState = transition.ToState;
                }
                return currentState.IsAcceptState;
            }
        }

        public class DfaTransition
        {
            public char Symbol { get; set; }
            public State ToState { get; set; }
        }

        public class Nfa
        {
            public State StartState { get; set; }
            public State AcceptState { get; set; }
            public Nfa() { State.ResetCounter(); }
        }

        public class Pda : BaseAutomaton
        {
            public char StartStackSymbol { get; set; } = 'Z';
            public Pda() { State.ResetCounter(); }
        }

        public class PdaTransition
        {
            public char InputSymbol { get; set; }
            public char StackPopSymbol { get; set; }
            public string StackPushString { get; set; }
            public State ToState { get; set; }
        }

        public class TuringMachine : BaseAutomaton
        {
            public char BlankSymbol { get; set; } = '_';
            public State AcceptState { get; set; }
            public State RejectState { get; set; }
            public TuringMachine() { State.ResetCounter(); }
        }

        public class TmTransition
        {
            public char ReadSymbol { get; set; }
            public char WriteSymbol { get; set; }
            public char Direction { get; set; }
            public State ToState { get; set; }
        }
    }

    public static class RegexConverter
    {
        public static Automata.Nfa ToNfa(string regex)
        {
            if (string.IsNullOrWhiteSpace(regex))
                throw new ArgumentException("التعبير النمطي لا يمكن أن يكون فارغاً");

            // Simplified implementation for common patterns
            if (regex == "(a|b)*abb")
            {
                var nfa = new Automata.Nfa();
                var q0 = new Automata.State(name: "q0");
                var q1 = new Automata.State(name: "q1");
                var q2 = new Automata.State(name: "q2");
                var q3 = new Automata.State(name: "q3", isAccept: true);

                q0.Transitions.Add(new Automata.DfaTransition { Symbol = 'a', ToState = q0 });
                q0.Transitions.Add(new Automata.DfaTransition { Symbol = 'b', ToState = q1 });
                q1.Transitions.Add(new Automata.DfaTransition { Symbol = 'a', ToState = q0 });
                q1.Transitions.Add(new Automata.DfaTransition { Symbol = 'b', ToState = q2 });
                q2.Transitions.Add(new Automata.DfaTransition { Symbol = 'a', ToState = q0 });
                q2.Transitions.Add(new Automata.DfaTransition { Symbol = 'b', ToState = q3 });
                q3.Transitions.Add(new Automata.DfaTransition { Symbol = 'a', ToState = q0 });
                q3.Transitions.Add(new Automata.DfaTransition { Symbol = 'b', ToState = q1 });

                nfa.StartState = q0;
                nfa.AcceptState = q3;
                return nfa;
            }
            else if (regex == "a*b*")
            {
                var nfa = new Automata.Nfa();
                var q0 = new Automata.State(name: "q0", isAccept: true);
                var q1 = new Automata.State(name: "q1", isAccept: true);

                q0.Transitions.Add(new Automata.DfaTransition { Symbol = 'a', ToState = q0 });
                q0.Transitions.Add(new Automata.DfaTransition { Symbol = 'b', ToState = q1 });
                q1.Transitions.Add(new Automata.DfaTransition { Symbol = 'b', ToState = q1 });

                nfa.StartState = q0;
                nfa.AcceptState = q1;
                return nfa;
            }

            throw new ArgumentException("هذا النمط غير مدعوم حالياً. الرجاء استخدام أحد الأنماط المعروفة مثل '(a|b)*abb' أو 'a*b*'");
        }
    }

    public static class NfaConverter
    {
        public static Automata.Dfa ToDfa(Automata.Nfa nfa)
        {
            var dfa = new Automata.Dfa();
            dfa.StartState = nfa.StartState;

            var queue = new Queue<Automata.State>();
            queue.Enqueue(nfa.StartState);
            dfa.States.Add(nfa.StartState);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                foreach (var transObj in current.Transitions)
                {
                    var trans = transObj as Automata.DfaTransition;
                    if (trans == null) continue;

                    if (!dfa.States.Contains(trans.ToState))
                    {
                        dfa.States.Add(trans.ToState);
                        queue.Enqueue(trans.ToState);
                    }

                    dfa.Alphabet.Add(trans.Symbol);
                }
            }

            return dfa;
        }
    }

    public static class CfgParser
    {
        public static Cfg Parse(string text)
        {
            var cfg = new Cfg();
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0) throw new ArgumentException("القواعد النحوية لا يمكن أن تكون فارغة");

            bool firstLine = true;
            foreach (var line in lines)
            {
                var parts = line.Split(new[] { "->" }, StringSplitOptions.None);
                if (parts.Length != 2)
                    throw new FormatException($"صيغة غير صحيحة في القاعدة: '{line}'");

                char variable = parts[0].Trim()[0];
                if (!char.IsUpper(variable))
                    throw new FormatException($"المتغير '{variable}' يجب أن يكون حرفاً كبيراً");

                if (firstLine)
                {
                    cfg.StartVariable = variable;
                    firstLine = false;
                }

                cfg.Variables.Add(variable);
                if (!cfg.Rules.ContainsKey(variable))
                    cfg.Rules[variable] = new List<string>();

                var productions = parts[1].Split('|');
                foreach (var prod in productions)
                {
                    var production = prod.Trim();
                    cfg.Rules[variable].Add(production);

                    foreach (char c in production)
                    {
                        if (char.IsLower(c)) 
                            cfg.Terminals.Add(c);
                        else if (c != 'ε' && !char.IsUpper(c))
                            throw new FormatException($"رمز غير صالح '{c}' في القاعدة");
                    }
                }
            }

            return cfg;
        }
    }

    public static class CfgConverter
    {
        public static Automata.Pda ToPda(Cfg cfg)
        {
            var pda = new Automata.Pda();
            var qStart = new Automata.State(name: "q_start");
            var qLoop = new Automata.State(name: "q_loop");
            var qAccept = new Automata.State(isAccept: true, name: "q_accept");

            pda.StartState = qStart;
            pda.States.Add(qStart);
            pda.States.Add(qLoop);
            pda.States.Add(qAccept);

            // Initial transition
            qStart.Transitions.Add(new Automata.PdaTransition
            {
                InputSymbol = 'ε',
                StackPopSymbol = 'ε',
                StackPushString = $"{cfg.StartVariable}{pda.StartStackSymbol}",
                ToState = qLoop
            });

            // Rule transitions
            foreach (var rule in cfg.Rules)
            {
                char variable = rule.Key;
                foreach (var production in rule.Value)
                {
                    qLoop.Transitions.Add(new Automata.PdaTransition
                    {
                        InputSymbol = 'ε',
                        StackPopSymbol = variable,
                        StackPushString = production == "ε" ? "" : production,
                        ToState = qLoop
                    });
                }
            }

            // Terminal matching transitions
            foreach (char terminal in cfg.Terminals)
            {
                qLoop.Transitions.Add(new Automata.PdaTransition
                {
                    InputSymbol = terminal,
                    StackPopSymbol = terminal,
                    StackPushString = "",
                    ToState = qLoop
                });
                pda.Alphabet.Add(terminal);
            }

            // Final transition
            qLoop.Transitions.Add(new Automata.PdaTransition
            {
                InputSymbol = 'ε',
                StackPopSymbol = pda.StartStackSymbol,
                StackPushString = "",
                ToState = qAccept
            });

            return pda;
        }
    }

    public static class TmBuilder
    {
        public static Automata.TuringMachine BuildFromDescription(string description)
        {
            description = description.Replace(" ", "").ToLower();

            if (description.Contains("a^nb^n") || description.Contains("anbn"))
                return Build_anbn();
            if (description.Contains("w#w"))
                return Build_w_hash_w();

            throw new NotImplementedException("هذا الوصف للغة غير مدعوم حالياً. الرجاء استخدام أحد الأوصاف المعروفة مثل 'a^n b^n' أو 'w#w'");
        }

        private static Automata.TuringMachine Build_anbn()
        {
            var tm = new Automata.TuringMachine();
            var q0 = new Automata.State(name: "q0"); // Start
            var q1 = new Automata.State(name: "q1"); // Found 'a', mark as X
            var q2 = new Automata.State(name: "q2"); // Find rightmost 'b'
            var q3 = new Automata.State(name: "q3"); // Found 'b', mark as Y
            var q4 = new Automata.State(name: "q4"); // Move left to start
            var q5 = new Automata.State(name: "q5", isAccept: true); // Accept

            tm.StartState = q0;
            tm.AcceptState = q5;
            tm.States.UnionWith(new[] { q0, q1, q2, q3, q4, q5 });
            tm.Alphabet.UnionWith(new[] { 'a', 'b', 'X', 'Y' });

            // q0: Start state, look for 'a'
            q0.Transitions.Add(new Automata.TmTransition { ReadSymbol = 'a', WriteSymbol = 'X', Direction = 'R', ToState = q1 });
            q0.Transitions.Add(new Automata.TmTransition { ReadSymbol = 'Y', WriteSymbol = 'Y', Direction = 'R', ToState = q4 });

            // q1: Scan right to find 'b'
            q1.Transitions.Add(new Automata.TmTransition { ReadSymbol = 'a', WriteSymbol = 'a', Direction = 'R', ToState = q1 });
            q1.Transitions.Add(new Automata.TmTransition { ReadSymbol = 'Y', WriteSymbol = 'Y', Direction = 'R', ToState = q1 });
            q1.Transitions.Add(new Automata.TmTransition { ReadSymbol = 'b', WriteSymbol = 'Y', Direction = 'L', ToState = q2 });

            // q2: Scan left to find X
            q2.Transitions.Add(new Automata.TmTransition { ReadSymbol = 'a', WriteSymbol = 'a', Direction = 'L', ToState = q2 });
            q2.Transitions.Add(new Automata.TmTransition { ReadSymbol = 'Y', WriteSymbol = 'Y', Direction = 'L', ToState = q2 });
            q2.Transitions.Add(new Automata.TmTransition { ReadSymbol = 'X', WriteSymbol = 'X', Direction = 'R', ToState = q0 });

            // q4: Verify all symbols are Y
            q4.Transitions.Add(new Automata.TmTransition { ReadSymbol = 'Y', WriteSymbol = 'Y', Direction = 'R', ToState = q4 });
            q4.Transitions.Add(new Automata.TmTransition { ReadSymbol = tm.BlankSymbol, WriteSymbol = tm.BlankSymbol, Direction = 'L', ToState = q5 });

            return tm;
        }

        private static Automata.TuringMachine Build_w_hash_w()
        {
            throw new NotImplementedException("بناء آلة تورنغ للغة w#w غير متاح حالياً");
        }
    }

    public static class DiagramPainter
    {
        public static void DrawAutomaton(Graphics g, Automata.BaseAutomaton automaton)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.White);

            if (automaton?.StartState == null || automaton.States.Count == 0)
                return;

            // Draw transitions first (so they appear behind states)
            using (var pen = new Pen(Color.Black, 2))
            using (var brush = new SolidBrush(Color.Black))
            using (var font = new Font("Tahoma", 9))
            {
                pen.CustomEndCap = new AdjustableArrowCap(5, 5);

                foreach (var state in automaton.States)
                {
                    foreach (var trans in state.Transitions)
                    {
                        var toState = GetToState(trans);
                        string label = GetTransitionLabel(trans);
                        DrawTransition(g, pen, brush, font, state.Position, toState.Position, label);
                    }
                }
            }

            // Draw states
            using (var font = new Font("Tahoma", 10, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.LightBlue))
            using (var pen = new Pen(Color.Black, 2))
            {
                foreach (var state in automaton.States)
                {
                    DrawState(g, font, brush, pen, state, state == automaton.StartState);
                }
            }
        }

        private static void DrawState(Graphics g, Font font, Brush fillBrush, Pen borderPen, Automata.State state, bool isStart)
        {
            int size = 35;
            Rectangle rect = new Rectangle(state.Position.X - size / 2, state.Position.Y - size / 2, size, size);

            g.FillEllipse(fillBrush, rect);
            g.DrawEllipse(borderPen, rect);

            if (state.IsAcceptState)
            {
                g.DrawEllipse(borderPen, Rectangle.Inflate(rect, 4, 4));
            }

            if (isStart)
            {
                g.DrawString("→", font, Brushes.Black, state.Position.X - 40, state.Position.Y - 10);
            }

            StringFormat sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            g.DrawString(state.Name, font, Brushes.Black, rect, sf);
        }

        private static void DrawTransition(Graphics g, Pen pen, Brush brush, Font font, Point from, Point to, string label)
        {
            // Adjust start/end points to be on the edge of the state circles
            double angle = Math.Atan2(to.Y - from.Y, to.X - from.X);
            int radius = 17;
            Point adjustedFrom = new Point(
                from.X + (int)(radius * Math.Cos(angle)),
                from.Y + (int)(radius * Math.Sin(angle)));
            Point adjustedTo = new Point(
                to.X - (int)(radius * Math.Cos(angle)),
                to.Y - (int)(radius * Math.Sin(angle)));

            g.DrawLine(pen, adjustedFrom, adjustedTo);

            // Draw label
            Point midPoint = new Point(
                (adjustedFrom.X + adjustedTo.X) / 2,
                (adjustedFrom.Y + adjustedTo.Y) / 2);

            var transform = g.Transform;
            float labelAngle = (float)(angle * 180 / Math.PI);

            g.TranslateTransform(midPoint.X, midPoint.Y);
            g.RotateTransform(labelAngle);
            g.DrawString(label, font, brush, 0, -20);
            g.Transform = transform;
        }

        private static Automata.State GetToState(object trans)
        {
            if (trans is Automata.DfaTransition dfa) return dfa.ToState;
            if (trans is Automata.PdaTransition pda) return pda.ToState;
            if (trans is Automata.TmTransition tm) return tm.ToState;
            return null;
        }

        private static string GetTransitionLabel(object trans)
        {
            if (trans is Automata.DfaTransition dfa) return dfa.Symbol.ToString();
            if (trans is Automata.PdaTransition pda)
                return $"{pda.InputSymbol}, {pda.StackPopSymbol}/{pda.StackPushString}";
            if (trans is Automata.TmTransition tm)
                return $"{tm.ReadSymbol}→{tm.WriteSymbol},{tm.Direction}";
            return "?";
        }
    }

    public static class TableManager
    {
        public static void UpdateFaTable(DataGridView dgv, Automata.Dfa dfa)
        {
            dgv.Rows.Clear();
            dgv.Columns.Clear();

            // Add columns
            dgv.Columns.Add("State", "الحالة");
            foreach (char symbol in dfa.Alphabet.OrderBy(c => c))
            {
                dgv.Columns.Add(symbol.ToString(), $"'{symbol}'");
            }

            // Add rows
            foreach (var state in dfa.States.OrderBy(s => s.Name))
            {
                var row = new DataGridViewRow();
                row.CreateCells(dgv);
                row.Cells[0].Value = GetStateLabel(state, state == dfa.StartState);

                for (int i = 0; i < dfa.Alphabet.Count; i++)
                {
                    char symbol = dfa.Alphabet.OrderBy(c => c).ElementAt(i);
                    var transition = state.Transitions.Cast<Automata.DfaTransition>()
                        .FirstOrDefault(t => t.Symbol == symbol);
                    row.Cells[i + 1].Value = transition?.ToState.Name ?? "-";
                }

                dgv.Rows.Add(row);
            }
        }

        public static void UpdatePdaTable(DataGridView dgv, Automata.Pda pda)
        {
            dgv.Rows.Clear();
            dgv.Columns.Clear();

            dgv.Columns.Add("FromState", "من حالة");
            dgv.Columns.Add("Input", "المدخل");
            dgv.Columns.Add("Pop", "Pop");
            dgv.Columns.Add("Push", "Push");
            dgv.Columns.Add("ToState", "إلى حالة");

            foreach (var state in pda.States.OrderBy(s => s.Name))
            {
                foreach (Automata.PdaTransition trans in state.Transitions)
                {
                    dgv.Rows.Add(
                        state.Name,
                        trans.InputSymbol == 'ε' ? "ε" : trans.InputSymbol.ToString(),
                        trans.StackPopSymbol == 'ε' ? "ε" : trans.StackPopSymbol.ToString(),
                        string.IsNullOrEmpty(trans.StackPushString) ? "ε" : trans.StackPushString,
                        trans.ToState.Name
                    );
                }
            }
        }

        public static void UpdateTmTable(DataGridView dgv, Automata.TuringMachine tm)
        {
            dgv.Rows.Clear();
            dgv.Columns.Clear();

            dgv.Columns.Add("State", "الحالة");
            dgv.Columns.Add("Read", "يقرأ");
            dgv.Columns.Add("Write", "يكتب");
            dgv.Columns.Add("Direction", "الاتجاه");
            dgv.Columns.Add("NextState", "الحالة التالية");

            foreach (var state in tm.States.OrderBy(s => s.Name))
            {
                foreach (Automata.TmTransition trans in state.Transitions)
                {
                    dgv.Rows.Add(
                        state.Name,
                        trans.ReadSymbol.ToString(),
                        trans.WriteSymbol.ToString(),
                        trans.Direction.ToString(),
                        trans.ToState.Name
                    );
                }
            }
        }

        public static void UpdateTmTapeDisplay(DataGridView dgv, List<char> tape, int head)
        {
            dgv.Rows.Clear();
            dgv.Columns.Clear();

            for (int i = 0; i < tape.Count; i++)
            {
                dgv.Columns.Add(i.ToString(), i == head ? "↓" : "");
                dgv.Columns[i].Width = 30;
            }

            var row = new DataGridViewRow();
            row.CreateCells(dgv);

            for (int i = 0; i < tape.Count; i++)
            {
                row.Cells[i].Value = tape[i].ToString();
                if (i == head)
                {
                    row.Cells[i].Style.BackColor = Color.Yellow;
                    row.Cells[i].Style.Font = new Font(dgv.Font, FontStyle.Bold);
                }
            }

            dgv.Rows.Add(row);
        }

        private static string GetStateLabel(Automata.State state, bool isStart)
        {
            string label = "";
            if (isStart) label += "→ ";
            label += state.Name;
            if (state.IsAcceptState) label += " *";
            return label;
        }
    }

    public class PdaSimulator
    {
        private readonly Automata.Pda _pda;
        private readonly string _input;
        private readonly Queue<PdaConfiguration> _configurations = new Queue<PdaConfiguration>();

        private class PdaConfiguration
        {
            public Automata.State State { get; set; }
            public int InputIndex { get; set; }
            public Stack<char> Stack { get; set; }
        }

        public PdaSimulator(Automata.Pda pda, string input)
        {
            _pda = pda;
            _input = input;
        }

        public async Task<bool> RunAsync(Action<dynamic> onStep)
        {
            var initialConfig = new PdaConfiguration
            {
                State = _pda.StartState,
                InputIndex = 0,
                Stack = new Stack<char>()
            };
            initialConfig.Stack.Push(_pda.StartStackSymbol);
            _configurations.Enqueue(initialConfig);

            while (_configurations.Count > 0)
            {
                var current = _configurations.Dequeue();

                string stackStr = new string(current.Stack.Reverse().ToArray());
                onStep(new
                {
                    State = current.State,
                    RemainingInput = _input.Substring(current.InputIndex),
                    StackSnapshot = stackStr
                });
                await Task.Delay(300);

                // Check for acceptance
                if (current.InputIndex == _input.Length && current.State.IsAcceptState)
                {
                    return true;
                }

                // Process epsilon transitions
                foreach (Automata.PdaTransition trans in current.State.Transitions
                    .Where(t => ((Automata.PdaTransition)t).InputSymbol == 'ε'))
                {
                    ProcessTransition(current, trans);
                }

                // Process input transitions
                if (current.InputIndex < _input.Length)
                {
                    char currentChar = _input[current.InputIndex];
                    foreach (Automata.PdaTransition trans in current.State.Transitions
                        .Where(t => ((Automata.PdaTransition)t).InputSymbol == currentChar))
                    {
                        ProcessTransition(current, trans, advanceInput: true);
                    }
                }
            }

            return false;
        }

        private void ProcessTransition(PdaConfiguration current, Automata.PdaTransition trans, bool advanceInput = false)
        {
            if (trans.StackPopSymbol == 'ε' ||
                (current.Stack.Count > 0 && current.Stack.Peek() == trans.StackPopSymbol))
            {
                var newStack = new Stack<char>(current.Stack.Reverse());
                if (trans.StackPopSymbol != 'ε') newStack.Pop();
                foreach (char c in trans.StackPushString.Reverse()) newStack.Push(c);

                _configurations.Enqueue(new PdaConfiguration
                {
                    State = trans.ToState,
                    InputIndex = advanceInput ? current.InputIndex + 1 : current.InputIndex,
                    Stack = newStack
                });
            }
        }
    }

    public class TmSimulator
    {
        private readonly Automata.TuringMachine _tm;
        private List<char> _tape;
        private int _head;
        private Automata.State _currentState;

        public TmSimulator(Automata.TuringMachine tm, string input)
        {
            _tm = tm;
            _tape = new List<char>(input);
            if (_tape.Count == 0) _tape.Add(_tm.BlankSymbol);
            _head = 0;
            _currentState = tm.StartState;
        }

        public async Task<bool> RunAsync(Action<List<char>, int, Automata.State> onStep)
        {
            while (_currentState != _tm.AcceptState && _currentState != _tm.RejectState)
            {
                onStep(_tape, _head, _currentState);
                await Task.Delay(400);

                char currentSymbol = _tape[_head];
                var transition = _currentState.Transitions.Cast<Automata.TmTransition>()
                    .FirstOrDefault(t => t.ReadSymbol == currentSymbol);

                if (transition == null)
                {
                    _currentState = _tm.RejectState ?? new Automata.State(name: "q_reject");
                    break;
                }

                // Execute transition
                _tape[_head] = transition.WriteSymbol;
                _currentState = transition.ToState;

                // Move head
                if (transition.Direction == 'R')
                {
                    _head++;
                    if (_head == _tape.Count) _tape.Add(_tm.BlankSymbol);
                }
                else if (transition.Direction == 'L')
                {
                    _head--;
                    if (_head < 0)
                    {
                        _tape.Insert(0, _tm.BlankSymbol);
                        _head = 0;
                    }
                }
            }

            onStep(_tape, _head, _currentState);
            return _currentState == _tm.AcceptState;
        }
    }

    public class Cfg
    {
        public char StartVariable { get; set; }
        public HashSet<char> Variables { get; } = new HashSet<char>();
        public HashSet<char> Terminals { get; } = new HashSet<char>();
        public Dictionary<char, List<string>> Rules { get; } = new Dictionary<char, List<string>>();
    }
}