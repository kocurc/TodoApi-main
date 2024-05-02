param
(
    [parameter(ValueFromPipeline = $true)]
    [string]$TodoApiUrl = 'https://localhost:5001'
)

try {
    $response = Invoke-WebRequest $TodoApiUrl

    if ($response.StatusCode -eq 200) {
         return 0 
    }
    else { 
        return 1 
    }
} catch {
    return 1
}
