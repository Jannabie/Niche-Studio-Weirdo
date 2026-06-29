"""
Melty Blood Translation Editor
================================
GUI tool for translating Melty Blood script files (.TXT) extracted from .p archives.

Features:
  - Open an extracted directory or a .p archive directly
  - File tree on the left showing all 189 script files
  - Split view: Original (top) | Translation (bottom) with line sync
  - Translatable lines are highlighted and editable in-place
  - Auto-save translations as JSON sidecar files
  - One-click Repack → writes new .p archive
  - Find & Replace across all files
  - Progress tracking (translated / total lines)
"""

import tkinter as tk
from tkinter import ttk, filedialog, messagebox, scrolledtext
import json
import os
import re
import threading
import tempfile
import shutil
from pathlib import Path

# Try importing core library from same directory
import sys
sys.path.insert(0, str(Path(__file__).parent))
import mb_core

# ─── Colour scheme ───────────────────────────────────────────────────────────
DARK_BG     = '#1e1e2e'
PANEL_BG    = '#181825'
ACCENT      = '#cba6f7'   # purple
ACCENT2     = '#89b4fa'   # blue
GREEN       = '#a6e3a1'
YELLOW      = '#f9e2af'
RED         = '#f38ba8'
TEXT_FG     = '#cdd6f4'
SUBTEXT     = '#6c7086'
ORIG_BG     = '#1e1e2e'
TRANS_BG    = '#11111b'
CMD_FG      = '#6c7086'
DIALOG_FG   = '#cdd6f4'
HIGHLIGHT   = '#313244'

FONT_MONO   = ('Consolas', 11)
FONT_MONO_S = ('Consolas', 10)
FONT_UI     = ('Segoe UI', 10)
FONT_TITLE  = ('Segoe UI', 12, 'bold')


# ─── Translation storage ─────────────────────────────────────────────────────

class TranslationStore:
    """
    Manages translation data for a set of script files.
    Storage format (per-file JSON):
      { "line_index": "translated text", ... }
    """
    def __init__(self, work_dir: Path):
        self.work_dir = work_dir
        self.trans_dir = work_dir / '.translations'
        self.trans_dir.mkdir(exist_ok=True)
        self._cache: dict[str, dict[int, str]] = {}

    def _path(self, filename: str) -> Path:
        return self.trans_dir / (filename + '.json')

    def load(self, filename: str) -> dict[int, str]:
        if filename in self._cache:
            return self._cache[filename]
        p = self._path(filename)
        if p.exists():
            raw = json.loads(p.read_text(encoding='utf-8'))
            data = {int(k): v for k, v in raw.items()}
        else:
            data = {}
        self._cache[filename] = data
        return data

    def save(self, filename: str, translations: dict[int, str]) -> None:
        self._cache[filename] = translations
        p = self._path(filename)
        p.write_text(
            json.dumps({str(k): v for k, v in translations.items()},
                       indent=2, ensure_ascii=False),
            encoding='utf-8'
        )

    def set_line(self, filename: str, line_idx: int, text: str) -> None:
        data = self.load(filename)
        if text.strip():
            data[line_idx] = text
        elif line_idx in data:
            del data[line_idx]
        self.save(filename, data)

    def get_line(self, filename: str, line_idx: int) -> str:
        return self.load(filename).get(line_idx, '')

    def stats(self, filename: str, total_translatable: int) -> tuple[int, int]:
        """Return (translated_count, total_translatable)."""
        data = self.load(filename)
        done = sum(1 for v in data.values() if v.strip())
        return done, total_translatable

    def apply_to_script(self, filename: str, original_text: str) -> str:
        """
        Return script text with translated lines substituted.
        Non-translated lines keep their original text.
        """
        translations = self.load(filename)
        lines = original_text.splitlines(keepends=True)
        result = []
        for i, line in enumerate(lines):
            if i in translations and translations[i].strip():
                # Preserve original line ending
                ending = '\r\n' if line.endswith('\r\n') else \
                         '\n'   if line.endswith('\n')   else ''
                result.append(translations[i].rstrip('\r\n') + ending)
            else:
                result.append(line)
        return ''.join(result)


# ─── Script viewer widget ─────────────────────────────────────────────────────

class ScriptView(tk.Frame):
    """
    Displays a script file with syntax-highlighted original text
    and inline translation text boxes for each translatable line.
    """
    ROW_H = 24   # approx px per line row

    def __init__(self, master, store: TranslationStore, **kw):
        super().__init__(master, bg=DARK_BG, **kw)
        self.store    = store
        self.filename = None
        self.lines    = []          # all lines of original text
        self.trans_vars: dict[int, tk.StringVar] = {}
        self.on_change_cb = None    # callback(filename, line_idx, text)

        self._build_ui()

    def _build_ui(self):
        # Toolbar
        tb = tk.Frame(self, bg=PANEL_BG, pady=4)
        tb.pack(fill='x')
        self.lbl_file = tk.Label(tb, text='No file open', bg=PANEL_BG,
                                 fg=ACCENT, font=FONT_TITLE, anchor='w', padx=8)
        self.lbl_file.pack(side='left')
        self.lbl_stats = tk.Label(tb, text='', bg=PANEL_BG,
                                  fg=YELLOW, font=FONT_UI, anchor='e', padx=8)
        self.lbl_stats.pack(side='right')

        # Canvas + scrollbar for the line-by-line view
        frame = tk.Frame(self, bg=DARK_BG)
        frame.pack(fill='both', expand=True)

        self.canvas = tk.Canvas(frame, bg=DARK_BG, highlightthickness=0)
        vsb = ttk.Scrollbar(frame, orient='vertical', command=self.canvas.yview)
        self.canvas.configure(yscrollcommand=vsb.set)
        vsb.pack(side='right', fill='y')
        self.canvas.pack(side='left', fill='both', expand=True)

        self.inner = tk.Frame(self.canvas, bg=DARK_BG)
        self.canvas_win = self.canvas.create_window(
            (0, 0), window=self.inner, anchor='nw'
        )

        self.inner.bind('<Configure>', self._on_inner_configure)
        self.canvas.bind('<Configure>', self._on_canvas_configure)
        self.canvas.bind_all('<MouseWheel>', self._on_mousewheel)

    def _on_inner_configure(self, e):
        self.canvas.configure(scrollregion=self.canvas.bbox('all'))

    def _on_canvas_configure(self, e):
        self.canvas.itemconfig(self.canvas_win, width=e.width)

    def _on_mousewheel(self, e):
        self.canvas.yview_scroll(-1 * (e.delta // 120), 'units')

    def load_file(self, filename: str, text: str) -> None:
        """Load and display a script file."""
        self.filename = filename
        self.lines    = text.splitlines()
        self.trans_vars.clear()

        # Clear previous content
        for w in self.inner.winfo_children():
            w.destroy()

        translations = self.store.load(filename)
        translatable_count = 0

        for i, line in enumerate(self.lines):
            is_cmd  = mb_core.is_command_line(line)
            is_text = not is_cmd and line.strip() != ''

            row = tk.Frame(self.inner, bg=DARK_BG)
            row.pack(fill='x', padx=0, pady=0)

            # Line number
            lnum = tk.Label(row, text=f'{i+1:4d}', bg=PANEL_BG,
                            fg=SUBTEXT, font=FONT_MONO_S, width=5, anchor='e',
                            padx=2)
            lnum.pack(side='left', fill='y')

            content = tk.Frame(row, bg=DARK_BG)
            content.pack(side='left', fill='both', expand=True)

            # Original line
            fg = CMD_FG if is_cmd else (DIALOG_FG if is_text else SUBTEXT)
            bg = PANEL_BG if is_cmd else DARK_BG

            orig_lbl = tk.Label(
                content, text=line or ' ', bg=bg, fg=fg,
                font=FONT_MONO_S, anchor='w', padx=6,
                justify='left', wraplength=0
            )
            orig_lbl.pack(fill='x')

            if is_text:
                translatable_count += 1
                # Translation entry
                var = tk.StringVar(value=translations.get(i, ''))
                self.trans_vars[i] = var

                entry_frame = tk.Frame(content, bg=TRANS_BG, pady=1)
                entry_frame.pack(fill='x', padx=20)

                tk.Label(entry_frame, text='↳', bg=TRANS_BG, fg=ACCENT2,
                         font=FONT_MONO_S).pack(side='left')

                entry = tk.Entry(
                    entry_frame, textvariable=var, bg=TRANS_BG,
                    fg=GREEN, font=FONT_MONO_S, relief='flat',
                    insertbackground=GREEN, bd=0
                )
                entry.pack(side='left', fill='x', expand=True, pady=2)

                # Auto-save on change
                def make_save_cb(line_idx, svar):
                    def _cb(*_):
                        self.store.set_line(filename, line_idx, svar.get())
                        self._update_stats()
                        if self.on_change_cb:
                            self.on_change_cb(filename, line_idx, svar.get())
                    return _cb
                var.trace_add('write', make_save_cb(i, var))

        self.lbl_file.config(text=filename)
        self._translatable_count = translatable_count
        self._update_stats()

    def _update_stats(self):
        if not self.filename:
            return
        done = sum(
            1 for idx, var in self.trans_vars.items()
            if var.get().strip()
        )
        total = self._translatable_count
        pct = int(100 * done / total) if total else 0
        fg  = GREEN if pct == 100 else (YELLOW if pct > 0 else SUBTEXT)
        self.lbl_stats.config(
            text=f'{done}/{total} lines ({pct}%)', fg=fg
        )

    def scroll_to_line(self, line_idx: int) -> None:
        if not self.lines:
            return
        frac = line_idx / max(len(self.lines), 1)
        self.canvas.yview_moveto(frac)


# ─── Find & Replace ──────────────────────────────────────────────────────────

class FindReplaceDialog(tk.Toplevel):
    def __init__(self, master, store: TranslationStore, file_list: list[str], on_done):
        super().__init__(master)
        self.store     = store
        self.file_list = file_list
        self.on_done   = on_done
        self.title('Find & Replace in Translations')
        self.configure(bg=DARK_BG)
        self.resizable(False, False)
        self._build()

    def _build(self):
        pad = dict(padx=10, pady=5)
        tk.Label(self, text='Find:', bg=DARK_BG, fg=TEXT_FG,
                 font=FONT_UI).grid(row=0, column=0, sticky='e', **pad)
        self.find_var = tk.StringVar()
        tk.Entry(self, textvariable=self.find_var, width=40,
                 bg=PANEL_BG, fg=TEXT_FG, insertbackground=TEXT_FG,
                 font=FONT_MONO_S).grid(row=0, column=1, **pad)

        tk.Label(self, text='Replace:', bg=DARK_BG, fg=TEXT_FG,
                 font=FONT_UI).grid(row=1, column=0, sticky='e', **pad)
        self.repl_var = tk.StringVar()
        tk.Entry(self, textvariable=self.repl_var, width=40,
                 bg=PANEL_BG, fg=TEXT_FG, insertbackground=TEXT_FG,
                 font=FONT_MONO_S).grid(row=1, column=1, **pad)

        self.scope_var = tk.StringVar(value='all')
        tk.Label(self, text='Scope:', bg=DARK_BG, fg=TEXT_FG,
                 font=FONT_UI).grid(row=2, column=0, sticky='e', **pad)
        sf = tk.Frame(self, bg=DARK_BG)
        sf.grid(row=2, column=1, sticky='w', **pad)
        for val, lbl in [('all','All files'), ('translations','Translations only')]:
            tk.Radiobutton(sf, text=lbl, variable=self.scope_var, value=val,
                           bg=DARK_BG, fg=TEXT_FG, selectcolor=PANEL_BG,
                           activebackground=DARK_BG, font=FONT_UI).pack(side='left')

        self.status_var = tk.StringVar()
        tk.Label(self, textvariable=self.status_var, bg=DARK_BG, fg=YELLOW,
                 font=FONT_UI).grid(row=3, column=0, columnspan=2, **pad)

        bf = tk.Frame(self, bg=DARK_BG)
        bf.grid(row=4, column=0, columnspan=2, pady=10)
        for txt, cmd in [('Replace All', self._do_replace), ('Close', self.destroy)]:
            tk.Button(bf, text=txt, command=cmd, bg=ACCENT, fg=DARK_BG,
                      font=FONT_UI, relief='flat', padx=12, pady=4).pack(
                side='left', padx=5)

    def _do_replace(self):
        find = self.find_var.get()
        repl = self.repl_var.get()
        if not find:
            return
        count = 0
        for fname in self.file_list:
            trans = self.store.load(fname)
            changed = False
            for k, v in trans.items():
                nv = v.replace(find, repl)
                if nv != v:
                    trans[k] = nv
                    count += 1
                    changed = True
            if changed:
                self.store.save(fname, trans)
        self.status_var.set(f'Replaced {count} occurrence(s) in translations.')
        if self.on_done:
            self.on_done()


# ─── Main Application ─────────────────────────────────────────────────────────

class MeltyTranslatorApp(tk.Tk):
    def __init__(self):
        super().__init__()
        self.title('Melty Blood Translation Editor')
        self.configure(bg=DARK_BG)
        self.geometry('1300x820')
        self.minsize(900, 600)

        self.work_dir: Path | None = None
        self.store:    TranslationStore | None = None
        self.file_list: list[str] = []
        self.orig_texts: dict[str, str] = {}   # cache of decoded scripts
        self.current_file: str | None = None
        self.archive_path: Path | None = None  # original archive (for repack)

        self._build_ui()
        self._apply_style()

    # ── UI construction ───────────────────────────────────────────────────────

    def _build_ui(self):
        self._build_menu()

        # Top toolbar
        tb = tk.Frame(self, bg=PANEL_BG, pady=6)
        tb.pack(fill='x')

        for txt, cmd in [
            ('📂 Open Archive (.p)', self.open_archive),
            ('📁 Open Extracted Dir', self.open_extracted_dir),
            ('💾 Save Current', self.save_current),
            ('📦 Repack Archive', self.repack_archive),
            ('🔍 Find & Replace', self.find_replace),
        ]:
            btn = tk.Button(tb, text=txt, command=cmd, bg=ACCENT,
                            fg=DARK_BG, font=FONT_UI, relief='flat',
                            padx=10, pady=3, cursor='hand2')
            btn.pack(side='left', padx=4)

        self.status_var = tk.StringVar(value='Open an archive or extracted directory to begin.')
        tk.Label(tb, textvariable=self.status_var, bg=PANEL_BG, fg=SUBTEXT,
                 font=FONT_UI).pack(side='right', padx=10)

        # Main pane: file list | script view
        pane = tk.PanedWindow(self, orient='horizontal', bg=DARK_BG,
                              sashwidth=4, sashrelief='flat',
                              sashpad=2)
        pane.pack(fill='both', expand=True, padx=4, pady=4)

        # LEFT: file list
        left = tk.Frame(pane, bg=PANEL_BG, width=220)
        pane.add(left, minsize=160)

        tk.Label(left, text='Script Files', bg=PANEL_BG, fg=ACCENT,
                 font=FONT_TITLE, anchor='w', padx=6, pady=6).pack(fill='x')

        search_frame = tk.Frame(left, bg=PANEL_BG)
        search_frame.pack(fill='x', padx=4, pady=2)
        self.search_var = tk.StringVar()
        self.search_var.trace_add('write', self._filter_files)
        tk.Entry(search_frame, textvariable=self.search_var, bg=DARK_BG,
                 fg=TEXT_FG, insertbackground=TEXT_FG, font=FONT_MONO_S,
                 relief='flat').pack(fill='x', padx=2, pady=2)

        # Listbox with scrollbar
        lb_frame = tk.Frame(left, bg=PANEL_BG)
        lb_frame.pack(fill='both', expand=True)
        lb_vsb = ttk.Scrollbar(lb_frame, orient='vertical')
        lb_vsb.pack(side='right', fill='y')
        self.listbox = tk.Listbox(
            lb_frame, bg=PANEL_BG, fg=TEXT_FG, selectbackground=HIGHLIGHT,
            selectforeground=ACCENT, font=FONT_MONO_S, relief='flat', bd=0,
            yscrollcommand=lb_vsb.set, activestyle='none'
        )
        self.listbox.pack(fill='both', expand=True)
        lb_vsb.config(command=self.listbox.yview)
        self.listbox.bind('<<ListboxSelect>>', self._on_file_select)

        # Progress bar
        self.progress_frame = tk.Frame(left, bg=PANEL_BG)
        self.progress_frame.pack(fill='x', padx=4, pady=4)
        tk.Label(self.progress_frame, text='Overall Progress',
                 bg=PANEL_BG, fg=SUBTEXT, font=FONT_UI).pack(anchor='w')
        self.progress_var = tk.DoubleVar()
        self.progress_bar = ttk.Progressbar(
            self.progress_frame, variable=self.progress_var,
            maximum=100, length=200
        )
        self.progress_bar.pack(fill='x', pady=2)
        self.progress_lbl = tk.Label(self.progress_frame, text='0%',
                                     bg=PANEL_BG, fg=YELLOW, font=FONT_UI)
        self.progress_lbl.pack(anchor='e')

        # RIGHT: script view
        self.script_view = ScriptView(pane, store=None)
        pane.add(self.script_view, minsize=600)

    def _build_menu(self):
        menu = tk.Menu(self, bg=PANEL_BG, fg=TEXT_FG, relief='flat',
                       activebackground=HIGHLIGHT, activeforeground=ACCENT)
        self.config(menu=menu)

        file_menu = tk.Menu(menu, tearoff=False, bg=PANEL_BG, fg=TEXT_FG)
        menu.add_cascade(label='File', menu=file_menu)
        file_menu.add_command(label='Open Archive (.p)', command=self.open_archive)
        file_menu.add_command(label='Open Extracted Directory', command=self.open_extracted_dir)
        file_menu.add_separator()
        file_menu.add_command(label='Save Current File', command=self.save_current)
        file_menu.add_command(label='Repack Archive', command=self.repack_archive)
        file_menu.add_separator()
        file_menu.add_command(label='Export All Translations (JSON)', command=self.export_translations)
        file_menu.add_command(label='Import Translations (JSON)', command=self.import_translations)
        file_menu.add_separator()
        file_menu.add_command(label='Quit', command=self.quit)

        tools_menu = tk.Menu(menu, tearoff=False, bg=PANEL_BG, fg=TEXT_FG)
        menu.add_cascade(label='Tools', menu=tools_menu)
        tools_menu.add_command(label='Find & Replace', command=self.find_replace)
        tools_menu.add_command(label='Show Stats', command=self.show_stats)

        help_menu = tk.Menu(menu, tearoff=False, bg=PANEL_BG, fg=TEXT_FG)
        menu.add_cascade(label='Help', menu=help_menu)
        help_menu.add_command(label='About', command=self.show_about)

    def _apply_style(self):
        style = ttk.Style()
        style.theme_use('clam')
        style.configure('Vertical.TScrollbar', background=PANEL_BG,
                        troughcolor=DARK_BG, arrowcolor=ACCENT)
        style.configure('TProgressbar', troughcolor=PANEL_BG,
                        background=ACCENT, thickness=8)

    # ── File operations ───────────────────────────────────────────────────────

    def open_archive(self):
        path = filedialog.askopenfilename(
            title='Open Melty Blood Archive',
            filetypes=[('Melty Blood Archive', '*.p'), ('All files', '*.*')]
        )
        if not path:
            return
        self.archive_path = Path(path)
        # Extract to temp directory
        tmp = Path(tempfile.mkdtemp(prefix='mb_extract_'))
        self._set_status(f'Extracting {path}…')
        try:
            mb_core.unpack(path, tmp)
        except Exception as e:
            messagebox.showerror('Error', f'Failed to open archive:\n{e}')
            return
        self._load_work_dir(tmp)
        self._set_status(f'Opened: {Path(path).name} ({len(self.file_list)} files)')

    def open_extracted_dir(self):
        path = filedialog.askdirectory(title='Open Extracted Directory')
        if not path:
            return
        self._load_work_dir(Path(path))
        self._set_status(f'Opened: {path} ({len(self.file_list)} files)')

    def _load_work_dir(self, work_dir: Path):
        self.work_dir = work_dir
        self.store    = TranslationStore(work_dir)
        self.script_view.store = self.store

        # Load manifest if available
        manifest_path = work_dir / '_manifest.json'
        if manifest_path.exists():
            manifest = json.loads(manifest_path.read_text(encoding='utf-8'))
            self.file_list = manifest.get('files', [])
        else:
            self.file_list = sorted(
                f.name for f in work_dir.iterdir()
                if f.suffix.upper() == '.TXT'
            )

        # Cache all original texts
        self.orig_texts = {}
        for fname in self.file_list:
            fpath = work_dir / fname
            if fpath.exists():
                raw = fpath.read_bytes()
                self.orig_texts[fname] = mb_core.decode_script(raw)

        self._populate_listbox()
        self._update_progress()

    def _populate_listbox(self, filter_text: str = ''):
        self.listbox.delete(0, 'end')
        for fname in self.file_list:
            if filter_text.lower() in fname.lower():
                trans = self.store.load(fname)
                done  = sum(1 for v in trans.values() if v.strip())
                txt   = self.orig_texts.get(fname, '')
                total = len(mb_core.get_translatable_lines(txt))
                tag   = f'  [{done}/{total}]' if total else ''
                self.listbox.insert('end', fname + tag)

    def _filter_files(self, *_):
        if self.file_list:
            self._populate_listbox(self.search_var.get())

    def _on_file_select(self, e):
        sel = self.listbox.curselection()
        if not sel:
            return
        # Extract filename (remove the [x/y] suffix)
        raw = self.listbox.get(sel[0])
        fname = raw.split('  [')[0]
        if fname not in self.orig_texts:
            return
        self.current_file = fname
        text = self.orig_texts[fname]
        self.script_view.load_file(fname, text)
        self._set_status(f'Editing: {fname}')

    # ── Saving ────────────────────────────────────────────────────────────────

    def save_current(self):
        """Translations are auto-saved; this applies them back to the file."""
        if not self.current_file or not self.work_dir:
            return
        fname = self.current_file
        orig  = self.orig_texts.get(fname, '')
        translated = self.store.apply_to_script(fname, orig)
        out_path   = self.work_dir / fname
        out_path.write_bytes(mb_core.encode_script(translated))
        self._set_status(f'Saved: {fname}')
        self._update_progress()

    def repack_archive(self):
        if not self.work_dir:
            messagebox.showwarning('Nothing to repack', 'Open a directory first.')
            return

        # Apply all translations to files first
        self._set_status('Applying translations…')
        for fname, orig in self.orig_texts.items():
            translated = self.store.apply_to_script(fname, orig)
            (self.work_dir / fname).write_bytes(mb_core.encode_script(translated))

        # Determine output path
        if self.archive_path:
            default = str(self.archive_path.parent / (self.archive_path.stem + '_translated.p'))
        else:
            default = str(self.work_dir / 'output.p')

        out = filedialog.asksaveasfilename(
            title='Save Repacked Archive',
            initialfile=Path(default).name,
            initialdir=Path(default).parent,
            defaultextension='.p',
            filetypes=[('Melty Blood Archive', '*.p')]
        )
        if not out:
            return

        try:
            mb_core.repack(self.work_dir, out)
            self._set_status(f'Repacked → {Path(out).name}')
            messagebox.showinfo('Done', f'Archive saved to:\n{out}')
        except Exception as e:
            messagebox.showerror('Error', f'Repack failed:\n{e}')

    # ── Find & Replace ────────────────────────────────────────────────────────

    def find_replace(self):
        if not self.store:
            return
        FindReplaceDialog(
            self, self.store, self.file_list,
            on_done=self._refresh_current
        )

    def _refresh_current(self):
        if self.current_file:
            self.script_view.load_file(
                self.current_file, self.orig_texts[self.current_file]
            )

    # ── Export / Import ───────────────────────────────────────────────────────

    def export_translations(self):
        if not self.store or not self.file_list:
            return
        out = filedialog.asksaveasfilename(
            title='Export Translations',
            defaultextension='.json',
            filetypes=[('JSON', '*.json')]
        )
        if not out:
            return
        all_trans = {}
        for fname in self.file_list:
            t = self.store.load(fname)
            if t:
                all_trans[fname] = {str(k): v for k, v in t.items()}
        Path(out).write_text(
            json.dumps(all_trans, indent=2, ensure_ascii=False),
            encoding='utf-8'
        )
        messagebox.showinfo('Exported', f'Translations saved to:\n{out}')

    def import_translations(self):
        if not self.store:
            return
        src = filedialog.askopenfilename(
            title='Import Translations',
            filetypes=[('JSON', '*.json')]
        )
        if not src:
            return
        data = json.loads(Path(src).read_text(encoding='utf-8'))
        count = 0
        for fname, trans in data.items():
            if fname in self.file_list:
                int_trans = {int(k): v for k, v in trans.items()}
                self.store.save(fname, int_trans)
                count += 1
        messagebox.showinfo('Imported', f'Imported translations for {count} files.')
        self._refresh_current()
        self._update_progress()

    # ── Stats ─────────────────────────────────────────────────────────────────

    def show_stats(self):
        if not self.store or not self.file_list:
            return
        total_lines = total_done = 0
        rows = []
        for fname in self.file_list:
            txt   = self.orig_texts.get(fname, '')
            tl    = mb_core.get_translatable_lines(txt)
            done  = sum(1 for v in self.store.load(fname).values() if v.strip())
            total_lines += len(tl)
            total_done  += done
            pct = int(100 * done / len(tl)) if tl else 0
            rows.append((fname, done, len(tl), pct))

        w = tk.Toplevel(self)
        w.title('Translation Stats')
        w.configure(bg=DARK_BG)
        w.geometry('480x500')

        tk.Label(w, text=f'Overall: {total_done}/{total_lines} lines translated '
                         f'({int(100*total_done/total_lines) if total_lines else 0}%)',
                 bg=DARK_BG, fg=GREEN, font=FONT_TITLE, pady=8).pack()

        cols = ('File', 'Done', 'Total', '%')
        tv = ttk.Treeview(w, columns=cols, show='headings', height=20)
        for c, w_ in zip(cols, (160, 60, 60, 60)):
            tv.heading(c, text=c)
            tv.column(c, width=w_, anchor='center' if c != 'File' else 'w')
        for row in rows:
            tv.insert('', 'end', values=row)
        tv.pack(fill='both', expand=True, padx=8, pady=8)

    def show_about(self):
        messagebox.showinfo(
            'About',
            'Melty Blood Translation Editor\n\n'
            'Supports: Melty Blood / Re·ACT / Act Cadenza / Actress Again\n'
            'Archive format: data0X.p\n\n'
            'Format research: ExtractData by Yuu / lioncash\n'
            'Tool: built with Python + Tkinter'
        )

    # ── Helpers ───────────────────────────────────────────────────────────────

    def _set_status(self, msg: str):
        self.status_var.set(msg)
        self.update_idletasks()

    def _update_progress(self):
        if not self.store or not self.file_list:
            return
        total_lines = total_done = 0
        for fname in self.file_list:
            txt  = self.orig_texts.get(fname, '')
            tl   = mb_core.get_translatable_lines(txt)
            done = sum(1 for v in self.store.load(fname).values() if v.strip())
            total_lines += len(tl)
            total_done  += done
        pct = int(100 * total_done / total_lines) if total_lines else 0
        self.progress_var.set(pct)
        self.progress_lbl.config(
            text=f'{total_done}/{total_lines} ({pct}%)',
            fg=GREEN if pct == 100 else (YELLOW if pct > 0 else SUBTEXT)
        )
        self._populate_listbox(self.search_var.get())


# ─── Entry point ─────────────────────────────────────────────────────────────

if __name__ == '__main__':
    app = MeltyTranslatorApp()
    app.mainloop()
