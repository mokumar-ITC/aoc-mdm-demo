using AOC_MDM.Core.Entities;
using AOC_MDM.Core.Interfaces;
using AOC_MDM.Infrastrucure;
using AOC_MDM.Infrastrucure.Repository;
using AOC_MDM.Infrastrucure.UnitOfWork;
using AOC_MDM.Services.AutoMapperProfile;
using AOC_MDM.Services.Interfaces;
using AOC_MDM.Services.Service;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NuGet.Protocol.Core.Types;
using System.Collections.Generic;
using Serilog;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authentication.Cookies;
using AOC_MDM_MetaData_GUI.Models;


var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration) // Reads configuration from appsettings.json
    .Enrich.FromLogContext()
    .WriteTo.Console() // Optional: Log to console during development
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day) // Log to file
    .CreateLogger();

builder.Host.UseSerilog(); // Replace default logger with Serilog

//// Configure DbContext with ODBC connection
//string dsn = "dsn=aoc_sandbox"; // Replace with your actual DSN
//builder.Services.AddDbContext<AOCMDMDbContext>(options =>
//    options.UseJetOdbc(dsn));

// Add services to the container.
builder.Services.AddControllersWithViews();


var connectionString = builder.Configuration.GetConnectionString("DatabricksConnection");


// Retrieve the catalog name from appsettings.json
string catalog = builder.Configuration.GetSection("DatabaseSettings:Catalog").Value;

builder.Services.AddSingleton(new DatabaseSettings { Catalog = catalog });

// Register the UnitOfWork and Service
// Scoped: one ODBC connection per request (a singleton shared one connection across all concurrent users)
builder.Services.AddScoped<IUnitOfWork>(provider => new UnitOfWork(connectionString,catalog));
builder.Services.AddScoped<IMDMSpendAreaService, MDMSpendAreaService>();
builder.Services.AddScoped<IMDMVendorRefAttriService, MDMVendorRefAttriService>();
builder.Services.AddScoped<IMDMAOCLevel1Service, MDMAOCLevel1Service>();
builder.Services.AddScoped<IMDMAOCLevel2Service, MDMAOCLevel2Service>();
builder.Services.AddScoped<IMDMAOCLevel3Service, MDMAOCLevel3Service>();
builder.Services.AddScoped<IMDMTeamService, MDMTeamService>();
builder.Services.AddScoped<IMDMViewVendorRefAttrsService, MDMViewVendorRefAttrsService>();
builder.Services.AddScoped<MDMViewItemRefAttrsService, MDMViewItemRefAttrsService>();
builder.Services.AddScoped<IMDMItemRefAttrsService, MDMItemRefAttriService>();
builder.Services.AddScoped<IMDMViewItemSupplierRefAttrsService, MDMViewItemSupplierRefAttrsService>();
builder.Services.AddScoped<IMDMItemSupplierRefAttriService, MDMItemSupplierRefAttriService>();

//builder.Services.AddScoped<IMDMViewAOCLevel1Service, MDMViewAOCLevel1Service>();



builder.Services.AddScoped<IUserRoleService, UserRoleService>();
builder.Services.AddMemoryCache(); //Memory Cache registered
builder.Services.AddScoped<RoleCacheHelper>();

//Register configuration from appsettings. 
builder.Services.Configure<UserRoleSettings>(
    builder.Configuration.GetSection("UserRoles"));

var config = new MapperConfiguration(cfg =>
{
    cfg.AddProfile(new AutoMapperProfile());
});

var mapper = config.CreateMapper();

builder.Services.AddSingleton(mapper);

builder.Services.AddScoped<IUserAccountService, UserAccountService>();
builder.Services.AddScoped<IHierarchyService, HierarchyService>();

// Authentication:Mode = "Forms" (login page, users in intranet.user_access) or "Windows" (Negotiate / SSO)
if (string.Equals(builder.Configuration["Authentication:Mode"], "Windows", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
        .AddNegotiate();
}
else
{
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
            options.AccessDeniedPath = "/Error/AccessDenied";
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
        });
}
builder.Services.AddAuthorization();


var app = builder.Build();

app.UseSerilogRequestLogging(); // Enable Serilog request logging middleware


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    //app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();//Windows authentication
app.UseAuthorization();

//app.UseExceptionHandler("/Error/Handle"); // Redirect to a custom error handling action

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=SpendArea}/{action=Index}");      //"{controller=Home}/{action=Index}/{id?}");

app.Run();
