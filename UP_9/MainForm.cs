using Npgsql;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace UP_9
{
    public partial class MainForm : Form
    {
        private string connectionString = "Host=localhost;Username=postgres;Password=12345;Database=UP_FishClub";
        private ComboBox clubComboBox;
        private DateTimePicker datePicker;
        private Panel mainPanel;
        private Button addButton;
        private Panel selectedPanel = null;
        public MainForm()
        {
            InitializeComponent();
            this.Text = "Рыболовный клуб - Учет посещений";
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = ColorTranslator.FromHtml("#F0F8FF");

            InitializeControls();
            LoadClubData();
            LoadVisitorData();
        }

        private void InitializeControls()
        {
            // Панель для фильтров
            var filterPanel = new Panel
            {
                Location = new Point(20, 20),
                Size = new Size(this.ClientSize.Width - 40, 60),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(filterPanel);

            // Выбор клуба
            var clubLabel = new Label
            {
                Text = "Клуб:",
                Location = new Point(10, 20),
                Size = new Size(50, 20),
                Font = new Font("Arial", 10)
            };
            filterPanel.Controls.Add(clubLabel);

            clubComboBox = new ComboBox
            {
                Location = new Point(70, 15),
                Size = new Size(200, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Arial", 10)
            };
            clubComboBox.SelectedIndexChanged += (s, e) => LoadVisitorData();
            filterPanel.Controls.Add(clubComboBox);

            // Выбор даты
            var dateLabel = new Label
            {
                Text = "Дата:",
                Location = new Point(300, 20),
                Size = new Size(50, 20),
                Font = new Font("Arial", 10)
            };
            filterPanel.Controls.Add(dateLabel);

            datePicker = new DateTimePicker
            {
                Location = new Point(360, 15),
                Size = new Size(150, 30),
                Font = new Font("Arial", 10),
                Format = DateTimePickerFormat.Short
            };
            datePicker.ValueChanged += (s, e) => LoadVisitorData();
            filterPanel.Controls.Add(datePicker);

            // Основная панель для данных
            mainPanel = new Panel
            {
                Location = new Point(20, 100),
                Size = new Size(this.ClientSize.Width - 40, this.ClientSize.Height - 180),
                AutoScroll = true,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            };
            this.Controls.Add(mainPanel);

            // Кнопка добавления
            addButton = new Button
            {
                Text = "Добавить клиента",
                Location = new Point(20, this.ClientSize.Height - 70),
                Size = new Size(200, 40),
                BackColor = ColorTranslator.FromHtml("#022E4D"),
                ForeColor = Color.White,
                Font = new Font("Arial", 10, FontStyle.Bold),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                FlatStyle = FlatStyle.Flat
            };
            addButton.FlatAppearance.BorderSize = 0;
            addButton.Click += AddButton_Click;
            this.Controls.Add(addButton);
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            var editForm = new EditClientForm(connectionString);
            if (editForm.ShowDialog() == DialogResult.OK)
            {
                LoadVisitorData(); // Обновляем список после добавления
            }
        }

        private void LoadClubData()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT DISTINCT clients_fishingclub FROM clients ORDER BY clients_fishingclub";
                    var cmd = new NpgsqlCommand(sql, conn);

                    clubComboBox.Items.Add("Все клубы");

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            clubComboBox.Items.Add(reader["clients_fishingclub"].ToString());
                        }
                    }

                    clubComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки клубов: {ex.Message}");
            }
        }

        private void LoadVisitorData()
        {
            mainPanel.Controls.Clear();
            selectedPanel = null;

            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"
                        SELECT 
                            c.clients_clientid,
                            c.clients_fio,
                            EXTRACT(YEAR FROM AGE(c.clients_birthdate)) as age,
                            c.clients_fishingclub,
                            MAX(v.visits_checkindate) as last_visit_date,
                            COUNT(v.visits_visitid) as visit_count,
                            COALESCE(SUM(vf.visitfish_quantitykg * 
                                CASE 
                                    WHEN EXTRACT(MONTH FROM v.visits_checkindate) = 9 THEN p.pricelist_priceperkgseptember
                                    ELSE p.pricelist_priceperkgoctober
                                END), 0) as total_amount
                        FROM clients c
                        LEFT JOIN visits v ON c.clients_clientid = v.visits_clientid
                        LEFT JOIN visitfish vf ON v.visits_visitid = vf.visitfish_visitid
                        LEFT JOIN pricelist p ON vf.visitfish_fishid = p.pricelist_fishid
                        WHERE (@club = 'Все клубы' OR c.clients_fishingclub = @club)
                        
                        GROUP BY c.clients_clientid, c.clients_fio, c.clients_birthdate, c.clients_fishingclub
                        ORDER BY c.clients_fio";

                    var cmd = new NpgsqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@club", clubComboBox.SelectedItem.ToString());
                    cmd.Parameters.AddWithValue("@date", datePicker.Checked ? datePicker.Value : (object)DBNull.Value);

                    using (var reader = cmd.ExecuteReader())
                    {
                        int yPos = 20;
                        int panelWidth = mainPanel.ClientSize.Width - 40;

                        while (reader.Read())
                        {
                            var clientId = reader.GetInt32(0);
                            var fullName = reader.GetString(1);
                            var age = reader.GetInt32(2);
                            var club = reader.GetString(3);
                            var lastVisit = reader.IsDBNull(4) ? "Нет визитов" : reader.GetDateTime(4).ToShortDateString();
                            var visitCount = reader.GetInt32(5);
                            var totalAmount = reader.GetDecimal(6);
                            var discount = CalculateDiscount(totalAmount);

                            // Основная панель клиента
                            var clientPanel = new Panel
                            {
                                Location = new Point(20, yPos),
                                Size = new Size(panelWidth, 120),
                                BackColor = ColorTranslator.FromHtml("#F7FCBD"),
                                BorderStyle = BorderStyle.FixedSingle,
                                Tag = clientId,
                                Cursor = Cursors.Hand
                            };

                            // Левая часть - данные клиента
                            var leftPanel = new Panel
                            {
                                Location = new Point(10, 10),
                                Size = new Size(panelWidth / 2 - 20, 100),
                                Tag = clientId,
                                BackColor = Color.White
                            };

                            leftPanel.Controls.Add(CreateLabel($"ФИО: {fullName}", 0, 0, leftPanel.Width));
                            leftPanel.Controls.Add(CreateLabel($"Возраст: {age}", 0, 25, leftPanel.Width));
                            leftPanel.Controls.Add(CreateLabel($"Клуб: {club}", 0, 50, leftPanel.Width));
                            leftPanel.Controls.Add(CreateLabel($"Последний визит: {lastVisit}", 0, 75, leftPanel.Width));

                            // Правая часть - расчеты
                            var rightPanel = new Panel
                            {
                                Location = new Point(panelWidth / 2 + 10, 10),
                                Size = new Size(panelWidth / 2 - 20, 100),
                                BackColor = Color.White
                            };

                            rightPanel.Controls.Add(CreateLabel($"Количество визитов: {visitCount}", 0, 0, rightPanel.Width));
                            rightPanel.Controls.Add(CreateLabel($"Общая сумма: {totalAmount:C}", 0, 25, rightPanel.Width));
                            rightPanel.Controls.Add(CreateLabel($"Скидка: {discount}%", 0, 50, rightPanel.Width,
                                discount > 0 ? Color.Green : Color.Black));

                            // Рассчитанная скидка
                            var discountAmount = totalAmount * discount / 100;
                            rightPanel.Controls.Add(CreateLabel($"Сумма скидки: {discountAmount:C}", 0, 75, rightPanel.Width));

                            clientPanel.Controls.Add(leftPanel);
                            clientPanel.Controls.Add(rightPanel);
                            clientPanel.Click += (s, e) => EditClient(clientPanel);
                            leftPanel.Click += (s, e) => EditClient(clientPanel);
                            rightPanel.Click += (s, e) => EditClient(clientPanel);


                            mainPanel.Controls.Add(clientPanel);
                            yPos += 130;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void EditClient(Panel clientPanel)
        {
            // Снимаем выделение с предыдущей панели
            if (selectedPanel != null)
            {
                selectedPanel.BackColor = Color.White;
            }

            // Выделяем новую панель
            clientPanel.BackColor = ColorTranslator.FromHtml("#E6F7FF");
            selectedPanel = clientPanel;

            // Открываем форму редактирования
            int clientId = (int)clientPanel.Tag;
            var editForm = new EditClientForm(connectionString, clientId);
            if (editForm.ShowDialog() == DialogResult.OK)
            {
                LoadVisitorData(); // Обновляем список после редактирования
            }
        }

        private Label CreateLabel(string text, int x, int y, int width, Color? color = null)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 20),
                Font = new Font("Arial", 10),
                ForeColor = color ?? Color.Black,
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private int CalculateDiscount(decimal totalAmount)
        {
            if (totalAmount >= 3000) return 15;
            if (totalAmount >= 2000) return 10;
            if (totalAmount >= 1000) return 5;
            return 0;
        }


    }

   
}