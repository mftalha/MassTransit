import http from 'k6/http';
import { check } from 'k6';

// Testin Kuralları (Ne kadar kullanıcı, ne kadar süre?)
export const options = {
    insecureSkipTLSVerify: true, // Lokaldeki HTTPS/SSL sertifika hatalarını yok say
    stages: [
        { duration: '15s', target: 100 },  // 15 saniye içinde 100 kullanıcıya yavaşça çık (Isınma)
        { duration: '40s', target: 1000 }, // 40 saniye boyunca tam 1000 eşzamanlı kullanıcı ile saldır (Ana Yük)
        { duration: '15s', target: 0 },    // 15 saniye içinde yavaşça kullanıcı sayısını sıfırla (Soğuma)
    ],
};

// Her bir sanal kullanıcının yapacağı işlem
export default function () {
    const url = 'https://localhost:7194/api/payment'; // Postman'deki adresin
    
    // Göndereceğimiz JSON veri
    const payload = JSON.stringify({
        Amount: Math.floor(Math.random() * 1000) + 100, // 100 ile 1100 arası rastgele tutar
        CardNumber: '4321-8765-1111-2226',
        Email: `test-${__VU}-${__ITER}@sistem.com` // Her istekte benzersiz email (__VU: User ID, __ITER: Döngü ID)
    });

    const params = {
        headers: {
            'Content-Type': 'application/json',
        },
    };

    // İsteği Fırlat
    const res = http.post(url, payload, params);

    // Dönen cevabın 202 Accepted olup olmadığını kontrol et
    check(res, {
        'Durum Kodu 202 mi?': (r) => r.status === 202,
    });
}