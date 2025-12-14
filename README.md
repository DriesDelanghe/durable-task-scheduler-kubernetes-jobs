# Durable Task Scheduler for Kubernetes

A proof-of-concept implementation of Azure Durable Functions orchestrating Kubernetes Jobs.

## Quick Start

```bash
# Build and deploy
make all

# Wait for pods to be ready (about 90 seconds)
make status

# Port forward and test
make port-forward
# In another terminal:
curl -X POST http://localhost:7071/api/TaskScheduler_HttpStart
```

## Architecture

```
HTTP Request → TaskScheduler → k8sController → Kubernetes API → ExampleWorker (Job)
                    ↓
              DTS Emulator (Task Storage)
```

## Components

| Component | Description |
|-----------|-------------|
| **TaskScheduler** | Azure Durable Functions orchestrator |
| **k8sController** | REST API for Kubernetes job management |
| **ExampleWorker** | Sample container that runs as K8s Jobs |
| **DTS Emulator** | Durable Task Scheduler storage emulator |

## Makefile Commands

| Command | Description |
|---------|-------------|
| `make all` | Build and deploy |
| `make build` | Build Docker images |
| `make deploy` | Deploy to Kubernetes |
| `make status` | Show pod status |
| `make port-forward` | Forward task-scheduler to localhost:7071 |
| `make logs-task-scheduler` | View task-scheduler logs |
| `make logs-k8s-controller` | View k8s-controller logs |
| `make logs-dts-emulator` | View emulator logs |
| `make undeploy` | Remove deployment |
| `make clean` | Remove Docker images |

## Prerequisites

- Docker Desktop with Kubernetes enabled
- Helm 3.x
- Make

## How It Works

1. HTTP POST to `/api/TaskScheduler_HttpStart` triggers orchestration
2. Orchestrator creates a Kubernetes Job via k8sController
3. Watches job completion using Server-Sent Events (SSE)
4. Retrieves logs and extracts structured JSON result
5. Returns the result to the caller

## Configuration

Edit `infrastructure/values.yaml` to customize settings.

## Troubleshooting

**Pods not starting**: Wait 90 seconds after deploy, Azure Functions takes time to start.

**500 errors**: Check logs with `make logs-task-scheduler`

**Task hub not found**: Ensure emulator and task-scheduler are both running.
