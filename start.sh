#!/bin/bash

echo "🚀 Starting Project Manager..."
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Start backend
echo -e "${YELLOW}Starting Backend (.NET 10)...${NC}"
cd ProjectManagerWebAPI
dotnet run &
BACKEND_PID=$!

# Wait for backend to start
sleep 3

# Start frontend
echo -e "${YELLOW}Starting Frontend (Angular)...${NC}"
cd ../ProjectManager/ProjectManagerWebUI
ng serve &
FRONTEND_PID=$!

echo ""
echo -e "${GREEN}✓ Backend started (PID: $BACKEND_PID)${NC}"
echo -e "${GREEN}✓ Frontend started (PID: $FRONTEND_PID)${NC}"
echo ""
echo -e "${YELLOW}Backend:  http://localhost:5000${NC}"
echo -e "${YELLOW}Frontend: http://localhost:4200${NC}"
echo ""
echo "Press Ctrl+C to stop both servers"
echo ""

# Wait for both processes
wait $BACKEND_PID $FRONTEND_PID
