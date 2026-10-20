# Aplikasi-Jasa-Titip-ITS

Aplikasi desktop berbasis Windows Forms untuk membantu pengelolaan pesanan jasa titip makanan dan minuman bagi mahasiswa ITS. Aplikasi ini dirancang untuk mempermudah pencatatan pesanan, perhitungan biaya, pemantauan status pesanan, serta rekapitulasi pendapatan.

## Tujuan

Jasa Titip ITS dikembangkan untuk menggantikan pencatatan pesanan secara manual melalui chat dengan sistem pengelolaan yang lebih terstruktur. Dengan aplikasi ini, pengelola jasa titip dapat mencatat pesanan pelanggan, mengatur katalog menu, menghitung total pembayaran secara otomatis, dan melihat laporan berdasarkan periode tertentu.

## Fitur Utama

- **Manajemen Pesanan** Menambahkan, melihat, mengubah, dan menghapus data pesanan.
- **Perhitungan Otomatis** Menghitung subtotal, ongkos titip, dan total pembayaran berdasarkan jumlah pesanan dan harga menu.
- **Pengelolaan Ongkos Titip** Menentukan ongkos titip berdasarkan lokasi pengantaran di sekitar kampus ITS.
- **Pelacakan Status Pesanan** Memantau proses pesanan melalui status Menunggu, Dibeli, Diantar, Selesai, dan Dibatalkan.
- **Katalog Menu dan Harga** Mengelola daftar makanan dan minuman berdasarkan kantin atau toko.
- **Pencarian dan Filter** Mencari pesanan berdasarkan nama pemesan, menu, atau lokasi, serta memfilter berdasarkan status.
- **Salin Struk ke WhatsApp** Menyalin rincian pesanan ke clipboard agar mudah dikirim kepada pelanggan.
- **Ekspor Data CSV** Mengekspor data pesanan untuk kebutuhan dokumentasi dan pengolahan lebih lanjut.
- **Laporan Pendapatan** Menampilkan ringkasan pesanan, omzet, pendapatan jasa, modal belanja, serta menu dan pelanggan teratas dalam rentang tanggal tertentu.
- **Penyimpanan Lokal** Menyimpan data menggunakan file JSON sehingga data dapat dimuat kembali saat aplikasi dibuka.

## Dokumentasi Aplikasi

### Halaman Pesanan

<img width="1180" height="706" alt="Screenshot 2026-10-20 220657" src="https://github.com/user-attachments/assets/8f4abeb7-daaa-4632-bf19-ed6cbb4062ef" />

### Tambah Pesanan

<img width="1177" height="708" alt="Screenshot 2026-10-20 215833" src="https://github.com/user-attachments/assets/6fa27b1b-08ef-41e0-a634-d20963512504" />

### Ubah Pesanan

<img width="1181" height="707" alt="Screenshot 2026-10-20 215925" src="https://github.com/user-attachments/assets/3368b9ce-4646-48bc-bc65-c288023f4cfb" />

### Hapus Pesanan

<img width="1181" height="707" alt="Screenshot 2026-10-20 220635" src="https://github.com/user-attachments/assets/d310c41d-6bdd-4810-bcda-ad6ae20dd64f" />

### Halaman Menu dan Harga

<img width="1177" height="706" alt="Screenshot 2026-10-20 220026" src="https://github.com/user-attachments/assets/c3856de6-0a16-40f4-b8d5-47f5aa44897d" />

### Tambah Daftar Menu

<img width="1180" height="707" alt="Screenshot 2026-10-20 220106" src="https://github.com/user-attachments/assets/c904b585-872b-4cb0-9df3-8dbad4b79615" />

### Ubah Daftar Menu

<img width="1178" height="707" alt="Screenshot 2026-10-20 220148" src="https://github.com/user-attachments/assets/c95de6b5-88f0-498a-a74d-b79406990644" />

### Hapus Menu dalam Daftar

<img width="1177" height="706" alt="Screenshot 2026-10-20 220219" src="https://github.com/user-attachments/assets/32f8a78e-1003-468d-98dc-73720268d407" />

### Halaman Laporan

<img width="1177" height="707" alt="Screenshot 2026-10-20 220323" src="https://github.com/user-attachments/assets/5510e345-47cd-4dac-afea-2260226bddcd" />

## Batasan Aplikasi

- Berjalan sebagai aplikasi desktop Windows.
- Ditujukan untuk satu pengguna pada satu komputer.
- Belum mendukung login dan manajemen hak akses.
- Belum mendukung sinkronisasi data antarperangkat.
- Pengiriman struk dilakukan dengan menyalin teks secara manual ke WhatsApp.
- Setiap pesanan hanya mencakup satu jenis menu.

## Pengembangan Selanjutnya

Beberapa pengembangan yang dapat dipertimbangkan meliputi integrasi WhatsApp API, dukungan banyak item dalam satu pesanan, autentikasi pengguna, pencadangan otomatis, penggunaan database, serta ekspor laporan ke PDF atau Excel.

## Link Video Demo:
`https://youtu.be/FAM2YQOpj30`
