
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Windows.Forms.VisualStyles;

namespace spammed_by_begin_invokes
{
    public partial class MainForm : Form
    {
        const int SAMPLE_SIZE = 55000;
        public MainForm()
        {
            InitializeComponent();
            buttonUpdate.Text = $"Update {SAMPLE_SIZE}x";
            buttonUpdate.CheckedChanged += async(sender, e) =>
            {
                if (buttonUpdate.Checked)
                {
                    _updateRun.Clear();
                    _updateScheduled.Clear();
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    _cts = new CancellationTokenSource();
                    await Task.Run(() =>
                    {
                        for (int i = 1; i <= SAMPLE_SIZE; i++)
                        {
                            if (_cts.Token.IsCancellationRequested) return;
                            BeginInvoke(() =>
                            {
                                // Perform a real update on the UI.
                                Text = i.ToString();
                            });
                            // Hardware delay. Spin some clock cycles.
                            // WARNING: Not production code. It's very system-dependent.
                            // THAT SAID: You can actually play with this and make it
                            // "take longer" to overring the buffer. For example, by setting
                            // this to 50000 I was able to run it up to  55000 but not 65000.
                            for (int count = 0; count < 50000; count++);
                        }
                    }, _cts.Token);

                    stopwatch.Stop();
                    MessageBox.Show($"Done @ {stopwatch.Elapsed.ToString(@"hh\:mm\:ss\:ffff")}");
                    BeginInvoke(()=>buttonUpdate.Checked = false);
                }
                else
                {
                    _cts?.Cancel();
                }
            };
        }
        CancellationTokenSource? _cts = null;
        List<string> _updateScheduled = new ();
        List<string> _updateRun = new ();
    }
}
