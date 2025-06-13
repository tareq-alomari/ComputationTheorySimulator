using System;
using System.Drawing;
using System.Windows.Forms;

namespace ComputationTheorySimulator.Presentation
{
    public class MainMenuForm : Form
    {
        public MainMenuForm()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Text = "نظام محاكاة نماذج الحوسبة النظرية";
            this.Size = new Size(850, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(30, 40, 50);
            this.Font = new Font("Arial", 10, FontStyle.Regular);
            this.ForeColor = Color.White;
            this.Icon = SystemIcons.Information;

            Panel mainPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(20) };
            Label titleLabel = new Label { Text = "نظرية الحوسبة", Font = new Font("Arial", 40, FontStyle.Bold), ForeColor = Color.FromArgb(110, 190, 255), TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Top, Height = 150, Padding = new Padding(0, 30, 0, 0) };
            Label descLabel = new Label { Text = "نظام محاكاة نماذج الحساب النظرية\n(Finite Automata - Turing Machine - Pushdown Automata)", Font = new Font("Arial", 14, FontStyle.Italic), TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Top, Height = 80, ForeColor = Color.FromArgb(180, 180, 180) };
            Panel centerPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            Button startButton = new Button { Text = "بدء المحاكاة", Font = new Font("Arial", 18, FontStyle.Bold), BackColor = Color.FromArgb(80, 160, 230), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Size = new Size(300, 70), Cursor = Cursors.Hand, Anchor = AnchorStyles.None };
            startButton.FlatAppearance.BorderSize = 0;
            startButton.MouseEnter += (s, e) => startButton.BackColor = Color.FromArgb(100, 180, 250);
            startButton.MouseLeave += (s, e) => startButton.BackColor = Color.FromArgb(80, 160, 230);
            startButton.Click += (s, e) => OpenAutomatonForm();
            startButton.Location = new Point((centerPanel.Width - startButton.Width) / 2, (centerPanel.Height - startButton.Height) / 2);
            Label teamLabel = new Label { Text = ":فريق التطوير\nأيمن قمحان - طارق العمري\nضياء الحضرمي - علي القواس - حازم العمري\nإشراف: د. خالد الكحسة", Font = new Font("Arial", 15), TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Bottom, Height = 120, ForeColor = Color.FromArgb(160, 160, 160) };
            Label copyrightLabel = new Label { Text = "© 2025 جامعة إب - كلية الحاسوب وتقنية المعلومات", Font = new Font("Arial", 12), TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Bottom, Height = 40, ForeColor = Color.FromArgb(120, 120, 120) };

            centerPanel.Controls.Add(startButton);
            mainPanel.Controls.Add(centerPanel);
            mainPanel.Controls.Add(descLabel);
            mainPanel.Controls.Add(titleLabel);
            mainPanel.Controls.Add(teamLabel);
            mainPanel.Controls.Add(copyrightLabel);
            this.Controls.Add(mainPanel);

            this.Resize += (s, e) => {
                startButton.Location = new Point((centerPanel.Width - startButton.Width) / 2, (centerPanel.Height - startButton.Height) / 2);
            };
        }

        private void OpenAutomatonForm()
        {
            var mainForm = new MainSimulator();
            mainForm.Show();
            this.Hide();
            mainForm.FormClosed += (s, e) => this.Show();
        }
    }
}