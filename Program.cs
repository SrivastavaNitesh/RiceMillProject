using Microsoft.AspNetCore.Authentication.Cookies;
using RiceMillProject.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options => { options.Filters.Add<RecordWorkflowFilter>(); options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute()); });
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("BillingAccess", policy => policy.RequireAuthenticatedUser().RequireAssertion(context => context.User.IsAdminUser()));
    options.AddPolicy("GatemanAccess", policy => policy.RequireAuthenticatedUser()
        .RequireAssertion(context => context.User.IsGatemanUser() || context.User.IsAdminUser()));
    options.AddPolicy("WeightmanAccess", policy => policy.RequireAuthenticatedUser()
        .RequireAssertion(context => context.User.IsWeightmanUser() || context.User.IsAdminUser()));
    options.AddPolicy("SupervisorAccess", policy => policy.RequireAuthenticatedUser()
        .RequireAssertion(context => context.User.IsSupervisorUser() || context.User.IsAdminUser()));
    options.AddPolicy("LabAccess", policy => policy.RequireAuthenticatedUser()
        .RequireAssertion(context =>
            int.TryParse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId)
            && int.TryParse(context.User.FindFirst("PersonId")?.Value, out var personId)
            && new RiceMillProject.DAL.LabWorkflowDAL(builder.Configuration).CanAccess(userId, personId)));
    options.AddPolicy(
    "MethAccess",
    policy => policy
        .RequireAuthenticatedUser()
        .RequireAssertion(context =>
            context.User.IsMethUser() ||
            context.User.IsAdminUser()
        )
);
});

// Add Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = System.TimeSpan.FromHours(8);
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler("/Home/Error");

if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.Use(async (context, next) =>
{
    RiceMillProject.DAL.RecordEditContext.UserId = int.TryParse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var actorId) && actorId > 0 ? actorId : null;
    try { await next(); } finally { RiceMillProject.DAL.RecordEditContext.UserId = null; }
});
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
