
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
                        _capture = true;
                    }
                    await Task.Run(() =>
                    {
                        for (int i = 1; i <= SAMPLE_SIZE; i++)
                        {
                            int captureN = i;
                            BeginInvoke(() =>
                            {
                                // Perform a real update on the UI.
                                Text = captureN.ToString();
                            });
                        }
                    });
                    lock (_lock)
                    {
                        _capture = false;
                    }
                    BeginInvoke(()=>buttonUpdate.Checked = false);
                }
                else
                {
                    lock (_lock)
                    {
                        _capture = false;
                    }
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
        List<string> _updateScheduled = new ();
        List<string> _updateRun = new ();


        int[] _histogram = new int[0x10000];

        bool _capture = false;
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (_capture)
            {
                _histogram[m.Msg]++;
            }
        }
    }
}
