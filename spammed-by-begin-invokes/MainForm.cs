
using System.Diagnostics;
using System.Windows.Forms.VisualStyles;

namespace spammed_by_begin_invokes
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            _buttons = Enumerable.Range(1, 10000).Select(n=>
                new Button
                {
                    Margin = new Padding(),
                    Text = $"Button {n}.{_round}",
                }
            ).ToArray();
            foreach (var button in _buttons) button.Click += Any_Clicked; recycler.Scroll += (sender, e) =>
            {
                // Get the range of visible rows
                int firstVisibleRow = recycler.FirstDisplayedScrollingRowIndex;
                int lastVisibleRow =
                    Math.Min(recycler.RowCount - 1,
                    firstVisibleRow + recycler.DisplayedRowCount(false) - 1);
            };
        }
        private readonly Button[] _buttons;
        private char _round = 'A';

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            recycler.RowTemplate.Height = 100;
            recycler.VirtualMode = true;
            recycler.ReadOnly = true;
            recycler.ColumnHeadersVisible = false;
            recycler.RowHeadersVisible = false;
            recycler.RowCount = _buttons.Length;
            recycler.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            recycler.CellPainting += async(sender, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                Debug.WriteLine(e.RowIndex);
                var button = _buttons[e.RowIndex];
                if (button.Parent is null)
                {
                    recycler.Controls.Add(button);
                }
                e.PaintBackground(e.ClipBounds, true);
                if (MouseButtons == MouseButtons.None)
                {
                    button.Bounds = recycler.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, true);
                    button.Refresh();
                    button.BringToFront();
                }
                else ButtonRenderer.DrawButton(e.Graphics, e.CellBounds, button.Text, button.Font, false, PushButtonState.Normal);
                e.Handled = true;
            };
        }


        private void Any_Clicked(object? sender, EventArgs e)
        {
            if (sender is Button button)
            {
                BeginInvoke(() => MessageBox.Show(button.Text));
            }
        }
    }
}
