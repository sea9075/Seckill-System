import { apiClient } from "./client";

export interface HealthResponse {
    status: string;
    timestamp: string;
}

export const getHealth = () => apiClient.get<HealthResponse>("/api/health");