using DotNetEnv;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Додати налаштування із змінних оточення
builder.Configuration.AddEnvironmentVariables();

// Отримання порту з змінної оточення PORT та налаштування URL для Kestrel
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://*:{port}");
}

// Add services to the container.
builder.Services.AddControllersWithViews();
//.AddNewtonsoftJson();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // За замовчуванням HSTS має 30 днів, його можна налаштувати для продакшну.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
