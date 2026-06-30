# Inventory Hold Microservice

This workspace contains a .NET 10 inventory hold service, MongoDB/Redis/RabbitMQ-backed infrastructure, NUnit tests, and a React + TypeScript frontend.

## What is included
- Backend API with endpoints for creating, retrieving, and releasing holds
- MongoDB-backed inventory and hold persistence with atomic stock reservation
- Redis-backed caching for inventory and hold reads
- RabbitMQ event publishing for hold lifecycle actions
- React dashboard for inventory, holds, and release actions
- Docker compose startup for the full environment

## Run locally

### Backend + infrastructure
```bash
docker compose up --build
```

The API will be available at http://localhost:8080.

### Frontend
```bash
cd ui
npm install
npm run dev
```

The UI will be available at http://localhost:3000.

## API overview
- POST /api/holds
- GET /api/holds/{holdId}
- DELETE /api/holds/{holdId}
- GET /api/inventory

## Tests
```bash
dotnet test InventoryHold.UnitTests/InventoryHold.UnitTests.csproj
```
