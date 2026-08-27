using BmiApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Servicio de dominio que calcula el IMC.
builder.Services.AddSingleton<IBmiCalculator, BmiCalculator>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Expuesta para que WebApplicationFactory<Program> pueda levantar la API en las pruebas.
public partial class Program { }
