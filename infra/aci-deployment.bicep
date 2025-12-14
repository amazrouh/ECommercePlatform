@description('The name of the environment')
param environmentName string = 'dev'

@description('Location for all resources.')
param location string = 'eastus'

@description('The name of the application')
param appName string = 'notificationservice'

@description('Container image to deploy')
param containerImage string = 'notificationservice:latest'

@description('Container registry server')
param registryServer string = 'index.docker.io'

@description('CPU cores for the container')
param cpuCores int = 1

@description('Memory in GB for the container')
param memoryInGb int = 2

@description('Number of container instances')
param instanceCount int = 1

var resourceName = '${appName}-${environmentName}'

resource containerGroup 'Microsoft.ContainerInstance/containerGroups@2021-09-01' = {
  name: '${resourceName}-aci'
  location: location
  properties: {
    containers: [
      {
        name: 'notificationservice'
        properties: {
          image: containerImage
          ports: [
            {
              port: 8080
              protocol: 'TCP'
            }
            {
              port: 80
              protocol: 'TCP'
            }
          ]
          environmentVariables: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: environmentName == 'prod' ? 'Production' : 'Development'
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://*:8080'
            }
          ]
          resources: {
            requests: {
              cpu: cpuCores
              memoryInGB: memoryInGb
            }
          }
        }
      }
    ]
    ipAddress: {
      type: 'Public'
      ports: [
        {
          port: 8080
          protocol: 'TCP'
        }
        {
          port: 80
          protocol: 'TCP'
        }
      ]
      dnsNameLabel: '${resourceName}-aci'
    }
    osType: 'Linux'
    restartPolicy: 'Always'
  }
}

output containerGroupName string = containerGroup.name
output containerUrl string = 'http://${containerGroup.properties.ipAddress.fqdn}:8080'
output containerIp string = containerGroup.properties.ipAddress.ip
