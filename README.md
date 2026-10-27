# ⚡ Flash Sale & Concurrency Control API (.NET 8 & T-SQL)

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet)
![C#](https://img.shields.io/badge/C%23-12.0-239120?style=flat&logo=csharp)
![SQL Server](https://img.shields.io/badge/SQL_Server-2022-CC292B?style=flat&logo=microsoftsqlserver)
![Dapper](https://img.shields.io/badge/OR/M-Dapper-000000?style=flat)

Yüksek trafikte anlık indirim (Flash Sale) ve sınırlı stok durumlarında **stok çakışmalarını (Race Condition)** önlemek, **veri tutarlılığını (Data Consistency)** garanti altına almak için tasarlanmış yüksek performanslı backend REST API altyapısı.

---
Bu sorgu sayesinde SQL Server satır kilitleme (Row Locking) mekanizmasını çalıştırır ve milisaniyeler içinde gelen isteklerden sadece stoğu karşılayanları işleme alır.
---

## 🏗️ Kullanılan Teknolojiler

* **Framework:** .NET 8.0 ASP.NET Core Web API
* **Veritabanı & ORM:** SQL Server 2022, Dapper (Yüksek hızlı mikro-ORM)
* **Mimari Yapı:** Service-Repository Pattern, DTO, Dependency Injection
* **Dökümantasyon:** Swagger UI (Türkçe & XML Yorum Destekli)
---
## 🚀 Proje Yapısı
---
FlashSale.Api/
├── Controllers/
│   └── FlashSaleController.cs    --> API Uç Noktaları
├── DTOs/
│   └── CreateOrderDto.cs          --> İstek Veri Modelleri
├── Models/
│   ├── Product.cs                 --> Ürün Varlığı
│   ├── FlashCampaign.cs           --> Kampanya Varlığı
│   └── Order.cs                   --> Sipariş Varlığı
├── Services/
│   └── OrderService.cs            --> Atomic Update & İş Mantığı Katmanı
└── Program.cs                     --> DI & Swagger Konfigürasyonu

---
## 🎯 Çözülen Teknik Problem

Klasik e-ticaret uygulamalarında aynı anda yüzlerce kullanıcının son kalan stoğu almaya çalışması durumunda (Eşzamanlılık / Concurrency) şu sorunlar yaşanır:
1. **Overselling (Aşırı Satış):** Stok sayısının eksiye (`-1`, `-5`) düşmesi.
2. **Race Condition:** İki farklı isteğin aynı anda stok durumunu `1` görüp ikisinin de sipariş oluşturması.

### 💡 Mimari Çözüm: Atomic Update Stratejisi
Bu projede C# tarafındaki pasif kontroller yerine, veritabanı seviyesinde **T-SQL Atomic Update** ve **Transaction** yapısı kurgulanmıştır:

```sql
UPDATE FlashCampaigns 
SET RemainingStock = RemainingStock - @Quantity 
WHERE Id = @CampaignId 
  AND RemainingStock >= @Quantity 
  AND IsActive = 1
```
## 🔄 Güncellemeler

### 27 Ekim 2026
- **Güvenlik:** Sipariş adedi doğrulaması eklendi. Sıfır veya negatif adet artık reddediliyor (önceden negatif adet stoğu artırabiliyordu). Adet 1 ile 5 arasında sınırlandı.
- **İş kuralı:** Siparişler artık yalnızca kampanya süresi içinde (StartTime ile EndTime arasında) oluşturulabiliyor.
- **Güvenlik:** Beklenmeyen hatalarda veritabanı hata ayrıntısı kullanıcıya gösterilmiyor, genel bir mesaj dönülüyor.
