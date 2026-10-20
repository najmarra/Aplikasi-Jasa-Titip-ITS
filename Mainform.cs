using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace JastipITS;

public class MainForm : Form
{
    static readonly CultureInfo Idn = new("id-ID");

    // Ongkos titip default per lokasi antar (bisa diedit manual per pesanan).
    static readonly Dictionary<string, decimal> OngkosLokasi = new()
    {
        ["Asrama Mahasiswa ITS"] = 3000,
        ["Perpustakaan Pusat"] = 2000,
        ["Gedung Rektorat"] = 3000,
        ["Departemen Teknik Informatika"] = 2000,
        ["Departemen Teknik Mesin"] = 3000,
        ["Departemen Teknik Sipil"] = 3000,
        ["Departemen Teknik Elektro"] = 2000,
        ["Kos sekitar Keputih"] = 5000,
        ["Kos sekitar Gebang / Mulyosari"] = 6000,
        ["Lainnya (dalam kampus)"] = 3000,
    };

    readonly DataStore store = new();
    bool ready;        // true setelah seluruh UI selesai dibangun
    bool loading;      // true saat mengisi form secara programatis
    bool suppressSel;  // true saat grid di-refresh
    int? selPesananId;
    int? selMenuId;

    readonly TextBox txtNama = new();
    readonly TextBox txtWa = new();
    readonly ComboBox cmbMenu = new() { DropDownStyle = ComboBoxStyle.DropDown };
    readonly TextBox txtKantin = new();
    readonly NumericUpDown nudJumlah = new() { Minimum = 1, Maximum = 999, Value = 1 };
    readonly NumericUpDown nudHarga = new() { Maximum = 10_000_000, Increment = 500, ThousandsSeparator = true };
    readonly ComboBox cmbLokasi = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    readonly NumericUpDown nudOngkos = new() { Maximum = 1_000_000, Increment = 500, ThousandsSeparator = true };
    readonly ComboBox cmbStatus = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    readonly TextBox txtCatatan = new() { Multiline = true, Height = 54 };
    readonly Label lblTotal = new() { Font = new Font("Segoe UI", 12f, FontStyle.Bold), AutoSize = true };
    readonly DataGridView gridPesanan = new();
    readonly TextBox txtCari = new() { Width = 180 };
    readonly ComboBox cmbFilter = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
    readonly Label lblRingkas = new() { Dock = DockStyle.Bottom, Height = 28, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(6, 0, 0, 0) };

    readonly TextBox txtMenuNama = new();
    readonly TextBox txtMenuKantin = new();
    readonly NumericUpDown nudMenuHarga = new() { Maximum = 10_000_000, Increment = 500, ThousandsSeparator = true };
    readonly DataGridView gridMenu = new();

    readonly DateTimePicker dtpDari = new() { Format = DateTimePickerFormat.Short, Width = 110 };
    readonly DateTimePicker dtpSampai = new() { Format = DateTimePickerFormat.Short, Width = 110 };
    readonly TextBox txtLaporan = new()
    {
        Multiline = true, ReadOnly = true, Dock = DockStyle.Fill,
        Font = new Font("Consolas", 10f), ScrollBars = ScrollBars.Vertical, BackColor = Color.White
    };
    TabPage tabLaporan = null!;

    public MainForm()
    {
        Text = "Jastip Makanan ITS - Manajemen Pesanan";
        Size = new Size(1200, 720);
        MinimumSize = new Size(1000, 600);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5f);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(BuildPesananTab());
        tabs.TabPages.Add(BuildMenuTab());
        tabLaporan = BuildLaporanTab();
        tabs.TabPages.Add(tabLaporan);
        tabs.SelectedIndexChanged += (_, _) =>
        {
            if (tabs.SelectedTab == tabLaporan) BuildReport();
        };
        Controls.Add(tabs);

        ready = true;
        RefreshMenuCombo();
        RefreshMenu();
        RefreshPesanan();
        ClearPesananForm();
        ClearMenuForm();
    }

    static void AddRow(TableLayoutPanel t, string label, Control c)
    {
        int r = t.RowCount++;
        t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        t.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 8, 3, 3) }, 0, r);
        c.Dock = DockStyle.Fill;
        c.Margin = new Padding(3, 4, 3, 4);
        t.Controls.Add(c, 1, r);
    }

    static void AddWide(TableLayoutPanel t, Control c)
    {
        int r = t.RowCount++;
        t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        t.Controls.Add(c, 0, r);
        t.SetColumnSpan(c, 2);
    }

    static TableLayoutPanel NewForm()
    {
        var t = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2 };
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return t;
    }

    static Button Btn(string text, EventHandler onClick)
    {
        var b = new Button { Text = text, AutoSize = true, Padding = new Padding(6, 2, 6, 2), Margin = new Padding(3) };
        b.Click += onClick;
        return b;
    }

    static void SetupGrid(DataGridView g)
    {
        g.Dock = DockStyle.Fill;
        g.AutoGenerateColumns = false;
        g.ReadOnly = true;
        g.AllowUserToAddRows = false;
        g.AllowUserToDeleteRows = false;
        g.AllowUserToResizeRows = false;
        g.MultiSelect = false;
        g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        g.RowHeadersVisible = false;
        g.BackgroundColor = Color.White;
        g.BorderStyle = BorderStyle.None;
        g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        g.EnableHeadersVisualStyles = true;
    }

    static void Col(DataGridView g, string header, string prop, int weight, string? fmt = null, bool right = false)
    {
        var c = new DataGridViewTextBoxColumn
        {
            HeaderText = header,
            DataPropertyName = prop,
            FillWeight = weight
        };
        if (fmt != null)
        {
            c.DefaultCellStyle.Format = fmt;
            c.DefaultCellStyle.FormatProvider = Idn;
        }
        if (right) c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        g.Columns.Add(c);
    }

    static string Rp(decimal v) => v.ToString("C0", Idn);

    static void Info(string msg) =>
        MessageBox.Show(msg, "Informasi", MessageBoxButtons.OK, MessageBoxIcon.Information);

    static void Warn(string msg) =>
        MessageBox.Show(msg, "Perhatian", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    void Persist()
    {
        try { store.Save(); }
        catch (Exception ex)
        {
            MessageBox.Show("Gagal menyimpan data: " + ex.Message, "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    TabPage BuildPesananTab()
    {
        var tab = new TabPage("Pesanan");

        var left = new Panel { Dock = DockStyle.Left, Width = 370, Padding = new Padding(10), AutoScroll = true };
        var t = NewForm();

        cmbLokasi.Items.AddRange(OngkosLokasi.Keys.Cast<object>().ToArray());
        cmbStatus.Items.AddRange(Enum.GetValues<StatusPesanan>().Cast<object>().ToArray());
        cmbMenu.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        cmbMenu.AutoCompleteSource = AutoCompleteSource.ListItems;

        AddRow(t, "Nama pemesan", txtNama);
        AddRow(t, "No. WhatsApp", txtWa);
        AddRow(t, "Menu", cmbMenu);
        AddRow(t, "Kantin/toko", txtKantin);
        AddRow(t, "Jumlah", nudJumlah);
        AddRow(t, "Harga satuan", nudHarga);
        AddRow(t, "Lokasi antar", cmbLokasi);
        AddRow(t, "Ongkos titip", nudOngkos);
        AddRow(t, "Status", cmbStatus);
        AddRow(t, "Catatan", txtCatatan);
        AddRow(t, "TOTAL", lblTotal);

        var btns = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 10, 0, 0) };
        btns.Controls.Add(Btn("Tambah", (_, _) => TambahPesanan()));
        btns.Controls.Add(Btn("Ubah", (_, _) => UbahPesanan()));
        btns.Controls.Add(Btn("Hapus", (_, _) => HapusPesanan()));
        btns.Controls.Add(Btn("Bersihkan", (_, _) => ClearPesananForm()));
        AddWide(t, btns);
        left.Controls.Add(t);

        cmbMenu.SelectedIndexChanged += (_, _) =>
        {
            if (loading || !ready) return;
            AutoIsiDariMenu(cmbMenu.SelectedItem as string, force: true);
        };
        cmbMenu.Leave += (_, _) =>
        {
            if (loading || !ready) return;
            if (nudHarga.Value == 0) AutoIsiDariMenu(cmbMenu.Text, force: false);
        };
        cmbLokasi.SelectedIndexChanged += (_, _) =>
        {
            if (loading || !ready) return;
            if (cmbLokasi.SelectedItem is string s && OngkosLokasi.TryGetValue(s, out var o))
                nudOngkos.Value = o;
        };
        nudJumlah.ValueChanged += (_, _) => UpdateTotal();
        nudHarga.ValueChanged += (_, _) => UpdateTotal();
        nudOngkos.ValueChanged += (_, _) => UpdateTotal();

        var right = new Panel { Dock = DockStyle.Fill };

        SetupGrid(gridPesanan);
        Col(gridPesanan, "ID", nameof(Pesanan.Id), 30);
        Col(gridPesanan, "Tanggal", nameof(Pesanan.Tanggal), 85, "dd/MM/yyyy HH:mm");
        Col(gridPesanan, "Pemesan", nameof(Pesanan.NamaPemesan), 90);
        Col(gridPesanan, "Menu", nameof(Pesanan.NamaMenu), 90);
        Col(gridPesanan, "Jml", nameof(Pesanan.Jumlah), 30, null, true);
        Col(gridPesanan, "Lokasi antar", nameof(Pesanan.Lokasi), 110);
        Col(gridPesanan, "Ongkos", nameof(Pesanan.OngkosTitip), 60, "C0", true);
        Col(gridPesanan, "Total", nameof(Pesanan.Total), 70, "C0", true);
        Col(gridPesanan, "Status", nameof(Pesanan.Status), 65);

        gridPesanan.SelectionChanged += (_, _) => GridPesananSelectionChanged();
        gridPesanan.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0) return;
            if (gridPesanan.Rows[e.RowIndex].DataBoundItem is Pesanan p && e.CellStyle != null)
            {
                e.CellStyle.BackColor = p.Status switch
                {
                    StatusPesanan.Menunggu => Color.FromArgb(255, 249, 219),
                    StatusPesanan.Dibeli => Color.FromArgb(222, 237, 255),
                    StatusPesanan.Diantar => Color.FromArgb(255, 236, 214),
                    StatusPesanan.Selesai => Color.FromArgb(220, 245, 224),
                    _ => Color.FromArgb(235, 235, 235),
                };
            }
        };

        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(4) };
        cmbFilter.Items.Add("Semua");
        foreach (var s in Enum.GetValues<StatusPesanan>()) cmbFilter.Items.Add(s.ToString());
        cmbFilter.SelectedIndex = 0;
        cmbFilter.SelectedIndexChanged += (_, _) => RefreshPesanan();
        txtCari.TextChanged += (_, _) => RefreshPesanan();

        bar.Controls.Add(new Label { Text = "Cari:", AutoSize = true, Margin = new Padding(3, 8, 3, 3) });
        bar.Controls.Add(txtCari);
        bar.Controls.Add(new Label { Text = "Status:", AutoSize = true, Margin = new Padding(12, 8, 3, 3) });
        bar.Controls.Add(cmbFilter);
        bar.Controls.Add(Btn("Lanjut Status >>", (_, _) => LanjutStatus()));
        bar.Controls.Add(Btn("Salin Struk", (_, _) => SalinStruk()));
        bar.Controls.Add(Btn("Export CSV", (_, _) => ExportCsv()));

        right.Controls.Add(gridPesanan);
        right.Controls.Add(lblRingkas);
        right.Controls.Add(bar);

        tab.Controls.Add(right);
        tab.Controls.Add(left);
        return tab;
    }

    void AutoIsiDariMenu(string? nama, bool force)
    {
        if (string.IsNullOrWhiteSpace(nama)) return;
        var m = store.Menus.FirstOrDefault(x => x.Nama.Equals(nama.Trim(), StringComparison.OrdinalIgnoreCase));
        if (m == null) return;
        if (force || string.IsNullOrWhiteSpace(txtKantin.Text)) txtKantin.Text = m.Kantin;
        if (force || nudHarga.Value == 0) nudHarga.Value = Math.Min(m.Harga, nudHarga.Maximum);
    }

    void UpdateTotal()
    {
        lblTotal.Text = Rp(nudJumlah.Value * nudHarga.Value + nudOngkos.Value);
    }

    void ClearPesananForm()
    {
        loading = true;
        selPesananId = null;
        txtNama.Clear();
        txtWa.Clear();
        cmbMenu.Text = "";
        txtKantin.Clear();
        nudJumlah.Value = 1;
        nudHarga.Value = 0;
        cmbLokasi.SelectedIndex = 0;
        nudOngkos.Value = OngkosLokasi[(string)cmbLokasi.SelectedItem!];
        cmbStatus.SelectedItem = StatusPesanan.Menunggu;
        txtCatatan.Clear();
        loading = false;
        gridPesanan.ClearSelection();
        UpdateTotal();
        txtNama.Focus();
    }

    void IsiForm(Pesanan p)
    {
        loading = true;
        txtNama.Text = p.NamaPemesan;
        txtWa.Text = p.NoWa;
        cmbMenu.Text = p.NamaMenu;
        txtKantin.Text = p.Kantin;
        nudJumlah.Value = Math.Clamp(p.Jumlah, (int)nudJumlah.Minimum, (int)nudJumlah.Maximum);
        nudHarga.Value = Math.Min(p.HargaSatuan, nudHarga.Maximum);
        cmbLokasi.SelectedItem = p.Lokasi;
        if (cmbLokasi.SelectedIndex < 0) cmbLokasi.SelectedIndex = 0;
        nudOngkos.Value = Math.Min(p.OngkosTitip, nudOngkos.Maximum);
        cmbStatus.SelectedItem = p.Status;
        txtCatatan.Text = p.Catatan;
        loading = false;
        UpdateTotal();
    }

    void IsiDariForm(Pesanan p)
    {
        p.NamaPemesan = txtNama.Text.Trim();
        p.NoWa = txtWa.Text.Trim();
        p.NamaMenu = cmbMenu.Text.Trim();
        p.Kantin = txtKantin.Text.Trim();
        p.Jumlah = (int)nudJumlah.Value;
        p.HargaSatuan = nudHarga.Value;
        p.Lokasi = (string)cmbLokasi.SelectedItem!;
        p.OngkosTitip = nudOngkos.Value;
        p.Status = (StatusPesanan)cmbStatus.SelectedItem!;
        p.Catatan = txtCatatan.Text.Trim();
    }

    bool ValidasiPesanan()
    {
        if (string.IsNullOrWhiteSpace(txtNama.Text))
        { Warn("Nama pemesan wajib diisi."); txtNama.Focus(); return false; }

        var wa = txtWa.Text.Trim();
        if (wa.Length > 0 && !Regex.IsMatch(wa, @"^(\+62|62|0)8[0-9]{7,12}$"))
        { Warn("Format No. WhatsApp tidak valid.\nContoh: 081234567890"); txtWa.Focus(); return false; }

        if (string.IsNullOrWhiteSpace(cmbMenu.Text))
        { Warn("Nama menu wajib diisi."); cmbMenu.Focus(); return false; }

        if (nudHarga.Value <= 0)
        { Warn("Harga satuan harus lebih dari 0."); nudHarga.Focus(); return false; }

        return true;
    }

    void TambahPesanan()
    {
        if (!ValidasiPesanan()) return;
        var p = new Pesanan { Id = store.NextPesananId(), Tanggal = DateTime.Now };
        IsiDariForm(p);
        store.Pesanans.Add(p);
        Persist();
        ClearPesananForm();
        RefreshPesanan();
        Info($"Pesanan #{p.Id} atas nama {p.NamaPemesan} berhasil ditambahkan.");
    }

    void UbahPesanan()
    {
        var p = SelectedPesanan();
        if (p == null) { Warn("Pilih pesanan pada tabel terlebih dahulu."); return; }
        if (!ValidasiPesanan()) return;
        IsiDariForm(p);
        Persist();
        RefreshPesanan();
        Info($"Pesanan #{p.Id} berhasil diperbarui.");
    }

    void HapusPesanan()
    {
        var p = SelectedPesanan();
        if (p == null) { Warn("Pilih pesanan pada tabel terlebih dahulu."); return; }
        var r = MessageBox.Show($"Hapus pesanan #{p.Id} ({p.NamaPemesan} - {p.NamaMenu})?",
            "Konfirmasi", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (r != DialogResult.Yes) return;
        store.Pesanans.Remove(p);
        Persist();
        ClearPesananForm();
        RefreshPesanan();
    }

    Pesanan? SelectedPesanan() => store.Pesanans.FirstOrDefault(p => p.Id == selPesananId);

    void LanjutStatus()
    {
        var p = SelectedPesanan();
        if (p == null) { Warn("Pilih pesanan pada tabel terlebih dahulu."); return; }
        switch (p.Status)
        {
            case StatusPesanan.Menunggu: p.Status = StatusPesanan.Dibeli; break;
            case StatusPesanan.Dibeli: p.Status = StatusPesanan.Diantar; break;
            case StatusPesanan.Diantar: p.Status = StatusPesanan.Selesai; break;
            default: Info($"Pesanan sudah berstatus {p.Status}."); return;
        }
        Persist();
        RefreshPesanan();
        loading = true;
        cmbStatus.SelectedItem = p.Status;
        loading = false;
    }

    void SalinStruk()
    {
        var p = SelectedPesanan();
        if (p == null) { Warn("Pilih pesanan pada tabel terlebih dahulu."); return; }

        var sb = new StringBuilder();
        sb.AppendLine("*JASTIP MAKANAN ITS*");
        sb.AppendLine($"No. Pesanan : #{p.Id:000}");
        sb.AppendLine($"Tanggal     : {p.Tanggal:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Pemesan     : {p.NamaPemesan}");
        sb.AppendLine($"Antar ke    : {p.Lokasi}");
        sb.AppendLine("--------------------------");
        sb.AppendLine($"{p.Jumlah} x {p.NamaMenu} ({p.Kantin}) @ {Rp(p.HargaSatuan)}");
        sb.AppendLine($"Subtotal    : {Rp(p.Subtotal)}");
        sb.AppendLine($"Ongkos titip: {Rp(p.OngkosTitip)}");
        sb.AppendLine($"*TOTAL      : {Rp(p.Total)}*");
        sb.AppendLine($"Status      : {p.Status}");
        if (!string.IsNullOrWhiteSpace(p.Catatan)) sb.AppendLine($"Catatan     : {p.Catatan}");

        try
        {
            Clipboard.SetText(sb.ToString());
            Info("Struk disalin ke clipboard. Tinggal paste ke WhatsApp.");
        }
        catch (Exception ex) { Warn("Gagal menyalin: " + ex.Message); }
    }

    void ExportCsv()
    {
        using var dlg = new SaveFileDialog
        {
            Filter = "File CSV (*.csv)|*.csv",
            FileName = $"pesanan-jastip-{DateTime.Now:yyyyMMdd}.csv"
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        static string Q(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";

        var sb = new StringBuilder();
        sb.AppendLine("ID;Tanggal;Pemesan;NoWA;Menu;Kantin;Jumlah;HargaSatuan;Lokasi;Ongkos;Total;Status;Catatan");
        foreach (var p in store.Pesanans.OrderBy(x => x.Id))
        {
            sb.AppendLine(string.Join(";", new[]
            {
                p.Id.ToString(), p.Tanggal.ToString("yyyy-MM-dd HH:mm"), Q(p.NamaPemesan), Q(p.NoWa),
                Q(p.NamaMenu), Q(p.Kantin), p.Jumlah.ToString(), p.HargaSatuan.ToString("0"),
                Q(p.Lokasi), p.OngkosTitip.ToString("0"), p.Total.ToString("0"),
                p.Status.ToString(), Q(p.Catatan)
            }));
        }
        try
        {
            File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(true));
            Info("Data berhasil diekspor:\n" + dlg.FileName);
        }
        catch (Exception ex) { Warn("Gagal mengekspor: " + ex.Message); }
    }

    void GridPesananSelectionChanged()
    {
        if (suppressSel || !ready) return;
        if (gridPesanan.SelectedRows.Count == 0) return;
        if (gridPesanan.SelectedRows[0].DataBoundItem is Pesanan p)
        {
            selPesananId = p.Id;
            IsiForm(p);
        }
    }

    void RefreshPesanan()
    {
        if (!ready) return;
        string q = txtCari.Text.Trim();
        string filter = cmbFilter.SelectedItem as string ?? "Semua";

        var list = store.Pesanans
            .Where(p => filter == "Semua" || p.Status.ToString() == filter)
            .Where(p => q.Length == 0
                || p.NamaPemesan.Contains(q, StringComparison.OrdinalIgnoreCase)
                || p.NamaMenu.Contains(q, StringComparison.OrdinalIgnoreCase)
                || p.Lokasi.Contains(q, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(p => p.Tanggal)
            .ToList();

        suppressSel = true;
        gridPesanan.DataSource = list;
        gridPesanan.ClearSelection();
        if (selPesananId != null)
        {
            foreach (DataGridViewRow r in gridPesanan.Rows)
            {
                if (r.DataBoundItem is Pesanan p && p.Id == selPesananId)
                {
                    r.Selected = true;
                    gridPesanan.CurrentCell = r.Cells[0];
                    break;
                }
            }
        }
        suppressSel = false;

        var aktif = list.Count(p => p.Status is StatusPesanan.Menunggu or StatusPesanan.Dibeli or StatusPesanan.Diantar);
        var omzet = list.Where(p => p.Status == StatusPesanan.Selesai).Sum(p => p.Total);
        lblRingkas.Text = $"Tampil: {list.Count} pesanan   |   Masih diproses: {aktif}   |   Total selesai: {Rp(omzet)}";
    }

    TabPage BuildMenuTab()
    {
        var tab = new TabPage("Menu & Harga");

        var left = new Panel { Dock = DockStyle.Left, Width = 340, Padding = new Padding(10) };
        var t = NewForm();
        AddRow(t, "Nama menu", txtMenuNama);
        AddRow(t, "Kantin/toko", txtMenuKantin);
        AddRow(t, "Harga", nudMenuHarga);

        var btns = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 10, 0, 0) };
        btns.Controls.Add(Btn("Tambah", (_, _) => TambahMenu()));
        btns.Controls.Add(Btn("Ubah", (_, _) => UbahMenu()));
        btns.Controls.Add(Btn("Hapus", (_, _) => HapusMenu()));
        btns.Controls.Add(Btn("Bersihkan", (_, _) => ClearMenuForm()));
        AddWide(t, btns);
        left.Controls.Add(t);

        SetupGrid(gridMenu);
        Col(gridMenu, "ID", nameof(MenuMakanan.Id), 20);
        Col(gridMenu, "Nama menu", nameof(MenuMakanan.Nama), 100);
        Col(gridMenu, "Kantin/toko", nameof(MenuMakanan.Kantin), 100);
        Col(gridMenu, "Harga", nameof(MenuMakanan.Harga), 60, "C0", true);
        gridMenu.SelectionChanged += (_, _) =>
        {
            if (suppressSel || !ready) return;
            if (gridMenu.SelectedRows.Count == 0) return;
            if (gridMenu.SelectedRows[0].DataBoundItem is MenuMakanan m)
            {
                selMenuId = m.Id;
                txtMenuNama.Text = m.Nama;
                txtMenuKantin.Text = m.Kantin;
                nudMenuHarga.Value = Math.Min(m.Harga, nudMenuHarga.Maximum);
            }
        };

        tab.Controls.Add(gridMenu);
        tab.Controls.Add(left);
        return tab;
    }

    void ClearMenuForm()
    {
        selMenuId = null;
        txtMenuNama.Clear();
        txtMenuKantin.Clear();
        nudMenuHarga.Value = 0;
        gridMenu.ClearSelection();
    }

    bool ValidasiMenu()
    {
        if (string.IsNullOrWhiteSpace(txtMenuNama.Text))
        { Warn("Nama menu wajib diisi."); txtMenuNama.Focus(); return false; }
        if (nudMenuHarga.Value <= 0)
        { Warn("Harga harus lebih dari 0."); nudMenuHarga.Focus(); return false; }
        return true;
    }

    void TambahMenu()
    {
        if (!ValidasiMenu()) return;
        store.Menus.Add(new MenuMakanan
        {
            Id = store.NextMenuId(),
            Nama = txtMenuNama.Text.Trim(),
            Kantin = txtMenuKantin.Text.Trim(),
            Harga = nudMenuHarga.Value
        });
        Persist();
        ClearMenuForm();
        RefreshMenu();
        RefreshMenuCombo();
    }

    void UbahMenu()
    {
        var m = store.Menus.FirstOrDefault(x => x.Id == selMenuId);
        if (m == null) { Warn("Pilih menu pada tabel terlebih dahulu."); return; }
        if (!ValidasiMenu()) return;
        m.Nama = txtMenuNama.Text.Trim();
        m.Kantin = txtMenuKantin.Text.Trim();
        m.Harga = nudMenuHarga.Value;
        Persist();
        RefreshMenu();
        RefreshMenuCombo();
    }

    void HapusMenu()
    {
        var m = store.Menus.FirstOrDefault(x => x.Id == selMenuId);
        if (m == null) { Warn("Pilih menu pada tabel terlebih dahulu."); return; }
        if (MessageBox.Show($"Hapus menu \"{m.Nama}\"?", "Konfirmasi",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        store.Menus.Remove(m);
        Persist();
        ClearMenuForm();
        RefreshMenu();
        RefreshMenuCombo();
    }

    void RefreshMenu()
    {
        if (!ready) return;
        suppressSel = true;
        gridMenu.DataSource = store.Menus.OrderBy(m => m.Kantin).ThenBy(m => m.Nama).ToList();
        gridMenu.ClearSelection();
        suppressSel = false;
    }

    void RefreshMenuCombo()
    {
        var cur = cmbMenu.Text;
        loading = true;
        cmbMenu.Items.Clear();
        cmbMenu.Items.AddRange(store.Menus
            .Select(m => m.Nama)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n)
            .Cast<object>().ToArray());
        cmbMenu.Text = cur;
        loading = false;
    }

    TabPage BuildLaporanTab()
    {
        var tab = new TabPage("Laporan");

        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(6) };
        var now = DateTime.Today;
        dtpDari.Value = new DateTime(now.Year, now.Month, 1);
        dtpSampai.Value = now;
        bar.Controls.Add(new Label { Text = "Dari:", AutoSize = true, Margin = new Padding(3, 8, 3, 3) });
        bar.Controls.Add(dtpDari);
        bar.Controls.Add(new Label { Text = "Sampai:", AutoSize = true, Margin = new Padding(12, 8, 3, 3) });
        bar.Controls.Add(dtpSampai);
        bar.Controls.Add(Btn("Tampilkan Laporan", (_, _) => BuildReport()));

        tab.Controls.Add(txtLaporan);
        tab.Controls.Add(bar);
        return tab;
    }

    void BuildReport()
    {
        var dari = dtpDari.Value.Date;
        var sampai = dtpSampai.Value.Date.AddDays(1);
        var data = store.Pesanans.Where(p => p.Tanggal >= dari && p.Tanggal < sampai).ToList();
        var selesai = data.Where(p => p.Status == StatusPesanan.Selesai).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("LAPORAN JASTIP MAKANAN ITS");
        sb.AppendLine($"Periode : {dari:dd MMM yyyy} s/d {sampai.AddDays(-1):dd MMM yyyy}");
        sb.AppendLine(new string('=', 56));
        sb.AppendLine();
        sb.AppendLine("RINGKASAN");
        sb.AppendLine($"  Total pesanan masuk      : {data.Count}");
        sb.AppendLine($"  Pesanan selesai          : {selesai.Count}");
        sb.AppendLine($"  Omzet (pesanan selesai)  : {Rp(selesai.Sum(p => p.Total))}");
        sb.AppendLine($"  Pendapatan jasa (ongkos) : {Rp(selesai.Sum(p => p.OngkosTitip))}");
        sb.AppendLine($"  Modal belanja makanan    : {Rp(selesai.Sum(p => p.Subtotal))}");
        if (selesai.Count > 0)
            sb.AppendLine($"  Rata-rata nilai pesanan  : {Rp(selesai.Average(p => p.Total))}");
        sb.AppendLine();

        sb.AppendLine("JUMLAH PESANAN PER STATUS");
        foreach (var s in Enum.GetValues<StatusPesanan>())
            sb.AppendLine($"  {s.ToString().PadRight(12)}: {data.Count(p => p.Status == s)}");
        sb.AppendLine();

        sb.AppendLine("5 MENU TERLARIS (pesanan selesai)");
        var topMenu = selesai
            .GroupBy(p => p.NamaMenu, StringComparer.OrdinalIgnoreCase)
            .Select(g => new { Nama = g.Key, Qty = g.Sum(x => x.Jumlah) })
            .OrderByDescending(x => x.Qty).Take(5).ToList();
        if (topMenu.Count == 0) sb.AppendLine("  (belum ada data)");
        int i = 1;
        foreach (var m in topMenu) sb.AppendLine($"  {i++}. {m.Nama.PadRight(24)} {m.Qty} porsi");
        sb.AppendLine();

        sb.AppendLine("5 LOKASI ANTAR TERBANYAK (pesanan selesai)");
        var topLok = selesai
            .GroupBy(p => p.Lokasi)
            .Select(g => new { Lokasi = g.Key, Jml = g.Count() })
            .OrderByDescending(x => x.Jml).Take(5).ToList();
        if (topLok.Count == 0) sb.AppendLine("  (belum ada data)");
        i = 1;
        foreach (var l in topLok) sb.AppendLine($"  {i++}. {l.Lokasi.PadRight(34)} {l.Jml}x");
        sb.AppendLine();

        sb.AppendLine("5 PELANGGAN TERBANYAK (pesanan selesai)");
        var topPel = selesai
            .GroupBy(p => p.NamaPemesan, StringComparer.OrdinalIgnoreCase)
            .Select(g => new { Nama = g.Key, Jml = g.Count(), Belanja = g.Sum(x => x.Total) })
            .OrderByDescending(x => x.Jml).ThenByDescending(x => x.Belanja).Take(5).ToList();
        if (topPel.Count == 0) sb.AppendLine("  (belum ada data)");
        i = 1;
        foreach (var p in topPel) sb.AppendLine($"  {i++}. {p.Nama.PadRight(22)} {p.Jml}x  {Rp(p.Belanja)}");

        sb.AppendLine();
        sb.AppendLine($"Lokasi file data: {store.FilePath}");
        txtLaporan.Text = sb.ToString();
    }
}