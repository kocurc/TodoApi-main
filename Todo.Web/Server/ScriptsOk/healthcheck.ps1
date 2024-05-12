# OK. Use this script to check if the TodoApi application is running in Dockerfile.
param
(
    [parameter(ValueFromPipeline = $true)]
    [string]TodoApiApplicationUrl = 'https://localhost:5001'
)

try {
    $response = Invoke-WebRequest TodoApiApplicationUrl

    if ($response.StatusCode -eq 200) {
         return 0 
    }
    else { 
        return 1 
    }
} catch {
    return 1
}
