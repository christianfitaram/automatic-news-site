# NewsWebsite

Portal de noticias construido con ASP.NET Core 8 que combina gestión editorial tradicional con automatización mediante webhooks y servicios externos. Esta documentación describe la arquitectura, requisitos, configuración y flujos clave para desarrollo y operación en producción.

## Características principales

- **ASP.NET Core MVC** con Entity Framework Core y SQL Server como motor relacional.
- **ASP.NET Identity** con roles `Admin`, `Editor` y `Suscriptor`, páginas de autenticación generadas (área *Identity*) y seeding automático de un usuario administrador (`admin@news.com` / `Admin123!`).
- **Gestión completa de artículos y categorías** (CRUD, filtrado, artículos relacionados, contenidos premium).
- **Automatización por webhook** (`POST /api/scraper/webhook`) que recibe noticias externas, sanea el texto mediante un servicio HTTP y crea artículos con categorías dinámicas.
- **Notificación a servicios externos** tras publicar un artículo usando `HttpClientFactory` (`ExternalNewsApi`).
- **UI modular** con View Components, mapeos entre entidades y ViewModels y recursos estáticos en `wwwroot`.

## Arquitectura y componentes

| Capa | Elementos destacados | Notas |
| ---- | ------------------- | ----- |
| Presentación | Controladores `Home`, `Articles`, `Categories`, `Admin` y vistas Razor en `Views/`. View Component `CategoryMenuViewComponent`. | `HomeController` muestra artículo destacado, últimos artículos y secciones por categoría; `ArticlesController` ofrece búsqueda y filtros; `AdminController` gestiona usuarios y roles. |
| Servicios | `Services/Articles/ArticlesService`, interfaz `IArticlesService`. | Encapsula la creación de artículos, adjunta categorías existentes o nuevas y dispara la notificación externa (`POST /news`). |
| Datos | `Data/ApplicationDbContext`, migraciones en `Migrations/`, `ApplicationDbContextFactory` para tooling. | Hereda de `IdentityDbContext<IdentityUser, IdentityRole, string>` e incluye `DbSet<Article>` y `DbSet<Category>`. |
| Modelos | Entidades en `Models/`, ViewModels en `Models/ViewModels/`, extensiones en `Extensions/ArticleMappingExtensions.cs`. | `Article` incluye campos `RelevanceScore` e `IsPremium`; extensiones generan resúmenes y detalles con recorte inteligente. |
| API | `Controllers/ScraperWebhook.cs` | Valida la cabecera `X-Signature`, limpia el texto vía `TextSanitizerApi` y crea artículos. |

## Modelo de datos

- `Article` (`Models/Article.cs`) – título, contenido, autor, fecha, imagen, relevancia, premium y relación muchos-a-muchos con `Category`.
- `Category` (`Models/Category.cs`) – nombre, descripción y colección inversa de artículos.
- Migraciones en `Migrations/` definen la tabla relacional para artículos↔categorías y los ajustes de Identity (por ejemplo `20251015035020_AddIdentityRoles`).  

## Autenticación y roles

- Configurada en `Program.cs` con `AddDefaultIdentity<IdentityUser>().AddRoles<IdentityRole>()` (líneas 32-39).  
- `SeedData.InitializeAsync` crea roles **Admin**, **Editor**, **Suscriptor** y el usuario admin inicial (líneas 11-51). Se ejecuta en cada arranque mediante `CreateScope()` al final de `Program.cs`.  
- `AdminController` expone vistas para listar usuarios, editar roles y crear cuentas (`Controllers/AdminController.cs`).  
- Las páginas de inicio de sesión/registro aparecen en `Areas/Identity`.  

## Integraciones externas

- **ExternalNewsApi**: cliente HTTP nombrado; su `BaseAddress` se toma de `ExternalNewsApi:BaseUrl` en configuración. Cada vez que se crea un artículo se envía un `POST /news` (`ArticlesService.SendToExternalAsync`).  
- **TextSanitizerApi**: cliente HTTP con timeout de 4 minutos (`Program.cs`:25-30). El webhook lo usa para limpiar contenido (modelo `llama3.1:latest` sobre `api/generate`). Ajusta `TextSanitizerApi:BaseUrl` según el despliegue; el default apunta a `http://34.65.174.164:11434`.  
- **Webhook secreto**: definido actualmente como constante (`Controllers/ScraperWebhook.cs:21`). Sustituir por un valor seguro almacenado en variables de entorno o servicios secretos.  

## Configuración y variables

Archivo `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=NewsDB;User Id=SA;Password=TuPassword123!;TrustServerCertificate=True;"
  },
  "ExternalNewsApi": {
    "BaseUrl": "https://tu-api-externa"
  },
  "TextSanitizerApi": {
    "BaseUrl": "http://34.65.174.164:11434"
  }
}
```

> **Producción**: no mantengas credenciales ni secretos en archivos committeados. Usa variables de entorno (`ConnectionStrings__DefaultConnection`, `ExternalNewsApi__BaseUrl`, `TextSanitizerApi__BaseUrl`, `Webhooks__ScraperSecret`, etc.) o un `appsettings.Production.json` fuera del control de código fuente.

Ejemplo de `/etc/newswebsite.env` usado por `systemd`:

```
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:5000
ConnectionStrings__DefaultConnection=Server=127.0.0.1,1433;Database=NewsDB;User Id=news_app;Password=***;TrustServerCertificate=True;Encrypt=False;
ExternalNewsApi__BaseUrl=https://api.tu-integracion.com
TextSanitizerApi__BaseUrl=http://34.65.174.164:11434
Webhooks__ScraperSecret=pon-aqui-tu-secreto
```

## Puesta en marcha local

1. **Requisitos**: .NET SDK 8.0, SQL Server (local o contenedor), Node/LibMan opcional para assets.  
2. **Clonar** y restaurar dependencias NuGet: `dotnet restore`.  
3. **Configurar conexión** en `appsettings.json` o variables de entorno.  
4. **Aplicar migraciones**: `dotnet tool install --global dotnet-ef` (si no está) y `dotnet ef database update`.  
5. **Restaurar assets front-end** (si `wwwroot/lib` no existe):  
   ```bash
   libman install bootstrap@5.3.3 -d wwwroot/lib/bootstrap --files dist/css/bootstrap.min.css --files dist/js/bootstrap.bundle.min.js
   libman install jquery@3.7.1 -d wwwroot/lib/jquery --files dist/jquery.min.js
   libman install jquery-validation@1.19.5 -d wwwroot/lib/jquery-validation --files dist/jquery.validate.min.js
   libman install jquery-validation-unobtrusive@3.2.12 -d wwwroot/lib/jquery-validation-unobtrusive --files jquery.validate.unobtrusive.min.js
   ```
6. **Ejecutar**: `dotnet run` o `dotnet watch run`. La app se inicia en `https://localhost:5001`.  
7. Inicia sesión con `admin@news.com` / `Admin123!` y cambia la contraseña.  

## Webhook de ingestión

- Endpoint: `POST /api/scraper/webhook`  
- Cabecera requerida: `X-Signature` igual al secreto configurado.  
- Payload ejemplo:
  ```json
  {
    "title": "Economy outlook 2025",
    "url": "https://example.com/story",
    "text": "Contenido completo...",
    "scraped_at": "2025-10-15T08:00:00Z",
    "topic": "Economy, Markets"
  }
  ```
- Flujo interno: valida firma → registra payload → limpia texto (`TextSanitizerApi`) → construye `CreateArticleRequest` → `ArticlesService.CreateArticleAsync` → responde con `{ message, articleId }`.  
- Errores de sanitizado no bloquean la creación: el servicio registra el fallo y conserva el texto original.  

## Publicación de artículos

`ArticlesService` centraliza la lógica (`Services/Articles/ArticlesService.cs`):

1. Normaliza autor, fecha, relevancia e indicador premium.  
2. Resuelve categorías por IDs y nombres (creando las ausentes).  
3. Guarda en EF Core, enviando un POST al servicio externo (`/news`) con datos relevantes.  
4. Internamente usa LINQ y `DistinctBy` para evitar duplicados.  

## Despliegue recomendado

1. **Publicar**: `dotnet publish NewsWebsite.csproj -c Release -o ./publish`.  
2. **Copiar al servidor** (`/var/www/newswebsite`) y replicar `wwwroot/lib`.  
3. **Servicio systemd** (ejemplo: `/etc/systemd/system/newswebsite.service`):
   ```ini
   [Unit]
   Description=NewsWebsite ASP.NET Core App
   After=network.target docker.service

   [Service]
   WorkingDirectory=/var/www/newswebsite
   ExecStart=/usr/bin/dotnet /var/www/newswebsite/NewsWebsite.dll
   EnvironmentFile=/etc/newswebsite.env
   Restart=always
   RestartSec=10
   User=www-data
   Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

   [Install]
   WantedBy=multi-user.target
   ```
4. **Reverse proxy** con Nginx apuntando a `127.0.0.1:5000`. Asegura que reenvíe `X-Forwarded-For` y `X-Forwarded-Proto`; la app lo procesa con `ForwardedHeadersOptions` (`Program.cs:51`).  
5. **CDN/Cloudflare**: tras cada despliegue purga la caché de `/lib/*` para reflejar los nuevos assets.  
6. **Base de datos**: aplicar migraciones en producción (`dotnet ef database update --project NewsWebsite`). Crear cuentas SQL dedicadas en lugar de usar `sa`.  

## Estructura del proyecto

```
NewsWebsite/
├── Areas/Identity/…          # Páginas de autenticación ASP.NET Identity
├── Controllers/               # Controladores MVC y API
├── Data/                      # DbContext, Factory y Seed
├── Extensions/                # Métodos de extensión (mapeos)
├── Migrations/                # Migraciones EF Core
├── Models/                    # Entidades y ViewModels
├── Services/Articles/         # Lógica de negocio para artículos
├── ViewComponents/            # Componentes de vista reutilizables
├── Views/                     # Vistas Razor y layout principal
└── wwwroot/                   # Assets estáticos (CSS, JS, imágenes, lib/)
```

## Buenas prácticas y próximos pasos

- **Secretos**: mueve el secreto del webhook y la cadena de conexión a variables seguras; considera Azure Key Vault, AWS Secrets Manager, etc.  
- **Seguridad**: habilita HTTPS de extremo a extremo (modo “Full/Strict” en Cloudflare con certificado de origen) y agrega políticas CSP.  
- **Monitoreo**: habilita `journalctl`, métricas de Cloud Monitoring o Prometheus para el servicio `newswebsite`.  
- **Pruebas automáticas**: planifica pruebas unitarias para `ArticlesService` y para el parser de categorías del webhook; considera pruebas de integración con una base de datos temporal.  
- **Front-end build**: automatiza `libman restore` en pipelines o versiona `wwwroot/lib` si prefieres simplicidad.  
- **Roles adicionales**: personaliza permisos (por ejemplo, suscriptores de pago) o integra Identity con proveedores externos.  

---

Para dudas operativas o contribuciones, documenta nuevas decisiones directamente en este README y mantén la información de despliegue al día.
