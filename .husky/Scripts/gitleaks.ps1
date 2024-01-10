Set-Location ../../
wsl gitleaks detect --source . --verbose --log-opts="--all HEAD~1..HEAD"

$gitLeaksOutput = wsl echo $?

if ($gitLeaksOutput -contains 'True')
{
    Write-Output "No leaks were found in the last commit."
    exit 0
}

Write-Error "Some leaks were found. Check gitleaks command manually."

exit -1
