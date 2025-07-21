using System;
using System.Windows.Forms;

namespace CorePanda
{
    public class SettingsForm : Form
    {
        private readonly CheckBox chkShowCpu;
        private readonly CheckBox chkShowTemp;
        private readonly Button btnSave;

        private readonly Form1 mainForm;

        public SettingsForm(Form1 parentForm)
        {
            this.Text = "CorePanda Settings";
            this.Size = new System.Drawing.Size(250, 180);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            mainForm = parentForm;

            chkShowCpu = new CheckBox()
            {
                Text = "Show CPU Usage",
                Checked = mainForm.ShowCpuUsage,
                Left = 20,
                Top = 20,
                AutoSize = true
            };

            chkShowTemp = new CheckBox()
            {
                Text = "Show CPU Temperature",
                Checked = mainForm.ShowCpuTemp,
                Left = 20,
                Top = 50,
                AutoSize = true
            };

            btnSave = new Button()
            {
                Text = "Save",
                Left = 70,
                Top = 90,
                Width = 100
            };
            btnSave.Click += BtnSave_Click;

            this.Controls.Add(chkShowCpu);
            this.Controls.Add(chkShowTemp);
            this.Controls.Add(btnSave);
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            mainForm.ShowCpuUsage = chkShowCpu.Checked;
            mainForm.ShowCpuTemp = chkShowTemp.Checked;
            this.Close();
        }
    }
}
