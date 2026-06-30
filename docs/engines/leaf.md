# Leaf

Engine Leaf ini terkenal gara-gara game White Album 2. Gamenya pakai archive `.pak` (berbasis KCAP).

Ada satu peringatan krusial banget buat engine ini: pas kamu mau ngekstrak file, pastikan kamu bikin satu folder kosong melompong khusus buat workspace. Folder ini nantinya cuma boleh diisi sama file-file dari gamenya aja. Jangan pernah iseng naruh file executables atau tool lain di folder ini. Soalnya pas repack nanti, semua file di folder itu bakal ikut dimasukin, dan kalau ada file asing, gamenya 100% bakal crash.

Keunggulan tool ini adalah dia bisa otomatis ngejaga nama file aslinya yang pakai encoding Shift-JIS. Jadi, kamu tinggal unpack ke folder kosong tadi, kerjain terjemahannya di dalem situ, lalu repack ulang kalau udah beres.
