
https://stackoverflow.com/q/79381919/5438626

I'm going to grab what you said in the comments, emphasis mine:

>_I want to also point out that I'm fully aware this particular example doesn't really make sense, and its not really supposed to, **I wanted to make the easiest way to reproduce the problem**. The real reason why I'm asking this is because I got into that kind of an issue, in the libraries I'm writing for real purposes, and they 'do a thing' and report progress with an event raise. When it's completed, an await able property is set._

And if I'm following, the "real" objective (in broad terms) is to make 10,000 + updates to the UI while keeping the UI responsive the entire time. So first of all, you're loading up the message queue and it matters not whether you use a background thread to do that, because loading it up happens almost instantaneously. But what I want to emphasize is that this snippet leaves a non-responsive UI:

- Click the button One Time.
- Click [X] One Time to "close the app".
- **Now we wait maybe ten seconds or so.** The app _will close_ but not without exhausting the 250,000 messages we posted in the message queue.

~~~
public partial class MainForm : Form
{
    const int SAMPLE_SIZE = 250000;
    public MainForm()
    {
        InitializeComponent();
        buttonUpdate.Text = $"Update {SAMPLE_SIZE}x";
        buttonUpdate.Click += async(sender, e) =>
        {
            await Task.Run(() =>
            {
                for (int i = 0; i < SAMPLE_SIZE; i++)
                {
                    BeginInvoke(() =>
                    {
                        // Perform a real update on the UI.
                        Text = i.ToString();
                    });
                }
            });
            MessageBox.Show("Done");
        };
        FormClosing += (sender, e) =>
        {
            if(DialogResult.Cancel == MessageBox.Show("App is Closing", "Alert", MessageBoxButtons.OKCancel))
            {
                e.Cancel = true;
            }
        };
    }
}
~~~




