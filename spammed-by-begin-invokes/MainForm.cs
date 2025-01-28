
using System.Diagnostics;
using System.Windows.Forms.VisualStyles;

namespace spammed_by_begin_invokes
{
    public partial class MainForm : Form
    {
        const int SAMPLE_SIZE = 100000;
        public MainForm()
        {
            InitializeComponent();
            buttonUpdate.Text = $"Update {SAMPLE_SIZE}x";
            buttonUpdate.CheckedChanged += async(sender, e) =>
            {
                if (buttonUpdate.Checked)
                {
                    _cts = new CancellationTokenSource();
                    await Task.Run(async () =>
                    {
                        for (int i = 0; i < SAMPLE_SIZE; i++)
                        {
                            if (_cts.Token.IsCancellationRequested) return;
                            var iAsyncResult = BeginInvoke(() =>
                            {
                                // Perform a real update on the UI.
                                Text = i.ToString();
                            });
                            await Task.Run(() => iAsyncResult.AsyncWaitHandle.WaitOne());
                        }
                    }, _cts.Token);
                    MessageBox.Show("Done");
                }
                else
                {
                    _cts?.Cancel();
                }
            };
        }
        Task? _runningTask = null;
        CancellationTokenSource? _cts = null;
    }
}
