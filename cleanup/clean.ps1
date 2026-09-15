#Prerequisites
# Make sure you are logged in to GitHub CLI and have the necessary permissions to delete codespaces and repositories.
# gh auth refresh -h github.com -s admin:org

$repos = gh codespace list --org XpiritCommunityEvents --limit 1000 --json name,displayName,owner,repository,state,machineName,createdAt,lastUsedAt | ConvertFrom-Json


Write-Host "Starting cleanup of codespaces..."
foreach ($codespace in $repos) {
    #Write-Host "createdAt: $($codespace.createdAt)"
    #Write-Host "displayName: $($codespace.displayName)"
    #Write-Host "lastUsedAt: $($codespace.lastUsedAt)"
    #Write-Host "machineName: $($codespace.machineName)"
    #Write-Host "name: $($codespace.name)"
    #Write-Host "owner: $($codespace.owner)"
    #Write-Host "repository: $($codespace.repository)"
    #Write-Host "state: $($codespace.state)"
    Write-Host "gh codespace delete -c $($codespace.name) --org XpiritCommunityEvents --user $($codespace.owner) -f"
}

# Write-Host "-----------------------------------"
# Write-Host "Starting cleanup of repositories..."
# $repositories = gh repo list xpiritcommunityevents --limit 1000 --json name,owner | ConvertFrom-Json

# foreach ($repository in $repositories | Where-Object { $_.name -like 'attendee-workshopAF-*' }) {
#     Write-Host "gh repo delete $($repository.owner.login)/$($repository.name) --yes"
# }