.PHONY: help build deploy undeploy status logs clean

# Configuration
NAMESPACE ?= dts-poc
RELEASE_NAME ?= durable-task-scheduler
IMAGE_TAG ?= latest

help: ## Show this help message
	@echo 'Usage: make [target]'
	@echo ''
	@echo 'Available targets:'
	@awk 'BEGIN {FS = ":.*?## "} /^[a-zA-Z_-]+:.*?## / {printf "  %-20s %s\n", $$1, $$2}' $(MAKEFILE_LIST)

build: ## Build all Docker images
	@echo "Building k8s-controller image..."
	docker build -t k8s-controller:$(IMAGE_TAG) -f src/k8sController/Dockerfile .
	@echo "Building task-scheduler image (amd64 for Azure Functions compatibility)..."
	docker build --platform linux/amd64 -t task-scheduler:$(IMAGE_TAG) -f src/TaskScheduler/Dockerfile .
	@echo "Building example-worker image..."
	docker build -t example-worker:$(IMAGE_TAG) -f src/ExampleWorker/Dockerfile .
	@echo "All images built successfully."

deploy: ## Deploy to Kubernetes using Helm
	@which helm > /dev/null || (echo "Error: helm is not installed. Install from https://helm.sh/docs/intro/install/" && exit 1)
	@echo "Deploying to Kubernetes..."
	helm upgrade --install $(RELEASE_NAME) ./infrastructure \
		--namespace $(NAMESPACE) \
		--create-namespace \
		--set k8sController.image.repository=k8s-controller \
		--set k8sController.image.tag=$(IMAGE_TAG) \
		--set taskScheduler.image.repository=task-scheduler \
		--set taskScheduler.image.tag=$(IMAGE_TAG) \
		--set exampleWorker.image.repository=example-worker \
		--set exampleWorker.image.tag=$(IMAGE_TAG) \
		--set kubernetes.workerImage=example-worker:$(IMAGE_TAG) \
		--set kubernetes.defaultNamespace=$(NAMESPACE) \
		--set imagePullPolicy=Never

undeploy: ## Remove deployment from Kubernetes
	@echo "Removing deployment..."
	helm uninstall $(RELEASE_NAME) --namespace $(NAMESPACE) || true

status: ## Show deployment status
	@echo "=== Pods ==="
	@kubectl get pods -n $(NAMESPACE) -l app.kubernetes.io/instance=$(RELEASE_NAME) 2>/dev/null || echo "No pods found"
	@echo ""
	@echo "=== Services ==="
	@kubectl get services -n $(NAMESPACE) -l app.kubernetes.io/instance=$(RELEASE_NAME) 2>/dev/null || echo "No services found"

logs-k8s-controller: ## Show k8s-controller logs
	@kubectl logs -n $(NAMESPACE) -l component=k8s-controller --tail=100 -f

logs-task-scheduler: ## Show task-scheduler logs
	@kubectl logs -n $(NAMESPACE) -l component=task-scheduler --tail=100 -f

logs-dts-emulator: ## Show dts-emulator logs
	@kubectl logs -n $(NAMESPACE) -l component=dts-emulator --tail=100 -f

# port-forward: ## Port forward task-scheduler service
# 	@echo "Port forwarding task-scheduler to http://localhost:7071"
# 	@echo "Press Ctrl+C to stop"
# 	@kubectl port-forward -n $(NAMESPACE) svc/$(RELEASE_NAME)-task-scheduler 7071:80

clean: ## Clean up Docker images
	@echo "Cleaning up Docker images..."
	-docker rmi k8s-controller:$(IMAGE_TAG) 2>/dev/null || true
	-docker rmi task-scheduler:$(IMAGE_TAG) 2>/dev/null || true
	-docker rmi example-worker:$(IMAGE_TAG) 2>/dev/null || true

# Main workflow
all: build deploy ## Build and deploy (recommended for local development)
