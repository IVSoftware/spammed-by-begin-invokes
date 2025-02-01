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

> Your hypothesis that the BeginInvoke silently discards messages is reasonable, but it's a frightening hypothesis. It's hard to believe that Microsoft opened intentionally such a pit of failure for the developers to fall in. Can you think of any experiment that would reinforce this hypothesis? It's frightening to think that I can await something on the UI thread, and the await will never complete because some subsequent events evicted the completion callback of the awaited task from the memory of the application!

YES! I had to think about it a couple days, but in fact I _can_ devise such an experiment.

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
    base.WndProc(ref m);
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
                    0x00AE => "Possible Error Message",
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
~~~

**Test Result**


http://crinc.com/WebHelp/Filepro/Windows_Error_Messages_1.htm

**With `SAMPLE_SIZE=20000`**


[20000]: 0X000C WM_SYSCOLORCHANGE
[80009]: 0X000D WM_QUERYOPEN
[80009]: 0X000E WM_ERASEBKGND
[    3]: 0X0014 WM_SETCURSOR
[    1]: 0X0021 Unknown (0x0021)
[    4]: 0X007F WM_GETICON
[20000]: 0X00AE WM_ENABLE
[    1]: 0X0210 Unknown (0x0210)
[    3]: 0X0318 Unknown (Possibly App-Specific)
[10001]: 0XC1F0 WM_USER+X (App-Defined Message)

