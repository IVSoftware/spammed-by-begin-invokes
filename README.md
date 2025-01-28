
https://stackoverflow.com/q/79381919/5438626

I'm going to grab what you said in the comments, emphasis mine:

>_**I wanted to make the easiest way to reproduce the problem**. The real reason why I'm asking this is because I got into that kind of an issue, in the libraries I'm writing for real purposes, and they 'do a thing' and report progress with an event raise. When it's completed, an await able property is set._

Let's start with what you _should_ be doing, if you want to keep this button responsive and this update of 100000 (that is, _ten times_ the 10000 threshold) cancellable.

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

~~~





