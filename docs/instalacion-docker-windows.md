# Instalación de Docker Desktop en Windows 10 Home

> Manual de la **clase 9** del curso de DevOps.
> Diagnóstico levantado sobre el equipo de trabajo el **26 de agosto de 2026**.

## Diagnóstico del equipo

| Requisito | Estado |
| --- | --- |
| Windows 10 Home Single Language 22H2 (build 19045) | ✅ Soportado |
| Virtualización VT-x habilitada en BIOS | ✅ Ya activa — no hay que tocar la BIOS |
| SLAT (requerido por WSL 2) | ✅ Intel i5-1035G1 lo soporta |
| Disco libre en C: | ✅ 72.7 GB |
| RAM | ⚠️ 7.7 GB — funciona, pero justo (ver paso 5) |
| **WSL 2** | ❌ **No instalado** — el servicio `LxssManager` no existe |
| winget | ✅ v1.29.290, con `Docker.DockerDesktop` 4.88.1 disponible |

Windows **Home** no incluye Hyper-V, así que Docker Desktop usará el backend **WSL 2**.
No es un plan B: es el backend recomendado también en Windows Pro.

Comandos usados para levantar este diagnóstico, por si hay que repetirlo en otra máquina:

```powershell
Get-CimInstance Win32_OperatingSystem  | Select-Object Caption, Version, BuildNumber
Get-CimInstance Win32_Processor        | Select-Object Name, VirtualizationFirmwareEnabled, SecondLevelAddressTranslationExtensions
Get-Service LxssManager -ErrorAction SilentlyContinue
winget search --id Docker.DockerDesktop --exact
```

---

## Paso 1 — Instalar WSL 2

Abrir **PowerShell como Administrador** (clic derecho en Inicio → *Windows PowerShell (Administrador)*):

```powershell
wsl --install
```

Ese único comando hace cuatro cosas: habilita la característica WSL, habilita *Plataforma de
máquina virtual*, descarga el kernel de Linux e instala Ubuntu.

**Reiniciar el equipo.** No es opcional.

Al volver a encender se abre una ventana de Ubuntu pidiendo usuario y contraseña. Hay que
crearlos (la contraseña no se ve mientras se escribe, es normal). Ese usuario no tiene ninguna
relación con la cuenta de Windows.

<details>
<summary><b>Si <code>wsl --install</code> falla</b></summary>

En PowerShell como Administrador:

```powershell
dism.exe /online /enable-feature /featurename:Microsoft-Windows-Subsystem-Linux /all /norestart
dism.exe /online /enable-feature /featurename:VirtualMachinePlatform /all /norestart
```

Reiniciar, instalar el [kernel de WSL 2](https://wslstorestorage.blob.core.windows.net/wslblob/wsl_update_x64.msi)
y luego:

```powershell
wsl --set-default-version 2
wsl --install -d Ubuntu
```
</details>

**Verificar antes de continuar:**

```powershell
wsl -l -v
```

Debe listar `Ubuntu` con **VERSION 2**. Si dice `1`, corregir con `wsl --set-version Ubuntu 2`.

---

## Paso 2 — Instalar Docker Desktop

```powershell
winget install --id Docker.DockerDesktop --exact
```

Alternativa gráfica: <https://www.docker.com/products/docker-desktop/> → *Download for Windows*.
Durante la instalación hay que dejar marcado **"Use WSL 2 instead of Hyper-V"**.

Al terminar, **cerrar sesión de Windows y volver a entrar** (o reiniciar). El instalador agrega
el usuario al grupo `docker-users` y ese cambio no aplica hasta re-loguearse.

---

## Paso 3 — Primer arranque

Abrir **Docker Desktop** desde el menú Inicio. La primera vez:

1. Aceptar los términos de servicio.
2. Ofrece crear cuenta o iniciar sesión — **se puede saltar** con *Skip* / *Continue without
   signing in*. No hace falta cuenta para trabajar en local.
3. Esperar a que el ícono de la ballena en la bandeja del sistema deje de animarse y el panel
   diga **"Engine running"**. La primera vez tarda entre 1 y 3 minutos.

Docker Desktop tiene que estar **abierto y corriendo** para que el comando `docker` funcione.
Si se cierra, el motor se apaga.

---

## Paso 4 — Verificar

Abrir una **terminal nueva** (PowerShell normal, sin admin — el PATH solo se actualiza en
terminales nuevas):

```powershell
docker --version
docker run hello-world
```

El segundo comando descarga una imagen mínima y la ejecuta. Si aparece *"Hello from Docker!"*,
todo está funcionando.

---

## Paso 5 — Limitar la memoria (importante con 7.7 GB de RAM)

WSL 2 por defecto puede tomar hasta el 50–80 % de la RAM y no la devuelve. En un equipo de
8 GB eso se nota. Crear el archivo `C:\Users\Sergio Garcia\.wslconfig`:

```ini
[wsl2]
memory=3GB
processors=2
swap=2GB
```

Aplicar con `wsl --shutdown` y volviendo a abrir Docker Desktop. 3 GB alcanzan de sobra para
la API de .NET de este repositorio.

---

## Paso 6 — Probar con el proyecto del curso

Con Docker andando, desde la raíz del repositorio:

```bash
docker build -t sgarcia/bmiapi src/BmiApi
docker run -it --rm --name bmiapi -p 8080:8080 sgarcia/bmiapi
```

Abrir <http://localhost:8080/> para ver el Swagger corriendo dentro del contenedor.
Para detenerlo: `Ctrl+C`.

El Dockerfile está en [src/BmiApi/Dockerfile](../src/BmiApi/Dockerfile) y su explicación en
[src/README.md](../src/README.md#docker).

---

## Advertencias

### Versión de Docker Desktop y Windows 10

Windows 10 salió de soporte en octubre de 2025 y Docker ha ido retirando el soporte para esa
versión en sus releases recientes. **No está confirmado que la 4.88.1 instale sobre el build
19045.** Si el instalador rechaza el equipo por versión de Windows, descargar una release
anterior desde las [notas de versión de Docker Desktop](https://docs.docker.com/desktop/release-notes/),
que conservan los enlaces históricos.

### Licenciamiento

Docker Desktop es gratuito para uso personal, educativo y empresas pequeñas, pero
**requiere suscripción pagada en organizaciones de más de 250 empleados o USD 10 M de ingresos
anuales**. Para uso corporativo conviene confirmarlo con el área de TI.

Alternativas gratuitas sin esa restricción, que corren el mismo Dockerfile sin cambios:

- **Rancher Desktop** — <https://rancherdesktop.io/>
- **Podman Desktop** — <https://podman-desktop.io/>

---

## Solución de problemas

| Síntoma | Causa probable | Solución |
| --- | --- | --- |
| `docker: command not found` | Terminal abierta antes de instalar | Cerrar y abrir una terminal nueva |
| `error during connect... docker daemon is not running` | Docker Desktop cerrado | Abrirlo y esperar a "Engine running" |
| `WSL 2 installation is incomplete` | Falta el kernel | Instalar el MSI del kernel (ver paso 1) |
| Docker Desktop no arranca tras instalar | Falta re-loguearse | Cerrar sesión de Windows y volver a entrar |
| El equipo se pone muy lento | WSL 2 consumiendo RAM | Aplicar el `.wslconfig` del paso 5 |
| `port is already allocated` | El 8080 está ocupado | Usar otro puerto: `-p 8081:8080` |
