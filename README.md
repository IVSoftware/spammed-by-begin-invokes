I'm going to pull from your comments to set the context here (emphasis mine):

>_**I wanted to make the easiest way to reproduce the problem**. The real reason why I'm asking this is because I got into that kind of an issue, in the libraries I'm writing for real purposes, and they 'do a thing' and report progress with an event raise. When it's completed, an await able property is set._

So, I'd like to first address the real problem and suggest what you _could_ do to keep the UI and button responsive which allows this testbench update of 100,000 (that is, ten times the 10,000 threshold) to be _cancellable at any moment_. The key difference is that by awaiting each `BeginInvoke` we can avoid flooding the message queue with all 100,000 posts at once. Instead, it processes one update at a time and posts each at the end.

___

**Solution: Awaiting BeginInvoke**

[![test bench][1]][1]

Writes to the title bar as a stand-in for "some UI update" like the buttons mentioned in the post.

___
~~~
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
                        // You could also just iAsyncResult.AsyncWaitHandle.WaitOne() but this seems more responsive.
                        await Task.Run(()=>iAsyncResult.AsyncWaitHandle.WaitOne());
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
    CancellationTokenSource? _cts = null;
}
~~~

___

**PREVIOUSLY: Without Awaiting BeginInvoke**

Now, take away the `await Task.Run(() => iAsyncResult.AsyncWaitHandle.WaitOne())`.

~~~
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
                await Task.Run(() =>
                {
                    for (int i = 0; i < SAMPLE_SIZE; i++)
                    {
                        if (_cts.Token.IsCancellationRequested) return;
                        var iAsyncResult = BeginInvoke(() =>
                        {
                            // Perform a real update on the UI.
                            Text = i.ToString();
                        });
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
~~~
### Observations

When the `BeginInvoke` is _not_ awaited in the loop:

- **For SAMPLE_SIZE = 10,000:**
  - It works; the "Done" message pops up. This is consistent with the behavior reported in the post.

- **For SAMPLE_SIZE = 10,000 + 1:**
  - It breaks; the "Done" message never appears. Also consistent with the behavior reported in the post.

- **For SAMPLE_SIZE = 100,000:**
  1. The UI is unresponsive (e.g. cannot [x] close app) until the message queue processes all 100,000 updates.
  2. The unresponsive button (checkbox) therefore cannot be used to cancel the task.
  3. The "Done" message still doesn’t fire, likely due to what happens to the captured synchronization context at 10,000 + 1 and beyond.
  4. Interestingly, the UI _does recover_ if allowed to run until completion.

___

**Summary Conclusion**

In the original code, the message queue is being flooded with ALL the pending UI updates. This isn't what we want, and it "matters not" whether the "act of flooding it" happens on background worker thread, because loading it up happens almost instantaneously. And until ALL of those messages are exhausted, we're not going to get even a mouse click to respond. The solution is to await the `BeginInvoke` so that there is only one "UI Update" task in the queue at a time.


  [1]: https://i.sstatic.net/65YD7LeB.png