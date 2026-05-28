# ===================================================================
#  Jig Inventory - One-Click IIS Publish & Deploy Script
#  Output: ./JIG_BUILD  ->  Auto-deploy to IIS path
# ===================================================================

$ErrorActionPreference = "Stop"
$ROOT = Split-Path -Parent $MyInvocation.MyCommand.Definition
$OUTPUT = Join-Path $ROOT "JIG_BUILD"
$FRONTEND = Join-Path $ROOT "frontend"
$BACKEND  = Join-Path $ROOT "backend"
$TEMP_FE  = Join-Path $ROOT ".publish_frontend_temp"
$TEMP_BE  = Join-Path $ROOT ".publish_backend_temp"

# === IIS Deployment Config ===
$IIS_DEPLOY_PATH = "C:\Web_application\JIGInventory"
$IIS_SITE_NAME   = "JIGInventory"

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Jig Inventory - IIS Publish & Deploy" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# --- Step 0: Clean old output ---
Write-Host "[0/5] Cleaning old build folders..." -ForegroundColor Yellow
if (Test-Path $OUTPUT)   { Remove-Item $OUTPUT   -Recurse -Force }
if (Test-Path $TEMP_FE)  { Remove-Item $TEMP_FE  -Recurse -Force }
if (Test-Path $TEMP_BE)  { Remove-Item $TEMP_BE  -Recurse -Force }
Write-Host "      Done." -ForegroundColor Green

# --- Step 1: Publish Frontend (Blazor WASM) ---
Write-Host ""
Write-Host "[1/5] Publishing Frontend (Blazor WebAssembly)..." -ForegroundColor Yellow
dotnet publish $FRONTEND -c Release -o $TEMP_FE --nologo
if ($LASTEXITCODE -ne 0) { throw "Frontend publish failed!" }
Write-Host "      Frontend published." -ForegroundColor Green

# --- Step 2: Publish Backend (ASP.NET Core Web API) ---
Write-Host ""
Write-Host "[2/5] Publishing Backend (ASP.NET Core API)..." -ForegroundColor Yellow
dotnet publish $BACKEND -c Release -o $TEMP_BE --nologo
if ($LASTEXITCODE -ne 0) { throw "Backend publish failed!" }
Write-Host "      Backend published." -ForegroundColor Green

# --- Step 3: Merge frontend wwwroot into backend wwwroot ---
Write-Host ""
Write-Host "[3/5] Merging Frontend into Backend wwwroot..." -ForegroundColor Yellow

$FE_WWWROOT = Join-Path $TEMP_FE "wwwroot"
$BE_WWWROOT = Join-Path $TEMP_BE "wwwroot"

if (-not (Test-Path $BE_WWWROOT)) { New-Item -ItemType Directory -Path $BE_WWWROOT | Out-Null }

# Copy all frontend static files (HTML, JS, CSS, WASM, DLL) into backend wwwroot
Copy-Item -Path (Join-Path $FE_WWWROOT "*") -Destination $BE_WWWROOT -Recurse -Force
Write-Host "      Merged." -ForegroundColor Green

# --- Step 4: Move to JIG_BUILD ---
Write-Host ""
Write-Host "[4/5] Creating JIG_BUILD folder..." -ForegroundColor Yellow
Rename-Item -Path $TEMP_BE -NewName "JIG_BUILD"
# Clean up temp frontend
Remove-Item $TEMP_FE -Recurse -Force
Write-Host "      Done." -ForegroundColor Green

# --- Step 5: Deploy to IIS ---
Write-Host ""
Write-Host "[5/5] Deploying to IIS ($IIS_DEPLOY_PATH)..." -ForegroundColor Yellow

# Stop IIS Site to avoid file lock
try {
    $site = Get-Website -Name $IIS_SITE_NAME -ErrorAction SilentlyContinue
    if ($site) {
        Write-Host "      Stopping IIS Site '$IIS_SITE_NAME'..." -ForegroundColor DarkYellow
        Stop-Website -Name $IIS_SITE_NAME -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
    }
}
catch {
    Write-Host "      (IIS cmdlet not available, skipping stop)" -ForegroundColor DarkYellow
}

# Create deploy folder if not exists
if (-not (Test-Path $IIS_DEPLOY_PATH)) {
    New-Item -ItemType Directory -Path $IIS_DEPLOY_PATH -Force | Out-Null
    Write-Host "      Created deploy folder." -ForegroundColor DarkYellow
}

# Copy everything from JIG_BUILD to IIS path
Copy-Item -Path (Join-Path $OUTPUT "*") -Destination $IIS_DEPLOY_PATH -Recurse -Force
Write-Host "      Files copied." -ForegroundColor Green

# Start IIS Site back up
try {
    $site = Get-Website -Name $IIS_SITE_NAME -ErrorAction SilentlyContinue
    if ($site) {
        Write-Host "      Starting IIS Site '$IIS_SITE_NAME'..." -ForegroundColor DarkYellow
        Start-Website -Name $IIS_SITE_NAME -ErrorAction SilentlyContinue
        Write-Host "      IIS Site started." -ForegroundColor Green
    }
    else {
        Write-Host "      IIS Site '$IIS_SITE_NAME' not found, please start manually." -ForegroundColor DarkYellow
    }
}
catch {
    Write-Host "      (Could not restart IIS Site, please restart manually)" -ForegroundColor DarkYellow
}

# --- Summary ---
Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "  BUILD & DEPLOY SUCCESS!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""
Write-Host "  Build folder : $OUTPUT" -ForegroundColor White
Write-Host "  Deploy path  : $IIS_DEPLOY_PATH" -ForegroundColor White
Write-Host ""
Write-Host "  Open: http://localhost:5062" -ForegroundColor Cyan
Write-Host ""
