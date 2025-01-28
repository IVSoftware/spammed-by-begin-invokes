
https://stackoverflow.com/q/79381919/5438626

I'm going to grab what you said in the comments, emphasis mine:

>_**I wanted to make the easiest way to reproduce the problem**. The real reason why I'm asking this is because I got into that kind of an issue, in the libraries I'm writing for real purposes, and they 'do a thing' and report progress with an event raise. When it's completed, an await able property is set._

Let's start with what you _should_ be doing, if you want to keep this button responsive and this update of 100000 (that is, _ten times_ the 10000 threshold) cancellable. The difference is that this doesn't load up the message queue with all 100000 posts. Rather, it's only posting one at a time and posts it at the end.

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
~~~

___

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



**Observations**

- As reported, SAMPLE_SIZE of 10000 _works_. We _will_ see the Done message popup.
- As reported, SAMPLE_SIZE of 10001 _breaks it_. We _will not_ see the Done message popup.

And with SAMPLE_SIZE of 100000
1. The button is unresponsive until the message queue has run all the way through.
2. This means that the button (checkbox) cannot be used to cancel the task.
3. The Done message, of course, still doesn't ever fire, due to whatever happens to the captured synchronization context at 10000 + 1.
4. But interestingly, the UI _will_ recover from this if one lets it run til the end.







