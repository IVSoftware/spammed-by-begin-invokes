
using System.Diagnostics;
using System.Windows.Forms.VisualStyles;

namespace spammed_by_begin_invokes
{
    public partial class MainForm : Form
    {
        const int SAMPLE_SIZE = 10001;
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
                        for (int i = 0; i < SAMPLE_SIZE; i++)
                        {
                            if (_cts.Token.IsCancellationRequested) return;
                            BeginInvoke(() =>
                            {
                                // Perform a real update on the UI.
                                Text = i.ToString();
                                _updateRun.Add(stopwatch.Elapsed.ToString(@"hh\:mm\:ss\:ffff"));
                            });
                        }
                        _updateScheduled.Add(stopwatch.Elapsed.ToString(@"hh\:mm\:ss\:ffff"));
                    }, _cts.Token);

                    stopwatch.Stop();
                    MessageBox.Show($"Done @ {stopwatch.Elapsed.ToString(@"hh\:mm\:ss\:ffff")}");
                    Debug.WriteLine(string.Join("\n", _updateScheduled));
                    Debug.WriteLine(string.Join("\n", _updateRun));
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
