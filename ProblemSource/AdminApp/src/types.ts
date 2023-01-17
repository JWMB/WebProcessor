export enum SeverityLevel {
    information,
    warning,
    error,
    critical
}

export interface NotificationItem {
    createdAt: Date;
    text: string;
    severity?: SeverityLevel;
}

export interface TrainingUpdateMessage {
    trainingId: number;
    username: string;
    events: any[];
    message?: string;
}
