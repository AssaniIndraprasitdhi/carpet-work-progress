using Microsoft.EntityFrameworkCore;
using Carpet_Work_Progress.Data;
using Carpet_Work_Progress.Services;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

var connectionString = Environment.GetEnvironmentVariable("POSTGRES_CONN")
    ?? throw new InvalidOperationException("POSTGRES_CONN is not set in .env");

var uploadDir = Environment.GetEnvironmentVariable("UPLOAD_DIR") ?? "wwwroot/uploads";
Directory.CreateDirectory(uploadDir);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddSingleton<IImageAnalysisService, ImageAnalysisService>();

builder.Services.AddControllersWithViews();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
