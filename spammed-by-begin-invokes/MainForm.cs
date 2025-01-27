
using System.Diagnostics;

namespace spammed_by_begin_invokes
{
    public partial class MainForm : Form
    {
        public MainForm() => InitializeComponent();

        private char _round = 'A';
        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            var stopwatch = Stopwatch.StartNew();
            for (int i = 1; i <= 10000; i++)
            {
                var button = new Button
                {
                    Margin = new Padding(),
                    Width = flowLayoutPanel.Width -
                    (flowLayoutPanel.Padding.Horizontal + SystemInformation.VerticalScrollBarWidth),
                    Height = 100,
                    Text = $"Button {i}.{_round}",
                };
                button.Click += Any_Clicked;
                flowLayoutPanel.Controls.Add(button);
                await Task.Delay(1);
            }
            stopwatch.Stop();
            MessageBox.Show(stopwatch.Elapsed.ToString(@"hh\:mm\:ss"));
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
