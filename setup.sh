#!/bin/bash

echo "Tactiq API Setup"

echo ""
echo "1. Installing dependencies..."
cd TactiqAPI
dotnet restore

echo ""
echo "2. Starting PostgreSQL with Docker..."
cd ..
docker-compose up -d

echo ""
echo "3. Waiting for PostgreSQL to start..."
sleep 5

echo ""
echo "4. Applying migrations..."
cd TactiqAPI
dotnet ef database update

echo ""
echo "5. Setup complete!"
echo ""
echo "To run the API:"
echo "  cd TactiqAPI"
echo "  dotnet run"
echo ""
echo "Swagger UI will be available at: https://localhost:5001/swagger"
