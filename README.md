# Photo It

Photo It is a .NET 10 Blazor application for organizing travel photos into albums, arranging them into a paged photo-book layout, and sharing the finished album.

## Features

- Create and edit albums with a name, description, and Markdown introduction.
- Upload and manage album photos. Images are stored in Azure Blob Storage; the app extracts available EXIF metadata such as capture date, camera, and GPS coordinates.
- Design multi-page album layouts with a title/cover page, selectable page templates, and draggable photos; preview the result as a presentation.
- Create a share link for an album layout so it can be viewed without signing in.
- Edit a trip route by entering places and transport types; place names are resolved using OpenStreetMap Nominatim.
- Use ASP.NET Core Identity for account and album-owner features.

## Architecture

The solution is split into these main projects:

| Project | Responsibility |
| --- | --- |
| `PhotoIt.Web` | Interactive Server Blazor UI, Identity, album/photo workflows, HTTP endpoints, and dependency registration. |
| `PhotoIt.Data` | Album and layout models, Cosmos DB-backed catalogs, photo metadata extraction, and Azure Blob Storage integration. |
| `PhotoIt.Core` | Shared route/geolocation services, including the Nominatim client. |
| `PhotoIt.ServiceDefaults` | Shared .NET Aspire service defaults, health checks, and observability integrations. |
| `PhotoIt.AppHost` | .NET Aspire host that launches the web app and API service together. |
| `CleanCode.ApiService` | Separate minimal API project; its current endpoints are a sample root status and weather forecast, not the album API. |

The web app stores Identity, albums, photo metadata, and layouts in Azure Cosmos DB through Entity Framework Core. The database is named `PhotoIt`; the app creates the database and containers at startup with `EnsureCreatedAsync`. Original album images and thumbnails remain in the configured Azure Blob Storage container. Existing SQLite and LiteDB files are not imported; configure an empty Cosmos DB database for this version.

## Deploying to Azure

`infra/main.bicep` creates a Linux App Service plan and web app (system-assigned managed identity, .NET 10, WebSockets on), a Log Analytics workspace, and workspace-based Application Insights. It reuses the existing Cosmos DB account `photoitadmin` (same resource group as the deployment) and the existing storage account `stvisionengine7632i5tmw`, granting the web app **Storage Blob Data Contributor** on it. The web app receives `CosmosdbConnection`, `AzureStorage__*`, and `APPLICATIONINSIGHTS_CONNECTION_STRING` as app settings; telemetry is exported to Application Insights through Azure Monitor OpenTelemetry.

```powershell
az deployment group create -g photoIt --template-file infra\main.bicep `
  --parameters googleClientId=<id> googleClientSecret=<secret>
dotnet publish PhotoIt.Web -c Release -o publish
Compress-Archive publish\* publish.zip -Force
az webapp deploy -g photoIt -n <webAppName output> --src-path publish.zip --type zip
```

## Requirements

- .NET 10 SDK.
- An Azure Cosmos DB account and its connection string.
- An Azure Storage account with Blob data permissions for the app. The app creates the configured private container if it does not exist.
- Network access to OpenStreetMap Nominatim for place-name lookup in the route editor.

## Configuration

The web app reads settings from `PhotoIt.Web/appsettings.json`, environment variables, and .NET user secrets. The checked-in appsettings file contains sample/default values; update them for your environment. The following settings are consumed by the current web app:

| Setting | Required | Purpose |
| --- | --- | --- |
| `Parameters:CosmosdbConnection` | Yes for AppHost | Cosmos DB connection string. AppHost passes this secret to the web app as `CosmosdbConnection`; the database name is `PhotoIt`. |
| `CosmosdbConnection` | Yes when running `PhotoIt.Web` directly | Cosmos DB connection string used by Identity and all catalog data. |
| `AzureStorage:AccountName` | Yes, unless derived from `AccountUri` | Azure Storage account name. |
| `AzureStorage:AccountUri` | No | Blob service URI; if omitted, it is built from the account name. The account name can also be derived from this URI. |
| `AzureStorage:ContainerName` | No | Blob container for photos. Defaults to `photos`. |
| `photostorageaccountkey` | No | Optional root-level configuration value for a storage account key. If omitted, the app uses `DefaultAzureCredential` (for example, an Azure CLI login in development or a managed identity when deployed). Prefer identity-based access where possible. |
Example (replace the storage account with values for your environment):

```json
{
  "AzureStorage": {
	"AccountName": "<storage-account-name>",
	"AccountUri": "https://<storage-account-name>.blob.core.windows.net/",
	"ContainerName": "photos"
  }
}
```

For local development with Aspire, set the Cosmos DB connection string in AppHost user secrets:

```powershell
dotnet user-secrets set "Parameters:CosmosdbConnection" "<cosmos-db-connection-string>" --project PhotoIt.AppHost\PhotoIt.AppHost.csproj
```

Cosmos DB uses `EnsureCreatedAsync` rather than relational EF Core migrations. To run the web project directly instead of through AppHost, set a `CosmosdbConnection` user secret on `PhotoIt.Web`:

```powershell
dotnet user-secrets set "CosmosdbConnection" "<cosmos-db-connection-string>" --project PhotoIt.Web\PhotoIt.Web.csproj
```

If using a storage account key instead of `DefaultAzureCredential`, keep it out of source control and set it as a user secret:

```powershell
dotnet user-secrets set "photostorageaccountkey" "<storage-account-key>" --project PhotoIt.Web\PhotoIt.Web.csproj
```

`OpenRouteService` entries currently present in appsettings are not read by the application; route place lookup currently uses Nominatim. Google sign-in parameters are declared in the Aspire host, but the web project's current Google authentication registration still contains placeholder credential strings, so external Google sign-in needs that registration wired to configuration before use.

## Run locally

1. Configure `Parameters:CosmosdbConnection` in AppHost user secrets, Azure Storage access, and point `VisionModel:ModelDirectory` to a compatible local model directory.
2. From the solution directory, run the Blazor application:

   ```powershell
   dotnet run --project PhotoIt.Web\PhotoIt.Web.csproj
   ```

   Alternatively, run `PhotoIt.AppHost` to launch the Aspire application host and its referenced services.

The app creates the Cosmos DB database and containers at startup. The Azure Blob container and local model are also initialized during startup, so a missing database connection, model files, or available/authorized Azure Storage can prevent the app from starting.
