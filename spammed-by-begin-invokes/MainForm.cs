
using System.Diagnostics;
using System.Diagnostics.Metrics;
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
                    lock (_lock)
                    {
                        _histogram = new int[0x10000];
                    }
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    _cts = new CancellationTokenSource();
                    await Task.Run(() =>
                    {
                        for (int i = 1; i <= SAMPLE_SIZE; i++)
                        {
                            if (_cts.Token.IsCancellationRequested) return;
                            int captureN = i;
                            BeginInvoke(() =>
                            {
                                // Perform a real update on the UI.
                                Text = captureN.ToString();
                            });
#if false
                            // Hardware delay. Spin some clock cycles.
                            // WARNING: Not production code. It's very system-dependent.
                            // THAT SAID: You can actually play with this and make it
                            // "take longer" to overring the buffer. For example, by setting
                            // this to 50000 I was able to run it up to  55000 but not 65000.
                            for (int count = 0; count < 50000; count++);
#endif
                        }
                    }, _cts.Token);

                    stopwatch.Stop();
                    MessageBox.Show($"Done @ {stopwatch.Elapsed.ToString(@"hh\:mm\:ss\:ffff")}");
                    BeginInvoke(()=>buttonUpdate.Checked = false);
                }
                else
                {
                    _cts?.Cancel();
                    for (int i = 0; i < _histogram.Length; i++)
                    {
                        if (_histogram[i] > 0)
                        {
                            string messageName = i switch
                            {
                                6 => "WM_ACTIVATE",
                                7 => "WM_SETFOCUS",
                                8 => "WM_KILLFOCUS",
                                10 => "WM_CLOSE",
                                12 => "WM_SYSCOLORCHANGE",
                                13 => "WM_QUERYOPEN",
                                14 => "WM_ERASEBKGND",
                                20 => "WM_SETCURSOR",
                                31 => "WM_WINDOWPOSCHANGING",
                                32 => "WM_WINDOWPOSCHANGED",
                                43 => "WM_COMPACTING",
                                70 => "WM_WINDOWPOSCHANGED",
                                127 => "WM_GETICON",
                                134 => "WM_NCACTIVATE",
                                174 => "WM_ENABLE",
                                309 => "WM_PRINTCLIENT",
                                641 => "WM_IME_SETCONTEXT",
                                642 => "WM_IME_NOTIFY",
                                792 => "Unknown",
                                49648 => "WM_USER+X (App-Defined Message)",
                                _ => $"Unknown ({i})"
                            };

                            Debug.WriteLine($"[{_histogram[i], 6}]: 0X{i:X4} {messageName}");
                        }
                    }
                }
            };
        }
        private readonly object _lock = new object();
        CancellationTokenSource? _cts = null;
        List<string> _updateScheduled = new ();
        List<string> _updateRun = new ();
        int[] _histogram = new int[0x10000];
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            _histogram[m.Msg]++;
        }
    }
}
