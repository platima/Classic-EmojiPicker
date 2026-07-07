@echo off
echo Building Classic Emoji Picker v0.1.2...
echo.

REM Clean previous builds
if exist "publish" rmdir /s /q "publish"

echo Building Release configuration...
dotnet build --configuration Release

if %ERRORLEVEL% neq 0 (
    echo Build failed!
    pause
    exit /b 1
)

echo.
echo Publishing self-contained executable...
dotnet publish EmojiPicker\EmojiPicker.csproj --configuration Release --runtime win-x64 --self-contained true --output publish\win-x64

if %ERRORLEVEL% neq 0 (
    echo Publish failed!
    pause
    exit /b 1
)

echo.
echo Creating portable version...
dotnet publish EmojiPicker\EmojiPicker.csproj --configuration Release --runtime win-x64 --self-contained false --output publish\portable

echo.
echo ========================================
echo Build Complete!
echo ========================================
echo.
echo Self-contained: publish\win-x64\EmojiPicker.exe
echo Portable:       publish\portable\EmojiPicker.exe
echo.
echo The self-contained version includes all dependencies.
echo The portable version requires .NET 8 to be installed.
echo.
pause
