# Buriko (BGI)

Buriko atau biasa disebut BGI ini engine yang sering banget dipakai buat VN, contohnya kayak Higurashi, Umineko, sampe Sakura no Uta.

Biasanya file-file gamenya dikompres di dalem archive `.arc`. Kamu bisa dengan gampang ekstrak semua isinya ke sebuah folder buat diedit, dan nanti bisa di-repack lagi kalau udah selesai.

Buat file scriptnya sendiri, BGI pake format `.sc`. Di tool ini, kamu bisa langsung parse file script ini jadi JSON biar gampang ditranslate. Setelah JSON-nya kamu translate, tinggal masukin lagi ke tool ini buat di-inject balik jadi file `.sc` yang baru. Tool ini ngebantu banget biar kamu nggak perlu pusing ngurusin offset byte secara manual.
