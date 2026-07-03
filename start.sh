#!/bin/bash

echo "🚀 Starting Project Manager + Gestão SEUR..."
echo ""

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# Cleanup on exit
cleanup() {
    echo ""
    echo -e "${RED}Stopping servers...${NC}"
    kill $BACKEND_PID $FRONTEND_PID 2>/dev/null
    exit 0
}
trap cleanup INT TERM

# Start backend
echo -e "${YELLOW}Starting Backend (.NET)...${NC}"
cd ProjectManagerWebAPI
dotnet run &
BACKEND_PID=$!

# Wait for backend to be ready
echo -e "${YELLOW}Waiting for backend...${NC}"
until curl -s http://localhost:5001/api/auth/users > /dev/null 2>&1 || \
      curl -s -o /dev/null -w "%{http_code}" http://localhost:5001 | grep -qE "^[0-9]"; do
    sleep 1
done

echo -e "${GREEN}✓ Backend running on http://localhost:5001${NC}"

# Start frontend
echo -e "${YELLOW}Starting Frontend (Angular)...${NC}"
cd ../ProjectManager/ProjectManagerWebUI
ng serve &
FRONTEND_PID=$!

echo ""
echo -e "${GREEN}✓ Backend:  http://localhost:5001${NC}"
echo -e "${GREEN}✓ Frontend: http://localhost:4200${NC}"
echo ""
echo -e "${YELLOW}Portal: http://localhost:4200/portal${NC}"
echo ""
echo "Press Ctrl+C to stop both servers"
echo ""

wait $BACKEND_PID $FRONTEND_PID
