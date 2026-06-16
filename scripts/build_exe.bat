@echo off
echo Instalando dependencias...
pip install -r requirements.txt

echo.
echo Gerando executavel...
python -m PyInstaller --onefile --windowed --name "VendApps Imagens" enrich_images_gui.py
if errorlevel 1 (
    echo.
    echo Erro ao gerar executavel.
    pause
    exit /b 1
)

echo.
echo Pronto! O exe esta em: dist\VendApps Imagens.exe
pause
