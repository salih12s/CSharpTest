# Kod Okuma Rehberi

Bu belge TedarikCore'un ADIM 03 sonundaki halini anlatır. Kodda neredeyse hiç yorum satırı yok; açıklamalar burada.

Sözlük (sık geçen İngilizce terimler):

| Terim | Basit anlamı |
|---|---|
| Solution (`.slnx`) | Birden çok projeyi tek çatı altında toplayan dosya. Visual Studio bunu açar. |
| Project (`.csproj`) | Tek başına derlenen bir kod paketi. Derlenince bir `.dll` (ya da çalışan uygulama) çıkar. |
| Katman (layer) | Benzer işi yapan kodun durduğu proje. Her katmanın tek bir sorumluluğu vardır. |
| Dependency (bağımlılık) | "A projesi B'nin kodunu kullanıyor" ilişkisi. |
| DI (Dependency Injection, bağımlılık enjeksiyonu) | Bir sınıf ihtiyacı olan nesneyi kendisi üretmez, dışarıdan hazır alır. Hangi nesnenin verileceği tek bir yerde (kayıt listesinde) yazılır. |
| Middleware | İsteğin sunucuya girip cevaba dönene kadar sırayla geçtiği küçük adımlar. |
| Endpoint | Bir adrese (`/health` gibi) karşılık gelen, cevap üreten uç. |
| Controller | Endpoint'leri sınıf olarak gruplayan yapı. Henüz hiç yok. |
| Integration test | Parçaların birlikte çalıştığını, uygulamayı gerçekten ayağa kaldırarak denemek. |
| Mimari test | Kodun nasıl bağlandığına dair kuralı test etmek (iş mantığını değil). |

---

## 1. Solution ve dört projenin görevleri

`TedarikCore.slnx` altı projeyi tutar: dördü `src/` altında (asıl uygulama), ikisi `tests/` altında (testler).

| Proje | Görevi | Şu an içinde ne var |
|---|---|---|
| `TedarikCore.Domain` | İşin kendisi: "Ürün", "Sipariş" gibi kavramlar ve değişmez iş kuralları. Hiçbir şeye bağımlı değildir. | Boş. Henüz kavram yok. |
| `TedarikCore.Application` | Uygulamanın yaptığı işler (use case): "sipariş oluştur" gibi. Dış dünyayla konuşacağı şeylerin sözleşmelerini (interface) burada tanımlar. | Yalnızca `AddApplication()` kayıt metodu (boş). |
| `TedarikCore.Infrastructure` | Dış dünya ile gerçek konuşma: veritabanı, e-posta, dosya vb. Application'daki sözleşmeleri gerçekler. | Yalnızca `AddInfrastructure()` kayıt metodu (boş). |
| `TedarikCore.Api` | Uygulamanın HTTP kapısı: istekleri alır, Application'a iletir, cevabı döner. Tüm parçaları `Program.cs`'te birleştirir. | `Program.cs` ve ayarlar. `/health` buradan çıkar. |

Mantık: iş kuralları (Domain) web'den, veritabanından ve çerçeveden habersiz kalır. Yarın veritabanı ya da web çerçevesi değişse bile Domain değişmez.

Bağımlılık yönü (ok "şunu kullanır" demek):

```
Api ──► Application ──► Domain
 │                        ▲
 └────► Infrastructure ───┤
              │           │
              └─► Application
```

- Domain kimseyi kullanmaz.
- Application yalnızca Domain'i kullanır.
- Infrastructure, Application ve Domain'i kullanır.
- Api, Application ve Infrastructure'ı kullanır.
- Döngü yok: iki proje birbirini kullanamaz. Bu kural 7. bölümdeki testle korunuyor.

Neden Application, Infrastructure'ı bilmiyor? Application "bana ürünleri kaydeden bir şey lazım" diye bir sözleşme yazar; hangi veritabanı olduğunu bilmez. Infrastructure bu sözleşmeyi gerçekler. Api da ikisini birbirine bağlar (DI ile).

---

## 2. Klasörler neden o katmanda?

| Klasör | Katman | Neden orada |
|---|---|---|
| `src/TedarikCore.Api/Controllers` | Api | Controller HTTP'ye aittir (adres, status kodu). Boş, `.gitkeep` var; ilk endpoint'ler gelince dolacak. |
| `src/TedarikCore.Api/Properties` | Api | `launchSettings.json` içerir: yerel çalıştırmada hangi portlar (http `5229`, https `7189`) ve hangi ortam (`Development`) kullanılacak. |
| `src/TedarikCore.Application/Abstractions` | Application | Interface'lerin (sözleşmelerin) duracağı yer. Application tanımlar, Infrastructure gerçekler. Henüz boş. |
| `src/TedarikCore.Infrastructure/Persistence` | Infrastructure | "Persistence" = verinin kalıcı saklanması. Veritabanı erişim kodu buraya gelecek. Henüz boş. |
| `tests/TedarikCore.UnitTests` | Test | Hızlı testler; uygulama ayağa kalkmaz. |
| `tests/TedarikCore.IntegrationTests` | Test | Uygulamayı bellekte ayağa kaldırıp HTTP ile deneyen testler. |
| `docs/` | — | Proje belgeleri (durum, kararlar, bu rehber). |

Klasör içinde olmayan ama önemli dosyalar:

- `CLAUDE.md` (kök): projenin kalıcı kuralları.
- `.gitignore`: git'in saymaması gereken dosyalar (`bin/`, `obj/`, `.vs/`, `*.user`, gizli dosyalar, test çıktıları).
- `appsettings.json` / `appsettings.Development.json`: uygulama ayarları (şimdilik yalnızca log seviyeleri ve `AllowedHosts`). Parola ve bağlantı bilgisi buraya yazılmaz.
- `TedarikCore.Api.http`: Visual Studio'dan `/health`'e elle istek atmak için örnek dosya.
- `TedarikCore.Api.csproj.user`: Visual Studio'nun kişisel ayarı (git'e girmez).
- `bin/` ve `obj/`: derleme çıktıları; silinse yeniden üretilir. Git'e girmez.

Temizlik notu: `Domain/Common` ve `Api/Middleware` klasörleri yalnızca `.gitkeep` içerdiği için kaldırıldı. (`.gitkeep` git'in boş klasörü saymaması yüzünden konan boş bir yer tutucu dosyadır.) Gerekince yeniden açılır.

---

## 3. Program.cs satır satır

Dosya: `src/TedarikCore.Api/Program.cs`. Uygulamanın giriş noktasıdır. Üç aşaması var: **kur** (servisleri kaydet), **kapıyı hazırla** (middleware sırasını belirle), **çalıştır**.

```csharp
using TedarikCore.Application;                 // 1
using TedarikCore.Infrastructure;              // 2
```
1–2. `AddApplication()` ve `AddInfrastructure()` metodlarını bu namespace'lerden kullanabilmek için.

```csharp
var builder = WebApplication.CreateBuilder(args);   // 4
```
4. Uygulamayı kuracak "inşaatçı"yı (builder) yaratır. `args` komut satırı parametreleridir. Ayar dosyalarını (`appsettings*.json`), log'u ve web sunucusunu (Kestrel) varsayılanlarla hazırlar. `WebApplication` için ayrıca `using` yazmaya gerek yok, çünkü `.csproj`'daki `ImplicitUsings` bunu otomatik ekler.

```csharp
builder.Services.AddApplication();             // 8
builder.Services.AddInfrastructure();          // 9
```
8–9. Katmanların kendi servislerini DI listesine (`Services`) kaydettirir. Şu an ikisi de boş; kayıt gerekince `Program.cs` büyümeden ilgili katmanda eklenir (bkz. 4. bölüm).

```csharp
builder.Services.AddControllers();             // 11
```
11. Controller'ları (sınıf tabanlı endpoint'ler) destekler. Henüz controller yok, ama ilerisi için hazır.

```csharp
builder.Services.AddHealthChecks();            // 12
```
12. Sağlık kontrol sistemini ekler. Hiç kontrol kaydı olmadığı için sonuç her zaman "Healthy" (sağlıklı) çıkar.

```csharp
builder.Services.AddOpenApi();                 // 14
```
14. API'nin makine tarafından okunabilir tarifini (OpenAPI belgesi, JSON) üretecek servisi ekler. Başka araçlar bu tarifle API'yi tanır.

```csharp
var app = builder.Build();                     // 16
```
16. Kayıt aşaması biter; kurulan uygulama nesnesi (`app`) oluşur. Bundan sonra servis eklenmez, yalnızca istek hattı düzenlenir.

```csharp
if (app.Environment.IsDevelopment())           // 19
{
    app.MapOpenApi();                          // 21
}
```
19–22. Yalnızca geliştirme ortamında `/openapi/v1.json` adresini açar. Canlıya çıkınca bu tarif dışarıya açılmaz.

```csharp
app.UseHttpsRedirection();                     // 24
```
24. `http://` ile gelen isteği `https://` adresine yönlendirir (307 cevabı ile). Bu yüzden testlerde istemci zaten https'e gider (6. bölüm).

```csharp
app.UseAuthorization();                        // 26
```
26. "Bu kullanıcının buna izni var mı?" kontrolünü hatta ekler. Henüz kimlik doğrulama ve `[Authorize]` olmadığı için şu an hiçbir şey engellemez; ileride giriş (JWT) eklenince devreye girecek.

```csharp
app.MapControllers();                          // 28
app.MapHealthChecks("/health");                // 29
```
28. Controller'ların adreslerini hatta bağlar (şu an sıfır adet).
29. `/health` adresini sağlık kontrolüne bağlar. Cevap: HTTP 200 ve düz metin `Healthy`. Bir kontrol başarısız olsaydı 503 dönerdi.

```csharp
app.Run();                                     // 31
```
31. Uygulamayı başlatır ve istek beklemeye başlar. Kapatılana kadar burada bekler.

```csharp
public partial class Program;                  // 34
```
34. Üst düzey (top-level) `Program.cs` sınıfı varsayılan olarak dışarıya kapalıdır. Bu satır sınıfı `public` yapar ki test projesi `WebApplicationFactory<Program>` ile uygulamayı bulup ayağa kaldırabilsin. Uygulama davranışını değiştirmez.

---

## 4. DependencyInjection.cs dosyaları

İki dosya var, aynı kalıpta:

```csharp
// Application
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
```

Infrastructure'daki `AddInfrastructure` de aynıdır.

- `static class`: nesnesi üretilmeyen, yalnızca yardımcı metot taşıyan sınıf.
- `this IServiceCollection services`: bu bir **extension method** (genişletme metodu). Sayesinde `Program.cs`'te `builder.Services.AddApplication()` diye doğal biçimde çağrılır.
- `IServiceCollection`: DI kayıt listesi. "Birisi `X` isterse `Y` ver" kayıtları buraya eklenir.
- `return services;`: zincirleme çağrı yapılabilsin diye listeyi geri verir.
- Şu an gövde boş çünkü kaydedilecek servis yok. Yine de yazıldı: her katman kendi kaydını kendi içinde tutar, `Program.cs` sade kalır.
- Bu iki projenin tek paketi `Microsoft.Extensions.DependencyInjection.Abstractions`; `IServiceCollection` tipini sağlar. Web çerçevesinin tamamına bağımlı olmadan DI kullanılır.

---

## 5. `.csproj`, ProjectReference ve PackageReference

`.csproj`, bir projenin "tarif dosyası"dır (XML). Ne olarak derleneceğini ve neye bağlı olduğunu söyler.

`TedarikCore.Api.csproj` üzerinden:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
```
Bu proje bir web uygulaması. (`Domain`, `Application`, `Infrastructure` düz `Microsoft.NET.Sdk` kullanır: kütüphane.)

```xml
<TargetFramework>net10.0</TargetFramework>
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>
```
- `net10.0`: .NET 10 için derle.
- `Nullable`: "boş (`null`) olabilir mi?" sorusunu derleyici takip eder; yanlış kullanım uyarı verir.
- `ImplicitUsings`: sık kullanılan `using` satırları görünmez olarak otomatik eklenir.

```xml
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.12" />
```
**PackageReference** = dışarıdan hazır paket (NuGet'ten indirilir). Burada OpenAPI belgesi üreten paket. Sürüm sabit yazılıdır.

```xml
<ProjectReference Include="..\TedarikCore.Application\TedarikCore.Application.csproj" />
<ProjectReference Include="..\TedarikCore.Infrastructure\TedarikCore.Infrastructure.csproj" />
```
**ProjectReference** = aynı solution'daki başka projeyi kullan. Mimarideki oklar tam olarak bu satırlardır. Visual Studio'daki "Dependencies" ağacında da bunlar görünür.

Hangi proje neye bağlı:

| Proje | ProjectReference | PackageReference |
|---|---|---|
| Domain | — | — |
| Application | Domain | DependencyInjection.Abstractions |
| Infrastructure | Application, Domain | DependencyInjection.Abstractions |
| Api | Application, Infrastructure | OpenApi |
| UnitTests | — (`.csproj` dosyalarını okur) | xunit, Test.Sdk, runner, coverlet |
| IntegrationTests | Api | Mvc.Testing, xunit, Test.Sdk, runner, coverlet |

Not: Api, Domain'e doğrudan bağlı değil; Application üzerinden dolaylı ulaşır. Bu bilinçli, mimari test de izin verilen kümeyi bu şekilde tutar.

---

## 6. HealthEndpointTests.cs

Dosya: `tests/TedarikCore.IntegrationTests/HealthEndpointTests.cs`.

Ortak kurulum:

- `IClassFixture<WebApplicationFactory<Program>>`: xUnit bu sınıfın testleri için tek bir `WebApplicationFactory` yaratıp paylaşır. Factory, gerçek `Program.cs`'i **bellekte** çalıştırır. Gerçek port açılmaz, ağ kullanılmaz, ama tüm kayıtlar ve middleware sırası gerçektir.
- `CreateClient()` yardımcı metodu iki ayar yapar:
  - `BaseAddress = https://localhost`: istemci baştan https adresine gider. Böylece `UseHttpsRedirection` isteği 307'ye çevirmez.
  - `AllowAutoRedirect = false`: yönlendirme olsa bile istemci sessizce takip etmez; olursa test bunu görür ve kırılır.

Testler:

1. `Get_Health_Returns200_And_Healthy`
   - `GET /health` gönderir.
   - Kontrol 1: status kodu `200 OK`.
   - Kontrol 2: gövde tam olarak `Healthy` metni.
   - Neyi korur: `AddHealthChecks` / `MapHealthChecks` satırlarının silinmesi ya da adresin değişmesi.

2. `Get_UnknownRoute_Returns404`
   - Var olmayan bir adres isteyince `404 Not Found` beklenir.
   - Neden var: "uygulama her isteğe 200 verir" tuzağını elemek. Bu test olmasaydı, 1. test yanlış sebeple de geçebilirdi.

Bu testler **integration** türündedir: tek bir fonksiyonu değil, kurulumun tamamını birlikte dener.

---

## 7. ProjectDependencyTests.cs

Dosya: `tests/TedarikCore.UnitTests/ArchitectureTests/ProjectDependencyTests.cs`.

Bu testler iş mantığını değil, **proje bağlantılarını** denetler. Uygulama başlatmazlar; yalnızca `.csproj` dosyalarını okuyup `<ProjectReference>` satırlarını çıkarırlar. (Derleme sonrası dosya incelemek yerine `.csproj` okunur, çünkü derleyici kullanılmayan referansı çıktıya yazmaz ve kural kırılsa bile test sessizce geçerdi.)

Yardımcı metodlar:
- `FindRepositoryRoot`: testin çalıştığı klasörden yukarı çıkıp `TedarikCore.slnx`'in durduğu kök klasörü bulur.
- `GetProjectReferences`: `src/<Proje>/<Proje>.csproj` dosyasını açar, `ProjectReference` satırlarının proje adlarını liste yapar.

İki test (her biri `[Theory]` + `[InlineData]` ile 4 senaryoya çoğalır = toplam 8 test):

1. `Project_References_Only_Allowed_Projects` — **yasak bağlantı yok mu?**
   - Domain: hiçbir projeye bağlanamaz.
   - Application: yalnızca Domain.
   - Infrastructure: yalnızca Application ve Domain.
   - Api: yalnızca Application ve Infrastructure.
   - Listede olmayan her referans testi kırar ve mesajda hangi proje olduğu yazar.

2. `Project_References_Required_Project` — **gerekli bağlantı var mı?**
   - Application → Domain, Infrastructure → Application, Api → Application ve Api → Infrastructure olmalı.
   - Neden gerekli: yalnızca "yasak yok" kontrolü, tüm referanslar silinse de geçerdi.

Bu test bir keresinde Domain'e geçici olarak Api referansı yazılarak gerçekten kırmızıya döndüğü gösterildi, sonra geri alındı.

---

## 8. Bir HTTP isteği bugün nasıl işleniyor? (`GET https://localhost:7189/health`)

1. **Sunucu (Kestrel)** isteği alır. Kestrel, ASP.NET Core'un yerleşik web sunucusudur.
2. İstek **middleware hattına** girer, `Program.cs`'teki sırayla:
   - Yönlendirme (routing): adrese bakıp hangi endpoint'in eşleştiğini bulur. (`WebApplication` bunu otomatik ekler.)
   - `UseHttpsRedirection`: istek zaten https; geçer. (`http://localhost:5229/health` olsaydı burada 307 ile https'e gönderilirdi.)
   - `UseAuthorization`: endpoint'te yetki şartı yok; geçer.
3. Eşleşen endpoint **sağlık kontrolü** (`MapHealthChecks("/health")`): kayıtlı kontrolleri çalıştırır. Hiç kontrol yok, sonuç `Healthy`.
4. Cevap **HTTP 200**, gövde `Healthy` (düz metin) olarak aynı hattan geri döner ve istemciye gider.

Dikkat: bu akışta Domain, Application, Infrastructure ve controller **hiç çalışmıyor**. `/health` yalnızca Api içinde yaşıyor. İş mantığı olan bir istekte (ileride) akış şöyle olacak:

`Controller` → `Application` use case → (`Application`'daki interface) → `Infrastructure` gerçeklemesi (veritabanı) → `Domain` kuralları → cevap geri döner.

Var olmayan adrese (`/bu-yol-yok`) gelen isteğin eşleşen endpoint'i olmadığı için cevabı **404 Not Found** olur.

`/openapi/v1.json` yalnızca `Development` ortamında vardır (bkz. satır 19–22).

---

## 9. Şu anki eksik parçalar ve ne zaman eklenecekleri

Aşağıdaki aşama sırası bir **öneridir**; kesin plan değildir. Her aşama için onay gerekir.

| Eksik | Neden henüz yok | Eklenebileceği aşama (öneri) |
|---|---|---|
| Veritabanı (PostgreSQL) ve EF Core (`DbContext`, migration) | Bilerek ertelendi | Persistence aşaması; `Infrastructure/Persistence` bu iş için ayrıldı |
| `/health` içinde veritabanı kontrolü | Veritabanı yok | Veritabanı ile birlikte |
| Domain kavramları (Product, Order, Inventory) | İş kuralı henüz tasarlanmadı | Veritabanından sonra ya da öncesinde; ilk modülle birlikte |
| Saf unit testler | Test edilecek iş kuralı yok | İlk Domain/Application kuralı yazılırken |
| Application use case'leri ve interface'ler (`Abstractions`) | İş kuralı yok | İlk modül |
| Controller'lar | Henüz endpoint ihtiyacı yok | İlk modül |
| Kimlik doğrulama (JWT) ve yetkilendirme | Bilerek ertelendi. `UseAuthorization` şu an boşta. | Kimlik aşaması; ilk korumalı endpoint'ten önce |
| Hata yönetimi middleware'i, doğrulama | Kullanılan bir yer yok. `Api/Middleware` bu sebeple kaldırıldı, gerekince açılır. | İlk gerçek endpoint'lerle birlikte |
| React arayüzü | Bilerek ertelendi | Backend oturunca |
| Docker / yayına alma | Bilerek ertelendi | En sona doğru |
| Otomatik CI | **Bilerek hiç yok** (karar: testler yalnızca yerelde) | — |
| Coverage ölçümü | Kurulmadı | İhtiyaç olursa |
