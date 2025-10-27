using RemoteDriveClient.Models;
using RemoteDriveClient.Services;
using RemoteDriveClient.Controllers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RemoteDriveClient.Forms
{
    public partial class MainForm : Form
    {
        private readonly string token;
        private readonly string username;
        private readonly IFileService fileService;
        private readonly SyncController syncController;

        private DataGridView dgv;
        private Button btnUpload, btnDownload, btnDelete, btnSync, btnSortExt;
        private ComboBox cbFilter;
        private CheckedListBox clbColumns;
        private TextBox txtPreview;
        private PictureBox picPreview;
        private List<FileMetadata> allFiles = new List<FileMetadata>();
        private bool sortExtAsc = true;

        public MainForm(string token, string username, IFileService svc)
        {
            this.token = token;
            this.username = username;
            this.fileService = svc;
            this.syncController = new SyncController(fileService, token, username);

            InitializeCustomComponents();
            LoadFilesAsync();
        }

        private void InitializeCustomComponents()
        {
            this.Text = "Remote Drive - " + username;
            this.WindowState = FormWindowState.Maximized;
            this.AutoScaleMode = AutoScaleMode.Font;

            dgv = new DataGridView();
            dgv.Left = 10; dgv.Top = 10;
            dgv.Width = 900; dgv.Height = 500;
            dgv.ReadOnly = true;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.AutoGenerateColumns = false;
            dgv.CellDoubleClick += new DataGridViewCellEventHandler(Dgv_CellDoubleClick);

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Назва", DataPropertyName = "Name", Width = 250 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Extension", HeaderText = "Розширення", DataPropertyName = "Extension", Width = 80 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Size", HeaderText = "Розмір", DataPropertyName = "Size", Width = 100 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedAt", HeaderText = "Створено", DataPropertyName = "CreatedAt", Width = 150 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ModifiedAt", HeaderText = "Змінено", DataPropertyName = "ModifiedAt", Width = 150 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "UploadedBy", HeaderText = "Завантажив", DataPropertyName = "UploadedBy", Width = 120 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "EditedBy", HeaderText = "Редагував", DataPropertyName = "EditedBy", Width = 120 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "RemotePath", HeaderText = "RemotePath", DataPropertyName = "RemotePath", Visible = false });

            btnUpload = new Button();
            btnUpload.Left = 10; btnUpload.Top = 520; btnUpload.Width = 100;
            btnUpload.Text = "Upload";
            btnUpload.Click += new EventHandler(async (s, e) => await UploadFile());

            btnDownload = new Button();
            btnDownload.Left = 120; btnDownload.Top = 520; btnDownload.Width = 100;
            btnDownload.Text = "Download";
            btnDownload.Click += new EventHandler(async (s, e) => await DownloadSelectedFile());

            btnDelete = new Button();
            btnDelete.Left = 230; btnDelete.Top = 520; btnDelete.Width = 100;
            btnDelete.Text = "Delete";
            btnDelete.Click += new EventHandler(async (s, e) => await DeleteSelectedFile());

            btnSync = new Button();
            btnSync.Left = 340; btnSync.Top = 520; btnSync.Width = 120;
            btnSync.Text = "Sync Folder";
            btnSync.Click += new EventHandler(async (s, e) => await StartSync());

            btnSortExt = new Button();
            btnSortExt.Left = 470; btnSortExt.Top = 520; btnSortExt.Width = 120;
            btnSortExt.Text = "Sort by ext";
            btnSortExt.Click += new EventHandler(delegate { sortExtAsc = !sortExtAsc; ApplyFilterAndSort(); });

            cbFilter = new ComboBox();
            cbFilter.Left = 600; cbFilter.Top = 520; cbFilter.Width = 200;
            cbFilter.Items.AddRange(new object[] { "All", ".js & .png only" });
            cbFilter.SelectedIndex = 0;
            cbFilter.SelectedIndexChanged += new EventHandler(delegate { ApplyFilterAndSort(); });

            clbColumns = new CheckedListBox();
            clbColumns.Left = 920; clbColumns.Top = 10; clbColumns.Width = 250; clbColumns.Height = 200;
            clbColumns.Items.Add("Розширення", true);
            clbColumns.Items.Add("Розмір", true);
            clbColumns.Items.Add("Створено", true);
            clbColumns.Items.Add("Змінено", true);
            clbColumns.Items.Add("Завантажив", true);
            clbColumns.Items.Add("Редагував", true);
            clbColumns.ItemCheck += new ItemCheckEventHandler(ClbColumns_ItemCheck);

            txtPreview = new TextBox();
            txtPreview.Left = 920; txtPreview.Top = 230;
            txtPreview.Width = 450; txtPreview.Height = 200;
            txtPreview.Multiline = true;
            txtPreview.ScrollBars = ScrollBars.Vertical;
            txtPreview.Visible = false;

            picPreview = new PictureBox();
            picPreview.Left = 920; picPreview.Top = 230;
            picPreview.Width = 450; picPreview.Height = 300;
            picPreview.SizeMode = PictureBoxSizeMode.Zoom;
            picPreview.Visible = false;

            this.Controls.Add(dgv);
            this.Controls.Add(btnUpload);
            this.Controls.Add(btnDownload);
            this.Controls.Add(btnDelete);
            this.Controls.Add(btnSync);
            this.Controls.Add(btnSortExt);
            this.Controls.Add(cbFilter);
            this.Controls.Add(clbColumns);
            this.Controls.Add(txtPreview);
            this.Controls.Add(picPreview);
        }

        private void ClbColumns_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            this.BeginInvoke(new Action(delegate
            {
                var map = new Dictionary<string, string>();
                map["Розширення"] = "Extension";
                map["Розмір"] = "Size";
                map["Створено"] = "CreatedAt";
                map["Змінено"] = "ModifiedAt";
                map["Завантажив"] = "UploadedBy";
                map["Редагував"] = "EditedBy";

                foreach (string key in map.Keys)
                {
                    for (int i = 0; i < clbColumns.Items.Count; i++)
                    {
                        if (clbColumns.Items[i].ToString() == key)
                            dgv.Columns[map[key]].Visible = clbColumns.GetItemChecked(i);
                    }
                }

                dgv.Columns["Name"].Visible = true;
            }));
        }

        private async void LoadFilesAsync()
        {
            try
            {
                allFiles = await fileService.ListFilesAsync(token);
                ApplyFilterAndSort();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка завантаження файлів: " + ex.Message);
            }
        }

        private void ApplyFilterAndSort()
        {
            IEnumerable<FileMetadata> query = allFiles;
            if (cbFilter.SelectedIndex == 1)
                query = query.Where(f => f.Extension == ".js" || f.Extension == ".png");

            if (sortExtAsc) query = query.OrderBy(f => f.Extension);
            else query = query.OrderByDescending(f => f.Extension);

            dgv.DataSource = query.Select(f => new
            {
                f.Name,
                f.Extension,
                Size = FormatFileSize(f.Size),
                CreatedAt = f.CreatedAt.ToLocalTime().ToString("g"),
                ModifiedAt = f.ModifiedAt.ToLocalTime().ToString("g"),
                f.UploadedBy,
                f.EditedBy,
                f.RemotePath
            }).ToList();
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double len = bytes;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private async Task UploadFile()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != DialogResult.OK) return;
            try
            {
                await fileService.UploadFileAsync(token, ofd.FileName, username);
                LoadFilesAsync();
            }
            catch (Exception ex) { MessageBox.Show("Upload error: " + ex.Message); }
        }

        private async Task DownloadSelectedFile()
        {
            if (dgv.SelectedRows.Count == 0) return;
            string remotePath = dgv.SelectedRows[0].Cells["RemotePath"].Value.ToString();
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = Path.GetFileName(remotePath);
            if (sfd.ShowDialog() != DialogResult.OK) return;

            Stream s = await fileService.GetFileStreamAsync(token, remotePath);
            using (FileStream fs = new FileStream(sfd.FileName, FileMode.Create, FileAccess.Write))
            {
                await s.CopyToAsync(fs);
            }
            s.Close();
            MessageBox.Show("Downloaded");
        }

        

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        private async Task DeleteSelectedFile()
        {
            if (dgv.SelectedRows.Count == 0) return;
            string remotePath = dgv.SelectedRows[0].Cells["RemotePath"].Value.ToString();
            bool ok = await fileService.DeleteFileAsync(token, remotePath);
            if (ok) { LoadFilesAsync(); MessageBox.Show("Deleted"); }
        }

        private async Task StartSync()
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() != DialogResult.OK) return;
            string folder = fbd.SelectedPath;
            btnSync.Enabled = false;
            syncController.ProgressChanged += SyncController_ProgressChanged;

            string report = await syncController.SyncFolderAsync(folder);
            MessageBox.Show(report);

            syncController.ProgressChanged -= SyncController_ProgressChanged;
            btnSync.Enabled = true;
            LoadFilesAsync();
        }

        private void SyncController_ProgressChanged(object sender, string e)
        {
            this.BeginInvoke(new Action(delegate
            {
                this.Text = "Remote Drive - " + username + " (" + e + ")";
            }));
        }

        private async void Dgv_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            string remotePath = dgv.Rows[e.RowIndex].Cells["RemotePath"].Value.ToString();
            string fileName = dgv.Rows[e.RowIndex].Cells["Name"].Value.ToString();
            string ext = dgv.Rows[e.RowIndex].Cells["Extension"].Value.ToString().ToLower();

            try
            {
                // ТІЛЬКИ .kt та .jpg файли можна переглядати
                if (ext == ".kt")
                {
                    await ShowKotlinPreview(remotePath, fileName);
                }
                else if (ext == ".jpg" || ext == ".jpeg")
                {
                    await ShowImagePreview(remotePath, fileName);
                }
                else
                {
                    MessageBox.Show($"Перегляд файлів з розширенням '{ext}' не підтримується.\n\n" +
                                  $"Файл: {fileName}\n\n" +
                                  "Доступний перегляд лише для:\n" +
                                  "• Kotlin файли: .kt\n" +
                                  "• Зображення: .jpg, .jpeg",
                                  "Перегляд не підтримується",
                                  MessageBoxButtons.OK,
                                  MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при перегляді файлу {fileName}: {ex.Message}",
                              "Помилка",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Error);
            }
        }

        private async Task ShowKotlinPreview(string remotePath, string fileName)
        {
            txtPreview.Visible = true;
            picPreview.Visible = false;

            Stream s = await fileService.GetFileStreamAsync(token, remotePath);
            using (StreamReader sr = new StreamReader(s))
            {
                string content = sr.ReadToEnd();

                // Обмеження довжини тексту для великих Kotlin файлів
                if (content.Length > 50000) // 50KB обмеження для Kotlin файлів
                {
                    content = content.Substring(0, 50000) + "\n\n...[ФАЙЛ ЗНАЧНО ВЕЛИКИЙ, ВІДОБРАЖЕНО ЛИШЕ ПЕРШІ 50KB]...";
                }

                txtPreview.Text = content;
            }
            s.Close();

            this.Text = $"Remote Drive - {username} (Перегляд Kotlin: {fileName})";
        }

        private async Task ShowImagePreview(string remotePath, string fileName)
        {
            picPreview.Visible = true;
            txtPreview.Visible = false;

            Stream s = await fileService.GetFileStreamAsync(token, remotePath);
            try
            {
                Image img = Image.FromStream(s);

                // Перевірка розміру зображення для JPG файлів
                if (img.Width > 2000 || img.Height > 2000)
                {
                    var result = MessageBox.Show($"Зображення має великий розмір ({img.Width}x{img.Height}).\nЗавантажити його може зайняти деякий час. Продовжити?",
                                               "Велике зображення",
                                               MessageBoxButtons.YesNo,
                                               MessageBoxIcon.Question);

                    if (result == DialogResult.No)
                    {
                        img.Dispose();
                        s.Close();
                        return;
                    }
                }

                picPreview.Image = img;
                this.Text = $"Remote Drive - {username} (Перегляд JPG: {fileName})";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не вдалося завантажити зображення: {ex.Message}",
                              "Помилка",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Error);
                picPreview.Image = null;
            }
            finally
            {
                s.Close();
            }
        }

        
    }
}