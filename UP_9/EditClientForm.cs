using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UP_9
{
    public partial class EditClientForm : Form
    {
        private string connectionString;
        private int? clientId;

        private TextBox txtFullName;
        private ComboBox cmbGender;
        private DateTimePicker dtpBirthDate;
        private TextBox txtFishingClub;
        private Button btnSave;
        private Button btnCancel;

        public EditClientForm(string connectionString, int? clientId = null)
        {
            this.connectionString = connectionString;
            this.clientId = clientId;

            InitializeComponent();
            this.Text = clientId.HasValue ? "Редактирование клиента" : "Добавление клиента";
            this.Size = new Size(500, 300);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            InitializeControls();

            if (clientId.HasValue)
            {
                LoadClientData(clientId.Value);
            }
        }

        private void InitializeControls()
        {
            int y = 20;

            // ФИО
            var lblFullName = new Label
            {
                Text = "ФИО:",
                Location = new Point(20, y),
                Size = new Size(100, 20),
                Font = new Font("Arial", 10),
                
            };
            this.Controls.Add(lblFullName);

            txtFullName = new TextBox
            {
                Location = new Point(130, y),
                Size = new Size(300, 25),
                Font = new Font("Arial", 10),
                 BackColor = ColorTranslator.FromHtml("#F7FCBD"),
            };
            this.Controls.Add(txtFullName);
            y += 40;

            // Пол
            var lblGender = new Label
            {
                Text = "Пол:",
                Location = new Point(20, y),
                Size = new Size(100, 20),
                Font = new Font("Arial", 10)
            };
            this.Controls.Add(lblGender);

            cmbGender = new ComboBox
            {
                Location = new Point(130, y),
                Size = new Size(100, 25),
                Font = new Font("Arial", 10),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbGender.Items.AddRange(new object[] { "М", "Ж" });
            cmbGender.SelectedIndex = 0;
            this.Controls.Add(cmbGender);
            y += 40;

            // Дата рождения
            var lblBirthDate = new Label
            {
                Text = "Дата рождения:",
                Location = new Point(20, y),
                Size = new Size(100, 20),
                Font = new Font("Arial", 10)
            };
            this.Controls.Add(lblBirthDate);

            dtpBirthDate = new DateTimePicker
            {
                Location = new Point(130, y),
                Size = new Size(150, 25),
                Font = new Font("Arial", 10),
                Format = DateTimePickerFormat.Short
            };
            this.Controls.Add(dtpBirthDate);
            y += 40;

            // Рыболовный клуб
            var lblFishingClub = new Label
            {
                Text = "Рыболовный клуб:",
                Location = new Point(20, y),
                Size = new Size(100, 20),
                Font = new Font("Arial", 10)
            };
            this.Controls.Add(lblFishingClub);

            txtFishingClub = new TextBox
            {
                Location = new Point(130, y),
                Size = new Size(300, 25),
                Font = new Font("Arial", 10),
                BackColor = ColorTranslator.FromHtml("#F7FCBD"),

            };
            this.Controls.Add(txtFishingClub);
            y += 60;

            // Кнопки
            btnSave = new Button
            {
                Text = "Сохранить",
                Location = new Point(130, y),
                Size = new Size(100, 35),
                BackColor = ColorTranslator.FromHtml("#022E4D"),
                ForeColor = Color.White,
                Font = new Font("Arial", 10, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            btnCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(250, y),
                Size = new Size(100, 35),
                BackColor = ColorTranslator.FromHtml("#CCCCCC"),
                ForeColor = Color.Black,
                Font = new Font("Arial", 10),
                FlatStyle = FlatStyle.Flat
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
            this.Controls.Add(btnCancel);
        }

        private void LoadClientData(int clientId)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT clients_fio, clients_gender, clients_birthdate, clients_fishingclub FROM clients WHERE clients_clientid = @id";
                    var cmd = new NpgsqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@id", clientId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            txtFullName.Text = reader["clients_fio"].ToString();
                            cmbGender.SelectedItem = reader["clients_gender"].ToString();
                            dtpBirthDate.Value = reader.GetDateTime(reader.GetOrdinal("clients_birthdate"));
                            txtFishingClub.Text = reader["clients_fishingclub"].ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных клиента: {ex.Message}");
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                MessageBox.Show("Введите ФИО клиента", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    if (clientId.HasValue)
                    {
                        // Обновление клиента
                        string sql = @"UPDATE clients SET 
                                     clients_fio = @fio,
                                     clients_gender = @gender,
                                     clients_birthdate = @birthdate,
                                     clients_fishingclub = @club
                                     WHERE clients_clientid = @id";

                        var cmd = new NpgsqlCommand(sql, conn);
                        cmd.Parameters.AddWithValue("@fio", txtFullName.Text.Trim());
                        cmd.Parameters.AddWithValue("@gender", cmbGender.SelectedItem.ToString());
                        cmd.Parameters.AddWithValue("@birthdate", dtpBirthDate.Value);
                        cmd.Parameters.AddWithValue("@club", txtFishingClub.Text.Trim());
                        cmd.Parameters.AddWithValue("@id", clientId.Value);

                        cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        // Добавление нового клиента
                        string sql = @"INSERT INTO clients 
                                     (clients_fio, clients_gender, clients_birthdate, clients_fishingclub)
                                     VALUES (@fio, @gender, @birthdate, @club)";

                        var cmd = new NpgsqlCommand(sql, conn);
                        cmd.Parameters.AddWithValue("@fio", txtFullName.Text.Trim());
                        cmd.Parameters.AddWithValue("@gender", cmbGender.SelectedItem.ToString());
                        cmd.Parameters.AddWithValue("@birthdate", dtpBirthDate.Value);
                        cmd.Parameters.AddWithValue("@club", txtFishingClub.Text.Trim());

                        cmd.ExecuteNonQuery();
                    }

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения данных: {ex.Message}");
            }
        }
    }
}

