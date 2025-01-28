
using System.Diagnostics;
using System.Windows.Forms.VisualStyles;

namespace spammed_by_begin_invokes
{
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
}
