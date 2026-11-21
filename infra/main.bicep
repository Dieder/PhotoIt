// Hosts Photo It on Azure App Service with Application Insights.
// Deploy at resource group scope into the resource group that contains the existing Cosmos DB account.
targetScope = 'resourceGroup'

@description('Azure region for the new resources. Defaults to the storage account region; B1 quota is unavailable in West US 2 for this subscription.')
param location string = 'swedencentral'

@description('Base name used for new resources.')
@minLength(3)
@maxLength(20)
param appName string = 'photoit'

@description('Existing Azure Cosmos DB account used by the app (must be in this resource group).')
param cosmosAccountName string = 'photoitadmin'

@description('Existing storage account that holds album photos.')
param storageAccountName string = 'stvisionengine7632i5tmw'

@description('Resource group of the existing storage account.')
param storageResourceGroupName string = 'rgVisionworker'

@description('Blob container for photos.')
param photoContainerName string = 'photos'

@description('App Service plan SKU.')
param appServiceSkuName string = 'B1'

@description('Google OAuth client id. Leave empty to disable Google sign-in.')
param googleClientId string = ''

@secure()
@description('Google OAuth client secret.')
param googleClientSecret string = ''

@description('Base URL of the VisionEngine API used for AI photo editing. Leave empty to disable the feature.')
param visionEngineBaseUrl string = ''

@description('VisionEngine API scope, e.g. api://{api-client-id}/.default.')
param visionEngineScope string = ''

@description('Entra tenant of the app registration PhotoIt uses to call VisionEngine.')
param visionEngineTenantId string = ''

@description('Client id of the app registration PhotoIt uses to call VisionEngine.')
param visionEngineClientId string = ''

@secure()
@description('Client secret of the app registration PhotoIt uses to call VisionEngine.')
param visionEngineClientSecret string = ''

var suffix = uniqueString(resourceGroup().id)
var webAppName = '${appName}-web-${suffix}'

resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' existing = {
  name: cosmosAccountName
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${appName}-logs-${suffix}'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${appName}-insights'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${appName}-plan'
  location: location
  kind: 'linux'
  sku: {
    name: appServiceSkuName
  }
  properties: {
    reserved: true
  }
}

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: webAppName
  location: location
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: appServiceSkuName != 'F1'
      webSocketsEnabled: true // Required by Blazor Server
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'CosmosdbConnection'
          value: cosmos.listConnectionStrings().connectionStrings[0].connectionString
        }
        {
          name: 'AzureStorage__AccountName'
          value: storageAccountName
        }
        {
          name: 'AzureStorage__AccountUri'
          value: 'https://${storageAccountName}.blob.${environment().suffixes.storage}/'
        }
        {
          name: 'AzureStorage__ContainerName'
          value: photoContainerName
        }
        {
          name: 'Google__ClientId'
          value: googleClientId
        }
        {
          name: 'Google__ClientSecret'
          value: googleClientSecret
        }
        {
          name: 'VisionEngine__BaseUrl'
          value: visionEngineBaseUrl
        }
        {
          name: 'VisionEngine__Scope'
          value: visionEngineScope
        }
        {
          name: 'VisionEngine__TenantId'
          value: visionEngineTenantId
        }
        {
          name: 'VisionEngine__ClientId'
          value: visionEngineClientId
        }
        {
          name: 'VisionEngine__ClientSecret'
          value: visionEngineClientSecret
        }
      ]
    }
  }
}

// The web app uses its managed identity (DefaultAzureCredential) to reach the existing storage account.
module storageAccess 'storage-access.bicep' = {
  name: 'storage-access'
  scope: resourceGroup(storageResourceGroupName)
  params: {
    storageAccountName: storageAccountName
    principalId: webApp.identity.principalId
  }
}

output webAppName string = webApp.name
output webAppUrl string = 'https://${webApp.properties.defaultHostName}'
output appInsightsName string = appInsights.name
