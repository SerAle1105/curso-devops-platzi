# Contenerizar la API de IMC: del Dockerfile a la prueba en el navegador

> Guía de la **clase 9**. Explica cómo se construyó el Dockerfile de este repositorio,
> cómo generar la imagen en tu equipo, cómo probarla y cómo exponerla en otro puerto.
>
> Requisito previo: tener Docker funcionando.
> Si aún no lo instalas, sigue [Instalación de Docker Desktop en Windows 10 Home](instalacion-docker-windows.md).

---

## 1. Dónde están los archivos

```
curso-devops-platzi/
└── src/
    └── BmiApi/
        ├── Dockerfile        ← la receta de la imagen
        ├── .dockerignore     ← qué NO copiar dentro
        ├── BmiApi.csproj
        └── Program.cs
```

Los dos archivos viven **junto al `.csproj`**, no en la raíz del repositorio. Eso importa y se
explica en la sección 3.

---

## 2. El Dockerfile explicado línea por línea

Este es el archivo completo, tal como está en [src/BmiApi/Dockerfile](../src/BmiApi/Dockerfile):

```docker
# ---------- Etapa 1: compilar ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY BmiApi.csproj .
RUN dotnet restore

COPY . .

RUN dotnet publish -c Release -o /app --no-restore

# ---------- Etapa 2: ejecutar ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app .

EXPOSE 8080

ENTRYPOINT ["dotnet", "BmiApi.dll"]
```

### La idea central: dos imágenes, no una

Esto se llama **build multi-etapa** (*multi-stage build*) y es lo más importante de entender.

| Etapa | Imagen base | Para qué sirve | Tamaño aprox. |
| --- | --- | --- | --- |
| 1 — `build` | `dotnet/sdk:8.0` | Compilar. Trae el compilador de C#, MSBuild y NuGet. | ~800 MB |
| 2 — final | `dotnet/aspnet:8.0` | Solo ejecutar. No sabe compilar. | ~220 MB |

La etapa 1 es el **taller**: se ensucia, usa herramientas pesadas y al final se descarta.
La etapa 2 es la **vitrina**: recibe únicamente el producto terminado.

La imagen que queda en tu equipo es la de la etapa 2. Todo lo de la etapa 1 desaparece.
Resultado: una imagen unas 4 veces más liviana que **no contiene tu código fuente**, solo los
DLL compilados.

### Instrucción por instrucción

```docker
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
```

Parte de la imagen oficial del SDK de .NET 8. El `AS build` le pone nombre a esta etapa para
poder referenciarla después.

```docker
WORKDIR /src
```

Crea la carpeta `/src` dentro del contenedor y se para ahí. Equivale a un `mkdir` seguido de
un `cd`. Todo lo que siga es relativo a esa ruta.

```docker
COPY BmiApi.csproj .
RUN dotnet restore
```

**Copia primero SOLO el `.csproj`**, y con eso restaura las dependencias.

Parece un rodeo innecesario, pero es la optimización más valiosa del archivo. Docker guarda en
caché el resultado de cada instrucción, y **si una instrucción cambia, esa y todas las
siguientes se vuelven a ejecutar**.

- Si copiaras todo el código de una vez, cualquier cambio en un `.cs` invalidaría la caché y
  `dotnet restore` volvería a descargar todos los paquetes de NuGet **en cada build**.
- Al separarlo, `restore` solo se re-ejecuta cuando cambian las dependencias declaradas en el
  `.csproj`.

En la práctica: builds de segundos en lugar de minutos.

```docker
COPY . .
```

Ahora sí, el resto del código fuente. Copia todo el contexto de build (ver sección 3),
respetando lo que excluye el `.dockerignore`.

```docker
RUN dotnet publish -c Release -o /app --no-restore
```

Compila en modo Release y deja el resultado listo para producción en `/app`.

- `-c Release` — optimizado, sin símbolos de depuración.
- `-o /app` — carpeta de salida.
- `--no-restore` — no repitas el restore, ya se hizo en una capa anterior.

> **Diferencia con el material de la clase.** El material original traía dos líneas:
>
> ```docker
> RUN dotnet build "ApiContactos.csproj" -c Release -o /app/build   ← sobra
> RUN dotnet publish -c release -o /app
> ```
>
> `dotnet publish` ya compila internamente. Ese `dotnet build` compila todo una primera vez
> para nada: alarga el build y no aporta al resultado final. Por eso aquí solo está `publish`.

```docker
FROM mcr.microsoft.com/dotnet/aspnet:8.0
```

**Empieza la segunda etapa.** Un nuevo `FROM` arranca de cero: se descarta todo lo anterior
salvo lo que copies explícitamente.

```docker
COPY --from=build /app .
```

La instrucción clave. El `--from=build` va a la etapa que llamamos `build` y trae **solo la
carpeta `/app`** — los DLL publicados. No viaja el código fuente, ni el compilador, ni la
caché de NuGet.

```docker
EXPOSE 8080
```

**Es solo documentación.** No abre ni publica nada. Le indica a quien lea el Dockerfile (y a
algunas herramientas) que la aplicación escucha en ese puerto. Quien realmente publica el
puerto es el `-p` del `docker run`, que veremos en la sección 6.

```docker
ENTRYPOINT ["dotnet", "BmiApi.dll"]
```

El comando que se ejecuta al arrancar el contenedor. Equivale a `dotnet BmiApi.dll` dentro
de `/app`.

### Por qué el puerto es 8080

Las imágenes de .NET **8** definen la variable `ASPNETCORE_HTTP_PORTS=8080`, así que la app
escucha ahí por defecto.

En .NET 6 y 7 el puerto por defecto era el **80**. Si ves un tutorial con `-p 8080:80`, es de
una versión anterior.

---

## 3. El `.dockerignore` y el contexto de build

Cuando ejecutas `docker build`, lo primero que hace Docker es **empaquetar toda la carpeta que
le indicas y enviarla al motor**. Esa carpeta se llama *contexto de build*.

El [.dockerignore](../src/BmiApi/.dockerignore) excluye del contexto:

```
bin/
obj/
Dockerfile
.dockerignore
```

Excluir `bin/` y `obj/` **no es cosmético**. Sin esa exclusión, el `COPY . .` mete dentro de la
imagen los binarios que compilaste en Windows, que se mezclan con los que se compilan en Linux
dentro del contenedor. El síntoma son errores de arranque muy difíciles de diagnosticar. Es el
error número uno al contenerizar .NET.

Como el Dockerfile está dentro de `src/BmiApi/`, el contexto es esa carpeta. Ventaja adicional:
el proyecto `BmiApi.Tests/` queda fuera por completo y las pruebas no viajan a la imagen.

---

## 4. Construir la imagen

Desde la **raíz del repositorio**:

```bash
docker build -t sgarcia/bmiapi src/BmiApi
```

Anatomía del comando:

| Parte | Qué significa |
| --- | --- |
| `docker build` | Construye una imagen a partir de un Dockerfile. |
| `-t sgarcia/bmiapi` | *Tag*: el nombre con el que quedará la imagen. |
| `src/BmiApi` | **El contexto de build.** La carpeta que se empaqueta y envía. |

Ese último argumento no es "dónde está el Dockerfile" sino "cuál es la raíz del build". Docker
busca el archivo `Dockerfile` ahí dentro por convención.

Si quisieras ponerle una versión:

```bash
docker build -t sgarcia/bmiapi:1.0 src/BmiApi
```

Sin `:algo`, Docker asume `:latest`.

### Qué vas a ver

La primera vez descarga las dos imágenes base (~1 GB en total) y puede tardar varios minutos.
Verás una línea por cada instrucción del Dockerfile:

```
 => [build 1/6] FROM mcr.microsoft.com/dotnet/sdk:8.0
 => [build 3/6] COPY BmiApi.csproj .
 => [build 4/6] RUN dotnet restore
 ...
 => => naming to docker.io/sgarcia/bmiapi
```

A partir del segundo build, las instrucciones que no cambiaron aparecen como `CACHED` y tardan
milisegundos.

### Verificar que quedó

```bash
docker images
```

Deberías ver `sgarcia/bmiapi` con su tamaño. Si el multi-etapa funcionó, ronda los
**230–250 MB**, no los 800 MB del SDK.

---

## 5. Ejecutar y probar

```bash
docker run -it --rm --name bmiapi -p 8080:8080 sgarcia/bmiapi
```

Anatomía:

| Bandera | Qué hace |
| --- | --- |
| `-it` | Modo interactivo con terminal: verás los logs de la API en vivo. |
| `--rm` | Borra el contenedor automáticamente al detenerlo. Evita acumular basura. |
| `--name bmiapi` | Le pone nombre, para poder referenciarlo en otros comandos. |
| `-p 8080:8080` | **Publica el puerto.** Ver sección 6. |
| `sgarcia/bmiapi` | La imagen a ejecutar. |

Cuando arranque verás en la terminal algo como:

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://[::]:8080
```

### Probar

En el navegador:

| Qué | URL |
| --- | --- |
| Swagger UI | <http://localhost:8080/> |
| Cálculo del IMC | <http://localhost:8080/api/bmi?weightKg=70&heightM=1.75> |

Desde PowerShell:

```powershell
Invoke-RestMethod "http://localhost:8080/api/bmi?weightKg=70&heightM=1.75"
```

O con curl. En PowerShell hay que escribir `curl.exe`, porque `curl` a secas es un alias de
`Invoke-WebRequest` y no acepta las mismas banderas:

```powershell
curl.exe "http://localhost:8080/api/bmi?weightKg=70&heightM=1.75"
```

Respuesta esperada:

```json
{"weightKg":70,"heightM":1.75,"bmi":22.86,"category":"Peso normal"}
```

Y el POST:

```powershell
curl.exe -X POST http://localhost:8080/api/bmi -H "Content-Type: application/json" -d "{\"weightKg\":70,\"heightM\":1.75}"
```

### Detener

`Ctrl+C` en la terminal donde corre. Como usaste `--rm`, el contenedor se borra solo.

### Ejecutarlo en segundo plano

Si prefieres que no te ocupe la terminal, cambia `-it` por `-d` (*detached*):

```bash
docker run -d --rm --name bmiapi -p 8080:8080 sgarcia/bmiapi

docker logs -f bmiapi     # ver los logs
docker stop bmiapi        # detenerlo
```

---

## 6. Exponerlo en otro puerto

Aquí está la parte que suele confundir. La bandera `-p` siempre tiene **dos números**:

```
-p 8080:8080
   ↑    ↑
   │    └── puerto DENTRO del contenedor (donde escucha la app)
   └─────── puerto en TU EQUIPO (por donde entras desde el navegador)
```

Se lee de izquierda a derecha: *"el tráfico que llegue a este puerto de mi máquina, mándalo a
este otro puerto del contenedor"*.

### Caso A — Cambiar solo el puerto de tu equipo (lo habitual)

Es lo que necesitas el 95 % de las veces: el 8080 está ocupado por otra cosa y quieres entrar
por el 9000.

```bash
docker run -it --rm --name bmiapi -p 9000:8080 sgarcia/bmiapi
```

Ahora entras por <http://localhost:9000/>.

**Dentro del contenedor la app sigue escuchando en el 8080** y no se entera de nada. No hay que
reconstruir la imagen ni cambiar el código: es puro redireccionamiento. Los logs seguirán
diciendo `Now listening on: http://[::]:8080`, y está bien.

Otros ejemplos válidos:

```bash
-p 5000:8080     # entras por localhost:5000
-p 80:8080       # entras por localhost, sin puerto en la URL
-p 3000:8080     # entras por localhost:3000
```

### Caso B — Cambiar el puerto interno de la aplicación

Si de verdad quieres que la app escuche en otro puerto dentro del contenedor, hay que decírselo
a ASP.NET con una variable de entorno, usando `-e`:

```bash
docker run -it --rm --name bmiapi -e ASPNETCORE_HTTP_PORTS=5000 -p 8080:5000 sgarcia/bmiapi
```

Léelo así:

1. `-e ASPNETCORE_HTTP_PORTS=5000` → la app ahora escucha en el 5000 **dentro** del contenedor.
2. `-p 8080:5000` → lo que llegue al 8080 de tu equipo va al 5000 del contenedor.

Los logs ahora dirán `Now listening on: http://[::]:5000`.

> **El error clásico:** cambiar la variable pero dejar `-p 8080:8080`. El contenedor arranca sin
> errores, pero el navegador no responde: Docker está enviando el tráfico al 8080 interno, donde
> ya no hay nadie escuchando. **Los dos números tienen que coincidir con la realidad.**

Alternativa equivalente, más explícita:

```bash
docker run -it --rm -e ASPNETCORE_URLS=http://+:5000 -p 8080:5000 sgarcia/bmiapi
```

El `+` significa "todas las interfaces de red". Es importante: si pusieras
`http://localhost:5000`, la app solo aceptaría conexiones desde dentro del contenedor y desde
afuera parecería caída.

### Caso C — Dejarlo fijo en el Dockerfile

Si quieres que el puerto interno sea otro **siempre**, sin repetir la variable en cada
`docker run`, agrégala al Dockerfile en la segunda etapa:

```docker
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_HTTP_PORTS=5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "BmiApi.dll"]
```

Requiere reconstruir la imagen. Recuerda actualizar también el `EXPOSE` para que la
documentación no mienta.

### Caso D — Que Docker elija un puerto libre

Si no te importa cuál sea, deja el lado del host vacío:

```bash
docker run -d --rm --name bmiapi -p 8080 sgarcia/bmiapi
docker port bmiapi
```

Docker asigna un puerto alto al azar y `docker port` te dice cuál tocó.

### Tabla resumen

| Quiero... | Comando | Entro por |
| --- | --- | --- |
| Lo estándar | `-p 8080:8080` | `localhost:8080` |
| Otro puerto en mi PC | `-p 9000:8080` | `localhost:9000` |
| Sin puerto en la URL | `-p 80:8080` | `localhost` |
| Cambiar el puerto interno | `-e ASPNETCORE_HTTP_PORTS=5000 -p 8080:5000` | `localhost:8080` |
| Dos instancias a la vez | `-p 8080:8080` y `-p 8081:8080` | `localhost:8080` y `localhost:8081` |

Ese último caso muestra por qué el diseño tiene sentido: la misma imagen, sin modificar, puede
correr muchas veces en paralelo; solo cambia el puerto del lado izquierdo.

---

## 7. Comandos útiles

```bash
docker images                    # imágenes en tu equipo
docker ps                        # contenedores corriendo
docker ps -a                     # incluidos los detenidos
docker logs -f bmiapi            # seguir los logs
docker exec -it bmiapi bash      # entrar a la terminal del contenedor
docker stop bmiapi               # detener
docker rm bmiapi                 # borrar el contenedor (si no usaste --rm)
docker rmi sgarcia/bmiapi        # borrar la imagen
docker system prune              # limpiar todo lo que no se usa
```

Para inspeccionar la imagen por dentro sin arrancar la API:

```bash
docker run -it --rm --entrypoint bash sgarcia/bmiapi
ls -la          # deberías ver BmiApi.dll y sus dependencias
```

---

## 8. Solución de problemas

| Síntoma | Causa | Solución |
| --- | --- | --- |
| `Bind for 0.0.0.0:8080 failed: port is already allocated` | Otro proceso o contenedor usa el 8080 | Usa otro puerto: `-p 8081:8080` |
| `The container name "/bmiapi" is already in use` | Quedó un contenedor con ese nombre | `docker rm bmiapi` o usa otro `--name` |
| El contenedor arranca pero el navegador no responde | Los dos números del `-p` no coinciden | Revisa la sección 6, caso B |
| `COPY failed: BmiApi.csproj not found` | El contexto de build está mal | El último argumento debe ser `src/BmiApi` |
| El build tarda muchísimo cada vez | Se está invalidando la caché | Verifica que el `.dockerignore` excluya `bin/` y `obj/` |
| Errores raros de .NET al arrancar | Se colaron los binarios de Windows | Falta el `.dockerignore` (sección 3) |
| `error during connect... docker daemon` | Docker Desktop está cerrado | Ábrelo y espera a "Engine running" |

---

## Referencias

- [src/BmiApi/Dockerfile](../src/BmiApi/Dockerfile) — el archivo comentado.
- [src/README.md](../src/README.md) — versión corta de estos comandos.
- [Instalación de Docker Desktop](instalacion-docker-windows.md) — requisito previo.
- [Documentación oficial de .NET en contenedores](https://learn.microsoft.com/dotnet/core/docker/build-container)
