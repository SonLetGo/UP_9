using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using UP_9.Properties;

namespace UP_9
{
    public partial class Form1 : Form
    {
        private readonly string connectionString = "Host=localhost;Port=5432;Database=UP_Recruitment;Username=postgres;Password=12345";
        // UI element declarations
        private PictureBox pictureBoxLogo;
        private Label labelTitle;
        private TextBox txtLogin;
        private TextBox txtPassword;
        private Button btnLogin;
        private Panel panelLogin;

        // Layout constants for clarity and alignment
        private const int FORM_WIDTH = 400;
        private const int FORM_HEIGHT = 350;
        private const int LOGO_SIZE = 100;
        private const int LOGO_TOP = 20;
        private const int TITLE_HEIGHT = 30;
        private const int TITLE_TOP = 130;
        private const int PANEL_WIDTH = 300;
        private const int PANEL_HEIGHT = 150;
        private const int PANEL_TOP = 160;
        private const int TEXTBOX_WIDTH = 200;
        private const int TEXTBOX_HEIGHT = 30;
        private const int LOGIN_TOP = 20;
        private const int PASSWORD_TOP = 60;
        private const int BUTTON_TOP = 100;
        private const int BUTTON_WIDTH = 200;
        private const int BUTTON_HEIGHT = 40;
        private const int MARGIN = 50;

        public Form1()
        {
            // Initialize controls and apply styling
            InitializeControls();
            // Load logo
            try
            {
                if (pictureBoxLogo != null && Resources.logo != null)
                {
                    using (MemoryStream stream = new MemoryStream(Resources.logo))
                    {
                        pictureBoxLogo.Image = Image.FromStream(stream); // Convert byte[] to Image
                    }
                }
                else
                {
                    MessageBox.Show("Ресурс логотипа (logo) не найден в Properties.Resources.",
                        "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки логотипа: {ex.Message}\nПроверьте, что logo.png добавлен в ресурсы проекта.",
                    "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            // Load form icon from resources
            try
            {
                if (Resources.logoFish != null)
                {
                    using (MemoryStream stream = new MemoryStream(Resources.logoFish))
                    {
                        this.Icon = new Icon(stream); // Convert byte[] to Icon
                    }
                }
                else
                {
                    MessageBox.Show("Ресурс иконки (favicon) не найден в Properties.Resources.",
                        "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки иконки: {ex.Message}\nПроверьте, что favicon.ico добавлен в ресурсы проекта.",
                    "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void InitializeControls()
        {
            // Form setup
            this.Text = "Центр занятости «Рекрутер»";
            this.ClientSize = new Size(FORM_WIDTH, FORM_HEIGHT);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Times New Roman", 12);
            this.BackColor = ColorTranslator.FromHtml("#FFFFFF");

            // Logo
            pictureBoxLogo = new PictureBox
            {
                Size = new Size(LOGO_SIZE, LOGO_SIZE),
                Location = new Point((FORM_WIDTH - LOGO_SIZE) / 2, LOGO_TOP),
                SizeMode = PictureBoxSizeMode.StretchImage
            };

            // Title
            labelTitle = new Label
            {
                Text = "Авторизация",
                Size = new Size(FORM_WIDTH - 2 * MARGIN, TITLE_HEIGHT),
                Location = new Point(MARGIN, TITLE_TOP),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Login panel
            panelLogin = new Panel
            {
                Size = new Size(PANEL_WIDTH, PANEL_HEIGHT),
                Location = new Point((FORM_WIDTH - PANEL_WIDTH) / 2, PANEL_TOP),
                BackColor = ColorTranslator.FromHtml("#F7FCBD")
            };

            // Login TextBox
            txtLogin = new TextBox
            {
                Size = new Size(TEXTBOX_WIDTH, TEXTBOX_HEIGHT),
                Location = new Point((PANEL_WIDTH - TEXTBOX_WIDTH) / 2, LOGIN_TOP),
                Text = "Логин",
                MaxLength = 50
            };
            txtLogin.GotFocus += (s, e) => { if (txtLogin.Text == "Логин") txtLogin.Text = ""; };
            txtLogin.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(txtLogin.Text)) txtLogin.Text = "Логин"; };
            ToolTip loginToolTip = new ToolTip();
            loginToolTip.SetToolTip(txtLogin, "Введите ваш логин");

            // Password TextBox
            txtPassword = new TextBox
            {
                Size = new Size(TEXTBOX_WIDTH, TEXTBOX_HEIGHT),
                Location = new Point((PANEL_WIDTH - TEXTBOX_WIDTH) / 2, PASSWORD_TOP),
                Text = "Пароль",
                UseSystemPasswordChar = true,
                MaxLength = 50
            };
            txtPassword.GotFocus += (s, e) => { if (txtPassword.Text == "Пароль") txtPassword.Text = ""; };
            txtPassword.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(txtPassword.Text)) txtPassword.Text = "Пароль"; };
            ToolTip passwordToolTip = new ToolTip();
            passwordToolTip.SetToolTip(txtPassword, "Введите ваш пароль");

            // Login Button
            btnLogin = new Button
            {
                Text = "Войти",
                Size = new Size(BUTTON_WIDTH, BUTTON_HEIGHT),
                Location = new Point((PANEL_WIDTH - BUTTON_WIDTH) / 2, BUTTON_TOP),
                BackColor = ColorTranslator.FromHtml("#022E4D"),
                ForeColor = Color.White
            };
            btnLogin.Click += new EventHandler(BtnLogin_Click);

            // Add controls to panel
            panelLogin.Controls.Add(txtLogin);
            panelLogin.Controls.Add(txtPassword);
            panelLogin.Controls.Add(btnLogin);

            // Add controls to form
            this.Controls.Add(pictureBoxLogo);
            this.Controls.Add(labelTitle);
            this.Controls.Add(panelLogin);
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            // Validate input fields
            if (string.IsNullOrWhiteSpace(txtLogin.Text) || txtLogin.Text == "Логин" ||
                string.IsNullOrWhiteSpace(txtPassword.Text) || txtPassword.Text == "Пароль")
            {
                MessageBox.Show("Заполните все поля: логин и пароль.",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (txtLogin.Text.Length > 50 || txtPassword.Text.Length > 50)
            {
                MessageBox.Show("Логин и пароль не должны превышать 50 символов.",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Attempt authentication
            string fullName = null;
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    // Query to check login and password, retrieving full_name
                    using (var command = new NpgsqlCommand("SELECT full_name FROM agents WHERE login = @Login AND password = @Password", connection))
                    {
                        command.Parameters.AddWithValue("Login", txtLogin.Text);
                        command.Parameters.AddWithValue("Password", txtPassword.Text);
                        var result = command.ExecuteScalar();
                        if (result != null)
                        {
                            fullName = result.ToString();
                        }
                    }
                }

                if (fullName != null)
                {

                    // Navigate to MainForm on success
                    var mainform = new MainForm(); // assumes mainform exists
                    mainform.ShowDialog();
                    this.Close();
                }
                else
                {
                    // Display authentication failure
                    MessageBox.Show("Неверный логин или пароль. Проверьте введенные данные.",
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                // Display database error to user
                MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}\nУбедитесь, что PostgreSQL запущен и настройки подключения верны.",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

}
