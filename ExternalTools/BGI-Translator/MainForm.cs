using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace BGITranslator
{
    public class MainForm : Form
    {
        // ── DLL wrapper ────────────────────────────────────────────────
        private readonly BurikoWrapper _dll = new BurikoWrapper();

        // ── Data ───────────────────────────────────────────────────────
        private string[]        _originals    = new string[0];   // from Import()
        private string[]        _translations = new string[0];   // editable
        private string          _currentFile  = null;
        private bool            _isDirty      = false;
        private List<int>       _viewIndices  = new List<int>(); // indices into _originals
        private int             _selViewRow   = -1;              // row in grid
        private string          _searchTerm   = "";
        private List<int>       _searchHits   = new List<int>(); // indices into _viewIndices
        private int             _searchCur    = -1;
        private List<string>    _recentFiles  = new List<string>();

        // ── Controls ───────────────────────────────────────────────────
        private MenuStrip            _menu;
        private ToolStripMenuItem    _mnuRecent;
        private ToolStrip            _tools;
        private ToolStripTextBox     _searchBox;
        private ToolStripLabel       _searchLbl;
        private ToolStripComboBox    _filterBox;
        private ToolStripLabel       _lblTotal, _lblDone, _lblPct;
        private ToolStripProgressBar _progBar;
        private SplitContainer       _splitV;   // vertical: left=grid, right=detail
        private SplitContainer       _splitH;   // horizontal: top=splitV, bottom=log
        private DataGridView         _grid;
        private Panel                _detail;
        private Label                _dRowNum;
        private TextBox              _dOriginal;
        private TextBox              _dTranslation;
        private Label                _dCharCount;
        private FlowLayoutPanel      _dTags;
        private Label                _dInjectVal;
        private RichTextBox          _log;
        private StatusStrip          _status;
        private ToolStripStatusLabel _statusFile;

        // ── Constructor ────────────────────────────────────────────────
        public MainForm()
        {
            BuildUI();
            ApplyTheme();
            UpdateTitle();
            UpdateStats();
            this.Load += OnLoad;
            AddLog("BGI Translator siap. Drag & drop file script BGI untuk memulai.", "INFO");
        }

        // ── OnLoad: safe to set SplitterDistance here ─────────────────
        private void OnLoad(object sender, EventArgs e)
        {
            _splitH.Panel1MinSize = 200;
            _splitH.Panel2MinSize = 80;
            _splitV.Panel1MinSize = 320;
            _splitV.Panel2MinSize = 240;
            try
            {
                int h = _splitH.ClientSize.Height;
                _splitH.SplitterDistance = h > 300 ? h - 120 : h / 2;
            }
            catch { }
            try
            {
                int w = _splitV.ClientSize.Width;
                _splitV.SplitterDistance = w > 600 ? w - 280 : w * 2 / 3;
            }
            catch { }
            LayoutDetail();
        }

        // ══════════════════════════════════════════════════════════════
        //  FILE — OPEN
        // ══════════════════════════════════════════════════════════════
        private void OpenFile(string path)
        {
            if (_isDirty && !AskDiscard()) return;

            if (path == null)
            {
                using (OpenFileDialog d = new OpenFileDialog())
                {
                    d.Title  = "Buka Script BGI";
                    d.Filter = "Semua File|*";
                    if (d.ShowDialog() != DialogResult.OK) return;
                    path = d.FileName;
                }
            }

            if (!File.Exists(path))
            {
                MessageBox.Show("File tidak ditemukan:\n" + path, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Load DLL if not yet
            if (!_dll.IsLoaded)
            {
                string dllPath = Path.Combine(
                    Path.GetDirectoryName(Application.ExecutablePath) ?? ".",
                    "EthornellEditor.dll");

                if (!_dll.Load(dllPath))
                {
                    MessageBox.Show(
                        "Gagal memuat EthornellEditor.dll:\n\n" + _dll.LastError +
                        "\n\nPastikan file DLL ada di folder yang sama dengan BGITranslator.exe",
                        "Error DLL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                AddLog("EthornellEditor.dll dimuat.", "OK");
            }

            // Read raw bytes and call Import(byte[])
            long sz = new FileInfo(path).Length;
            AddLog("Membuka: " + Path.GetFileName(path) + " (" + sz.ToString("N0") + " bytes)...", "INFO");

            try
            {
                byte[] raw = File.ReadAllBytes(path);
                string[] result = _dll.Import(raw);

                if (result == null || result.Length == 0)
                {
                    AddLog("Import() mengembalikan 0 string. File mungkin bukan script BGI yang valid.", "WARN");
                    result = new string[0];
                }

                _originals    = result;
                _translations = new string[_originals.Length];

                // Copy originals to translations as starting point
                for (int i = 0; i < _originals.Length; i++)
                    _translations[i] = "";

                _currentFile = path;
                _isDirty     = false;

                AddLog("Berhasil: " + _originals.Length + " string ditemukan.", "OK");
                ApplyFilter();
                UpdateTitle();
                UpdateStats();
                AddRecentFile(path);
                _statusFile.Text = Path.GetFileName(path);
            }
            catch (Exception ex)
            {
                string msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                AddLog("Import() gagal: " + msg, "ERROR");
                MessageBox.Show("Gagal membuka file:\n\n" + msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  FILE — SAVE
        // ══════════════════════════════════════════════════════════════
        private void SaveFile(bool saveAs)
        {
            if (!_dll.IsLoaded || _originals.Length == 0)
            {
                MessageBox.Show("Buka file script terlebih dahulu.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string outPath = _currentFile;
            if (saveAs || outPath == null)
            {
                using (SaveFileDialog d = new SaveFileDialog())
                {
                    d.Title    = "Simpan Script BGI";
                    d.Filter   = "Semua File|*";
                    d.FileName = _currentFile != null ? Path.GetFileName(_currentFile) : "output";
                    if (d.ShowDialog() != DialogResult.OK) return;
                    outPath = d.FileName;
                }
            }

            try
            {
                AddLog("Menyimpan ke: " + Path.GetFileName(outPath) + "...", "INFO");

                // Build final translation array:
                // use translation if not empty, otherwise use original string
                string[] final = new string[_originals.Length];
                for (int i = 0; i < _originals.Length; i++)
                    final[i] = (_translations[i] != null && _translations[i] != "")
                        ? _translations[i]
                        : _originals[i];

                byte[] output = _dll.Export(final);
                File.WriteAllBytes(outPath, output);

                _currentFile = outPath;
                _isDirty     = false;
                UpdateTitle();
                AddLog("Tersimpan: " + Path.GetFileName(outPath) + " (" + output.Length.ToString("N0") + " bytes)", "OK");
                _statusFile.Text = Path.GetFileName(outPath);
            }
            catch (Exception ex)
            {
                string msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                AddLog("Export() gagal: " + msg, "ERROR");
                MessageBox.Show("Gagal menyimpan:\n\n" + msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  TSV
        // ══════════════════════════════════════════════════════════════
        private void ExportTSV()
        {
            if (_originals.Length == 0) { MessageBox.Show("Buka file script terlebih dahulu.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            using (SaveFileDialog d = new SaveFileDialog())
            {
                d.Title = "Export TSV"; d.Filter = "TSV|*.tsv|Text|*.txt";
                d.FileName = (_currentFile != null ? Path.GetFileNameWithoutExtension(_currentFile) : "export") + ".tsv";
                if (d.ShowDialog() != DialogResult.OK) return;
                using (StreamWriter sw = new StreamWriter(d.FileName, false, Encoding.UTF8))
                {
                    sw.WriteLine("# BGI Translator Export | " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    sw.WriteLine("# Index\tOriginal\tTerjemahan");
                    for (int i = 0; i < _originals.Length; i++)
                        sw.WriteLine(i + "\t" + _originals[i] + "\t" + _translations[i]);
                }
                AddLog("Export TSV: " + _originals.Length + " baris → " + Path.GetFileName(d.FileName), "OK");
            }
        }

        private void ImportTSV()
        {
            if (_originals.Length == 0) { MessageBox.Show("Buka file script terlebih dahulu.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            using (OpenFileDialog d = new OpenFileDialog())
            {
                d.Title = "Import TSV"; d.Filter = "TSV|*.tsv|Text|*.txt|Semua|*.*";
                if (d.ShowDialog() != DialogResult.OK) return;
                int count = 0;
                foreach (string line in File.ReadAllLines(d.FileName, Encoding.UTF8))
                {
                    if (line.StartsWith("#") || line.Trim() == "") continue;
                    string[] parts = line.Split('\t');
                    if (parts.Length < 3) continue;
                    int idx;
                    if (!int.TryParse(parts[0], out idx)) continue;
                    if (idx >= 0 && idx < _translations.Length)
                    { _translations[idx] = parts[2]; count++; }
                }
                _isDirty = true;
                ApplyFilter(); UpdateStats();
                AddLog("Import TSV: " + count + " terjemahan dimuat.", "OK");
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  FILTER & GRID
        // ══════════════════════════════════════════════════════════════
        private void ApplyFilter()
        {
            string f = "all";
            if (_filterBox.SelectedItem != null) f = ((FilterOpt)_filterBox.SelectedItem).Val;

            _viewIndices.Clear();
            for (int i = 0; i < _originals.Length; i++)
            {
                bool hasTrans = _translations[i] != null && _translations[i] != "";
                if (f == "empty" && hasTrans) continue;
                if (f == "done"  && !hasTrans) continue;
                _viewIndices.Add(i);
            }

            // Re-run search on new view
            if (_searchTerm != "") RunSearch();
            else { _searchHits.Clear(); _searchCur = -1; }

            RefreshGrid();
        }

        private void RefreshGrid()
        {
            _grid.SuspendLayout();
            _grid.Rows.Clear();

            for (int vi = 0; vi < _viewIndices.Count; vi++)
            {
                int i = _viewIndices[vi];
                string orig  = _originals[i]    ?? "";
                string trans = _translations[i] ?? "";
                string stat  = trans == "" ? "○" : "●";

                int ri = _grid.Rows.Add(i + 1, orig, trans, stat);
                _grid.Rows[ri].Tag = i;  // store original index

                // Search highlight
                if (_searchTerm != "")
                {
                    bool hit = orig.ToLowerInvariant().Contains(_searchTerm) ||
                               trans.ToLowerInvariant().Contains(_searchTerm);
                    bool cur = (_searchCur >= 0 && _searchCur < _searchHits.Count && _searchHits[_searchCur] == vi);

                    if (cur)       _grid.Rows[ri].DefaultCellStyle.BackColor = Clr.CurBg;
                    else if (hit)  _grid.Rows[ri].DefaultCellStyle.BackColor = Clr.HitBg;
                }
                else if (ri % 2 == 1)
                {
                    _grid.Rows[ri].DefaultCellStyle.BackColor = Clr.BgAlt;
                }

                if (trans == "")
                    _grid.Rows[ri].DefaultCellStyle.ForeColor = Clr.TxDim;
            }

            _grid.ResumeLayout();
            UpdateStats();
        }

        private void SelectGridRow(int gridRow)
        {
            if (gridRow < 0 || gridRow >= _grid.Rows.Count) return;
            _selViewRow = gridRow;

            object tag = _grid.Rows[gridRow].Tag;
            if (!(tag is int)) return;
            int i = (int)tag;

            _dRowNum.Text       = "Baris #" + (i + 1) + "  (index " + i + ")";
            _dOriginal.Text     = _originals[i]    ?? "";
            _dTranslation.Text  = _translations[i] ?? "";
            _dCharCount.Text    = (_translations[i] ?? "").Length + " karakter";
            ShowTags(_originals[i] ?? "");
        }

        // ══════════════════════════════════════════════════════════════
        //  SEARCH
        // ══════════════════════════════════════════════════════════════
        private void RunSearch()
        {
            _searchHits.Clear();
            _searchCur = -1;
            if (_searchTerm == "") { _searchLbl.Text = ""; return; }

            for (int vi = 0; vi < _viewIndices.Count; vi++)
            {
                int i = _viewIndices[vi];
                bool hit = (_originals[i]    ?? "").ToLowerInvariant().Contains(_searchTerm) ||
                           (_translations[i] ?? "").ToLowerInvariant().Contains(_searchTerm);
                if (hit) _searchHits.Add(vi);
            }

            _searchLbl.Text = _searchHits.Count == 0 ? "0 hasil" : "1 / " + _searchHits.Count;
            if (_searchHits.Count > 0) { _searchCur = 0; JumpToHit(0); }
        }

        private void DoSearch(string term)
        {
            _searchTerm = term.ToLowerInvariant();
            RunSearch();
            RefreshGrid();
        }

        private void NavSearch(int dir)
        {
            if (_searchHits.Count == 0) return;
            _searchCur = (_searchCur + dir + _searchHits.Count) % _searchHits.Count;
            _searchLbl.Text = (_searchCur + 1) + " / " + _searchHits.Count;
            JumpToHit(_searchCur);
            RefreshGrid();
        }

        private void JumpToHit(int hitIdx)
        {
            if (hitIdx < 0 || hitIdx >= _searchHits.Count) return;
            int vi = _searchHits[hitIdx];
            if (vi < 0 || vi >= _grid.Rows.Count) return;
            _grid.ClearSelection();
            _grid.Rows[vi].Selected = true;
            _grid.FirstDisplayedScrollingRowIndex = Math.Max(0, vi - 4);
            SelectGridRow(vi);
        }

        // ══════════════════════════════════════════════════════════════
        //  STATS
        // ══════════════════════════════════════════════════════════════
        private void UpdateStats()
        {
            int total = _originals.Length;
            int done  = 0;
            for (int i = 0; i < _translations.Length; i++)
                if (_translations[i] != null && _translations[i] != "") done++;

            int pct = total == 0 ? 0 : (int)((double)done / total * 100.0);
            _lblTotal.Text = "  Total: " + total;
            _lblDone.Text  = "  Selesai: " + done;
            _lblPct.Text   = "  " + pct + "%";
            _progBar.Maximum = total == 0 ? 1 : total;
            _progBar.Value   = done > _progBar.Maximum ? _progBar.Maximum : done;
            _dInjectVal.Text = done + " / " + total;
            _dInjectVal.ForeColor = (done == total && total > 0) ? Clr.Green : Clr.Gold;
        }

        private void UpdateTitle()
        {
            string d = _isDirty ? " •" : "";
            string f = _currentFile != null ? " — " + Path.GetFileName(_currentFile) : "";
            Text = "BGI Translator" + f + d;
        }

        private void MarkDirty() { _isDirty = true; UpdateTitle(); }

        private bool AskDiscard()
        {
            return MessageBox.Show(
                "Ada perubahan yang belum disimpan. Buang dan lanjutkan?",
                "Konfirmasi", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
        }

        // ══════════════════════════════════════════════════════════════
        //  TAG DETECTION
        // ══════════════════════════════════════════════════════════════
        private void ShowTags(string text)
        {
            _dTags.Controls.Clear();
            List<string> found = new List<string>();
            string[] pats = new string[] { @"\\n", @"\\t", @"\\\\", @"@[a-zA-Z]+", @"\[[^\]]{1,30}\]", @"\{[^\}]{1,30}\}" };
            foreach (string p in pats)
                foreach (Match m in Regex.Matches(text, p))
                    if (!found.Contains(m.Value)) found.Add(m.Value);

            if (found.Count == 0)
            {
                Label lbl = new Label(); lbl.Text = "tidak ada tag"; lbl.AutoSize = true;
                lbl.Font = new Font("Consolas", 9f); lbl.ForeColor = Clr.TxMuted;
                _dTags.Controls.Add(lbl); return;
            }

            foreach (string tag in found)
            {
                string t = tag;
                Button btn = new Button();
                btn.Text = t;
                btn.AutoSize = false;
                btn.Width = TextRenderer.MeasureText(t, new Font("Consolas", 9f)).Width + 20;
                btn.Height = 24;
                btn.FlatStyle = FlatStyle.Flat;
                btn.BackColor = Color.FromArgb(28, 48, 68);
                btn.ForeColor = Clr.Blue;
                btn.Font = new Font("Consolas", 9f);
                btn.Cursor = Cursors.Hand;
                btn.FlatAppearance.BorderColor = Color.FromArgb(48, 88, 118);
                btn.Click += delegate {
                    int pos = _dTranslation.SelectionStart;
                    _dTranslation.Text = _dTranslation.Text.Insert(pos, t);
                    _dTranslation.SelectionStart = pos + t.Length;
                    _dTranslation.Focus();
                };
                _dTags.Controls.Add(btn);
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  RECENT FILES
        // ══════════════════════════════════════════════════════════════
        private void AddRecentFile(string path)
        {
            for (int i = _recentFiles.Count - 1; i >= 0; i--)
                if (_recentFiles[i] == path) _recentFiles.RemoveAt(i);
            _recentFiles.Insert(0, path);
            if (_recentFiles.Count > 8) _recentFiles.RemoveAt(8);
            RebuildRecentMenu();
        }

        private void RebuildRecentMenu()
        {
            _mnuRecent.DropDownItems.Clear();
            if (_recentFiles.Count == 0) { var e = new ToolStripMenuItem("(kosong)"); e.Enabled = false; _mnuRecent.DropDownItems.Add(e); return; }
            foreach (string f in _recentFiles)
            {
                string fc = f;
                ToolStripMenuItem it = new ToolStripMenuItem(Path.GetFileName(fc));
                it.ToolTipText = fc;
                it.Click += delegate { OpenFile(fc); };
                _mnuRecent.DropDownItems.Add(it);
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  NAME DICTIONARY — karakter name header mapping
        // ══════════════════════════════════════════════════════════════
        // Deteksi otomatis: string pendek yang muncul berkali-kali
        // di script = kemungkinan besar nama karakter.
        // User tinggal isi kolom "Terjemahan", klik Apply.
        // Semua baris yang exact-match nama itu langsung diganti.
        // ══════════════════════════════════════════════════════════════
        private void OpenNameDict()
        {
            if (_originals.Length == 0)
            {
                MessageBox.Show("Buka file script terlebih dahulu.", "Info",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // ── Auto-detect nama karakter ─────────────────────────────
            // Kriteria: string pendek (1-12 char), muncul >= 2 kali,
            // tidak mengandung spasi/newline panjang, bukan perintah/path
            Dictionary<string, int> freq = new Dictionary<string, int>();
            foreach (string s in _originals)
            {
                if (s == null || s.Trim() == "") continue;
                string t = s.Trim();
                // Nama karakter biasanya: 1-12 karakter, tidak mengandung
                // titik/tanda baca panjang, tidak dimulai dengan @
                if (t.Length >= 1 && t.Length <= 12 &&
                    !t.StartsWith("@") && !t.StartsWith("//") &&
                    !t.Contains("\n") && !t.Contains(".bss") &&
                    !t.Contains("/")  && !t.Contains("\\") &&
                    !t.Contains("。") && !t.Contains("、") &&
                    !t.Contains("「") && !t.Contains("」") &&
                    !t.Contains("…"))
                {
                    if (freq.ContainsKey(t)) freq[t]++;
                    else freq[t] = 1;
                }
            }

            // Sort by frequency desc, keep entries that appear >= 2x
            List<KeyValuePair<string, int>> candidates = new List<KeyValuePair<string, int>>();
            foreach (KeyValuePair<string, int> kv in freq)
                if (kv.Value >= 2) candidates.Add(kv);
            candidates.Sort(delegate(KeyValuePair<string,int> a, KeyValuePair<string,int> b) {
                return b.Value.CompareTo(a.Value);
            });

            // ── Build dialog ──────────────────────────────────────────
            Form dlg = new Form();
            dlg.Text            = "Kamus Nama Karakter";
            dlg.Size            = new Size(600, 520);
            dlg.MinimumSize     = new Size(500, 400);
            dlg.StartPosition   = FormStartPosition.CenterParent;
            dlg.FormBorderStyle = FormBorderStyle.Sizable;
            dlg.BackColor       = Clr.BgPanel;
            dlg.ForeColor       = Clr.TxMain;

            // Info bar
            Label lblInfo = new Label();
            lblInfo.Dock      = DockStyle.Top;
            lblInfo.Height    = 52;
            lblInfo.Font      = new Font("Segoe UI", 8.5f);
            lblInfo.ForeColor = Clr.Blue;
            lblInfo.BackColor = Color.FromArgb(16, 22, 32);
            lblInfo.Padding   = new Padding(10, 8, 10, 0);
            lblInfo.Text      =
                "Nama karakter terdeteksi otomatis (pendek, muncul berulang).\n" +
                "Isi kolom Terjemahan, lalu klik Terapkan. Semua baris yang exact-match\n" +
                "nama tersebut akan langsung diganti sekaligus.";

            // Grid
            DataGridView dgv = new DataGridView();
            dgv.Dock                      = DockStyle.Fill;
            dgv.BackgroundColor           = Clr.BgDark;
            dgv.GridColor                 = Clr.Bdr;
            dgv.AllowUserToAddRows        = false;
            dgv.AllowUserToDeleteRows     = false;
            dgv.MultiSelect               = false;
            dgv.RowHeadersVisible         = false;
            dgv.SelectionMode             = DataGridViewSelectionMode.FullRowSelect;
            dgv.EditMode                  = DataGridViewEditMode.EditOnKeystrokeOrF2;
            dgv.EnableHeadersVisualStyles = false;
            dgv.DefaultCellStyle.BackColor          = Clr.BgInput;
            dgv.DefaultCellStyle.ForeColor          = Clr.TxMain;
            dgv.DefaultCellStyle.SelectionBackColor = Clr.SelBg;
            dgv.DefaultCellStyle.SelectionForeColor = Clr.Gold;
            dgv.DefaultCellStyle.Font               = new Font("MS Gothic", 10f);
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Clr.BgPanel;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Clr.TxMuted;
            dgv.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 8.5f);

            // Columns: Nama JP | Frekuensi | Terjemahan | Status
            var cJP   = new DataGridViewTextBoxColumn(); cJP.HeaderText = "Nama (JP)"; cJP.Width = 140; cJP.ReadOnly = true;
            cJP.DefaultCellStyle.Font = new Font("MS Gothic", 11f);
            cJP.DefaultCellStyle.ForeColor = Clr.TxDim;

            var cFreq = new DataGridViewTextBoxColumn(); cFreq.HeaderText = "Muncul"; cFreq.Width = 60; cFreq.ReadOnly = true;
            cFreq.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            cFreq.DefaultCellStyle.ForeColor  = Clr.TxMuted;
            cFreq.DefaultCellStyle.Font       = new Font("Segoe UI", 9f);

            var cTrans = new DataGridViewTextBoxColumn(); cTrans.HeaderText = "Terjemahan / Nama ID"; cTrans.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            cTrans.DefaultCellStyle.Font = new Font("Consolas", 10f);
            cTrans.DefaultCellStyle.ForeColor = Clr.TxMain;

            var cStat = new DataGridViewTextBoxColumn(); cStat.HeaderText = ""; cStat.Width = 24; cStat.ReadOnly = true;
            cStat.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            cStat.DefaultCellStyle.Font      = new Font("Segoe UI", 9f);

            dgv.Columns.Add(cJP); dgv.Columns.Add(cFreq); dgv.Columns.Add(cTrans); dgv.Columns.Add(cStat);

            // Populate rows
            foreach (KeyValuePair<string, int> kv in candidates)
            {
                // Check if already has a translation in any matching row
                string existingTrans = "";
                for (int i = 0; i < _originals.Length; i++)
                {
                    if (_originals[i] != null && _originals[i].Trim() == kv.Key &&
                        _translations[i] != null && _translations[i].Trim() != "")
                    { existingTrans = _translations[i].Trim(); break; }
                }
                string stat = existingTrans != "" ? "●" : "○";
                int ri = dgv.Rows.Add(kv.Key, kv.Value + "×", existingTrans, stat);
                if (existingTrans != "")
                    dgv.Rows[ri].DefaultCellStyle.ForeColor = Clr.Green;
            }

            // Update status dot when cell edited
            dgv.CellEndEdit += delegate(object s2, DataGridViewCellEventArgs e2) {
                if (e2.ColumnIndex != 2) return;
                string v = dgv.Rows[e2.RowIndex].Cells[2].Value != null
                    ? dgv.Rows[e2.RowIndex].Cells[2].Value.ToString() : "";
                dgv.Rows[e2.RowIndex].Cells[3].Value = v.Trim() != "" ? "●" : "○";
                dgv.Rows[e2.RowIndex].DefaultCellStyle.ForeColor = v.Trim() != "" ? Clr.Green : Clr.TxMain;
            };

            // Bottom panel
            Panel pnlBot = new Panel(); pnlBot.Dock = DockStyle.Bottom; pnlBot.Height = 48; pnlBot.BackColor = Clr.BgPanel;

            Label lblResult = new Label();
            lblResult.Text = candidates.Count + " nama terdeteksi.";
            lblResult.ForeColor = Clr.TxDim;
            lblResult.Font = new Font("Segoe UI", 8.5f);
            lblResult.SetBounds(10, 14, 280, 20);
            pnlBot.Controls.Add(lblResult);

            // Add manual row button
            Button btnAdd = MkBtn("+ Tambah Manual", 10);
            btnAdd.Top  = 10;
            btnAdd.Width = 130;
            btnAdd.Click += delegate {
                int ri = dgv.Rows.Add("", "", "", "○");
                dgv.ClearSelection();
                dgv.Rows[ri].Selected  = true;
                dgv.CurrentCell        = dgv.Rows[ri].Cells[0];
                // Make JP column editable for manual entries
                dgv.Columns[0].ReadOnly = false;
                dgv.BeginEdit(true);
            };
            pnlBot.Controls.Add(btnAdd);

            Button btnClose = MkBtn("Tutup", 0);
            btnClose.Anchor       = AnchorStyles.Right | AnchorStyles.Top;
            btnClose.DialogResult = DialogResult.Cancel;
            btnClose.Width        = 80;

            Button btnApply = MkBtn("Terapkan Semua", 0);
            btnApply.Anchor    = AnchorStyles.Right | AnchorStyles.Top;
            btnApply.ForeColor = Clr.Gold;
            btnApply.Width     = 140;
            btnApply.BackColor = Color.FromArgb(35, 32, 18);
            btnApply.FlatAppearance.BorderColor = Clr.GoldDim;

            pnlBot.Controls.Add(btnClose);
            pnlBot.Controls.Add(btnApply);
            pnlBot.Resize += delegate {
                int margin = 12;
                btnApply.Left = pnlBot.Width - btnApply.Width - margin;
                btnApply.Top  = 10;
                btnClose.Left = btnApply.Left - btnClose.Width - 8;
                btnClose.Top  = 10;
            };

            // ── Apply logic ───────────────────────────────────────────
            btnApply.Click += delegate
            {
                // Make sure JP column is read-only again for auto-rows
                dgv.Columns[0].ReadOnly = false; // we'll check per-row

                int totalReplaced = 0;
                int namesApplied  = 0;

                foreach (DataGridViewRow row in dgv.Rows)
                {
                    if (row.IsNewRow) continue;
                    string jp    = row.Cells[0].Value != null ? row.Cells[0].Value.ToString().Trim() : "";
                    string trans = row.Cells[2].Value != null ? row.Cells[2].Value.ToString().Trim() : "";

                    if (jp == "" || trans == "") continue;

                    // Replace ALL exact-match rows in the script
                    int count = 0;
                    for (int i = 0; i < _originals.Length; i++)
                    {
                        if (_originals[i] != null && _originals[i].Trim() == jp)
                        {
                            _translations[i] = trans;
                            count++;
                        }
                    }

                    if (count > 0)
                    {
                        namesApplied++;
                        totalReplaced += count;
                        // Update status dot in dialog grid
                        row.Cells[3].Value = "●";
                        row.DefaultCellStyle.ForeColor = Clr.Green;
                    }
                }

                if (totalReplaced > 0)
                {
                    MarkDirty();
                    ApplyFilter();
                    UpdateStats();
                    string msg = namesApplied + " nama diterapkan, " + totalReplaced + " baris diperbarui.";
                    lblResult.Text      = msg;
                    lblResult.ForeColor = Clr.Green;
                    AddLog("Kamus Nama: " + msg, "OK");
                }
                else
                {
                    lblResult.Text      = "Tidak ada yang diterapkan. Isi kolom Terjemahan terlebih dahulu.";
                    lblResult.ForeColor = Clr.TxMuted;
                }
            };

            dlg.Controls.Add(dgv);
            dlg.Controls.Add(lblInfo);
            dlg.Controls.Add(pnlBot);
            dlg.CancelButton = btnClose;
            dlg.Show(this); // non-modal
        }

        // ══════════════════════════════════════════════════════════════
        //  GLOSSARY
        // ══════════════════════════════════════════════════════════════
        private void OpenGlossary()
        {
            if (_originals.Length == 0) { MessageBox.Show("Buka file script terlebih dahulu.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            using (Form dlg = new Form())
            {
                dlg.Text = "Glosarium Otomatis"; dlg.Size = new Size(520, 440);
                dlg.StartPosition = FormStartPosition.CenterParent; dlg.BackColor = Clr.BgPanel;

                Label info = new Label();
                info.Text = "Term Jepang → Terjemahan. Hanya baris yang masih kosong akan diisi (kecuali 'Timpa' dicentang).";
                info.Dock = DockStyle.Top; info.Height = 36;
                info.Font = new Font("Segoe UI", 8.5f); info.ForeColor = Clr.Blue;
                info.BackColor = Color.FromArgb(16, 22, 32); info.Padding = new Padding(8, 6, 8, 0);

                DataGridView dgv = new DataGridView();
                dgv.Dock = DockStyle.Fill; dgv.BackgroundColor = Clr.BgDark; dgv.GridColor = Clr.Bdr;
                dgv.AllowUserToAddRows = false; dgv.AllowUserToDeleteRows = false;
                dgv.RowHeadersVisible = false; dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgv.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
                dgv.EnableHeadersVisualStyles = false;
                dgv.DefaultCellStyle.BackColor = Clr.BgInput; dgv.DefaultCellStyle.ForeColor = Clr.TxMain;
                dgv.DefaultCellStyle.SelectionBackColor = Clr.SelBg; dgv.DefaultCellStyle.SelectionForeColor = Clr.Gold;
                dgv.DefaultCellStyle.Font = new Font("Consolas", 9.5f);
                dgv.ColumnHeadersDefaultCellStyle.BackColor = Clr.BgPanel; dgv.ColumnHeadersDefaultCellStyle.ForeColor = Clr.TxMuted;
                var cJ = new DataGridViewTextBoxColumn(); cJ.HeaderText = "Teks Jepang"; cJ.Width = 200;
                var cI = new DataGridViewTextBoxColumn(); cI.HeaderText = "Terjemahan"; cI.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dgv.Columns.Add(cJ); dgv.Columns.Add(cI);

                // Default glossary
                string[][] gloss = new string[][] {
                    new string[]{"主人公","Protagonis"}, new string[]{"月曜日","Senin"}, new string[]{"火曜日","Selasa"},
                    new string[]{"水曜日","Rabu"}, new string[]{"木曜日","Kamis"}, new string[]{"金曜日","Jumat"},
                    new string[]{"土曜日","Sabtu"}, new string[]{"日曜日","Minggu"}, new string[]{"学校","Sekolah"},
                    new string[]{"教室","Kelas"}, new string[]{"廊下","Lorong"}, new string[]{"朝","Pagi"},
                    new string[]{"午後","Sore"}, new string[]{"夜","Malam"}, new string[]{"声","Suara"},
                    new string[]{"空","Langit"}, new string[]{"心","Hati"}, new string[]{"時間","Waktu"},
                };
                foreach (var g in gloss) dgv.Rows.Add(g[0], g[1]);

                CheckBox chk = new CheckBox(); chk.Text = "Timpa baris yang sudah ada terjemahannya";
                chk.Dock = DockStyle.Top; chk.Height = 26; chk.Font = new Font("Segoe UI", 9f);
                chk.ForeColor = Clr.TxDim; chk.BackColor = Clr.BgPanel; chk.Padding = new Padding(8, 0, 0, 0);

                Panel pnlBot = new Panel(); pnlBot.Dock = DockStyle.Bottom; pnlBot.Height = 44; pnlBot.BackColor = Clr.BgPanel;
                Button btnAdd = MkBtn("+ Tambah", 8); pnlBot.Controls.Add(btnAdd);
                btnAdd.Click += delegate { int r = dgv.Rows.Add("",""); dgv.ClearSelection(); dgv.Rows[r].Selected = true; dgv.CurrentCell = dgv.Rows[r].Cells[0]; dgv.BeginEdit(true); };
                Button btnDel = MkBtn("Hapus", 100); btnDel.ForeColor = Color.FromArgb(200,80,70); pnlBot.Controls.Add(btnDel);
                btnDel.Click += delegate { var rm = new List<DataGridViewRow>(); foreach (DataGridViewRow rx in dgv.SelectedRows) if (!rx.IsNewRow) rm.Add(rx); foreach (var rx in rm) dgv.Rows.Remove(rx); };
                Button btnOK = MkBtn("Terapkan", 0); btnOK.ForeColor = Clr.Gold;
                btnOK.Anchor = AnchorStyles.Right | AnchorStyles.Top; btnOK.DialogResult = DialogResult.OK;
                Button btnCx = MkBtn("Batal", 0); btnCx.Anchor = AnchorStyles.Right | AnchorStyles.Top; btnCx.DialogResult = DialogResult.Cancel;
                pnlBot.Controls.Add(btnOK); pnlBot.Controls.Add(btnCx);
                pnlBot.Resize += delegate { btnCx.Left = pnlBot.Width - 178; btnCx.Top = 8; btnOK.Left = pnlBot.Width - 88; btnOK.Top = 8; };

                btnOK.Click += delegate
                {
                    bool overwrite = chk.Checked; int count = 0;
                    foreach (DataGridViewRow row in dgv.Rows)
                    {
                        string jp = row.Cells[0].Value != null ? row.Cells[0].Value.ToString() : "";
                        string id = row.Cells[1].Value != null ? row.Cells[1].Value.ToString() : "";
                        if (jp == "" || id == "") continue;
                        for (int i = 0; i < _originals.Length; i++)
                        {
                            if (!overwrite && _translations[i] != null && _translations[i] != "") continue;
                            if ((_originals[i] ?? "").Contains(jp))
                            { _translations[i] = (_originals[i] ?? "").Replace(jp, id); count++; }
                        }
                    }
                    MessageBox.Show("Selesai: " + count + " baris diperbarui.", "Glosarium", MessageBoxButtons.OK, MessageBoxIcon.Information);
                };

                dlg.Controls.Add(dgv); dlg.Controls.Add(chk); dlg.Controls.Add(info); dlg.Controls.Add(pnlBot);
                dlg.AcceptButton = btnOK; dlg.CancelButton = btnCx;
                if (dlg.ShowDialog(this) == DialogResult.OK) { MarkDirty(); ApplyFilter(); UpdateStats(); }
            }
        }

        private static Button MkBtn(string text, int left)
        {
            Button b = new Button(); b.Text = text; b.Width = 82; b.Height = 28; b.Top = 8; b.Left = left;
            b.FlatStyle = FlatStyle.Flat; b.BackColor = Color.FromArgb(26, 26, 30);
            b.ForeColor = Clr.TxDim; b.Font = new Font("Segoe UI", 8.5f);
            b.FlatAppearance.BorderColor = Clr.Bdr2; return b;
        }

        // ══════════════════════════════════════════════════════════════
        //  GO TO LINE
        // ══════════════════════════════════════════════════════════════
        private void GoToLine()
        {
            using (Form dlg = new Form())
            {
                dlg.Text = "Pergi ke Baris"; dlg.Size = new Size(300, 130);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog; dlg.MaximizeBox = false; dlg.MinimizeBox = false;
                dlg.BackColor = Clr.BgPanel;
                Label lbl = new Label(); lbl.Text = "Nomor baris (1 – " + _viewIndices.Count + "):";
                lbl.ForeColor = Clr.TxDim; lbl.Font = new Font("Segoe UI", 9f); lbl.SetBounds(10, 12, 270, 20);
                TextBox txt = new TextBox(); txt.Text = "1"; txt.BackColor = Clr.BgInput; txt.ForeColor = Clr.TxMain;
                txt.BorderStyle = BorderStyle.FixedSingle; txt.Font = new Font("Segoe UI", 10f); txt.SetBounds(10, 34, 270, 24);
                Button bOK = new Button(); bOK.Text = "OK"; bOK.DialogResult = DialogResult.OK; bOK.FlatStyle = FlatStyle.Flat;
                bOK.BackColor = Color.FromArgb(35,32,18); bOK.ForeColor = Clr.Gold; bOK.Font = new Font("Segoe UI", 9f);
                bOK.FlatAppearance.BorderColor = Clr.GoldDim; bOK.SetBounds(110, 62, 80, 26);
                Button bCx = new Button(); bCx.Text = "Batal"; bCx.DialogResult = DialogResult.Cancel; bCx.FlatStyle = FlatStyle.Flat;
                bCx.BackColor = Clr.BgInput; bCx.ForeColor = Clr.TxDim; bCx.Font = new Font("Segoe UI", 9f);
                bCx.FlatAppearance.BorderColor = Clr.Bdr2; bCx.SetBounds(200, 62, 80, 26);
                dlg.Controls.Add(lbl); dlg.Controls.Add(txt); dlg.Controls.Add(bOK); dlg.Controls.Add(bCx);
                dlg.AcceptButton = bOK; dlg.CancelButton = bCx;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    int line; if (int.TryParse(txt.Text, out line) && line >= 1 && line <= _viewIndices.Count)
                    { int gi = line - 1; _grid.ClearSelection(); _grid.Rows[gi].Selected = true; _grid.FirstDisplayedScrollingRowIndex = Math.Max(0, gi - 4); SelectGridRow(gi); }
                }
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  REPLACE (Find & Replace dialog)
        // ══════════════════════════════════════════════════════════════
        private void OpenReplace()
        {
            if (_originals.Length == 0)
            {
                MessageBox.Show("Buka file script terlebih dahulu.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Form dlg = new Form();
            dlg.Text            = "Cari & Ganti";
            dlg.Size            = new Size(460, 280);
            dlg.MinimumSize     = new Size(400, 280);
            dlg.MaximumSize     = new Size(700, 280);
            dlg.StartPosition   = FormStartPosition.CenterParent;
            dlg.FormBorderStyle = FormBorderStyle.Sizable;
            dlg.MaximizeBox     = false;
            dlg.BackColor       = Clr.BgPanel;
            dlg.ForeColor       = Clr.TxMain;
            dlg.ShowInTaskbar   = false;

            // ── Scope radio ───────────────────────────────────────────
            GroupBox grpScope = new GroupBox();
            grpScope.Text      = "Lingkup";
            grpScope.ForeColor = Clr.TxMuted;
            grpScope.Font      = new Font("Segoe UI", 8.5f);
            grpScope.SetBounds(10, 8, 200, 52);
            grpScope.BackColor = Clr.BgPanel;
            grpScope.FlatStyle = FlatStyle.Flat;

            RadioButton rAll = new RadioButton();
            rAll.Text      = "Semua baris";
            rAll.Checked   = true;
            rAll.ForeColor = Clr.TxDim;
            rAll.Font      = new Font("Segoe UI", 8.5f);
            rAll.SetBounds(8, 18, 90, 22);
            rAll.BackColor = Color.Transparent;

            RadioButton rSel = new RadioButton();
            rSel.Text      = "Hanya view sekarang";
            rSel.ForeColor = Clr.TxDim;
            rSel.Font      = new Font("Segoe UI", 8.5f);
            rSel.SetBounds(100, 18, 150, 22);
            rSel.BackColor = Color.Transparent;

            grpScope.Controls.Add(rAll);
            grpScope.Controls.Add(rSel);

            // ── Case-sensitive checkbox ───────────────────────────────
            CheckBox chkCase = new CheckBox();
            chkCase.Text      = "Case sensitive";
            chkCase.ForeColor = Clr.TxDim;
            chkCase.Font      = new Font("Segoe UI", 8.5f);
            chkCase.SetBounds(218, 20, 120, 22);
            chkCase.BackColor = Clr.BgPanel;

            // ── Search in ─────────────────────────────────────────────
            CheckBox chkInOrig = new CheckBox();
            chkInOrig.Text      = "Cari di teks JP juga";
            chkInOrig.ForeColor = Clr.TxDim;
            chkInOrig.Font      = new Font("Segoe UI", 8.5f);
            chkInOrig.SetBounds(218, 42, 160, 22);
            chkInOrig.BackColor = Clr.BgPanel;

            // ── Find field ────────────────────────────────────────────
            Label lblFind = new Label();
            lblFind.Text      = "CARI";
            lblFind.ForeColor = Clr.TxMuted;
            lblFind.Font      = new Font("Segoe UI", 7.5f);
            lblFind.SetBounds(10, 68, 60, 16);

            TextBox txtFind = new TextBox();
            txtFind.BackColor   = Clr.BgInput;
            txtFind.ForeColor   = Clr.TxMain;
            txtFind.BorderStyle = BorderStyle.FixedSingle;
            txtFind.Font        = new Font("Consolas", 10f);
            txtFind.SetBounds(10, 86, 420, 26);

            // Seed from current search term if any
            if (_searchTerm != "") txtFind.Text = _searchTerm;

            // ── Replace field ─────────────────────────────────────────
            Label lblRepl = new Label();
            lblRepl.Text      = "GANTI DENGAN";
            lblRepl.ForeColor = Clr.TxMuted;
            lblRepl.Font      = new Font("Segoe UI", 7.5f);
            lblRepl.SetBounds(10, 118, 120, 16);

            TextBox txtRepl = new TextBox();
            txtRepl.BackColor   = Clr.BgInput;
            txtRepl.ForeColor   = Clr.TxMain;
            txtRepl.BorderStyle = BorderStyle.FixedSingle;
            txtRepl.Font        = new Font("Consolas", 10f);
            txtRepl.SetBounds(10, 136, 420, 26);

            // ── Result label ──────────────────────────────────────────
            Label lblResult = new Label();
            lblResult.Text      = "";
            lblResult.ForeColor = Clr.Gold;
            lblResult.Font      = new Font("Segoe UI", 8.5f);
            lblResult.SetBounds(10, 168, 260, 20);

            // ── Buttons ───────────────────────────────────────────────
            Button btnReplOne = MkBtn2("Ganti Ini", Clr.TxDim,  300, 168, 60);
            Button btnReplAll = MkBtn2("Ganti Semua", Clr.Gold, 368, 168, 80);
            btnReplAll.FlatAppearance.BorderColor = Clr.GoldDim;
            btnReplAll.BackColor = Color.FromArgb(35, 32, 18);

            Button btnClose = MkBtn2("Tutup", Clr.TxDim, 370, 204, 60);
            btnClose.DialogResult = DialogResult.Cancel;

            // ── "Ganti Ini" logic ─────────────────────────────────────
            btnReplOne.Click += delegate
            {
                string find = txtFind.Text;
                string repl = txtRepl.Text;
                if (find == "") { lblResult.Text = "Isi dulu kolom CARI."; return; }

                // Jump to next hit first if nothing selected
                if (_searchTerm != find)
                {
                    _searchTerm = chkCase.Checked ? find : find.ToLowerInvariant();
                    RunSearch(); RefreshGrid();
                }

                if (_searchHits.Count == 0) { lblResult.Text = "Tidak ditemukan."; lblResult.ForeColor = Clr.TxMuted; return; }

                // Replace at current hit
                int vi = _searchHits[_searchCur < 0 ? 0 : _searchCur];
                int i  = _viewIndices[vi];
                string current = _translations[i] ?? "";
                StringComparison sc = chkCase.Checked ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

                if (current.IndexOf(find, sc) >= 0)
                {
                    _translations[i] = ReplaceFirst(current, find, repl, sc);
                    _grid.Rows[vi].Cells[2].Value = _translations[i];
                    _grid.Rows[vi].Cells[3].Value = _translations[i] == "" ? "○" : "●";
                    if (_selViewRow == vi) { _dTranslation.Text = _translations[i]; _dCharCount.Text = _translations[i].Length + " karakter"; }
                    MarkDirty(); UpdateStats();
                    lblResult.Text = "1 penggantian dilakukan."; lblResult.ForeColor = Clr.Green;
                    NavSearch(1); RefreshGrid();
                }
                else
                {
                    NavSearch(1); RefreshGrid();
                    lblResult.Text = "Lanjut ke hasil berikutnya…"; lblResult.ForeColor = Clr.TxMuted;
                }
            };

            // ── "Ganti Semua" logic ───────────────────────────────────
            btnReplAll.Click += delegate
            {
                string find = txtFind.Text;
                string repl = txtRepl.Text;
                if (find == "") { lblResult.Text = "Isi dulu kolom CARI."; lblResult.ForeColor = Clr.TxMuted; return; }

                StringComparison sc = chkCase.Checked ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

                // Determine which indices to work on
                List<int> targets = new List<int>();
                if (rSel.Checked)
                {
                    // Only visible rows in current view
                    foreach (int vi in _viewIndices) targets.Add(vi);
                }
                else
                {
                    // All rows
                    for (int i = 0; i < _translations.Length; i++) targets.Add(i);
                }

                int count = 0;
                foreach (int i in targets)
                {
                    string src = "";
                    // Check terjemahan
                    if (_translations[i] != null && _translations[i].IndexOf(find, sc) >= 0)
                    {
                        _translations[i] = ReplaceAll(_translations[i], find, repl, sc);
                        count++;
                    }
                    // Check JP juga kalau dicentang (replace ke terjemahan based on JP match)
                    else if (chkInOrig.Checked && (_originals[i] ?? "").IndexOf(find, sc) >= 0)
                    {
                        _translations[i] = ReplaceAll(_originals[i], find, repl, sc);
                        count++;
                    }
                }

                if (count > 0)
                {
                    MarkDirty();
                    ApplyFilter();
                    UpdateStats();
                    lblResult.Text = count + " penggantian dilakukan.";
                    lblResult.ForeColor = Clr.Green;
                    AddLog("Replace All: \"" + find + "\" → \"" + repl + "\" | " + count + " baris.", "OK");
                }
                else
                {
                    lblResult.Text = "Tidak ada yang cocok.";
                    lblResult.ForeColor = Clr.TxMuted;
                }
            };

            dlg.Controls.AddRange(new Control[] {
                grpScope, chkCase, chkInOrig,
                lblFind, txtFind,
                lblRepl, txtRepl,
                lblResult, btnReplOne, btnReplAll, btnClose
            });

            dlg.CancelButton = btnClose;
            dlg.KeyPreview   = true;
            dlg.KeyDown += delegate(object s, KeyEventArgs e) {
                if (e.KeyCode == Keys.Escape) dlg.Close();
                if (e.KeyCode == Keys.Return && !e.Shift) { btnReplOne.PerformClick(); e.Handled = true; }
            };

            txtFind.Focus();
            dlg.Show(this);  // non-modal so user can still see grid
        }

        // ── String replace helpers ─────────────────────────────────────
        private static string ReplaceFirst(string src, string find, string repl, StringComparison sc)
        {
            int idx = src.IndexOf(find, sc);
            if (idx < 0) return src;
            return src.Substring(0, idx) + repl + src.Substring(idx + find.Length);
        }

        private static string ReplaceAll(string src, string find, string repl, StringComparison sc)
        {
            if (src == null || find == "") return src;
            StringBuilder sb = new StringBuilder();
            int start = 0;
            while (true)
            {
                int idx = src.IndexOf(find, start, sc);
                if (idx < 0) { sb.Append(src, start, src.Length - start); break; }
                sb.Append(src, start, idx - start);
                sb.Append(repl);
                start = idx + find.Length;
            }
            return sb.ToString();
        }

        private static Button MkBtn2(string text, Color fc, int x, int y, int w)
        {
            Button b = new Button();
            b.Text = text; b.Width = w; b.Height = 26; b.Left = x; b.Top = y;
            b.FlatStyle = FlatStyle.Flat; b.BackColor = Color.FromArgb(26, 26, 30);
            b.ForeColor = fc; b.Font = new Font("Segoe UI", 8.5f);
            b.FlatAppearance.BorderColor = Clr.Bdr2;
            return b;
        }

        // ══════════════════════════════════════════════════════════════
        //  LOG
        // ══════════════════════════════════════════════════════════════
        private void AddLog(string msg, string level)
        {
            string line = DateTime.Now.ToString("HH:mm:ss") + "  [" + level + "]  " + msg + "\r\n";
            _log.AppendText(line);
            _log.ScrollToCaret();
        }

        // ══════════════════════════════════════════════════════════════
        //  KEYBOARD SHORTCUTS
        // ══════════════════════════════════════════════════════════════
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Control && e.KeyCode == Keys.O) { OpenFile(null); e.Handled = true; }
            if (e.Control && e.KeyCode == Keys.S) { SaveFile(false); e.Handled = true; }
            if (e.Control && e.KeyCode == Keys.F) { _searchBox.Focus(); _searchBox.SelectAll(); e.Handled = true; }
            if (e.Control && e.KeyCode == Keys.H) { OpenReplace(); e.Handled = true; }
            if (e.Control && e.KeyCode == Keys.G) { GoToLine(); e.Handled = true; }
            if (e.Control && e.KeyCode == Keys.N) { OpenNameDict(); e.Handled = true; }
            if (e.KeyCode == Keys.F3 && !e.Shift) { NavSearch(1);  e.Handled = true; }
            if (e.KeyCode == Keys.F3 && e.Shift)  { NavSearch(-1); e.Handled = true; }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_isDirty && !AskDiscard()) e.Cancel = true;
            base.OnFormClosing(e);
        }

        // ══════════════════════════════════════════════════════════════
        //  THEME
        // ══════════════════════════════════════════════════════════════
        private void ApplyTheme()
        {
            BackColor = Clr.BgDark; ForeColor = Clr.TxMain;
            DarkRenderer rnd = new DarkRenderer();
            _menu.BackColor = Clr.BgPanel; _menu.ForeColor = Clr.TxDim; _menu.Renderer = rnd;
            _tools.BackColor = Clr.BgPanel; _tools.ForeColor = Clr.TxDim; _tools.Renderer = rnd;

            _grid.BackgroundColor = Clr.BgDark; _grid.GridColor = Clr.Bdr;
            _grid.DefaultCellStyle.BackColor = Clr.BgDark; _grid.DefaultCellStyle.ForeColor = Clr.TxMain;
            _grid.DefaultCellStyle.SelectionBackColor = Clr.SelBg; _grid.DefaultCellStyle.SelectionForeColor = Clr.Gold;
            _grid.DefaultCellStyle.Font = new Font("Consolas", 9.5f);
            _grid.ColumnHeadersDefaultCellStyle.BackColor = Clr.BgPanel;
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = Clr.TxMuted;
            _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5f);
            _grid.AlternatingRowsDefaultCellStyle.BackColor = Clr.BgAlt;
            _grid.EnableHeadersVisualStyles = false;

            _dOriginal.BackColor = Clr.BgInput; _dOriginal.ForeColor = Clr.TxDim;
            _dTranslation.BackColor = Clr.BgInput; _dTranslation.ForeColor = Clr.TxMain;
            _searchBox.BackColor = Clr.BgInput; _searchBox.ForeColor = Clr.TxMain;

            _detail.BackColor = Clr.BgPanel;
            _splitH.BackColor = Clr.Bdr; _splitV.BackColor = Clr.Bdr;
            _log.BackColor = Color.FromArgb(10,10,11); _log.ForeColor = Color.FromArgb(80,175,80); _log.Font = new Font("Consolas", 8.5f);
            _status.BackColor = Clr.BgPanel; _statusFile.ForeColor = Clr.TxDim;
        }

        // ══════════════════════════════════════════════════════════════
        //  DETAIL PANEL LAYOUT (explicit bounds, called on resize)
        // ══════════════════════════════════════════════════════════════
        private void LayoutDetail()
        {
            if (_detail == null) return;
            int cw = _detail.ClientSize.Width - 16;
            if (cw < 10) return;
            int x = 8, y = 8;
            _dRowNum.SetBounds(x, y, cw, 22);     y += 26;
            // original header + box
            _detail.Controls["lblOriH"].SetBounds(x, y, cw, 16); y += 18;
            _dOriginal.SetBounds(x, y, cw, 78);   y += 82;
            // translation header + box
            _detail.Controls["lblTransH"].SetBounds(x, y, cw, 18); y += 20;
            _dTranslation.SetBounds(x, y, cw, 88); y += 92;
            _dCharCount.SetBounds(x, y, cw, 16);   y += 20;
            // tags header + panel
            _detail.Controls["lblTagH"].SetBounds(x, y, cw, 16); y += 18;
            _dTags.SetBounds(x, y, cw, 54);        y += 58;
            // tip panel
            _detail.Controls["pnlTip"].SetBounds(x, y, cw, 56); y += 60;
            // inject info
            _detail.Controls["pnlInj"].SetBounds(x, y, cw, 86);
        }

        // ══════════════════════════════════════════════════════════════
        //  BUILD UI
        // ══════════════════════════════════════════════════════════════
        private void BuildUI()
        {
            SuspendLayout();

            // ── Menu ──────────────────────────────────────────────────
            _menu      = new MenuStrip(); _menu.Dock = DockStyle.Top;
            var mFile  = new ToolStripMenuItem("File");
            var mEdit  = new ToolStripMenuItem("Edit");
            var mTools = new ToolStripMenuItem("Tools");
            var mHelp  = new ToolStripMenuItem("Bantuan");

            Add(mFile, "Buka Script BGI\tCtrl+O",  delegate { OpenFile(null); });
            Add(mFile, "Simpan\tCtrl+S",             delegate { SaveFile(false); });
            Add(mFile, "Simpan Sebagai…",            delegate { SaveFile(true); });
            _mnuRecent = new ToolStripMenuItem("File Terakhir");
            mFile.DropDownItems.Add(_mnuRecent);
            mFile.DropDownItems.Add(new ToolStripSeparator());
            Add(mFile, "Export TSV…",               delegate { ExportTSV(); });
            Add(mFile, "Import TSV…",               delegate { ImportTSV(); });
            mFile.DropDownItems.Add(new ToolStripSeparator());
            Add(mFile, "Keluar",                    delegate { Close(); });

            Add(mEdit, "Cari\tCtrl+F",              delegate { _searchBox.Focus(); _searchBox.SelectAll(); });
            Add(mEdit, "Cari & Ganti\tCtrl+H",      delegate { OpenReplace(); });
            Add(mEdit, "Pergi ke Baris\tCtrl+G",    delegate { GoToLine(); });

            Add(mTools, "Glosarium Otomatis…",      delegate { OpenGlossary(); });
            Add(mTools, "Kamus Nama Karakter…\tCtrl+N", delegate { OpenNameDict(); });
            Add(mTools, "Salin Semua Teks JP",      delegate { CopyAllJP(); });
            mTools.DropDownItems.Add(new ToolStripSeparator());
            Add(mTools, "Statistik…",               delegate { ShowStats(); });

            Add(mHelp, "Pintasan Keyboard",         delegate { ShowShortcuts(); });
            mHelp.DropDownItems.Add(new ToolStripSeparator());
            Add(mHelp, "Tentang",                   delegate { ShowAbout(); });

            _menu.Items.AddRange(new ToolStripItem[] { mFile, mEdit, mTools, mHelp });

            // ── Toolbar ───────────────────────────────────────────────
            _tools = new ToolStrip(); _tools.Dock = DockStyle.Top; _tools.GripStyle = ToolStripGripStyle.Hidden; _tools.Padding = new Padding(4, 2, 4, 2);
            _tools.Items.Add(TsBtn("📂 Buka",  delegate { OpenFile(null); }, "Buka file BGI (Ctrl+O)"));
            _tools.Items.Add(TsBtn("💾 Simpan", delegate { SaveFile(false); }, "Simpan (Ctrl+S)"));
            _tools.Items.Add(new ToolStripSeparator());
            _tools.Items.Add(new ToolStripLabel("  🔍 "));

            _searchBox = new ToolStripTextBox(); _searchBox.Width = 200;
            _searchBox.ToolTipText = "Cari (Ctrl+F)";
            _searchBox.TextChanged += delegate { DoSearch(_searchBox.Text); };
            _searchBox.KeyDown += delegate(object s, KeyEventArgs e) {
                if (e.KeyCode == Keys.Enter && !e.Shift) { NavSearch(1);  e.Handled = e.SuppressKeyPress = true; }
                if (e.KeyCode == Keys.Enter && e.Shift)  { NavSearch(-1); e.Handled = e.SuppressKeyPress = true; }
                if (e.KeyCode == Keys.Escape) { _searchBox.Text = ""; DoSearch(""); _grid.Focus(); e.Handled = e.SuppressKeyPress = true; }
            };
            _tools.Items.Add(_searchBox);
            _tools.Items.Add(TsBtn("◀", delegate { NavSearch(-1); }, "Sebelumnya (Shift+F3)"));
            _tools.Items.Add(TsBtn("▶", delegate { NavSearch(1);  }, "Berikutnya (F3)"));
            _searchLbl = new ToolStripLabel(""); _searchLbl.ForeColor = Clr.Gold; _tools.Items.Add(_searchLbl);
            _tools.Items.Add(new ToolStripSeparator());
            _tools.Items.Add(new ToolStripLabel("  Filter: "));

            _filterBox = new ToolStripComboBox(); _filterBox.DropDownStyle = ComboBoxStyle.DropDownList; _filterBox.Width = 168;
            _filterBox.Items.Add(new FilterOpt("Semua baris", "all"));
            _filterBox.Items.Add(new FilterOpt("Belum diterjemahkan", "empty"));
            _filterBox.Items.Add(new FilterOpt("Sudah selesai", "done"));
            _filterBox.SelectedIndex = 0;
            _filterBox.SelectedIndexChanged += delegate { ApplyFilter(); };
            _filterBox.ComboBox.BackColor = Clr.BgInput; _filterBox.ComboBox.ForeColor = Clr.TxDim;
            _tools.Items.Add(_filterBox);
            _tools.Items.Add(new ToolStripSeparator());
            _lblTotal = new ToolStripLabel("  Total: 0");    _lblTotal.ForeColor = Clr.TxDim;   _tools.Items.Add(_lblTotal);
            _lblDone  = new ToolStripLabel("  Selesai: 0");  _lblDone.ForeColor  = Clr.Green;   _tools.Items.Add(_lblDone);
            _lblPct   = new ToolStripLabel("  0%");          _lblPct.ForeColor   = Clr.Gold;    _tools.Items.Add(_lblPct);
            _progBar  = new ToolStripProgressBar(); _progBar.Width = 110; _progBar.Minimum = 0; _progBar.Maximum = 1; _tools.Items.Add(_progBar);

            // ── Split containers (no SplitterDistance here!) ──────────
            _splitH = new SplitContainer(); _splitH.Dock = DockStyle.Fill; _splitH.Orientation = Orientation.Horizontal; _splitH.SplitterWidth = 3;
            _splitV = new SplitContainer(); _splitV.Dock = DockStyle.Fill; _splitV.Orientation = Orientation.Vertical;   _splitV.SplitterWidth = 3;

            // ── DataGridView ──────────────────────────────────────────
            _grid = new DataGridView(); _grid.Dock = DockStyle.Fill;
            _grid.AllowUserToAddRows = false; _grid.AllowUserToDeleteRows = false; _grid.AllowUserToResizeRows = false;
            _grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            _grid.MultiSelect = false; _grid.RowHeadersVisible = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
            _grid.ScrollBars = ScrollBars.Both; _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;

            var cIdx   = new DataGridViewTextBoxColumn(); cIdx.HeaderText = "#"; cIdx.Width = 46; cIdx.ReadOnly = true; cIdx.DefaultCellStyle.ForeColor = Clr.TxMuted; cIdx.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            var cOrig  = new DataGridViewTextBoxColumn(); cOrig.HeaderText = "Teks Asli (JP)"; cOrig.Width = 300; cOrig.ReadOnly = true; cOrig.DefaultCellStyle.WrapMode = DataGridViewTriState.True; cOrig.DefaultCellStyle.Font = new Font("MS Gothic", 9.5f); cOrig.DefaultCellStyle.ForeColor = Clr.TxDim;
            var cTrans = new DataGridViewTextBoxColumn(); cTrans.HeaderText = "Terjemahan (ID)"; cTrans.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; cTrans.DefaultCellStyle.WrapMode = DataGridViewTriState.True; cTrans.DefaultCellStyle.ForeColor = Clr.TxMain;
            var cStat  = new DataGridViewTextBoxColumn(); cStat.HeaderText = ""; cStat.Width = 26; cStat.ReadOnly = true; cStat.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _grid.Columns.Add(cIdx); _grid.Columns.Add(cOrig); _grid.Columns.Add(cTrans); _grid.Columns.Add(cStat);

            _grid.SelectionChanged += delegate { if (_grid.SelectedRows.Count == 1) SelectGridRow(_grid.SelectedRows[0].Index); };
            _grid.CellEndEdit += delegate(object s, DataGridViewCellEventArgs ev) {
                if (ev.ColumnIndex != 2) return;
                object tag = _grid.Rows[ev.RowIndex].Tag;
                if (!(tag is int)) return;
                int i = (int)tag;
                string val = _grid.Rows[ev.RowIndex].Cells[2].Value != null ? _grid.Rows[ev.RowIndex].Cells[2].Value.ToString() : "";
                _translations[i] = val;
                _grid.Rows[ev.RowIndex].Cells[3].Value = val == "" ? "○" : "●";
                MarkDirty(); UpdateStats();
                if (_selViewRow == ev.RowIndex) { _dTranslation.Text = val; _dCharCount.Text = val.Length + " karakter"; }
            };

            _splitV.Panel1.Controls.Add(_grid);

            // ── Detail panel ──────────────────────────────────────────
            _detail = new Panel(); _detail.Dock = DockStyle.Fill; _detail.AutoScroll = false;
            _detail.Resize += delegate { LayoutDetail(); };

            _dRowNum = MkLbl("— Pilih baris —", new Font("Segoe UI", 9f, FontStyle.Bold), Clr.Gold);

            var lblOriH = MkLbl("TEKS ASLI", new Font("Segoe UI", 7.5f), Clr.TxMuted); lblOriH.Name = "lblOriH";
            _dOriginal = new TextBox(); _dOriginal.Multiline = true; _dOriginal.ReadOnly = true; _dOriginal.ScrollBars = ScrollBars.Vertical;
            _dOriginal.Font = new Font("MS Gothic", 10f); _dOriginal.BorderStyle = BorderStyle.FixedSingle; _dOriginal.Padding = new Padding(3);

            var lblTransH = MkLbl("TERJEMAHAN", new Font("Segoe UI", 7.5f), Clr.TxMuted); lblTransH.Name = "lblTransH";
            _dTranslation = new TextBox(); _dTranslation.Multiline = true; _dTranslation.ScrollBars = ScrollBars.Vertical;
            _dTranslation.Font = new Font("Consolas", 10f); _dTranslation.BorderStyle = BorderStyle.FixedSingle; _dTranslation.Padding = new Padding(3);
            _dTranslation.TextChanged += delegate {
                if (_selViewRow < 0 || _selViewRow >= _grid.Rows.Count) return;
                object tag = _grid.Rows[_selViewRow].Tag;
                if (!(tag is int)) return;
                int i = (int)tag;
                string val = _dTranslation.Text;
                _translations[i] = val;
                _grid.Rows[_selViewRow].Cells[2].Value = val;
                _grid.Rows[_selViewRow].Cells[3].Value = val == "" ? "○" : "●";
                _dCharCount.Text = val.Length + " karakter";
                MarkDirty(); UpdateStats();
            };

            _dCharCount = MkLbl("0 karakter", new Font("Segoe UI", 7.5f), Clr.TxMuted); _dCharCount.TextAlign = ContentAlignment.MiddleRight;
            var lblTagH = MkLbl("TAG  (klik untuk sisipkan)", new Font("Segoe UI", 7.5f), Clr.TxMuted); lblTagH.Name = "lblTagH";

            _dTags = new FlowLayoutPanel(); _dTags.BackColor = Color.FromArgb(15,15,17); _dTags.Padding = new Padding(4); _dTags.AutoScroll = true;

            // Tip panel
            Panel pnlTip = new Panel(); pnlTip.Name = "pnlTip"; pnlTip.BackColor = Color.FromArgb(15,21,30);
            pnlTip.Paint += delegate(object s, PaintEventArgs e) { using (Pen p = new Pen(Color.FromArgb(44,76,106))) e.Graphics.DrawRectangle(p, 0, 0, pnlTip.Width-1, pnlTip.Height-1); };
            Label lblTip = new Label(); lblTip.Dock = DockStyle.Fill; lblTip.Text = "💡  \\n = baris baru  ·  \\t = tab\n    Jangan hapus tag dari terjemahan!\n    Klik tag di atas untuk menyisipkan.";
            lblTip.ForeColor = Clr.Blue; lblTip.Font = new Font("Segoe UI", 8f); lblTip.BackColor = Color.Transparent; lblTip.Padding = new Padding(6,4,4,4);
            pnlTip.Controls.Add(lblTip);

            // Inject info panel
            Panel pnlInj = new Panel(); pnlInj.Name = "pnlInj"; pnlInj.BackColor = Color.FromArgb(14,14,16);
            pnlInj.Paint += delegate(object s, PaintEventArgs e) { using (Pen p = new Pen(Clr.Bdr2)) e.Graphics.DrawRectangle(p, 0, 0, pnlInj.Width-1, pnlInj.Height-1); };
            Label iHdr = new Label(); iHdr.Text = "  INFO INJEKSI"; iHdr.Font = new Font("Segoe UI", 7.5f, FontStyle.Bold); iHdr.ForeColor = Clr.TxMuted; iHdr.BackColor = Color.Transparent; iHdr.AutoSize = false; iHdr.SetBounds(0,0,240,20);
            Label iL1 = ILbl("Encoding", 8, 22); Label iV1 = IVal("Shift-JIS", Clr.Green, 110, 22);
            Label iL2 = ILbl("Engine",   8, 42); Label iV2 = IVal("Ethornell/BGI", Clr.TxDim, 110, 42);
            Label iL3 = ILbl("Selesai",  8, 62);
            _dInjectVal = IVal("0 / 0", Clr.Gold, 110, 62);
            pnlInj.Controls.AddRange(new Control[] { iHdr, iL1, iV1, iL2, iV2, iL3, _dInjectVal });

            _detail.Controls.AddRange(new Control[] { _dRowNum, lblOriH, _dOriginal, lblTransH, _dTranslation, _dCharCount, lblTagH, _dTags, pnlTip, pnlInj });
            _splitV.Panel2.Controls.Add(_detail);

            _splitH.Panel1.Controls.Add(_splitV);

            // ── Log panel ─────────────────────────────────────────────
            Panel pnlLog = new Panel(); pnlLog.Dock = DockStyle.Fill;
            Label logHdr = new Label(); logHdr.Text = "  LOG OUTPUT"; logHdr.Dock = DockStyle.Top; logHdr.Height = 22;
            logHdr.Font = new Font("Segoe UI", 7.5f, FontStyle.Bold); logHdr.ForeColor = Clr.TxMuted; logHdr.BackColor = Color.FromArgb(13,13,14); logHdr.TextAlign = ContentAlignment.MiddleLeft;
            _log = new RichTextBox(); _log.Dock = DockStyle.Fill; _log.ReadOnly = true; _log.BorderStyle = BorderStyle.None;
            _log.ScrollBars = RichTextBoxScrollBars.Vertical; _log.WordWrap = false;
            pnlLog.Controls.Add(_log); pnlLog.Controls.Add(logHdr);
            _splitH.Panel2.Controls.Add(pnlLog);

            // ── Status bar ────────────────────────────────────────────
            _status = new StatusStrip(); _status.Dock = DockStyle.Bottom;
            _statusFile = new ToolStripStatusLabel("Belum ada file dibuka"); _statusFile.Spring = true; _statusFile.TextAlign = ContentAlignment.MiddleLeft;
            var sEnc = new ToolStripStatusLabel("Shift-JIS"); sEnc.ForeColor = Clr.TxMuted;
            var sEng = new ToolStripStatusLabel("BGI / Ethornell  "); sEng.ForeColor = Clr.Green;
            _status.Items.AddRange(new ToolStripItem[] { _statusFile, sEnc, sEng });

            // ── Drag & drop ───────────────────────────────────────────
            AllowDrop = true;
            DragEnter += delegate(object s, DragEventArgs e) { if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; };
            DragDrop  += delegate(object s, DragEventArgs e) { string[] fs = (string[])e.Data.GetData(DataFormats.FileDrop); if (fs != null && fs.Length > 0) OpenFile(fs[0]); };

            // ── Form ──────────────────────────────────────────────────
            Text = "BGI Translator"; Size = new Size(1200, 760); MinimumSize = new Size(800, 520);
            StartPosition = FormStartPosition.CenterScreen; KeyPreview = true;
            Controls.Add(_splitH); Controls.Add(_tools); Controls.Add(_menu); Controls.Add(_status);
            MainMenuStrip = _menu;
            ResumeLayout(false); PerformLayout();
            RebuildRecentMenu();
        }

        // ── UI helpers ────────────────────────────────────────────────
        private static void Add(ToolStripMenuItem parent, string text, EventHandler handler)
        { var it = new ToolStripMenuItem(text); it.Click += handler; parent.DropDownItems.Add(it); }
        private static ToolStripButton TsBtn(string text, EventHandler handler, string tip)
        { var b = new ToolStripButton(text); b.DisplayStyle = ToolStripItemDisplayStyle.Text; b.Click += handler; b.ToolTipText = tip; return b; }
        private static Label MkLbl(string text, Font font, Color fc)
        { Label l = new Label(); l.Text = text; l.Font = font; l.ForeColor = fc; l.AutoSize = false; l.BackColor = Color.Transparent; return l; }
        private static Label ILbl(string text, int x, int y)
        { Label l = new Label(); l.Text = text; l.Font = new Font("Segoe UI", 8f); l.ForeColor = Clr.TxMuted; l.BackColor = Color.Transparent; l.AutoSize = false; l.SetBounds(x, y, 100, 18); l.TextAlign = ContentAlignment.MiddleLeft; return l; }
        private static Label IVal(string text, Color fc, int x, int y)
        { Label l = new Label(); l.Text = text; l.Font = new Font("Segoe UI", 8f, FontStyle.Bold); l.ForeColor = fc; l.BackColor = Color.Transparent; l.AutoSize = false; l.SetBounds(x, y, 130, 18); l.TextAlign = ContentAlignment.MiddleRight; return l; }

        // ── Misc actions ──────────────────────────────────────────────
        private void CopyAllJP()
        {
            StringBuilder sb = new StringBuilder();
            foreach (string s in _originals) sb.AppendLine(s);
            if (sb.Length > 0) Clipboard.SetText(sb.ToString());
            AddLog("Teks JP disalin (" + _originals.Length + " baris).", "OK");
        }

        private void ShowStats()
        {
            int total = _originals.Length, done = 0;
            double avg = 0; int dc = 0;
            for (int i = 0; i < _translations.Length; i++)
                if (_translations[i] != null && _translations[i] != "") { done++; avg += _translations[i].Length; dc++; }
            if (dc > 0) avg /= dc;
            int pct = total == 0 ? 0 : (int)((double)done / total * 100);
            MessageBox.Show(
                "File          : " + (_currentFile != null ? Path.GetFileName(_currentFile) : "(belum dibuka)") + "\n\n" +
                "Total string  : " + total + "\n" +
                "Sudah selesai : " + done + " (" + pct + "%)\n" +
                "Belum selesai : " + (total - done) + "\n" +
                "Rata-rata     : " + avg.ToString("F1") + " karakter",
                "Statistik", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowShortcuts()
        {
            MessageBox.Show(
                "Ctrl+O        Buka file\n" +
                "Ctrl+S        Simpan\n" +
                "Ctrl+F        Fokus ke pencarian\n" +
                "Ctrl+H        Buka Cari & Ganti\n" +
                "Ctrl+N        Kamus Nama Karakter\n" +
                "F3            Hasil pencarian berikutnya\n" +
                "Shift+F3      Hasil pencarian sebelumnya\n" +
                "Ctrl+G        Pergi ke nomor baris\n" +
                "Enter         Edit sel terjemahan\n" +
                "Tab           Pindah ke baris berikutnya\n" +
                "Esc           Bersihkan pencarian / tutup dialog",
                "Pintasan Keyboard", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowAbout()
        {
            MessageBox.Show(
                "BGI Translator — Pengganti EEGUI.exe\n\n" +
                "Engine  : Ethornell / BGI (Buriko General Interpreter)\n" +
                "Library : EthornellEditor.dll\n\n" +
                "API yang digunakan:\n" +
                "  string[] Import(byte[] rawFileData)\n" +
                "  byte[]   Export(string[] translations)\n\n" +
                "Fitur: editor dua kolom, pencarian, filter, tag klik,\n" +
                "       glosarium batch, progress bar, export/import TSV,\n" +
                "       drag & drop, recent files.",
                "Tentang", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    // ── Helper classes ────────────────────────────────────────────────
    public class FilterOpt
    {
        public string Disp { get; set; }
        public string Val  { get; set; }
        public FilterOpt(string d, string v) { Disp = d; Val = v; }
        public override string ToString() { return Disp; }
    }
}
