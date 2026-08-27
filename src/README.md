# API de Índice de Masa Corporal (.NET 8)

Solución de ejemplo para el curso de DevOps: una API REST que calcula el IMC y lo clasifica
según las categorías de la OMS, con su suite de pruebas.

## Estructura

| Proyecto | Descripción |
| --- | --- |
| `BmiApi` | Web API en .NET 8 (controllers + Swagger). |
| `BmiApi.Tests` | Pruebas unitarias (xUnit) y de integración (`WebApplicationFactory`). |

## Comandos

```bash
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
dotnet run --project BmiApi        # Swagger UI en http://localhost:5290/
```

## Swagger / OpenAPI

La documentación interactiva se sirve en la **raíz** de la aplicación (`/`) en todos los
entornos, y el contrato en `/swagger/v1/swagger.json`.

| Recurso | URL local |
| --- | --- |
| Swagger UI | <http://localhost:5290/> |
| Contrato OpenAPI | <http://localhost:5290/swagger/v1/swagger.json> |

El contrato se genera a partir de los comentarios `///` del código (`GenerateDocumentationFile`),
así que las descripciones y los ejemplos de Swagger salen del propio código fuente.
Hay una copia versionada del contrato en [docs/swagger.json](../docs/swagger.json).

## Endpoints

### `POST /api/bmi`

```json
{ "weightKg": 70, "heightM": 1.75 }
```

### `GET /api/bmi?weightKg=70&heightM=1.75`

Ambos devuelven:

```json
{
  "weightKg": 70,
  "heightM": 1.75,
  "bmi": 22.86,
  "category": "Peso normal"
}
```

Los datos fuera de rango (peso 1–500 kg, estatura 0.5–2.5 m) devuelven `400 Bad Request`
con un `ValidationProblemDetails`.

## Clasificación (OMS)

| IMC | Categoría |
| --- | --- |
| < 18.5 | Bajo peso |
| 18.5 – 24.9 | Peso normal |
| 25 – 29.9 | Sobrepeso |
| 30 – 34.9 | Obesidad grado I |
| 35 – 39.9 | Obesidad grado II |
| >= 40 | Obesidad grado III |
