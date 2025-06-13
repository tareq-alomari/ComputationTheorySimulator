using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ComputationTheorySimulator.UI
{
    public static class TapeVisualizerPainter
    {
        public static void DrawTape(Graphics g, Panel tapePanel, Dictionary<int, char> tape, int headPosition)
        {
            g.Clear(Color.White);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int cellSize = 40;
            int visibleCells = tapePanel.Width / cellSize;
            int startCellIndex = headPosition - (visibleCells / 2);

            using (var font = new Font("Consolas", 14, FontStyle.Bold))
            using (var pen = new Pen(Color.Gray))
            using (var headBrush = new SolidBrush(Color.FromArgb(100, 255, 193, 7)))
            {
                for (int i = 0; i < visibleCells; i++)
                {
                    int cellIndex = startCellIndex + i;
                    int x = i * cellSize;
                    var rect = new Rectangle(x, (tapePanel.Height / 2) - (cellSize / 2), cellSize, cellSize);
                    char symbol = tape.ContainsKey(cellIndex) ? tape[cellIndex] : '_';

                    g.DrawRectangle(pen, rect);
                    TextRenderer.DrawText(g, symbol.ToString(), font, rect, Color.Black, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                    if (cellIndex == headPosition)
                    {
                        g.FillRectangle(headBrush, rect);
                        Point[] arrow = { new Point(x + cellSize / 2, 5), new Point(x + cellSize / 2 - 5, 15), new Point(x + cellSize / 2 + 5, 15) };
                        g.FillPolygon(Brushes.Crimson, arrow);
                    }
                }
            }
        }
    }
}

