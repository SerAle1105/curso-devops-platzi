# HU-001 — Cálculo del Índice de Masa Corporal

| Campo | Valor |
| --- | --- |
| **ID** | HU-001 |
| **Épica** | Servicios de salud y bienestar |
| **Estado** | Terminada |
| **Rama** | `Config/Workflow260826` |
| **Fecha** | 2026-08-26 |
| **Autor** | Sergio Garcia |

## Historia

> **Como** consumidor de los servicios de salud (aplicación web o móvil),
> **quiero** enviar el peso y la estatura de una persona a una API
> **para** obtener su Índice de Masa Corporal y la categoría de riesgo correspondiente,
> sin tener que replicar la fórmula ni la tabla de clasificación en cada cliente.

## Contexto

El IMC se calcula como `peso (kg) / estatura (m)²` y se clasifica según las categorías de la
Organización Mundial de la Salud. Centralizar el cálculo en un servicio evita que cada cliente
implemente su propia versión de la tabla y permite ajustar los rangos en un solo lugar.

## Criterios de aceptación

### CA-1 — Cálculo con datos válidos (POST)

```gherkin
Dado que envío una petición POST a /api/bmi
  con el cuerpo { "weightKg": 70, "heightM": 1.75 }
Cuando el servicio procesa la petición
Entonces recibo un 200 OK
  Y el cuerpo contiene "bmi": 22.86
  Y el cuerpo contiene "category": "Peso normal"
  Y el cuerpo repite el peso y la estatura recibidos
```

### CA-2 — Cálculo con datos válidos (GET)

```gherkin
Dado que envío una petición GET a /api/bmi?weightKg=95&heightM=1.75
Cuando el servicio procesa la petición
Entonces recibo un 200 OK
  Y el cuerpo contiene "bmi": 31.02
  Y el cuerpo contiene "category": "Obesidad grado I"
```

### CA-3 — El IMC se redondea a dos decimales

```gherkin
Dado cualquier par de peso y estatura válidos
Cuando el servicio calcula el IMC
Entonces el valor devuelto tiene como máximo dos decimales
  Y el redondeo se hace alejándose de cero en los empates
```

### CA-4 — Clasificación según la OMS

```gherkin
Dado un IMC calculado
Cuando el servicio lo clasifica
Entonces aplica exactamente estos rangos:
  | IMC          | Categoría           |
  | < 18.5       | Bajo peso           |
  | 18.5 – 24.99 | Peso normal         |
  | 25 – 29.99   | Sobrepeso           |
  | 30 – 34.99   | Obesidad grado I    |
  | 35 – 39.99   | Obesidad grado II   |
  | >= 40        | Obesidad grado III  |
```

### CA-5 — Rechazo de datos fuera de rango

```gherkin
Dado que envío un peso fuera del rango 1–500 kg
  o una estatura fuera del rango 0.5–2.5 m
Cuando el servicio valida la petición
Entonces recibo un 400 Bad Request
  Y el cuerpo es un ValidationProblemDetails con al menos un error
  Y el mensaje de error indica qué campo es inválido
```

Aplica tanto al `POST` como al `GET`.

### CA-6 — Petición malformada

```gherkin
Dado que envío una petición POST a /api/bmi sin cuerpo
Cuando el servicio la procesa
Entonces recibo un 400 Bad Request
```

### CA-7 — Documentación interactiva

```gherkin
Dado que ejecuto la API en cualquier entorno
Cuando abro la raíz de la aplicación
Entonces veo la Swagger UI con ambos endpoints documentados
  Y el contrato está disponible en /swagger/v1/swagger.json
  Y las descripciones y ejemplos provienen de los comentarios /// del código
```

## Solución implementada

Proyecto en .NET 8 dentro de [src/](../src/), siguiendo la ruta que ya esperaba el workflow
de CI (`working-directory: src`).

| Componente | Archivo | Responsabilidad |
| --- | --- | --- |
| Entrada | [BmiRequest.cs](../src/BmiApi/Models/BmiRequest.cs) | Modelo con las validaciones de rango (`DataAnnotations`). |
| Salida | [BmiResponse.cs](../src/BmiApi/Models/BmiResponse.cs) | `record` con peso, estatura, IMC y categoría. |
| Dominio | [BmiCalculator.cs](../src/BmiApi/Services/BmiCalculator.cs) | Fórmula, redondeo y tabla de la OMS. Sin dependencias de ASP.NET. |
| Contrato | [IBmiCalculator.cs](../src/BmiApi/Services/IBmiCalculator.cs) | Abstracción registrada en el contenedor de DI. |
| Exposición | [BmiController.cs](../src/BmiApi/Controllers/BmiController.cs) | Endpoints `POST` y `GET`, validación y códigos de respuesta. |
| Arranque | [Program.cs](../src/BmiApi/Program.cs) | Controllers, registro de `IBmiCalculator` y configuración de Swagger. |
| Contrato | [docs/swagger.json](swagger.json) | Copia versionada del OpenAPI generado por la API. |

El cálculo vive en una clase de dominio sin dependencias del framework, de modo que se puede
probar en aislamiento y reutilizar desde otro tipo de host si más adelante hace falta.

## Pruebas

45 casos, todos en verde con `dotnet test --configuration Release`.

| Suite | Archivo | Qué cubre |
| --- | --- | --- |
| Unitarias | [BmiCalculatorTests.cs](../src/BmiApi.Tests/BmiCalculatorTests.cs) | Fórmula y redondeo (CA-1, CA-3), cada límite de las seis categorías incluyendo 18.49/18.5, 24.99/25, 29.99/30, 34.99/35, 39.99/40 (CA-4), y entradas inválidas: cero, negativos, `NaN` e infinito. |
| Integración | [BmiControllerTests.cs](../src/BmiApi.Tests/BmiControllerTests.cs) | API levantada en memoria con `WebApplicationFactory`: respuestas 200 de ambos endpoints (CA-1, CA-2), 400 por rango (CA-5) y 400 por cuerpo vacío (CA-6). |
| Contrato | [SwaggerTests.cs](../src/BmiApi.Tests/SwaggerTests.cs) | El `swagger.json` se genera, describe el `GET` y el `POST`, expone título y versión, incluye descripciones y ejemplos en los esquemas, y la UI responde en la raíz (CA-7). |

## Definición de terminado

- [x] La solución compila en Release sin advertencias.
- [x] Las 45 pruebas pasan.
- [x] Los criterios CA-1 a CA-7 están cubiertos por pruebas automatizadas.
- [x] La API está documentada en Swagger y en [src/README.md](../src/README.md).
- [x] `bin/` y `obj/` quedan fuera del control de versiones vía `.gitignore`.
- [x] El contrato OpenAPI está versionado en [docs/swagger.json](swagger.json).

## Pendientes / notas

- El workflow [pull_request_review.yml](../workflows/pull_request_review.yml) está en `workflows/`
  y no en `.github/workflows/`, así que GitHub Actions todavía no lo ejecuta en los PR.
  Mover el archivo es requisito para que estas pruebas corran en CI.
- El IMC no distingue masa muscular de masa grasa ni ajusta por edad o sexo. Si el producto
  necesita esa precisión, corresponde a otra historia (p. ej. percentiles pediátricos).
