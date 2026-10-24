namespace Aris.UI;

partial class CitizenControl
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

    #region Component Designer generated code

    /// <summary> 
    /// Required method for Designer support - do not modify 
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        lblTitle = new Label();
        lblMessage = new Label();
        SuspendLayout();
        // 
        // lblTitle
        // 
        lblTitle.AutoSize = true;
        lblTitle.Font = new Font("Segoe UI", 20.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
        lblTitle.ForeColor = Color.DarkSlateGray;
        lblTitle.Location = new Point(25, 25);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(283, 37);
        lblTitle.TabIndex = 0;
        lblTitle.Text = "Citizen Management";
        // 
        // lblMessage
        // 
        lblMessage.AutoSize = true;
        lblMessage.BackColor = Color.WhiteSmoke;
        lblMessage.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        lblMessage.ForeColor = Color.Gray;
        lblMessage.Location = new Point(28, 85);
        lblMessage.Name = "lblMessage";
        lblMessage.Size = new Size(237, 20);
        lblMessage.TabIndex = 1;
        lblMessage.Text = "This feature is under development.";
        // 
        // CitizenControl
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.WhiteSmoke;
        Controls.Add(lblMessage);
        Controls.Add(lblTitle);
        Name = "CitizenControl";
        Size = new Size(800, 500);
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label lblTitle;
    private Label lblMessage;
}
