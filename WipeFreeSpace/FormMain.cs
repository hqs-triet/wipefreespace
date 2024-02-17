using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace WipeFreeSpace
{
    public partial class FormMain : Form
    {
        private bool m_stop = true;
        const int BLOCK_SIZE = 256;
        DriveInfo m_drive = null;
        public FormMain()
        {
            InitializeComponent();
        }

        private void btnBrowseFolder_Click(object sender, EventArgs e)
        {
            if(this.selectFolder.ShowDialog() == DialogResult.OK)
            {
                this.txtBaseDir.Text = this.selectFolder.SelectedPath;
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if(!Directory.Exists(this.txtBaseDir.Text))
            {
                MessageBox.Show("The base directory does not exist!");
                return;
            }
            if(string.IsNullOrEmpty(this.txtFileName.Text))
            {
                MessageBox.Show("The file name is empty!");
                return;
            }
            string fFullPath = Path.Combine(this.txtBaseDir.Text, this.txtFileName.Text);
            if (File.Exists(fFullPath))
                if (MessageBox.Show("The target file existed. Do you want to overwrite?", "Confirmation", MessageBoxButtons.YesNo) == DialogResult.No)
                    return;

            FileInfo f = new FileInfo(fFullPath);
            m_drive = new DriveInfo(Path.GetPathRoot(f.FullName));

            Thread thr = new Thread(new ThreadStart(() =>
            {
                byte[] arrData = GenerateBlock();
                DateTime timeStart = DateTime.Now;
                double totalSize = 0, sizeCountInOneSecond = 0, totalWrite = 0;
                try
                {
                    using (var fs = new FileStream(fFullPath, FileMode.Create))
                    {
                        DateTime timeCountStart = DateTime.Now;
                        while (!m_stop)
                        {
                            fs.Write(arrData, 0, BLOCK_SIZE);
                            totalWrite++;
                            totalSize += BLOCK_SIZE;
                            sizeCountInOneSecond += BLOCK_SIZE;

                            // Update parameters
                            if (DateTime.Now.Subtract(timeCountStart).TotalSeconds >= 1)
                            {
                                //Console.WriteLine("================= Total write:  " + totalWrite);
                                //Console.WriteLine("================= sizeCountInOneSecond:  " + sizeCountInOneSecond);
                                Invoke(new Action(() =>
                                {
                                    this.lblSize.Text = MakeLabelSize(totalSize);
                                    this.lblSpeed.Text = MakeLabelSpeed(sizeCountInOneSecond);

                                    TimeSpan diffTime = DateTime.Now.Subtract(timeStart);
                                    this.lblTime.Text = diffTime.Hours.ToString().PadLeft(2, '0') + ":" +
                                                        diffTime.Minutes.ToString().PadLeft(2, '0') + ":" +
                                                        diffTime.Seconds.ToString().PadLeft(2, '0');
                                    if (sizeCountInOneSecond > 0)
                                    {
                                        long totalSeconds = (long)(m_drive.TotalFreeSpace / sizeCountInOneSecond);
                                        var tsTimeLeft = TimeSpan.FromSeconds(totalSeconds);
                                        this.lblTimeLeft.Text = tsTimeLeft.Hours.ToString().PadLeft(2, '0') + ":" +
                                                            tsTimeLeft.Minutes.ToString().PadLeft(2, '0') + ":" +
                                                            tsTimeLeft.Seconds.ToString().PadLeft(2, '0');
                                    }

                                }));
                                sizeCountInOneSecond = 0;
                                timeCountStart = DateTime.Now;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (m_drive.TotalFreeSpace < BLOCK_SIZE)
                        Invoke(new Action(() =>
                        {
                            MessageBox.Show("Done!");
                        }));
                    else
                        Invoke(new Action(() =>
                        {
                            MessageBox.Show(ex.Message);
                        }));

                }

                Invoke(new Action(() =>
                {
                    this.btnStart.Enabled = true;
                    this.btnStop.Enabled = false;
                    this.progressMain.Visible = false;
                    this.txtBaseDir.Enabled = true;
                    this.txtFileName.Enabled = true;
                    this.btnBrowseFolder.Enabled = true;
                }));
            }));
            m_stop = false;
            this.btnStart.Enabled = false;
            this.btnStop.Enabled = true;
            this.progressMain.Visible = true;
            this.txtBaseDir.Enabled = false;
            this.txtFileName.Enabled = false;
            this.btnBrowseFolder.Enabled = false;
            thr.Start();
        }
        private string MakeLabelSpeed(double count)
        {
            if (count < 1024)
                return count + " b/s";
            
            if (count < 1024 * 1024)
                return (count / 1024).ToString("0.00") + " kb/s";

            if (count < 1024 * 1024 * 1024)
                return (count / 1024 / 1024).ToString("0.00") + " mb/s";
            
            if (count < (double)1024 * 1024 * 1024 * 1024)
                return (count / 1024 / 1024 / 1024).ToString("0.00") + " gb/s";
            
            return "" + count;
        }
        private string MakeLabelSize(double size)
        {
            if (size < 1024)
                return size + " byte(s)";

            if (size < 1024 * 1024)
                return (size / 1024).ToString("0.00") + " kb(s)";

            if (size < 1024 * 1024 * 1024)
                return (size / 1024 / 1024).ToString("0.00") + " mb(s)";

            if (size < (double)1024 * 1024 * 1024 * 1024)
                return (size / 1024 / 1024 / 1024).ToString("0.00") + " gb(s)";

            return "";
        }
        private byte[] GenerateBlock()
        {
            byte[] arrData = new byte[BLOCK_SIZE];
            for(int idx = 0; idx < BLOCK_SIZE; idx++)
            {
                arrData[idx] = 0;
            }
            return arrData;
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            this.lblSize.Text = string.Empty;
            this.lblSpeed.Text = string.Empty;
            this.lblTime.Text = string.Empty;
            this.progressMain.Visible = false;
            this.lblTimeLeft.Text = string.Empty;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            m_stop = true;
        }
    }
}
