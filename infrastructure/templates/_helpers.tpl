{{/*
Expand the name of the chart.
*/}}
{{- define "durable-task-scheduler.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
*/}}
{{- define "durable-task-scheduler.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "durable-task-scheduler.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common labels
*/}}
{{- define "durable-task-scheduler.labels" -}}
helm.sh/chart: {{ include "durable-task-scheduler.chart" . }}
{{ include "durable-task-scheduler.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{/*
Selector labels
*/}}
{{- define "durable-task-scheduler.selectorLabels" -}}
app.kubernetes.io/name: {{ include "durable-task-scheduler.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{/*
Image name helper
Takes a dict with "root" (root context) and "image" (image values with repository and tag)
*/}}
{{- define "durable-task-scheduler.image" -}}
{{- $registry := .root.Values.imageRegistry | default "" }}
{{- if and $registry (ne $registry "") }}
{{- printf "%s/%s:%s" $registry .image.repository .image.tag }}
{{- else }}
{{- printf "%s:%s" .image.repository .image.tag }}
{{- end }}
{{- end }}

