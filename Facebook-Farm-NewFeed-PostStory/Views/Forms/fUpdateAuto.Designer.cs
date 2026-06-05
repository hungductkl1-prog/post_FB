namespace Facebook_Farm_NewFeed_PostStory.Views.Forms
{
    partial class fUpdateAuto
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            divider1 = new AntdUI.Divider();
            progress5 = new AntdUI.Progress();
            SuspendLayout();
            // 
            // divider1
            // 
            divider1.Dock = DockStyle.Top;
            divider1.Font = new Font("Microsoft YaHei UI", 10F);
            divider1.LocalizationText = "Progress.{id}";
            divider1.Location = new Point(30, 30);
            divider1.Name = "divider1";
            divider1.Orientation = AntdUI.TOrientation.Left;
            divider1.Size = new Size(249, 28);
            divider1.TabIndex = 2;
            divider1.Text = "Đang tải vui lòng chờ...";
            // 
            // progress5
            // 
            progress5.Dock = DockStyle.Fill;
            progress5.Font = new Font("Microsoft YaHei UI Light", 16F);
            progress5.Loading = true;
            progress5.Location = new Point(30, 58);
            progress5.Name = "progress5";
            progress5.Padding = new Padding(10);
            progress5.Radius = 5;
            progress5.Shape = AntdUI.TShapeProgress.Circle;
            progress5.Size = new Size(249, 143);
            progress5.TabIndex = 12;
            progress5.ValueRatio = 0.5F;
            // 
            // fUpdateAuto
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(309, 231);
            Controls.Add(progress5);
            Controls.Add(divider1);
            FormBorderStyle = FormBorderStyle.None;
            MaximumSize = new Size(448, 334);
            Name = "fUpdateAuto";
            Padding = new Padding(30);
            StartPosition = FormStartPosition.CenterScreen;
            ResumeLayout(false);
        }

        #endregion

        private AntdUI.Divider divider1;
        private AntdUI.Progress progress5;
    }
}