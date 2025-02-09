//  Parameters
@description('The target environment. Provide one of the allowed values.')
@allowed(['Development', 'Test', 'Production'])
param environment string

// Web App secrets

@secure()
param mySQLadministratorPassword string

@secure()
param mySQLadministratorLogin string

param location string = resourceGroup().location

var environmentSuffix = toLower(substring(environment, 0, 1))

@description('The App Service Plan SKU name.')
var appServicePlanSkuName = (environment == 'Production') ? 'F1' : 'F1'

var tags = {
  Environment: environment
}

// Resources
@description('The MySQL server')
resource mySQLserver 'Microsoft.DBforMySQL/flexibleServers@2022-01-01' = {
  name: 'Template-uw-mysql-${environmentSuffix}'
  location: location
  tags: tags
  sku: {
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }
  properties: {
    version: '8.0.21'
    administratorLogin: mySQLadministratorLogin
    administratorLoginPassword: mySQLadministratorPassword
    highAvailability: {
      mode: 'Disabled'
    }
    storage: {
      storageSizeGB: 20
      iops: 360
      autoGrow: 'Enabled'
      autoIoScaling: 'Disabled'
      logOnDisk: 'Disabled'
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
  }
}

@description('The firewall rule for MySQL server to allow access to Azure resources.')
resource mySQLserverFirewallRule 'Microsoft.DBforMySQL/flexibleServers/firewallRules@2022-01-01' = {
  name: 'AllowAccessWithinAzureIps'
  parent: mySQLserver
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

@description('The Template database in MySQL server')
resource mySQLdatabase 'Microsoft.DBforMySQL/flexibleServers/databases@2022-01-01' = {
  name: 'Template'
  parent: mySQLserver
}

@description('The App Service Plan for web app')
resource appServicePlanWA 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: 'Template-uw-asp-${environmentSuffix}'
  location: location
  sku: {
    name: appServicePlanSkuName
  }
  tags: tags
  kind: 'app'
  properties: {
    perSiteScaling: false
    elasticScaleEnabled: false
    maximumElasticWorkerCount: 1
    isSpot: false
    reserved: false
    isXenon: false
    hyperV: false
    targetWorkerCount: 0
    targetWorkerSizeId: 0
    zoneRedundant: false
  }
}

@description('The Template-api App Service App')
resource TemplateApiAppService 'Microsoft.Web/sites@2022-09-01' = {
  name: 'Template-api-uw-wa-${environmentSuffix}'
  location: location
  tags: tags
  kind: 'app'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlanWA.id
    httpsOnly: true
    publicNetworkAccess: 'Enabled'
    redundancyMode: 'None'
    clientAffinityEnabled: true
    enabled: true
    clientCertEnabled: false
    siteConfig: {
      alwaysOn: false
      netFrameworkVersion: '8.0'
      numberOfWorkers: 1
      appSettings: [
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: applicationInsights.properties.InstrumentationKey
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsights.properties.ConnectionString
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: environment
        }
        {
          name: 'MySQL:DefaultConnection'
          value: 'Server=${mySQLserver.name}.mysql.database.azure.com; Database=${mySQLdatabase.name}; Uid=${mySQLadministratorLogin}; Pwd=${mySQLadministratorPassword}; default command timeout=120'
        }
        {
          name: 'XDT_MicrosoftApplicationInsights_Mode'
          value: 'Recommended'
        }
      ]
    }
  }
}

@description('The Workspace for Application Insights')
resource workspaceForApplicationInsights 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: 'Template-uw-ws-${environmentSuffix}'
  location: location
  tags: tags
}

@description('The Application Insights')
resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'Template-uw-ai-${environmentSuffix}'
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
    IngestionMode: 'LogAnalytics'
    WorkspaceResourceId: workspaceForApplicationInsights.id
    SamplingPercentage: 100
  }
}

output TemplateApiHostName string = TemplateApiAppService.properties.defaultHostName
