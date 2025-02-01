
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Windows.Forms.VisualStyles;

// https://learn.microsoft.com/en-us/visualstudio/debugger/using-the-debuggerdisplay-attribute?view=vs-2022

namespace spammed_by_begin_invokes
{
    public partial class MainForm : Form, IMessageFilter
    {
        const int SAMPLE_SIZE = 100000;
        public MainForm()
        {
            InitializeComponent();
            Application.AddMessageFilter(this);
            Disposed += (sender, e) => Application.RemoveMessageFilter(this);

            buttonUpdate.Text = $"Update {SAMPLE_SIZE}x";
            buttonUpdate.CheckedChanged += async(sender, e) =>
            {
                if (buttonUpdate.Checked)
                {
                    _updateRun.Clear();
                    _updateScheduled.Clear();
                    lock (_lock)
                    {
                        _stopwatch = Stopwatch.StartNew();
                        _histogram = new int[0x10000];
                        // Add in the events that got us here (before the histogram started counting).
                        _histogram[0x0201]++;
                        _histogram[0x0202]++;
                        _capture = true;
                    }
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
                    lock (_lock)
                    {
                        _capture = false;
                    }
                    BeginInvoke(()=>buttonUpdate.Checked = false);
                }
                else
                {
                    _cts?.Cancel();
                    lock (_lock)
                    {
                        _capture = false;
                    }
                    Debug.WriteLine(string.Empty);
                    Debug.WriteLine($"The SECOND mouse click FINALLY comes to front of queue @ {_stopwatch?.Elapsed.TotalSeconds:f2} S");
                    for (int i = 0; i < _histogram.Length; i++)
                    {
                        if (_histogram[i] > 0)
                        {
                            string messageName = i switch
                            {
                                0x000C => "WM_SYSCOLORCHANGE",
                                0x000D => "WM_GETTEXT",
                                0x000E => "WM_GETTEXTLENGTH",
                                0x0014 => "WM_ERASEBKGND",
                                0x0021 => "WM_MOUSEACTIVATE",
                                0x007F => "WM_GETICON",
                                0x00AE => "WM_NCUAHDRAWCAPTION (Undocumented, according to best available source)",
                                0x0200 => "WM_MOUSEMOVE",
                                0x0201 => "WM_LBUTTONDOWN",
                                0x0202 => "WM_LBUTTONUP",
                                0x0203 => "WM_LBUTTONDBLCLK (Do second click a little slower please)",
                                0x0210 => "WM_PARENTNOTIFY",
                                0x0318 => "WM_PRINTCLIENT",
                                0xC1F0 => "WM_USER+X (App-Defined Message)",
                                _ => $"Unknown (0x{i:X4}) UNEXPECTED"
                            };

                            Debug.WriteLine($"[{_histogram[i], 5}]: 0X{i:X4} {messageName}");
                        }
                    }
                    Debug.WriteLine(string.Empty);
                }
            };
        }
        private readonly object _lock = new object();
        CancellationTokenSource? _cts = null;
        List<string> _updateScheduled = new ();
        List<string> _updateRun = new ();


        int[] _histogram = new int[0x10000];

        bool _capture = false;
        protected override void WndProc(ref Message m)
        {
            if (_capture)
            {
                _histogram[m.Msg]++;
            }
            base.WndProc(ref m);
        }

        Stopwatch? _stopwatch = null;

        // Count child control messages too.
        public bool PreFilterMessage(ref Message m)
        {
            if (_capture && FromHandle(m.HWnd) is CheckBox button)
            {
                switch (m.Msg)
                {
                    // Either way:
                    // This will be the "second" click because we weren't
                    // capturing the first time it clicked to start.
                    case 0x0201: // MouseDowm
                    case 0x0203: // MouseDoubleClick
                        _stopwatch?.Stop();
                        break;
                }
                _histogram[m.Msg]++;
            }
            return false;
        }
    }
}
