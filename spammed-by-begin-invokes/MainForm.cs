
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Windows.Forms.VisualStyles;

// https://learn.microsoft.com/en-us/visualstudio/debugger/using-the-debuggerdisplay-attribute?view=vs-2022

namespace spammed_by_begin_invokes
{
    public partial class MainForm : Form
    {
        const int SAMPLE_SIZE = 20000;
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
                                0x0006 => "WM_ACTIVATE",
                                0x0007 => "WM_SETFOCUS",
                                0x0008 => "WM_KILLFOCUS",
                                0x000A => "WM_CLOSE",
                                0x000C => "WM_SYSCOLORCHANGE",
                                0x000D => "WM_QUERYOPEN",
                                0x000E => "WM_ERASEBKGND",
                                0x0014 => "WM_SETCURSOR",
                                0x001F => "WM_WINDOWPOSCHANGING",
                                0x0020 => "WM_WINDOWPOSCHANGED",
                                0x002B => "WM_COMPACTING",
                                0x0046 => "WM_WINDOWPOSCHANGED",
                                0x007F => "WM_GETICON",
                                0x0086 => "WM_NCACTIVATE",
                                0x00AE => "WM_NCUAHDRAWCAPTION (undocumented, according to 'best information'",
                                0x0135 => "WM_PRINTCLIENT",
                                0x0201 => "WM_LBUTTONDOWN",
                                0x0202 => "WM_LBUTTONUP",  
                                0x0281 => "WM_IME_SETCONTEXT",
                                0x0282 => "WM_IME_NOTIFY",
                                0x0318 => "Unknown (Possibly App-Specific)",
                                0xC1F0 => "WM_USER+X (App-Defined Message)",
                                _ => $"Unknown (0x{i:X4})"
                            };

                            Debug.WriteLine($"[{_histogram[i], 5}]: 0X{i:X4} {messageName}");
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
        DateTime _firstWMUSER;
        DateTime _lastWMUSER;
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            _histogram[m.Msg]++;
        }
    }
}
