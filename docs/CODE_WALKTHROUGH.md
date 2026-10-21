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
| `TedarikCore.Domain` | İşin kendisi: "Ürün", "Sipariş" gibi kavramlar ve değişmez iş kuralları. Hiçbir şeye bağımlı değildir. | `Catalog/Product.cs`: ürün kavramı ve kuralları (bkz. 10. bölüm). |
| `TedarikCore.Application` | Uygulamanın yaptığı işler (use case): "sipariş oluştur" gibi. Dış dünyayla konuşacağı şeylerin sözleşmelerini (interface) burada tanımlar. | Yalnızca `AddApplication()` kayıt metodu (boş). |
| `TedarikCore.Infrastructure` | Dış dünya ile gerçek konuşma: veritabanı, e-posta, dosya vb. Application'daki sözleşmeleri gerçekler. | `AppDbContext` (PostgreSQL kapısı, `Products` tablosu), `ProductConfiguration` (tablo eşlemesi), `Migrations` (veritabanı değişiklik dosyaları), `AddInfrastructure()` kayıt metodu ve `/health/ready` için veritabanı sağlık kontrolü. |
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
| `src/TedarikCore.Domain/Catalog` | Domain | "Catalog" = ürün kataloğu. Ürünle ilgili iş kuralları burada; veritabanını ya da web'i bilmez. |
| `src/TedarikCore.Infrastructure/Persistence` | Infrastructure | "Persistence" = verinin kalıcı saklanması. Veritabanı erişim kodu burada: `AppDbContext.cs`. |
| `src/TedarikCore.Infrastructure/Persistence/Configurations` | Infrastructure | Her entity'nin tablo eşlemesi (hangi kolon, hangi tür, hangi indeks). Tablo detayı Domain'e sızmasın diye burada. |
| `src/TedarikCore.Infrastructure/Persistence/Migrations` | Infrastructure | **Migration** = veritabanının şemasını adım adım değiştiren, EF Core'un ürettiği dosyalar. Şu an tek dosya: `InitialCatalog` (**henüz hiçbir veritabanına uygulanmadı**). |
| `tests/TedarikCore.UnitTests` | Test | Hızlı testler; uygulama ayağa kalkmaz. |
| `tests/TedarikCore.IntegrationTests` | Test | Uygulamayı bellekte ayağa kaldırıp HTTP ile deneyen testler. |
| `docs/` | — | Proje belgeleri (durum, kararlar, bu rehber). |

Klasör içinde olmayan ama önemli dosyalar:

- `CLAUDE.md` (kök): projenin kalıcı kuralları.
- `.gitignore`: git'in saymaması gereken dosyalar (`bin/`, `obj/`, `.vs/`, `*.user`, gizli dosyalar, test çıktıları).
- `appsettings.json` / `appsettings.Development.json`: uygulama ayarları (şimdilik yalnızca log seviyeleri ve `AllowedHosts`). Parola ve bağlantı bilgisi buraya yazılmaz; veritabanı bağlantı dizesi **User Secrets**'tadır (bkz. aşağıdaki "Veritabanı bağlantısı" bölümü).
- `TedarikCore.Api.http`: Visual Studio'dan `/health`'e elle istek atmak için örnek dosya.
- `dotnet-tools.json` (kök): bu projede kullanılan komut satırı araçlarını sabitler. Şu an `dotnet-ef` 10.0.12 (migration üretmek için). `dotnet tool restore` ile kurulur.
- `TedarikCore.Api.csproj.user`: Visual Studio'nun kişisel ayarı (git'e girmez).
- `bin/` ve `obj/`: derleme çıktıları; silinse yeniden üretilir. Git'e girmez.

Temizlik notu: `Domain/Common` ve `Api/Middleware` klasörleri yalnızca `.gitkeep` içerdiği için kaldırıldı. (`.gitkeep` git'in boş klasörü saymaması yüzünden konan boş bir yer tutucu dosyadır.) Gerekince yeniden açılır.

---

## 3. Program.cs satır satır

Dosya: `src/TedarikCore.Api/Program.cs`. Uygulamanın giriş noktasıdır. Üç aşaması var: **kur** (servisleri kaydet), **kapıyı hazırla** (middleware sırasını belirle), **çalıştır**.

```csharp
using Microsoft.AspNetCore.Diagnostics.HealthChecks;   // 1
using TedarikCore.Application;                          // 2
using TedarikCore.Infrastructure;                       // 3
```
1. `HealthCheckOptions` sınıfını kullanabilmek için (sağlık adreslerini ayarlarken lazım).
2–3. `AddApplication()`, `AddInfrastructure()` ve `HealthCheckTags` için bu namespace'leri açar.

```csharp
var builder = WebApplication.CreateBuilder(args);       // 5
```
5. Uygulamayı kuracak "inşaatçı"yı (builder) yaratır. `args` komut satırı parametreleridir. Ayar kaynaklarını (`appsettings*.json`, **User Secrets**, ortam değişkenleri), log'u ve web sunucusunu (Kestrel) varsayılanlarla hazırlar. `WebApplication` için ayrıca `using` yazmaya gerek yok, çünkü `.csproj`'daki `ImplicitUsings` bunu otomatik ekler.

```csharp
builder.Services.AddApplication();                      // 9
builder.Services.AddInfrastructure(builder.Configuration);   // 10
```
9–10. Katmanların kendi servislerini DI listesine (`Services`) kaydettirir. `AddInfrastructure`'a ayar nesnesi (`builder.Configuration`) verilir; veritabanı bağlantı dizesi oradan okunur (bkz. 4. bölüm). `AddApplication` şu an boş.

```csharp
builder.Services.AddControllers();                      // 12
```
12. Controller'ları (sınıf tabanlı endpoint'ler) destekler. Henüz controller yok, ama ilerisi için hazır.

```csharp
builder.Services.AddHealthChecks();                     // 13
```
13. Sağlık kontrol sistemini ekler. (Veritabanı kontrolünü `AddInfrastructure` bu sisteme ekler.)

```csharp
builder.Services.AddOpenApi();                          // 15
```
15. API'nin makine tarafından okunabilir tarifini (OpenAPI belgesi, JSON) üretecek servisi ekler.

```csharp
var app = builder.Build();                              // 17
```
17. Kayıt aşaması biter; kurulan uygulama nesnesi (`app`) oluşur. Bundan sonra servis eklenmez, yalnızca istek hattı düzenlenir.

```csharp
if (app.Environment.IsDevelopment())                    // 20
{
    app.MapOpenApi();                                   // 22
}
```
20–23. Yalnızca geliştirme ortamında `/openapi/v1.json` adresini açar.

```csharp
app.UseHttpsRedirection();                              // 25
```
25. `http://` ile gelen isteği `https://` adresine yönlendirir (307 cevabı ile). Bu yüzden testlerde istemci zaten https'e gider (6. bölüm).

```csharp
app.UseAuthorization();                                 // 27
```
27. "Bu kullanıcının buna izni var mı?" kontrolünü hatta ekler. Henüz kimlik doğrulama ve `[Authorize]` olmadığı için şu an hiçbir şeyi engellemez.

```csharp
app.MapControllers();                                   // 29
```
29. Controller'ların adreslerini hatta bağlar (şu an sıfır adet).

```csharp
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });   // 31
```
31. `/health` adresi. `Predicate = _ => false` "hiçbir kontrolü çalıştırma" demektir. Yani bu adres **yalnızca uygulamanın ayakta olduğunu** söyler ve veritabanına hiç bakmaz. Veritabanı kapalı olsa bile `200 Healthy` döner. (Buna "liveness", yani yaşıyor mu kontrolü denir.)

```csharp
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains(HealthCheckTags.Ready)
});                                                     // 33–36
```
33–36. `/health/ready` adresi. Yalnızca `ready` etiketi taşıyan kontrolleri çalıştırır; şu an bu tek kontrol veritabanı bağlantısıdır. Veritabanına ulaşılabiliyorsa `200 Healthy`, ulaşılamıyorsa `503 Unhealthy` döner. (Buna "readiness", yani iş almaya hazır mı kontrolü denir.) Hata ayrıntısı dışarı verilmez, yalnızca `Unhealthy` yazar.

```csharp
app.Run();                                              // 38
```
38. Uygulamayı başlatır ve istek beklemeye başlar. Kapatılana kadar burada bekler.

```csharp
public partial class Program;                           // 41
```
41. Üst düzey (top-level) `Program.cs` sınıfı varsayılan olarak dışarıya kapalıdır. Bu satır sınıfı `public` yapar ki test projesi `WebApplicationFactory<Program>` ile uygulamayı bulup ayağa kaldırabilsin. Uygulama davranışını değiştirmez.

---

## 4. Infrastructure: DependencyInjection.cs, AppDbContext ve veritabanı bağlantısı

### Application ve Infrastructure `DependencyInjection.cs`

İki dosya benzer kalıptadır. Application'daki hâlâ boştur:

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
```

- `static class`: nesnesi üretilmeyen, yalnızca yardımcı metot taşıyan sınıf.
- `this IServiceCollection services`: bu bir **extension method** (genişletme metodu). Sayesinde `Program.cs`'te `builder.Services.AddApplication()` diye doğal biçimde çağrılır.
- `IServiceCollection`: DI kayıt listesi. "Birisi `X` isterse `Y` ver" kayıtları buraya eklenir.
- `return services;`: zincirleme çağrı yapılabilsin diye listeyi geri verir.
- Her katman kendi kaydını kendi içinde tutar, `Program.cs` sade kalır.

### `AddInfrastructure(services, configuration)` ne yapar?

1. `AddDbContext<AppDbContext>(...)`: `AppDbContext`'i DI listesine kaydeder. İçindeki ayar fonksiyonu **ilk kullanıldığı anda** çalışır:
   - Ayarlardan `ConnectionStrings:Default` değerini okur.
   - Yoksa "ConnectionStrings:Default tanımlı değil" diye açık bir hata verir.
   - Varsa `UseNpgsql(...)` ile "bu veritabanı PostgreSQL" der.
2. `AddHealthChecks().AddDbContextCheck<AppDbContext>("database", tags: ["ready"])`: veritabanı bağlantısını deneyen bir sağlık kontrolü ekler ve ona `ready` etiketi koyar. `/health/ready` bu etikete bakar.

Neden bağlantı dizesi ilk kullanımda okunuyor? Bağlantı bilgisi yoksa uygulama yine açılır; yalnızca veritabanına ihtiyaç duyan işler (`/health/ready`) başarısız olur. Böylece `/health` ve testler veritabanı kurulu olmayan makinede de çalışır.

### Terimler
- **EF Core (Entity Framework Core):** Veritabanı tablolarına C# nesneleriyle ulaşmayı sağlayan kütüphane.
- **Npgsql:** .NET'in PostgreSQL ile konuşma sürücüsü. `Npgsql.EntityFrameworkCore.PostgreSQL` paketi EF Core'u PostgreSQL'e bağlar.
- **`DbContext` (`AppDbContext`):** Veritabanıyla yapılan işlerin (sorgu, kaydetme) geçtiği kapı. İçinde `Products` adlı bir `DbSet<Product>` (ürün tablosunun kapısı) var; eşlemeyi `OnModelCreating` içindeki `ApplyConfigurationsFromAssembly` ile `Configurations` klasöründen toplar. Migration var ama veritabanına uygulanmadı, o yüzden gerçek veritabanında tablo yok.
- **`HealthCheckTags.Ready`:** `"ready"` yazısını tek yerde tutan sabit; yazım hatası olmasın diye.

### Veritabanı bağlantısı ve gizli bilgi
Bağlantı dizesi parola içerdiği için Git'e girmez. Geliştirmede **User Secrets** kullanılır: Windows'ta kullanıcı klasöründe duran, proje dışı bir dosyadır. Api projesinin `.csproj` dosyasındaki `UserSecretsId` (rastgele bir kimlik, sır değildir) hangi dosyanın okunacağını söyler. Kurulum:

```
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=tedarikcore_dev;Username=...;Password=..." --project src/TedarikCore.Api
```

Ortam değişkeni de olur: `ConnectionStrings__Default=...` (çift alt çizgi, `:` yerine geçer). Ortam değişkeni User Secrets'ın önüne geçer.

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
| Infrastructure | Application, Domain | DependencyInjection.Abstractions, Npgsql.EntityFrameworkCore.PostgreSQL, HealthChecks.EntityFrameworkCore |
| Api | Application, Infrastructure | OpenApi, EntityFrameworkCore.Design (yalnızca migration aracı için; `PrivateAssets=all`, uygulamaya taşınmaz) |
| UnitTests | Domain, Infrastructure | Configuration, xunit, Test.Sdk, runner, coverlet |
| IntegrationTests | Api | Mvc.Testing, Configuration.UserSecrets, xunit, Test.Sdk, runner, coverlet |

Yeni paketlerin işi: `Npgsql.EntityFrameworkCore.PostgreSQL` EF Core'u (ve onun üzerinden `Microsoft.EntityFrameworkCore` paketini) getirir ve PostgreSQL'e bağlar. `HealthChecks.EntityFrameworkCore` bir `DbContext`'in veritabanına bağlanabildiğini deneyen hazır sağlık kontrolünü verir. `Api.csproj` içindeki `<UserSecretsId>` satırı User Secrets dosyasının kimliğidir; sır değildir, Git'e girer.

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

### HealthReadyEndpointTests.cs (veritabanı sağlık testleri)

Dosya: `tests/TedarikCore.IntegrationTests/HealthReadyEndpointTests.cs`. Aynı https istemci ayarını kullanır. "Veritabanı erişilemez" durumunu, test sırasında bağlantı dizesini port 1'e (orada kimse dinlemez) çeviren bellek içi bir ayarla yapar. Bu ayar User Secrets'taki gerçek bağlantı dizesinin önüne geçer; yani testler gerçek veritabanı açıkken de aynı sonucu verir ve gerçek veritabanına zarar vermez.

1. `Get_Health_Returns200_Even_When_Database_Unreachable`: veritabanı erişilemezken `/health` yine `200 Healthy`. Korunan şey: yaşıyor mu kontrolü veritabanına bağlanmamalı.
2. `Get_HealthReady_Returns503_When_Database_Unreachable`: veritabanı erişilemezken `/health/ready` `503` ve `Unhealthy`. Korunan şey: hazır değilken "hazır" dememek.
3. `Get_HealthReady_Returns200_When_Database_Reachable`: gerçek PostgreSQL'e bağlanıp `200 Healthy` bekler. Bunu ancak gerçek veritabanı kanıtlayabilir, sahte (mock) kanıtlayamaz.
   - `[RequiresDatabaseFact]` özel bir attribute'tür (`RequiresDatabaseFactAttribute.cs`): bağlantı dizesi (User Secrets veya ortam değişkeni) yoksa testi **atlar** (skip). Böylece veritabanı olmayan makinede test kırmızıya dönmez ama raporda "Skipped" olarak görünür, sessizce geçmiş gibi durmaz.

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

### DependencyInjectionTests.cs (UnitTests/Infrastructure)

Dosya: `tests/TedarikCore.UnitTests/Infrastructure/DependencyInjectionTests.cs`. Gerçek veritabanı ya da uygulama gerekmeden `AddInfrastructure` kaydını dener:

1. `AddInfrastructure_Registers_AppDbContext_Without_Opening_Connection`: bağlantı dizesi varsa `AppDbContext` kayıt listesinden alınabilir. `DbContext` nesnesini oluşturmak bağlantı açmaz, o yüzden ağ kullanılmaz.
2. `AppDbContext_Without_ConnectionString_Throws_Clear_Error`: bağlantı dizesi yoksa hata mesajı `ConnectionStrings:Default` ifadesini içerir. Bu yolu integration testte denemek zordur, çünkü User Secrets doluyken "yok" durumu oluşmaz.

---

## 8. Bir HTTP isteği bugün nasıl işleniyor? (`GET https://localhost:7189/health`)

1. **Sunucu (Kestrel)** isteği alır. Kestrel, ASP.NET Core'un yerleşik web sunucusudur.
2. İstek **middleware hattına** girer, `Program.cs`'teki sırayla:
   - Yönlendirme (routing): adrese bakıp hangi endpoint'in eşleştiğini bulur. (`WebApplication` bunu otomatik ekler.)
   - `UseHttpsRedirection`: istek zaten https; geçer. (`http://localhost:5229/health` olsaydı burada 307 ile https'e gönderilirdi.)
   - `UseAuthorization`: endpoint'te yetki şartı yok; geçer.
3. Eşleşen endpoint **sağlık kontrolü** (`MapHealthChecks("/health")`): `Predicate = _ => false` olduğu için hiçbir kontrolü çalıştırmaz; sonuç `Healthy`.
4. Cevap **HTTP 200**, gövde `Healthy` (düz metin) olarak aynı hattan geri döner ve istemciye gider.

`GET /health/ready` farkı: 3. adımda `ready` etiketli kontroller çalışır. `AppDbContext` DI listesinden alınır (bu sırada bağlantı dizesi okunur), PostgreSQL'e bağlanmayı dener:
- Bağlanırsa: `200 Healthy`.
- Bağlanamazsa ya da bağlantı dizesi yoksa: `503 Unhealthy`. Uygulama çökmez, `/health` etkilenmez.

Dikkat: `/health` akışında Domain, Application, controller ve veritabanı **hiç çalışmıyor**. `/health/ready` ise Infrastructure'daki `AppDbContext`'e kadar iner; Domain ve Application yine devrede değildir. İş mantığı olan bir istekte (ileride) akış şöyle olacak:

`Controller` → `Application` use case → (`Application`'daki interface) → `Infrastructure` gerçeklemesi (veritabanı) → `Domain` kuralları → cevap geri döner.

Var olmayan adrese (`/bu-yol-yok`) gelen isteğin eşleşen endpoint'i olmadığı için cevabı **404 Not Found** olur.

`/openapi/v1.json` yalnızca `Development` ortamında vardır (bkz. satır 19–22).

---

## 9. Şu anki eksik parçalar ve ne zaman eklenecekleri

Aşağıdaki aşama sırası bir **öneridir**; kesin plan değildir. Her aşama için onay gerekir.

| Eksik | Neden henüz yok | Eklenebileceği aşama (öneri) |
|---|---|---|
| `InitialCatalog` migration'ının **uygulanması** | Bilerek yapılmadı: onay bekliyor. Migration dosyası ve SQL'i var, ama `tedarikcore_dev` hâlâ boş | Kullanıcı onayından sonra (öneri: ADIM 06) |
| Ürün için veri yazan/okuyan kod (repository, servis), API | Kapsam dışıydı | Migration uygulandıktan sonra |
| CHECK kısıtının (`price_kurus > 0`) PostgreSQL'de gerçekten reddettiğinin kanıtı | Kısıt SQL'de ve modelde var, ama gerçek veritabanında denenmedi | Ayrı test veritabanı onaylanınca |
| Güvenli veritabanı kullanıcısı | Geliştirmede `postgres` yönetici kullanıcısı ve basit bir demo parolası kullanılıyor | Canlıya çıkmadan önce; ayrı, yetkisi sınırlı kullanıcı |
| Test için ayrı veritabanı | Gerçek-DB testi şimdilik `tedarikcore_dev`'e bağlanıyor (yalnızca okuma/bağlanma) | Veri yazan ilk testlerden önce (öneri: `tedarikcore_test`) |
| Domain kavramları Order, Inventory (Product artık var) | İş kuralı henüz tasarlanmadı | Sonraki modüllerle |
| Application katmanında unit test | Application'da henüz iş mantığı yok (Domain'de `Product` testleri var) | İlk use case yazılırken |
| Application use case'leri ve interface'ler (`Abstractions`) | İş kuralı yok | İlk modül |
| Controller'lar | Henüz endpoint ihtiyacı yok | İlk modül |
| Kimlik doğrulama (JWT) ve yetkilendirme | Bilerek ertelendi. `UseAuthorization` şu an boşta. | Kimlik aşaması; ilk korumalı endpoint'ten önce |
| Hata yönetimi middleware'i, doğrulama | Kullanılan bir yer yok. `Api/Middleware` bu sebeple kaldırıldı, gerekince açılır. | İlk gerçek endpoint'lerle birlikte |
| React arayüzü | Bilerek ertelendi | Backend oturunca |
| Docker / yayına alma | Bilerek ertelendi | En sona doğru |
| Otomatik CI | **Bilerek hiç yok** (karar: testler yalnızca yerelde) | — |
| Coverage ölçümü | Kurulmadı | İhtiyaç olursa |

---

## 10. Product, eşleme, migration ve yeni testler

### Product (`Domain/Catalog/Product.cs`)
Bir ürünü ve onun değişmez kurallarını tarif eder. Veritabanını, web'i ya da başka projeyi bilmez.

| Özellik | Anlamı | Kural |
|---|---|---|
| `Id` | Benzersiz kimlik (`Guid`) | Nesne doğarken kod üretir |
| `Sku` | Ürün kodu (stok takip kodu) | Boş olamaz, kırpıldıktan sonra en fazla 64 karakter, **BÜYÜK HARFE** çevrilir |
| `Name` | Ürün adı | Boş olamaz, kırpıldıktan sonra en fazla 200 karakter, harf büyüklüğü korunur |
| `PriceKurus` | Fiyat, **kuruş** cinsinden (`long`) | 0'dan büyük olmalı. 12,50 TL = 1250 |
| `IsActive` | Satışta mı | Başlangıçta `true` |

- **`private set`:** Özellikler dışarıdan doğrudan değiştirilemez. Değer yalnızca `Create` ve `ChangePrice` üzerinden, doğrulanarak girer.
- **`Product.Create(sku, name, priceKurus)`:** Ürün üretmenin tek yolu. Bu tür metoda **factory (fabrika metodu)** denir: nesneyi doğrulayıp hazır halde verir. Geçersiz değerde hata fırlatır, yarım ürün oluşmaz.
- **`ChangePrice(newPriceKurus)`:** Fiyatı değiştirir. Yeni fiyat da aynı kuralla doğrulanır; geçersizse eski fiyat korunur.
- **`private Product()` (parametresiz constructor):** Yalnızca EF Core içindir. EF veritabanından okuduğu satırdan nesne üretirken kullanır; kod tarafından çağrılamaz.
- **Hatalar:** SKU/ad için `ArgumentException`, fiyat için `ArgumentOutOfRangeException` (ikisi de .NET'in yerleşik hata türü).
- **`ToUpperInvariant`:** Büyük harf çevirisi kültürden bağımsız yapılır. Türkçe ayarlı bir makinede `ToUpper()` "i"yi "İ" yapar ve aynı SKU iki makinede farklı çıkardı.
- **SKU benzersizliği burada kontrol edilmez.** Bir nesne diğer ürünleri göremez. Bu kural veritabanındaki **unique index** ile korunur (aşağıda). İki kişi aynı anda aynı SKU'yu eklemeye çalışırsa veritabanı birini reddeder; bu, kod tarafında "önce var mı diye bak" kontrolünden daha güvenlidir.
- Sipariş sırasında fiyat değişirse müşterinin yeniden onayı kuralı sonraki Order/Checkout modülünde uygulanacak; `Product` yalnızca fiyatı doğru tutar.

### Eşleme (`Infrastructure/Persistence/Configurations/ProductConfiguration.cs`)
**Fluent API:** Kolon ve tablo ayarlarını attribute (`[Column]`) yerine kodla, ayrı bir sınıfta yazma yolu. Böylece `Product` sınıfı veritabanı ayrıntısıyla kirlenmez.

| Yapı | PostgreSQL karşılığı |
|---|---|
| tablo | `products` |
| `Id` | `id uuid`, birincil anahtar `pk_products` (değeri veritabanı değil kod üretir, `ValueGeneratedNever`) |
| `Sku` | `sku character varying(64) NOT NULL` + **unique index** `ix_products_sku` |
| `Name` | `name character varying(200) NOT NULL` |
| `PriceKurus` | `price_kurus bigint NOT NULL` + **CHECK kısıtı** `ck_products_price_kurus_positive`: `price_kurus > 0` |
| `IsActive` | `is_active boolean NOT NULL` |

Uzunluk sınırları `Product.MaxSkuLength` ve `Product.MaxNameLength` sabitlerinden gelir: kural tek yerde durur, Domain ile veritabanı ayrışamaz.

### Migration (`Persistence/Migrations`)
- **Ne yapar:** Veritabanı şemasını koddan üretilen adımlarla değiştirir. `InitialCatalog` yalnızca `products` tablosunu (içinde birincil anahtar ve **CHECK** kısıtı), `ix_products_sku` indeksini oluşturur (SQL'i `CREATE TABLE`, `CREATE UNIQUE INDEX` ve bir geçmiş kaydı ekler; `DROP`/`DELETE`/`ALTER` yoktur). **CHECK kısıtı** = tabloya her satır eklenirken/güncellenirken veritabanının kontrol ettiği kural; `price_kurus > 0` olmayan satırı Domain'i atlayan bir yol bile ekleyemez.
- **Dosyalar:** `..._InitialCatalog.cs` (uygula: `Up`; geri al: `Down`), `.Designer.cs` (EF'in iç kaydı), `AppDbContextModelSnapshot.cs` (modelin son hâlinin fotoğrafı; bir sonraki migration "ne değişti" farkını bununla bulur).
- **Hâlâ uygulanmadı.** Dosya ve SQL var ama hiçbir veritabanında çalıştırılmadı. `tedarikcore_dev` boş. SQL'in üretilmiş olması PostgreSQL'de başarıyla çalıştığını **kanıtlamaz**; bunu ancak onaydan sonraki uygulama adımı gösterir.
- **Güvenlik önlemi:** Migration üretilirken bağlantı, kimsenin dinlemediği bir adrese çevrilir; araç yanlışlıkla gerçek veritabanına bağlansa bile bağlanamaz.

### Yeni testler (UnitTests)
- `Domain/Catalog/ProductTests.cs` (28 test): geçerli ürün, boş SKU/ad (`null`, `""`, boşluk), SKU kırpma ve büyük harf, **sınır değerleri** (tam 64/200 geçer, 65/201 reddedilir, uzunluk kırpmadan sonra ölçülür), sıfır/negatif/`long.MinValue` fiyat reddi, en küçük (1) ve en büyük (`long.MaxValue`) fiyat, geçerli ve geçersiz `ChangePrice` (geçersizde eski fiyat korunur), public setter olmaması, Türkçe kültürde SKU. Hepsi saf; DB ve ağ yok.
- `Infrastructure/ProductMappingTests.cs` (11 test): `AppDbContext`'in **modelini** veritabanına bağlanmadan kurar ve tablo, kolon adları, `uuid`/`bigint`/`boolean` türleri, unique index, uzunluklar ve CHECK kısıtının adı ile ifadesini kontrol eder. Bağlantı dizesi bilerek ulaşılamaz bir adrestir.
- `Infrastructure/MigrationTests.cs` (5 test), hiçbiri veritabanına bağlanmaz:
  1. `InitialCatalog` var ve tek migration.
  2. Model ile migration fotoğrafı (snapshot) uyuşuyor mu. Biri entity'yi değiştirip migration'ı güncellemeyi unutursa kırılır.
  3. `Up` yalnızca `products` tablosunu (CHECK ile) ve `ix_products_sku` unique indeksini oluşturuyor.
  4. `Up` içinde `CreateTable`/`CreateIndex` dışında hiçbir işlem (silme, ham SQL vb.) yok.
  5. `Down` yalnızca `products` tablosunu düşürüyor.
  3. ve 4. test, migration dosyasındaki bir satır silinirse snapshot hâlâ doğru görünse bile kırılır (mutasyonla doğrulandı).
- Test edilmeyen: SKU unique kısıtının **gerçekten** çalıştığı ve migration'ın gerçek PostgreSQL'de uygulandığı. İkisi de gerçek veritabanı ister; ayrı test veritabanı onayı bekliyor.
