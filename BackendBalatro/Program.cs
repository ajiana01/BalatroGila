var builder = WebApplication.CreateBuilder(args);

// Mendaftarkan service Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 1. TAMBAHKAN KEBIJAKAN CORS
// Mengizinkan aplikasi dari port lain (React) untuk mengakses API ini
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173") // Port default Vite/React
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

var app = builder.Build();

// 2. AKTIFKAN CORS DI PIPELINE
app.UseCors("AllowReactApp");

// 3. BUAT ENDPOINT (Jembatan Data)
// Ketika React mengakses URL "/api/status", .NET akan membalas dengan JSON ini
app.MapGet("/api/status", () =>
{
    return Results.Ok(new
    {
        Message = "Koneksi ke .NET berhasil!",
        Timestamp = DateTime.UtcNow,
        Server = "Windows"
    });
});

// Mengaktifkan middleware Swagger (biasanya hanya di mode Development)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();

public partial class Program { }