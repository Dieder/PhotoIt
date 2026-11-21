@description('Existing storage account.')
param storageAccountName string

@description('Principal that needs blob access.')
param principalId string

// Storage Blob Data Contributor
var blobContributorRoleId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

resource blobAccess 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storage.id, principalId, blobContributorRoleId)
  scope: storage
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', blobContributorRoleId)
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}
