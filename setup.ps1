# BookSpot Development Environment Setup Script
param(
    [switch]$SkipDocker,
    [switch]$SkipDotNet,
    [switch]$SkipAWS
)

Write-Host "🚀 Setting up BookSpot development environment..." -ForegroundColor Green

# Check if running as administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
if (-not $isAdmin) {
    Write-Host "⚠️  Consider running as administrator for best results" -ForegroundColor Yellow
}

# Function to check if command exists
function Test-Command($command) {
    try {
        Get-Command $command -ErrorAction Stop
        return $true
    } catch {
        return $false
    }
}

# Check .NET 8
if (-not $SkipDotNet) {
    Write-Host "`n📦 Checking .NET 8 SDK..." -ForegroundColor Cyan
    if (Test-Command "dotnet") {
        $dotnetVersion = dotnet --version
        Write-Host "✓ .NET SDK found: $dotnetVersion" -ForegroundColor Green
        
        # Check if .NET 8 is available
        $frameworks = dotnet --list-sdks
        if ($frameworks -match "8\.") {
            Write-Host "✓ .NET 8 SDK is available" -ForegroundColor Green
        } else {
            Write-Host "⚠️  .NET 8 SDK not found. Please install from https://dotnet.microsoft.com/download" -ForegroundColor Yellow
        }
    } else {
        Write-Host "✗ .NET SDK not found. Please install from https://dotnet.microsoft.com/download" -ForegroundColor Red
    }
}

# Check Docker
if (-not $SkipDocker) {
    Write-Host "`n🐳 Checking Docker..." -ForegroundColor Cyan
    if (Test-Command "docker") {
        try {
            $dockerVersion = docker --version
            Write-Host "✓ Docker found: $dockerVersion" -ForegroundColor Green
            
            # Check if Docker is running
            docker info 2>$null | Out-Null
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✓ Docker is running" -ForegroundColor Green
            } else {
                Write-Host "⚠️  Docker is installed but not running. Please start Docker Desktop" -ForegroundColor Yellow
            }
        } catch {
            Write-Host "✗ Docker is installed but not accessible" -ForegroundColor Red
        }
    } else {
        Write-Host "✗ Docker not found. Please install Docker Desktop from https://docker.com/products/docker-desktop" -ForegroundColor Red
    }
}

# Check AWS CLI (optional)
if (-not $SkipAWS) {
    Write-Host "`n☁️  Checking AWS CLI (optional)..." -ForegroundColor Cyan
    if (Test-Command "aws") {
        $awsVersion = aws --version
        Write-Host "✓ AWS CLI found: $awsVersion" -ForegroundColor Green
    } else {
        Write-Host "ℹ️  AWS CLI not found (optional for local development)" -ForegroundColor Gray
        Write-Host "   Install from: https://aws.amazon.com/cli/" -ForegroundColor Gray
    }
}

# Check Git
Write-Host "`n📝 Checking Git..." -ForegroundColor Cyan
if (Test-Command "git") {
    $gitVersion = git --version
    Write-Host "✓ Git found: $gitVersion" -ForegroundColor Green
} else {
    Write-Host "✗ Git not found. Please install from https://git-scm.com/" -ForegroundColor Red
}

# Restore .NET packages
Write-Host "`n📦 Restoring .NET packages..." -ForegroundColor Cyan
try {
    dotnet restore
    Write-Host "✓ Packages restored successfully" -ForegroundColor Green
} catch {
    Write-Host "✗ Failed to restore packages" -ForegroundColor Red
}

# Build the solution
Write-Host "`n🔨 Building solution..." -ForegroundColor Cyan
try {
    dotnet build --configuration Debug
    Write-Host "✓ Solution built successfully" -ForegroundColor Green
} catch {
    Write-Host "✗ Build failed" -ForegroundColor Red
}

# Make scripts executable
Write-Host "`n🔧 Setting up scripts..." -ForegroundColor Cyan
try {
    if (Test-Path "scripts/start-localstack.sh") {
        # For WSL/Git Bash users
        & wsl chmod +x scripts/start-localstack.sh scripts/stop-localstack.sh 2>$null
    }
    Write-Host "✓ Scripts configured" -ForegroundColor Green
} catch {
    Write-Host "ℹ️  Script permissions set (WSL not available)" -ForegroundColor Gray
}

# Create local settings if they don't exist
Write-Host "`n⚙️  Checking configuration..." -ForegroundColor Cyan
$devSettings = "BookSpot.API/appsettings.Development.json"
if (Test-Path $devSettings) {
    Write-Host "✓ Development settings found" -ForegroundColor Green
} else {
    Write-Host "⚠️  Development settings not found" -ForegroundColor Yellow
}

# Summary
Write-Host "`n📋 Setup Summary:" -ForegroundColor Green
Write-Host "==================" -ForegroundColor Green

$requirements = @(
    @{ Name = ".NET 8 SDK"; Command = "dotnet"; Required = $true },
    @{ Name = "Docker"; Command = "docker"; Required = $true },
    @{ Name = "Git"; Command = "git"; Required = $true },
    @{ Name = "AWS CLI"; Command = "aws"; Required = $false }
)

$allGood = $true
foreach ($req in $requirements) {
    $status = if (Test-Command $req.Command) { "✓" } else { "✗" }
    $color = if (Test-Command $req.Command) { "Green" } else { if ($req.Required) { "Red"; $allGood = $false } else { "Yellow" } }
    $required = if ($req.Required) { "(Required)" } else { "(Optional)" }
    
    Write-Host "$status $($req.Name) $required" -ForegroundColor $color
}

Write-Host "`n🚀 Next Steps:" -ForegroundColor Green
Write-Host "==============" -ForegroundColor Green

if ($allGood) {
    Write-Host "1. Start LocalStack: .\scripts\start-localstack.ps1" -ForegroundColor Cyan
    Write-Host "2. Run the API: cd BookSpot.API && dotnet run" -ForegroundColor Cyan
    Write-Host "3. Test endpoints: Use BookSpot.http file" -ForegroundColor Cyan
    Write-Host "4. View Swagger: https://localhost:7071/swagger" -ForegroundColor Cyan
    Write-Host "`n✨ You're ready to develop! Happy coding!" -ForegroundColor Green
} else {
    Write-Host "Please install the missing required components above" -ForegroundColor Red
    Write-Host "Then run this setup script again" -ForegroundColor Yellow
}

Write-Host "`n📚 Documentation:" -ForegroundColor Green
Write-Host "- README.md - Project overview and quick start" -ForegroundColor Gray
Write-Host "- LocalStack-Setup.md - Detailed LocalStack guide" -ForegroundColor Gray
Write-Host "- CONTRIBUTING.md - Development guidelines" -ForegroundColor Gray
Write-Host "- BookSpot.http - API test collection" -ForegroundColor Gray

Write-Host "`n🆘 Need help? Check the documentation or open an issue!" -ForegroundColor Cyan