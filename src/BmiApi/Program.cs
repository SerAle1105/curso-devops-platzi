using System.Reflection;
using BmiApi.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "API de Índice de Masa Corporal",
        Description =
            "Calcula el Índice de Masa Corporal (IMC = peso / estatura²) y lo clasifica " +
            "según las categorías de la Organización Mundial de la Salud.",
        Contact = new OpenApiContact
        {
            Name = "Curso de DevOps - Platzi",
            Url = new Uri("https://github.com/SerAle1105/curso-devops-platzi"),
        },
        License = new OpenApiLicense { Name = "MIT" },
    });

    // Incorpora los comentarios /// del código a la documentación.
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
});

// Servicio de dominio que calcula el IMC.
builder.Services.AddSingleton<IBmiCalculator, BmiCalculator>();

var app = builder.Build();

// Swagger queda disponible en todos los entornos: es una API de demostración del curso.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "API de IMC v1");
    options.DocumentTitle = "API de IMC - Documentación";
    // La UI queda en la raíz: http://localhost:5290/
    options.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Expuesta para que WebApplicationFactory<Program> pueda levantar la API en las pruebas.
public partial class Program { }
