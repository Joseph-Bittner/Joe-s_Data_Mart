# Joe's Data Mart - GitHub Setup Script
# Run this after creating the GitHub repository

param([string]$GitHubUsername)

if (-not $GitHubUsername) {
    Write-Host "Usage: .\setup-github.ps1 -GitHubUsername 'yourusername'"
    Write-Host "Example: .\setup-github.ps1 -GitHubUsername 'johndoe'"
    exit 1
}

$gitPath = "C:\Users\joseph.bittner\AppData\Local\GitHubDesktop\app-3.5.6\resources\app\git\cmd\git.exe"
$repoUrl = "https://github.com/$GitHubUsername/Joe-s_Data_Mart.git"

Write-Host "Setting up GitHub repository connection..."
Write-Host "Repository URL: $repoUrl"

# Rename branch to main
& $gitPath branch -m master main

# Add remote
& $gitPath remote add origin $repoUrl

# Push to GitHub
Write-Host "Pushing to GitHub..."
& $gitPath push -u origin main

Write-Host "✅ Successfully pushed Joe's Data Mart to GitHub!"
Write-Host "Repository: https://github.com/$GitHubUsername/Joe-s_Data_Mart"
Write-Host ""
Write-Host "Next steps:"
Write-Host "1. Visit the repository URL above"
Write-Host "2. Enable GitHub Pages if you want web hosting"
Write-Host "3. Add a README badge or description"
Write-Host "4. Set up GitHub Actions for CI/CD if desired"