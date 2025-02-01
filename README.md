Running the repro code in the post, what stuck me is how this smells like a hardware overrun and I asked myself how this could be. In the process of experimenting I, too, noticed how repeatable and stable the 10000 threshold is, not "moving around" the way certain race conditions might. The idea becomes finding the canonical source of it, which I believe I found in the registry at: 

`Computer\HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Windows` 

where the key is:

`USERPostMessageLimit`.

This seems to lie at the heart of the matter. I went ahead and edited it to 20000, restarted the PC, and confirmed in the repro code that the threshold tracked the new value.

The nature of this being an OS value in the registry has me draw some preliminary conclusions:

- The behavior would be consistent with an `alloc` of a block of unmanaged physical memory. For all intents and purposes, this "is" a hardware register. 
- It's a fair and reasonable assumption that `USERPostMessageLimit` would be a queue, not a stack, and that if it were overrun then the head and tail of this circular buffer could either overlap or more likely just start throwing messages in the bit bucket (the overlap is disallowed). 
- So, a plausible explanation is that this buffer fills up so quickly that the earlier messages haven't dequeued and are therefore irretrievably lost.
- This jibes with my observations, that even though the intermittent "debug message" was not called in the case of exceeding the limit, the app seemed to remain viable and healthy in all other respects.
___

My test engineering spidey senses tell me there is some better way to observe this timing, but I'm still ironing out the specifics.




___
@ Theodor Zoulias:

> Your hypothesis that the BeginInvoke silently discards messages is reasonable, but it's a frightening hypothesis. It's hard to believe that Microsoft opened intentionally such a pit of failure for the developers to fall in. Can you think of any experiment that would reinforce this hypothesis?

YES! I had to think about it a couple days, but in fact I _can_ devise such an experiment. We just have to hook the `WndProc` and capture a histogram of the messages in the sample period. **NOTE** The act of observation WILL change the thng observed. I will slow down the flooding of the queue and may result in an axtra 1 or 2 WM_USER _entries.

___

Hypothesis: 

1. THIS WOULD BE CONSISTENT WITH GOOD OS DESIGN:

"Limit the extent that user messages (specifically) flooding the message queue can impact the stabililty of the process."

2. The limit for _USER_ messages is set in the registry:

`Computer\HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Windows : USERPostMessageLimit`

3. To eliminate "tiny" timing variations, we will "greatly" exceed this limit e.g. N = 20000.

4. *If a Histogram of WM_ message IDs is captured in `WndProc`:**
 
- We expect to be able to identify the message that results from `BeginInvoke` because of its high count.
- We expect to see the count of WM_USER to be throttled right around `USERPostMessageLimit`.

___

**Histogram**

~~~
int[] _histogram = new int[0x10000];
protected override void WndProc(ref Message m)
{
    if (_capture)
    {
        base.WndProc(ref m);
    }
    _histogram[m.Msg]++;
}
~~~

**Test Routine**

~~~
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
~~~
~~~

___

**Test Result**


~~~plaintext
With SAMPLE_SIZE=20000

[20000]: 0X000C WM_SYSCOLORCHANGE
[80006]: 0X000D WM_GETTEXT
[80006]: 0X000E WM_GETTEXTLENGTH
[    2]: 0X0014 WM_ERASEBKGND
[    1]: 0X0021 WM_MOUSEACTIVATE
[    3]: 0X007F WM_GETICON
[20000]: 0X00AE WM_NCUAHDRAWCAPTION (Undocumented, according to best available source)
[    1]: 0X0210 WM_PARENTNOTIFY
[    2]: 0X0318 WM_PRINTCLIENT
[10001]: 0XC1F0 WM_USER+X (App-Defined Message)
~~~

---

**Key Takeaways**

1. **`WM_USER+X` Messages Are Throttled at ~10000**
   - The count aligns **almost exactly** with `USERPostMessageLimit`, confirming **Windows enforces a cap on user-defined messages.**
   - **Any excess messages were discarded by Windows**—not just queued.

2. **System Messages (`WM_SYSCOLORCHANGE`, `WM_ERASEBKGND`, etc.) Are NOT Throttled**
   - Despite message flooding, **Windows continued processing core system messages.**
   - This supports the hypothesis that **Windows prioritizes system messages over user-generated ones.**

___


@ Theodor Zoulias:

> It's frightening to think that I can await something on the UI thread, and the await will never complete because some subsequent events evicted the completion callback of the awaited task from the memory of the application!

It's probably not as frightening as you think. 

- **First:** it's hard to imagine a real-world scenario that would require 10000+ UI updates inside a couple of seconds. Even with a `Progress` flow of 10000+ updates, you're likely going to use the modulo operator to throttle the `ProgressBar` updates. So _show me your use case for that_.

- **Second:** Your UI is unresponsive in the meantime and you're going to notice this.

Here is a second experiment that measures the unresponsiveness (it's what I was trying to show before).

___

**Second Hypothesis**

If the button is clicked TWICE, the second click won't respond until ALL 10000+ BeginInvokes have cycled through!!! 

This is why the _solution_ (if you really have to do this in the first place) would be to await individual BeginInvokes in the loop, so that new messages like WM_LBUTTONDOWN_ will be interspersed.

**Minor Changes to Test Code**

Implement `IMessageFilter` in order to be able to detect the mouse messages in the child control.

___

~~~plaintext
With SAMPLE_SIZE=100000

The SECOND mouse click FINALLY comes to front of queue @ 6.61 S
[100000]: 0X000C WM_SYSCOLORCHANGE
[400006]: 0X000D WM_GETTEXT
[400006]: 0X000E WM_GETTEXTLENGTH
[    2]: 0X0014 WM_ERASEBKGND
[    1]: 0X0021 WM_MOUSEACTIVATE
[100000]: 0X00AE WM_NCUAHDRAWCAPTION (Undocumented, according to best available source)
[    1]: 0X0200 WM_MOUSEMOVE
[    2]: 0X0201 WM_LBUTTONDOWN
[    2]: 0X0202 WM_LBUTTONUP
[    1]: 0X0210 WM_PARENTNOTIFY
[    2]: 0X0318 WM_PRINTCLIENT
[10001]: 0XC1F0 WM_USER+X (App-Defined Message)
~~~

**Key Takeaways**

1. **UI Thread Saturation Blocks Interactive Events**
   - The **second mouse click was queued behind all `BeginInvoke` calls** and only processed **6.61 seconds later**.
   - This **confirms UI thread starvation** under high-load scenarios.

2. **Mouse Messages (`WM_LBUTTONDOWN`) Are Not Prioritized**
   - **Mouse clicks were ignored until the queue cleared**.
   - This confirms that **Windows does NOT prioritize user interaction over message queue floods**.


**Updated Histogram Code**

~~~
public partial class MainForm : Form, IMessageFilter
{
    const int SAMPLE_SIZE = 20000;
    public MainForm()
    {
        InitializeComponent();
        // Hook the message filter
        Application.AddMessageFilter(this);
        Disposed += (sender, e) => Application.RemoveMessageFilter(this);
        .
        .
        .
    }

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
        }
        return false;
    }
    
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
            };
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
~~~

