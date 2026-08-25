using Luong.Kernel.Abstractions;
using Luong.Kernel.Application;
using Luong.Kernel.AspNetCore.Middleware;
using Luong.Kernel.AspNetCore.Security;
using ONoOffice.Api.Extensions;
using ONoOffice.Api.Infrastructure;
using ONoOffice.Api.Middleware;
using ONoOffice.Identity.Application.Authentication.Login;
using ONoOffice.Identity.Infrastructure;
using ONoOffice.Identity.Infrastructure.Persistence;
using ONoOffice.Org.Application.Departments.GetTree;
using ONoOffice.Org.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
//  ÄÄ‚NG KÃ Dá»ŠCH Vá»¤
// â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

builder.Services
    .AddApiControllers()
    .AddMessageCatalog()
    .AddConfiguredLocalization()
    .AddConfiguredCors(builder.Configuration)
    .AddJwtAuthentication(builder.Configuration)
    .AddPermissionAuthorization();

// ICurrentUser vÃ  ICurrentTenant do CÃ™NG má»™t lá»›p cÃ i â€” cÃ¹ng Ä‘á»c tá»« má»™t ClaimsPrincipal.
// ÄÄƒng kÃ½ lá»›p cá»¥ thá»ƒ má»™t láº§n rá»“i báº¯c cáº§u hai cá»•ng vÃ o nÃ³, Ä‘á»ƒ hai cá»•ng trong cÃ¹ng má»™t
// request khÃ´ng bao giá» nhÃ¬n tháº¥y hai báº£n khÃ¡c nhau.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<HttpContextCurrentUser>();
builder.Services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<HttpContextCurrentUser>());
builder.Services.AddScoped<ICurrentTenant>(sp => sp.GetRequiredService<HttpContextCurrentUser>());

// KhÃ´ng giá»¯ tráº¡ng thÃ¡i gÃ¬, vÃ  job ná»n cÅ©ng cáº§n -> Singleton.
builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

// MediatR + 3 behavior (log â†’ kiá»ƒm dá»¯ liá»‡u â†’ transaction), quÃ©t handler trong assembly
// Application cá»§a Tá»ªNG module. Thiáº¿u má»™t assembly á»Ÿ Ä‘Ã¢y thÃ¬ handler cá»§a module Ä‘Ã³ khÃ´ng
// Ä‘Æ°á»£c Ä‘Äƒng kÃ½, vÃ  MediatR nÃ©m "no handler" ngay láº§n gá»i Ä‘áº§u â€” á»“n Ã o, dá»… tháº¥y, cháº¥p nháº­n
// Ä‘Æ°á»£c. Nguy hiá»ƒm hÆ¡n nhiá»u lÃ  chá»— `IUnitOfWork` ngay bÃªn dÆ°á»›i.
builder.Services.AddApplicationLayer(
    typeof(LoginCommand).Assembly,
    typeof(GetDepartmentTreeQuery).Assembly);

builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddOrgModule(builder.Configuration);

/*
  âš ï¸ ÄÄ‚NG KÃ SAU Cáº¢ HAI MODULE â€” thá»© tá»± á»Ÿ Ä‘Ã¢y quyáº¿t Ä‘á»‹nh dá»¯ liá»‡u cÃ³ Ä‘Æ°á»£c ghi hay khÃ´ng.

  `TransactionBehavior` phÃ¢n giáº£i Má»˜T `IUnitOfWork` rá»“i gá»i `SaveChanges` sau má»—i má»‡nh
  lá»‡nh thÃ nh cÃ´ng. `AddIdentityModule` Ä‘Ã£ Ä‘Äƒng kÃ½ `IUnitOfWork` trá» vÃ o `IdentityDbContext`.
  Äá»ƒ nguyÃªn nhÆ° váº­y thÃ¬ má»i má»‡nh lá»‡nh cá»§a Org gá»i `SaveChanges` trÃªn context KHÃ”NG theo
  dÃµi thá»±c thá»ƒ nÃ o cá»§a Org: EF khÃ´ng sinh cÃ¢u SQL nÃ o, khÃ´ng nÃ©m lá»—i, tráº£ vá» 0 â€” API tráº£
  200, giao diá»‡n hiá»‡n "Ä‘Ã£ lÆ°u", database trá»‘ng trÆ¡n.

  Báº£n ghi Ä‘Ã¨ nÃ y chá»‘t cáº£ hai context. Giá»›i háº¡n vá» tÃ­nh nguyÃªn tá»­ ghi rÃµ trong
  `CompositeUnitOfWork`, vÃ  `UnitOfWorkWiringTests` canh Ä‘á»ƒ khÃ´ng ai vÃ´ tÃ¬nh gá»¡ nÃ³.
*/
builder.Services.AddScoped<IUnitOfWork, CompositeUnitOfWork>();

var app = builder.Build();

// Thá»­ bá»™ tÃ i nguyÃªn dá»‹ch NGAY, trÆ°á»›c khi nháº­n request Ä‘áº§u tiÃªn. Thiáº¿u satellite assembly
// lÃ  lá»—i Ä‘Ã³ng gÃ³i im láº·ng: há»‡ thá»‘ng váº«n cháº¡y Ä‘Ãºng, chá»‰ lÃ  khÃ´ng cÃ²n dá»‹ch Ä‘Æ°á»£c gÃ¬.
app.Services.KiemTraBanDich();

// Gieo dá»¯ liá»‡u má»“i â€” KHÃ”NG lÃ m gÃ¬ cáº£ trá»« khi `Seed:Enabled` Ä‘Æ°á»£c báº­t (máº·c Ä‘á»‹nh táº¯t).
//
// Cháº¡y TRÆ¯á»šC app.Run(), tá»©c lÃ  trÆ°á»›c khi cá»•ng HTTP má»Ÿ: náº¿u migration há»ng thÃ¬ á»©ng dá»¥ng
// khÃ´ng lÃªn, thay vÃ¬ lÃªn rá»“i tráº£ 500 cho má»i request. NgÆ°á»i phÃ¡t hiá»‡n pháº£i lÃ  ngÆ°á»i
// triá»ƒn khai, khÃ´ng pháº£i ngÆ°á»i dÃ¹ng Ä‘áº§u tiÃªn.
await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<IdentityDataSeeder>().RunAsync();
}

// â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
//  ÄÆ¯á»œNG ÄI Cá»¦A Má»˜T REQUEST
//
//  Thá»© tá»± dÆ°á»›i Ä‘Ã¢y KHÃ”NG pháº£i sá»Ÿ thÃ­ch. Má»—i dÃ²ng náº±m á»Ÿ Ä‘Ãºng chá»— cá»§a nÃ³ vÃ¬ má»™t lÃ½
//  do, vÃ  Ä‘áº£o Ä‘i thÃ¬ há»ng theo má»™t kiá»ƒu riÃªng â€” chÃº thÃ­ch tá»«ng dÃ²ng.
// â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

// â‘  Sá»›m nháº¥t cÃ³ thá»ƒ: má»i dÃ²ng log sinh ra sau Ä‘Ã¢y Ä‘á»u mang mÃ£ láº§n váº¿t. Äáº·t muá»™n hÆ¡n
//    thÃ¬ Ä‘Ãºng nhá»¯ng log cá»§a máº¥y táº§ng ngoÃ i cÃ¹ng láº¡i lÃ  log khÃ´ng láº§n váº¿t Ä‘Æ°á»£c.
app.UseCorrelationId();

// â‘¡ Bá»c ngoÃ i má»i thá»© cÃ²n láº¡i. Exception lá»t tá»›i Ä‘Ã¢y thÃ nh 500 rá»—ng ruá»™t + má»™t dÃ²ng
//    log Ä‘áº§y Ä‘á»§. Äáº·t vÃ o trong thÃ¬ exception cá»§a cÃ¡c middleware ngoÃ i nÃ³ khÃ´ng ai báº¯t,
//    vÃ  ngÆ°á»i dÃ¹ng nháº­n má»™t trang lá»—i máº·c Ä‘á»‹nh cá»§a mÃ¡y chá»§, kÃ¨m stack trace.
app.UseProblemDetailsExceptionHandler();

// â‘¢ Header an toÃ n. Äáº·t trong â‘¡ nhÆ°ng gáº¯n header qua OnStarting, nÃªn pháº£n há»“i lá»—i do
//    â‘¡ dá»±ng ra cÅ©ng Ä‘Æ°á»£c báº£o vá»‡.
app.UseSecurityHeaders();

// â‘£ Äáº·t CultureInfo cho request theo Accept-Language. Pháº£i TRÆ¯á»šC MVC, vÃ¬ bá»™ lá»c dá»‹ch
//    thÃ´ng bÃ¡o lá»—i Ä‘á»c CultureInfo.CurrentUICulture lÃºc dá»±ng pháº£n há»“i.
app.UseRequestLocalization();

// â‘¤ â­ TRÆ¯á»šC â‘¦, vÃ  Ä‘Ã¢y lÃ  lÃ½ do THáº¬T â€” Ä‘Ã£ kiá»ƒm chá»©ng báº±ng test, khÃ´ng pháº£i nghe ká»ƒ.
//
//    Lá»i giáº£i thÃ­ch quen thuá»™c lÃ  "preflight OPTIONS khÃ´ng mang token nÃªn sáº½ bá»‹ 401".
//    á»ž cáº¥u hÃ¬nh nÃ y Ä‘iá»u Ä‘Ã³ KHÃ”NG xáº£y ra: OPTIONS khÃ´ng khá»›p endpoint nÃ o (Ä‘á»‹nh tuyáº¿n
//    theo thuá»™c tÃ­nh chá»‰ map GET/POST), nÃªn â‘¦ khÃ´ng cÃ³ policy nÃ o Ä‘á»ƒ Ã¡p vÃ  cá»© tháº¿ cho qua.
//
//    Chuyá»‡n tháº­t sá»± há»ng lÃ  á»Ÿ request BÃŒNH THÆ¯á»œNG bá»‹ tá»« chá»‘i: â‘¦ cáº¯t ngang vÃ  tráº£ 401
//    ngay táº¡i chá»— nÃ³ Ä‘á»©ng. Náº¿u â‘¤ náº±m sau â‘¦ thÃ¬ middleware CORS khÃ´ng bao giá» cháº¡y cho
//    pháº£n há»“i Ä‘Ã³, vÃ  401 Ä‘i ra KHÃ”NG cÃ³ Access-Control-Allow-Origin. TrÃ¬nh duyá»‡t vÃ¬ tháº¿
//    khÃ´ng cho JavaScript Ä‘á»c pháº£n há»“i â€” ká»ƒ cáº£ mÃ£ tráº¡ng thÃ¡i. Frontend khÃ´ng phÃ¢n biá»‡t
//    Ä‘Æ°á»£c "phiÃªn háº¿t háº¡n" vá»›i "mÃ¡y chá»§ há»ng", nÃªn khÃ´ng biáº¿t pháº£i Ä‘Æ°a ngÆ°á»i dÃ¹ng vá» mÃ n
//    Ä‘Äƒng nháº­p; console thÃ¬ hiá»‡n lá»—i CORS, vÃ  ngÆ°á»i ta Ä‘i sá»­a CORS trong khi chuyá»‡n tháº­t
//    chá»‰ lÃ  token háº¿t háº¡n.
//
//    Äáº·t trÆ°á»›c thÃ¬ middleware CORS gáº¯n header qua OnStarting, nÃªn header dÃ­nh vÃ o cáº£
//    nhá»¯ng pháº£n há»“i do táº§ng dÆ°á»›i cáº¯t ngang.
app.UseCors(CorsSetup.PolicyName);

// â‘¥ "Anh lÃ  ai" â€” Ä‘á»c vÃ  xÃ¡c minh token, dá»±ng ClaimsPrincipal.
app.UseAuthentication();

// â‘¦ "Anh Ä‘Æ°á»£c lÃ m gÃ¬" â€” báº¯t buá»™c SAU â‘¥. Äáº£o láº¡i thÃ¬ lÃºc kiá»ƒm quyá»n chÆ°a cÃ³ ai Ä‘á»ƒ kiá»ƒm,
//    vÃ  má»i endpoint cÃ³ [Authorize] Ä‘á»u tráº£ 401 ká»ƒ cáº£ khi token hoÃ n toÃ n há»£p lá»‡.
app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>
/// Má»Ÿ <c>Program</c> ra cho assembly test nhÃ¬n tháº¥y.
///
/// ChÆ°Æ¡ng trÃ¬nh viáº¿t báº±ng cÃ¢u lá»‡nh cáº¥p cao nháº¥t váº«n sinh ra má»™t lá»›p <c>Program</c>, nhÆ°ng
/// nÃ³ lÃ  <c>internal</c> â€” mÃ  <c>WebApplicationFactory&lt;T&gt;</c> Ä‘Ã²i má»™t kiá»ƒu cÃ´ng khai
/// Ä‘á»ƒ biáº¿t pháº£i khá»Ÿi Ä‘á»™ng cÃ¡i gÃ¬. Má»™t dÃ²ng nÃ y lÃ  toÃ n bá»™ cÃ¡i giÃ¡ pháº£i tráº£ Ä‘á»ƒ test dá»±ng
/// Ä‘Æ°á»£c Ä‘Ãºng mÃ¡y chá»§ tháº­t thay vÃ¬ má»™t báº£n dá»±ng láº¡i gáº§n giá»‘ng.
/// </summary>
public partial class Program;
