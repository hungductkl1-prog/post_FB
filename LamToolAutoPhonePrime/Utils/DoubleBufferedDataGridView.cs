using System.Windows.Forms;

namespace LamToolAutoPhonePrime.Utils
{
    /// <summary>
    /// DataGridView subclass with DoubleBuffered enabled via protected property override.
    /// Use this instead of reflection (InvokeMember/GetProperty NonPublic) which breaks in AOT.
    /// </summary>
    public class DoubleBufferedDataGridView : DataGridView
    {
        public DoubleBufferedDataGridView()
        {
            DoubleBuffered = true;
        }
    }
}
