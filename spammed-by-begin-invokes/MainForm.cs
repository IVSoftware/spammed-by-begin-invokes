
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
            recycler.CellPainting += (sender, e) =>
            {
                Debug.WriteLine(e.RowIndex);
                var button = _buttons[e.RowIndex];
                //if (button.Parent is null)
                //{
                //    recycler.Controls.Add(button);
                //}
                ButtonRenderer.DrawButton(e.Graphics, e.CellBounds, button.Text, button.Font, false, PushButtonState.Normal);

                //if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                //e.PaintBackground(e.ClipBounds, true);
                //button.Bounds = e.CellBounds;
                //button.Refresh();
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
