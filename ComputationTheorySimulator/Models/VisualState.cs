using System.Drawing;

namespace ComputationTheorySimulator.Models2
{
    /// <summary>
    /// يمثل الحالة المرئية لأي آلة على لوحة الرسم.
    /// </summary>
    public class VisualState
    {
        public int Id { get; set; }
        public Point Position { get; set; }
        public bool IsAcceptState { get; set; }
        public bool IsRejectState { get; set; } // خاص بآلة تورنغ
    }
}