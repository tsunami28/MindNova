# this script is for creating service bus message for triggering the dns handler function

$resourceId = '/subscriptions/aab80d35-e4a6-4c34-9c93-57a78545c8bb/resourceGroups/MindNova-dev/providers/Microsoft.Network/privateEndpoints/stgauditMindNovadev-pe'
$info = $resourceId.split('/', [System.StringSplitOptions]::RemoveEmptyEntries) 
$subscriptionGuid = $info[1]

$subscriptionId = "/subscriptions/$subscriptionGuid"

$example = @'
{"subject":"/subscriptions/aab80d35-e4a6-4c34-9c93-57a78545c8bb/resourceGroups/MindNova-dev/providers/Microsoft.Network/privateEndpoints/stgauditMindNovadev-pe","eventType":"Microsoft.Resources.ResourceWriteSuccess","id":"f4c20242-cba5-48e4-a050-7d942ae107c0","data":{"authorization":{"Scope":"/subscriptions/aab80d35-e4a6-4c34-9c93-57a78545c8bb/resourceGroups/MindNova-dev/providers/Microsoft.Network/privateEndpoints/stgauditMindNovadev-pe","Action":"Microsoft.Network/privateEndpoints/write","Evidence":{"Role":"Owner","RoleAssignmentScope":"/subscriptions/aab80d35-e4a6-4c34-9c93-57a78545c8bb","RoleAssignmentId":"f0f8bc2673e1452eaaa45928c23cae2b","RoleDefinitionId":"8e3af657a8ff443ca75c2fe8c4bcb635","PrincipalId":"cd92e6516f70455e9da8d34b8bc85fea","PrincipalType":"Group"}},"claims":{"aud":"https://management.core.windows.net/","iss":"https://sts.windows.net/d7790549-8c35-40ea-ad75-954ac3e86be8/","iat":"1713447259","nbf":"1713447259","exp":"1713452693","http://schemas.microsoft.com/claims/authnclassreference":"1","aio":"AVQAq/8WAAAAOfEjWGmG6GMsxVvGlPDUybkII/Mnv39x01Eou0N8FitYumZ76v3OWmPmmHEyohVSK4HcIxWHDxpVPdGAJT6wBw66ph6T6EVsBkrkv54XvME=","http://schemas.microsoft.com/claims/authnmethodsreferences":"pwd,rsa,mfa","appid":"c44b4083-3bb0-49c1-b47d-974e53cbdf3c","appidacr":"0","http://schemas.microsoft.com/2012/01/devicecontext/claims/identifier":"2dfce803-a68d-4a53-9852-5a65751f630a","http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname":"Heerink - Wijnja","http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname":"No�lle","groups":"23b4f235-f6f4-469b-9d67-f67bcee2b06c","idtyp":"user","ipaddr":"86.87.243.35","name":"bhr_wijnj004","http://schemas.microsoft.com/identity/claims/objectidentifier":"a348f815-0d14-4a85-b2fe-d3b36519e4fc","onprem_sid":"S-1-5-21-1305377269-4159824490-449157577-589835","puid":"100320004DC04553","rh":"0.AR8ASQV51zWM6kCtdZVKw-hr6EZIf3kAutdPukPawfj2MBOFABk.","http://schemas.microsoft.com/identity/claims/scope":"user_impersonation","http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier":"jYVrbQ7pRWbhnGvRJHxss3ac3oovnCD4WYJBQmU1nIk","http://schemas.microsoft.com/identity/claims/tenantid":"d7790549-8c35-40ea-ad75-954ac3e86be8","http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name":"bhr_wijnj004@[PLACEHOLDER].com","http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn":"bhr_wijnj004@[PLACEHOLDER].com","uti":"VAnQMPCJw0iGLhPs6Z1QAA","ver":"1.0","wids":"5d6b6bb7-de71-4623-b4af-96380a352509","xms_tcdt":"1398243312"},"correlationId":"714a7b62-ee15-41bd-afd5-020c75108185","httpRequest":{},"resourceProvider":"Microsoft.Network","resourceUri":"/subscriptions/aab80d35-e4a6-4c34-9c93-57a78545c8bb/resourceGroups/MindNova-dev/providers/Microsoft.Network/privateEndpoints/stgauditMindNovadev-pe","operationName":"Microsoft.Network/privateEndpoints/write","status":"Succeeded","subscriptionId":"aab80d35-e4a6-4c34-9c93-57a78545c8bb/","tenantId":"d7790549-8c35-40ea-ad75-954ac3e86be8"},"dataVersion":"2","metadataVersion":"1","eventTime":"2025-11-10T15:54:21.4286861Z","topic":"/subscriptions/aab80d35-e4a6-4c34-9c93-57a78545c8bb"}
'@| convertfrom-json


$example.subject = $resourceId
$example.data.authorization.Scope = $resourceId
$example.data.authorization.Evidence.RoleAssignmentScope = $subscriptionId
$example.data.resourceUri = $resourceId
$example.data.subscriptionId = $subscriptionGuid
$example.eventTime = [DateTime]::UtcNow.ToString("o")
$example.topic = $subscriptionId
$example | convertto-json -Depth 100 -Compress | Set-Clipboard