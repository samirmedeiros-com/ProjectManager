@echo off
echo.
echo 🚀 Starting Project Manager...
echo.

REM Start backend
echo Starting Backend (.NET 10)...
cd ProjectManagerWebAPI
start "ProjectManager Backend" cmd /k dotnet run

REM Wait for backend to start
timeout /t 3 /nobreak

REM Start frontend
echo Starting Frontend (Angular)...
cd ..\ProjectManager\ProjectManagerWebUI
start "ProjectManager Frontend" cmd /k ng serve

echo.
echo ✓ Backend started on http://localhost:5000
echo ✓ Frontend started on http://localhost:4200
echo.
echo Press any key to close this window...
pause
