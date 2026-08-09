# BookSpot OmniSharp Recovery Script
Write-Host "Starting OmniSharp recovery process..." -ForegroundColor Green

# Step 1: Clean all build artifacts
Write-Host "Cleaning build artifacts..." -ForegroundColor Yellow
dotnet clean
Remove-Item -Recurse -Force -ErrorAction SilentlyContinue bin, obj, .vs
Get-ChildItem -Recurse -Directory -Name bin, obj | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

# Step 2: Restore packages
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore

# Step 3: Build solution
Write-Host "Building solution..." -ForegroundColor Yellow
dotnet build

# Step 4: Check for VS Code processes and restart if needed
Write-Host "Checking for VS Code processes..." -ForegroundColor Yellow
$vscodeProcesses = Get-Process -Name "Code" -ErrorAction SilentlyContinue
if ($vscodeProcesses) {
    Write-Host "VS Code is running. Please close VS Code and restart it manually." -ForegroundColor Red
    Write-Host "After restarting VS Code:" -ForegroundColor Cyan
    Write-Host "1. Open Command Palette (Ctrl+Shift+P)" -ForegroundColor Cyan
    Write-Host "2. Run 'OmniSharp: Restart OmniSharp'" -ForegroundColor Cyan
    Write-Host "3. Wait for project to load completely" -ForegroundColor Cyan
} else {
    Write-Host "VS Code is not running. You can now start VS Code." -ForegroundColor Green
}

Write-Host "Recovery script completed!" -ForegroundColor Green
Write-Host "If issues persist, try switching to C# Dev Kit extension." -ForegroundColor Yellow