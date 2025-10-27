using RemoteDriveClient.Services;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RemoteDriveClient.Forms
{
    public partial class LoginForm : Form
    {
        private TextBox txtUser;
        private TextBox txtPass;
        private Button btnLogin;
        private Label lblStatus;
        private IFileService fileService;

        public LoginForm()
        {
            InitializeCustomComponents();
            fileService = new MockFileService();
        }

        private void InitializeCustomComponents()
        {
            this.Text = "Авторизація – Remote Drive";
            this.Width = 360;
            this.Height = 200;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.AutoScaleMode = AutoScaleMode.Font;

            Label lblUser = new Label();
            lblUser.Text = "Логін:";
            lblUser.Left = 20; lblUser.Top = 20;

            txtUser = new TextBox();
            txtUser.Left = 20; txtUser.Top = 40; txtUser.Width = 300;

            Label lblPass = new Label();
            lblPass.Text = "Пароль:";
            lblPass.Left = 20; lblPass.Top = 70;

            txtPass = new TextBox();
            txtPass.Left = 20; txtPass.Top = 90; txtPass.Width = 300;
            txtPass.UseSystemPasswordChar = true;

            btnLogin = new Button();
            btnLogin.Left = 20; btnLogin.Top = 130;
            btnLogin.Width = 100; btnLogin.Text = "Увійти";
            btnLogin.Click += new EventHandler(this.BtnLogin_Click);

            lblStatus = new Label();
            lblStatus.Left = 140; lblStatus.Top = 135; lblStatus.Width = 180;

            this.Controls.Add(lblUser);
            this.Controls.Add(txtUser);
            this.Controls.Add(lblPass);
            this.Controls.Add(txtPass);
            this.Controls.Add(btnLogin);
            this.Controls.Add(lblStatus);
        }

        private async void BtnLogin_Click(object sender, EventArgs e)
        {
            await DoLogin();
        }

        private async Task DoLogin()
        {
            try
            {
                btnLogin.Enabled = false;
                lblStatus.Text = "Авторизація...";
                string token = await fileService.AuthenticateAsync(txtUser.Text.Trim(), txtPass.Text);
                lblStatus.Text = "Успішно";

                MainForm main = new MainForm(token, txtUser.Text.Trim(), fileService);
                main.FormClosed += delegate { this.Close(); };
                main.Show();
                this.Hide();
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Помилка: " + ex.Message;
            }
            finally
            {
                btnLogin.Enabled = true;
            }
        }
    }
}