using Microsoft.AspNetCore.Authentication.Cookies;
using RiceMillProject.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("GatemanAccess", policy => policy.RequireAuthenticatedUser()
        .RequireAssertion(context => context.User.IsGatemanUser() || context.User.IsAdminUser()));
    options.AddPolicy("WeightmanAccess", policy => policy.RequireAuthenticatedUser()
        .RequireAssertion(context => context.User.IsWeightmanUser() || context.User.IsAdminUser()));
    options.AddPolicy("SupervisorAccess", policy => policy.RequireAuthenticatedUser()
        .RequireAssertion(context => context.User.IsSupervisorUser() || context.User.IsAdminUser()));
    options.AddPolicy("LabAccess", policy => policy.RequireAuthenticatedUser()
        .RequireAssertion(context => context.User.IsLabUser() || context.User.IsAdminUser()));
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

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
