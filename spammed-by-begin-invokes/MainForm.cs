
using System.Diagnostics;

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
                    Dock = DockStyle.Fill,
                    Text = $"Button {n}.{_round}",
                }
            ).ToArray();
            foreach (var button in _buttons)
            {
                button.Click += Any_Clicked;
            }
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
